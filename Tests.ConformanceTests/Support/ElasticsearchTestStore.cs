// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Serialization;
using Elastic.SemanticKernel.Connectors.Elasticsearch;
using Elastic.Transport;

using Microsoft.Extensions.VectorData;

using Testcontainers.Elasticsearch;

using VectorData.ConformanceTests.Support;

namespace Elasticsearch.ConformanceTests.Support;

#pragma warning disable CA1001 // Type owns disposable fields but is not disposable

internal sealed class ElasticsearchTestStore : TestStore
{
    public static ElasticsearchTestStore Instance { get; } = new();

    private readonly ElasticsearchContainer _container = new ElasticsearchBuilder()
        .WithImage("elasticsearch:8.18.1")
        .WithEnvironment("discovery.type", "single-node")
        .WithEnvironment("xpack.security.enabled", "false")
        .WithEnvironment("xpack.license.self_generated.type", "trial")
        .Build();

    private ElasticsearchClient? _client;

    public ElasticsearchClient Client => this._client ?? throw new InvalidOperationException("Not initialized");

    public ElasticsearchVectorStore GetVectorStore(ElasticsearchVectorStoreOptions options)
        => new(this.Client,
            ownsClient: false, // The client is shared, it's not owned by the vector store.
            new()
            {
                EmbeddingGenerator = options.EmbeddingGenerator
            });

    private ElasticsearchTestStore()
    {
    }

    public override bool VectorsComparable => true;

    protected override async Task StartAsync()
    {
        await this._container.StartAsync();

        var host = this._container.Hostname;
        var port = this._container.GetMappedPublicPort(ElasticsearchBuilder.ElasticsearchHttpsPort);

        var settings = new ElasticsearchClientSettings(
                new SingleNodePool(new Uri($"http://{host}:{port}")),
                (_, settings) => new DefaultSourceSerializer(settings, x => x.DefaultIgnoreCondition = JsonIgnoreCondition.Never)
            )
            .EnableDebugMode();
        this._client = new ElasticsearchClient(settings);

        // The client is shared, it's not owned by the vector store.
        this.DefaultVectorStore = new ElasticsearchVectorStore(this._client, ownsClient: false, new());
    }

    protected override async Task StopAsync()
    {
        this._client?.ElasticsearchClientSettings.Dispose();
        await this._container.StopAsync();
    }
}
