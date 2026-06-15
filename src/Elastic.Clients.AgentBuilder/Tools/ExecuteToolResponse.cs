// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Transport;

namespace Elastic.Clients.AgentBuilder.Tools;

/// <summary>
/// Response from executing a tool.
/// </summary>
public class ExecuteToolResponse : TransportResponse
{
	[JsonPropertyName("results")]
	public IReadOnlyList<ToolResult> Results { get; set; } = default!;
}

/// <summary>
/// A single result from a tool execution. The <see cref="Type"/> discriminator
/// indicates which shape <see cref="Data"/> takes.
/// </summary>
public record ToolResult
{
	[JsonPropertyName("type")]
	public required string Type { get; init; }

	[JsonPropertyName("data")]
	public JsonElement Data { get; init; }

	/// <summary> Deserialize as tabular data when <see cref="Type"/> is <c>"tabular_data"</c>. </summary>
	public TabularData? AsTabularData() =>
		Type == "tabular_data" ? Data.Deserialize(AgentBuilderSerializationContext.Default.TabularData) : null;

	/// <summary> Deserialize as a query result when <see cref="Type"/> is <c>"query"</c>. </summary>
	public QueryResult? AsQuery() =>
		Type == "query" ? Data.Deserialize(AgentBuilderSerializationContext.Default.QueryResult) : null;
}

/// <summary>
/// A query result containing an ES|QL query string.
/// </summary>
public record QueryResult
{
	[JsonPropertyName("esql")]
	public string? Esql { get; init; }
}

/// <summary>
/// Tabular data returned by a tool execution.
/// Use <see cref="Row"/> to access values by column name.
/// </summary>
public record TabularData
{
	[JsonPropertyName("source")]
	public string? Source { get; init; }

	[JsonPropertyName("query")]
	public string? Query { get; init; }

	[JsonPropertyName("columns")]
	public required IReadOnlyList<TabularColumn> Columns { get; init; }

	[JsonPropertyName("values")]
	public required IReadOnlyList<IReadOnlyList<JsonElement>> Values { get; init; }

	/// <summary> Returns a <see cref="TabularRow"/> accessor for the specified row index. </summary>
	public TabularRow Row(int index) => new(Values[index], Columns);
}

/// <summary>
/// Column metadata for tabular data.
/// </summary>
public record TabularColumn
{
	[JsonPropertyName("name")]
	public required string Name { get; init; }

	[JsonPropertyName("type")]
	public required string Type { get; init; }
}

/// <summary>
/// Provides column-name–based access to a single row of <see cref="TabularData"/>.
/// </summary>
public readonly struct TabularRow
{
	private readonly IReadOnlyList<JsonElement> _values;
	private readonly IReadOnlyList<TabularColumn> _columns;

	internal TabularRow(IReadOnlyList<JsonElement> values, IReadOnlyList<TabularColumn> columns)
	{
		_values = values;
		_columns = columns;
	}

	/// <summary> Access a cell by column index. </summary>
	public JsonElement this[int index] => _values[index];

	/// <summary> Access a cell by column name. </summary>
	public JsonElement this[string columnName] => _values[IndexOf(columnName)];

	/// <summary> Get the string value of a cell by column name. </summary>
	public string? GetString(string columnName) => this[columnName].GetString();

	/// <summary> Get the int value of a cell by column name. </summary>
	public int GetInt32(string columnName) => this[columnName].GetInt32();

	/// <summary> Get the long value of a cell by column name. </summary>
	public long GetInt64(string columnName) => this[columnName].GetInt64();

	/// <summary> Get the double value of a cell by column name. </summary>
	public double GetDouble(string columnName) => this[columnName].GetDouble();

	/// <summary> Get the boolean value of a cell by column name. </summary>
	public bool GetBoolean(string columnName) => this[columnName].GetBoolean();

	private int IndexOf(string columnName)
	{
		for (var i = 0; i < _columns.Count; i++)
		{
			if (_columns[i].Name == columnName)
				return i;
		}
		throw new KeyNotFoundException($"Column '{columnName}' not found.");
	}
}
