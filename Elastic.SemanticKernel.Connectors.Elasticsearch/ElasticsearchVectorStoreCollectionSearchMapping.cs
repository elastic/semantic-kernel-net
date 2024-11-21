// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
/// Contains mapping helpers to use when searching for documents using Elasticsearch.
/// </summary>
internal static class ElasticsearchVectorStoreCollectionSearchMapping
{
    /// <summary>
    /// Build a list of Elasticsearch filter <see cref="Query"/> from the provided <see cref="VectorSearchFilter"/>.
    /// </summary>
    /// <param name="basicVectorSearchFilter">The <see cref="VectorSearchFilter"/> to build the Elasticsearch filter queries from.</param>
    /// <param name="propertyToStorageName">A mapping from <see cref="VectorStoreRecordDefinition" /> to storage model property name.</param>
    /// <returns>The Elasticsearch filter queries.</returns>
    /// <exception cref="NotSupportedException">Thrown when the provided filter contains unsupported types, values or unknown properties.</exception>
    public static ICollection<Query> BuildFilter(VectorSearchFilter? basicVectorSearchFilter, Dictionary<VectorStoreRecordProperty, string> propertyToStorageName)
    {
        Verify.NotNull(propertyToStorageName);

        if (basicVectorSearchFilter is null)
        {
            return [];
        }

        var filterClauses = basicVectorSearchFilter.FilterClauses.ToArray();
        var filterQueries = new List<Query>();

        foreach (var filterClause in filterClauses)
        {
            switch (filterClause)
            {
                case EqualToFilterClause equalToClause:
                {
                    var mapping = GetPropertyNameMapping(equalToClause.FieldName);
                    VerifyFilterable(mapping.Key);

                    filterQueries.Add(Query.Term(new TermQuery(mapping.Value!)
                    {
                        Value = FieldValueFromValue(equalToClause.Value)
                    }));

                    break;
                }
                case AnyTagEqualToFilterClause anyTagEqualToClause:
                {
                    var mapping = GetPropertyNameMapping(anyTagEqualToClause.FieldName);
                    VerifyFilterable(mapping.Key);

                    filterQueries.Add(Query.Terms(new TermsQuery
                    {
                        Field = mapping.Value!,
                        Terms = new TermsQueryField([FieldValueFromValue(anyTagEqualToClause.Value)])
                    }));

                    break;
                }

                default:
                    throw new NotSupportedException($"Filter clause of type {filterClause.GetType().FullName} is not supported.");
            }
        }

        return filterQueries;

        KeyValuePair<VectorStoreRecordProperty, string> GetPropertyNameMapping(string dataModelPropertyName)
        {
            var result = propertyToStorageName
                .FirstOrDefault(x => string.Equals(x.Key.DataModelPropertyName, dataModelPropertyName, StringComparison.Ordinal));

            if (result.Key is null)
            {
                throw new NotSupportedException($"Property '{dataModelPropertyName}' is not supported as a filter value.");
            }

            return result;
        }

        static void VerifyFilterable(VectorStoreRecordProperty property)
        {
            if (property is not VectorStoreRecordDataProperty { IsFilterable: true })
            {
                throw new NotSupportedException($"Property '{property.DataModelPropertyName}' can not be used for filtering.");
            }
        }
    }

    /// <summary>
    /// TODO: TBC
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    private static FieldValue FieldValueFromValue(object? value)
    {
        // TODO: Implement FieldValue.FromValue() in Elasticsearch client
        // TODO: FieldValue.Any()
        // TODO: FieldValue.Array()

        return value switch
        {
            null => FieldValue.Null,
            bool v => v,
            float v => v,
            double v => v,
            sbyte v => v,
            short v => v,
            int v => v,
            long v => v,
            byte v => v,
            ushort v => v,
            uint v => v,
            ulong v => v,
            string v => v,
            char v => v,
            _ => throw new NotSupportedException($"Unsupported filter value type '{value!.GetType().Name}'.")
        };
    }
}
