// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Nodes;

using Elastic.Clients.Elasticsearch;
using Elastic.Transport.Extensions;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData.ProviderServices;
using Microsoft.SemanticKernel;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
/// A mapper that maps between the generic Semantic Kernel data model and the model that the data is stored under,
/// within Elasticsearch.
/// </summary>
internal sealed class ElasticsearchMapper<TKey, TRecord> :
    IElasticsearchMapper<TRecord, (string? id, JsonObject document)>
    where TKey : notnull
    where TRecord : notnull
{
    /// <summary>
    /// A model representing a record in a vector store collection.
    /// </summary>
    private readonly CollectionModel _model;

    /// <summary>
    /// The Elasticsearch client settings.
    /// </summary>
    private readonly IElasticsearchClientSettings _elasticsearchClientSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticsearchDynamicMapper" /> class.
    /// </summary>
    /// <param name="model">A model representing a record in a vector store collection.</param>
    /// <param name="elasticsearchClientSettings">The Elasticsearch client settings to use.</param>
    public ElasticsearchMapper(
        CollectionModel model,
        IElasticsearchClientSettings elasticsearchClientSettings)
    {
        Verify.NotNull(model);
        Verify.NotNull(elasticsearchClientSettings);

        // Assign.
        _model = model;
        _elasticsearchClientSettings = elasticsearchClientSettings;
    }

    /// <inheritdoc />
    public (string? id, JsonObject document) MapFromDataToStorageModel(TRecord dataModel, Embedding<float>?[]? generatedEmbeddings)
    {
        Verify.NotNull(dataModel);

        var keyProperty = _model.KeyProperty;
        var keyValue = ElasticsearchKeyMapping.KeyToElasticsearchId(keyProperty.GetValueAsObject(dataModel) ?? throw new InvalidOperationException("Key can not be 'null'."));

        var document = SerializeSource(dataModel, _elasticsearchClientSettings).AsObject();

        // In Elasticsearch, the key aka. id is stored outside the document payload.
        document.Remove(keyProperty.StorageName);

        // Update vector properties.

        for (var i = 0; i < _model.VectorProperties.Count; i++)
        {
            var vectorProperty = _model.VectorProperties[i];

            Embedding<float>? embedding = null;

            if (vectorProperty.Type == typeof(Embedding<float>))
            {
                // STJ serializes `Embedding<T>` as complex object by default, but we have to make sure the vector is
                // stored in array representation instead.
                embedding = vectorProperty.GetValueAsObject(dataModel) as Embedding<float>;
            }

            if (generatedEmbeddings?[i] is { } generatedEmbedding)
            {
                // Use generated embedding for this vector property.
                embedding = generatedEmbedding;
            }

            if (embedding is null)
            {
                continue;
            }

            document[vectorProperty.StorageName] = JsonValue.Create(embedding.Vector);
        }

        return (keyValue, document);
    }

    /// <inheritdoc />
    public TRecord MapFromStorageToDataModel((string? id, JsonObject document) storageModel, bool includeVectors)
    {
        Verify.NotNull(storageModel);
        Verify.NotNull(storageModel.id);
        Verify.NotNull(storageModel.document);

        var dataModel = storageModel.document;

        // In Elasticsearch, the key aka. id is stored outside the document payload.

        var keyProperty = _model.KeyProperty;
        dataModel.Add(keyProperty.StorageName, JsonValue.Create(ElasticsearchKeyMapping.ElasticsearchIdToKey<TKey>(storageModel.id)));

        // Update vector properties.

        foreach (var vectorProperty in _model.VectorProperties)
        {
            if (!storageModel.document.TryGetPropertyValue(vectorProperty.StorageName, out var value) || (value is null))
            {
                // Skip property if it's not in the storage model or `null`.
                continue;
            }

            if (!includeVectors || !ElasticsearchModelBuilder.IsVectorPropertyTypeValidCore(vectorProperty.Type, out _))
            {
                // For vector properties which have embedding generation configured, we need to remove the embeddings
                // before deserializing (we can't go back from an embedding to e.g. string).
                dataModel.Remove(vectorProperty.StorageName);
                continue;
            }

            if (vectorProperty.Type != typeof(Embedding<float>))
            {
                continue;
            }

            // Create `Embedding<T>` from array representation.
            var embedding = new Embedding<float>(_elasticsearchClientSettings.SourceSerializer.Deserialize<ReadOnlyMemory<float>>(value));
            dataModel[vectorProperty.StorageName] = JsonValue.Create(embedding);
        }

        return _elasticsearchClientSettings.SourceSerializer.Deserialize<TRecord>(dataModel)!;
    }

    private static JsonNode SerializeSource<T>(T obj, IElasticsearchClientSettings settings)
    {
        // TODO: Add SerializeToNode extension in Elastic.Transport.

        if (settings.SourceSerializer.TryGetJsonSerializerOptions(out var options))
        {
            return JsonSerializer.SerializeToNode(obj, options)!;
        }

        using var stream = settings.MemoryStreamFactory.Create();

        settings.SourceSerializer.Serialize(obj, stream);
        stream.Position = 0;

        return settings.SourceSerializer.Deserialize<JsonNode>(stream);
    }
}
