// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace Elastic.Extensions.AI;

/// <summary>
/// Factory for creating an MCP client connected to the Kibana Agent Builder MCP server.
/// The returned <see cref="McpClient"/> exposes tools as <see cref="McpClientTool"/> instances
/// that implement <c>AIFunction</c> from <c>Microsoft.Extensions.AI</c>, making them
/// immediately usable with any <c>IChatClient</c>.
/// </summary>
public static class AgentBuilderMcp
{
	/// <summary>
	/// Creates an <see cref="McpClient"/> connected to the Kibana Agent Builder MCP endpoint.
	/// </summary>
	/// <param name="kibanaUri">The Kibana base URL.</param>
	/// <param name="apiKey">The API key for authentication (base64-encoded).</param>
	/// <param name="space">Optional Kibana space name.</param>
	/// <param name="namespaceFilter">Optional comma-separated namespace filter for tools.</param>
	/// <param name="loggerFactory">Optional logger factory for MCP diagnostics.</param>
	/// <param name="ct">Cancellation token.</param>
	public static async Task<McpClient> CreateClientAsync(
		Uri kibanaUri,
		string apiKey,
		string? space = null,
		string? namespaceFilter = null,
		ILoggerFactory? loggerFactory = null,
		CancellationToken ct = default)
	{
		var mcpPath = string.IsNullOrWhiteSpace(space)
			? "/api/agent_builder/mcp"
			: $"/s/{space}/api/agent_builder/mcp";

		if (!string.IsNullOrWhiteSpace(namespaceFilter))
			mcpPath += $"?namespace={Uri.EscapeDataString(namespaceFilter)}";

		var endpoint = new Uri(kibanaUri, mcpPath);

		var headers = new Dictionary<string, string>
		{
			["Authorization"] = $"ApiKey {apiKey}",
			["kbn-xsrf"] = "true"
		};

		var transport = new HttpClientTransport(new HttpClientTransportOptions
		{
			Endpoint = endpoint,
			Name = "Elastic Agent Builder MCP",
			AdditionalHeaders = headers,
		});

		return await McpClient.CreateAsync(transport, loggerFactory: loggerFactory, cancellationToken: ct)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Creates an <see cref="McpClient"/> connected to the Kibana Agent Builder MCP endpoint
	/// using a Cloud ID to resolve the Kibana URL.
	/// </summary>
	/// <param name="cloudId">The Elastic Cloud ID.</param>
	/// <param name="apiKey">The API key for authentication (base64-encoded).</param>
	/// <param name="space">Optional Kibana space name.</param>
	/// <param name="namespaceFilter">Optional comma-separated namespace filter for tools.</param>
	/// <param name="loggerFactory">Optional logger factory for MCP diagnostics.</param>
	/// <param name="ct">Cancellation token.</param>
	public static Task<McpClient> CreateClientFromCloudIdAsync(
		string cloudId,
		string apiKey,
		string? space = null,
		string? namespaceFilter = null,
		ILoggerFactory? loggerFactory = null,
		CancellationToken ct = default)
	{
		var kibanaUri = ResolveKibanaUriFromCloudId(cloudId);
		return CreateClientAsync(kibanaUri, apiKey, space, namespaceFilter, loggerFactory, ct);
	}

	private static Uri ResolveKibanaUriFromCloudId(string cloudId)
	{
		var colonIndex = cloudId.IndexOf(':');
		if (colonIndex < 0)
			throw new ArgumentException("Invalid cloud ID format. Expected 'name:base64data'.", nameof(cloudId));

		var base64 = cloudId.Substring(colonIndex + 1);
		var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
		var parts = decoded.Split('$');
		if (parts.Length < 3 || string.IsNullOrWhiteSpace(parts[2]))
			throw new ArgumentException("Cloud ID does not contain a Kibana UUID.", nameof(cloudId));

		var host = parts[0].Trim();
		var hostPortSep = host.IndexOf(':');
		string defaultPort;
		if (hostPortSep >= 0)
		{
			defaultPort = host.Substring(hostPortSep + 1);
			host = host.Substring(0, hostPortSep);
		}
		else
		{
			defaultPort = "443";
		}

		var kbRaw = parts[2].Trim();
		var kbPortSep = kbRaw.IndexOf(':');
		string kbId, kbPort;
		if (kbPortSep >= 0)
		{
			kbId = kbRaw.Substring(0, kbPortSep);
			kbPort = kbRaw.Substring(kbPortSep + 1);
		}
		else
		{
			kbId = kbRaw;
			kbPort = defaultPort;
		}

		return kbPort == "443"
			? new Uri($"https://{kbId}.{host}")
			: new Uri($"https://{kbId}.{host}:{kbPort}");
	}
}
