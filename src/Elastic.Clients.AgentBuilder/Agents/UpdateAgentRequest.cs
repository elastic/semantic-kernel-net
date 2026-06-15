// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elastic.Clients.AgentBuilder.Agents;

/// <summary>
/// Request to update an existing agent.
/// </summary>
public record UpdateAgentRequest
{
	[JsonPropertyName("name")]
	public string? Name { get; init; }

	[JsonPropertyName("description")]
	public string? Description { get; init; }

	[JsonPropertyName("labels")]
	public IReadOnlyList<string>? Labels { get; init; }

	[JsonPropertyName("avatar_color")]
	public string? AvatarColor { get; init; }

	[JsonPropertyName("avatar_symbol")]
	public string? AvatarSymbol { get; init; }

	[JsonPropertyName("configuration")]
	public AgentConfiguration? Configuration { get; init; }
}
