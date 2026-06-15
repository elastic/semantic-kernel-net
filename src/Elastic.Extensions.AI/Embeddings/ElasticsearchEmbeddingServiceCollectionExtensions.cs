// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

using Elastic.Clients.Elasticsearch;
using Elastic.Extensions.AI;

using Microsoft.Extensions.AI;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to register <see cref="ElasticsearchEmbeddingGenerator{TEmbedding}"/>
/// instances on an <see cref="IServiceCollection"/>.
/// </summary>
public static class ElasticsearchEmbeddingServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="ElasticsearchEmbeddingGenerator{TEmbedding}"/> as <see cref="IEmbeddingGenerator{String, TEmbedding}"/>
    /// with <see cref="ElasticsearchClient"/> returned by <paramref name="clientProvider"/>
    /// or retrieved from the dependency injection container if <paramref name="clientProvider"/> was not provided.
    /// </summary>
    /// <inheritdoc cref="AddKeyedElasticsearchEmbeddingGenerator{TEmbedding}(IServiceCollection, object?, Func{IServiceProvider, ElasticsearchClient}?, Func{IServiceProvider, ElasticsearchEmbeddingGeneratorOptions}, ServiceLifetime)"/>
    public static IServiceCollection AddElasticsearchEmbeddingGenerator<TEmbedding>(
        this IServiceCollection services,
        Func<IServiceProvider, ElasticsearchClient>? clientProvider,
        Func<IServiceProvider, ElasticsearchEmbeddingGeneratorOptions> optionsProvider,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TEmbedding : Embedding
    {
        return AddKeyedElasticsearchEmbeddingGenerator<TEmbedding>(
            services,
            serviceKey: null,
            clientProvider,
            optionsProvider,
            lifetime);
    }

    /// <summary>
    /// Registers a keyed <see cref="ElasticsearchEmbeddingGenerator{TEmbedding}"/> as <see cref="IEmbeddingGenerator{String, TEmbedding}"/>
    /// with <see cref="ElasticsearchClient"/> returned by <paramref name="clientProvider"/>
    /// or retrieved from the dependency injection container if <paramref name="clientProvider"/> was not provided.
    /// </summary>
    /// <typeparam name="TEmbedding">The embedding type. Must be <see cref="Embedding{Single}"/> or <see cref="Embedding{Byte}"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to register the generator on.</param>
    /// <param name="serviceKey">The key with which to associate the generator.</param>
    /// <param name="clientProvider">The <see cref="ElasticsearchClient"/> provider. If null, the client is resolved from DI.</param>
    /// <param name="optionsProvider">The options provider for configuring the generator.</param>
    /// <param name="lifetime">The service lifetime for the generator. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddKeyedElasticsearchEmbeddingGenerator<TEmbedding>(
        this IServiceCollection services,
        object? serviceKey,
        Func<IServiceProvider, ElasticsearchClient>? clientProvider,
        Func<IServiceProvider, ElasticsearchEmbeddingGeneratorOptions> optionsProvider,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TEmbedding : Embedding
    {
        Verify.NotNull(services);
        Verify.NotNull(optionsProvider);

        services.Add(new ServiceDescriptor(
            typeof(ElasticsearchEmbeddingGenerator<TEmbedding>),
            serviceKey,
            (sp, _) =>
            {
                var client = clientProvider is null
                    ? sp.GetRequiredService<ElasticsearchClient>()
                    : clientProvider(sp);
                var options = optionsProvider(sp);

                // The client was restored from the DI container, so we do not own it.
                return new ElasticsearchEmbeddingGenerator<TEmbedding>(client, ownsClient: false, options);
            },
            lifetime));

        services.Add(new ServiceDescriptor(
            typeof(IEmbeddingGenerator<string, TEmbedding>),
            serviceKey,
            static (sp, key) => sp.GetRequiredKeyedService<ElasticsearchEmbeddingGenerator<TEmbedding>>(key),
            lifetime));

        return services;
    }

    /// <summary>
    /// Registers an <see cref="ElasticsearchEmbeddingGenerator{TEmbedding}"/> as <see cref="IEmbeddingGenerator{String, TEmbedding}"/>
    /// with <see cref="ElasticsearchClient"/> created with <paramref name="clientSettings"/>.
    /// </summary>
    /// <inheritdoc cref="AddKeyedElasticsearchEmbeddingGenerator{TEmbedding}(IServiceCollection, object?, IElasticsearchClientSettings, ElasticsearchEmbeddingGeneratorOptions, ServiceLifetime)"/>
    public static IServiceCollection AddElasticsearchEmbeddingGenerator<TEmbedding>(
        this IServiceCollection services,
        IElasticsearchClientSettings clientSettings,
        ElasticsearchEmbeddingGeneratorOptions options,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TEmbedding : Embedding
    {
        return AddKeyedElasticsearchEmbeddingGenerator<TEmbedding>(
            services,
            serviceKey: null,
            clientSettings,
            options,
            lifetime);
    }

    /// <summary>
    /// Registers a keyed <see cref="ElasticsearchEmbeddingGenerator{TEmbedding}"/> as <see cref="IEmbeddingGenerator{String, TEmbedding}"/>
    /// with <see cref="ElasticsearchClient"/> created with <paramref name="clientSettings"/>.
    /// </summary>
    /// <typeparam name="TEmbedding">The embedding type. Must be <see cref="Embedding{Single}"/> or <see cref="Embedding{Byte}"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to register the generator on.</param>
    /// <param name="serviceKey">The key with which to associate the generator.</param>
    /// <param name="clientSettings">The Elasticsearch client settings.</param>
    /// <param name="options">The options for configuring the generator.</param>
    /// <param name="lifetime">The service lifetime for the generator. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddKeyedElasticsearchEmbeddingGenerator<TEmbedding>(
        this IServiceCollection services,
        object? serviceKey,
        IElasticsearchClientSettings clientSettings,
        ElasticsearchEmbeddingGeneratorOptions options,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TEmbedding : Embedding
    {
        Verify.NotNull(clientSettings);
        Verify.NotNull(options);

        return AddKeyedElasticsearchEmbeddingGenerator<TEmbedding>(
            services,
            serviceKey,
            _ => new ElasticsearchClient(clientSettings),
            _ => options,
            lifetime);
    }
}
