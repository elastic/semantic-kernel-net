// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Clients.AgentBuilder.Conversations;

/// <summary>
/// Request to send a synchronous chat message to an agent.
/// </summary>
public record ConverseRequest
{
	[JsonPropertyName("input")]
	public required string Input { get; init; }

	[JsonPropertyName("agent_id")]
	public string? AgentId { get; init; }

	[JsonPropertyName("conversation_id")]
	public string? ConversationId { get; init; }

	[JsonPropertyName("connector_id")]
	public string? ConnectorId { get; init; }
}
