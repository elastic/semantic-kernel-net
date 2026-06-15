// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Elastic.Clients.AgentBuilder.Agents;
using FluentAssertions;
using Xunit;

namespace Elastic.Clients.AgentBuilder.IntegrationTests;

public class AgentConsumptionTests : AgentBuilderTestBase
{
	[Fact]
	public async Task CanGetConsumptionForAgent()
	{
		var agents = await Client.ListAgentsAsync();
		agents.Results.Should().NotBeEmpty("at least one agent must exist");

		var agentId = agents.Results[0].Id;
		var response = await Client.GetAgentConsumptionAsync(agentId, new AgentConsumptionRequest
		{
			Size = 5
		});

		response.Should().NotBeNull();
		response.Results.Should().NotBeNull();
	}
}
