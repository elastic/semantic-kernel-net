// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Clients.AgentBuilder.Conversations;

/// <summary>
/// Request to create a new conversation attachment.
/// </summary>
public record CreateAttachmentRequest
{
	[JsonPropertyName("id")]
	public string? Id { get; init; }

	[JsonPropertyName("type")]
	public string? Type { get; init; }

	[JsonPropertyName("description")]
	public string? Description { get; init; }

	[JsonPropertyName("origin")]
	public string? Origin { get; init; }

	[JsonPropertyName("hidden")]
	public bool? Hidden { get; init; }
}

/// <summary>
/// Request to update a conversation attachment's content (creates a new version if content changed).
/// </summary>
public record UpdateAttachmentRequest
{
	[JsonPropertyName("description")]
	public string? Description { get; init; }
}

/// <summary>
/// Request to update the origin reference for an attachment.
/// </summary>
public record UpdateAttachmentOriginRequest
{
	[JsonPropertyName("origin")]
	public required string Origin { get; init; }
}
