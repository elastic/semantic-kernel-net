// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using System.Threading.Tasks;
using Elastic.Clients.AgentBuilder;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Xunit;

namespace Elastic.Extensions.AI.IntegrationTests;

public class ChatClientTests(KibanaFixture fixture) : IClassFixture<KibanaFixture>
{
	[Fact]
	public async Task GetResponseAsync_ReturnsAssistantMessage()
	{
		var response = await fixture.ChatClient.GetResponseAsync("Say hello in one sentence.");

		response.Should().NotBeNull();
		response.Messages.Should().NotBeEmpty();

		var assistant = response.Messages.FirstOrDefault(m => m.Role == ChatRole.Assistant);
		assistant.Should().NotBeNull();
		assistant!.Text.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public async Task GetResponseAsync_ReturnsConversationId()
	{
		var response = await fixture.ChatClient.GetResponseAsync("What is 2 + 2?");

		response.ConversationId.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public async Task GetResponseAsync_MultiTurnConversation()
	{
		var r1 = await fixture.ChatClient.GetResponseAsync("Remember the number 42.");

		r1.ConversationId.Should().NotBeNullOrWhiteSpace();

		var r2 = await fixture.ChatClient.GetResponseAsync(
			[new ChatMessage(ChatRole.User, "What number did I just ask you to remember?")],
			new ChatOptions { ConversationId = r1.ConversationId });

		r2.Should().NotBeNull();
		r2.ConversationId.Should().Be(r1.ConversationId);
		r2.Messages.Should().NotBeEmpty();

		var text = r2.Messages.First(m => m.Role == ChatRole.Assistant).Text;
		text.Should().Contain("42");

		await fixture.AgentClient.DeleteConversationAsync(r1.ConversationId!);
	}

	[Fact]
	public async Task GetStreamingResponseAsync_YieldsUpdate()
	{
		var updates = fixture.ChatClient.GetStreamingResponseAsync("Say hi briefly.");

		var textChunks = 0;
		ChatFinishReason? finishReason = null;
		await foreach (var update in updates)
		{
			update.Role.Should().Be(ChatRole.Assistant);
			update.ConversationId.Should().NotBeNullOrWhiteSpace();

			if (!string.IsNullOrWhiteSpace(update.Text))
				textChunks++;

			if (update.FinishReason is not null)
				finishReason = update.FinishReason;
		}

		textChunks.Should().BeGreaterThan(0, "expected at least one text chunk from the stream");
		finishReason.Should().Be(ChatFinishReason.Stop);
	}

	[Fact]
	public void Metadata_IsPopulated()
	{
		var metadata = fixture.ChatClient.GetService<ChatClientMetadata>();
		metadata.Should().NotBeNull();
		metadata!.ProviderName.Should().Be("elastic-agent-builder");
	}

	[Fact]
	public void GetService_ReturnsAgentBuilderClient()
	{
		var client = fixture.ChatClient.GetService<AgentBuilderClient>();
		client.Should().NotBeNull();
		client.Should().BeSameAs(fixture.AgentClient);
	}

	[Fact]
	public void GetService_ReturnsSelfForIChatClient()
	{
		var self = fixture.ChatClient.GetService<ElasticAgentChatClient>();
		self.Should().NotBeNull();
		self.Should().BeSameAs(fixture.ChatClient);
	}
}
