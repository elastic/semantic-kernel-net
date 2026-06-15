// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elastic.Clients.AgentBuilder.Tools;

/// <summary>
/// Request to execute a tool with parameters.
/// </summary>
public record ExecuteToolRequest
{
	[JsonPropertyName("tool_id")]
	public required string ToolId { get; init; }

	[JsonPropertyName("tool_params")]
	public Dictionary<string, JsonElement>? ToolParams { get; init; }

	[JsonPropertyName("connector_id")]
	public string? ConnectorId { get; init; }
}
