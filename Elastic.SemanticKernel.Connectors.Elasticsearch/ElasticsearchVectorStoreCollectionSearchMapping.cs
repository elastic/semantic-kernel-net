// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.SemanticKernel.Connectors.Elasticsearch.Internal.Helpers;

using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.VectorData.ConnectorSupport;
using Microsoft.SemanticKernel;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
/// Contains mapping helpers to use when searching for documents using Elasticsearch.
/// </summary>
internal static class ElasticsearchVectorStoreCollectionSearchMapping
{
#pragma warning disable CS0618 // Type or member is obsolete

    /// <summary>
    /// Build a list of Elasticsearch filter <see cref="Query"/> from the provided <see cref="VectorSearchFilter"/>.
    /// </summary>
    /// <param name="basicVectorSearchFilter">The <see cref="VectorSearchFilter"/> to build the Elasticsearch filter queries from.</param>
    /// <param name="model">A model representing a record in a vector store collection.</param>
    /// <returns>The Elasticsearch filter queries.</returns>
    /// <exception cref="NotSupportedException">Thrown when the provided filter contains unsupported types, values or unknown properties.</exception>
    public static Query? BuildFromLegacyFilter(VectorSearchFilter? basicVectorSearchFilter, VectorStoreRecordModel model)
    {
        Verify.NotNull(model);

        if (basicVectorSearchFilter is null)
        {
            return null;
        }

        var filterClauses = basicVectorSearchFilter.FilterClauses.ToArray();
        var filterQueries = new List<Query>();

        foreach (var filterClause in filterClauses)
        {
            switch (filterClause)
            {
                case EqualToFilterClause equalToClause:
                {
                    var propertyModel = GetPropertyNameMapping(equalToClause.FieldName);

                    filterQueries.Add(new TermQuery(field: propertyModel.StorageName!) { Value = FieldValueFactory.FromValue(equalToClause.Value) });

                    break;
                }
                case AnyTagEqualToFilterClause anyTagEqualToClause:
                {
                    var propertyModel = GetPropertyNameMapping(anyTagEqualToClause.FieldName);

                    filterQueries.Add(new TermsQuery
                    {
                        Field = propertyModel.StorageName!,
                        Terms = new TermsQueryField([FieldValueFactory.FromValue(anyTagEqualToClause.Value)])
                    });

                    break;
                }

                default:
                    throw new InvalidOperationException($"Unsupported filter clause type '{filterClause.GetType().Name}'.");
            }
        }

        return Query.Bool(new() { Must = filterQueries });

        VectorStoreRecordDataPropertyModel GetPropertyNameMapping(string fieldName)
        {
            if (!model.PropertyMap.TryGetValue(fieldName, out var property))
            {
                throw new InvalidOperationException($"Property name '{fieldName}' provided as part of the filter clause is not a valid property name.");
            }

            if (property is not VectorStoreRecordDataPropertyModel { IsIndexed: true } dataProperty)
            {
                throw new InvalidOperationException($"Property '{fieldName}' is not an indexed data property.");
            }

            return dataProperty;
        }
    }

#pragma warning restore CS0618 // Type or member is obsolete
}
