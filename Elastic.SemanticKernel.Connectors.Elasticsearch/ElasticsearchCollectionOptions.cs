// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.VectorData;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
/// Options when creating a <see cref="ElasticsearchCollection{TKey, TRecord}"/>.
/// </summary>
public sealed class ElasticsearchCollectionOptions :
    VectorStoreCollectionOptions
{
    internal static readonly ElasticsearchCollectionOptions Default = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticsearchCollectionOptions"/> class.
    /// </summary>
    public ElasticsearchCollectionOptions()
    {
    }

    internal ElasticsearchCollectionOptions(ElasticsearchCollectionOptions? source) :
        base(source)
    {
    }
}
