// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

using Elastic.Clients.Elasticsearch;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
/// Represents a collection of vector store records in an Elasticsearch database, mapped to a dynamic <c>Dictionary&lt;string, object?&gt;</c>.
/// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix

public sealed class ElasticsearchDynamicCollection :
    ElasticsearchCollection<object, Dictionary<string, object?>>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticsearchDynamicCollection"/> class.
    /// </summary>
    /// <param name="elasticsearchClient">Elasticsearch client that can be used to manage the collections and documents in an Elasticsearch store.</param>
    /// <param name="name">The name of the collection.</param>
    /// <param name="ownsClient">A value indicating whether <paramref name="elasticsearchClient"/> is disposed when the collection is disposed.</param>
    /// <param name="options">Optional configuration options for this class.</param>
    public ElasticsearchDynamicCollection(ElasticsearchClient elasticsearchClient, string name, bool ownsClient, ElasticsearchCollectionOptions options)
        : this(() => new MockableElasticsearchClient(elasticsearchClient, ownsClient), name, options)
    {
    }

    internal ElasticsearchDynamicCollection(Func<MockableElasticsearchClient> clientFactory, string name, ElasticsearchCollectionOptions options)
        : base(
            clientFactory,
            name,
            static (client, options) => new ElasticsearchModelBuilder(client.ElasticsearchClient.ElasticsearchClientSettings)
                .BuildDynamic(
                    options.Definition ?? throw new ArgumentException("Definition is required for dynamic collections."),
                    options.EmbeddingGenerator),
            options)
    {
    }
}
