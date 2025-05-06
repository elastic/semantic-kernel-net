// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Elastic.Clients.Elasticsearch;

using Microsoft.Extensions.DependencyInjection;
using Elastic.SemanticKernel.Connectors.Elasticsearch;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace Microsoft.SemanticKernel;

/// <summary>
/// Extension methods to register Elasticsearch <see cref="IVectorStore"/> instances on an <see cref="IServiceCollection"/>.
/// </summary>
public static class ElasticsearchServiceCollectionExtensions
{
    /// <summary>
    /// Register an Elasticsearch <see cref="IVectorStore"/> with the specified service ID and where <see cref="ElasticsearchClient"/> is retrieved from the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register the <see cref="IVectorStore"/> on.</param>
    /// <param name="options">Optional options to further configure the <see cref="IVectorStore"/>.</param>
    /// <param name="serviceId">An optional service id to use as the service key.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddElasticsearchVectorStore(
        this IServiceCollection services,
        ElasticsearchVectorStoreOptions? options = default,
        string? serviceId = default)
    {
        // If we are not constructing the ElasticsearchClient, add the IVectorStore as transient, since we
        // cannot make assumptions about how ElasticsearchClient is being managed.
        services.AddKeyedTransient<IVectorStore>(
            serviceId,
            (sp, _) =>
            {
                var elasticsearchClient = sp.GetRequiredService<ElasticsearchClient>();
                options ??= sp.GetService<ElasticsearchVectorStoreOptions>() ?? new()
                {
                    EmbeddingGenerator = sp.GetService<IEmbeddingGenerator>()
                };

                return new ElasticsearchVectorStore(elasticsearchClient, options);
            });

        return services;
    }

    /// <summary>
    /// Register an Elasticsearch <see cref="IVectorStore"/> with the specified service ID and where <see cref="ElasticsearchClient"/> is constructed using the provided client settings.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register the <see cref="IVectorStore"/> on.</param>
    /// <param name="clientSettings">The Elasticsearch client settings.</param>
    /// <param name="options">Optional options to further configure the <see cref="IVectorStore"/>.</param>
    /// <param name="serviceId">An optional service id to use as the service key.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddElasticsearchVectorStore(
        this IServiceCollection services,
        IElasticsearchClientSettings clientSettings,
        ElasticsearchVectorStoreOptions? options = default,
        string? serviceId = default)
    {
        services.AddKeyedSingleton<IVectorStore>(
            serviceId,
            (sp, _) =>
            {
                var elasticsearchClient = new ElasticsearchClient(clientSettings);
                options ??= sp.GetService<ElasticsearchVectorStoreOptions>() ?? new()
                {
                    EmbeddingGenerator = sp.GetService<IEmbeddingGenerator>()
                };

                return new ElasticsearchVectorStore(elasticsearchClient, options);
            });

        return services;
    }

    /// <summary>
    /// Register an Elasticsearch <see cref="IVectorStoreRecordCollection{TKey, TRecord}"/> and <see cref="IVectorSearch{TRecord}"/> with the specified service ID
    /// and where the <see cref="ElasticsearchClient"/> is retrieved from the dependency injection container.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TRecord">The type of the record.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to register the <see cref="IVectorStoreRecordCollection{TKey, TRecord}"/> on.</param>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="options">Optional options to further configure the <see cref="IVectorStoreRecordCollection{TKey, TRecord}"/>.</param>
    /// <param name="serviceId">An optional service id to use as the service key.</param>
    /// <returns>Service collection.</returns>
    public static IServiceCollection AddElasticsearchVectorStoreRecordCollection<TKey, TRecord>(
        this IServiceCollection services,
        string collectionName,
        ElasticsearchVectorStoreRecordCollectionOptions<TRecord>? options = default,
        string? serviceId = default)
        where TKey : notnull
        where TRecord : notnull
    {
        services.AddKeyedTransient<IVectorStoreRecordCollection<TKey, TRecord>>(
            serviceId,
            (sp, _) =>
            {
                var elasticsearchClient = sp.GetRequiredService<ElasticsearchClient>();
                options ??= sp.GetService<ElasticsearchVectorStoreRecordCollectionOptions<TRecord>>() ?? new()
                {
                    EmbeddingGenerator = sp.GetService<IEmbeddingGenerator>()
                };

                return (new ElasticsearchVectorStoreRecordCollection<TKey, TRecord>(elasticsearchClient, collectionName, options) as IVectorStoreRecordCollection<TKey, TRecord>)!;
            });

        AddVectorizedSearch<TKey, TRecord>(services, serviceId);

        return services;
    }

    /// <summary>
    /// Register an Elasticsearch <see cref="IVectorStoreRecordCollection{TKey, TRecord}"/> and <see cref="IVectorSearch{TRecord}"/> with the specified service ID
    /// and where the <see cref="ElasticsearchClient"/> is constructed using the provided client settings.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TRecord">The type of the record.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to register the <see cref="IVectorStoreRecordCollection{TKey, TRecord}"/> on.</param>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="clientSettings">The Elasticsearch client settings.</param>
    /// <param name="options">Optional options to further configure the <see cref="IVectorStoreRecordCollection{TKey, TRecord}"/>.</param>
    /// <param name="serviceId">An optional service id to use as the service key.</param>
    /// <returns>Service collection.</returns>
    public static IServiceCollection AddElasticsearchVectorStoreRecordCollection<TKey, TRecord>(
        this IServiceCollection services,
        string collectionName,
        IElasticsearchClientSettings clientSettings,
        ElasticsearchVectorStoreRecordCollectionOptions<TRecord>? options = default,
        string? serviceId = default)
        where TKey : notnull
        where TRecord : notnull
    {
        services.AddKeyedSingleton<IVectorStoreRecordCollection<TKey, TRecord>>(
            serviceId,
            (sp, _) =>
            {
                var elasticsearchClient = new ElasticsearchClient(clientSettings);
                options ??= sp.GetService<ElasticsearchVectorStoreRecordCollectionOptions<TRecord>>() ?? new()
                {
                    EmbeddingGenerator = sp.GetService<IEmbeddingGenerator>()
                };

                return (new ElasticsearchVectorStoreRecordCollection<TKey, TRecord>(elasticsearchClient, collectionName, options) as IVectorStoreRecordCollection<TKey, TRecord>)!;
            });

        AddVectorizedSearch<TKey, TRecord>(services, serviceId);

        return services;
    }

    /// <summary>
    /// Also register the <see cref="IVectorStoreRecordCollection{TKey, TRecord}"/> with the given <paramref name="serviceId"/> as a <see cref="IVectorSearch{TRecord}"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TRecord">The type of the data model that the collection should contain.</typeparam>
    /// <param name="services">The service collection to register on.</param>
    /// <param name="serviceId">The service id that the registrations should use.</param>
    private static void AddVectorizedSearch<TKey, TRecord>(IServiceCollection services, string? serviceId)
        where TKey : notnull
        where TRecord : notnull
    {
        services.AddKeyedTransient<IVectorSearch<TRecord>>(
            serviceId,
            (sp, _) =>
            {
                return sp.GetRequiredKeyedService<IVectorStoreRecordCollection<TKey, TRecord>>(serviceId);
            });
    }
}
