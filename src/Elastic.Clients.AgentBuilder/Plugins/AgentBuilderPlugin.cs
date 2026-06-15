// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Elastic.Transport;

namespace Elastic.Clients.AgentBuilder.Plugins;

/// <summary>
/// Represents an installed plugin as returned by the Agent Builder API.
/// </summary>
public class AgentBuilderPlugin : TransportResponse
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = default!;

	[JsonPropertyName("name")]
	public string? Name { get; set; }

	[JsonPropertyName("description")]
	public string? Description { get; set; }

	[JsonPropertyName("url")]
	public string? Url { get; set; }

	[JsonPropertyName("managed_assets")]
	public PluginManagedAssets? ManagedAssets { get; set; }
}

/// <summary>
/// Assets managed by a plugin (e.g. skills, tools).
/// </summary>
public record PluginManagedAssets
{
	[JsonPropertyName("skills")]
	public IReadOnlyList<string>? Skills { get; init; }

	[JsonPropertyName("tools")]
	public IReadOnlyList<string>? Tools { get; init; }
}

/// <summary>
/// Response wrapper for listing plugins.
/// </summary>
public class ListPluginsResponse : TransportResponse
{
	[JsonPropertyName("results")]
	public IReadOnlyList<AgentBuilderPlugin> Results { get; set; } = default!;
}
