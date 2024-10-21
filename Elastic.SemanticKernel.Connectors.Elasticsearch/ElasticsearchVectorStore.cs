using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
///     Class for accessing the list of collections in a Elasticsearch vector store.
/// </summary>
/// <remarks>
///     This class can be used with collections of any schema type, but requires you to provide schema information when
///     getting a collection.
/// </remarks>
public sealed class ElasticsearchVectorStore :
    IVectorStore
{
    /// <summary>The name of this database for telemetry purposes.</summary>
    private const string DatabaseName = "Elasticsearch";

    /// <summary>Elasticsearch client that can be used to manage the collections and points in an Elasticsearch store.</summary>
    private readonly MockableElasticsearchClient _elasticsearchClient;

    /// <summary>Optional configuration options for this class.</summary>
    private readonly ElasticsearchVectorStoreOptions _options;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ElasticsearchVectorStore" /> class.
    /// </summary>
    /// <param name="elasticsearchClient">
    ///     Elasticsearch client that can be used to manage the collections and points in an
    ///     Elasticsearch store.
    /// </param>
    /// <param name="options">Optional configuration options for this class.</param>
    public ElasticsearchVectorStore(ElasticsearchClient elasticsearchClient,
        ElasticsearchVectorStoreOptions? options = default)
        : this(new MockableElasticsearchClient(elasticsearchClient), options)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ElasticsearchVectorStore" /> class.
    /// </summary>
    /// <param name="elasticsearchClient">
    ///     Elasticsearch client that can be used to manage the collections and points in an
    ///     Elasticsearch store.
    /// </param>
    /// <param name="options">Optional configuration options for this class.</param>
    internal ElasticsearchVectorStore(MockableElasticsearchClient elasticsearchClient,
        ElasticsearchVectorStoreOptions? options = default)
    {
        Verify.NotNull(elasticsearchClient);

        _elasticsearchClient = elasticsearchClient;
        _options = options ?? new ElasticsearchVectorStoreOptions();
    }

    /// <inheritdoc />
    public IVectorStoreRecordCollection<TKey, TRecord> GetCollection<TKey, TRecord>(string name,
        VectorStoreRecordDefinition? vectorStoreRecordDefinition = null)
        where TKey : notnull
    {
        if (typeof(TKey) != typeof(string))
        {
            throw new NotSupportedException("Only string keys are supported.");
        }

        if (_options.VectorStoreCollectionFactory is not null)
        {
            return _options.VectorStoreCollectionFactory.CreateVectorStoreRecordCollection<TKey, TRecord>(
                _elasticsearchClient.ElasticsearchClient, name, vectorStoreRecordDefinition);
        }

        var recordCollection = new ElasticsearchVectorStoreRecordCollection<TRecord>(_elasticsearchClient, name,
            new ElasticsearchVectorStoreRecordCollectionOptions<TRecord>
            {
                VectorStoreRecordDefinition = vectorStoreRecordDefinition
            });

        var castRecordCollection = recordCollection as IVectorStoreRecordCollection<TKey, TRecord>;
        return castRecordCollection!;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ListCollectionNamesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> collections;

        try
        {
            collections = await _elasticsearchClient.ListIndicesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (TransportException ex)
        {
            throw new VectorStoreOperationException("Call to vector store failed.", ex)
            {
                VectorStoreType = DatabaseName,
                OperationName = "ListCollections"
            };
        }

        foreach (var collection in collections)
        {
            yield return collection;
        }
    }
}
