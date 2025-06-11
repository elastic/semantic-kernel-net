// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.AI;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
/// Options when creating a <see cref="ElasticsearchVectorStore" />.
/// </summary>
public sealed class ElasticsearchVectorStoreOptions
{
    internal static readonly ElasticsearchVectorStoreOptions Default = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticsearchVectorStoreOptions"/> class.
    /// </summary>
    public ElasticsearchVectorStoreOptions()
    {
    }

    internal ElasticsearchVectorStoreOptions(ElasticsearchVectorStoreOptions? source)
    {
        EmbeddingGenerator = source?.EmbeddingGenerator;
    }

    /// <summary>
    /// Gets or sets the default embedding generator to use when generating vectors embeddings with this vector store.
    /// </summary>
    public IEmbeddingGenerator? EmbeddingGenerator { get; set; }
}
