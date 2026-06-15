// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.AgentBuilder.Plugins;

namespace Elastic.Clients.AgentBuilder;

public partial class AgentBuilderClient
{
	/// <summary> List all installed plugins. </summary>
	public Task<ListPluginsResponse> ListPluginsAsync(CancellationToken ct = default) =>
		GetAsync<ListPluginsResponse>("/plugins", ct);

	/// <summary> Get a plugin by its ID. </summary>
	public Task<AgentBuilderPlugin> GetPluginAsync(string pluginId, CancellationToken ct = default) =>
		GetAsync<AgentBuilderPlugin>($"/plugins/{pluginId}", ct);

	/// <summary> Install a plugin from a URL. </summary>
	public Task<AgentBuilderPlugin> InstallPluginAsync(InstallPluginRequest request, CancellationToken ct = default) =>
		PostAsync<InstallPluginRequest, AgentBuilderPlugin>("/plugins/install", request, ct);

	/// <summary> Delete an installed plugin by its ID. </summary>
	public Task DeletePluginAsync(string pluginId, CancellationToken ct = default) =>
		DeleteAsync($"/plugins/{pluginId}", ct);
}
