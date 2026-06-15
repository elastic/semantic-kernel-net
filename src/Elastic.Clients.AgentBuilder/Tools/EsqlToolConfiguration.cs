// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elastic.Clients.AgentBuilder.Tools;

/// <summary>
/// Configuration for an ES|QL tool. Contains a parameterized query and typed parameter definitions.
/// </summary>
public record EsqlToolConfiguration
{
	[JsonPropertyName("query")]
	public required string Query { get; init; }

	[JsonPropertyName("params")]
	public Dictionary<string, EsqlToolParam>? Params { get; init; }
}

/// <summary>
/// Defines a single parameter for an ES|QL tool query.
/// </summary>
public record EsqlToolParam
{
	[JsonPropertyName("type")]
	public required string Type { get; init; }

	[JsonPropertyName("description")]
	public required string Description { get; init; }

	[JsonPropertyName("optional")]
	public bool? Optional { get; init; }

	[JsonPropertyName("defaultValue")]
	public object? DefaultValue { get; init; }
}
