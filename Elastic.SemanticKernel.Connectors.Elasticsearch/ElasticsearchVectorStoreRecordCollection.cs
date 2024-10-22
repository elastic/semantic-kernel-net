using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;

using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix

/// <summary>
///     Service for storing and retrieving vector records, that uses Elasticsearch as the underlying storage.
/// </summary>
/// <typeparam name="TRecord">The data model to use for adding, updating and retrieving data from storage.</typeparam>
public sealed class ElasticsearchVectorStoreRecordCollection<TRecord> :
    IVectorStoreRecordCollection<string, TRecord>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    /// <summary>The name of this database for telemetry purposes.</summary>
    private const string DatabaseName = "Elasticsearch";

    /// <summary>A set of types that a key on the provided model may have.</summary>
    private static readonly HashSet<Type> SupportedKeyTypes =
    [
        typeof(string)
    ];

    /// <summary>The default options for vector search.</summary>
    private static readonly VectorSearchOptions DefaultVectorSearchOptions = new();

    /// <summary>Elasticsearch client that can be used to manage the collections and points in an Elasticsearch store.</summary>
    private readonly MockableElasticsearchClient _elasticsearchClient;

    /// <summary>Optional configuration options for this class.</summary>
    private readonly ElasticsearchVectorStoreRecordCollectionOptions<TRecord> _options;

    /// <summary>A helper to access property information for the current data model and record definition.</summary>
    private readonly VectorStoreRecordPropertyReader _propertyReader;

    /// <summary>A mapping from <see cref="VectorStoreRecordDefinition" /> to storage model property name.</summary>
    private readonly Dictionary<VectorStoreRecordProperty, string> _propertyToStorageName;

    /// <summary>TODO: TBC</summary>
    private readonly IVectorStoreRecordMapper<TRecord, (string? id, JsonObject document)> _mapper;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ElasticsearchVectorStoreRecordCollection{TRecord}" /> class.
    /// </summary>
    /// <param name="elasticsearchClient">
    ///     Elasticsearch client that can be used to manage the collections and points in an
    ///     Elasticsearch store.
    /// </param>
    /// <param name="collectionName">
    ///     The name of the collection that this
    ///     <see cref="ElasticsearchVectorStoreRecordCollection{TRecord}" /> will access.
    /// </param>
    /// <param name="options">Optional configuration options for this class.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="elasticsearchClient" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown for any misconfigured options.</exception>
    public ElasticsearchVectorStoreRecordCollection(ElasticsearchClient elasticsearchClient, string collectionName,
        ElasticsearchVectorStoreRecordCollectionOptions<TRecord>? options = null)
        : this(new MockableElasticsearchClient(elasticsearchClient), collectionName, options)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ElasticsearchVectorStoreRecordCollection{TRecord}" /> class.
    /// </summary>
    /// <param name="elasticsearchClient">
    ///     Elasticsearch client that can be used to manage the collections and points in an
    ///     Elasticsearch store.
    /// </param>
    /// <param name="collectionName">
    ///     The name of the collection that this
    ///     <see cref="ElasticsearchVectorStoreRecordCollection{TRecord}" /> will access.
    /// </param>
    /// <param name="options">Optional configuration options for this class.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="elasticsearchClient" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown for any misconfigured options.</exception>
    internal ElasticsearchVectorStoreRecordCollection(MockableElasticsearchClient elasticsearchClient,
        string collectionName, ElasticsearchVectorStoreRecordCollectionOptions<TRecord>? options = null)
    {
        // Verify.
        Verify.NotNull(elasticsearchClient);
        Verify.NotNullOrWhiteSpace(collectionName);
        VectorStoreRecordPropertyVerification.VerifyGenericDataModelKeyType(typeof(TRecord), false /* TODO: options?.CustomMapper is not null */, SupportedKeyTypes);
        VectorStoreRecordPropertyVerification.VerifyGenericDataModelDefinitionSupplied(typeof(TRecord), options?.VectorStoreRecordDefinition is not null);

        // Assign.
        _elasticsearchClient = elasticsearchClient;
        CollectionName = collectionName;
        _options = options ?? new ElasticsearchVectorStoreRecordCollectionOptions<TRecord>();
        _propertyReader = new VectorStoreRecordPropertyReader(
            typeof(TRecord),
            _options.VectorStoreRecordDefinition,
            new VectorStoreRecordPropertyReaderOptions
            {
                RequiresAtLeastOneVector = false,
                SupportsMultipleKeys = false,
                SupportsMultipleVectors = true
            });

        if (typeof(TRecord) == typeof(VectorStoreGenericDataModel<string>))
        {
            // Prioritize the user provided `StoragePropertyName` or fall-back to using the `DefaultFieldNameInferrer`
            // function of the Elasticsearch client which by default redirects to the
            // `JsonSerializerOptions.PropertyNamingPolicy.Convert() method.
            _propertyToStorageName = _propertyReader.Properties.ToDictionary(k => k, v => v.StoragePropertyName ??
                _elasticsearchClient.ElasticsearchClient.ElasticsearchClientSettings.DefaultFieldNameInferrer(v.DataModelPropertyName));

            _mapper = (new ElasticsearchGenericDataModelMapper(_propertyToStorageName, elasticsearchClient.ElasticsearchClient.ElasticsearchClientSettings) as
                IVectorStoreRecordMapper<TRecord, (string id, JsonObject document)>)!;
        }
        else
        {
            // Use the built-in property name inference of the Elasticsearch client. The default implementation
            // prioritizes `JsonPropertyName` attributes and falls-back to the `DefaultFieldNameInferrer` function,
            // which by default redirects to the `JsonSerializerOptions.PropertyNamingPolicy.Convert() method.
            _propertyToStorageName = _propertyReader.Properties.ToDictionary(k => k, v =>
            {
                var info = _propertyReader.KeyPropertiesInfo.FirstOrDefault(x => string.Equals(x.Name, v.DataModelPropertyName, StringComparison.Ordinal)) ??
                           _propertyReader.VectorPropertiesInfo.FirstOrDefault(x => string.Equals(x.Name, v.DataModelPropertyName, StringComparison.Ordinal)) ??
                           _propertyReader.DataPropertiesInfo.FirstOrDefault(x => string.Equals(x.Name, v.DataModelPropertyName, StringComparison.Ordinal));

                if (info is null)
                {
                    throw new InvalidOperationException("unreachable");
                }

                return _elasticsearchClient.ElasticsearchClient.Infer.PropertyName(info);
            });

            _mapper = new ElasticsearchDataModelMapper<TRecord>(_propertyToStorageName, elasticsearchClient.ElasticsearchClient.ElasticsearchClientSettings);
        }

        // Validate property types.
        _propertyReader.VerifyKeyProperties(SupportedKeyTypes);
        VectorStoreRecordPropertyVerification.VerifyPropertyTypes(_propertyReader.VectorProperties, [typeof(ReadOnlyMemory<float>)], [typeof(float)], "Vector");
    }

    /// <inheritdoc />
    public string CollectionName { get; }

    /// <inheritdoc />
    public Task<bool> CollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        return RunOperationAsync(
            "indices.exists",
            () => _elasticsearchClient.IndexExistsAsync(CollectionName, cancellationToken));
    }

    /// <inheritdoc />
    public Task CreateCollectionAsync(CancellationToken cancellationToken = default)
    {
        var propertyMappings = ElasticsearchVectorStoreCollectionCreateMapping.BuildPropertyMappings(_propertyReader, _propertyToStorageName);

        return RunOperationAsync(
            "indices.create",
            () => _elasticsearchClient.CreateIndexAsync(CollectionName, propertyMappings, cancellationToken));
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
            () => _elasticsearchClient.DeleteIndexAsync(CollectionName, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<TRecord?> GetAsync(string key, GetRecordOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // TODO: Handle options

        var storageModel = await RunOperationAsync(
                "get",
                () => _elasticsearchClient.GetDocumentAsync(CollectionName, key, cancellationToken))
            .ConfigureAwait(false);

        if (!storageModel.HasValue)
        {
            return default;
        }

        var record = VectorStoreErrorHandler.RunModelConversion(DatabaseName, CollectionName, "get",
            () => _mapper.MapFromStorageToDataModel(storageModel.Value, new StorageToDataModelMapperOptions()));

        return record;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<TRecord> GetBatchAsync(IEnumerable<string> keys, GetRecordOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // TODO: Use mget endpoint
        // TODO: Handle options

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
    public async Task DeleteAsync(string key, DeleteRecordOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // TODO: Handle options

        Verify.NotNullOrWhiteSpace(key);

        await RunOperationAsync(
                "delete",
                () => _elasticsearchClient.DeleteDocumentAsync(CollectionName, key, cancellationToken))
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteBatchAsync(IEnumerable<string> keys, DeleteRecordOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // TODO: Use _bulk endpoint
        // TODO: Handle options

        Verify.NotNull(keys);

        foreach (var key in keys)
        {
            await DeleteAsync(key, options, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<string> UpsertAsync(TRecord record, UpsertRecordOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // TODO: Handle options

        Verify.NotNull(record);

        var storageModel = VectorStoreErrorHandler.RunModelConversion(DatabaseName, CollectionName, "index",
            () => _mapper.MapFromDataToStorageModel(record));

        var id = await RunOperationAsync(
                "index",
                () => _elasticsearchClient.IndexDocumentAsync(CollectionName, storageModel.id!, storageModel.document,
                    cancellationToken))
            .ConfigureAwait(false);

        return id;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> UpsertBatchAsync(IEnumerable<TRecord> records, UpsertRecordOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // TODO: Use _bulk endpoint
        // TODO: Handle options

        Verify.NotNull(records);

        foreach (var record in records)
        {
            yield return await UpsertAsync(record, options, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<VectorSearchResults<TRecord>> VectorizedSearchAsync<TVector>(TVector vector, VectorSearchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(vector);

        // Validate inputs.

        if (this._propertyReader.FirstVectorPropertyName is null)
        {
            throw new InvalidOperationException("The collection does not have any vector fields, so vector search is not possible.");
        }

        var floatVector = vector switch
        {
            ICollection<float> v => v,
            IEnumerable<float> v => [.. v],
            ReadOnlyMemory<float> v => v.ToArray(),
            _ => throw new NotSupportedException($"The provided vector type {vector.GetType().FullName} is not supported by the Elasticsearch connector.")
        };

        var searchOptions = options ?? DefaultVectorSearchOptions;

        // Specify the vector name.

        var vectorProperty = _propertyReader.VectorProperties[0];
        if (!string.IsNullOrWhiteSpace(searchOptions.VectorPropertyName))
        {
            vectorProperty = _propertyReader.VectorProperties.First(x => string.Equals(x.DataModelPropertyName, searchOptions.VectorPropertyName, StringComparison.Ordinal));
        }

        // Build search query.

        var knnQuery = new KnnQuery
        {
            Field = _propertyToStorageName[vectorProperty]!,
            QueryVector = floatVector.ToArray()
        };

        var filterQueries = ElasticsearchVectorStoreCollectionSearchMapping.BuildFilter(searchOptions.Filter, _propertyToStorageName);
        if (filterQueries.Count != 0)
        {
            knnQuery.Filter = filterQueries;
        }

        // Execute search query.

        var result = await RunOperationAsync(
                "search",
                () => _elasticsearchClient.SearchAsync(
                    CollectionName,
                    Query.Knn(knnQuery),
                    searchOptions.Skip,
                    searchOptions.Top,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        // Map results.

        var mappedResults = result.hits.Select(x =>
            new VectorSearchResult<TRecord>(
                VectorStoreErrorHandler.RunModelConversion(DatabaseName, CollectionName, "search",
                    () => _mapper.MapFromStorageToDataModel((x.id, x.document), new StorageToDataModelMapperOptions())),
                x.score
            )
        );

        return new VectorSearchResults<TRecord>(mappedResults.ToAsyncEnumerable())
        {
            TotalCount = searchOptions.IncludeTotalCount ? (result.total is var total and >= 0) ? total : null : null
        };
    }

    /// <summary>
    ///     Run the given operation and wrap any <see cref="TransportException" /> with
    ///     <see cref="VectorStoreOperationException" />."/>
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
                VectorStoreType = DatabaseName,
                CollectionName = CollectionName,
                OperationName = operationName
            };
        }
    }

    /// <summary>
    ///     Run the given operation and wrap any <see cref="TransportException" /> with
    ///     <see cref="VectorStoreOperationException" />."/>
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
                VectorStoreType = DatabaseName,
                CollectionName = CollectionName,
                OperationName = operationName
            };
        }
    }
}
