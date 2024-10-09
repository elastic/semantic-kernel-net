using System;

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
    public static string GetIndexKind(VectorStoreRecordVectorProperty vectorProperty)
    {
        const string hnswIndexKind = "hnsw";
        const string int8HnswIndexKind = "int8_hnsw";
        const string int4HnswIndexKind = "int4_hnsw";
        const string flatIndexKind = "flat";
        const string int8FlatIndexKind = "int8_flat";
        const string int4FlatIndexKind = "int4_flat";

        if (vectorProperty.DistanceFunction is null)
        {
            return int8HnswIndexKind;
        }

        return vectorProperty.DistanceFunction switch
        {
            IndexKind.Hnsw => hnswIndexKind,
            int8HnswIndexKind => int8HnswIndexKind,
            int4HnswIndexKind => int4HnswIndexKind,
            IndexKind.Flat => flatIndexKind,
            int8FlatIndexKind => int8FlatIndexKind,
            int4FlatIndexKind => int4FlatIndexKind,
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
    public static string GetSimilarityFunction(VectorStoreRecordVectorProperty vectorProperty)
    {
        const string cosineSimilarity = "cosine";
        const string dotProductSimilarity = "dot_product";
        const string euclideanSimilarity = "l2_norm";
        const string maxInnerProductSimilarity = "max_inner_product";

        if (vectorProperty.DistanceFunction is null)
        {
            return cosineSimilarity;
        }

        return vectorProperty.DistanceFunction switch
        {
            DistanceFunction.CosineSimilarity => cosineSimilarity,
            DistanceFunction.DotProductSimilarity => dotProductSimilarity,
            DistanceFunction.EuclideanDistance => euclideanSimilarity,
            maxInnerProductSimilarity => maxInnerProductSimilarity,
            _ => throw new InvalidOperationException(
                $"Distance function '{vectorProperty.DistanceFunction}' for {nameof(VectorStoreRecordVectorProperty)} '{vectorProperty.DataModelPropertyName}' is not supported by the Elasticsearch VectorStore.")
        };
    }
}
