// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Inference;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;

using Microsoft.Extensions.AI;

namespace Elastic.Extensions.AI;

/// <summary>
/// Internal decorator class for <see cref="ElasticsearchClient"/> that exposes the required inference methods
/// as virtual allowing for mocking in unit tests.
/// </summary>
internal class ElasticsearchInferenceClient :
    IDisposable
{
    private static readonly RequestConfiguration CustomUserAgentRequestConfiguration = new()
    {
        UserAgent = UserAgent.Create("elasticsearch-net", typeof(IElasticsearchClientSettings), ["integration=MEAI"]),
        ThrowExceptions = true // TODO: Fixme.
    };

    private readonly bool _ownsClient;
    private int _referenceCount = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticsearchInferenceClient"/> class.
    /// </summary>
    /// <param name="elasticsearchClient">Elasticsearch client that can be used to call inference endpoints.</param>
    /// <param name="ownsClient">A value indicating whether <paramref name="elasticsearchClient"/> is disposed when this instance is disposed.</param>
    public ElasticsearchInferenceClient(ElasticsearchClient elasticsearchClient, bool ownsClient = true)
    {
        Verify.NotNull(elasticsearchClient);

        ElasticsearchClient = elasticsearchClient;
        _ownsClient = ownsClient;
    }

    /// <summary>
    /// Constructor for mocking purposes only.
    /// </summary>
    internal ElasticsearchInferenceClient()
    {
        ElasticsearchClient = null!;
    }

    /// <summary>
    /// Gets the internal <see cref="ElasticsearchClient"/> that this instance wraps.
    /// </summary>
    public ElasticsearchClient ElasticsearchClient { get; }

    /// <summary>
    /// Creates a shared reference to this client, incrementing the reference count if this client owns the underlying client.
    /// </summary>
    /// <returns>This instance.</returns>
    internal ElasticsearchInferenceClient Share()
    {
        if (_ownsClient)
        {
            Interlocked.Increment(ref _referenceCount);
        }

        return this;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_ownsClient)
        {
            return;
        }

        if (Interlocked.Decrement(ref _referenceCount) == 0)
        {
            ElasticsearchClient.ElasticsearchClientSettings.Dispose();
        }
    }

    /// <summary>
    /// Generates embeddings for the given input text using the specified inference endpoint.
    /// </summary>
    /// <param name="inferenceId">The inference endpoint ID configured in Elasticsearch.</param>
    /// <param name="inputs">The input texts to generate embeddings for.</param>
    /// <param name="options">The embedding generation options with which to configure the request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The text embedding response containing the embeddings.</returns>
    public virtual async Task<TextEmbeddingResponse> TextEmbeddingAsync(
        string inferenceId,
        IReadOnlyList<string> inputs,
        EmbeddingGenerationOptions? options,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(inferenceId);
        Verify.NotNull(inputs);

        if (inputs.Count == 0)
        {
            throw new ArgumentException("The value cannot be empty.", nameof(inputs));
        }

        var inputCollection = new List<string>(inputs);

        var request = new TextEmbeddingRequest(inferenceId, inputCollection)
        {
            RequestConfiguration = CustomUserAgentRequestConfiguration,
            TaskSettings = options?.AdditionalProperties
        };

        var response = await ElasticsearchClient.Inference
            .TextEmbeddingAsync(request, cancellationToken)
            .ConfigureAwait(false);

        ThrowOnError(response);

        return response;
    }

    private static void ThrowOnError(ElasticsearchResponse response)
    {
        if (!response.IsValidResponse)
        {
            throw new TransportException(
                PipelineFailure.Unexpected,
                $"Failed to execute inference request:\n{response.ApiCallDetails}",
                response);
        }
    }
}
