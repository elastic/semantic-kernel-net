using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
/// Defines an interface for mapping between a storage model and the consumer record data model.
/// </summary>
/// <typeparam name="TRecordDataModel">The consumer record data model to map to or from.</typeparam>
/// <typeparam name="TStorageModel">The storage model to map to or from.</typeparam>
internal interface IElasticsearchVectorStoreRecordMapper<TRecordDataModel, TStorageModel>
{
    /// <summary>
    /// Maps from the consumer record data model to the storage model.
    /// </summary>
    /// <param name="dataModel">The consumer record data model record to map.</param>
    /// <param name="generatedEmbeddings">A list that contains generated embeddings for each vector property.</param>
    /// <returns>The mapped result.</returns>
    public TStorageModel MapFromDataToStorageModel(TRecordDataModel dataModel, Embedding<float>?[]? generatedEmbeddings);

    /// <summary>
    /// Maps from the storage model to the consumer record data model.
    /// </summary>
    /// <param name="storageModel">The storage data model record to map.</param>
    /// <param name="options">Options to control the mapping behavior.</param>
    /// <returns>The mapped result.</returns>
    public TRecordDataModel MapFromStorageToDataModel(TStorageModel storageModel, StorageToDataModelMapperOptions options);
}
