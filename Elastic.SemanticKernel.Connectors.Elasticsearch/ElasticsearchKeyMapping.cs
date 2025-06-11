using System;
using System.Globalization;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
/// Contains helper methods for mapping keys to and from the format required by Elasticsearch.
/// </summary>
internal static class ElasticsearchKeyMapping
{
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
        where TKey : notnull
    {
        return key switch
        {
            string => (string)(object)key,
            long => key.ToString()!,
            Guid => key.ToString()!,
            _ => throw new NotSupportedException($"The provided key type '{key.GetType().Name}' is not supported by Elasticsearch.")
        };
    }
}
