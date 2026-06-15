// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Clients.AgentBuilder.Tools;

/// <summary>
/// Configuration for an MCP tool that connects to an external Model Context Protocol server.
/// </summary>
public record McpToolConfiguration
{
	[JsonPropertyName("connector_id")]
	public required string ConnectorId { get; init; }

	[JsonPropertyName("tool_name")]
	public required string ToolName { get; init; }
}
