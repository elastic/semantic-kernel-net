using System;

using Elastic.Clients.Elasticsearch.Mapping;

using Microsoft.SemanticKernel.Data;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
///     Contains mapping helpers to use when creating an Elasticsearch vector collection.
/// </summary>
internal static class ElasticsearchVectorStoreCollectionCreateMapping
{
    /// <summary>
    ///     Get the configured Elasticsearch index kind from the given <paramref name="vectorProperty" />.
    ///     If none is configured, the default is <c>int8_hnsw</c>.
    /// </summary>
    /// <param name="vectorProperty">The vector property definition.</param>
    /// <returns>The chosen Elasticsearch index kind.</returns>
    /// <exception cref="InvalidOperationException">Thrown if an index kind is chosen that isn't supported by Elasticsearch.</exception>
    public static DenseVectorIndexOptionsType GetIndexKind(VectorStoreRecordVectorProperty vectorProperty)
    {
        const string int8HnswIndexKind = "int8_hnsw";
        const string int4HnswIndexKind = "int4_hnsw";
        const string int8FlatIndexKind = "int8_flat";
        const string int4FlatIndexKind = "int4_flat";

        if (vectorProperty.DistanceFunction is null)
        {
            return DenseVectorIndexOptionsType.Int8Hnsw;
        }

        return vectorProperty.DistanceFunction switch
        {
            IndexKind.Hnsw => DenseVectorIndexOptionsType.Hnsw,
            int8HnswIndexKind => DenseVectorIndexOptionsType.Int8Hnsw,
            int4HnswIndexKind => DenseVectorIndexOptionsType.Int4Hnsw,
            IndexKind.Flat => DenseVectorIndexOptionsType.Flat,
            int8FlatIndexKind => DenseVectorIndexOptionsType.Int8Flat,
            int4FlatIndexKind => DenseVectorIndexOptionsType.Int4Flat,
            _ => throw new InvalidOperationException(
                $"Index kind '{vectorProperty.IndexKind}' for {nameof(VectorStoreRecordVectorProperty)} '{vectorProperty.DataModelPropertyName}' is not supported by the Elasticsearch VectorStore.")
        };
    }

    /// <summary>
    ///     Get the configured Elasticsearch distance function from the given <paramref name="vectorProperty" />.
    ///     If none is configured, the default is <c>cosine</c>.
    /// </summary>
    /// <param name="vectorProperty">The vector property definition.</param>
    /// <returns>The chosen Elasticsearch distance function.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if a distance function is chosen that isn't supported by
    ///     Elasticsearch.
    /// </exception>
    public static DenseVectorSimilarity GetSimilarityFunction(VectorStoreRecordVectorProperty vectorProperty)
    {
        const string maxInnerProductSimilarity = "max_inner_product";

        if (vectorProperty.DistanceFunction is null)
        {
            return DenseVectorSimilarity.Cosine;
        }

        return vectorProperty.DistanceFunction switch
        {
            DistanceFunction.CosineSimilarity => DenseVectorSimilarity.Cosine,
            DistanceFunction.DotProductSimilarity => DenseVectorSimilarity.DotProduct,
            DistanceFunction.EuclideanDistance => DenseVectorSimilarity.L2Norm,
            maxInnerProductSimilarity => DenseVectorSimilarity.MaxInnerProduct,
            _ => throw new InvalidOperationException(
                $"Distance function '{vectorProperty.DistanceFunction}' for {nameof(VectorStoreRecordVectorProperty)} '{vectorProperty.DataModelPropertyName}' is not supported by the Elasticsearch VectorStore.")
        };
    }
}
