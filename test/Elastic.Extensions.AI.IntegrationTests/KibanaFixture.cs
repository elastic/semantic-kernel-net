// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Clients.AgentBuilder;
using Elastic.Clients.AgentBuilder.Agents;
using Elastic.Transport;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace Elastic.Extensions.AI.IntegrationTests;

/// <summary>
/// Assembly-wide fixture for Elastic.Extensions.AI integration tests.
/// Reads Kibana connection details from shared user secrets, bootstraps a
/// temporary test agent on first use, and tears it down when disposed.
/// </summary>
public class KibanaFixture : IDisposable
{
	private const string TestAgentId = "dotnet-extensions-ai-test-agent";

	public AgentBuilderClient AgentClient { get; }
	public IChatClient ChatClient { get; }
	public AgentTransportConfiguration TransportConfiguration { get; }
	public string AgentId => TestAgentId;
	public string? CloudId { get; }
	public string? KibanaUrl { get; }
	public string ApiKey { get; }

	public KibanaFixture()
	{
		var config = new ConfigurationBuilder()
			.AddUserSecrets(typeof(KibanaFixture).Assembly, optional: true)
			.Build();

		KibanaUrl = config["Parameters:KibanaUrl"];
		ApiKey = config["Parameters:KibanaApiKey"]
			?? throw new InvalidOperationException(MissingSecretsMessage);
		CloudId = config["Parameters:CloudId"];
		var space = config["Parameters:KibanaSpace"];

		if (!string.IsNullOrEmpty(CloudId))
		{
			TransportConfiguration = new AgentTransportConfiguration(CloudId, new ApiKey(ApiKey))
			{
				Space = space
			};
		}
		else if (!string.IsNullOrEmpty(KibanaUrl))
		{
			TransportConfiguration = new AgentTransportConfiguration(new Uri(KibanaUrl), new ApiKey(ApiKey))
			{
				Space = space
			};
		}
		else
		{
			throw new InvalidOperationException(MissingSecretsMessage);
		}

		AgentClient = new AgentBuilderClient(TransportConfiguration);
		EnsureTestAgent();
		ChatClient = new ElasticAgentChatClient(AgentClient, AgentId);
	}

	private void EnsureTestAgent()
	{
		try
		{
			AgentClient.GetAgentAsync(TestAgentId).GetAwaiter().GetResult();
			return;
		}
		catch
		{
			// agent doesn't exist yet — create it
		}

		try
		{
			AgentClient.CreateAgentAsync(new CreateAgentRequest
			{
				Id = TestAgentId,
				Name = "Extensions.AI Integration Test Agent",
				Description = "Auto-created for integration tests — safe to delete",
				Labels = ["integration-test"],
				Configuration = new AgentConfiguration
				{
					Instructions = "You are a helpful test assistant. Keep answers short.",
					Tools = []
				}
			}).GetAwaiter().GetResult();
		}
		catch (AgentBuilderException ex) when (ex.Message.Contains("already exists"))
		{
			// race condition guard
		}
	}

	private const string MissingSecretsMessage =
		"Integration tests require user secrets. Run from any test project directory:\n" +
		"\n" +
		"  dotnet user-secrets set \"Parameters:CloudId\" \"<cloud-id>\"\n" +
		"  dotnet user-secrets set \"Parameters:KibanaApiKey\" \"<base64-api-key>\"\n" +
		"\n" +
		"Or use a direct Kibana URL instead of CloudId:\n" +
		"\n" +
		"  dotnet user-secrets set \"Parameters:KibanaUrl\" \"https://my-kibana:5601\"\n" +
		"  dotnet user-secrets set \"Parameters:KibanaApiKey\" \"<base64-api-key>\"\n" +
		"\n" +
		"Optional:\n" +
		"  dotnet user-secrets set \"Parameters:KibanaSpace\" \"<space-name>\"";

	public void Dispose()
	{
		try { AgentClient.DeleteAgentAsync(TestAgentId).GetAwaiter().GetResult(); }
		catch { /* best-effort cleanup */ }
		GC.SuppressFinalize(this);
	}
}
