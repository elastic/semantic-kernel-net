// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
/// Class for accessing the list of collections in an Elasticsearch vector store.
/// </summary>
/// <remarks>
/// This class can be used with collections of any schema type, but requires you to provide schema information when getting a collection.
/// </remarks>
public class ElasticsearchVectorStore :
    IVectorStore
{
    /// <summary>A general purpose definition that can be used to construct a collection when needing to proxy schema agnostic operations.</summary>
    private static readonly VectorStoreRecordDefinition GeneralPurposeDefinition = new()
    {
        Properties =
        [
            new VectorStoreRecordKeyProperty("Key", typeof(string)),
            new VectorStoreRecordVectorProperty("Vector", typeof(ReadOnlyMemory<float>), 1)
        ]
    };

    /// <summary>Metadata about vector store.</summary>
    private readonly VectorStoreMetadata _metadata;

    /// <summary>Elasticsearch client that can be used to manage the collections and points in an Elasticsearch store.</summary>
    private readonly MockableElasticsearchClient _elasticsearchClient;

    /// <summary>Optional configuration options for this class.</summary>
    private readonly ElasticsearchVectorStoreOptions _options;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ElasticsearchVectorStore" /> class.
    /// </summary>
    /// <param name="elasticsearchClient">Elasticsearch client that can be used to manage the collections and documents in an Elasticsearch store.</param>
    /// <param name="options">Optional configuration options for this class.</param>
    public ElasticsearchVectorStore(ElasticsearchClient elasticsearchClient, ElasticsearchVectorStoreOptions? options = default)
        : this(new MockableElasticsearchClient(elasticsearchClient), options)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ElasticsearchVectorStore" /> class.
    /// </summary>
    /// <param name="elasticsearchClient">Elasticsearch client that can be used to manage the collections and documents in an Elasticsearch store.</param>
    /// <param name="options">Optional configuration options for this class.</param>
    internal ElasticsearchVectorStore(MockableElasticsearchClient elasticsearchClient, ElasticsearchVectorStoreOptions? options = default)
    {
        Verify.NotNull(elasticsearchClient);

        _metadata = new()
        {
            VectorStoreSystemName = ElasticsearchConstants.VectorStoreSystemName
        };

        _elasticsearchClient = elasticsearchClient;
        _options = options ?? new ElasticsearchVectorStoreOptions();
    }

    /// <inheritdoc />
    public virtual IVectorStoreRecordCollection<TKey, TRecord> GetCollection<TKey, TRecord>(string name, VectorStoreRecordDefinition? vectorStoreRecordDefinition = null)
        where TKey : notnull
        where TRecord : notnull
    {
#pragma warning disable CS0618 // IElasticsearchVectorStoreRecordCollectionFactory is obsolete
        if (_options.VectorStoreCollectionFactory is not null)
        {
            return _options.VectorStoreCollectionFactory.CreateVectorStoreRecordCollection<TKey, TRecord>(
                _elasticsearchClient.ElasticsearchClient, name, vectorStoreRecordDefinition);
        }
#pragma warning restore CS0618

        var recordCollection = new ElasticsearchVectorStoreRecordCollection<TKey, TRecord>(_elasticsearchClient, name,
            new ElasticsearchVectorStoreRecordCollectionOptions<TRecord>
            {
                VectorStoreRecordDefinition = vectorStoreRecordDefinition,
                EmbeddingGenerator = _options.EmbeddingGenerator
            });

        return recordCollection;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ListCollectionNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
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
                VectorStoreSystemName = ElasticsearchConstants.VectorStoreSystemName,
                VectorStoreName = _metadata.VectorStoreName,
                OperationName = "ListCollections"
            };
        }

        foreach (var collection in collections)
        {
            yield return collection;
        }
    }

    /// <inheritdoc />
    public Task<bool> CollectionExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection<string, Dictionary<string, object>>(name, GeneralPurposeDefinition);
        return collection.CollectionExistsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteCollectionAsync(string name, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection<string, Dictionary<string, object>>(name, GeneralPurposeDefinition);
        return collection.DeleteCollectionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        Verify.NotNull(serviceType);

#pragma warning disable IDE0055

        return
            serviceKey is not null ? null :
            serviceType == typeof(VectorStoreMetadata) ? _metadata :
            serviceType == typeof(ElasticsearchClient) ? _elasticsearchClient.ElasticsearchClient:
            serviceType.IsInstanceOfType(this) ? this :
            null;

#pragma warning restore IDE0055
    }
}
