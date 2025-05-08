using System;
using System.Collections.Generic;
using System.Globalization;

using Microsoft.Extensions.VectorData.ConnectorSupport;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
/// Contains helper methods for mapping fields to and from the format required by the Elasticsearch client.
/// </summary>
internal static class ElasticsearchVectorStoreRecordFieldMapping
{
    public static VectorStoreRecordModelBuildingOptions GetModelBuildOptions()
    {
        return new VectorStoreRecordModelBuildingOptions
        {
            RequiresAtLeastOneVector = false,
            SupportsMultipleKeys = false,
            SupportsMultipleVectors = true,
            SupportedKeyPropertyTypes = [typeof(string), typeof(long), typeof(Guid)],
            SupportedDataPropertyTypes = null,
            SupportedEnumerableDataPropertyElementTypes = null,
            SupportedVectorPropertyTypes = SupportedVectorTypes,
            UsesExternalSerializer = true
        };
    }

    /// <summary>A set of types that vectors on the provided model may have.</summary>
    public static readonly HashSet<Type> SupportedVectorTypes =
    [
        typeof(ReadOnlyMemory<float>),
        typeof(ReadOnlyMemory<float>?),
        typeof(IEnumerable<float>),
        typeof(IReadOnlyCollection<float>),
        typeof(ICollection<float>),
        typeof(IReadOnlyList<float>),
        typeof(IList<float>),
        typeof(float[])
    ];

    public static TKey ElasticsearchIdToKey<TKey>(string id)
    {
        if (typeof(TKey) == typeof(object))
        {
            return (TKey)(object)id;
        }

        if (typeof(TKey) == typeof(string))
        {
            return (TKey)(object)id;
        }

        if (typeof(TKey) == typeof(long))
        {
            return (TKey)(object)long.Parse(id, CultureInfo.InvariantCulture);
        }

        if (typeof(TKey) == typeof(Guid))
        {
            return (TKey)(object)Guid.Parse(id);
        }

        throw new NotSupportedException($"The provided key type '{typeof(TKey).Name}' is not supported by Elasticsearch.");
    }

    public static string KeyToElasticsearchId<TKey>(TKey key)
    {
        return key switch
        {
            string => (string)(object)key,
            long => key.ToString(),
            Guid => key.ToString(),
            _ => throw new NotSupportedException($"The provided key type '{key.GetType().Name}' is not supported by Elasticsearch.")
        };
    }
}
