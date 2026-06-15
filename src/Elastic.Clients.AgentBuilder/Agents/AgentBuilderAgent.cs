// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Elastic.Transport;

namespace Elastic.Clients.AgentBuilder.Agents;

/// <summary>
/// Represents an agent as returned by the Agent Builder API.
/// </summary>
public class AgentBuilderAgent : TransportResponse
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = default!;

	[JsonPropertyName("type")]
	public string? Type { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = default!;

	[JsonPropertyName("description")]
	public string? Description { get; set; }

	[JsonPropertyName("labels")]
	public IReadOnlyList<string>? Labels { get; set; }

	[JsonPropertyName("avatar_color")]
	public string? AvatarColor { get; set; }

	[JsonPropertyName("avatar_symbol")]
	public string? AvatarSymbol { get; set; }

	[JsonPropertyName("configuration")]
	public AgentConfiguration? Configuration { get; set; }

	[JsonPropertyName("readonly")]
	public bool Readonly { get; set; }
}

/// <summary>
/// Configuration settings for an agent.
/// </summary>
public record AgentConfiguration
{
	[JsonPropertyName("instructions")]
	public string? Instructions { get; init; }

	[JsonPropertyName("tools")]
	public IReadOnlyList<AgentToolGroup>? Tools { get; init; }
}

/// <summary>
/// A group of tool IDs assigned to an agent.
/// </summary>
public record AgentToolGroup
{
	[JsonPropertyName("tool_ids")]
	public required IReadOnlyList<string> ToolIds { get; init; }
}

/// <summary>
/// Response wrapper for listing agents.
/// </summary>
public class ListAgentsResponse : TransportResponse
{
	[JsonPropertyName("results")]
	public IReadOnlyList<AgentBuilderAgent> Results { get; set; } = default!;
}
