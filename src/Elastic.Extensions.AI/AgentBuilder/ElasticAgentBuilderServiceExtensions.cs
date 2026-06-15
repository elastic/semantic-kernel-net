// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Clients.AgentBuilder;
using Elastic.Transport;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Elastic.Extensions.AI;

/// <summary>
/// Extension methods for registering Elastic Agent Builder services with <see cref="IServiceCollection"/>.
/// </summary>
public static class ElasticAgentBuilderServiceExtensions
{
	/// <summary>
	/// Registers an <see cref="AgentBuilderClient"/> and an <see cref="IChatClient"/> backed
	/// by the specified Kibana agent, using an <see cref="AgentTransportConfiguration"/>.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="configuration">
	/// Pre-configured <see cref="AgentTransportConfiguration"/>. Set
	/// <see cref="AgentTransportConfiguration.Space"/> to target a specific Kibana space.
	/// </param>
	/// <param name="agentId">The Agent Builder agent ID to use for chat.</param>
	/// <param name="connectorId">Optional LLM connector ID.</param>
	public static IServiceCollection AddElasticAgentBuilder(
		this IServiceCollection services,
		AgentTransportConfiguration configuration,
		string agentId,
		string? connectorId = null)
	{
		var client = new AgentBuilderClient(configuration);

		services.AddSingleton(client);
		services.AddSingleton<IChatClient>(new ElasticAgentChatClient(client, agentId, connectorId));

		return services;
	}

	/// <summary>
	/// Registers an <see cref="AgentBuilderClient"/> and an <see cref="IChatClient"/> using
	/// a pre-configured <see cref="ITransport"/>. You are responsible for setting the
	/// <c>kbn-xsrf</c> header.
	/// </summary>
	public static IServiceCollection AddElasticAgentBuilder(
		this IServiceCollection services,
		ITransport transport,
		string agentId,
		string? space = null,
		string? connectorId = null)
	{
		var client = new AgentBuilderClient(transport, space);

		services.AddSingleton(client);
		services.AddSingleton<IChatClient>(new ElasticAgentChatClient(client, agentId, connectorId));

		return services;
	}
}
