// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.AgentBuilder.Conversations;

namespace Elastic.Clients.AgentBuilder;

public partial class AgentBuilderClient
{
	/// <summary> Send a synchronous chat message to an agent. </summary>
	public Task<ConverseResponse> ConverseAsync(ConverseRequest request, CancellationToken ct = default) =>
		PostAsync<ConverseRequest, ConverseResponse>("/converse", request, ct);

	/// <summary> Send a chat message and receive a stream of typed SSE events. </summary>
	public async IAsyncEnumerable<ConverseStreamEvent> ConverseStreamAsync(
		ConverseRequest request,
		[EnumeratorCancellation] CancellationToken ct = default)
	{
		using var response = await PostStreamAsync("/converse/async", request, ct)
			.ConfigureAwait(false);

		var parser = SseParser.Create(response.Body, ParseSseEvent);
		await foreach (var item in parser.EnumerateAsync(ct).ConfigureAwait(false))
		{
			if (item.Data is { } evt)
				yield return evt;
		}
	}

	/// <summary> List all conversations. </summary>
	public Task<ListConversationsResponse> ListConversationsAsync(CancellationToken ct = default) =>
		GetAsync<ListConversationsResponse>("/conversations", ct);

	/// <summary> Get a conversation by its ID. </summary>
	public Task<Conversation> GetConversationAsync(string conversationId, CancellationToken ct = default) =>
		GetAsync<Conversation>($"/conversations/{conversationId}", ct);

	/// <summary> Delete a conversation by its ID. </summary>
	public Task DeleteConversationAsync(string conversationId, CancellationToken ct = default) =>
		DeleteAsync($"/conversations/{conversationId}", ct);

	/// <summary> List all attachments for a conversation. </summary>
	public Task<ListAttachmentsResponse> ListAttachmentsAsync(
		string conversationId, bool includeDeleted = false, CancellationToken ct = default)
	{
		var path = includeDeleted
			? $"/conversations/{conversationId}/attachments?include_deleted=true"
			: $"/conversations/{conversationId}/attachments";
		return GetAsync<ListAttachmentsResponse>(path, ct);
	}

	/// <summary> Create a new attachment for a conversation. </summary>
	public Task<Attachment> CreateAttachmentAsync(
		string conversationId, CreateAttachmentRequest request, CancellationToken ct = default) =>
		PostAsync<CreateAttachmentRequest, Attachment>(
			$"/conversations/{conversationId}/attachments", request, ct);

	/// <summary> Update an attachment's content (creates a new version if content changed). </summary>
	public Task<Attachment> UpdateAttachmentAsync(
		string conversationId, string attachmentId,
		UpdateAttachmentRequest request, CancellationToken ct = default) =>
		PutAsync<UpdateAttachmentRequest, Attachment>(
			$"/conversations/{conversationId}/attachments/{attachmentId}", request, ct);

	/// <summary> Delete an attachment (soft delete by default). </summary>
	public Task DeleteAttachmentAsync(
		string conversationId, string attachmentId,
		bool permanent = false, CancellationToken ct = default)
	{
		var path = permanent
			? $"/conversations/{conversationId}/attachments/{attachmentId}?permanent=true"
			: $"/conversations/{conversationId}/attachments/{attachmentId}";
		return DeleteAsync(path, ct);
	}

	/// <summary> Restore a soft-deleted attachment. </summary>
	public Task<Attachment> RestoreAttachmentAsync(
		string conversationId, string attachmentId, CancellationToken ct = default) =>
		PostEmptyAsync<Attachment>(
			$"/conversations/{conversationId}/attachments/{attachmentId}/_restore", ct);

	/// <summary> Update the origin reference for an attachment. </summary>
	public Task<Attachment> UpdateAttachmentOriginAsync(
		string conversationId, string attachmentId,
		UpdateAttachmentOriginRequest request, CancellationToken ct = default) =>
		PutAsync<UpdateAttachmentOriginRequest, Attachment>(
			$"/conversations/{conversationId}/attachments/{attachmentId}/origin", request, ct);

	/// <summary>
	/// Parses the raw UTF-8 bytes of an SSE <c>data:</c> line directly into a typed
	/// <see cref="ConverseStreamEvent"/>. Kibana wraps every event payload in a
	/// <c>{"data": {…}}</c> envelope, so we use a <see cref="Utf8JsonReader"/> to
	/// navigate to the inner value and deserialize from there — single pass, zero
	/// intermediate string allocations, fully AOT-safe.
	/// </summary>
	private static ConverseStreamEvent? ParseSseEvent(string eventType, ReadOnlySpan<byte> data)
	{
		var reader = new Utf8JsonReader(data);

		if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
			return null;
		if (!reader.Read() || reader.TokenType != JsonTokenType.PropertyName)
			return null;
		if (!reader.ValueTextEquals("data"u8))
			return null;

		reader.Read();

		var ctx = AgentBuilderSerializationContext.Default;
		return eventType switch
		{
			"conversation_id_set" => JsonSerializer.Deserialize(ref reader, ctx.ConversationIdSetEvent),
			"conversation_created" => JsonSerializer.Deserialize(ref reader, ctx.ConversationCreatedEvent),
			"conversation_updated" => JsonSerializer.Deserialize(ref reader, ctx.ConversationUpdatedEvent),
			"reasoning" => JsonSerializer.Deserialize(ref reader, ctx.ReasoningEvent),
			"tool_call" => JsonSerializer.Deserialize(ref reader, ctx.ToolCallEvent),
			"tool_progress" => JsonSerializer.Deserialize(ref reader, ctx.ToolProgressEvent),
			"tool_result" => JsonSerializer.Deserialize(ref reader, ctx.ToolResultEvent),
			"message_chunk" => JsonSerializer.Deserialize(ref reader, ctx.MessageChunkEvent),
			"message_complete" => JsonSerializer.Deserialize(ref reader, ctx.MessageCompleteEvent),
			"thinking_complete" => JsonSerializer.Deserialize(ref reader, ctx.ThinkingCompleteEvent),
			"round_complete" => JsonSerializer.Deserialize(ref reader, ctx.RoundCompleteEvent),
			_ => null
		};
	}
}
