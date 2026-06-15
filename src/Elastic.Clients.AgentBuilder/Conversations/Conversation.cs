// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Transport;

namespace Elastic.Clients.AgentBuilder.Conversations;

/// <summary>
/// Represents a conversation as returned by the Agent Builder API.
/// </summary>
public class Conversation : TransportResponse
{
	[JsonPropertyName("id")]
	public string ConversationId { get; set; } = default!;

	[JsonPropertyName("title")]
	public string? Title { get; set; }

	[JsonPropertyName("agent_id")]
	public string? AgentId { get; set; }

	[JsonPropertyName("rounds")]
	public IReadOnlyList<JsonElement>? Rounds { get; set; }
}

/// <summary>
/// Response wrapper for listing conversations.
/// </summary>
public class ListConversationsResponse : TransportResponse
{
	[JsonPropertyName("results")]
	public IReadOnlyList<Conversation> Results { get; set; } = default!;
}
