// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;

using Elastic.Clients.Elasticsearch;
using Elastic.SemanticKernel.Connectors.Elasticsearch;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to register <see cref="ElasticsearchVectorStore"/> and <see cref="ElasticsearchCollection{TKey, TRecord}"/>
/// instances on an <see cref="IServiceCollection"/>.
/// </summary>
public static class ElasticsearchServiceCollectionExtensions
{
    private const string DynamicCodeMessage = "This method is incompatible with NativeAOT, consult the documentation for adding collections in a way that's compatible with NativeAOT.";
    private const string UnreferencedCodeMessage = "This method is incompatible with trimming, consult the documentation for adding collections in a way that's compatible with NativeAOT.";

    /// <summary>
    /// Registers a <see cref="ElasticsearchVectorStore"/> as <see cref="VectorStore"/>
    /// with <see cref="ElasticsearchClient"/> returned by <paramref name="clientProvider"/>
    /// or retrieved from the dependency injection container if <paramref name="clientProvider"/> was not provided.
    /// </summary>
    /// <inheritdoc cref="AddKeyedElasticsearchVectorStore(IServiceCollection, object?, Func{IServiceProvider, ElasticsearchClient}, Func{IServiceProvider, ElasticsearchVectorStoreOptions}?, ServiceLifetime)"/>
    [RequiresUnreferencedCode(DynamicCodeMessage)]
    [RequiresDynamicCode(UnreferencedCodeMessage)]
    public static IServiceCollection AddElasticsearchVectorStore(
        this IServiceCollection services,
        Func<IServiceProvider, ElasticsearchClient>? clientProvider = default,
        Func<IServiceProvider, ElasticsearchVectorStoreOptions>? optionsProvider = default,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        return AddKeyedElasticsearchVectorStore(services, serviceKey: null, clientProvider, optionsProvider, lifetime);
    }

    /// <summary>
    /// Registers a keyed <see cref="ElasticsearchVectorStore"/> as <see cref="VectorStore"/>
    /// with <see cref="ElasticsearchClient"/> returned by <paramref name="clientProvider"/> or retrieved from the dependency injection
    /// container if <paramref name="clientProvider"/> was not provided.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register the <see cref="ElasticsearchVectorStore"/> on.</param>
    /// <param name="serviceKey">The key with which to associate the vector store.</param>
    /// <param name="clientProvider">The <see cref="ElasticsearchClient"/> provider.</param>
    /// <param name="optionsProvider">Options provider to further configure the <see cref="ElasticsearchVectorStore"/>.</param>
    /// <param name="lifetime">The service lifetime for the store. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>Service collection.</returns>
    [RequiresUnreferencedCode(DynamicCodeMessage)]
    [RequiresDynamicCode(UnreferencedCodeMessage)]
    public static IServiceCollection AddKeyedElasticsearchVectorStore(
        this IServiceCollection services,
        object? serviceKey,
        Func<IServiceProvider, ElasticsearchClient>? clientProvider = default,
        Func<IServiceProvider, ElasticsearchVectorStoreOptions>? optionsProvider = default,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        Verify.NotNull(services);

        services.Add(new ServiceDescriptor(typeof(ElasticsearchVectorStore), serviceKey, (sp, _) =>
        {
            var client = clientProvider is null ? sp.GetRequiredService<ElasticsearchClient>() : clientProvider(sp);
            var options = GetStoreOptions(sp, optionsProvider);

            // The client was restored from the DI container, so we do not own it.
            return new ElasticsearchVectorStore(client, ownsClient: false, options);
        }, lifetime));

        services.Add(new ServiceDescriptor(typeof(VectorStore), serviceKey,
            static (sp, key) => sp.GetRequiredKeyedService<ElasticsearchVectorStore>(key), lifetime));

        return services;
    }

    /// <summary>
    /// Registers a <see cref="ElasticsearchVectorStore"/> as <see cref="VectorStore"/>
    /// with <see cref="ElasticsearchClient"/> created with <paramref name="clientSettings"/>.
    /// </summary>
    /// <inheritdoc cref="AddKeyedElasticsearchVectorStore(IServiceCollection, object?, Func{IServiceProvider, ElasticsearchClient}, Func{IServiceProvider, ElasticsearchVectorStoreOptions}?, ServiceLifetime)"/>
    [RequiresUnreferencedCode(DynamicCodeMessage)]
    [RequiresDynamicCode(UnreferencedCodeMessage)]
    public static IServiceCollection AddElasticsearchVectorStore(
        this IServiceCollection services,
        IElasticsearchClientSettings clientSettings,
        ElasticsearchVectorStoreOptions? options = default,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        return AddKeyedElasticsearchVectorStore(services, serviceKey: null, clientSettings, options, lifetime);
    }

    /// <summary>
    /// Registers a keyed <see cref="ElasticsearchVectorStore"/> as <see cref="VectorStore"/>
    /// with <see cref="ElasticsearchClient"/> created with <paramref name="clientSettings"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register the <see cref="ElasticsearchVectorStore"/> on.</param>
    /// <param name="serviceKey">The key with which to associate the vector store.</param>
    /// <param name="clientSettings">The Elasticsearch client settings.</param>
    /// <param name="options">Options to further configure the <see cref="ElasticsearchVectorStore"/>.</param>
    /// <param name="lifetime">The service lifetime for the store. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>Service collection.</returns>
    [RequiresUnreferencedCode(DynamicCodeMessage)]
    [RequiresDynamicCode(UnreferencedCodeMessage)]
    public static IServiceCollection AddKeyedElasticsearchVectorStore(
        this IServiceCollection services,
        object? serviceKey,
        IElasticsearchClientSettings clientSettings,
        ElasticsearchVectorStoreOptions? options = default,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        Verify.NotNull(clientSettings);

        return AddKeyedElasticsearchVectorStore(services, serviceKey, _ => new ElasticsearchClient(clientSettings), _ => options!, lifetime);
    }

    /// <summary>
    /// Registers a <see cref="ElasticsearchCollection{TKey, TRecord}"/> as <see cref="VectorStoreCollection{TKey, TRecord}"/>
    /// with <see cref="ElasticsearchClient"/> returned by <paramref name="clientProvider"/> or retrieved from the dependency injection container if <paramref name="clientProvider"/> was not provided.
    /// </summary>
    [RequiresUnreferencedCode(DynamicCodeMessage)]
    [RequiresDynamicCode(UnreferencedCodeMessage)]
    public static IServiceCollection AddElasticsearchCollection<TKey, TRecord>(
        this IServiceCollection services,
        string name,
        Func<IServiceProvider, ElasticsearchClient>? clientProvider = default,
        Func<IServiceProvider, ElasticsearchCollectionOptions>? optionsProvider = default,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TKey : notnull
        where TRecord : class
    {
        return AddKeyedElasticsearchCollection<TKey, TRecord>(services, serviceKey: null, name, clientProvider,
            optionsProvider, lifetime);
    }

    /// <summary>
    /// Registers a keyed <see cref="ElasticsearchCollection{TKey, TRecord}"/> as <see cref="VectorStoreCollection{TKey, TRecord}"/>
    /// with <see cref="ElasticsearchClient"/> returned by <paramref name="clientProvider"/> or retrieved from the dependency injection container if <paramref name="clientProvider"/> was not provided.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register the <see cref="ElasticsearchCollection{TKey, TRecord}"/> on.</param>
    /// <param name="serviceKey">The key with which to associate the collection.</param>
    /// <param name="name">The name of the collection.</param>
    /// <param name="clientProvider">The <see cref="ElasticsearchClient"/> provider.</param>
    /// <param name="optionsProvider">Options provider to further configure the <see cref="ElasticsearchCollection{TKey, TRecord}"/>.</param>
    /// <param name="lifetime">The service lifetime for the store. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>Service collection.</returns>
    [RequiresUnreferencedCode(DynamicCodeMessage)]
    [RequiresDynamicCode(UnreferencedCodeMessage)]
    public static IServiceCollection AddKeyedElasticsearchCollection<TKey, TRecord>(
        this IServiceCollection services,
        object? serviceKey,
        string name,
        Func<IServiceProvider, ElasticsearchClient>? clientProvider = default,
        Func<IServiceProvider, ElasticsearchCollectionOptions>? optionsProvider = default,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TKey : notnull
        where TRecord : class
    {
        Verify.NotNull(services);
        Verify.NotNullOrWhiteSpace(name);

        services.Add(new ServiceDescriptor(typeof(ElasticsearchCollection<TKey, TRecord>), serviceKey, (sp, _) =>
        {
            var client = clientProvider is null ? sp.GetRequiredService<ElasticsearchClient>() : clientProvider(sp);
            var options = GetCollectionOptions(sp, optionsProvider);

            // The client was restored from the DI container, so we do not own it.
            return new ElasticsearchCollection<TKey, TRecord>(client, name, ownsClient: false, options);
        }, lifetime));

        services.Add(new ServiceDescriptor(typeof(VectorStoreCollection<TKey, TRecord>), serviceKey,
            static (sp, key) => sp.GetRequiredKeyedService<ElasticsearchCollection<TKey, TRecord>>(key), lifetime));

        services.Add(new ServiceDescriptor(typeof(IVectorSearchable<TRecord>), serviceKey,
            static (sp, key) => sp.GetRequiredKeyedService<ElasticsearchCollection<TKey, TRecord>>(key), lifetime));

        services.Add(new ServiceDescriptor(typeof(IKeywordHybridSearchable<TRecord>), serviceKey,
            static (sp, key) => sp.GetRequiredKeyedService<ElasticsearchCollection<TKey, TRecord>>(key), lifetime));

        return services;
    }

    /// <summary>
    /// Registers a <see cref="ElasticsearchCollection{TKey, TRecord}"/> as <see cref="VectorStoreCollection{TKey, TRecord}"/>
    /// with <see cref="ElasticsearchClient"/> created with <paramref name="clientSettings"/>.
    /// </summary>
    /// <inheritdoc cref="AddKeyedElasticsearchCollection{TKey, TRecord}(IServiceCollection, object?, string, IElasticsearchClientSettings, ElasticsearchCollectionOptions?, ServiceLifetime)"/>
    [RequiresUnreferencedCode(DynamicCodeMessage)]
    [RequiresDynamicCode(UnreferencedCodeMessage)]
    public static IServiceCollection AddElasticsearchCollection<TKey, TRecord>(
        this IServiceCollection services,
        string name,
        IElasticsearchClientSettings clientSettings,
        ElasticsearchCollectionOptions? options = default,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TKey : notnull
        where TRecord : class
    {
        return AddKeyedElasticsearchCollection<TKey, TRecord>(services, serviceKey: null, name, clientSettings, options, lifetime);
    }

    /// <summary>
    /// Registers a keyed <see cref="ElasticsearchCollection{TKey, TRecord}"/> as <see cref="VectorStoreCollection{TKey, TRecord}"/>
    /// with <see cref="ElasticsearchClient"/> created with <paramref name="clientSettings"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register the <see cref="ElasticsearchCollection{TKey, TRecord}"/> on.</param>
    /// <param name="serviceKey">The key with which to associate the collection.</param>
    /// <param name="name">The name of the collection.</param>
    /// <param name="clientSettings">The Elasticsearch client settings.</param>
    /// <param name="options">Options to further configure the <see cref="ElasticsearchCollection{TKey, TRecord}"/>.</param>
    /// <param name="lifetime">The service lifetime for the store. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>Service collection.</returns>
    [RequiresUnreferencedCode(DynamicCodeMessage)]
    [RequiresDynamicCode(UnreferencedCodeMessage)]
    public static IServiceCollection AddKeyedElasticsearchCollection<TKey, TRecord>(
        this IServiceCollection services,
        object? serviceKey,
        string name,
        IElasticsearchClientSettings clientSettings,
        ElasticsearchCollectionOptions? options = default,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TKey : notnull
        where TRecord : class
    {
        Verify.NotNull(clientSettings);

        return AddKeyedElasticsearchCollection<TKey, TRecord>(services, serviceKey, name, _ => new ElasticsearchClient(clientSettings), _ => options!, lifetime);
    }

    private static ElasticsearchVectorStoreOptions? GetStoreOptions(IServiceProvider sp, Func<IServiceProvider, ElasticsearchVectorStoreOptions?>? optionsProvider)
    {
        var options = optionsProvider?.Invoke(sp);
        if (options?.EmbeddingGenerator is not null)
        {
            return options; // The user has provided everything, there is nothing to change.
        }

        var embeddingGenerator = sp.GetService<IEmbeddingGenerator>();
        return embeddingGenerator is null
            ? options // There is nothing to change.
            : new(options) { EmbeddingGenerator = embeddingGenerator }; // Create a brand new copy in order to avoid modifying the original options.
    }

    private static ElasticsearchCollectionOptions? GetCollectionOptions(IServiceProvider sp, Func<IServiceProvider, ElasticsearchCollectionOptions?>? optionsProvider)
    {
        var options = optionsProvider?.Invoke(sp);
        if (options?.EmbeddingGenerator is not null)
        {
            return options; // The user has provided everything, there is nothing to change.
        }

        var embeddingGenerator = sp.GetService<IEmbeddingGenerator>();
        return embeddingGenerator is null
            ? options // There is nothing to change.
            : new(options) { EmbeddingGenerator = embeddingGenerator }; // Create a brand new copy in order to avoid modifying the original options.
    }
}
