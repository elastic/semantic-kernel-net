// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Clients.AgentBuilder.Tools;
using Elastic.Transport;
using FluentAssertions;
using Xunit;

namespace Elastic.Clients.AgentBuilder.Tests;

public class BootstrapHashTests
{
	[Fact]
	public void SameDefinition_ProducesSameHash()
	{
		var config = new TransportConfiguration();

		var request = CreateSampleRequest();
		var hash1 = AgentBuilderBootstrapper.ComputeHash(request, AgentBuilderSerializationContext.Default.CreateEsqlToolRequest);
		var hash2 = AgentBuilderBootstrapper.ComputeHash(request, AgentBuilderSerializationContext.Default.CreateEsqlToolRequest);

		hash1.Should().Be(hash2);
	}

	[Fact]
	public void DifferentDefinition_ProducesDifferentHash()
	{
		var request1 = CreateSampleRequest();
		var request2 = request1 with { Description = "Changed description" };

		var hash1 = AgentBuilderBootstrapper.ComputeHash(request1, AgentBuilderSerializationContext.Default.CreateEsqlToolRequest);
		var hash2 = AgentBuilderBootstrapper.ComputeHash(request2, AgentBuilderSerializationContext.Default.CreateEsqlToolRequest);

		hash1.Should().NotBe(hash2);
	}

	[Fact]
	public void Hash_IsReasonableLength()
	{
		var request = CreateSampleRequest();
		var hash = AgentBuilderBootstrapper.ComputeHash(request, AgentBuilderSerializationContext.Default.CreateEsqlToolRequest);

		hash.Should().NotBeNullOrWhiteSpace();
		hash.Length.Should().Be(16);
	}

	private static CreateEsqlToolRequest CreateSampleRequest() =>
		new()
		{
			Id = "test-tool",
			Description = "Test tool",
			Configuration = new EsqlToolConfiguration
			{
				Query = "FROM test | LIMIT 10",
				Params = new Dictionary<string, EsqlToolParam>
				{
					["limit"] = new() { Type = "integer", Description = "Max" }
				}
			}
		};
}
