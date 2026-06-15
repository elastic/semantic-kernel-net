// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Transport;

namespace Elastic.Clients.AgentBuilder;

/// <summary>
/// Configuration for connecting to the Kibana Agent Builder API.
/// Automatically injects the <c>kbn-xsrf</c> header and supports Kibana Spaces.
/// </summary>
public class AgentTransportConfiguration
{
	private static readonly NameValueCollection KibanaHeaders = new() { { "kbn-xsrf", "true" } };

	private readonly Func<TransportConfigurationDescriptor> _factory;

	/// <summary> Optional Kibana space name. All API paths will be prefixed with <c>/s/{Space}</c>. </summary>
	public string? Space { get; set; }

	/// <summary>
	/// Connect to Kibana via an Elastic Cloud ID with an API key.
	/// The Cloud ID is parsed to resolve the Kibana service URL automatically.
	/// </summary>
	public AgentTransportConfiguration(string cloudId, ApiKey credentials) =>
		_factory = () => Descriptor(new CloudNodePool(cloudId, credentials, CloudService.Kibana));

	/// <summary>
	/// Connect to Kibana via an Elastic Cloud ID with basic credentials.
	/// </summary>
	public AgentTransportConfiguration(string cloudId, BasicAuthentication credentials) =>
		_factory = () => Descriptor(new CloudNodePool(cloudId, credentials, CloudService.Kibana));

	/// <summary>
	/// Connect to a Kibana instance at the given URL with an API key.
	/// </summary>
	public AgentTransportConfiguration(Uri kibanaUri, ApiKey credentials) =>
		_factory = () => Descriptor(new SingleNodePool(kibanaUri))
			.Authentication(credentials);

	/// <summary>
	/// Connect to a Kibana instance at the given URL with basic credentials.
	/// </summary>
	public AgentTransportConfiguration(Uri kibanaUri, BasicAuthentication credentials) =>
		_factory = () => Descriptor(new SingleNodePool(kibanaUri))
			.Authentication(credentials);

	private static TransportConfigurationDescriptor Descriptor(NodePool pool) =>
		new TransportConfigurationDescriptor(pool, null, AgentBuilderSerializer.Instance)
			.GlobalHeaders(KibanaHeaders);

	internal ITransportConfiguration CreateTransportConfiguration() => _factory();
}

/// <summary>
/// An <see cref="ITransport"/> pre-configured for the Kibana Agent Builder API.
/// Wraps a <see cref="DistributedTransport"/> created from an <see cref="AgentTransportConfiguration"/>.
/// </summary>
public class AgentTransport(AgentTransportConfiguration configuration) : ITransport
{
	private readonly DistributedTransport _inner = new(configuration.CreateTransportConfiguration());

	/// <summary> The configuration used to create this transport. </summary>
	public AgentTransportConfiguration AgentConfiguration { get; } = configuration ?? throw new ArgumentNullException(nameof(configuration));

	/// <inheritdoc />
	public ITransportConfiguration Configuration => _inner.Configuration;

	/// <inheritdoc />
	public TResponse Request<TResponse>(in EndpointPath path, PostData? postData = null,
		Action<Activity>? configureActivity = null, IRequestConfiguration? localConfiguration = null)
		where TResponse : TransportResponse, new() =>
		_inner.Request<TResponse>(in path, postData, configureActivity, localConfiguration);

	/// <inheritdoc />
	public Task<TResponse> RequestAsync<TResponse>(in EndpointPath path, PostData? postData = null,
		Action<Activity>? configureActivity = null, IRequestConfiguration? localConfiguration = null,
		CancellationToken cancellationToken = default)
		where TResponse : TransportResponse, new() =>
		_inner.RequestAsync<TResponse>(in path, postData, configureActivity, localConfiguration, cancellationToken);
}
