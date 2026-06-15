// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using System.Threading.Tasks;
using Elastic.Clients.AgentBuilder;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elastic.Extensions.AI.IntegrationTests;

public class DependencyInjectionTests(KibanaFixture fixture) : IClassFixture<KibanaFixture>
{
	[Fact]
	public void AddElasticAgentBuilder_RegistersBothServices()
	{
		var services = new ServiceCollection();
		services.AddElasticAgentBuilder(fixture.TransportConfiguration, fixture.AgentId);

		var provider = services.BuildServiceProvider();

		provider.GetService<AgentBuilderClient>().Should().NotBeNull();
		provider.GetService<IChatClient>().Should().NotBeNull();
	}

	[Fact]
	public void AddElasticAgentBuilder_RegistersSingletons()
	{
		var services = new ServiceCollection();
		services.AddElasticAgentBuilder(fixture.TransportConfiguration, fixture.AgentId);

		var provider = services.BuildServiceProvider();

		var client1 = provider.GetService<AgentBuilderClient>();
		var client2 = provider.GetService<AgentBuilderClient>();
		client1.Should().BeSameAs(client2);

		var chat1 = provider.GetService<IChatClient>();
		var chat2 = provider.GetService<IChatClient>();
		chat1.Should().BeSameAs(chat2);
	}

	[Fact]
	public async Task AddElasticAgentBuilder_ChatClientCanConverse()
	{
		var services = new ServiceCollection();
		services.AddElasticAgentBuilder(fixture.TransportConfiguration, fixture.AgentId);

		var provider = services.BuildServiceProvider();
		var chatClient = provider.GetRequiredService<IChatClient>();

		var response = await chatClient.GetResponseAsync("Say hello in one word.");

		response.Should().NotBeNull();
		response.Messages.Should().NotBeEmpty();
		response.Messages.First(m => m.Role == ChatRole.Assistant).Text
			.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public void AddElasticAgentBuilder_WithRawTransport_RegistersBothServices()
	{
		var services = new ServiceCollection();
		var transport = new AgentTransport(fixture.TransportConfiguration);
		services.AddElasticAgentBuilder(
			transport, fixture.AgentId, fixture.TransportConfiguration.Space);

		var provider = services.BuildServiceProvider();

		provider.GetService<AgentBuilderClient>().Should().NotBeNull();
		provider.GetService<IChatClient>().Should().NotBeNull();
	}
}
