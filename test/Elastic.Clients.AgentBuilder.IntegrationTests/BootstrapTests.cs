// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elastic.Clients.AgentBuilder.Agents;
using Elastic.Clients.AgentBuilder.Tools;
using FluentAssertions;
using Xunit;

namespace Elastic.Clients.AgentBuilder.IntegrationTests;

public class BootstrapTests : AgentBuilderTestBase
{
	private readonly ConcurrentBag<string> _createdToolIds = new();
	private readonly ConcurrentBag<string> _createdAgentIds = new();

	private static string ToolId([CallerMemberName] string caller = "") =>
		$"dotnet-bt-{caller}".ToLowerInvariant();

	private static string AgentId([CallerMemberName] string caller = "") =>
		$"dotnet-bt-{caller}".ToLowerInvariant();

	private static string GetHashTag(IReadOnlyList<string>? tagsOrLabels) =>
		tagsOrLabels!.Single(t => t.StartsWith("_hash:", System.StringComparison.Ordinal));

	private static BootstrapDefinition ToolDefinition(
		string toolId,
		string description = "Bootstrap test tool",
		string query = "FROM test | LIMIT 1") =>
		new()
		{
			EsqlTools =
			[
				new CreateEsqlToolRequest
				{
					Id = toolId,
					Description = description,
					Configuration = new EsqlToolConfiguration
					{
						Query = query,
						Params = new Dictionary<string, EsqlToolParam>()
					}
				}
			]
		};

	private static BootstrapDefinition AgentDefinition(
		string agentId,
		string name = "Bootstrap test agent",
		string? description = "An agent for bootstrap tests",
		string instructions = "You are a test agent.",
		IReadOnlyList<string>? toolIds = null) =>
		new()
		{
			Agents =
			[
				new CreateAgentRequest
				{
					Id = agentId,
					Name = name,
					Description = description,
					Configuration = new AgentConfiguration
					{
						Instructions = instructions,
						Tools = [new AgentToolGroup { ToolIds = toolIds ?? [] }]
					}
				}
			]
		};

	// ── Tool idempotency ───────────────────────────────────────────────

	[Fact]
	public async Task Tool_SecondBootstrapWithSameDefinition_DoesNotUpdate()
	{
		var id = ToolId();
		_createdToolIds.Add(id);
		await CleanupToolAsync(id);

		var bootstrapper = new AgentBuilderBootstrapper(Client);
		var definition = ToolDefinition(id);

		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure, definition);

		var after1 = await Client.GetToolAsync(id);
		var hash1 = GetHashTag(after1.Tags);

		// Tamper the description directly; keep hash tag intact so the
		// bootstrapper should see a matching hash and skip the update.
		await Client.UpdateToolAsync(id, new UpdateEsqlToolRequest
		{
			Description = "tampered",
			Tags = after1.Tags,
			Configuration = after1.AsEsql()
		});

		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure, definition);

		var after2 = await Client.GetToolAsync(id);
		after2.Description.Should().Be("tampered",
			"bootstrapper should skip update when the hash matches");
		GetHashTag(after2.Tags).Should().Be(hash1);
	}

	// ── Tool update on description change ──────────────────────────────

	[Fact]
	public async Task Tool_ChangedDescription_TriggersUpdate()
	{
		var id = ToolId();
		_createdToolIds.Add(id);
		await CleanupToolAsync(id);

		var bootstrapper = new AgentBuilderBootstrapper(Client);

		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure, ToolDefinition(id));
		var hash1 = GetHashTag((await Client.GetToolAsync(id)).Tags);

		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure,
			ToolDefinition(id, description: "Updated description"));

		var after = await Client.GetToolAsync(id);
		after.Description.Should().Be("Updated description");
		GetHashTag(after.Tags).Should().NotBe(hash1,
			"a new hash should be written when the definition changes");
	}

	// ── Tool update on query change ────────────────────────────────────

	[Fact]
	public async Task Tool_ChangedQuery_TriggersUpdate()
	{
		var id = ToolId();
		_createdToolIds.Add(id);
		await CleanupToolAsync(id);

		var bootstrapper = new AgentBuilderBootstrapper(Client);

		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure, ToolDefinition(id));
		var hash1 = GetHashTag((await Client.GetToolAsync(id)).Tags);

		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure,
			ToolDefinition(id, query: "FROM test | LIMIT 99"));

		var after = await Client.GetToolAsync(id);
		after.AsEsql()!.Query.Should().Be("FROM test | LIMIT 99");
		GetHashTag(after.Tags).Should().NotBe(hash1);
	}

	// ── Agent idempotency ──────────────────────────────────────────────

	[Fact]
	public async Task Agent_SecondBootstrapWithSameDefinition_DoesNotUpdate()
	{
		var id = AgentId();
		_createdAgentIds.Add(id);
		await CleanupAgentAsync(id);

		var bootstrapper = new AgentBuilderBootstrapper(Client);
		var definition = AgentDefinition(id);

		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure, definition);

		var after1 = await Client.GetAgentAsync(id);
		var hash1 = GetHashTag(after1.Labels);

		// Tamper the name directly; keep hash label intact.
		await Client.UpdateAgentAsync(id, new UpdateAgentRequest
		{
			Name = "tampered",
			Description = after1.Description,
			Labels = after1.Labels,
			Configuration = after1.Configuration
		});

		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure, definition);

		var after2 = await Client.GetAgentAsync(id);
		after2.Name.Should().Be("tampered",
			"bootstrapper should skip update when the hash matches");
		GetHashTag(after2.Labels).Should().Be(hash1);
	}

	// ── Agent update on prompt (instructions) change ───────────────────

	[Fact]
	public async Task Agent_ChangedInstructions_TriggersUpdate()
	{
		var id = AgentId();
		_createdAgentIds.Add(id);
		await CleanupAgentAsync(id);

		var bootstrapper = new AgentBuilderBootstrapper(Client);

		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure, AgentDefinition(id));
		var hash1 = GetHashTag((await Client.GetAgentAsync(id)).Labels);

		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure,
			AgentDefinition(id, instructions: "You are a different test agent."));

		var after = await Client.GetAgentAsync(id);
		after.Configuration!.Instructions.Should().Be("You are a different test agent.");
		GetHashTag(after.Labels).Should().NotBe(hash1);
	}

	// ── Agent update on description change ─────────────────────────────

	[Fact]
	public async Task Agent_ChangedDescription_TriggersUpdate()
	{
		var id = AgentId();
		_createdAgentIds.Add(id);
		await CleanupAgentAsync(id);

		var bootstrapper = new AgentBuilderBootstrapper(Client);

		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure, AgentDefinition(id));
		var hash1 = GetHashTag((await Client.GetAgentAsync(id)).Labels);

		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure,
			AgentDefinition(id, description: "Changed agent description"));

		var after = await Client.GetAgentAsync(id);
		after.Description.Should().Be("Changed agent description");
		GetHashTag(after.Labels).Should().NotBe(hash1);
	}

	// ── Agent update on tool list change ───────────────────────────────

	[Fact]
	public async Task Agent_AddedTool_TriggersUpdate()
	{
		var toolId = ToolId();
		var agentId = AgentId();
		_createdToolIds.Add(toolId);
		_createdAgentIds.Add(agentId);
		await CleanupToolAsync(toolId);
		await CleanupAgentAsync(agentId);

		var bootstrapper = new AgentBuilderBootstrapper(Client);

		// Create the tool so the API can resolve the reference.
		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure, ToolDefinition(toolId));

		// Bootstrap agent with no tools.
		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure, AgentDefinition(agentId));
		var hash1 = GetHashTag((await Client.GetAgentAsync(agentId)).Labels);

		// Re-bootstrap with the tool added.
		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure,
			AgentDefinition(agentId, toolIds: [toolId]));

		var after = await Client.GetAgentAsync(agentId);
		after.Configuration!.Tools.Should().ContainSingle()
			.Which.ToolIds.Should().Contain(toolId);
		GetHashTag(after.Labels).Should().NotBe(hash1,
			"adding a tool should change the hash and trigger an update");
	}

	[Fact]
	public async Task Agent_RemovedTool_TriggersUpdate()
	{
		var toolId = ToolId();
		var agentId = AgentId();
		_createdToolIds.Add(toolId);
		_createdAgentIds.Add(agentId);
		await CleanupToolAsync(toolId);
		await CleanupAgentAsync(agentId);

		var bootstrapper = new AgentBuilderBootstrapper(Client);

		// Create the tool so the API can resolve the reference.
		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure, ToolDefinition(toolId));

		// Bootstrap agent with the tool assigned.
		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure,
			AgentDefinition(agentId, toolIds: [toolId]));
		var hash1 = GetHashTag((await Client.GetAgentAsync(agentId)).Labels);

		// Re-bootstrap with the tool removed.
		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure, AgentDefinition(agentId));

		var after = await Client.GetAgentAsync(agentId);
		after.Configuration!.Tools.Should().ContainSingle()
			.Which.ToolIds.Should().BeEmpty();
		GetHashTag(after.Labels).Should().NotBe(hash1,
			"removing a tool should change the hash and trigger an update");
	}

	// ── Agent update on metadata (name/description) change ─────────────

	[Fact]
	public async Task Agent_ChangedName_TriggersUpdate()
	{
		var id = AgentId();
		_createdAgentIds.Add(id);
		await CleanupAgentAsync(id);

		var bootstrapper = new AgentBuilderBootstrapper(Client);

		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure, AgentDefinition(id));
		var hash1 = GetHashTag((await Client.GetAgentAsync(id)).Labels);

		await bootstrapper.BootstrapAsync(BootstrapMethod.Failure,
			AgentDefinition(id, name: "Renamed agent"));

		var after = await Client.GetAgentAsync(id);
		after.Name.Should().Be("Renamed agent");
		GetHashTag(after.Labels).Should().NotBe(hash1);
	}

	// ── Cleanup helpers ────────────────────────────────────────────────

	private async Task CleanupToolAsync(string toolId)
	{
		try { await Client.DeleteToolAsync(toolId); } catch { /* not found */ }
	}

	private async Task CleanupAgentAsync(string agentId)
	{
		try { await Client.DeleteAgentAsync(agentId); } catch { /* not found */ }
	}

	public override void Dispose()
	{
		foreach (var id in _createdToolIds)
			try { Client.DeleteToolAsync(id).GetAwaiter().GetResult(); } catch { /* cleanup */ }
		foreach (var id in _createdAgentIds)
			try { Client.DeleteAgentAsync(id).GetAwaiter().GetResult(); } catch { /* cleanup */ }
		base.Dispose();
	}
}
