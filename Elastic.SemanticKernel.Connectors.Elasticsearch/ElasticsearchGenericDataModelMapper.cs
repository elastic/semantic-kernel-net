using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

using Elastic.Clients.Elasticsearch;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
///     A mapper that maps between the generic Semantic Kernel data model and the model that the data is stored under,
///     within Elasticsearch.
/// </summary>
internal sealed class ElasticsearchGenericDataModelMapper :
    IVectorStoreRecordMapper<VectorStoreGenericDataModel<string>, (string? id, JsonObject document)>
{
    /// <summary>The Elasticsearch client settings.</summary>
    private readonly IElasticsearchClientSettings _elasticsearchClientSettings;

    /// <summary>A mapping from <see cref="VectorStoreRecordDefinition" /> to storage model property name.</summary>
    private readonly Dictionary<VectorStoreRecordProperty, string> _properties;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ElasticsearchGenericDataModelMapper" /> class.
    /// </summary>
    /// <param name="propertyReader">A helper to access property information for the current data model and record definition.</param>
    /// <param name="elasticsearchClientSettings">The Elasticsearch client settings to use.</param>
    public ElasticsearchGenericDataModelMapper(
        VectorStoreRecordPropertyReader propertyReader,
        IElasticsearchClientSettings elasticsearchClientSettings)
    {
        Verify.NotNull(propertyReader);

        // Assign.
        _elasticsearchClientSettings = elasticsearchClientSettings;
        _properties = propertyReader.Properties.ToDictionary(k => k, v => elasticsearchClientSettings.DefaultFieldNameInferrer(v.DataModelPropertyName!));
    }

    /// <inheritdoc />
    public (string? id, JsonObject document) MapFromDataToStorageModel(VectorStoreGenericDataModel<string> dataModel)
    {
        Verify.NotNull(dataModel);

        var document = new JsonObject();

        foreach (var item in _properties)
        {
            var property = item.Key;
            var storageModelPropertyName = item.Value;

            var sourceDictionary = property switch
            {
                VectorStoreRecordKeyProperty => null,
                VectorStoreRecordVectorProperty => dataModel.Vectors,
                VectorStoreRecordDataProperty => dataModel.Data,
                _ => throw new NotSupportedException($"Property of type '{property.GetType().Name}' is not supported.")
            };

            if (sourceDictionary is null)
            {
                continue;
            }

            if (!sourceDictionary.TryGetValue(property.DataModelPropertyName, out var value))
            {
                // Just skip this property if it's not in the data model.
                continue;
            }

            document.Add(storageModelPropertyName, SerializeSource(value, _elasticsearchClientSettings));
        }

        return (dataModel.Key, document);
    }

    /// <inheritdoc />
    public VectorStoreGenericDataModel<string> MapFromStorageToDataModel((string? id, JsonObject document) storageModel,
        StorageToDataModelMapperOptions options)
    {
        Verify.NotNull(storageModel);

        var dataModel = new VectorStoreGenericDataModel<string>(storageModel.id!);

        foreach (var item in _properties)
        {
            var property = item.Key;
            var storageModelPropertyName = item.Value;

            if (!storageModel.document.TryGetPropertyValue(storageModelPropertyName, out var value))
            {
                // Just skip this property if it's not in the storage model.
                continue;
            }

            var targetDictionary = property switch
            {
                VectorStoreRecordKeyProperty => null,
                VectorStoreRecordVectorProperty => dataModel.Vectors,
                VectorStoreRecordDataProperty => dataModel.Data,
                _ => throw new NotSupportedException($"Property of type '{property.GetType().Name}' is not supported.")
            };

            if (targetDictionary is null)
            {
                continue;
            }

            targetDictionary[property.DataModelPropertyName] = DeserializeSource(value, _elasticsearchClientSettings, property.PropertyType);
        }

        return dataModel;
    }

    private static JsonNode? SerializeSource<T>(T? obj, IElasticsearchClientSettings settings)
    {
        // TODO: Use `SourceSerialization` helper

        if (obj is null)
        {
            return null;
        }

        using var stream = settings.MemoryStreamFactory.Create();

        settings.SourceSerializer.Serialize(obj, stream);
        stream.Position = 0;

        return JsonNode.Parse(stream);
    }

    private static object? DeserializeSource(JsonNode? node, IElasticsearchClientSettings settings, Type type)
    {
        // TODO: Use `SourceSerialization` helper

        if (node is null)
        {
            return null;
        }

        using var stream = settings.MemoryStreamFactory.Create();

        using var writer = new Utf8JsonWriter(stream);
        node.WriteTo(writer);
        writer.Flush();
        stream.Position = 0;

        return settings.SourceSerializer.Deserialize(type, stream);
    }
}
