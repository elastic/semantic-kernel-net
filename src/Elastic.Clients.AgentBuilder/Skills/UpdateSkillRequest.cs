// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elastic.Clients.AgentBuilder.Skills;

/// <summary>
/// Request to update an existing skill.
/// </summary>
public record UpdateSkillRequest
{
	[JsonPropertyName("name")]
	public string? Name { get; init; }

	[JsonPropertyName("description")]
	public string? Description { get; init; }

	[JsonPropertyName("content")]
	public string? Content { get; init; }

	[JsonPropertyName("tool_ids")]
	public IReadOnlyList<string>? ToolIds { get; init; }

	[JsonPropertyName("referenced_content")]
	public IReadOnlyList<SkillReferencedContent>? ReferencedContent { get; init; }
}
