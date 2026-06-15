// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elastic.Clients.AgentBuilder.Tools;

/// <summary> Create request for an ES|QL tool. </summary>
public record CreateEsqlToolRequest
{
	[JsonPropertyName("id")]
	public required string Id { get; init; }

	[JsonPropertyName("type")]
	public string Type => ToolType.Esql;

	[JsonPropertyName("description")]
	public required string Description { get; init; }

	[JsonPropertyName("tags")]
	public IReadOnlyList<string>? Tags { get; init; }

	[JsonPropertyName("configuration")]
	public required EsqlToolConfiguration Configuration { get; init; }
}

/// <summary> Create request for an index search tool. </summary>
public record CreateIndexSearchToolRequest
{
	[JsonPropertyName("id")]
	public required string Id { get; init; }

	[JsonPropertyName("type")]
	public string Type => ToolType.IndexSearch;

	[JsonPropertyName("description")]
	public required string Description { get; init; }

	[JsonPropertyName("tags")]
	public IReadOnlyList<string>? Tags { get; init; }

	[JsonPropertyName("configuration")]
	public required IndexSearchToolConfiguration Configuration { get; init; }
}

/// <summary> Create request for an MCP tool. </summary>
public record CreateMcpToolRequest
{
	[JsonPropertyName("id")]
	public required string Id { get; init; }

	[JsonPropertyName("type")]
	public string Type => ToolType.Mcp;

	[JsonPropertyName("description")]
	public required string Description { get; init; }

	[JsonPropertyName("tags")]
	public IReadOnlyList<string>? Tags { get; init; }

	[JsonPropertyName("configuration")]
	public required McpToolConfiguration Configuration { get; init; }
}

/// <summary> Create request for a workflow tool. </summary>
public record CreateWorkflowToolRequest
{
	[JsonPropertyName("id")]
	public required string Id { get; init; }

	[JsonPropertyName("type")]
	public string Type => ToolType.Workflow;

	[JsonPropertyName("description")]
	public required string Description { get; init; }

	[JsonPropertyName("tags")]
	public IReadOnlyList<string>? Tags { get; init; }

	[JsonPropertyName("configuration")]
	public required WorkflowToolConfiguration Configuration { get; init; }
}
