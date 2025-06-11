// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.VectorData.ProviderServices;
using Microsoft.SemanticKernel;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
/// Class for accessing the list of collections in an Elasticsearch vector store.
/// </summary>
/// <remarks>
/// This class can be used with collections of any schema type, but requires you to provide schema information when getting a collection.
/// </remarks>
public class ElasticsearchVectorStore :
    VectorStore
{
    /// <summary>A general purpose definition that can be used to construct a collection when needing to proxy schema agnostic operations.</summary>
    private static readonly VectorStoreCollectionDefinition GeneralPurposeDefinition = new()
    {
        Properties =
        [
            new VectorStoreKeyProperty("Key", typeof(string)),
            new VectorStoreVectorProperty("Vector", typeof(ReadOnlyMemory<float>), 1)
        ]
    };

    /// <summary>Metadata about vector store.</summary>
    private readonly VectorStoreMetadata _metadata;

    /// <summary>Elasticsearch client that can be used to manage the collections and points in an Elasticsearch store.</summary>
    private readonly MockableElasticsearchClient _elasticsearchClient;

    private readonly IEmbeddingGenerator? _embeddingGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticsearchVectorStore" /> class.
    /// </summary>
    /// <param name="elasticsearchClient">Elasticsearch client that can be used to manage the collections and documents in an Elasticsearch store.</param>
    /// <param name="ownsClient">A value indicating whether <paramref name="elasticsearchClient"/> is disposed after the vector store is disposed.</param>
    /// <param name="options">Optional configuration options for this class.</param>
    public ElasticsearchVectorStore(ElasticsearchClient elasticsearchClient, bool ownsClient, ElasticsearchVectorStoreOptions? options = default)
#pragma warning disable CA2000
        : this(new MockableElasticsearchClient(elasticsearchClient, ownsClient), options)
#pragma warning restore CA2000
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticsearchVectorStore" /> class.
    /// </summary>
    /// <param name="elasticsearchClient">Elasticsearch client that can be used to manage the collections and documents in an Elasticsearch store.</param>
    /// <param name="options">Optional configuration options for this class.</param>
    internal ElasticsearchVectorStore(MockableElasticsearchClient elasticsearchClient, ElasticsearchVectorStoreOptions? options = default)
    {
        Verify.NotNull(elasticsearchClient);

        _elasticsearchClient = elasticsearchClient;

        options ??= new ElasticsearchVectorStoreOptions();
        _embeddingGenerator = options.EmbeddingGenerator;

        _metadata = new()
        {
            VectorStoreSystemName = ElasticsearchConstants.VectorStoreSystemName
        };
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        _elasticsearchClient.Dispose();
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    [RequiresDynamicCode("This overload of GetCollection() is incompatible with NativeAOT. For dynamic mapping via Dictionary<string, object?>, call GetDynamicCollection() instead.")]
    [RequiresUnreferencedCode("This overload of GetCollecttion() is incompatible with trimming. For dynamic mapping via Dictionary<string, object?>, call GetDynamicCollection() instead.")]
#if NET8_0_OR_GREATER
    public override ElasticsearchCollection<TKey, TRecord> GetCollection<TKey, TRecord>(string name, VectorStoreCollectionDefinition? definition = null)
#else
    public override VectorStoreCollection<TKey, TRecord> GetCollection<TKey, TRecord>(string name, VectorStoreCollectionDefinition? definition = null)
#endif
    {
        return typeof(TRecord) == typeof(Dictionary<string, object?>)
            ? throw new ArgumentException(VectorDataStrings.GetCollectionWithDictionaryNotSupported)
            : new ElasticsearchCollection<TKey, TRecord>(_elasticsearchClient.Share, name, new()
            {
                Definition = definition,
                EmbeddingGenerator = _embeddingGenerator
            });
    }

    /// <inheritdoc />
#if NET8_0_OR_GREATER
    public override ElasticsearchDynamicCollection GetDynamicCollection(string name, VectorStoreCollectionDefinition definition)
#else

    public override VectorStoreCollection<object, Dictionary<string, object?>> GetDynamicCollection(string name, VectorStoreCollectionDefinition definition)
#endif
    {
        return new ElasticsearchDynamicCollection(_elasticsearchClient.Share, name, new()
        {
            Definition = definition,
            EmbeddingGenerator = _embeddingGenerator
        });
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<string> ListCollectionNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var collections = await VectorStoreErrorHandler.RunOperationAsync<IReadOnlyList<string>, TransportException>(
            _metadata,
            "indices.list",
            () => _elasticsearchClient.ListIndicesAsync(cancellationToken)).ConfigureAwait(false);

        foreach (var collection in collections)
        {
            yield return collection;
        }
    }

    /// <inheritdoc />
    public override Task<bool> CollectionExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        var collection = GetDynamicCollection(name, GeneralPurposeDefinition);
        return collection.CollectionExistsAsync(cancellationToken);
    }

    public override Task EnsureCollectionDeletedAsync(string name, CancellationToken cancellationToken = new CancellationToken())
    {
        var collection = GetDynamicCollection(name, GeneralPurposeDefinition);
        return collection.EnsureCollectionDeletedAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override object? GetService(Type serviceType, object? serviceKey = null)
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
