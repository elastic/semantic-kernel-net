// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Clients.AgentBuilder.Tools;

/// <summary>
/// Configuration for an index search tool. Scopes LLM-driven search to specific index patterns.
/// </summary>
public record IndexSearchToolConfiguration
{
	[JsonPropertyName("index_pattern")]
	public required string IndexPattern { get; init; }

	[JsonPropertyName("row_limit")]
	public int? RowLimit { get; init; }

	[JsonPropertyName("custom_instructions")]
	public string? CustomInstructions { get; init; }
}
