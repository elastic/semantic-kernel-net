// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elastic.Clients.AgentBuilder.Conversations;
using FluentAssertions;
using Xunit;

namespace Elastic.Clients.AgentBuilder.IntegrationTests;

public class ConversationTests : AgentBuilderTestBase
{
	private readonly List<string> _conversationIds = [];

	[Fact]
	public async Task CanConverseAndListConversations()
	{
		var response = await Client.ConverseAsync(new ConverseRequest
		{
			Input = "Say hello in exactly three words."
		});

		response.Should().NotBeNull();
		response.ConversationId.Should().NotBeNullOrWhiteSpace();
		response.Status.Should().Be("completed");
		response.Response.Should().NotBeNull();
		response.Response!.Message.Should().NotBeNullOrWhiteSpace();
		_conversationIds.Add(response.ConversationId);

		var conversations = await Client.ListConversationsAsync();
		conversations.Should().NotBeNull();
		conversations.Results.Should().Contain(c => c.ConversationId == response.ConversationId);
	}

	[Fact]
	public async Task CanConverseGetAndDeleteConversation()
	{
		var created = await Client.ConverseAsync(new ConverseRequest
		{
			Input = "Reply with the single word 'pong'."
		});
		created.ConversationId.Should().NotBeNullOrWhiteSpace();
		_conversationIds.Add(created.ConversationId);

		var fetched = await Client.GetConversationAsync(created.ConversationId);
		fetched.Should().NotBeNull();
		fetched.ConversationId.Should().Be(created.ConversationId);
		fetched.Rounds.Should().NotBeNullOrEmpty();

		await Client.DeleteConversationAsync(created.ConversationId);
		_conversationIds.Remove(created.ConversationId);

		Func<Task> act = async () => await Client.GetConversationAsync(created.ConversationId);
		await act.Should().ThrowAsync<AgentBuilderException>();
	}

	[Fact]
	public async Task CanContinueConversation()
	{
		var first = await Client.ConverseAsync(new ConverseRequest
		{
			Input = "Remember the word 'banana'. Reply with just that word."
		});
		first.ConversationId.Should().NotBeNullOrWhiteSpace();
		_conversationIds.Add(first.ConversationId);

		var second = await Client.ConverseAsync(new ConverseRequest
		{
			Input = "What word did I ask you to remember? Reply with just that word.",
			ConversationId = first.ConversationId
		});

		second.ConversationId.Should().Be(first.ConversationId);
		second.Response.Should().NotBeNull();
		second.Response!.Message.Should().NotBeNullOrWhiteSpace();

		var conversation = await Client.GetConversationAsync(first.ConversationId);
		conversation.Rounds.Should().NotBeNull();
		conversation.Rounds!.Count.Should().BeGreaterThanOrEqualTo(2);
	}

	[Fact]
	public async Task CanStreamConversation()
	{
		var events = new List<ConverseStreamEvent>();
		var messageText = new StringBuilder();

		await foreach (var evt in Client.ConverseStreamAsync(new ConverseRequest
		{
			Input = "Reply with the single word 'streamed'."
		}))
		{
			events.Add(evt);

			switch (evt)
			{
				case ConversationIdSetEvent idSet:
					_conversationIds.Add(idSet.ConversationId);
					break;
				case MessageChunkEvent chunk:
					messageText.Append(chunk.TextChunk);
					break;
			}
		}

		events.Should().NotBeEmpty();
		events.OfType<ConversationIdSetEvent>().Should().ContainSingle();
		events.OfType<MessageCompleteEvent>().Should().ContainSingle();
		events.OfType<RoundCompleteEvent>().Should().ContainSingle();
		messageText.Length.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task ConverseResponse_IncludesModelUsage()
	{
		var response = await Client.ConverseAsync(new ConverseRequest
		{
			Input = "Reply with the single word 'usage'."
		});
		_conversationIds.Add(response.ConversationId);

		response.ModelUsage.Should().NotBeNull();
		response.ModelUsage!.InputTokens.Should().BeGreaterThan(0);
		response.ModelUsage.OutputTokens.Should().BeGreaterThan(0);
	}

	public override void Dispose()
	{
		foreach (var id in _conversationIds)
		{
			try { Client.DeleteConversationAsync(id).GetAwaiter().GetResult(); } catch { /* cleanup */ }
		}
		base.Dispose();
	}
}
