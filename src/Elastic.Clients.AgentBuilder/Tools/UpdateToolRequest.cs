// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elastic.Clients.AgentBuilder.Tools;

/// <summary> Update request for an ES|QL tool. </summary>
public record UpdateEsqlToolRequest
{
	[JsonPropertyName("description")]
	public string? Description { get; init; }

	[JsonPropertyName("tags")]
	public IReadOnlyList<string>? Tags { get; init; }

	[JsonPropertyName("configuration")]
	public EsqlToolConfiguration? Configuration { get; init; }
}

/// <summary> Update request for an index search tool. </summary>
public record UpdateIndexSearchToolRequest
{
	[JsonPropertyName("description")]
	public string? Description { get; init; }

	[JsonPropertyName("tags")]
	public IReadOnlyList<string>? Tags { get; init; }

	[JsonPropertyName("configuration")]
	public IndexSearchToolConfiguration? Configuration { get; init; }
}

/// <summary> Update request for an MCP tool. </summary>
public record UpdateMcpToolRequest
{
	[JsonPropertyName("description")]
	public string? Description { get; init; }

	[JsonPropertyName("tags")]
	public IReadOnlyList<string>? Tags { get; init; }

	[JsonPropertyName("configuration")]
	public McpToolConfiguration? Configuration { get; init; }
}

/// <summary> Update request for a workflow tool. </summary>
public record UpdateWorkflowToolRequest
{
	[JsonPropertyName("description")]
	public string? Description { get; init; }

	[JsonPropertyName("tags")]
	public IReadOnlyList<string>? Tags { get; init; }

	[JsonPropertyName("configuration")]
	public WorkflowToolConfiguration? Configuration { get; init; }
}
