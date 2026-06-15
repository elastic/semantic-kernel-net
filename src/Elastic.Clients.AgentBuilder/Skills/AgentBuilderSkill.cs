// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Elastic.Transport;

namespace Elastic.Clients.AgentBuilder.Skills;

/// <summary>
/// Represents a skill as returned by the Agent Builder API.
/// </summary>
public class AgentBuilderSkill : TransportResponse
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = default!;

	[JsonPropertyName("name")]
	public string Name { get; set; } = default!;

	[JsonPropertyName("description")]
	public string? Description { get; set; }

	[JsonPropertyName("content")]
	public string? Content { get; set; }

	[JsonPropertyName("tool_ids")]
	public IReadOnlyList<string>? ToolIds { get; set; }

	[JsonPropertyName("referenced_content")]
	public IReadOnlyList<SkillReferencedContent>? ReferencedContent { get; set; }

	[JsonPropertyName("readonly")]
	public bool Readonly { get; set; }
}

/// <summary>
/// A piece of referenced content attached to a skill.
/// </summary>
public record SkillReferencedContent
{
	[JsonPropertyName("id")]
	public string? Id { get; init; }

	[JsonPropertyName("title")]
	public string? Title { get; init; }

	[JsonPropertyName("type")]
	public string? Type { get; init; }
}

/// <summary>
/// Response wrapper for listing skills.
/// </summary>
public class ListSkillsResponse : TransportResponse
{
	[JsonPropertyName("results")]
	public IReadOnlyList<AgentBuilderSkill> Results { get; set; } = default!;
}
