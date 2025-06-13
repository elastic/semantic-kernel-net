// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
internal sealed class ElasticsearchDynamicMapper :
    IElasticsearchMapper<Dictionary<string, object?>, (string? id, JsonObject document)>
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
    public ElasticsearchDynamicMapper(
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
    public (string? id, JsonObject document) MapFromDataToStorageModel(Dictionary<string, object?> dataModel, Embedding<float>?[]? generatedEmbeddings)
    {
        Verify.NotNull(dataModel);

        var keyProperty = _model.KeyProperty;
        var keyValue = ElasticsearchKeyMapping.KeyToElasticsearchId(keyProperty.GetValueAsObject(dataModel) ?? throw new InvalidOperationException("Key can not be 'null'."));

        var document = new JsonObject();
        var vectorPropertyIndex = 0;

        foreach (var property in _model.Properties)
        {
            if (property is KeyPropertyModel)
            {
                // In Elasticsearch, the key aka. id is stored outside the document payload.
                continue;
            }

            if (property is VectorPropertyModel vectorProperty)
            {
                var i = vectorPropertyIndex++;

                Embedding<float>? embedding = null;

                if (vectorProperty.Type == typeof(Embedding<float>))
                {
                    // STJ serializes `Embedding<T>` as complex object by default, but we have to make sure the vector
                    // is stored in array representation instead.
                    embedding = vectorProperty.GetValueAsObject(dataModel) as Embedding<float>;
                }

                if (generatedEmbeddings?[i] is { } generatedEmbedding)
                {
                    // Use generated embedding for this vector property.
                    embedding = generatedEmbedding;
                }

                if (embedding is not null)
                {
                    document[vectorProperty.StorageName] = JsonValue.Create(embedding.Vector);
                    continue;
                }
            }

            var value = property.GetValueAsObject(dataModel);

            // TODO: Use JsonSerializerOptions.DefaultIgnoreCondition
            document.Add(property.StorageName, (value is null) ? null : SerializeSource(value, _elasticsearchClientSettings));
        }

        return (keyValue, document);
    }

    /// <inheritdoc />
    public Dictionary<string, object?> MapFromStorageToDataModel((string? id, JsonObject document) storageModel, bool includeVectors)
    {
        Verify.NotNull(storageModel);

        var dataModel = new Dictionary<string, object?>();

        var keyProperty = _model.KeyProperty;
        keyProperty.SetValueAsObject(dataModel, storageModel.id);

        foreach (var property in _model.Properties)
        {
            if (property is KeyPropertyModel)
            {
                // In Elasticsearch, the key aka. id is stored outside the document payload.
                continue;
            }

            if (!storageModel.document.TryGetPropertyValue(property.StorageName, out var value))
            {
                // 
                property.SetValueAsObject(dataModel, property.Type.IsValueType ? Activator.CreateInstance(property.Type) : null);
                continue;
            }

            if (value is null)
            {
                property.SetValueAsObject(dataModel, null);
                continue;
            }

            if (property is VectorPropertyModel vectorProperty)
            {
                if (!includeVectors)
                {
                    continue;
                }

                object vectorValue = (Nullable.GetUnderlyingType(vectorProperty.Type) ?? vectorProperty.Type) switch
                {
                    {} t when t == typeof(ReadOnlyMemory<float>) => new ReadOnlyMemory<float>(ToArray<float>(value)),
                    {} t when t == typeof(Embedding<float>) => new Embedding<float>(ToArray<float>(value)),
                    {} t when t == typeof(float[]) => ToArray<float>(value),
                    {} t when t == typeof(IEnumerable<float>) => ToArray<float>(value),
                    {} t when t == typeof(IReadOnlyCollection<float>) => ToArray<float>(value),
                    {} t when t == typeof(ICollection<float>) => ToArray<float>(value),
                    {} t when t == typeof(IReadOnlyList<float>) => ToArray<float>(value),
                    {} t when t == typeof(IList<float>) => ToArray<float>(value),
                    _ => throw new InvalidOperationException("unreachable")
                };

                property.SetValueAsObject(dataModel, vectorValue);
                continue;
            }

            var deserializedData = _elasticsearchClientSettings.SourceSerializer.Deserialize(value, property.Type);

            property.SetValueAsObject(dataModel, deserializedData);
        }

        return dataModel;

        static T[] ToArray<T>(JsonNode jsonNode)
        {
            var jsonArray = jsonNode.AsArray();
            var array = new T[jsonArray.Count];

            for (var i = 0; i < jsonArray.Count; i++)
            {
                array[i] = jsonArray[i]!.GetValue<T>();
            }

            return array;
        }
    }

    private static JsonNode SerializeSource<T>(T obj, IElasticsearchClientSettings settings)
    {
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
