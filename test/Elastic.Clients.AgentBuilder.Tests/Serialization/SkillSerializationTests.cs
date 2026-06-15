// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Clients.AgentBuilder.Skills;
using FluentAssertions;
using Xunit;

namespace Elastic.Clients.AgentBuilder.Tests.Serialization;

public class SkillSerializationTests
{
	private static readonly AgentBuilderSerializationContext Ctx = AgentBuilderSerializationContext.Default;

	[Fact]
	public void CreateSkillRequest_SerializesCorrectly()
	{
		var request = new CreateSkillRequest
		{
			Id = "my-skill",
			Name = "Research Skill",
			Description = "Helps with research tasks",
			Content = "## Instructions\nUse the search tool to find relevant documents.",
			ToolIds = ["search-tool", "esql-tool"],
			ReferencedContent =
			[
				new SkillReferencedContent { Id = "ref-1", Title = "Guide", Type = "document" }
			]
		};

		var json = JsonSerializer.Serialize(request, Ctx.CreateSkillRequest);
		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		root.GetProperty("id").GetString().Should().Be("my-skill");
		root.GetProperty("name").GetString().Should().Be("Research Skill");
		root.GetProperty("description").GetString().Should().Be("Helps with research tasks");
		root.GetProperty("content").GetString().Should().Contain("## Instructions");
		root.GetProperty("tool_ids").GetArrayLength().Should().Be(2);
		root.GetProperty("referenced_content")[0].GetProperty("id").GetString().Should().Be("ref-1");
	}

	[Fact]
	public void CreateSkillRequest_OmitsNullProperties()
	{
		var request = new CreateSkillRequest
		{
			Id = "minimal-skill",
			Name = "Minimal"
		};

		var json = JsonSerializer.Serialize(request, Ctx.CreateSkillRequest);
		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		root.GetProperty("id").GetString().Should().Be("minimal-skill");
		root.TryGetProperty("description", out _).Should().BeFalse();
		root.TryGetProperty("content", out _).Should().BeFalse();
		root.TryGetProperty("tool_ids", out _).Should().BeFalse();
	}

	[Fact]
	public void UpdateSkillRequest_SerializesCorrectly()
	{
		var request = new UpdateSkillRequest
		{
			Name = "Updated Skill",
			Content = "Updated instructions"
		};

		var json = JsonSerializer.Serialize(request, Ctx.UpdateSkillRequest);
		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		root.GetProperty("name").GetString().Should().Be("Updated Skill");
		root.GetProperty("content").GetString().Should().Be("Updated instructions");
		root.TryGetProperty("description", out _).Should().BeFalse();
	}

	[Fact]
	public void AgentBuilderSkill_DeserializesFromApi()
	{
		var json = """
		{
			"id": "research-skill",
			"name": "Research Assistant",
			"description": "Helps with research",
			"content": "Use the search tool to find relevant data.",
			"tool_ids": ["search-tool"],
			"referenced_content": [
				{ "id": "ref-1", "title": "Guide", "type": "document" }
			],
			"readonly": false
		}
		""";

		var skill = JsonSerializer.Deserialize(json, Ctx.AgentBuilderSkill);

		skill.Should().NotBeNull();
		skill!.Id.Should().Be("research-skill");
		skill.Name.Should().Be("Research Assistant");
		skill.Description.Should().Be("Helps with research");
		skill.Content.Should().Contain("search tool");
		skill.ToolIds.Should().ContainSingle().Which.Should().Be("search-tool");
		skill.ReferencedContent.Should().ContainSingle();
		skill.ReferencedContent![0].Id.Should().Be("ref-1");
		skill.Readonly.Should().BeFalse();
	}

	[Fact]
	public void ListSkillsResponse_DeserializesFromApi()
	{
		var json = """
		{
			"results": [
				{
					"id": "builtin-search",
					"name": "Search",
					"description": "Built-in search skill",
					"readonly": true
				},
				{
					"id": "custom-skill",
					"name": "Custom",
					"description": "User skill",
					"content": "Custom instructions",
					"tool_ids": ["my-tool"],
					"readonly": false
				}
			]
		}
		""";

		var response = JsonSerializer.Deserialize(json, Ctx.ListSkillsResponse);

		response.Should().NotBeNull();
		response!.Results.Should().HaveCount(2);
		response.Results[0].Readonly.Should().BeTrue();
		response.Results[1].ToolIds.Should().ContainSingle().Which.Should().Be("my-tool");
	}
}
