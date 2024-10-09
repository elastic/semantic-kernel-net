using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Transport;

using Microsoft.SemanticKernel;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

#pragma warning disable CA1852 // TODO: Remove after using MockableElasticsearchClient in unit tests

/// <summary>
///     Decorator class for <see cref="Elastic.Clients.Elasticsearch.ElasticsearchClient" /> that exposes the required
///     methods as virtual allowing for mocking in unit tests.
/// </summary>
internal class MockableElasticsearchClient
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MockableElasticsearchClient" /> class.
    /// </summary>
    /// <param name="elasticsearchClient">
    ///     Elasticsearch client that can be used to manage the collections and points in an
    ///     Elasticsearch store.
    /// </param>
    public MockableElasticsearchClient(ElasticsearchClient elasticsearchClient)
    {
        Verify.NotNull(elasticsearchClient);

        ElasticsearchClient = elasticsearchClient;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <summary>
    ///     Constructor for mocking purposes only.
    /// </summary>
    internal MockableElasticsearchClient()
    {
    }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <summary>
    ///     Gets the internal <see cref="ElasticsearchClient" /> that this mockable instance wraps.
    /// </summary>
    public ElasticsearchClient ElasticsearchClient { get; }

    /// <summary>
    ///     Gets the names of all existing indices.
    /// </summary>
    /// <param name="cancellationToken">
    ///     The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public virtual async Task<IReadOnlyList<string>> ListIndicesAsync(CancellationToken cancellationToken = default)
    {
        var response = await ElasticsearchClient.Indices
            .StatsAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess())
        {
            throw new TransportException(PipelineFailure.Unexpected, "Failed to execute request.", response);
        }

        return response.Indices?.Keys.ToArray() ?? [];
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
            .ExistsAsync(indexName, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess())
        {
            throw new TransportException(PipelineFailure.Unexpected, "Failed to execute request.", response);
        }

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
            .CreateAsync(new CreateIndexRequest(indexName)
            {
                Mappings = new TypeMapping
                {
                    Properties = properties
                }
            },
                cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess())
        {
            throw new TransportException(PipelineFailure.Unexpected, "Failed to execute request.", response);
        }
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

        var response = await ElasticsearchClient.Indices.DeleteAsync(indexName, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess())
        {
            throw new TransportException(PipelineFailure.Unexpected, "Failed to execute request.", response);
        }
    }

    public virtual async Task<(string id, JsonObject document)?> GetDocumentAsync(
        IndexName indexName,
        Id id,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(indexName);
        Verify.NotNull(id);

        var response = await ElasticsearchClient
            .GetAsync<JsonObject>(indexName, id, x => { }, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess())
        {
            throw new TransportException(PipelineFailure.Unexpected, "Failed to execute request.", response);
        }

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
            .IndexAsync(document, indexName, id, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess())
        {
            throw new TransportException(PipelineFailure.Unexpected, "Failed to execute request.", response);
        }

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
            .DeleteAsync(indexName, id, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess())
        {
            throw new TransportException(PipelineFailure.Unexpected, "Failed to execute request.", response);
        }
    }
}

#pragma warning restore CA1852 // TODO: Remove after using MockableElasticsearchClient in unit tests
