// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Elastic.Clients.Elasticsearch;

using Elastic.SemanticKernel.Connectors.Elasticsearch;

using Microsoft.Extensions.VectorData;

namespace Microsoft.SemanticKernel;

#pragma warning disable CA1062

/// <summary>
/// Extension methods to register Elasticsearch <see cref="IVectorStore"/> instances on the <see cref="IKernelBuilder"/>.
/// </summary>
public static class ElasticsearchKernelBuilderExtensions
{
    /// <summary>
    /// Register an Elasticsearch <see cref="IVectorStore"/> with the specified service ID and where <see cref="ElasticsearchClient"/> is retrieved from the dependency injection container.
    /// </summary>
    /// <param name="builder">The builder to register the <see cref="IVectorStore"/> on.</param>
    /// <param name="options">Optional options to further configure the <see cref="IVectorStore"/>.</param>
    /// <param name="serviceId">An optional service id to use as the service key.</param>
    /// <returns>The kernel builder.</returns>
    public static IKernelBuilder AddElasticsearchVectorStore(this IKernelBuilder builder, ElasticsearchVectorStoreOptions? options = default, string? serviceId = default)
    {
        builder.Services.AddElasticsearchVectorStore(options, serviceId);
        return builder;
    }

    /// <summary>
    /// Register an Elasticsearch <see cref="IVectorStore"/> with the specified service ID and where <see cref="ElasticsearchClient"/> is constructed using the provided client settings.
    /// </summary>
    /// <param name="builder">The builder to register the <see cref="IVectorStore"/> on.</param>
    /// <param name="clientSettings">The Elasticsearch client settings.</param>
    /// <param name="options">Optional options to further configure the <see cref="IVectorStore"/>.</param>
    /// <param name="serviceId">An optional service id to use as the service key.</param>
    /// <returns>The kernel builder.</returns>
    public static IKernelBuilder AddElasticsearchVectorStore(this IKernelBuilder builder, IElasticsearchClientSettings clientSettings, ElasticsearchVectorStoreOptions? options = default, string? serviceId = default)
    {
        builder.Services.AddElasticsearchVectorStore(clientSettings, options, serviceId);
        return builder;
    }

    /// <summary>
    /// Register an Elasticsearch <see cref="IVectorStoreRecordCollection{TKey, TRecord}"/> and <see cref="IVectorizedSearch{TRecord}"/> with the specified service ID
    /// and where the <see cref="ElasticsearchClient"/> is retrieved from the dependency injection container.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TRecord">The type of the record.</typeparam>
    /// <param name="builder">The builder to register the <see cref="IVectorStoreRecordCollection{TKey, TRecord}"/> on.</param>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="options">Optional options to further configure the <see cref="IVectorStoreRecordCollection{TKey, TRecord}"/>.</param>
    /// <param name="serviceId">An optional service id to use as the service key.</param>
    /// <returns>The kernel builder.</returns>
    public static IKernelBuilder AddElasticsearchVectorStoreRecordCollection<TKey, TRecord>(
        this IKernelBuilder builder,
        string collectionName,
        ElasticsearchVectorStoreRecordCollectionOptions<TRecord>? options = default,
        string? serviceId = default)
        where TKey : notnull
    {
        builder.Services.AddElasticsearchVectorStoreRecordCollection<TKey, TRecord>(collectionName, options, serviceId);
        return builder;
    }

    /// <summary>
    /// Register an Elasticsearch <see cref="IVectorStoreRecordCollection{TKey, TRecord}"/> and <see cref="IVectorizedSearch{TRecord}"/> with the specified service ID
    /// and where the <see cref="ElasticsearchClient"/> is constructed using the provided client settings.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TRecord">The type of the record.</typeparam>
    /// <param name="builder">The builder to register the <see cref="IVectorStoreRecordCollection{TKey, TRecord}"/> on.</param>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="clientSettings">The Elasticsearch client settings.</param>
    /// <param name="options">Optional options to further configure the <see cref="IVectorStoreRecordCollection{TKey, TRecord}"/>.</param>
    /// <param name="serviceId">An optional service id to use as the service key.</param>
    /// <returns>The kernel builder.</returns>
    public static IKernelBuilder AddElasticsearchVectorStoreRecordCollection<TKey, TRecord>(
        this IKernelBuilder builder,
        string collectionName,
        IElasticsearchClientSettings clientSettings,
        ElasticsearchVectorStoreRecordCollectionOptions<TRecord>? options = default,
        string? serviceId = default)
        where TKey : notnull
    {
        builder.Services.AddElasticsearchVectorStoreRecordCollection<TKey, TRecord>(collectionName, clientSettings, options, serviceId);
        return builder;
    }
}

#pragma warning restore CA1062
