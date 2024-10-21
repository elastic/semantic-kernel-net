using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

using Elastic.Clients.Elasticsearch;
using Elastic.Transport.Extensions;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
///     A mapper that maps between the generic Semantic Kernel data model and the model that the data is stored under,
///     within Elasticsearch.
/// </summary>
internal sealed class ElasticsearchDataModelMapper<TRecord> :
    IVectorStoreRecordMapper<TRecord, (string? id, JsonObject document)>
    where TRecord : class
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
    public ElasticsearchDataModelMapper(
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
    public (string? id, JsonObject document) MapFromDataToStorageModel(TRecord dataModel)
    {
        // Serialize the whole record to JsonObject.

        var document = SerializeSource(dataModel, _elasticsearchClientSettings)!;

        // Extract key property.

        var keyProperty = _propertyToStorageName.Single(x => x.Key is VectorStoreRecordKeyProperty);
        var keyValue = document[keyProperty.Value]!.AsValue();

        var id = keyValue.GetValue<string?>();

        // Remove key property from document.

        document.Remove(keyProperty.Value);

        return (id, document);
    }

    /// <inheritdoc />
    public TRecord MapFromStorageToDataModel((string? id, JsonObject document) storageModel,
        StorageToDataModelMapperOptions options)
    {
        // Add key property to document.

        var keyProperty = _propertyToStorageName.Single(x => x.Key is VectorStoreRecordKeyProperty);
        storageModel.document.Add(keyProperty.Value, storageModel.id);

        // Serialize the whole model into the user-defined record type.

        return _elasticsearchClientSettings.SourceSerializer.Deserialize<TRecord>(storageModel.document)!;
    }

    private static JsonObject? SerializeSource<T>(T? obj, IElasticsearchClientSettings settings)
    {
        if (obj is null)
        {
            return null;
        }

        using var stream = settings.MemoryStreamFactory.Create();

        settings.SourceSerializer.Serialize(obj, stream);
        stream.Position = 0;

        return settings.SourceSerializer.Deserialize<JsonObject>(stream);
    }
}
