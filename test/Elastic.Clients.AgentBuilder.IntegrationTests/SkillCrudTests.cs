// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using Elastic.Clients.AgentBuilder.Skills;
using FluentAssertions;
using Xunit;

namespace Elastic.Clients.AgentBuilder.IntegrationTests;

public class SkillCrudTests : AgentBuilderTestBase
{
	private const string TestSkillId = "dotnet-integration-test-skill";

	[Fact]
	public async Task CanListSkills()
	{
		try
		{
			var response = await Client.ListSkillsAsync();
			response.Should().NotBeNull();
			response.Results.Should().NotBeNull();
		}
		catch (AgentBuilderException ex) when (ex.ApiCallDetails.HttpStatusCode == 404)
		{
			// Skills API not available (requires Kibana 9.4.0+) — skip gracefully
		}
	}

	[Fact]
	public async Task CanCreateGetUpdateDeleteSkill()
	{
		try { await Client.DeleteSkillAsync(TestSkillId); }
		catch (AgentBuilderException ex) when (ex.ApiCallDetails.HttpStatusCode == 404)
		{
			// Skills API not available (requires Kibana 9.4.0+) — skip gracefully
			return;
		}
		catch { /* cleanup from previous runs */ }

		var created = await Client.CreateSkillAsync(new CreateSkillRequest
		{
			Id = TestSkillId,
			Name = "Integration Test Skill",
			Description = "A skill created by integration tests",
			Content = "## Instructions\nHelp the user with testing.",
			ToolIds = ["platform.core.search"]
		});
		created.Id.Should().Be(TestSkillId);
		created.Name.Should().Be("Integration Test Skill");

		var fetched = await Client.GetSkillAsync(TestSkillId);
		fetched.Id.Should().Be(TestSkillId);
		fetched.Description.Should().Be("A skill created by integration tests");
		fetched.Content.Should().Contain("## Instructions");

		var updated = await Client.UpdateSkillAsync(TestSkillId, new UpdateSkillRequest
		{
			Name = "Updated Integration Test Skill",
			Description = "Updated description",
			Content = "## Updated Instructions\nNew instructions."
		});
		updated.Name.Should().Be("Updated Integration Test Skill");

		await Client.DeleteSkillAsync(TestSkillId);

		Func<Task> act = async () => await Client.GetSkillAsync(TestSkillId);
		await act.Should().ThrowAsync<AgentBuilderException>();
	}

	public override void Dispose()
	{
		try { Client.DeleteSkillAsync(TestSkillId).GetAwaiter().GetResult(); } catch { /* cleanup */ }
		base.Dispose();
	}
}
