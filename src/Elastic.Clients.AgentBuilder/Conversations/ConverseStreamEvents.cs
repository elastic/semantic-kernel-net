// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elastic.Clients.AgentBuilder.Conversations;

/// <summary> Base type for all SSE events emitted by the <c>/converse/async</c> endpoint. </summary>
public abstract record ConverseStreamEvent;

/// <summary> Fired when the conversation ID is assigned. </summary>
public record ConversationIdSetEvent : ConverseStreamEvent
{
	[JsonPropertyName("conversation_id")]
	public required string ConversationId { get; init; }
}

/// <summary> Fired when a new conversation is persisted. </summary>
public record ConversationCreatedEvent : ConverseStreamEvent
{
	[JsonPropertyName("conversation_id")]
	public required string ConversationId { get; init; }

	[JsonPropertyName("title")]
	public string? Title { get; init; }
}

/// <summary> Fired when a conversation is updated. </summary>
public record ConversationUpdatedEvent : ConverseStreamEvent
{
	[JsonPropertyName("conversation_id")]
	public required string ConversationId { get; init; }

	[JsonPropertyName("title")]
	public string? Title { get; init; }
}

/// <summary> Agent reasoning content. </summary>
public record ReasoningEvent : ConverseStreamEvent
{
	[JsonPropertyName("reasoning")]
	public required string Reasoning { get; init; }

	[JsonPropertyName("transient")]
	public bool Transient { get; init; }
}

/// <summary> Fired when a tool is invoked. </summary>
public record ToolCallEvent : ConverseStreamEvent
{
	[JsonPropertyName("tool_call_id")]
	public required string ToolCallId { get; init; }

	[JsonPropertyName("tool_id")]
	public required string ToolId { get; init; }

	[JsonPropertyName("params")]
	public JsonElement? Params { get; init; }
}

/// <summary> Reports progress of a running tool. </summary>
public record ToolProgressEvent : ConverseStreamEvent
{
	[JsonPropertyName("tool_call_id")]
	public required string ToolCallId { get; init; }

	[JsonPropertyName("message")]
	public string? Message { get; init; }
}

/// <summary> Results from a completed tool call. </summary>
public record ToolResultEvent : ConverseStreamEvent
{
	[JsonPropertyName("tool_call_id")]
	public required string ToolCallId { get; init; }

	[JsonPropertyName("tool_id")]
	public required string ToolId { get; init; }

	[JsonPropertyName("results")]
	public IReadOnlyList<JsonElement>? Results { get; init; }
}

/// <summary> A partial text chunk streamed from the agent. </summary>
public record MessageChunkEvent : ConverseStreamEvent
{
	[JsonPropertyName("message_id")]
	public string? MessageId { get; init; }

	[JsonPropertyName("text_chunk")]
	public required string TextChunk { get; init; }
}

/// <summary> Signals that the full message has been delivered. </summary>
public record MessageCompleteEvent : ConverseStreamEvent
{
	[JsonPropertyName("message_id")]
	public string? MessageId { get; init; }

	[JsonPropertyName("message_content")]
	public string? MessageContent { get; init; }
}

/// <summary> Marks the end of the thinking/reasoning phase. </summary>
public record ThinkingCompleteEvent : ConverseStreamEvent
{
	[JsonPropertyName("time_to_first_token")]
	public int TimeToFirstToken { get; init; }
}

/// <summary> Marks the end of one conversation round. </summary>
public record RoundCompleteEvent : ConverseStreamEvent
{
	[JsonPropertyName("round")]
	public JsonElement? Round { get; init; }
}
