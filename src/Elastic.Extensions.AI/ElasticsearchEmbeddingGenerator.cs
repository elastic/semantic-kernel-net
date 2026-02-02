// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Inference;

using Microsoft.Extensions.AI;

namespace Elastic.Extensions.AI;

/// <summary>
/// An embedding generator that uses Elasticsearch inference endpoints to generate embeddings.
/// </summary>
/// <typeparam name="TEmbedding">The type of embedding to generate. Must be <see cref="Embedding{Single}"/> or <see cref="Embedding{Byte}"/>.</typeparam>
public sealed class ElasticsearchEmbeddingGenerator<TEmbedding> :
    IEmbeddingGenerator<string, TEmbedding>
    where TEmbedding : Embedding
{
    private readonly ElasticsearchInferenceClient _client;
    private readonly ElasticsearchEmbeddingGeneratorOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticsearchEmbeddingGenerator{TEmbedding}"/> class.
    /// </summary>
    /// <param name="client">The Elasticsearch client to use for inference requests.</param>
    /// <param name="options">The options for configuring the embedding generator.</param>
    public ElasticsearchEmbeddingGenerator(ElasticsearchClient client, ElasticsearchEmbeddingGeneratorOptions options)
#pragma warning disable CA2000
        : this(new ElasticsearchInferenceClient(client, ownsClient: true), options)
#pragma warning restore CA2000
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticsearchEmbeddingGenerator{TEmbedding}"/> class.
    /// </summary>
    /// <param name="client">The Elasticsearch client to use for inference requests.</param>
    /// <param name="ownsClient">A value indicating whether the client is disposed when this instance is disposed.</param>
    /// <param name="options">The options for configuring the embedding generator.</param>
    public ElasticsearchEmbeddingGenerator(ElasticsearchClient client, bool ownsClient, ElasticsearchEmbeddingGeneratorOptions options)
#pragma warning disable CA2000
        : this(new ElasticsearchInferenceClient(client, ownsClient), options)
#pragma warning restore CA2000
    {
    }

    /// <summary>
    /// Internal constructor for testing purposes.
    /// </summary>
    /// <param name="client">The mockable inference client.</param>
    /// <param name="options">The options for configuring the embedding generator.</param>
    internal ElasticsearchEmbeddingGenerator(ElasticsearchInferenceClient client, ElasticsearchEmbeddingGeneratorOptions options)
    {
        Verify.NotNull(client);
        Verify.NotNull(options);
        Verify.NotNullOrWhiteSpace(options.InferenceEndpointId);

        ValidateEmbeddingType();

        _client = client;
        _options = options.Clone();
        Metadata = new EmbeddingGeneratorMetadata(
            providerName: "elasticsearch",
            providerUri: _client.ElasticsearchClient?.ElasticsearchClientSettings?.NodePool?.Nodes?.FirstOrDefault()?.Uri,
            defaultModelId: /* _options.ModelId ?? */ _options.InferenceEndpointId,
            defaultModelDimensions: null /* _options.Dimensions */);
    }

    public EmbeddingGeneratorMetadata Metadata { get; }

    /// <inheritdoc/>
    public async Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(values);

        var inputList = values.ToList();
        if (inputList.Count is 0)
        {
            return [];
        }

        var response = await _client
            .TextEmbeddingAsync(
                _options.InferenceEndpointId,
                inputList,
                options,
                cancellationToken
            )
            .ConfigureAwait(false);

        return ConvertToEmbeddings(response);
    }

    public TService? GetService<TService>(object? key = null)
        where TService : class
    {
        return GetService(typeof(TService), key) as TService;
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        Verify.NotNull(serviceType);

        if (serviceKey is not null)
        {
            return null;
        }

        if (serviceType == typeof(ElasticsearchClient))
        {
            return _client.ElasticsearchClient;
        }

        if (serviceType == typeof(IEmbeddingGenerator<string, TEmbedding>))
        {
            return this;
        }

        return null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _client.Dispose();
    }

    private static void ValidateEmbeddingType()
    {
        var embeddingType = typeof(TEmbedding);

        if (embeddingType != typeof(Embedding<float>) && embeddingType != typeof(Embedding<byte>))
        {
            throw new NotSupportedException(
                $"Embedding type '{embeddingType.Name}' is not supported. " +
                $"Supported types are Embedding<float> and Embedding<byte>.");
        }
    }

    private static GeneratedEmbeddings<TEmbedding> ConvertToEmbeddings(TextEmbeddingResponse response)
    {
        var result = new GeneratedEmbeddings<TEmbedding>();

        if (typeof(TEmbedding) == typeof(Embedding<float>))
        {
            if (response.InferenceResult?.TextEmbedding is { } floatEmbeddings)
            {
                foreach (var embedding in floatEmbeddings)
                {
                    var floatArray = embedding.Embedding.ToArray();
                    result.Add((TEmbedding)(object)new Embedding<float>(floatArray));
                }
            }
        }
        else if (typeof(TEmbedding) == typeof(Embedding<byte>))
        {
            var embeddings = response.InferenceResult?.TextEmbeddingBytes ?? response.InferenceResult?.TextEmbeddingBits;

            if (embeddings is not null)
            {
                foreach (var embedding in embeddings)
                {
                    var byteArray = embedding.Embedding.ToArray();
                    result.Add((TEmbedding)(object)new Embedding<byte>(byteArray));
                }
            }
        }

        return result;
    }
}
