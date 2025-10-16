// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.VectorData.ProviderServices;
using Microsoft.SemanticKernel;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
/// Service for storing and retrieving vector records, that uses Elasticsearch as the underlying storage.
/// </summary>
/// <typeparam name="TRecord">The data model to use for adding, updating and retrieving data from storage.</typeparam>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix

public class ElasticsearchCollection<TKey, TRecord> :
    VectorStoreCollection<TKey, TRecord>,
    IKeywordHybridSearchable<TRecord>
    where TKey : notnull
    where TRecord : class
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    /// <summary>The default options for vector search.</summary>
    private static readonly VectorSearchOptions<TRecord> DefaultVectorSearchOptions = new();

    /// <summary>The default options for hybrid vector search.</summary>
    private static readonly HybridSearchOptions<TRecord> DefaultKeywordVectorizedHybridSearchOptions = new();

    /// <summary>Metadata about vector store record collection.</summary>
    private readonly VectorStoreCollectionMetadata _collectionMetadata;

    /// <summary>Elasticsearch client that can be used to manage the collections and documents in an Elasticsearch store.</summary>
    private readonly MockableElasticsearchClient _elasticsearchClient;

    /// <summary>The model for this collection.</summary>
    private readonly CollectionModel _model;

    /// <summary>A mapper to use for converting between Elasticsearch record and consumer models.</summary>
    private readonly IElasticsearchMapper<TRecord, (string? id, JsonObject document)> _mapper;

    private readonly Fields _vectorFields;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticsearchCollection{TKey,TRecord}" /> class.
    /// </summary>
    /// <param name="elasticsearchClient">Elasticsearch client that can be used to manage the collections and documents in an Elasticsearch store.</param>
    /// <param name="name">The name of the collection that this <see cref="ElasticsearchCollection{TKey,TRecord}" /> will access.</param>
    /// <param name="ownsClient">A value indicating whether <paramref name="elasticsearchClient"/> is disposed when the collection is disposed.</param>
    /// <param name="options">Optional configuration options for this class.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="elasticsearchClient" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown for any misconfigured options.</exception>
    [RequiresDynamicCode("This constructor is incompatible with NativeAOT. For dynamic mapping via Dictionary<string, object?>, instantiate ElasticsearchDynamicCollection instead.")]
    [RequiresUnreferencedCode("This constructor is incompatible with trimming. For dynamic mapping via Dictionary<string, object?>, instantiate ElasticsearchDynamicCollection instead")]
    public ElasticsearchCollection(ElasticsearchClient elasticsearchClient, string name, bool ownsClient, ElasticsearchCollectionOptions? options = null)
#pragma warning disable CA2000
        : this(() => new MockableElasticsearchClient(elasticsearchClient, ownsClient), name, options)
#pragma warning restore CA2000
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticsearchCollection{TKey, TRecord}"/> class.
    /// </summary>
    /// <param name="clientFactory">Elasticsearch client factory.</param>
    /// <param name="name">The name of the collection that this <see cref="ElasticsearchCollection{TKey, TRecord}"/> will access.</param>
    /// <param name="options">Optional configuration options for this class.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="clientFactory"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown for any misconfigured options.</exception>
    [RequiresDynamicCode("This constructor is incompatible with NativeAOT. For dynamic mapping via Dictionary<string, object?>, instantiate ElasticsearchDynamicCollection instead.")]
    [RequiresUnreferencedCode("This constructor is incompatible with trimming. For dynamic mapping via Dictionary<string, object?>, instantiate ElasticsearchDynamicCollection instead")]
    internal ElasticsearchCollection(Func<MockableElasticsearchClient> clientFactory, string name, ElasticsearchCollectionOptions? options = null)
        : this(
            clientFactory,
            name,
            static (client, options) => typeof(TRecord) == typeof(Dictionary<string, object?>)
                ? throw new NotSupportedException(VectorDataStrings.NonDynamicCollectionWithDictionaryNotSupported(typeof(ElasticsearchDynamicCollection)))
                : new ElasticsearchModelBuilder(client.ElasticsearchClient.ElasticsearchClientSettings).Build(typeof(TRecord), options.Definition, options.EmbeddingGenerator),
            options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticsearchCollection{TKey,TRecord}" /> class.
    /// </summary>
    /// <param name="clientFactory">Elasticsearch client factory.</param>
    /// <param name="name">The name of the collection that this <see cref="ElasticsearchCollection{TKey,TRecord}" /> will access.</param>
    /// <param name="options">Optional configuration options for this class.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="clientFactory" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown for any misconfigured options.</exception>
    internal ElasticsearchCollection(
        Func<MockableElasticsearchClient> clientFactory,
        string name,
        Func<MockableElasticsearchClient, ElasticsearchCollectionOptions, CollectionModel> modelFactory,
        ElasticsearchCollectionOptions? options = null)
    {
        // Verify.
        Verify.NotNull(clientFactory);
        Verify.NotNullOrWhiteSpace(name);

        if (typeof(TKey) != typeof(string) && typeof(TKey) != typeof(ulong) && typeof(TKey) != typeof(Guid) && typeof(TKey) != typeof(object))
        {
            throw new NotSupportedException("Only 'string', 'ulong' and 'Guid' keys are supported.");
        }

        options ??= ElasticsearchCollectionOptions.Default;

        // Assign.
        Name = name;
        _elasticsearchClient = clientFactory();

        try
        {
            _model = modelFactory(_elasticsearchClient, options);
        }
        catch
        {
            _elasticsearchClient.Dispose();
            throw;
        }

        var isDynamic = (typeof(TRecord) == typeof(Dictionary<string, object?>));

        _mapper = isDynamic
            ? (new ElasticsearchDynamicMapper(_model, _elasticsearchClient.ElasticsearchClient.ElasticsearchClientSettings) as IElasticsearchMapper<TRecord, (string? id, JsonObject document)>)!
            : new ElasticsearchMapper<TKey, TRecord>(_model, _elasticsearchClient.ElasticsearchClient.ElasticsearchClientSettings);

        _collectionMetadata = new()
        {
            VectorStoreSystemName = ElasticsearchConstants.VectorStoreSystemName,
            CollectionName = name
        };

        _vectorFields = Fields.FromFields(_model.VectorProperties.Select(x => new Field(x.StorageName)).ToArray());
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        this._elasticsearchClient.Dispose();
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override string Name { get; }

    /// <inheritdoc />
    public override Task<bool> CollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        return RunOperationAsync(
            "indices.exists",
            () => _elasticsearchClient.IndexExistsAsync(Name, cancellationToken));
    }

    /// <inheritdoc />
    public override async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        // Don't even try to create if the collection already exists.
        if (await CollectionExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var propertyMappings = ElasticsearchCollectionCreateMapping.BuildPropertyMappings(_model);

        await RunOperationAsync(
            "indices.create",
            () => _elasticsearchClient.CreateIndexAsync(Name, propertyMappings, cancellationToken))
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task EnsureCollectionDeletedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await RunOperationAsync(
                "indices.delete",
                () => _elasticsearchClient.DeleteIndexAsync(Name, cancellationToken))
                .ConfigureAwait(false);
        }
        catch (VectorStoreException e) when (e.InnerException is TransportException { ApiCallDetails.HttpStatusCode: 404 })
        {
            // Swallow "not found" exception.
        }
    }

    /// <inheritdoc />
    public override async Task<TRecord?> GetAsync(TKey key, RecordRetrievalOptions? options = null, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(key);

        var keyValue = ElasticsearchKeyMapping.KeyToElasticsearchId(key);

        var includeVectors = options?.IncludeVectors ?? false;
        if (includeVectors && _model.VectorProperties.Any(p => p.EmbeddingGenerator is not null))
        {
            throw new NotSupportedException(VectorDataStrings.IncludeVectorsNotSupportedWithEmbeddingGeneration);
        }

        var storageModel = await RunOperationAsync(
                "get",
                () => _elasticsearchClient.GetDocumentAsync(
                    Name,
                    keyValue,
                    includeVectors ? null : _vectorFields,
                    cancellationToken))
            .ConfigureAwait(false);

        if (!storageModel.HasValue)
        {
            return default;
        }

        return _mapper.MapFromStorageToDataModel(storageModel.Value, includeVectors);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<TRecord> GetAsync(IEnumerable<TKey> keys, RecordRetrievalOptions? options = default,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // TODO: Use mget endpoint

        Verify.NotNull(keys);

        foreach (var key in keys)
        {
            var record = await GetAsync(key, options, cancellationToken).ConfigureAwait(false);
            if (record is null)
            {
                continue;
            }

            yield return record;
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<TRecord> GetAsync(Expression<Func<TRecord, bool>> filter, int top, FilteredRecordRetrievalOptions<TRecord>? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Verify.NotNull(filter);
        Verify.NotLessThan(top, 1);

        options ??= new();

        var translatedFilter = new ElasticsearchFilterTranslator().Translate(filter, _model);
        var orderByValues = options.OrderBy?.Invoke(new()).Values;

        List<SortOptions>? sort = null;
        if (orderByValues?.Count is > 0)
        {
            sort = orderByValues
                .Select(sortInfo => new SortOptions
                {
                    Field = new FieldSort(_model.GetDataOrKeyProperty(sortInfo.PropertySelector).StorageName)
                    {
                        Order = sortInfo.Ascending ? SortOrder.Asc : SortOrder.Desc
                    }
                })
                .ToList();
        }

        // Build search query.

        var query = translatedFilter ?? new MatchAllQuery();

        // Execute search query.

        var hits = await RunOperationAsync(
                "search",
                () => _elasticsearchClient.SearchAsync(
                    Name,
                    query,
                    sort,
                    options.IncludeVectors ? null : _vectorFields,
                    options.Skip,
                    top,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        // Map results.

        var mappedResults = hits.Select(x => _mapper.MapFromStorageToDataModel((x.Id, x.Source!), options.IncludeVectors));

        foreach (var result in mappedResults)
        {
            yield return result;
        }
    }

    /// <inheritdoc />
    public override async Task DeleteAsync(TKey key, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(key);

        var keyValue = ElasticsearchKeyMapping.KeyToElasticsearchId(key);

        try
        {
            await RunOperationAsync(
                    "delete",
                    () => _elasticsearchClient.DeleteDocumentAsync(Name, keyValue, cancellationToken))
                .ConfigureAwait(false);
        }
        catch (VectorStoreException e) when (e.InnerException is TransportException { ApiCallDetails.HttpStatusCode: 404 })
        {
            // Swallow "not found" exception.
        }
    }

    /// <inheritdoc />
    public override async Task DeleteAsync(IEnumerable<TKey> keys, CancellationToken cancellationToken = default)
    {
        // TODO: Use _bulk endpoint

        Verify.NotNull(keys);

        foreach (var key in keys)
        {
            await DeleteAsync(key, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public override async Task UpsertAsync(TRecord record, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(record);

        // If an embedding generator is defined, invoke it once per property.
        Embedding<float>?[]? generatedEmbeddings = null;

        var vectorPropertyCount = _model.VectorProperties.Count;
        for (var i = 0; i < vectorPropertyCount; i++)
        {
            var vectorProperty = _model.VectorProperties[i];

            if (ElasticsearchModelBuilder.IsVectorPropertyTypeValidCore(vectorProperty.Type, out _))
            {
                continue;
            }

            // We have a vector property whose type isn't natively supported - we need to generate embeddings.
            Debug.Assert(vectorProperty.EmbeddingGenerator is not null);

            // TODO: Ideally we'd group together vector properties using the same generator (and with the same input and output properties),
            //       and generate embeddings for them in a single batch. That's some more complexity though.

            if (vectorProperty.TryGenerateEmbedding<TRecord, Embedding<float>>(record, cancellationToken, out var task))
            {
                generatedEmbeddings ??= new Embedding<float>?[vectorPropertyCount];
                generatedEmbeddings[i] = await task.ConfigureAwait(false);
            }
            else
            {
                throw new InvalidOperationException(
                    $"The embedding generator configured on property '{vectorProperty.ModelName}' cannot produce an embedding of type '{typeof(Embedding<float>).Name}' for the given input type.");
            }
        }

        var storageModel = _mapper.MapFromDataToStorageModel(record, generatedEmbeddings);

        var id = await RunOperationAsync(
                "index",
                () => _elasticsearchClient.IndexDocumentAsync(Name, storageModel.id!, storageModel.document, cancellationToken))
            .ConfigureAwait(false);

        var key = ElasticsearchKeyMapping.ElasticsearchIdToKey<TKey>(id);

        // TODO: Update record with the generated key.
        _ = key;
    }

    /// <inheritdoc />
    public override async Task UpsertAsync(IEnumerable<TRecord> records, CancellationToken cancellationToken = default)
    {
        // TODO: Use _bulk endpoint

        Verify.NotNull(records);

        foreach (var record in records)
        {
            await UpsertAsync(record, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<VectorSearchResult<TRecord>> SearchAsync<TInput>(
        TInput searchValue,
        int top,
        VectorSearchOptions<TRecord>? options = default,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Verify.NotNull(searchValue);

        options ??= DefaultVectorSearchOptions;
        if (options.IncludeVectors && _model.EmbeddingGenerationRequired)
        {
            throw new NotSupportedException(VectorDataStrings.IncludeVectorsNotSupportedWithEmbeddingGeneration);
        }

        var vectorProperty = _model.GetVectorPropertyOrSingle(options);
        var searchVector = await GetSearchVectorAsync(searchValue, vectorProperty, cancellationToken).ConfigureAwait(false);

        // Build filter queries.

#pragma warning disable CS0618 // Type or member is obsolete
        var filter = options switch
        {
            { OldFilter: not null, Filter: not null } => throw new ArgumentException($"Either '{nameof(options.Filter)}' or '{nameof(options.OldFilter)}' can be specified, but not both."),
            { OldFilter: {} legacyFilter } => ElasticsearchCollectionSearchMapping.BuildFromLegacyFilter(legacyFilter, _model),
            { Filter: {} newFilter } => new ElasticsearchFilterTranslator().Translate(newFilter, _model),
            _ => null
        };
#pragma warning restore CS0618 // Type or member is obsolete

        // Build search query.

        var knnQuery = new KnnQuery(vectorProperty.StorageName)
        {
            QueryVector = searchVector,
            Filter = filter is null ? null : [filter],
            K = 10,
            NumCandidates = 20
        };

        // TODO: factors

        // Execute search query.

        var hits = await RunOperationAsync(
                "search",
                () => _elasticsearchClient.SearchAsync(
                    Name,
                    knnQuery,
                    null,
                    options.IncludeVectors ? null : _vectorFields,
                    options.Skip,
                    top,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        // Map to date model.

        var mappedResults = hits.Select(x => new VectorSearchResult<TRecord>(_mapper.MapFromStorageToDataModel((x.Id, x.Source!), options.IncludeVectors), x.Score));

        foreach (var result in mappedResults)
        {
            yield return result;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<VectorSearchResult<TRecord>> HybridSearchAsync<TInput>(
        TInput searchValue,
        ICollection<string> keywords,
        int top,
        HybridSearchOptions<TRecord>? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TInput : notnull
    {
        Verify.NotLessThan(top, 1);
        Verify.NotNull(keywords);

        // Resolve options.

        options ??= DefaultKeywordVectorizedHybridSearchOptions;
        var vectorProperty = _model.GetVectorPropertyOrSingle<TRecord>(new() { VectorProperty = options.VectorProperty });
        var searchVector = await GetSearchVectorAsync(searchValue, vectorProperty, cancellationToken).ConfigureAwait(false);
        var textDataProperty = _model.GetFullTextDataPropertyOrSingle(options.AdditionalProperty);

        // Build filter queries.

#pragma warning disable CS0618 // Type or member is obsolete
        var filter = options switch
        {
            { OldFilter: not null, Filter: not null } => throw new ArgumentException($"Either '{nameof(options.Filter)}' or '{nameof(options.OldFilter)}' can be specified, but not both."),
            { OldFilter: {} legacyFilter } => ElasticsearchCollectionSearchMapping.BuildFromLegacyFilter(legacyFilter, _model),
            { Filter: {} newFilter } => new ElasticsearchFilterTranslator().Translate(newFilter, _model),
            _ => null
        };
#pragma warning restore CS0618 // Type or member is obsolete

        // Build search query.

        var knn = new KnnRetriever(field: vectorProperty.StorageName, k: 10, numCandidates: 20)
        {
            QueryVector = searchVector,
            Filter = filter is null ? null : [filter]
        };

        Query query = new MatchQuery(textDataProperty.StorageName, string.Join(" ", keywords));

        if (filter is not null)
        {
            query = new BoolQuery
            {
                Filter = [filter],
                Must = [query]
            };
        }

        // Execute search query.

        var hits = await RunOperationAsync(
                "search",
                () => _elasticsearchClient.HybridSearchAsync(
                    Name,
                    knn,
                    query,
                    options.IncludeVectors ? null : _vectorFields,
                    options.Skip,
                    top,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        // Map to date model.

        var mappedResults = hits.Select(x => new VectorSearchResult<TRecord>(_mapper.MapFromStorageToDataModel((x.Id, x.Source!), options.IncludeVectors), x.Score));

        foreach (var result in mappedResults)
        {
            yield return result;
        }
    }

    private static async ValueTask<ICollection<float>> GetSearchVectorAsync<TInput>(TInput searchValue, VectorPropertyModel vectorProperty, CancellationToken cancellationToken)
        where TInput : notnull
    {
        if (searchValue is ICollection<float> collection)
        {
            return collection;
        }

        if (searchValue is IEnumerable<float> enumerable)
        {
            return [.. enumerable];
        }

        var memory = searchValue switch
        {
            ReadOnlyMemory<float> r => r,
            Embedding<float> e => e.Vector,
            _ when vectorProperty.EmbeddingGenerator is IEmbeddingGenerator<TInput, Embedding<float>> generator
                => await generator.GenerateVectorAsync(searchValue, cancellationToken: cancellationToken).ConfigureAwait(false),

            _ => vectorProperty.EmbeddingGenerator is null
                ? throw new NotSupportedException(VectorDataStrings.InvalidSearchInputAndNoEmbeddingGeneratorWasConfigured(searchValue.GetType(), ElasticsearchModelBuilder.SupportedVectorTypes))
                : throw new InvalidOperationException(VectorDataStrings.IncompatibleEmbeddingGeneratorWasConfiguredForInputType(typeof(TInput), vectorProperty.EmbeddingGenerator.GetType()))
        };

        return MemoryMarshal.TryGetArray(memory, out var segment) && segment.Count == segment.Array!.Length
            ? segment.Array
            : memory.ToArray();
    }

    /// <inheritdoc />
    public override object? GetService(Type serviceType, object? serviceKey = null)
    {
        Verify.NotNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(VectorStoreCollectionMetadata) ? _collectionMetadata :
            serviceType == typeof(ElasticsearchClient) ? _elasticsearchClient.ElasticsearchClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <summary>
    /// Run the given operation and wrap any <see cref="TransportException"/> with <see cref="VectorStoreException"/>."/>
    /// </summary>
    /// <param name="operationName">The type of database operation being run.</param>
    /// <param name="operation">The operation to run.</param>
    /// <returns>The result of the operation.</returns>
    private Task RunOperationAsync(string operationName, Func<Task> operation)
    {
        return VectorStoreErrorHandler.RunOperationAsync<TransportException>(
            _collectionMetadata,
            operationName,
            operation);
    }

    /// <summary>
    /// Run the given operation and wrap any <see cref="TransportException"/> with <see cref="VectorStoreException"/>."/>
    /// </summary>
    /// <typeparam name="T">The response type of the operation.</typeparam>
    /// <param name="operationName">The type of database operation being run.</param>
    /// <param name="operation">The operation to run.</param>
    /// <returns>The result of the operation.</returns>
    private Task<T> RunOperationAsync<T>(string operationName, Func<Task<T>> operation)
    {
        return VectorStoreErrorHandler.RunOperationAsync<T, TransportException>(
            _collectionMetadata,
            operationName,
            operation);
    }
}
