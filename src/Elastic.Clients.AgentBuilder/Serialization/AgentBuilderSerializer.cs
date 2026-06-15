// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;

namespace Elastic.Clients.AgentBuilder;

internal sealed class AgentBuilderSerializer : SystemTextJsonSerializer
{
	public static readonly AgentBuilderSerializer Instance = new();

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
		Justification = "DefaultJsonTypeInfoResolver is a fallback; all known types are covered by AgentBuilderSerializationContext")]
	[UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
		Justification = "DefaultJsonTypeInfoResolver is a fallback; all known types are covered by AgentBuilderSerializationContext")]
	public AgentBuilderSerializer() : base(new TransportSerializerOptionsProvider([], null, options =>
	{
		options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
		options.TypeInfoResolver = JsonTypeInfoResolver.Combine(
			AgentBuilderSerializationContext.Default,
			ElasticsearchTransportSerializerContext.Default,
			new DefaultJsonTypeInfoResolver()
		);
	}))
	{
	}
}
