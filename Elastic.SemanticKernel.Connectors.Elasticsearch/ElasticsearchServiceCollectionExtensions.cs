// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Elastic.Clients.Elasticsearch;

using Microsoft.Extensions.DependencyInjection;
using Elastic.SemanticKernel.Connectors.Elasticsearch;

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
    public static IServiceCollection AddElasticsearchVectorStore(this IServiceCollection services, ElasticsearchVectorStoreOptions? options = default, string? serviceId = default)
    {
        // If we are not constructing the ElasticsearchClient, add the IVectorStore as transient, since we
        // cannot make assumptions about how ElasticsearchClient is being managed.
        services.AddKeyedTransient<IVectorStore>(
            serviceId,
            (sp, obj) =>
            {
                var elasticsearchClient = sp.GetRequiredService<ElasticsearchClient>();
                var selectedOptions = options ?? sp.GetService<ElasticsearchVectorStoreOptions>();

                return new ElasticsearchVectorStore(
                    elasticsearchClient,
                    selectedOptions);
            });

        return services;
    }

    /// <summary>
    /// Register an Elasticsearch <see cref="IVectorStore"/> with the specified service ID and where <see cref="ElasticsearchClient"/> is constructed using the provided settings.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register the <see cref="IVectorStore"/> on.</param>
    /// <param name="settings">The Elasticsearch client settings.</param>
    /// <param name="options">Optional options to further configure the <see cref="IVectorStore"/>.</param>
    /// <param name="serviceId">An optional service id to use as the service key.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddElasticsearchVectorStore(this IServiceCollection services, IElasticsearchClientSettings settings, ElasticsearchVectorStoreOptions? options = default, string? serviceId = default)
    {
        services.AddKeyedSingleton<IVectorStore>(
            serviceId,
            (sp, obj) =>
            {
                var elasticsearchClient = new ElasticsearchClient(settings);
                var selectedOptions = options ?? sp.GetService<ElasticsearchVectorStoreOptions>();

                return new ElasticsearchVectorStore(
                    elasticsearchClient,
                    selectedOptions);
            });

        return services;
    }
}
