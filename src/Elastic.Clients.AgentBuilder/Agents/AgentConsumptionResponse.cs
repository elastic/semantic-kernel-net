// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Transport;

namespace Elastic.Clients.AgentBuilder.Agents;

/// <summary>
/// Response containing paginated consumption data for an agent.
/// </summary>
public class AgentConsumptionResponse : TransportResponse
{
	[JsonPropertyName("results")]
	public IReadOnlyList<ConversationConsumption> Results { get; set; } = default!;

	[JsonPropertyName("search_after")]
	public IReadOnlyList<JsonElement>? SearchAfter { get; set; }
}

/// <summary>
/// Token consumption data for a single conversation.
/// </summary>
public record ConversationConsumption
{
	[JsonPropertyName("conversation_id")]
	public string ConversationId { get; init; } = default!;

	[JsonPropertyName("title")]
	public string? Title { get; init; }

	[JsonPropertyName("username")]
	public string? Username { get; init; }

	[JsonPropertyName("input_tokens")]
	public long InputTokens { get; init; }

	[JsonPropertyName("output_tokens")]
	public long OutputTokens { get; init; }

	[JsonPropertyName("round_count")]
	public int RoundCount { get; init; }

	[JsonPropertyName("llm_call_count")]
	public int LlmCallCount { get; init; }

	[JsonPropertyName("has_warnings")]
	public bool HasWarnings { get; init; }
}
