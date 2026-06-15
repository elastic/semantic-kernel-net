// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Clients.AgentBuilder.Tools;

/// <summary>
/// Known tool type identifiers used by the Agent Builder API.
/// </summary>
public static class ToolType
{
	public const string Esql = "esql";
	public const string IndexSearch = "index_search";
	public const string Mcp = "mcp";
	public const string Workflow = "workflow";
	public const string Builtin = "builtin";
}
