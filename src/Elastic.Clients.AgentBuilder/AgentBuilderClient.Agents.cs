// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.AgentBuilder.Agents;

namespace Elastic.Clients.AgentBuilder;

public partial class AgentBuilderClient
{
	/// <summary> List all agents. </summary>
	public Task<ListAgentsResponse> ListAgentsAsync(CancellationToken ct = default) =>
		GetAsync<ListAgentsResponse>("/agents", ct);

	/// <summary> Get an agent by its ID. </summary>
	public Task<AgentBuilderAgent> GetAgentAsync(string agentId, CancellationToken ct = default) =>
		GetAsync<AgentBuilderAgent>($"/agents/{agentId}", ct);

	/// <summary> Create a new agent. </summary>
	public Task<AgentBuilderAgent> CreateAgentAsync(CreateAgentRequest request, CancellationToken ct = default) =>
		PostAsync<CreateAgentRequest, AgentBuilderAgent>("/agents", request, ct);

	/// <summary> Update an existing agent. </summary>
	public Task<AgentBuilderAgent> UpdateAgentAsync(string agentId, UpdateAgentRequest request, CancellationToken ct = default) =>
		PutAsync<UpdateAgentRequest, AgentBuilderAgent>($"/agents/{agentId}", request, ct);

	/// <summary> Delete an agent by its ID. </summary>
	public Task DeleteAgentAsync(string agentId, CancellationToken ct = default) =>
		DeleteAsync($"/agents/{agentId}", ct);

	/// <summary> Get paginated, per-conversation token consumption data for an agent. </summary>
	public Task<AgentConsumptionResponse> GetAgentConsumptionAsync(
		string agentId, AgentConsumptionRequest request, CancellationToken ct = default) =>
		PostAsync<AgentConsumptionRequest, AgentConsumptionResponse>(
			$"/agents/{agentId}/consumption", request, ct);
}
