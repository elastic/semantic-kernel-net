// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Clients.AgentBuilder.Conversations;
using FluentAssertions;
using Xunit;

namespace Elastic.Clients.AgentBuilder.Tests.Serialization;

public class AttachmentSerializationTests
{
	private static readonly AgentBuilderSerializationContext Ctx = AgentBuilderSerializationContext.Default;

	[Fact]
	public void CreateAttachmentRequest_SerializesCorrectly()
	{
		var request = new CreateAttachmentRequest
		{
			Id = "att-123",
			Type = "text",
			Description = "My attachment",
			Origin = "saved-object-abc",
			Hidden = false
		};

		var json = JsonSerializer.Serialize(request, Ctx.CreateAttachmentRequest);
		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		root.GetProperty("id").GetString().Should().Be("att-123");
		root.GetProperty("type").GetString().Should().Be("text");
		root.GetProperty("description").GetString().Should().Be("My attachment");
		root.GetProperty("origin").GetString().Should().Be("saved-object-abc");
		root.GetProperty("hidden").GetBoolean().Should().BeFalse();
	}

	[Fact]
	public void CreateAttachmentRequest_OmitsNullProperties()
	{
		var request = new CreateAttachmentRequest
		{
			Type = "esql"
		};

		var json = JsonSerializer.Serialize(request, Ctx.CreateAttachmentRequest);
		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		root.GetProperty("type").GetString().Should().Be("esql");
		root.TryGetProperty("id", out _).Should().BeFalse();
		root.TryGetProperty("description", out _).Should().BeFalse();
		root.TryGetProperty("origin", out _).Should().BeFalse();
		root.TryGetProperty("hidden", out _).Should().BeFalse();
	}

	[Fact]
	public void UpdateAttachmentRequest_SerializesCorrectly()
	{
		var request = new UpdateAttachmentRequest
		{
			Description = "Updated description"
		};

		var json = JsonSerializer.Serialize(request, Ctx.UpdateAttachmentRequest);
		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		root.GetProperty("description").GetString().Should().Be("Updated description");
	}

	[Fact]
	public void UpdateAttachmentOriginRequest_SerializesCorrectly()
	{
		var request = new UpdateAttachmentOriginRequest
		{
			Origin = "saved-object-xyz"
		};

		var json = JsonSerializer.Serialize(request, Ctx.UpdateAttachmentOriginRequest);
		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		root.GetProperty("origin").GetString().Should().Be("saved-object-xyz");
	}

	[Fact]
	public void Attachment_DeserializesFromApi()
	{
		var json = """
		{
			"id": "att-001",
			"type": "visualization",
			"description": "Sales dashboard",
			"origin": "saved-object-123",
			"hidden": false,
			"deleted": false,
			"version": 3,
			"data": { "title": "Sales Q1" }
		}
		""";

		var attachment = JsonSerializer.Deserialize(json, Ctx.Attachment);

		attachment.Should().NotBeNull();
		attachment!.Id.Should().Be("att-001");
		attachment.Type.Should().Be("visualization");
		attachment.Description.Should().Be("Sales dashboard");
		attachment.Origin.Should().Be("saved-object-123");
		attachment.Hidden.Should().BeFalse();
		attachment.Deleted.Should().BeFalse();
		attachment.Version.Should().Be(3);
		attachment.Data.Should().NotBeNull();
		attachment.Data!.Value.GetProperty("title").GetString().Should().Be("Sales Q1");
	}

	[Fact]
	public void Attachment_DeserializesDeletedAttachment()
	{
		var json = """
		{
			"id": "att-deleted",
			"type": "text",
			"description": "Removed",
			"deleted": true,
			"version": 1
		}
		""";

		var attachment = JsonSerializer.Deserialize(json, Ctx.Attachment);

		attachment.Should().NotBeNull();
		attachment!.Deleted.Should().BeTrue();
		attachment.Origin.Should().BeNull();
	}

	[Fact]
	public void ListAttachmentsResponse_DeserializesFromApi()
	{
		var json = """
		{
			"results": [
				{
					"id": "att-a",
					"type": "text",
					"description": "First",
					"version": 1
				},
				{
					"id": "att-b",
					"type": "esql",
					"description": "Second",
					"hidden": true,
					"version": 2
				}
			]
		}
		""";

		var response = JsonSerializer.Deserialize(json, Ctx.ListAttachmentsResponse);

		response.Should().NotBeNull();
		response!.Results.Should().HaveCount(2);
		response.Results[0].Id.Should().Be("att-a");
		response.Results[1].Hidden.Should().BeTrue();
	}
}
