// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

using Elastic.Clients.Elasticsearch;
using Elastic.Transport.Extensions;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.VectorData.ConnectorSupport;
using Microsoft.SemanticKernel;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
/// A mapper that maps between the generic Semantic Kernel data model and the model that the data is stored under,
/// within Elasticsearch.
/// </summary>
internal sealed class ElasticsearchDataModelMapper<TKey, TRecord> :
    IElasticsearchVectorStoreRecordMapper<TRecord, (string? id, JsonObject document)>
    where TKey : notnull
    where TRecord : notnull
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
    public ElasticsearchDataModelMapper(
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
    public (string? id, JsonObject document) MapFromDataToStorageModel(TRecord dataModel, Embedding<float>?[]? generatedEmbeddings)
    {
        Verify.NotNull(dataModel);

        // Serialize the whole record to JsonObject.

        var document = SerializeSource(dataModel, _elasticsearchClientSettings);

        // Extract key property and remove it from document.

        var keyProperty = _model.KeyProperty;
        var id = (TKey?)keyProperty.GetValueAsObject(dataModel);
        document.Remove(keyProperty.StorageName);

        // Update vector properties with generated embeddings.

        if (generatedEmbeddings is not null)
        {
            for (var i = 0; i < _model.VectorProperties.Count; ++i)
            {
                if (generatedEmbeddings[i] is not {} embedding)
                {
                    continue;
                }

                var vectorProperty = _model.VectorProperties[i];
                document[vectorProperty.StorageName] = JsonValue.Create(embedding.Vector);
            }
        }

        return ((id is null) ? null : ElasticsearchVectorStoreRecordFieldMapping.KeyToElasticsearchId(id), document);
    }

    /// <inheritdoc />
    public TRecord MapFromStorageToDataModel((string? id, JsonObject document) storageModel,
        StorageToDataModelMapperOptions options)
    {
        // Add key property to document.

        var keyProperty = _model.KeyProperty;
        storageModel.document.Add(keyProperty.StorageName, JsonValue.Create(ElasticsearchVectorStoreRecordFieldMapping.ElasticsearchIdToKey<TKey>(storageModel.id!)));

        // Serialize the whole model into the user-defined record type.

        return _elasticsearchClientSettings.SourceSerializer.Deserialize<TRecord>(storageModel.document)!;
    }

    private static JsonObject SerializeSource<T>(T obj, IElasticsearchClientSettings settings)
    {
        if (settings.SourceSerializer.TryGetJsonSerializerOptions(out var options))
        {
            return (JsonObject)JsonSerializer.SerializeToNode(obj, options)!;
        }

        using var stream = settings.MemoryStreamFactory.Create();

        settings.SourceSerializer.Serialize(obj, stream);
        stream.Position = 0;

        return settings.SourceSerializer.Deserialize<JsonObject>(stream);
    }
}
