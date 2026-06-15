// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using ModelContextProtocol.Client;
using Xunit;

namespace Elastic.Extensions.AI.IntegrationTests;

/// <summary>
/// Integration tests for <see cref="AgentBuilderMcp"/>.
/// These require a running Kibana instance with MCP enabled.
/// </summary>
public class McpClientTests(KibanaFixture fixture) : IClassFixture<KibanaFixture>
{
	[Fact]
	public async Task CreateClientAsync_CanConnect_AndListTools()
	{
		var kibanaUri = ResolveKibanaBaseUri();
		if (kibanaUri is null)
			return;

		await using var mcpClient = await AgentBuilderMcp.CreateClientAsync(
			kibanaUri, fixture.ApiKey, fixture.TransportConfiguration.Space);

		var tools = await mcpClient.ListToolsAsync();

		tools.Should().NotBeNull();
		tools.Should().NotBeEmpty("Kibana should expose at least one MCP tool");
	}

	[Fact]
	public async Task CreateClientFromCloudIdAsync_CanConnect_WhenCloudIdAvailable()
	{
		if (string.IsNullOrEmpty(fixture.CloudId))
			return;

		await using var mcpClient = await AgentBuilderMcp.CreateClientFromCloudIdAsync(
			fixture.CloudId, fixture.ApiKey, fixture.TransportConfiguration.Space);

		var tools = await mcpClient.ListToolsAsync();

		tools.Should().NotBeNull();
		tools.Should().NotBeEmpty();
	}

	[Fact]
	public async Task McpTools_HaveNameAndDescription()
	{
		var kibanaUri = ResolveKibanaBaseUri();
		if (kibanaUri is null)
			return;

		await using var mcpClient = await AgentBuilderMcp.CreateClientAsync(
			kibanaUri, fixture.ApiKey, fixture.TransportConfiguration.Space);

		var tools = await mcpClient.ListToolsAsync();
		tools.Should().NotBeEmpty();

		var tool = tools.First();
		tool.Name.Should().NotBeNullOrWhiteSpace();
		tool.Description.Should().NotBeNullOrWhiteSpace();
	}

	private Uri? ResolveKibanaBaseUri()
	{
		if (!string.IsNullOrEmpty(fixture.KibanaUrl))
			return new Uri(fixture.KibanaUrl);

		if (!string.IsNullOrEmpty(fixture.CloudId))
			return ResolveKibanaUriFromCloudId(fixture.CloudId);

		return null;
	}

	private static Uri ResolveKibanaUriFromCloudId(string cloudId)
	{
		var colonIndex = cloudId.IndexOf(':');
		var base64 = cloudId.Substring(colonIndex + 1);
		var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
		var parts = decoded.Split('$');

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
