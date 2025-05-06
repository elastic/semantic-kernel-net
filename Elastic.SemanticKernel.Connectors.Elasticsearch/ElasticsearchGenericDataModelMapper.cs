// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

using Elastic.Clients.Elasticsearch;
using Elastic.Transport.Extensions;

using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.VectorData.ConnectorSupport;
using Microsoft.SemanticKernel;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
/// A mapper that maps between the generic Semantic Kernel data model and the model that the data is stored under,
/// within Elasticsearch.
/// </summary>
internal sealed class ElasticsearchGenericDataModelMapper :
    IElasticsearchVectorStoreRecordMapper<Dictionary<string, object?>, (string? id, JsonObject document)>
{
    /// <summary>
    /// A model representing a record in a vector store collection.
    /// </summary>
    private readonly VectorStoreRecordModel _model;

    /// <summary>
    /// The Elasticsearch client settings.
    /// </summary>
    private readonly IElasticsearchClientSettings _elasticsearchClientSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticsearchGenericDataModelMapper" /> class.
    /// </summary>
    /// <param name="model">A model representing a record in a vector store collection.</param>
    /// <param name="elasticsearchClientSettings">The Elasticsearch client settings to use.</param>
    public ElasticsearchGenericDataModelMapper(
        VectorStoreRecordModel model,
        IElasticsearchClientSettings elasticsearchClientSettings)
    {
        Verify.NotNull(model);
        Verify.NotNull(elasticsearchClientSettings);

        // Assign.
        _model = model;
        _elasticsearchClientSettings = elasticsearchClientSettings;
    }

    /// <inheritdoc />
    public (string? id, JsonObject document) MapFromDataToStorageModel(Dictionary<string, object?> dataModel)
    {
        Verify.NotNull(dataModel);

        var keyProperty = _model.KeyProperty;
        var keyValue = (string?)keyProperty.GetValueAsObject(dataModel);

        var document = new JsonObject();

        foreach (var property in _model.Properties)
        {
            if (property is VectorStoreRecordKeyPropertyModel)
            {
                // The key is not part of the document payload.
                continue;
            }

            var value = property.GetValueAsObject(dataModel);
            if (value is null)
            {
                continue;
            }

            document.Add(property.StorageName, SerializeSource(value, _elasticsearchClientSettings));
        }

        return (keyValue, document);
    }

    /// <inheritdoc />
    public Dictionary<string, object?> MapFromStorageToDataModel((string? id, JsonObject document) storageModel,
        StorageToDataModelMapperOptions options)
    {
        Verify.NotNull(storageModel);

        var dataModel = new Dictionary<string, object?>();

        var keyProperty = _model.KeyProperty;
        keyProperty.SetValueAsObject(dataModel, storageModel.id);

        foreach (var property in _model.Properties)
        {
            if (property is VectorStoreRecordKeyPropertyModel)
            {
                // The key is not part of the document payload.
                continue;
            }

            if (!storageModel.document.TryGetPropertyValue(property.StorageName, out var value))
            {
                // Just skip this property if it's not in the storage model.
                continue;
            }

            var deserializedData = value is null
                ? null
                : _elasticsearchClientSettings.SourceSerializer.Deserialize(value, property.Type);

            property.SetValueAsObject(dataModel, deserializedData);
        }

        return dataModel;
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
