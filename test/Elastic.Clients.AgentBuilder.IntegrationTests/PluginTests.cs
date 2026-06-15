// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Elastic.Clients.AgentBuilder.IntegrationTests;

public class PluginTests : AgentBuilderTestBase
{
	[Fact]
	public async Task CanListPlugins()
	{
		try
		{
			var response = await Client.ListPluginsAsync();
			response.Should().NotBeNull();
			response.Results.Should().NotBeNull();
		}
		catch (AgentBuilderException ex) when (ex.ApiCallDetails.HttpStatusCode == 404)
		{
			// Plugins API not available (requires Kibana 9.4.0+) — skip gracefully
		}
	}
}
