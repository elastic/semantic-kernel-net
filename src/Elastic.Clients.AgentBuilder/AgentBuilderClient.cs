// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Transport;

namespace Elastic.Clients.AgentBuilder;

/// <summary>
/// Client for the Elastic Agent Builder Kibana API.
/// Provides CRUD operations for tools, agents, and conversations.
/// </summary>
public partial class AgentBuilderClient
{
	private readonly ITransport _transport;
	private readonly string _pathPrefix;

	/// <summary>
	/// Creates a new <see cref="AgentBuilderClient"/> from an <see cref="AgentTransportConfiguration"/>.
	/// This is the recommended constructor — it auto-wires the <c>kbn-xsrf</c> header and
	/// reads the <see cref="AgentTransportConfiguration.Space"/> for path prefixing.
	/// </summary>
	public AgentBuilderClient(AgentTransportConfiguration configuration)
		: this(new AgentTransport(configuration), configuration.Space)
	{
	}

	/// <summary>
	/// Creates a new <see cref="AgentBuilderClient"/> from a pre-configured <see cref="AgentTransport"/>.
	/// </summary>
	public AgentBuilderClient(AgentTransport transport)
		: this(transport, transport.AgentConfiguration.Space)
	{
	}

	/// <summary>
	/// Creates a new <see cref="AgentBuilderClient"/> from a raw <see cref="ITransport"/>.
	/// You are responsible for setting the <c>kbn-xsrf</c> header on the transport.
	/// </summary>
	/// <param name="transport">An <see cref="ITransport"/> configured for Kibana.</param>
	/// <param name="space">Optional Kibana space name. When set, all API paths are prefixed with <c>/s/{space}</c>.</param>
	public AgentBuilderClient(ITransport transport, string? space = null)
	{
		_transport = transport ?? throw new ArgumentNullException(nameof(transport));
		_pathPrefix = string.IsNullOrWhiteSpace(space)
			? "/api/agent_builder"
			: $"/s/{space}/api/agent_builder";
	}

	private string Path(string relative) => $"{_pathPrefix}{relative}";

	private async Task<TResponse> GetAsync<TResponse>(string path, CancellationToken ct)
		where TResponse : TransportResponse, new()
	{
		var response = await _transport
			.RequestAsync<TResponse>(HttpMethod.GET, Path(path), cancellationToken: ct)
			.ConfigureAwait(false);
		EnsureSuccess(response);
		return response;
	}

	private async Task<TResponse> PostAsync<TRequest, TResponse>(
		string path, TRequest body, CancellationToken ct)
		where TResponse : TransportResponse, new()
	{
		var response = await _transport
			.RequestAsync<TResponse>(HttpMethod.POST, Path(path),
				PostData.Serializable(body), cancellationToken: ct)
			.ConfigureAwait(false);
		EnsureSuccess(response);
		return response;
	}

	private async Task<TResponse> PutAsync<TRequest, TResponse>(
		string path, TRequest body, CancellationToken ct)
		where TResponse : TransportResponse, new()
	{
		var response = await _transport
			.RequestAsync<TResponse>(HttpMethod.PUT, Path(path),
				PostData.Serializable(body), cancellationToken: ct)
			.ConfigureAwait(false);
		EnsureSuccess(response);
		return response;
	}

	private static readonly RequestConfiguration SseRequestConfig = new() { Accept = "application/octet-stream" };

	internal async Task<StreamResponse> PostStreamAsync<TRequest>(
		string path, TRequest body, CancellationToken ct)
	{
		var response = await _transport
			.RequestAsync<StreamResponse>(HttpMethod.POST, Path(path),
				PostData.Serializable(body),
				localConfiguration: SseRequestConfig, cancellationToken: ct)
			.ConfigureAwait(false);
		EnsureSuccess(response);
		return response;
	}

	private async Task DeleteAsync(string path, CancellationToken ct)
	{
		var response = await _transport
			.RequestAsync<VoidResponse>(HttpMethod.DELETE, Path(path), cancellationToken: ct)
			.ConfigureAwait(false);
		EnsureSuccess(response);
	}

	private async Task<TResponse> PostEmptyAsync<TResponse>(string path, CancellationToken ct)
		where TResponse : TransportResponse, new()
	{
		var response = await _transport
			.RequestAsync<TResponse>(HttpMethod.POST, Path(path),
				PostData.Empty, cancellationToken: ct)
			.ConfigureAwait(false);
		EnsureSuccess(response);
		return response;
	}

	private static void EnsureSuccess(TransportResponse response)
	{
		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
		{
			var body = response.ApiCallDetails.ResponseBodyInBytes is { Length: > 0 } bytes
				? $": {Encoding.UTF8.GetString(bytes)}"
				: string.Empty;
			throw new AgentBuilderException(
				$"Agent Builder API returned {response.ApiCallDetails.HttpStatusCode}{body}",
				response.ApiCallDetails);
		}
	}
}

/// <summary>
/// Exception thrown when the Agent Builder API returns a non-success status code.
/// </summary>
public class AgentBuilderException : Exception
{
	public ApiCallDetails ApiCallDetails { get; }

	public AgentBuilderException(string message, ApiCallDetails apiCallDetails)
		: base(message) =>
		ApiCallDetails = apiCallDetails;
}
