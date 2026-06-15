// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.AgentBuilder.Tools;

namespace Elastic.Clients.AgentBuilder;

public partial class AgentBuilderClient
{
	/// <summary> List all available tools. </summary>
	public Task<ListToolsResponse> ListToolsAsync(CancellationToken ct = default) =>
		GetAsync<ListToolsResponse>("/tools", ct);

	/// <summary> Get a tool by its ID. </summary>
	public Task<AgentBuilderTool> GetToolAsync(string toolId, CancellationToken ct = default) =>
		GetAsync<AgentBuilderTool>($"/tools/{toolId}", ct);

	/// <summary> Create an ES|QL tool. </summary>
	public Task<AgentBuilderTool> CreateToolAsync(CreateEsqlToolRequest request, CancellationToken ct = default) =>
		PostAsync<CreateEsqlToolRequest, AgentBuilderTool>("/tools", request, ct);

	/// <summary> Create an index search tool. </summary>
	public Task<AgentBuilderTool> CreateToolAsync(CreateIndexSearchToolRequest request, CancellationToken ct = default) =>
		PostAsync<CreateIndexSearchToolRequest, AgentBuilderTool>("/tools", request, ct);

	/// <summary> Create an MCP tool. </summary>
	public Task<AgentBuilderTool> CreateToolAsync(CreateMcpToolRequest request, CancellationToken ct = default) =>
		PostAsync<CreateMcpToolRequest, AgentBuilderTool>("/tools", request, ct);

	/// <summary> Create a workflow tool. </summary>
	public Task<AgentBuilderTool> CreateToolAsync(CreateWorkflowToolRequest request, CancellationToken ct = default) =>
		PostAsync<CreateWorkflowToolRequest, AgentBuilderTool>("/tools", request, ct);

	/// <summary> Update an ES|QL tool. </summary>
	public Task<AgentBuilderTool> UpdateToolAsync(string toolId, UpdateEsqlToolRequest request, CancellationToken ct = default) =>
		PutAsync<UpdateEsqlToolRequest, AgentBuilderTool>($"/tools/{toolId}", request, ct);

	/// <summary> Update an index search tool. </summary>
	public Task<AgentBuilderTool> UpdateToolAsync(string toolId, UpdateIndexSearchToolRequest request, CancellationToken ct = default) =>
		PutAsync<UpdateIndexSearchToolRequest, AgentBuilderTool>($"/tools/{toolId}", request, ct);

	/// <summary> Update an MCP tool. </summary>
	public Task<AgentBuilderTool> UpdateToolAsync(string toolId, UpdateMcpToolRequest request, CancellationToken ct = default) =>
		PutAsync<UpdateMcpToolRequest, AgentBuilderTool>($"/tools/{toolId}", request, ct);

	/// <summary> Update a workflow tool. </summary>
	public Task<AgentBuilderTool> UpdateToolAsync(string toolId, UpdateWorkflowToolRequest request, CancellationToken ct = default) =>
		PutAsync<UpdateWorkflowToolRequest, AgentBuilderTool>($"/tools/{toolId}", request, ct);

	/// <summary> Delete a tool by its ID. </summary>
	public Task DeleteToolAsync(string toolId, CancellationToken ct = default) =>
		DeleteAsync($"/tools/{toolId}", ct);

	/// <summary> Execute a tool with parameters. </summary>
	public Task<ExecuteToolResponse> ExecuteToolAsync(ExecuteToolRequest request, CancellationToken ct = default) =>
		PostAsync<ExecuteToolRequest, ExecuteToolResponse>("/tools/_execute", request, ct);
}
