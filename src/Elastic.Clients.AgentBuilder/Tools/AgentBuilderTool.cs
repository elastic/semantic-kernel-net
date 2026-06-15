// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Transport;

namespace Elastic.Clients.AgentBuilder.Tools;

/// <summary>
/// Represents a tool as returned by the Agent Builder API.
/// The <see cref="Configuration"/> is a raw <see cref="JsonElement"/> that can be
/// deserialized into the appropriate typed configuration using <see cref="AsEsql"/>,
/// <see cref="AsIndexSearch"/>, <see cref="AsMcp"/>, or <see cref="AsWorkflow"/>.
/// </summary>
public class AgentBuilderTool : TransportResponse
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = default!;

	[JsonPropertyName("type")]
	public string Type { get; set; } = default!;

	[JsonPropertyName("description")]
	public string? Description { get; set; }

	[JsonPropertyName("tags")]
	public IReadOnlyList<string>? Tags { get; set; }

	[JsonPropertyName("configuration")]
	public JsonElement Configuration { get; set; }

	[JsonPropertyName("readonly")]
	public bool Readonly { get; set; }

	[JsonPropertyName("schema")]
	public JsonElement? Schema { get; set; }

	/// <summary> Deserialize configuration as an ES|QL tool. </summary>
	public EsqlToolConfiguration? AsEsql() =>
		Type == ToolType.Esql ? Configuration.Deserialize(AgentBuilderSerializationContext.Default.EsqlToolConfiguration) : null;

	/// <summary> Deserialize configuration as an index search tool. </summary>
	public IndexSearchToolConfiguration? AsIndexSearch() =>
		Type == ToolType.IndexSearch ? Configuration.Deserialize(AgentBuilderSerializationContext.Default.IndexSearchToolConfiguration) : null;

	/// <summary> Deserialize configuration as an MCP tool. </summary>
	public McpToolConfiguration? AsMcp() =>
		Type == ToolType.Mcp ? Configuration.Deserialize(AgentBuilderSerializationContext.Default.McpToolConfiguration) : null;

	/// <summary> Deserialize configuration as a workflow tool. </summary>
	public WorkflowToolConfiguration? AsWorkflow() =>
		Type == ToolType.Workflow ? Configuration.Deserialize(AgentBuilderSerializationContext.Default.WorkflowToolConfiguration) : null;
}

/// <summary>
/// Response wrapper for listing tools.
/// </summary>
public class ListToolsResponse : TransportResponse
{
	[JsonPropertyName("results")]
	public IReadOnlyList<AgentBuilderTool> Results { get; set; } = default!;
}
