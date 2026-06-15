// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Clients.AgentBuilder.Agents;
using FluentAssertions;
using Xunit;

namespace Elastic.Clients.AgentBuilder.Tests.Serialization;

public class AgentSerializationTests
{
	private static readonly AgentBuilderSerializationContext Ctx = AgentBuilderSerializationContext.Default;

	[Fact]
	public void CreateAgentRequest_SerializesCorrectly()
	{
		var request = new CreateAgentRequest
		{
			Id = "books-agent",
			Name = "Books Helper",
			Description = "Helps search books",
			Labels = ["books", "search"],
			AvatarColor = "#BFDBFF",
			AvatarSymbol = "BK",
			Configuration = new AgentConfiguration
			{
				Instructions = "You help users search book data.",
				Tools =
				[
					new AgentToolGroup
					{
						ToolIds = ["my-esql-tool", "platform.core.search"]
					}
				]
			}
		};

		var json = JsonSerializer.Serialize(request, Ctx.CreateAgentRequest);
		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		root.GetProperty("id").GetString().Should().Be("books-agent");
		root.GetProperty("name").GetString().Should().Be("Books Helper");
		root.GetProperty("avatar_color").GetString().Should().Be("#BFDBFF");
		root.GetProperty("configuration").GetProperty("tools")[0]
			.GetProperty("tool_ids").GetArrayLength().Should().Be(2);
	}

	[Fact]
	public void AgentBuilderAgent_DeserializesFromApi()
	{
		var json = """
		{
			"id": "books-search-agent",
			"type": "chat",
			"name": "Books Search Helper",
			"description": "Search books",
			"labels": ["books"],
			"avatar_color": "#BFDBFF",
			"avatar_symbol": "📚",
			"configuration": {
				"instructions": "Help users search books.",
				"tools": [{ "tool_ids": ["platform.core.search"] }]
			},
			"readonly": false
		}
		""";

		var agent = JsonSerializer.Deserialize(json, Ctx.AgentBuilderAgent);

		agent.Should().NotBeNull();
		agent!.Id.Should().Be("books-search-agent");
		agent.Name.Should().Be("Books Search Helper");
		agent.Type.Should().Be("chat");
		agent.Configuration.Should().NotBeNull();
		agent.Configuration!.Tools.Should().HaveCount(1);
		agent.Configuration.Tools![0].ToolIds.Should().Contain("platform.core.search");
	}
}
