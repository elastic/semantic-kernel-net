// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Clients.AgentBuilder.Plugins;

/// <summary>
/// Request to install a plugin from a URL.
/// </summary>
public record InstallPluginRequest
{
	[JsonPropertyName("url")]
	public required string Url { get; init; }

	[JsonPropertyName("plugin_name")]
	public string? PluginName { get; init; }
}
