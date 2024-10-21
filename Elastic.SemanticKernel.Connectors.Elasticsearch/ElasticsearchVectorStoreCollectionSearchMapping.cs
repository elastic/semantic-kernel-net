using System;
using System.Collections.Generic;
using System.Linq;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
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
            // TODO: Implement full text search

            switch (filterClause)
            {
                case EqualToFilterClause equalToClause:
                {
                    var field = GetPropertyMapping(equalToClause.FieldName).Value;

                    filterQueries.Add(Query.Term(new TermQuery(field!)
                    {
                        Value = FieldValueFromValue(equalToClause.Value)
                    }));

                    break;
                }
                case AnyTagEqualToFilterClause anyTagEqualToClause:
                {
                    var field = GetPropertyMapping(anyTagEqualToClause.FieldName).Value;

                    // TODO: Replace with TermsQuery
                    filterQueries.Add(Query.TermsSet(new TermsSetQuery(field!)
                    {
                        Terms = [FieldValueFromValue(anyTagEqualToClause.Value)],
                        MinimumShouldMatchScript = new Script
                        {
                            Source = "1"
                        }
                    }));

                    break;
                }

                default:
                    throw new NotSupportedException($"Filter clause of type {filterClause.GetType().FullName} is not supported.");
            }
        }

        return filterQueries;

        KeyValuePair<VectorStoreRecordProperty, string> GetPropertyMapping(string dataModelPropertyName)
        {
            var result = propertyToStorageName
                .FirstOrDefault(x => string.Equals(x.Key.DataModelPropertyName, dataModelPropertyName, StringComparison.Ordinal));

            if (result.Key is null)
            {
                throw new NotSupportedException($"Property '{dataModelPropertyName}' is not supported as a filter value.");
            }

            return result;
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
