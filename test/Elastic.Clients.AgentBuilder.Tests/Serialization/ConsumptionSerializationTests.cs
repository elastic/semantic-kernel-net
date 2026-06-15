// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Clients.AgentBuilder.Agents;
using FluentAssertions;
using Xunit;

namespace Elastic.Clients.AgentBuilder.Tests.Serialization;

public class ConsumptionSerializationTests
{
	private static readonly AgentBuilderSerializationContext Ctx = AgentBuilderSerializationContext.Default;

	[Fact]
	public void AgentConsumptionRequest_SerializesCorrectly()
	{
		var request = new AgentConsumptionRequest
		{
			Size = 25,
			SortField = "input_tokens",
			SortOrder = "desc",
			Search = "books",
			HasWarnings = true,
			Usernames = ["user1", "user2"]
		};

		var json = JsonSerializer.Serialize(request, Ctx.AgentConsumptionRequest);
		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		root.GetProperty("size").GetInt32().Should().Be(25);
		root.GetProperty("sort_field").GetString().Should().Be("input_tokens");
		root.GetProperty("sort_order").GetString().Should().Be("desc");
		root.GetProperty("search").GetString().Should().Be("books");
		root.GetProperty("has_warnings").GetBoolean().Should().BeTrue();
		root.GetProperty("usernames").GetArrayLength().Should().Be(2);
	}

	[Fact]
	public void AgentConsumptionRequest_OmitsNullProperties()
	{
		var request = new AgentConsumptionRequest
		{
			Size = 10
		};

		var json = JsonSerializer.Serialize(request, Ctx.AgentConsumptionRequest);
		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		root.GetProperty("size").GetInt32().Should().Be(10);
		root.TryGetProperty("sort_field", out _).Should().BeFalse();
		root.TryGetProperty("search", out _).Should().BeFalse();
		root.TryGetProperty("has_warnings", out _).Should().BeFalse();
	}

	[Fact]
	public void AgentConsumptionResponse_DeserializesFromApi()
	{
		var json = """
		{
			"results": [
				{
					"conversation_id": "conv-abc",
					"title": "Book search conversation",
					"username": "testuser",
					"input_tokens": 50000,
					"output_tokens": 2000,
					"round_count": 5,
					"llm_call_count": 12,
					"has_warnings": true
				},
				{
					"conversation_id": "conv-def",
					"title": "Simple query",
					"username": "testuser",
					"input_tokens": 1000,
					"output_tokens": 200,
					"round_count": 1,
					"llm_call_count": 2,
					"has_warnings": false
				}
			],
			"search_after": [50000, "conv-abc"]
		}
		""";

		var response = JsonSerializer.Deserialize(json, Ctx.AgentConsumptionResponse);

		response.Should().NotBeNull();
		response!.Results.Should().HaveCount(2);

		var first = response.Results[0];
		first.ConversationId.Should().Be("conv-abc");
		first.Title.Should().Be("Book search conversation");
		first.InputTokens.Should().Be(50000);
		first.OutputTokens.Should().Be(2000);
		first.RoundCount.Should().Be(5);
		first.LlmCallCount.Should().Be(12);
		first.HasWarnings.Should().BeTrue();

		response.SearchAfter.Should().HaveCount(2);
	}

	[Fact]
	public void AgentConsumptionResponse_DeserializesWithoutSearchAfter()
	{
		var json = """
		{
			"results": []
		}
		""";

		var response = JsonSerializer.Deserialize(json, Ctx.AgentConsumptionResponse);

		response.Should().NotBeNull();
		response!.Results.Should().BeEmpty();
		response.SearchAfter.Should().BeNull();
	}
}
