// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Elastic.Extensions.AI.Tests;

public class DependencyInjectionTests
{
    private const string Host = "localhost";
    private const int Port = 9200;
    private const string ApiKey = "fakeKey";

    private static readonly IElasticsearchClientSettings ClientSettings =
        new ElasticsearchClientSettings(new SingleNodePool(new Uri($"https://{Host}:{Port}")))
            .Authentication(new ApiKey(ApiKey));

    private static readonly ElasticsearchEmbeddingGeneratorOptions Options = new()
    {
        InferenceEndpointId = "test-endpoint"
    };

    [Fact]
    public void AddElasticsearchEmbeddingGenerator_WithClientSettings_RegistersServices()
    {
        var services = new ServiceCollection();

        services.AddElasticsearchEmbeddingGenerator<Embedding<float>>(
            ClientSettings,
            Options);

        var provider = services.BuildServiceProvider();

        var generator = provider.GetService<ElasticsearchEmbeddingGenerator<Embedding<float>>>();
        Assert.NotNull(generator);

        var interfaceGenerator = provider.GetService<IEmbeddingGenerator<string, Embedding<float>>>();
        Assert.NotNull(interfaceGenerator);
        Assert.Same(generator, interfaceGenerator);
    }

    [Fact]
    public void AddKeyedElasticsearchEmbeddingGenerator_WithClientSettings_RegistersKeyedServices()
    {
        var services = new ServiceCollection();
        const string serviceKey = "my-key";

        services.AddKeyedElasticsearchEmbeddingGenerator<Embedding<float>>(
            serviceKey,
            ClientSettings,
            Options);

        var provider = services.BuildServiceProvider();

        var generator = provider.GetKeyedService<ElasticsearchEmbeddingGenerator<Embedding<float>>>(serviceKey);
        Assert.NotNull(generator);

        var interfaceGenerator = provider.GetKeyedService<IEmbeddingGenerator<string, Embedding<float>>>(serviceKey);
        Assert.NotNull(interfaceGenerator);
        Assert.Same(generator, interfaceGenerator);
    }

    [Fact]
    public void AddElasticsearchEmbeddingGenerator_WithClientFromDI_RegistersServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ElasticsearchClient>(_ => new ElasticsearchClient(ClientSettings));
        services.AddElasticsearchEmbeddingGenerator<Embedding<float>>(
            clientProvider: null,
            optionsProvider: _ => Options);

        var provider = services.BuildServiceProvider();

        var generator = provider.GetService<ElasticsearchEmbeddingGenerator<Embedding<float>>>();
        Assert.NotNull(generator);
    }

    [Fact]
    public void AddElasticsearchEmbeddingGenerator_WithClientProvider_RegistersServices()
    {
        var services = new ServiceCollection();

        services.AddElasticsearchEmbeddingGenerator<Embedding<float>>(
            clientProvider: _ => new ElasticsearchClient(ClientSettings),
            optionsProvider: _ => Options);

        var provider = services.BuildServiceProvider();

        var generator = provider.GetService<ElasticsearchEmbeddingGenerator<Embedding<float>>>();
        Assert.NotNull(generator);
    }

    [Fact]
    public void AddElasticsearchEmbeddingGenerator_WithByteEmbedding_RegistersServices()
    {
        var services = new ServiceCollection();

        services.AddElasticsearchEmbeddingGenerator<Embedding<byte>>(
            ClientSettings,
            Options);

        var provider = services.BuildServiceProvider();

        var generator = provider.GetService<ElasticsearchEmbeddingGenerator<Embedding<byte>>>();
        Assert.NotNull(generator);

        var interfaceGenerator = provider.GetService<IEmbeddingGenerator<string, Embedding<byte>>>();
        Assert.NotNull(interfaceGenerator);
    }

    [Fact]
    public void AddElasticsearchEmbeddingGenerator_WithNullClientSettings_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddElasticsearchEmbeddingGenerator<Embedding<float>>(
                clientSettings: null!,
                Options));
    }

    [Fact]
    public void AddElasticsearchEmbeddingGenerator_WithNullOptions_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddElasticsearchEmbeddingGenerator<Embedding<float>>(
                ClientSettings,
                options: null!));
    }

    [Fact]
    public void AddElasticsearchEmbeddingGenerator_WithNullOptionsProvider_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddElasticsearchEmbeddingGenerator<Embedding<float>>(
                clientProvider: null,
                optionsProvider: null!));
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddElasticsearchEmbeddingGenerator_RespectsServiceLifetime(ServiceLifetime lifetime)
    {
        var services = new ServiceCollection();

        services.AddElasticsearchEmbeddingGenerator<Embedding<float>>(
            ClientSettings,
            Options,
            lifetime);

        var descriptor = Assert.Single(services, d =>
            d.ServiceType == typeof(ElasticsearchEmbeddingGenerator<Embedding<float>>));
        Assert.Equal(lifetime, descriptor.Lifetime);
    }
}
