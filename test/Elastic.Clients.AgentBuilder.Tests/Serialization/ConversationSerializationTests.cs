// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Clients.AgentBuilder.Conversations;
using FluentAssertions;
using Xunit;

namespace Elastic.Clients.AgentBuilder.Tests.Serialization;

public class ConversationSerializationTests
{
	private static readonly AgentBuilderSerializationContext Ctx = AgentBuilderSerializationContext.Default;

	[Fact]
	public void ConverseRequest_SerializesCorrectly()
	{
		var request = new ConverseRequest
		{
			Input = "What books do we have?",
			AgentId = "books-agent",
			ConversationId = "conv-123"
		};

		var json = JsonSerializer.Serialize(request, Ctx.ConverseRequest);
		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		root.GetProperty("input").GetString().Should().Be("What books do we have?");
		root.GetProperty("agent_id").GetString().Should().Be("books-agent");
		root.GetProperty("conversation_id").GetString().Should().Be("conv-123");
	}

	[Fact]
	public void ConverseResponse_DeserializesFromApi()
	{
		var json = """
		{
			"conversation_id": "e5b4e3ff-f5c9-4f52-9001-87b403df1a6a",
			"round_id": "a37100bc-3996-4be5-b874-7920a36e2ede",
			"status": "completed",
			"steps": [
				{
					"type": "reasoning",
					"reasoning": "The user is asking about books."
				},
				{
					"type": "tool_call",
					"tool_id": "platform.core.search",
					"params": { "query": "all books" }
				}
			],
			"model_usage": {
				"input_tokens": 30257,
				"output_tokens": 670
			},
			"response": {
				"message": "Your collection contains 6 books."
			}
		}
		""";

		var response = JsonSerializer.Deserialize(json, Ctx.ConverseResponse);

		response.Should().NotBeNull();
		response!.ConversationId.Should().Be("e5b4e3ff-f5c9-4f52-9001-87b403df1a6a");
		response.Status.Should().Be("completed");
		response.Steps.Should().HaveCount(2);
		response.Steps![0].Type.Should().Be("reasoning");
		response.Steps[0].Reasoning.Should().Contain("asking about books");
		response.Steps[1].ToolId.Should().Be("platform.core.search");
		response.ModelUsage.Should().NotBeNull();
		response.ModelUsage!.InputTokens.Should().Be(30257);
		response.Response.Should().NotBeNull();
		response.Response!.Message.Should().Contain("6 books");
	}
}
