// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Transport;
using Microsoft.Extensions.Configuration;

namespace Elastic.Clients.AgentBuilder.IntegrationTests;

/// <summary>
/// Base class for Agent Builder integration tests.
/// Reads Kibana connection details from user secrets.
/// </summary>
public abstract class AgentBuilderTestBase : IDisposable
{
	protected AgentBuilderClient Client { get; }
	protected string? CloudId { get; }

	protected AgentBuilderTestBase()
	{
		var config = new ConfigurationBuilder()
			.AddUserSecrets(typeof(AgentBuilderTestBase).Assembly, optional: true)
			.Build();

		var kibanaUrl = config["Parameters:KibanaUrl"];
		var apiKey = config["Parameters:KibanaApiKey"];
		CloudId = config["Parameters:CloudId"];
		var space = config["Parameters:KibanaSpace"];

		AgentTransportConfiguration transportConfig;
		if (!string.IsNullOrEmpty(CloudId) && !string.IsNullOrEmpty(apiKey))
		{
			transportConfig = new AgentTransportConfiguration(CloudId, new ApiKey(apiKey))
			{
				Space = space
			};
		}
		else if (!string.IsNullOrEmpty(kibanaUrl) && !string.IsNullOrEmpty(apiKey))
		{
			transportConfig = new AgentTransportConfiguration(new Uri(kibanaUrl), new ApiKey(apiKey))
			{
				Space = space
			};
		}
		else
		{
			throw new InvalidOperationException(
				"Integration tests require user secrets. Run from tests/Elastic.Clients.AgentBuilder.IntegrationTests:\n" +
				"\n" +
				"  dotnet user-secrets set \"Parameters:CloudId\" \"<cloud-id>\"\n" +
				"  dotnet user-secrets set \"Parameters:KibanaApiKey\" \"<base64-api-key>\"\n" +
				"\n" +
				"Or use a direct Kibana URL instead of CloudId:\n" +
				"\n" +
				"  dotnet user-secrets set \"Parameters:KibanaUrl\" \"https://my-kibana:5601\"\n" +
				"  dotnet user-secrets set \"Parameters:KibanaApiKey\" \"<base64-api-key>\"\n" +
				"\n" +
				"Optional: dotnet user-secrets set \"Parameters:KibanaSpace\" \"<space-name>\"");
		}

		Client = new AgentBuilderClient(transportConfig);
	}

	public virtual void Dispose() => GC.SuppressFinalize(this);
}
