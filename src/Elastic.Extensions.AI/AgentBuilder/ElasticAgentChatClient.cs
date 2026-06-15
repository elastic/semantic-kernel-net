// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.AgentBuilder;
using Elastic.Clients.AgentBuilder.Conversations;
using Microsoft.Extensions.AI;

namespace Elastic.Extensions.AI;

/// <summary>
/// An <see cref="IChatClient"/> implementation that wraps the Elastic Agent Builder converse API.
/// Enables Kibana agents to participate in the <c>Microsoft.Extensions.AI</c> ecosystem —
/// composable with <c>ChatClientBuilder</c> middleware such as function invocation,
/// OpenTelemetry, caching, and rate limiting.
/// </summary>
public class ElasticAgentChatClient : IChatClient
{
	private readonly AgentBuilderClient _client;
	private readonly string _agentId;
	private readonly string? _connectorId;
	private readonly ChatClientMetadata _metadata;

	/// <summary>
	/// Creates a new <see cref="ElasticAgentChatClient"/> targeting a specific Kibana agent.
	/// </summary>
	/// <param name="client">The underlying <see cref="AgentBuilderClient"/>.</param>
	/// <param name="agentId">The ID of the agent to converse with.</param>
	/// <param name="connectorId">Optional LLM connector ID to use for the conversation.</param>
	/// <param name="providerUri">Optional URI for metadata (e.g. the Kibana URL).</param>
	public ElasticAgentChatClient(
		AgentBuilderClient client,
		string agentId,
		string? connectorId = null,
		Uri? providerUri = null)
	{
		_client = client ?? throw new ArgumentNullException(nameof(client));
		_agentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
		_connectorId = connectorId;
		_metadata = new ChatClientMetadata("elastic-agent-builder", providerUri, agentId);
	}

	/// <inheritdoc />
	public ChatClientMetadata Metadata => _metadata;

	/// <inheritdoc />
	public async Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		var messageList = messages?.ToList() ?? throw new ArgumentNullException(nameof(messages));
		var lastUserMessage = messageList.LastOrDefault(m => m.Role == ChatRole.User);
		var input = lastUserMessage?.Text ?? string.Empty;

		var conversationId = options?.ConversationId;

		var response = await _client.ConverseAsync(new ConverseRequest
		{
			Input = input,
			AgentId = _agentId,
			ConversationId = conversationId,
			ConnectorId = _connectorId
		}, cancellationToken).ConfigureAwait(false);

		var assistantMessage = new ChatMessage(ChatRole.Assistant, response.Response?.Message ?? string.Empty);

		var chatResponse = new ChatResponse(assistantMessage)
		{
			ConversationId = response.ConversationId,
			ModelId = _agentId,
			FinishReason = response.Status == "completed" ? ChatFinishReason.Stop : null,
		};

		if (response.ModelUsage is { } usage)
		{
			chatResponse.Usage = new UsageDetails
			{
				InputTokenCount = usage.InputTokens,
				OutputTokenCount = usage.OutputTokens,
				TotalTokenCount = usage.InputTokens + usage.OutputTokens
			};
		}

		return chatResponse;
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var messageList = messages?.ToList() ?? throw new ArgumentNullException(nameof(messages));
		var lastUserMessage = messageList.LastOrDefault(m => m.Role == ChatRole.User);
		var input = lastUserMessage?.Text ?? string.Empty;

		var conversationId = options?.ConversationId;

		await foreach (var evt in _client.ConverseStreamAsync(new ConverseRequest
		{
			Input = input,
			AgentId = _agentId,
			ConversationId = conversationId,
			ConnectorId = _connectorId
		}, cancellationToken).ConfigureAwait(false))
		{
			switch (evt)
			{
				case ConversationIdSetEvent idSet:
					conversationId = idSet.ConversationId;
					break;

				case MessageChunkEvent chunk:
					yield return new ChatResponseUpdate
					{
						Role = ChatRole.Assistant,
						Contents = [new TextContent(chunk.TextChunk)],
						ConversationId = conversationId,
						ModelId = _agentId,
					};
					break;

				case MessageCompleteEvent:
					yield return new ChatResponseUpdate
					{
						Role = ChatRole.Assistant,
						FinishReason = ChatFinishReason.Stop,
						ConversationId = conversationId,
						ModelId = _agentId,
					};
					break;
			}
		}
	}

	/// <inheritdoc />
	public object? GetService(Type serviceType, object? serviceKey = null)
	{
		if (serviceKey is null)
		{
			if (serviceType == typeof(ChatClientMetadata))
				return _metadata;

			if (serviceType == typeof(AgentBuilderClient))
				return _client;

			if (serviceType?.IsInstanceOfType(this) == true)
				return this;
		}

		return null;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}
}
