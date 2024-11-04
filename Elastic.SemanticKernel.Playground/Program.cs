using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

using Elastic.SemanticKernel.Connectors.Elasticsearch;

using Microsoft.Extensions.VectorData;

namespace Elastic.SemanticKernel.Playground;

internal class Program
{
    static async Task Main(string[] args)
    {
        using var settings = new ElasticsearchClientSettings(new Uri("https://primary.es.europe-west3.gcp.cloud.es.io"))
            .Authentication(new BasicAuthentication("elastic", "g7wuRWPrJF2yAzytvd9w18s8"))
            .DisableDirectStreaming()
            .EnableDebugMode(cd =>
            {
                Console.WriteLine(cd.DebugInformation);
            });

        var client = new ElasticsearchClient(settings);

        var vectorStore = new ElasticsearchVectorStore(client);

        var collection = vectorStore.GetCollection<string, MyRecord>("sk");

        await collection.CreateCollectionIfNotExistsAsync();

        await collection.UpsertAsync(new MyRecord
        {
            MyKey = "40",
            Vec1 = new float[] { 2.1f, 2.0f, 2.2f },
            Vec2 = new float[] { 1.2f, 1.1f, 1.3f },
            Data1 = 1337,
            Data2 = DateTimeKind.Utc,
            Data3 = ["a", "b", "c"]
        });

        await collection.UpsertAsync(new MyRecord
        {
            MyKey = "41",
            Vec1 = new float[] { 0.1f, 0.1f, 1000.1f },
            Vec2 = new float[] { 1.2f, 1.1f, 1.3f },
            Data1 = 1338,
            Data2 = DateTimeKind.Utc,
            Data3 = ["a", "abba", "c"]
        });

        var id = await collection.UpsertAsync(new MyRecord
        {
            MyKey = "42",
            Vec1 = new float[] { 2.2f, 2.1f, 2.3f, 0.0f },
            Vec2 = new float[] { 1.2f, 1.1f, 1.3f },
            Data1 = 1337,
            Data2 = DateTimeKind.Utc,
            Data3 = ["a", "b", "c"]
        });

        var record = await collection.GetAsync(id);

        var search = await collection.VectorizedSearchAsync(new float[] { 2.2f, 2.1f, 2.3f }, new VectorSearchOptions
        {
            IncludeTotalCount = true,
            Filter = new VectorSearchFilter(new FilterClause[]
            {
                new AnyTagEqualToFilterClause(nameof(MyRecord.Data3), "abba"),
                new EqualToFilterClause(nameof(MyRecord.Data1), 1338) // TODO: Test char
            })
        });

        var genericCollection = vectorStore.GetCollection<string, VectorStoreGenericDataModel<string>>("sk", new VectorStoreRecordDefinition
        {
            Properties =
            [
                new VectorStoreRecordKeyProperty(nameof(MyRecord.MyKey), typeof(string)),
                new VectorStoreRecordVectorProperty(nameof(MyRecord.Vec1), typeof(ReadOnlyMemory<float>))
                {
                    StoragePropertyName = "xxx"
                },
                new VectorStoreRecordVectorProperty(nameof(MyRecord.Vec2), typeof(ReadOnlyMemory<float>)),
                new VectorStoreRecordDataProperty(nameof(MyRecord.Data1), typeof(int)),
                new VectorStoreRecordDataProperty(nameof(MyRecord.Data2), typeof(DateTimeKind)),
                new VectorStoreRecordDataProperty(nameof(MyRecord.Data3), typeof(string[]))
            ]
        });

        var genericRecord = await genericCollection.GetAsync(id);
        genericRecord.Key = null;

        var id2 = await genericCollection.UpsertAsync(genericRecord);
        var record2 = await collection.GetAsync(id2);

        await collection.DeleteAsync(id);
        await collection.DeleteAsync(id2);

        await collection.DeleteCollectionAsync();
    }
}

public sealed class MyRecord
{
    [VectorStoreRecordKey]
    public required string? MyKey { get; init; }

    [VectorStoreRecordVector(3)]
    [JsonPropertyName("xxx")]
    public required IReadOnlyCollection<float> Vec1 { get; init; }

    [VectorStoreRecordVector(3)]
    public required ReadOnlyMemory<float> Vec2 { get; init; }

    [VectorStoreRecordData(IsFilterable = true, IsFullTextSearchable = true)]
    public required int Data1 { get; init; }

    [VectorStoreRecordData(IsFilterable = true)]
    public required DateTimeKind Data2 { get; init; }

    [VectorStoreRecordData(IsFilterable = true)]

#pragma warning disable CA1819

    public required string[] Data3 { get; init; }

#pragma warning restore CA1819
}
