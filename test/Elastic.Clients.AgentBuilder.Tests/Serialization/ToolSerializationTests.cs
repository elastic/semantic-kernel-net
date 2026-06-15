// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json;
using Elastic.Clients.AgentBuilder.Tools;
using FluentAssertions;
using Xunit;

namespace Elastic.Clients.AgentBuilder.Tests.Serialization;

public class ToolSerializationTests
{
	private static readonly AgentBuilderSerializationContext Ctx = AgentBuilderSerializationContext.Default;

	[Fact]
	public void CreateEsqlToolRequest_SerializesCorrectly()
	{
		var request = new CreateEsqlToolRequest
		{
			Id = "my-esql-tool",
			Description = "Find books by page count",
			Tags = ["analytics", "books"],
			Configuration = new EsqlToolConfiguration
			{
				Query = "FROM books | SORT page_count DESC | LIMIT ?limit",
				Params = new Dictionary<string, EsqlToolParam>
				{
					["limit"] = new() { Type = "integer", Description = "Max results" }
				}
			}
		};

		var json = JsonSerializer.Serialize(request, Ctx.CreateEsqlToolRequest);
		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		root.GetProperty("id").GetString().Should().Be("my-esql-tool");
		root.GetProperty("type").GetString().Should().Be("esql");
		root.GetProperty("description").GetString().Should().Be("Find books by page count");
		root.GetProperty("tags").GetArrayLength().Should().Be(2);
		root.GetProperty("configuration").GetProperty("query").GetString()
			.Should().Contain("LIMIT ?limit");
		root.GetProperty("configuration").GetProperty("params")
			.GetProperty("limit").GetProperty("type").GetString()
			.Should().Be("integer");
	}

	[Fact]
	public void CreateIndexSearchToolRequest_SerializesCorrectly()
	{
		var request = new CreateIndexSearchToolRequest
		{
			Id = "search-logs",
			Description = "Search application logs",
			Configuration = new IndexSearchToolConfiguration
			{
				IndexPattern = "logs-myapp-*",
				RowLimit = 50,
				CustomInstructions = "Always include @timestamp"
			}
		};

		var json = JsonSerializer.Serialize(request, Ctx.CreateIndexSearchToolRequest);
		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		root.GetProperty("type").GetString().Should().Be("index_search");
		root.GetProperty("configuration").GetProperty("index_pattern").GetString()
			.Should().Be("logs-myapp-*");
		root.GetProperty("configuration").GetProperty("row_limit").GetInt32()
			.Should().Be(50);
	}

	[Fact]
	public void AgentBuilderTool_DeserializesFromApi()
	{
		var json = """
		{
			"id": "example-books-esql-tool",
			"type": "esql",
			"description": "An ES|QL query tool",
			"tags": ["analytics"],
			"configuration": {
				"query": "FROM books | LIMIT 1",
				"params": {}
			},
			"readonly": false
		}
		""";

		var tool = JsonSerializer.Deserialize(json, Ctx.AgentBuilderTool);

		tool.Should().NotBeNull();
		tool!.Id.Should().Be("example-books-esql-tool");
		tool.Type.Should().Be("esql");
		tool.Readonly.Should().BeFalse();

		var config = tool.AsEsql();
		config.Should().NotBeNull();
		config!.Query.Should().Be("FROM books | LIMIT 1");
	}

	[Fact]
	public void ListToolsResponse_DeserializesFromApi()
	{
		var json = """
		{
			"results": [
				{
					"id": "platform.core.search",
					"type": "builtin",
					"description": "Search tool",
					"configuration": {},
					"readonly": true
				},
				{
					"id": "my-tool",
					"type": "esql",
					"description": "Custom tool",
					"tags": ["custom"],
					"configuration": { "query": "FROM test | LIMIT 1", "params": {} },
					"readonly": false
				}
			]
		}
		""";

		var response = JsonSerializer.Deserialize(json, Ctx.ListToolsResponse);

		response.Should().NotBeNull();
		response!.Results.Should().HaveCount(2);
		response.Results[0].Type.Should().Be("builtin");
		response.Results[1].AsEsql().Should().NotBeNull();
	}

	[Fact]
	public void ExecuteToolResponse_TabularData_DeserializesCorrectly()
	{
		var json = """
		{
			"results": [
				{
					"type": "query",
					"data": { "esql": "FROM books | SORT page_count DESC | LIMIT 1" }
				},
				{
					"type": "tabular_data",
					"data": {
						"source": "esql",
						"columns": [
							{ "name": "author", "type": "text" },
							{ "name": "page_count", "type": "integer" }
						],
						"values": [
							["Alastair Reynolds", 585]
						]
					}
				}
			]
		}
		""";

		var response = JsonSerializer.Deserialize(json, Ctx.ExecuteToolResponse);

		response.Should().NotBeNull();
		response!.Results.Should().HaveCount(2);

		var query = response.Results[0].AsQuery();
		query.Should().NotBeNull();
		query!.Esql.Should().Contain("SORT page_count");

		var tabular = response.Results[1].AsTabularData();
		tabular.Should().NotBeNull();
		tabular!.Columns.Should().HaveCount(2);
		tabular.Values.Should().HaveCount(1);

		var row = tabular.Row(0);
		row.GetString("author").Should().Be("Alastair Reynolds");
		row.GetInt32("page_count").Should().Be(585);
	}
}
