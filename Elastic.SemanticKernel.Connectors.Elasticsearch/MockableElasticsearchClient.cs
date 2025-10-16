// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;

using Microsoft.SemanticKernel;

using ExistsRequest = Elastic.Clients.Elasticsearch.IndexManagement.ExistsRequest;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

#pragma warning disable CA1852 // TODO: Remove after using MockableElasticsearchClient in unit tests

/// <summary>
///     Decorator class for <see cref="ElasticsearchClient" /> that exposes the required methods as virtual allowing
///     for mocking in unit tests.
/// </summary>
internal class MockableElasticsearchClient :
    IDisposable
{
    private static readonly RequestConfiguration CustomUserAgentRequestConfiguration = new()
    {
        UserAgent = UserAgent.Create("elasticsearch-net", typeof(IElasticsearchClientSettings), ["integration=MSSK"]),
        ThrowExceptions = true // TODO: Fixme.
    };

#pragma warning disable IDE0032

    /// <summary>Elasticsearch client that can be used to manage documents in an Elasticsearch store.</summary>
    private readonly ElasticsearchClient _elasticsearchClient;

#pragma warning restore IDE0032

    private readonly bool _ownsClient;
    private int _referenceCount = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockableElasticsearchClient"/> class.
    /// </summary>
    /// <param name="elasticsearchClient">Elasticsearch client that can be used to manage documents in an Elasticsearch store.</param>
    /// <param name="ownsClient">A value indicating whether <paramref name="elasticsearchClient"/> is disposed when the vector store is disposed.</param>
    public MockableElasticsearchClient(ElasticsearchClient elasticsearchClient, bool ownsClient = true)
    {
        Verify.NotNull(elasticsearchClient);

        _elasticsearchClient = elasticsearchClient;
        _ownsClient = ownsClient;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <summary>
    /// Constructor for mocking purposes only.
    /// </summary>
    internal MockableElasticsearchClient()
    {
    }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <summary>
    /// Gets the internal <see cref="ElasticsearchClient"/> that this mockable instance wraps.
    /// </summary>
    public ElasticsearchClient ElasticsearchClient => _elasticsearchClient;

    public void Dispose()
    {
        if (!_ownsClient)
        {
            return;
        }

        if (Interlocked.Decrement(ref _referenceCount) == 0)
        {
            _elasticsearchClient.ElasticsearchClientSettings.Dispose();
        }
    }

    /// <summary>
    ///     Gets the names of all existing indices.
    /// </summary>
    /// <param name="cancellationToken">
    ///     The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public virtual async Task<IReadOnlyList<string>> ListIndicesAsync(CancellationToken cancellationToken = default)
    {
        var response = await ElasticsearchClient.Indices
            .StatsAsync(
                new IndicesStatsRequest
                {
                    RequestConfiguration = CustomUserAgentRequestConfiguration
                },
                cancellationToken)
            .ConfigureAwait(false);

        ThrowOnError(response);

        return response.Indices?.Keys.Where(x => x[0] != '.').ToArray() ?? [];
    }

    /// <summary>
    ///     Check if an index exists.
    /// </summary>
    /// <param name="indexName">The name of the index.</param>
    /// <param name="cancellationToken">
    ///     The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public virtual async Task<bool> IndexExistsAsync(
        IndexName indexName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(indexName);

        var response = await ElasticsearchClient.Indices
            .ExistsAsync(
                new ExistsRequest(indexName)
                {
                    RequestConfiguration = CustomUserAgentRequestConfiguration
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (response.ApiCallDetails.HttpStatusCode == 404)
        {
            // TODO: IsValidResponse should return `true` for ExistsResponse. Fix this upstream in the client library.
            return false;
        }

        ThrowOnError(response);

        return response.Exists;
    }

    /// <summary>
    ///     Creates an index and configures the required mappings.
    /// </summary>
    /// <param name="indexName">The name of the index.</param>
    /// <param name="properties">The property mappings.</param>
    /// <param name="cancellationToken">
    ///     The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public virtual async Task CreateIndexAsync(
        IndexName indexName,
        Properties properties,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(indexName);

        var response = await ElasticsearchClient.Indices
            .CreateAsync(
                new CreateIndexRequest(indexName)
                {
                    Mappings = new TypeMapping
                    {
                        Properties = properties
                    },
                    RequestConfiguration = CustomUserAgentRequestConfiguration
                },
                cancellationToken)
            .ConfigureAwait(false);

        ThrowOnError(response);
    }

    /// <summary>
    ///     Drop an index and all its associated data.
    /// </summary>
    /// <param name="indexName">The name of the index.</param>
    /// <param name="cancellationToken">
    ///     The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public virtual async Task DeleteIndexAsync(
        IndexName indexName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(indexName);

        var response = await ElasticsearchClient.Indices.DeleteAsync(
                new DeleteIndexRequest(indexName)
                {
                    RequestConfiguration = CustomUserAgentRequestConfiguration
                },
                cancellationToken)
            .ConfigureAwait(false);

        ThrowOnError(response);
    }

    public virtual async Task<(string id, JsonObject document)?> GetDocumentAsync(
        IndexName indexName,
        Id id,
        Fields? sourceExcludes,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(indexName);
        Verify.NotNull(id);

        var response = await ElasticsearchClient
            .GetAsync<JsonObject>(
                new GetRequest(indexName, id)
                {
                    SourceExcludes = sourceExcludes,
                    RequestConfiguration = CustomUserAgentRequestConfiguration
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (response.ApiCallDetails.HttpStatusCode == 404)
        {
            // TODO: IsValidResponse should return `true` for GetResponse<T>. Fix this upstream in the client library.
            return null;
        }

        ThrowOnError(response);

        if (!response.Found)
        {
            return null;
        }

        return (response.Id, response.Source!);
    }

    /// <summary>
    ///     TODO: TBC
    /// </summary>
    /// <typeparam name="TDocument"></typeparam>
    /// <param name="indexName"></param>
    /// <param name="id"></param>
    /// <param name="document"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="TransportException"></exception>
    public virtual async Task<string> IndexDocumentAsync<TDocument>(
        IndexName indexName,
        Id id,
        TDocument document,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(indexName);
        Verify.NotNull(document);

        var response = await ElasticsearchClient
            .IndexAsync(
                new IndexRequest<TDocument>(document, indexName, id)
                {
                    RequestConfiguration = CustomUserAgentRequestConfiguration
                },
                cancellationToken)
            .ConfigureAwait(false);

        ThrowOnError(response);

        return response.Id;
    }

    /// <summary>
    /// TODO: TBC
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="TransportException"></exception>
    public virtual async Task DeleteDocumentAsync(
        IndexName indexName,
        Id id,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(indexName);
        Verify.NotNull(id);

        var response = await ElasticsearchClient
            .DeleteAsync(
                new DeleteRequest(indexName, id)
                {
                    RequestConfiguration = CustomUserAgentRequestConfiguration
                },
                cancellationToken)
            .ConfigureAwait(false);

        ThrowOnError(response);
    }

    /// <summary>
    /// TODO: TBC
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="query"></param>
    /// <param name="sort"></param>
    /// <param name="sourceExcludes"></param>
    /// <param name="from"></param>
    /// <param name="size"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="TransportException"></exception>
    public virtual async Task<IReadOnlyCollection<Hit<JsonObject>>> SearchAsync(
        IndexName indexName,
        Query query,
        ICollection<SortOptions>? sort,
        Fields? sourceExcludes,
        int? from,
        int? size,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(indexName);
        Verify.NotNull(query);

        var response = await ElasticsearchClient
            .SearchAsync<JsonObject>(
                new SearchRequest(indexName)
                {
                    Query = query,
                    Sort = sort,
                    SourceExcludes = sourceExcludes,
                    From = from,
                    Size = size,
                    RequestConfiguration = CustomUserAgentRequestConfiguration
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        ThrowOnError(response);

        return response.Hits;
    }

    /// <summary>
    /// TODO: TBC
    /// </summary>
    /// <param name="indexName"></param>
    /// <param name="knn"></param>
    /// <param name="query"></param>
    /// <param name="sourceExcludes"></param>
    /// <param name="from"></param>
    /// <param name="size"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="TransportException"></exception>
    public virtual async Task<IReadOnlyCollection<Hit<JsonObject>>> HybridSearchAsync(
        IndexName indexName,
        KnnRetriever knn,
        Query query,
        Fields? sourceExcludes,
        int? from,
        int? size,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(indexName);
        Verify.NotNull(knn);
        Verify.NotNull(query);

        var response = await ElasticsearchClient
            .SearchAsync<JsonObject>(
                new SearchRequest(indexName)
                {
                    Retriever = new RRFRetriever(
                    [
                        knn,
                        new StandardRetriever { Query = query }
                    ]),
                    SourceExcludes = sourceExcludes,
                    From = from,
                    Size = size,
                    RequestConfiguration = CustomUserAgentRequestConfiguration
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        ThrowOnError(response);

        return response.Hits;
    }

    internal MockableElasticsearchClient Share()
    {
        if (_ownsClient)
        {
            Interlocked.Increment(ref _referenceCount);
        }

        return this;
    }

    private static void ThrowOnError(ElasticsearchResponse response)
    {
        if (!response.IsValidResponse)
        {
            throw new TransportException(PipelineFailure.Unexpected, $"Failed to execute request:\n{response.ApiCallDetails}", response);
        }
    }
}

#pragma warning restore CA1852 // TODO: Remove after using MockableElasticsearchClient in unit tests
