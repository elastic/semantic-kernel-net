// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
///     Options when creating a <see cref="ElasticsearchVectorStoreOptions" />.
/// </summary>
public sealed class ElasticsearchVectorStoreOptions
{
    // TODO: Do we want to support a prefix here that is used to filter collections/indices (and automatically prepend when creating a collection, etc.) or is that something that should be up to the user?

    /// <summary>
    ///     An optional factory to use for constructing <see cref="ElasticsearchVectorStoreRecordCollection{TRecord}" />
    ///     instances, if a custom record collection is required.
    /// </summary>
    public IElasticsearchVectorStoreRecordCollectionFactory? VectorStoreCollectionFactory { get; init; }
}
