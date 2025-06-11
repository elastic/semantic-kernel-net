// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

using Elastic.Clients.Elasticsearch.Mapping;

using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.VectorData.ProviderServices;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
/// Contains mapping helpers to use when creating an Elasticsearch vector collection.
/// </summary>
internal static class ElasticsearchCollectionCreateMapping
{
    /// <summary>
    /// TBC
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public static Properties BuildPropertyMappings(
        CollectionModel model)
    {
        var propertyMappings = new Properties();

        var vectorProperties = model.VectorProperties;
        foreach (var property in vectorProperties)
        {
            propertyMappings.Add(property.StorageName,
                new DenseVectorProperty
                {
                    Dims = property.Dimensions,
                    Index = true,
                    Similarity = GetSimilarityFunction(property),
                    IndexOptions = new DenseVectorIndexOptions
                    {
                        Type = GetIndexKind(property)
                    }
                });
        }

        var dataProperties = model.DataProperties;
        foreach (var property in dataProperties)
        {
            if (property.IsFullTextIndexed)
            {
                propertyMappings.Add(property.StorageName, new TextProperty());
            }
            else if (property.IsIndexed)
            {
                IProperty? mapping = (Nullable.GetUnderlyingType(property.Type) ?? property.Type) switch
                {
                    { } t when t == typeof(string) => new KeywordProperty(),
                    { } t when t == typeof(bool) => new BooleanProperty(),
                    { } t when t == typeof(byte) => new ByteNumberProperty(),
                    { } t when t == typeof(sbyte) => new ByteNumberProperty(),
                    { } t when t == typeof(ushort) => new ShortNumberProperty(),
                    { } t when t == typeof(short) => new ShortNumberProperty(),
                    { } t when t == typeof(int) => new IntegerNumberProperty(),
                    { } t when t == typeof(uint) => new IntegerNumberProperty(),
                    { } t when t == typeof(long) => new LongNumberProperty(),
                    { } t when t == typeof(ulong) => new UnsignedLongNumberProperty(),
                    { } t when t == typeof(double) => new DoubleNumberProperty(),
                    { } t when t == typeof(float) => new FloatNumberProperty(),
                    { } t when t == typeof(DateTime) => new DateProperty(),
                    { } t when t == typeof(DateTimeOffset) => new DateProperty(),
                    _ => new KeywordProperty()
                };

                propertyMappings.Add(property.StorageName, mapping);
            }
            else
            {
                propertyMappings.Add(property.StorageName, new KeywordProperty { Index = false });
            }
        }

        return propertyMappings;
    }

    /// <summary>
    /// Get the configured Elasticsearch index kind from the given <paramref name="vectorProperty" />.
    /// If none is configured, the default is <c>int8_hnsw</c>.
    /// </summary>
    /// <param name="vectorProperty">The vector property definition.</param>
    /// <returns>The chosen Elasticsearch index kind.</returns>
    /// <exception cref="InvalidOperationException">Thrown if an index kind is chosen that isn't supported by Elasticsearch.</exception>
    private static DenseVectorIndexOptionsType GetIndexKind(VectorPropertyModel vectorProperty)
    {
        const string int8HnswIndexKind = "int8_hnsw";
        const string int4HnswIndexKind = "int4_hnsw";
        const string bbqHnswIndexKind  = "bbq_hnsw";
        const string int8FlatIndexKind = "int8_flat";
        const string int4FlatIndexKind = "int4_flat";
        const string bbqFlatIndexKind  = "bbq_flat";

        if (vectorProperty.DistanceFunction is null)
        {
            return DenseVectorIndexOptionsType.Int8Hnsw;
        }

        return vectorProperty.IndexKind switch
        {
            IndexKind.Hnsw => DenseVectorIndexOptionsType.Hnsw,
            int8HnswIndexKind => DenseVectorIndexOptionsType.Int8Hnsw,
            int4HnswIndexKind => DenseVectorIndexOptionsType.Int4Hnsw,
            bbqHnswIndexKind => DenseVectorIndexOptionsType.BbqHnsw,
            IndexKind.Flat => DenseVectorIndexOptionsType.Flat,
            int8FlatIndexKind => DenseVectorIndexOptionsType.Int8Flat,
            int4FlatIndexKind => DenseVectorIndexOptionsType.Int4Flat,
            bbqFlatIndexKind => DenseVectorIndexOptionsType.BbqFlat,
            _ => throw new InvalidOperationException($"Index kind '{vectorProperty.IndexKind}' for '{vectorProperty.ModelName}' is not supported by the Elasticsearch VectorStore.")
        };
    }

    /// <summary>
    /// Get the configured Elasticsearch distance function from the given <paramref name="vectorProperty" />.
    /// If none is configured, the default is <c>cosine</c>.
    /// </summary>
    /// <param name="vectorProperty">The vector property definition.</param>
    /// <returns>The chosen Elasticsearch distance function.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a distance function is chosen that isn't supported by Elasticsearch.
    /// </exception>
    private static DenseVectorSimilarity GetSimilarityFunction(VectorPropertyModel vectorProperty)
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
            _ => throw new InvalidOperationException($"Distance function '{vectorProperty.DistanceFunction}' for '{vectorProperty.ModelName}' is not supported by the Elasticsearch VectorStore.")
        };
    }
}
