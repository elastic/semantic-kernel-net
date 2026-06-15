// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Clients.AgentBuilder.Agents;
using Elastic.Clients.AgentBuilder.Conversations;
using Elastic.Clients.AgentBuilder.Plugins;
using Elastic.Clients.AgentBuilder.Skills;
using Elastic.Clients.AgentBuilder.Tools;

namespace Elastic.Clients.AgentBuilder;

[JsonSerializable(typeof(AgentBuilderTool))]
[JsonSerializable(typeof(ListToolsResponse))]
[JsonSerializable(typeof(EsqlToolConfiguration))]
[JsonSerializable(typeof(IndexSearchToolConfiguration))]
[JsonSerializable(typeof(McpToolConfiguration))]
[JsonSerializable(typeof(WorkflowToolConfiguration))]
[JsonSerializable(typeof(EsqlToolParam))]
[JsonSerializable(typeof(CreateEsqlToolRequest))]
[JsonSerializable(typeof(CreateIndexSearchToolRequest))]
[JsonSerializable(typeof(CreateMcpToolRequest))]
[JsonSerializable(typeof(CreateWorkflowToolRequest))]
[JsonSerializable(typeof(UpdateEsqlToolRequest))]
[JsonSerializable(typeof(UpdateIndexSearchToolRequest))]
[JsonSerializable(typeof(UpdateMcpToolRequest))]
[JsonSerializable(typeof(UpdateWorkflowToolRequest))]
[JsonSerializable(typeof(ExecuteToolRequest))]
[JsonSerializable(typeof(ExecuteToolResponse))]
[JsonSerializable(typeof(ToolResult))]
[JsonSerializable(typeof(TabularData))]
[JsonSerializable(typeof(TabularColumn))]
[JsonSerializable(typeof(QueryResult))]
[JsonSerializable(typeof(AgentBuilderAgent))]
[JsonSerializable(typeof(ListAgentsResponse))]
[JsonSerializable(typeof(AgentConfiguration))]
[JsonSerializable(typeof(AgentToolGroup))]
[JsonSerializable(typeof(CreateAgentRequest))]
[JsonSerializable(typeof(UpdateAgentRequest))]
[JsonSerializable(typeof(AgentConsumptionRequest))]
[JsonSerializable(typeof(AgentConsumptionResponse))]
[JsonSerializable(typeof(ConversationConsumption))]
[JsonSerializable(typeof(Conversation))]
[JsonSerializable(typeof(ListConversationsResponse))]
[JsonSerializable(typeof(Attachment))]
[JsonSerializable(typeof(ListAttachmentsResponse))]
[JsonSerializable(typeof(CreateAttachmentRequest))]
[JsonSerializable(typeof(UpdateAttachmentRequest))]
[JsonSerializable(typeof(UpdateAttachmentOriginRequest))]
[JsonSerializable(typeof(ConverseRequest))]
[JsonSerializable(typeof(ConverseResponse))]
[JsonSerializable(typeof(ConverseStep))]
[JsonSerializable(typeof(ConverseMessage))]
[JsonSerializable(typeof(ModelUsage))]
[JsonSerializable(typeof(ConversationIdSetEvent))]
[JsonSerializable(typeof(ConversationCreatedEvent))]
[JsonSerializable(typeof(ConversationUpdatedEvent))]
[JsonSerializable(typeof(ReasoningEvent))]
[JsonSerializable(typeof(ToolCallEvent))]
[JsonSerializable(typeof(ToolProgressEvent))]
[JsonSerializable(typeof(ToolResultEvent))]
[JsonSerializable(typeof(MessageChunkEvent))]
[JsonSerializable(typeof(MessageCompleteEvent))]
[JsonSerializable(typeof(ThinkingCompleteEvent))]
[JsonSerializable(typeof(RoundCompleteEvent))]
[JsonSerializable(typeof(AgentBuilderSkill))]
[JsonSerializable(typeof(ListSkillsResponse))]
[JsonSerializable(typeof(SkillReferencedContent))]
[JsonSerializable(typeof(CreateSkillRequest))]
[JsonSerializable(typeof(UpdateSkillRequest))]
[JsonSerializable(typeof(AgentBuilderPlugin))]
[JsonSerializable(typeof(ListPluginsResponse))]
[JsonSerializable(typeof(PluginManagedAssets))]
[JsonSerializable(typeof(InstallPluginRequest))]
[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public sealed partial class AgentBuilderSerializationContext : JsonSerializerContext;
