// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.AgentBuilder.Agents;
using Elastic.Clients.AgentBuilder.Tools;

namespace Elastic.Clients.AgentBuilder;

/// <summary>
/// Defines the tools and agents to bootstrap.
/// </summary>
public record BootstrapDefinition
{
	/// <summary> Tool definitions to create or update. Each must have a unique <c>Id</c>. </summary>
	public IReadOnlyList<CreateEsqlToolRequest>? EsqlTools { get; init; }

	/// <summary> Index search tool definitions to create or update. </summary>
	public IReadOnlyList<CreateIndexSearchToolRequest>? IndexSearchTools { get; init; }

	/// <summary> Agent definitions to create or update. Each must have a unique <c>Id</c>. </summary>
	public IReadOnlyList<CreateAgentRequest>? Agents { get; init; }
}

/// <summary>
/// Idempotent bootstrapper for Agent Builder resources.
/// Computes a hash of each definition and stores it in <c>tags</c> (tools) or <c>labels</c> (agents).
/// On subsequent runs, resources are only updated if their hash has changed.
/// </summary>
public class AgentBuilderBootstrapper
{
	private const string HashPrefix = "_hash:";

	private readonly AgentBuilderClient _client;

	public AgentBuilderBootstrapper(AgentBuilderClient client) =>
		_client = client ?? throw new ArgumentNullException(nameof(client));

	/// <summary>
	/// Bootstrap all tools and agents defined in <paramref name="definition"/>.
	/// </summary>
	public async Task<bool> BootstrapAsync(
		BootstrapMethod method, BootstrapDefinition definition, CancellationToken ct = default)
	{
		if (method == BootstrapMethod.None)
			return true;

		try
		{
			if (definition.EsqlTools != null)
			{
				foreach (var tool in definition.EsqlTools)
					await BootstrapEsqlToolAsync(tool, ct).ConfigureAwait(false);
			}

			if (definition.IndexSearchTools != null)
			{
				foreach (var tool in definition.IndexSearchTools)
					await BootstrapIndexSearchToolAsync(tool, ct).ConfigureAwait(false);
			}

			if (definition.Agents != null)
			{
				foreach (var agent in definition.Agents)
					await BootstrapAgentAsync(agent, ct).ConfigureAwait(false);
			}

			return true;
		}
		catch when (method == BootstrapMethod.Silent)
		{
			return false;
		}
	}

	private async Task BootstrapEsqlToolAsync(CreateEsqlToolRequest request, CancellationToken ct)
	{
		var hash = ComputeHash(request, AgentBuilderSerializationContext.Default.CreateEsqlToolRequest);
		var existing = await TryGetToolAsync(request.Id, ct).ConfigureAwait(false);

		if (existing != null && HasMatchingHash(existing.Tags, hash))
			return;

		if (existing != null)
		{
			await _client.UpdateToolAsync(request.Id, new UpdateEsqlToolRequest
			{
				Description = request.Description,
				Tags = MergeHashTag(request.Tags, hash),
				Configuration = request.Configuration
			}, ct).ConfigureAwait(false);
		}
		else
		{
			var tagged = request with { Tags = MergeHashTag(request.Tags, hash) };
			await _client.CreateToolAsync(tagged, ct).ConfigureAwait(false);
		}
	}

	private async Task BootstrapIndexSearchToolAsync(CreateIndexSearchToolRequest request, CancellationToken ct)
	{
		var hash = ComputeHash(request, AgentBuilderSerializationContext.Default.CreateIndexSearchToolRequest);
		var existing = await TryGetToolAsync(request.Id, ct).ConfigureAwait(false);

		if (existing != null && HasMatchingHash(existing.Tags, hash))
			return;

		if (existing != null)
		{
			await _client.UpdateToolAsync(request.Id, new UpdateIndexSearchToolRequest
			{
				Description = request.Description,
				Tags = MergeHashTag(request.Tags, hash),
				Configuration = request.Configuration
			}, ct).ConfigureAwait(false);
		}
		else
		{
			var tagged = request with { Tags = MergeHashTag(request.Tags, hash) };
			await _client.CreateToolAsync(tagged, ct).ConfigureAwait(false);
		}
	}

	private async Task BootstrapAgentAsync(CreateAgentRequest request, CancellationToken ct)
	{
		var hash = ComputeHash(request, AgentBuilderSerializationContext.Default.CreateAgentRequest);
		AgentBuilderAgent? existing;
		try
		{
			existing = await _client.GetAgentAsync(request.Id, ct).ConfigureAwait(false);
		}
		catch (AgentBuilderException ex) when (ex.ApiCallDetails.HttpStatusCode == 404)
		{
			existing = null;
		}

		if (existing != null && HasMatchingHash(existing.Labels, hash))
			return;

		if (existing != null)
		{
			await _client.UpdateAgentAsync(request.Id, new UpdateAgentRequest
			{
				Name = request.Name,
				Description = request.Description,
				Labels = MergeHashTag(request.Labels, hash),
				AvatarColor = request.AvatarColor,
				AvatarSymbol = request.AvatarSymbol,
				Configuration = request.Configuration
			}, ct).ConfigureAwait(false);
		}
		else
		{
			var labeled = request with { Labels = MergeHashTag(request.Labels, hash) };
			await _client.CreateAgentAsync(labeled, ct).ConfigureAwait(false);
		}
	}

	private async Task<AgentBuilderTool?> TryGetToolAsync(string toolId, CancellationToken ct)
	{
		try
		{
			return await _client.GetToolAsync(toolId, ct).ConfigureAwait(false);
		}
		catch (AgentBuilderException ex) when (ex.ApiCallDetails.HttpStatusCode == 404)
		{
			return null;
		}
	}

	private static bool HasMatchingHash(IReadOnlyList<string>? tagsOrLabels, string hash)
	{
		if (tagsOrLabels == null)
			return false;
		return tagsOrLabels.Any(t => t == HashPrefix + hash);
	}

	private static IReadOnlyList<string> MergeHashTag(IReadOnlyList<string>? existing, string hash)
	{
		var result = new List<string>();
		if (existing != null)
		{
			foreach (var tag in existing)
			{
				if (!tag.StartsWith(HashPrefix, StringComparison.Ordinal))
					result.Add(tag);
			}
		}
		result.Add(HashPrefix + hash);
		return result;
	}

	public static string ComputeHash<T>(T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
	{
		var json = JsonSerializer.Serialize(value, typeInfo);
		var input = Encoding.UTF8.GetBytes(json);
#if NET8_0_OR_GREATER
		var bytes = SHA256.HashData(input);
#else
		using var sha = SHA256.Create();
		var bytes = sha.ComputeHash(input);
#endif
		return Convert.ToBase64String(bytes, 0, 12);
	}
}
