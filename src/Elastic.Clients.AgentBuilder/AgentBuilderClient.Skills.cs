// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.AgentBuilder.Skills;

namespace Elastic.Clients.AgentBuilder;

public partial class AgentBuilderClient
{
	/// <summary> List all available skills. </summary>
	public Task<ListSkillsResponse> ListSkillsAsync(CancellationToken ct = default) =>
		GetAsync<ListSkillsResponse>("/skills", ct);

	/// <summary> Get a skill by its ID. </summary>
	public Task<AgentBuilderSkill> GetSkillAsync(string skillId, CancellationToken ct = default) =>
		GetAsync<AgentBuilderSkill>($"/skills/{skillId}", ct);

	/// <summary> Create a new skill. </summary>
	public Task<AgentBuilderSkill> CreateSkillAsync(CreateSkillRequest request, CancellationToken ct = default) =>
		PostAsync<CreateSkillRequest, AgentBuilderSkill>("/skills", request, ct);

	/// <summary> Update an existing skill. </summary>
	public Task<AgentBuilderSkill> UpdateSkillAsync(string skillId, UpdateSkillRequest request, CancellationToken ct = default) =>
		PutAsync<UpdateSkillRequest, AgentBuilderSkill>($"/skills/{skillId}", request, ct);

	/// <summary> Delete a skill by its ID. </summary>
	public Task DeleteSkillAsync(string skillId, CancellationToken ct = default) =>
		DeleteAsync($"/skills/{skillId}", ct);
}
