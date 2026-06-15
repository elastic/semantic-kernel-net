// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Transport;

namespace Elastic.Clients.AgentBuilder.Conversations;

/// <summary>
/// Synchronous response from the converse API.
/// </summary>
public class ConverseResponse : TransportResponse
{
	[JsonPropertyName("conversation_id")]
	public string ConversationId { get; set; } = default!;

	[JsonPropertyName("round_id")]
	public string? RoundId { get; set; }

	[JsonPropertyName("status")]
	public string? Status { get; set; }

	[JsonPropertyName("steps")]
	public IReadOnlyList<ConverseStep>? Steps { get; set; }

	[JsonPropertyName("model_usage")]
	public ModelUsage? ModelUsage { get; set; }

	[JsonPropertyName("response")]
	public ConverseMessage? Response { get; set; }
}

/// <summary>
/// A step in the agent's processing of a conversation round.
/// </summary>
public record ConverseStep
{
	[JsonPropertyName("type")]
	public required string Type { get; init; }

	[JsonPropertyName("reasoning")]
	public string? Reasoning { get; init; }

	[JsonPropertyName("tool_id")]
	public string? ToolId { get; init; }

	[JsonPropertyName("params")]
	public JsonElement? Params { get; init; }

	[JsonPropertyName("results")]
	public IReadOnlyList<JsonElement>? Results { get; init; }
}

/// <summary>
/// The agent's response message.
/// </summary>
public record ConverseMessage
{
	[JsonPropertyName("message")]
	public required string Message { get; init; }
}

/// <summary>
/// Token usage information for a conversation round.
/// </summary>
public record ModelUsage
{
	[JsonPropertyName("input_tokens")]
	public int InputTokens { get; init; }

	[JsonPropertyName("output_tokens")]
	public int OutputTokens { get; init; }
}
