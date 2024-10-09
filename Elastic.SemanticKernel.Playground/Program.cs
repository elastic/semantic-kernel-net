using System;
using System.Threading.Tasks;

using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

using Elastic.SemanticKernel.Connectors.Elasticsearch;
using Microsoft.SemanticKernel.Data;

namespace Elastic.SemanticKernel.Playground;

internal class Program
{
    static async Task Main(string[] args)
    {
        using var settings = new ElasticsearchClientSettings(new Uri("https://primary.es.europe-west3.gcp.cloud.es.io"))
            .Authentication(new BasicAuthentication("elastic", "dJGI1c9D87cFFZZRNsv4xfxM"))
            .DisableDirectStreaming()
            .EnableDebugMode(cd =>
            {
                Console.WriteLine(cd.DebugInformation);
            });

        var client = new ElasticsearchClient(settings);

        var vectorStore = new ElasticsearchVectorStore(client);

        var collection = vectorStore.GetCollection<string, MyRecord>("sk");
        await collection.CreateCollectionIfNotExistsAsync();

        var id = await collection.UpsertAsync(new MyRecord
        {
            Id = "42",
            Vec1 = "bla",
            Vec2 = new float[] { 1.2f, 1.1f, 1.3f },
            Data1 = 1337,
            Data2 = DateTimeKind.Utc
        });


        var record = await collection.GetAsync(id);
        var genericCollection = vectorStore.GetCollection<string, VectorStoreGenericDataModel<string>>("sk", new VectorStoreRecordDefinition
        {
            Properties =
            [
                new VectorStoreRecordKeyProperty(nameof(MyRecord.Id), typeof(string)),
                new VectorStoreRecordVectorProperty(nameof(MyRecord.Vec1), typeof(string)),
                new VectorStoreRecordVectorProperty(nameof(MyRecord.Vec2), typeof(ReadOnlyMemory<float>)),
                new VectorStoreRecordDataProperty(nameof(MyRecord.Data1), typeof(int)),
                new VectorStoreRecordDataProperty(nameof(MyRecord.Data2), typeof(DateTimeKind))
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
    public string? Id { get; init; }

    [VectorStoreRecordVector(3)]
    public string Vec1 { get; init; }

    [VectorStoreRecordVector(3)]
    public ReadOnlyMemory<float> Vec2 { get; init; }

    [VectorStoreRecordData(IsFilterable = true, IsFullTextSearchable = true)]
    public int Data1 { get; init; }

    [VectorStoreRecordData(IsFilterable = true)]
    public DateTimeKind Data2 { get; init; }
}
