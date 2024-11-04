// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

using Elastic.Clients.Elasticsearch;
using Elastic.Transport.Extensions;

using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;

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
    private readonly Dictionary<VectorStoreRecordProperty, string> _propertyToStorageName;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ElasticsearchGenericDataModelMapper" /> class.
    /// </summary>
    /// <param name="propertyToStorageName">A mapping from <see cref="VectorStoreRecordDefinition" /> to storage model property name.</param>
    /// <param name="elasticsearchClientSettings">The Elasticsearch client settings to use.</param>
    public ElasticsearchGenericDataModelMapper(
        Dictionary<VectorStoreRecordProperty, string> propertyToStorageName,
        IElasticsearchClientSettings elasticsearchClientSettings)
    {
        Verify.NotNull(propertyToStorageName);
        Verify.NotNull(elasticsearchClientSettings);

        // Assign.
        _elasticsearchClientSettings = elasticsearchClientSettings;
        _propertyToStorageName = propertyToStorageName;
    }

    /// <inheritdoc />
    public (string? id, JsonObject document) MapFromDataToStorageModel(VectorStoreGenericDataModel<string> dataModel)
    {
        Verify.NotNull(dataModel);

        var document = new JsonObject();

        foreach (var item in _propertyToStorageName)
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

        foreach (var item in _propertyToStorageName)
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

            targetDictionary[property.DataModelPropertyName] = value is null
                ? null
                : _elasticsearchClientSettings.SourceSerializer.Deserialize(value, property.PropertyType);
        }

        return dataModel;
    }

    private static JsonNode? SerializeSource<T>(T? obj, IElasticsearchClientSettings settings)
    {
        if (obj is null)
        {
            return null;
        }

        using var stream = settings.MemoryStreamFactory.Create();

        settings.SourceSerializer.Serialize(obj, stream);
        stream.Position = 0;

        return settings.SourceSerializer.Deserialize<JsonNode>(stream);
    }
}
