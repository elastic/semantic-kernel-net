// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Transport;

namespace Elastic.Clients.AgentBuilder.Conversations;

/// <summary>
/// Represents a conversation attachment as returned by the Agent Builder API.
/// </summary>
public class Attachment : TransportResponse
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = default!;

	[JsonPropertyName("type")]
	public string? Type { get; set; }

	[JsonPropertyName("description")]
	public string? Description { get; set; }

	[JsonPropertyName("origin")]
	public string? Origin { get; set; }

	[JsonPropertyName("hidden")]
	public bool? Hidden { get; set; }

	[JsonPropertyName("deleted")]
	public bool? Deleted { get; set; }

	[JsonPropertyName("version")]
	public int? Version { get; set; }

	[JsonPropertyName("data")]
	public JsonElement? Data { get; set; }
}

/// <summary>
/// Response wrapper for listing conversation attachments.
/// </summary>
public class ListAttachmentsResponse : TransportResponse
{
	[JsonPropertyName("results")]
	public IReadOnlyList<Attachment> Results { get; set; } = default!;
}
