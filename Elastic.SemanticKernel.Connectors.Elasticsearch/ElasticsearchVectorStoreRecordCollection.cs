// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.VectorData.ConnectorSupport;
using Microsoft.Extensions.VectorData.Properties;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
/// Service for storing and retrieving vector records, that uses Elasticsearch as the underlying storage.
/// </summary>
/// <typeparam name="TRecord">The data model to use for adding, updating and retrieving data from storage.</typeparam>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix

public sealed class ElasticsearchVectorStoreRecordCollection<TKey, TRecord> :
    IVectorStoreRecordCollection<TKey, TRecord>,
    IKeywordHybridSearch<TRecord>
    where TKey : notnull
    where TRecord : notnull
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    /// <summary>The default options for vector search.</summary>
    private static readonly VectorSearchOptions<TRecord> DefaultVectorSearchOptions = new();

    /// <summary>The default options for hybrid vector search.</summary>
    private static readonly HybridSearchOptions<TRecord> DefaultKeywordVectorizedHybridSearchOptions = new();

    /// <summary>Metadata about vector store record collection.</summary>
    private readonly VectorStoreRecordCollectionMetadata _collectionMetadata;

#pragma warning disable IDE0032

    /// <summary>The name of the collection that this <see cref="ElasticsearchVectorStoreRecordCollection{TKey, TRecord}"/> will access.</summary>
    private readonly string _collectionName;

#pragma warning restore IDE0032

    /// <summary>Elasticsearch client that can be used to manage the collections and documents in an Elasticsearch store.</summary>
    private readonly MockableElasticsearchClient _elasticsearchClient;

    /// <summary>Optional configuration options for this class.</summary>
    private readonly ElasticsearchVectorStoreRecordCollectionOptions<TRecord> _options;

    /// <summary>The model for this collection.</summary>
    private readonly VectorStoreRecordModel _model;

    /// <summary>A mapper to use for converting between Elasticsearch record and consumer models.</summary>
    private readonly IElasticsearchVectorStoreRecordMapper<TRecord, (string? id, JsonObject document)> _mapper;

    private readonly Fields _vectorFields;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticsearchVectorStoreRecordCollection{TKey, TRecord}" /> class.
    /// </summary>
    /// <param name="elasticsearchClient">Elasticsearch client that can be used to manage the collections and documents in an Elasticsearch store.</param>
    /// <param name="name">The name of the collection that this <see cref="ElasticsearchVectorStoreRecordCollection{TKey, TRecord}" /> will access.</param>
    /// <param name="options">Optional configuration options for this class.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="elasticsearchClient" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown for any misconfigured options.</exception>
    public ElasticsearchVectorStoreRecordCollection(ElasticsearchClient elasticsearchClient, string name, ElasticsearchVectorStoreRecordCollectionOptions<TRecord>? options = null)
        : this(new MockableElasticsearchClient(elasticsearchClient), name, options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticsearchVectorStoreRecordCollection{TKey, TRecord}" /> class.
    /// </summary>
    /// <param name="elasticsearchClient">Elasticsearch client that can be used to manage the collections and documents in an Elasticsearch store.</param>
    /// <param name="name">The name of the collection that this <see cref="ElasticsearchVectorStoreRecordCollection{TKey, TRecord}" /> will access.</param>
    /// <param name="options">Optional configuration options for this class.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="elasticsearchClient" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown for any misconfigured options.</exception>
    internal ElasticsearchVectorStoreRecordCollection(MockableElasticsearchClient elasticsearchClient, string name, ElasticsearchVectorStoreRecordCollectionOptions<TRecord>? options = null)
    {
        // Verify.
        Verify.NotNull(elasticsearchClient);
        Verify.NotNullOrWhiteSpace(name);

        if (typeof(TKey) != typeof(string) && typeof(TKey) != typeof(ulong) && typeof(TKey) != typeof(Guid) && typeof(TKey) != typeof(object))
        {
            throw new NotSupportedException("Only 'string', 'ulong' and 'Guid' keys are supported (and 'object' for dynamic mapping).");
        }

        // Assign.
        _collectionMetadata = new()
        {
            VectorStoreSystemName = ElasticsearchConstants.VectorStoreSystemName,
            CollectionName = name
        };

        _collectionName = name;
        _elasticsearchClient = elasticsearchClient;
        _options = options ?? new ElasticsearchVectorStoreRecordCollectionOptions<TRecord>();

        _model = new ElasticsearchVectorStoreRecordModelBuilder(
                ElasticsearchVectorStoreRecordFieldMapping.GetModelBuildOptions(),
                elasticsearchClient.ElasticsearchClient.ElasticsearchClientSettings
            )
            .Build(typeof(TRecord), _options.VectorStoreRecordDefinition, options?.EmbeddingGenerator);

        if (typeof(TRecord) == typeof(Dictionary<string, object?>))
        {
            // TODO: Fixme
            _mapper = (new ElasticsearchGenericDataModelMapper(_model, _elasticsearchClient.ElasticsearchClient.ElasticsearchClientSettings)
                as IElasticsearchVectorStoreRecordMapper<TRecord, (string? id, JsonObject document)>)!;
        }
        else
        {
            _mapper = new ElasticsearchDataModelMapper<TKey, TRecord>(_model, _elasticsearchClient.ElasticsearchClient.ElasticsearchClientSettings);
        }

        _vectorFields = Fields.FromFields(_model.VectorProperties.Select(x => new Field(x.StorageName)).ToArray())!;
    }

    #region IVectorStoreRecordCollection

    /// <inheritdoc />
    public string Name => _collectionName;

    /// <inheritdoc />
    public Task<bool> CollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        return RunOperationAsync(
            "indices.exists",
            () => _elasticsearchClient.IndexExistsAsync(_collectionName, cancellationToken));
    }

    /// <inheritdoc />
    public Task CreateCollectionAsync(CancellationToken cancellationToken = default)
    {
        var propertyMappings = ElasticsearchVectorStoreCollectionCreateMapping.BuildPropertyMappings(_model);

        return RunOperationAsync(
            "indices.create",
            () => _elasticsearchClient.CreateIndexAsync(_collectionName, propertyMappings, cancellationToken));
    }

    /// <inheritdoc />
    public async Task CreateCollectionIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        if (!await CollectionExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            await CreateCollectionAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public Task DeleteCollectionAsync(CancellationToken cancellationToken = default)
    {
        return RunOperationAsync(
            "indices.delete",
            () => _elasticsearchClient.DeleteIndexAsync(_collectionName, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<TRecord?> GetAsync(TKey key, GetRecordOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var keyValue = ElasticsearchVectorStoreRecordFieldMapping.KeyToElasticsearchId(key);

        var includeVectors = options?.IncludeVectors ?? false;
        if (includeVectors && _model.VectorProperties.Any(p => p.EmbeddingGenerator is not null))
        {
            throw new NotSupportedException(VectorDataStrings.IncludeVectorsNotSupportedWithEmbeddingGeneration);
        }

        var storageModel = await RunOperationAsync(
                "get",
                () => _elasticsearchClient.GetDocumentAsync(
                    _collectionName,
                    keyValue,
                    includeVectors ? null : _vectorFields,
                    cancellationToken))
            .ConfigureAwait(false);

        if (!storageModel.HasValue)
        {
            return default;
        }

        var record = VectorStoreErrorHandler.RunModelConversion(
            ElasticsearchConstants.VectorStoreSystemName,
            _collectionMetadata.VectorStoreName,
            _collectionName,
            "get",
            () => _mapper.MapFromStorageToDataModel(storageModel.Value, new StorageToDataModelMapperOptions()));

        return record;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<TRecord> GetAsync(IEnumerable<TKey> keys, GetRecordOptions? options = default,
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
    public async IAsyncEnumerable<TRecord> GetAsync(Expression<Func<TRecord, bool>> filter, int top, GetFilteredRecordOptions<TRecord>? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Verify.NotNull(filter);
        Verify.NotLessThan(top, 1);

        options ??= new();

        var translatedFilter = new ElasticsearchFilterTranslator().Translate(filter, _model);

        List<SortOptions>? sort = null;
        if (options.OrderBy?.Values?.Count is > 0)
        {
            sort = options.OrderBy.Values
                .Select(sortInfo => SortOptions.Field(_model.GetDataOrKeyProperty(sortInfo.PropertySelector).StorageName!, new FieldSort
                {
                    Order = sortInfo.Ascending ? SortOrder.Asc : SortOrder.Desc
                }))
                .ToList();
        }

        // Build search query.

        var query = translatedFilter ?? new MatchAllQuery();

        // Execute search query.

        var hits = await RunOperationAsync(
                "search",
                () => _elasticsearchClient.SearchAsync(
                    _collectionName,
                    query,
                    sort,
                    options.IncludeVectors ? null : _vectorFields,
                    options.Skip,
                    top,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        // Map results.

        var mappingOptions = new StorageToDataModelMapperOptions { IncludeVectors = options.IncludeVectors };
        var mappedResults = hits.Select(x =>
            _mapper.MapFromStorageToDataModel((x.Id, x.Source!), mappingOptions)
        );

        foreach (var result in mappedResults)
        {
            yield return result;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(TKey key, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(key);

        var keyValue = ElasticsearchVectorStoreRecordFieldMapping.KeyToElasticsearchId(key);

        await RunOperationAsync(
                "delete",
                () => _elasticsearchClient.DeleteDocumentAsync(_collectionName, keyValue, cancellationToken))
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(IEnumerable<TKey> keys, CancellationToken cancellationToken = default)
    {
        // TODO: Use _bulk endpoint

        Verify.NotNull(keys);

        foreach (var key in keys)
        {
            await DeleteAsync(key, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<TKey> UpsertAsync(TRecord record, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(record);

        // If an embedding generator is defined, invoke it once per property.
        Embedding<float>?[]? generatedEmbeddings = null;

        var vectorPropertyCount = _model.VectorProperties.Count;
        for (var i = 0; i < vectorPropertyCount; i++)
        {
            var vectorProperty = _model.VectorProperties[i];

            if (vectorProperty.EmbeddingGenerator is null)
            {
                continue;
            }

            // TODO: Ideally we'd group together vector properties using the same generator (and with the same input and output properties),
            //       and generate embeddings for them in a single batch. That's some more complexity though.

            if (vectorProperty.TryGenerateEmbedding<TRecord, Embedding<float>, ReadOnlyMemory<float>>(record, cancellationToken, out var task))
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

        var storageModel = VectorStoreErrorHandler.RunModelConversion(
            ElasticsearchConstants.VectorStoreSystemName,
            _collectionMetadata.VectorStoreName,
            _collectionName,
            "index",
            () => _mapper.MapFromDataToStorageModel(record, generatedEmbeddings));

        var id = await RunOperationAsync(
                "index",
                () => _elasticsearchClient.IndexDocumentAsync(Name, storageModel.id!, storageModel.document, cancellationToken))
            .ConfigureAwait(false);

        return ElasticsearchVectorStoreRecordFieldMapping.ElasticsearchIdToKey<TKey>(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TKey>> UpsertAsync(IEnumerable<TRecord> records, CancellationToken cancellationToken = default)
    {
        // TODO: Use _bulk endpoint

        Verify.NotNull(records);

        var result = new List<TKey>();

        foreach (var record in records)
        {
            result.Add(await UpsertAsync(record, cancellationToken).ConfigureAwait(false));
        }

        return result;
    }

    #endregion IVectorStoreRecordCollection

    #region IVectorSearch

    /// <inheritdoc />
    public async IAsyncEnumerable<VectorSearchResult<TRecord>> SearchAsync<TInput>(
        TInput value,
        int top,
        VectorSearchOptions<TRecord>? options = default,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TInput : notnull
    {
        Verify.NotNull(value);

        options ??= DefaultVectorSearchOptions;
        var vectorProperty = _model.GetVectorPropertyOrSingle(options);

        switch (vectorProperty.EmbeddingGenerator)
        {
            case IEmbeddingGenerator<TInput, Embedding<float>> generator:
                var embedding = await generator.GenerateAsync(value, new() { Dimensions = vectorProperty.Dimensions }, cancellationToken).ConfigureAwait(false);

                await foreach (var record in SearchCoreAsync(embedding.Vector, top, vectorProperty, options, cancellationToken).ConfigureAwait(false))
                {
                    yield return record;
                }

                yield break;

            case null:
                throw new InvalidOperationException(VectorDataStrings.NoEmbeddingGeneratorWasConfiguredForSearch);

            default:
#pragma warning disable CA1863
                throw new InvalidOperationException(
                    ElasticsearchVectorStoreRecordFieldMapping.SupportedVectorTypes.Contains(typeof(TInput))
                        ? string.Format(CultureInfo.InvariantCulture, VectorDataStrings.EmbeddingTypePassedToSearchAsync)
                        : string.Format(CultureInfo.InvariantCulture, VectorDataStrings.IncompatibleEmbeddingGeneratorWasConfiguredForInputType, typeof(TInput).Name, vectorProperty.EmbeddingGenerator.GetType().Name));
#pragma warning restore CA1863
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<VectorSearchResult<TRecord>> SearchEmbeddingAsync<TVector>(
        TVector vector,
        int top,
        VectorSearchOptions<TRecord>? options = null,
        CancellationToken cancellationToken = default)
        where TVector : notnull
    {
        options ??= DefaultVectorSearchOptions;
        var vectorProperty = _model.GetVectorPropertyOrSingle(options);

        return SearchCoreAsync(vector, top, vectorProperty, options, cancellationToken);
    }

    private async IAsyncEnumerable<VectorSearchResult<TRecord>> SearchCoreAsync<TVector>(
        TVector vector,
        int top,
        VectorStoreRecordVectorPropertyModel vectorProperty,
        VectorSearchOptions<TRecord> options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TVector : notnull
    {
        var floatVector = VerifyVectorParam(vector);
        Verify.NotLessThan(top, 1);

        if (options.IncludeVectors && _model.VectorProperties.Any(p => p.EmbeddingGenerator is not null))
        {
            throw new NotSupportedException(VectorDataStrings.IncludeVectorsNotSupportedWithEmbeddingGeneration);
        }

        // Build filter queries.

#pragma warning disable CS0618 // Type or member is obsolete
        var filter = options switch
        {
            { OldFilter: not null, Filter: not null } => throw new ArgumentException($"Either '{nameof(options.Filter)}' or '{nameof(options.OldFilter)}' can be specified, but not both."),
            { OldFilter: {} legacyFilter } => ElasticsearchVectorStoreCollectionSearchMapping.BuildFromLegacyFilter(legacyFilter, _model),
            { Filter: {} newFilter } => new ElasticsearchFilterTranslator().Translate(newFilter, _model),
            _ => null
        };
#pragma warning restore CS0618 // Type or member is obsolete

        // Build search query.

        var knnQuery = new KnnQuery
        {
            Field = vectorProperty.StorageName!,
            QueryVector = floatVector.ToArray(),
            Filter = filter is null ? null : [filter]
        };

        // Execute search query.

        var hits = await RunOperationAsync(
                "search",
                () => _elasticsearchClient.SearchAsync(
                    _collectionName,
                    Query.Knn(knnQuery),
                    null,
                    options.IncludeVectors ? null : _vectorFields,
                    options.Skip,
                    top,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        // Map results.

        var mappingOptions = new StorageToDataModelMapperOptions { IncludeVectors = options.IncludeVectors };
        var mappedResults = hits.Select(x =>
            new VectorSearchResult<TRecord>(
                VectorStoreErrorHandler.RunModelConversion(
                    ElasticsearchConstants.VectorStoreSystemName,
                    _collectionMetadata.VectorStoreName,
                    _collectionName,
                    "search",
                    () => _mapper.MapFromStorageToDataModel((x.Id, x.Source!), mappingOptions)),
                x.Score
            )
        );

        foreach (var result in mappedResults)
        {
            yield return result;
        }
    }

    /// <inheritdoc cref="IVectorSearch{TRecord}.GetService"/>
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        Verify.NotNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(VectorStoreRecordCollectionMetadata) ? _collectionMetadata :
            serviceType == typeof(ElasticsearchClient) ? this._elasticsearchClient.ElasticsearchClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    #endregion IVectorSearch

    #region IKeywordHybridSearch

    /// <inheritdoc />
    public async IAsyncEnumerable<VectorSearchResult<TRecord>> HybridSearchAsync<TVector>(
        TVector vector,
        ICollection<string> keywords,
        int top,
        HybridSearchOptions<TRecord>? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var floatVector = VerifyVectorParam(vector);
        Verify.NotLessThan(top, 1);
        Verify.NotNull(keywords);

        // Resolve options.

        options ??= DefaultKeywordVectorizedHybridSearchOptions;
        var vectorProperty = _model.GetVectorPropertyOrSingle<TRecord>(new() { VectorProperty = options.VectorProperty });
        var textDataProperty = _model.GetFullTextDataPropertyOrSingle(options.AdditionalProperty);

        // Build filter queries.

#pragma warning disable CS0618 // Type or member is obsolete
        var filter = options switch
        {
            { OldFilter: not null, Filter: not null } => throw new ArgumentException($"Either '{nameof(options.Filter)}' or '{nameof(options.OldFilter)}' can be specified, but not both."),
            { OldFilter: {} legacyFilter } => ElasticsearchVectorStoreCollectionSearchMapping.BuildFromLegacyFilter(legacyFilter, _model),
            { Filter: {} newFilter } => new ElasticsearchFilterTranslator().Translate(newFilter, _model),
            _ => null
        };
#pragma warning restore CS0618 // Type or member is obsolete

        // Build search query.

        var knn = new KnnSearch
        {
            Field = vectorProperty.StorageName!,
            k = top,
            NumCandidates = Math.Max((int)Math.Ceiling(1.5f * top), 100),
            QueryVector = floatVector,
            Filter = filter is null ? null : [filter]
        };

        var query = Query.Terms(new TermsQuery()
        {
            Field = textDataProperty.StorageName!,
            Terms = new TermsQueryField(keywords.Select(FieldValue.String).ToArray())
        });

        if (filter is not null)
        {
            query = filter && query;
        }

        var rank = Rank.Rrf(new RrfRank
        {
            RankWindowSize = knn.NumCandidates
        });

        // Execute search query.

        var hits = await RunOperationAsync(
                "search",
                () => _elasticsearchClient.HybridSearchAsync(
                    _collectionName,
                    knn,
                    query,
                    rank,
                    options.IncludeVectors ? null : _vectorFields,
                    options.Skip,
                    top,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        // Map results.

        var mapperOptions = new StorageToDataModelMapperOptions { IncludeVectors = options.IncludeVectors };
        var mappedResults = hits.Select(x =>
            new VectorSearchResult<TRecord>(
                VectorStoreErrorHandler.RunModelConversion(
                    ElasticsearchConstants.VectorStoreSystemName,
                    _collectionMetadata.VectorStoreName,
                    _collectionName,
                    "search",
                    () => _mapper.MapFromStorageToDataModel((x.Id, x.Source!), mapperOptions)),
                x.Score
            )
        );

        foreach (var result in mappedResults)
        {
            yield return result;
        }
    }

    #endregion IKeywordHybridSearch

    #region IVectorizedSearch

    /// <inheritdoc />
    [Obsolete("Use either SearchEmbeddingAsync to search directly on embeddings, or SearchAsync to handle embedding generation internally as part of the call.")]
    public IAsyncEnumerable<VectorSearchResult<TRecord>> VectorizedSearchAsync<TVector>(TVector vector, int top, VectorSearchOptions<TRecord>? options = null, CancellationToken cancellationToken = default)
        where TVector : notnull
    {
        return SearchEmbeddingAsync(vector, top, options, cancellationToken);
    }

    #endregion IVectorizedSearch

    /// <summary>
    /// Run the given operation and wrap any <see cref="TransportException" /> with <see cref="VectorStoreOperationException" />."/>
    /// </summary>
    /// <param name="operationName">The type of database operation being run.</param>
    /// <param name="operation">The operation to run.</param>
    /// <returns>The result of the operation.</returns>
    private async Task RunOperationAsync(string operationName, Func<Task> operation)
    {
        try
        {
            await operation.Invoke().ConfigureAwait(false);
        }
        catch (TransportException ex)
        {
            throw new VectorStoreOperationException("Call to vector store failed.", ex)
            {
                VectorStoreSystemName = ElasticsearchConstants.VectorStoreSystemName,
                VectorStoreName = _collectionMetadata.VectorStoreName,
                CollectionName = Name,
                OperationName = operationName
            };
        }
    }

    /// <summary>
    /// Run the given operation and wrap any <see cref="TransportException" /> with <see cref="VectorStoreOperationException" />."/>
    /// </summary>
    /// <typeparam name="T">The response type of the operation.</typeparam>
    /// <param name="operationName">The type of database operation being run.</param>
    /// <param name="operation">The operation to run.</param>
    /// <returns>The result of the operation.</returns>
    private async Task<T> RunOperationAsync<T>(string operationName, Func<Task<T>> operation)
    {
        try
        {
            return await operation.Invoke().ConfigureAwait(false);
        }
        catch (TransportException ex)
        {
            throw new VectorStoreOperationException("Call to vector store failed.", ex)
            {
                VectorStoreSystemName = ElasticsearchConstants.VectorStoreSystemName,
                VectorStoreName = _collectionMetadata.VectorStoreName,
                CollectionName = Name,
                OperationName = operationName
            };
        }
    }

    private static ICollection<float> VerifyVectorParam<TVector>(TVector vector)
    {
        Verify.NotNull(vector);

        return vector switch
        {
            ReadOnlyMemory<float> v => v.ToArray(),
            ICollection<float> v => v,
            IEnumerable<float> v => [.. v],
            _ => throw new NotSupportedException($"The provided vector type {vector.GetType().FullName} is not supported by the Elasticsearch connector.")
        };
    }
}
