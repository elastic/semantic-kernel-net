// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Elastic.Clients.Elasticsearch.Inference;

using Microsoft.Extensions.AI;

using Xunit;

namespace Elastic.Extensions.AI.Tests;

public class ElasticsearchEmbeddingGeneratorTests
{
    [Fact]
    public void Constructor_WithNullClient_ThrowsArgumentNullException()
    {
        var options = new ElasticsearchEmbeddingGeneratorOptions
        {
            InferenceEndpointId = "test-endpoint"
        };

        Assert.Throws<ArgumentNullException>(() =>
            new ElasticsearchEmbeddingGenerator<Embedding<float>>(
                (ElasticsearchInferenceClient)null!,
                options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        using var mockClient = new MockElasticsearchInferenceClient();

        Assert.Throws<ArgumentNullException>(() =>
            new ElasticsearchEmbeddingGenerator<Embedding<float>>(
                mockClient,
                null!));
    }

    [Fact]
    public void Constructor_WithEmptyInferenceEndpointId_ThrowsArgumentException()
    {
        using var mockClient = new MockElasticsearchInferenceClient();
        var options = new ElasticsearchEmbeddingGeneratorOptions
        {
            InferenceEndpointId = ""
        };

        Assert.Throws<ArgumentException>(() =>
            new ElasticsearchEmbeddingGenerator<Embedding<float>>(
                mockClient,
                options));
    }

    [Fact]
    public void Constructor_WithUnsupportedEmbeddingType_ThrowsNotSupportedException()
    {
        using var mockClient = new MockElasticsearchInferenceClient();
        var options = new ElasticsearchEmbeddingGeneratorOptions
        {
            InferenceEndpointId = "test-endpoint"
        };

        Assert.Throws<NotSupportedException>(() =>
            new ElasticsearchEmbeddingGenerator<Embedding<int>>(
                mockClient,
                options));
    }

    [Fact]
    public void Constructor_WithValidParameters_SetsMetadata()
    {
        var mockClient = new MockElasticsearchInferenceClient();
        var options = new ElasticsearchEmbeddingGeneratorOptions
        {
            InferenceEndpointId = "test-endpoint",
            //ModelId = "test-model"
        };

        using var generator = new ElasticsearchEmbeddingGenerator<Embedding<float>>(
            mockClient,
            options);

        Assert.NotNull(generator.Metadata);
        Assert.Equal("elasticsearch", generator.Metadata.ProviderName);
        Assert.Equal("test-model", generator.Metadata.DefaultModelId);
    }

    [Fact]
    public void Constructor_WithoutModelId_UsesInferenceEndpointIdAsModelId()
    {
        var mockClient = new MockElasticsearchInferenceClient();
        var options = new ElasticsearchEmbeddingGeneratorOptions
        {
            InferenceEndpointId = "test-endpoint"
        };

        using var generator = new ElasticsearchEmbeddingGenerator<Embedding<float>>(
            mockClient,
            options);

        Assert.Equal("test-endpoint", generator.Metadata.DefaultModelId);
    }

    [Fact]
    public async Task GenerateAsync_WithEmptyInput_ReturnsEmptyEmbeddings()
    {
        var mockClient = new MockElasticsearchInferenceClient();
        var options = new ElasticsearchEmbeddingGeneratorOptions
        {
            InferenceEndpointId = "test-endpoint"
        };

        using var generator = new ElasticsearchEmbeddingGenerator<Embedding<float>>(
            mockClient,
            options);

        var result = await generator.GenerateAsync(new List<string>());

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GenerateAsync_WithNullInput_ThrowsArgumentNullException()
    {
        var mockClient = new MockElasticsearchInferenceClient();
        var options = new ElasticsearchEmbeddingGeneratorOptions
        {
            InferenceEndpointId = "test-endpoint"
        };

        using var generator = new ElasticsearchEmbeddingGenerator<Embedding<float>>(
            mockClient,
            options);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            generator.GenerateAsync(null!));
    }

    [Fact]
    public void GetService_WithElasticsearchClientType_ReturnsNull_WhenMockClient()
    {
        var mockClient = new MockElasticsearchInferenceClient();
        var options = new ElasticsearchEmbeddingGeneratorOptions
        {
            InferenceEndpointId = "test-endpoint"
        };

        using var generator = new ElasticsearchEmbeddingGenerator<Embedding<float>>(
            mockClient,
            options);

        // Mock client has null underlying ElasticsearchClient.
        var result = generator.GetService<Clients.Elasticsearch.ElasticsearchClient>();

        Assert.Null(result);
    }

    [Fact]
    public void GetService_WithIEmbeddingGeneratorType_ReturnsSelf()
    {
        var mockClient = new MockElasticsearchInferenceClient();
        var options = new ElasticsearchEmbeddingGeneratorOptions
        {
            InferenceEndpointId = "test-endpoint"
        };

        using var generator = new ElasticsearchEmbeddingGenerator<Embedding<float>>(
            mockClient,
            options);

        var result = generator.GetService<IEmbeddingGenerator<string, Embedding<float>>>();

        Assert.Same(generator, result);
    }

    [Fact]
    public void GetService_WithUnsupportedType_ReturnsNull()
    {
        var mockClient = new MockElasticsearchInferenceClient();
        var options = new ElasticsearchEmbeddingGeneratorOptions
        {
            InferenceEndpointId = "test-endpoint"
        };

        using var generator = new ElasticsearchEmbeddingGenerator<Embedding<float>>(
            mockClient,
            options);

        var result = generator.GetService<string>();

        Assert.Null(result);
    }

    /// <summary>
    /// Mock implementation of ElasticsearchInferenceClient for testing.
    /// </summary>
    private sealed class MockElasticsearchInferenceClient :
        ElasticsearchInferenceClient
    {
        private readonly Func<string, IReadOnlyList<string>, EmbeddingGenerationOptions?, CancellationToken, Task<TextEmbeddingResponse>>? _textEmbeddingHandler;

        public MockElasticsearchInferenceClient(
            Func<string, IReadOnlyList<string>, EmbeddingGenerationOptions?, CancellationToken, Task<TextEmbeddingResponse>>? textEmbeddingHandler = null)
        {
            _textEmbeddingHandler = textEmbeddingHandler;
        }

        public override Task<TextEmbeddingResponse> TextEmbeddingAsync(
            string inferenceId,
            IReadOnlyList<string> inputs,
            EmbeddingGenerationOptions? options,
            CancellationToken cancellationToken = default)
        {
            if (_textEmbeddingHandler != null)
            {
                return _textEmbeddingHandler(inferenceId, inputs, options, cancellationToken);
            }

            throw new NotImplementedException("TextEmbeddingAsync handler not configured");
        }
    }
}
