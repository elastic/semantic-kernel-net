// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Clients.AgentBuilder.Plugins;
using FluentAssertions;
using Xunit;

namespace Elastic.Clients.AgentBuilder.Tests.Serialization;

public class PluginSerializationTests
{
	private static readonly AgentBuilderSerializationContext Ctx = AgentBuilderSerializationContext.Default;

	[Fact]
	public void InstallPluginRequest_SerializesCorrectly()
	{
		var request = new InstallPluginRequest
		{
			Url = "https://github.com/example/my-plugin",
			PluginName = "my-custom-plugin"
		};

		var json = JsonSerializer.Serialize(request, Ctx.InstallPluginRequest);
		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		root.GetProperty("url").GetString().Should().Be("https://github.com/example/my-plugin");
		root.GetProperty("plugin_name").GetString().Should().Be("my-custom-plugin");
	}

	[Fact]
	public void InstallPluginRequest_OmitsNullPluginName()
	{
		var request = new InstallPluginRequest
		{
			Url = "https://github.com/example/my-plugin"
		};

		var json = JsonSerializer.Serialize(request, Ctx.InstallPluginRequest);
		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		root.GetProperty("url").GetString().Should().Be("https://github.com/example/my-plugin");
		root.TryGetProperty("plugin_name", out _).Should().BeFalse();
	}

	[Fact]
	public void AgentBuilderPlugin_DeserializesFromApi()
	{
		var json = """
		{
			"id": "example-plugin",
			"name": "Example Plugin",
			"description": "An example plugin",
			"url": "https://github.com/example/plugin",
			"managed_assets": {
				"skills": ["skill-from-plugin"],
				"tools": ["tool-from-plugin"]
			}
		}
		""";

		var plugin = JsonSerializer.Deserialize(json, Ctx.AgentBuilderPlugin);

		plugin.Should().NotBeNull();
		plugin!.Id.Should().Be("example-plugin");
		plugin.Name.Should().Be("Example Plugin");
		plugin.Description.Should().Be("An example plugin");
		plugin.Url.Should().Be("https://github.com/example/plugin");
		plugin.ManagedAssets.Should().NotBeNull();
		plugin.ManagedAssets!.Skills.Should().ContainSingle().Which.Should().Be("skill-from-plugin");
		plugin.ManagedAssets.Tools.Should().ContainSingle().Which.Should().Be("tool-from-plugin");
	}

	[Fact]
	public void AgentBuilderPlugin_DeserializesMinimalResponse()
	{
		var json = """
		{
			"id": "minimal-plugin",
			"name": "Minimal"
		}
		""";

		var plugin = JsonSerializer.Deserialize(json, Ctx.AgentBuilderPlugin);

		plugin.Should().NotBeNull();
		plugin!.Id.Should().Be("minimal-plugin");
		plugin.ManagedAssets.Should().BeNull();
	}

	[Fact]
	public void ListPluginsResponse_DeserializesFromApi()
	{
		var json = """
		{
			"results": [
				{
					"id": "plugin-a",
					"name": "Plugin A",
					"managed_assets": { "skills": ["s1", "s2"] }
				},
				{
					"id": "plugin-b",
					"name": "Plugin B"
				}
			]
		}
		""";

		var response = JsonSerializer.Deserialize(json, Ctx.ListPluginsResponse);

		response.Should().NotBeNull();
		response!.Results.Should().HaveCount(2);
		response.Results[0].ManagedAssets!.Skills.Should().HaveCount(2);
		response.Results[1].ManagedAssets.Should().BeNull();
	}
}
