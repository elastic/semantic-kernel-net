// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elastic.Clients.AgentBuilder.Tools;
using FluentAssertions;
using Xunit;

namespace Elastic.Clients.AgentBuilder.IntegrationTests;

public class ToolCrudTests : AgentBuilderTestBase
{
	private const string TestToolId = "dotnet-integration-test-tool";

	[Fact]
	public async Task CanListTools()
	{
		var response = await Client.ListToolsAsync();
		response.Should().NotBeNull();
		response.Results.Should().NotBeEmpty();
	}

	[Fact]
	public async Task CanCreateGetUpdateDeleteEsqlTool()
	{
		try { await Client.DeleteToolAsync(TestToolId); } catch { /* cleanup from previous runs */ }

		var created = await Client.CreateToolAsync(new CreateEsqlToolRequest
		{
			Id = TestToolId,
			Description = "Integration test tool",
			Tags = ["integration-test"],
			Configuration = new EsqlToolConfiguration
			{
				Query = "FROM kibana_sample_data_agents | LIMIT ?limit",
				Params = new Dictionary<string, EsqlToolParam>
				{
					["limit"] = new() { Type = "integer", Description = "Max results" }
				}
			}
		});
		created.Id.Should().Be(TestToolId);
		created.Type.Should().Be("esql");

		var fetched = await Client.GetToolAsync(TestToolId);
		fetched.Id.Should().Be(TestToolId);
		fetched.Description.Should().Be("Integration test tool");

		var updated = await Client.UpdateToolAsync(TestToolId, new UpdateEsqlToolRequest
		{
			Description = "Updated integration test tool",
			Tags = ["integration-test", "updated"],
			Configuration = new EsqlToolConfiguration
			{
				Query = "FROM kibana_sample_data_agents | LIMIT ?limit",
				Params = new Dictionary<string, EsqlToolParam>
				{
					["limit"] = new() { Type = "integer", Description = "Maximum number of results" }
				}
			}
		});
		updated.Description.Should().Be("Updated integration test tool");

		await Client.DeleteToolAsync(TestToolId);

		Func<Task> act = async () => await Client.GetToolAsync(TestToolId);
		await act.Should().ThrowAsync<AgentBuilderException>();
	}

	public override void Dispose()
	{
		try { Client.DeleteToolAsync(TestToolId).GetAwaiter().GetResult(); } catch { /* cleanup */ }
		base.Dispose();
	}
}
