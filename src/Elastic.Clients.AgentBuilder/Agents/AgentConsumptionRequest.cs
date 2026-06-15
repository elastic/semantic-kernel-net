// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elastic.Clients.AgentBuilder.Agents;

/// <summary>
/// Request for paginated, per-conversation token consumption data for an agent.
/// </summary>
public record AgentConsumptionRequest
{
	[JsonPropertyName("size")]
	public int? Size { get; init; }

	[JsonPropertyName("sort_field")]
	public string? SortField { get; init; }

	[JsonPropertyName("sort_order")]
	public string? SortOrder { get; init; }

	[JsonPropertyName("search")]
	public string? Search { get; init; }

	[JsonPropertyName("has_warnings")]
	public bool? HasWarnings { get; init; }

	[JsonPropertyName("usernames")]
	public IReadOnlyList<string>? Usernames { get; init; }

	[JsonPropertyName("search_after")]
	public IReadOnlyList<JsonElement>? SearchAfter { get; init; }
}
