// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

using Microsoft.Extensions.AI;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
///     Options when creating a <see cref="ElasticsearchVectorStoreOptions" />.
/// </summary>
public sealed class ElasticsearchVectorStoreOptions
{
    // TODO: Do we want to support a prefix here that is used to filter collections/indices (and automatically prepend when creating a collection, etc.) or is that something that should be up to the user?

    /// <summary>
    /// Gets or sets the default embedding generator to use when generating vectors embeddings with this vector store.
    /// </summary>
    public IEmbeddingGenerator? EmbeddingGenerator { get; init; }

    /// <summary>
    /// An optional factory to use for constructing <see cref="ElasticsearchVectorStoreRecordCollection{TKey, TRecord}" />instances, if a custom record collection is required.
    /// </summary>
    [Obsolete("To control how collections are instantiated, extend your provider's IVectorStore implementation and override GetCollection()")]
    public IElasticsearchVectorStoreRecordCollectionFactory? VectorStoreCollectionFactory { get; init; }
}
