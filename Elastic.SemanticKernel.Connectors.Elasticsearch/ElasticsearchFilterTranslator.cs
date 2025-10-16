using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.SemanticKernel.Connectors.Elasticsearch.Internal.Helpers;

using Microsoft.Extensions.VectorData.ProviderServices;
using Microsoft.Extensions.VectorData.ProviderServices.Filter;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

internal sealed class ElasticsearchFilterTranslator
{
    private CollectionModel _model = null!;
    private ParameterExpression _recordParameter = null!;

    public Query? Translate(LambdaExpression lambdaExpression, CollectionModel model)
    {
        _model = model;

        Debug.Assert(lambdaExpression.Parameters.Count == 1);
        _recordParameter = lambdaExpression.Parameters[0];

        var preprocessor = new FilterTranslationPreprocessor { SupportsParameterization = false };
        var preprocessedExpression = preprocessor.Visit(lambdaExpression.Body);

        return Translate(preprocessedExpression);
    }

    private Query Translate(Expression? node)
    {
        return node switch
        {
            BinaryExpression { NodeType: ExpressionType.Equal } equal => TranslateEqual(equal.Left, equal.Right),
            BinaryExpression { NodeType: ExpressionType.NotEqual } notEqual => TranslateEqual(notEqual.Left, notEqual.Right, negated: true),

            BinaryExpression
            {
                NodeType: ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual or ExpressionType.LessThan or ExpressionType.LessThanOrEqual
            } comparison => TranslateComparison(comparison),

            BinaryExpression { NodeType: ExpressionType.AndAlso } andAlso => TranslateAndAlso(andAlso.Left, andAlso.Right),
            BinaryExpression { NodeType: ExpressionType.OrElse } orElse => TranslateOrElse(orElse.Left, orElse.Right),
            UnaryExpression { NodeType: ExpressionType.Not } not => TranslateNot(not.Operand),

            // Special handling for bool constant as the filter expression (r => r.Bool)
            { } when node.Type == typeof(bool) && TryBindProperty(node, out var property) => GenerateEqual(property.StorageName, value: true),

            MethodCallExpression methodCall => TranslateMethodCall(methodCall),

#pragma warning disable CA1508
            _ => throw new NotSupportedException("Elasticsearch does not support the following NodeType in filters: " + node?.NodeType)
#pragma warning restore CA1508
        };
    }

    private Query TranslateEqual(Expression left, Expression right, bool negated = false)
    {
        return TryBindProperty(left, out var property) && right is ConstantExpression { Value: var rightConstant }
            ? GenerateEqual(property.StorageName, rightConstant, negated)
            : TryBindProperty(right, out property) && left is ConstantExpression { Value: var leftConstant }
                ? GenerateEqual(property.StorageName, leftConstant, negated)
                : throw new NotSupportedException("Invalid equality/comparison.");
    }

    private static Query GenerateEqual(string propertyStorageName, object? value, bool negated = false)
    {
        Query coreQuery = value is null
            ? new BoolQuery { MustNot = [ new ExistsQuery(propertyStorageName) ] }
            : new TermQuery(propertyStorageName, FieldValueFactory.FromValue(value));

        return negated
            ? new BoolQuery { MustNot = [coreQuery] }
            : coreQuery;
    }

    private Query TranslateComparison(BinaryExpression comparison)
    {
        return TryBindProperty(comparison.Left, out var property) && comparison.Right is ConstantExpression { Value: var rightConstant }
            ? GenerateComparison(comparison.NodeType, property.StorageName, rightConstant)
            : TryBindProperty(comparison.Right, out property) && comparison.Left is ConstantExpression { Value: var leftConstant }
                ? GenerateComparison(comparison.NodeType, property.StorageName, leftConstant)
                : throw new NotSupportedException("Comparison expression not supported by Elasticsearch.");
    }

    private static Query GenerateComparison(ExpressionType nodeType, string propertyStorageName, object? value)
    {
        return nodeType switch
        {
            ExpressionType.GreaterThan => new UntypedRangeQuery(propertyStorageName) { Gt = value },
            ExpressionType.GreaterThanOrEqual => new UntypedRangeQuery(propertyStorageName) { Gte = value },
            ExpressionType.LessThan => new UntypedRangeQuery(propertyStorageName) { Lt = value },
            ExpressionType.LessThanOrEqual => new UntypedRangeQuery(propertyStorageName) { Lte = value },

            _ => throw new InvalidOperationException("Unreachable")
        };
    }

    private Query TranslateAndAlso(Expression left, Expression right)
    {
        var leftFilter = Translate(left);
        var rightFilter = Translate(right);

        return leftFilter && rightFilter;
    }

    private Query TranslateOrElse(Expression left, Expression right)
    {
        var leftFilter = Translate(left);
        var rightFilter = Translate(right);

        return leftFilter || rightFilter;
    }

    private Query TranslateNot(Expression expression)
    {
        var filter = Translate(expression);

        return !filter;
    }

    private Query TranslateMethodCall(MethodCallExpression methodCall)
    {
        return methodCall switch
        {
            // Enumerable.Contains()
            {
                Method.Name: nameof(Enumerable.Contains), Arguments: [var source, var item]
            } contains
                when (contains.Method.DeclaringType == typeof(Enumerable))
                => TranslateContains(source, item),

            // List.Contains()
            {
                Method:
                {
                    Name: nameof(Enumerable.Contains),
                    DeclaringType: { IsGenericType: true } declaringType
                },
                Object: { } source,
                Arguments: [var item]
            }
                when (declaringType.GetGenericTypeDefinition() == typeof(List<>))
                => TranslateContains(source, item),

            _ => throw new NotSupportedException($"Unsupported method call: {methodCall.Method.DeclaringType?.Name}.{methodCall.Method.Name}.")
        };
    }

    private Query TranslateContains(Expression source, Expression item)
    {
        switch (source)
        {
            // Contains over field enumerable.
            case var _ when TryBindProperty(source, out var enumerableProperty):
            {
                if (item is not ConstantExpression constant)
                {
                    throw new NotSupportedException("Value must be a constant.");
                }

                return new TermsQuery(enumerableProperty.StorageName, new TermsQueryField([FieldValueFactory.FromValue(constant.Value)]));
            }

            // Contains over inline enumerable.
            case NewArrayExpression newArray:
            {
                var elements = new object?[newArray.Expressions.Count];

                for (var i = 0; i < newArray.Expressions.Count; i++)
                {
                    if (newArray.Expressions[i] is not ConstantExpression { Value: var elementValue })
                    {
                        throw new NotSupportedException("Inline array elements must be constants.");
                    }

                    elements[i] = elementValue;
                }

                return ProcessInlineEnumerable(elements, item);
            }

            case ConstantExpression { Value: IEnumerable enumerable and not string }:
            {
                return ProcessInlineEnumerable(enumerable, item);
            }

            default:
                throw new NotSupportedException("Unsupported Contains filter.");
        }

        Query ProcessInlineEnumerable(IEnumerable elements, Expression item)
        {
            if (!TryBindProperty(item, out var property))
            {
                throw new NotSupportedException("Unsupported item type in Contains filter.");
            }

            var values = new List<FieldValue>();

            foreach (var element in elements)
            {
                values.Add(FieldValueFactory.FromValue(element));
            }

            return new TermsQuery(property.StorageName, new TermsQueryField(values.ToArray()));
        }
    }

    private bool TryBindProperty(Expression expression, [NotNullWhen(true)] out PropertyModel? property)
    {
        var unwrappedExpression = expression;
        while (unwrappedExpression is UnaryExpression { NodeType: ExpressionType.Convert } convert)
        {
            unwrappedExpression = convert.Operand;
        }

        var modelName = unwrappedExpression switch
        {
            // Regular member access for strongly-typed POCO binding (e.g. r => r.SomeInt == 8)
            MemberExpression memberExpression when memberExpression.Expression == this._recordParameter
                => memberExpression.Member.Name,

            // Dictionary lookup for weakly-typed dynamic binding (e.g. r => r["SomeInt"] == 8)
            MethodCallExpression
            {
                Method: { Name: "get_Item", DeclaringType: var declaringType },
                Arguments: [ConstantExpression { Value: string keyName }]
            } methodCall when methodCall.Object == this._recordParameter && declaringType == typeof(Dictionary<string, object?>)
                => keyName,

            _ => null
        };

        if (modelName is null)
        {
            property = null;
            return false;
        }

        if (!this._model.PropertyMap.TryGetValue(modelName, out property))
        {
            throw new InvalidOperationException($"Property name '{modelName}' provided as part of the filter clause is not a valid property name.");
        }

        // Now that we have the property, go over all wrapping Convert nodes again to ensure that they're compatible with the property type
        unwrappedExpression = expression;
        while (unwrappedExpression is UnaryExpression { NodeType: ExpressionType.Convert } convert)
        {
            var convertType = Nullable.GetUnderlyingType(convert.Type) ?? convert.Type;
            if (convertType != property.Type && convertType != typeof(object))
            {
                throw new InvalidCastException($"Property '{property.ModelName}' is being cast to type '{convert.Type.Name}', but its configured type is '{property.Type.Name}'.");
            }

            unwrappedExpression = convert.Operand;
        }

        return true;
    }
}
