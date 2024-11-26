using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace Elastic.SemanticKernel.Playground;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

internal sealed class Program
{
    public static async Task Main(string[] args)
    {
#pragma warning disable SKEXP0010 // Some SK methods are still experimental

        var builder = Host.CreateApplicationBuilder(args);

        // Register AI services.
        var kernelBuilder = builder.Services.AddKernel();
        kernelBuilder.AddAzureOpenAIChatCompletion("gpt-4o", "https://my-service.openai.azure.com", "my_token");
        kernelBuilder.AddAzureOpenAITextEmbeddingGeneration("ada-002", "https://my-service.openai.azure.com", "my_token");

        // Register text search service.
        kernelBuilder.AddVectorStoreTextSearch<Hotel>();

        // Register Elasticsearch vector store.
        var elasticsearchClientSettings = new ElasticsearchClientSettings(new Uri("https://my-elasticsearch-instance.cloud"))
            .Authentication(new BasicAuthentication("elastic", "my_password"))
            .DisableDirectStreaming()
            .EnableDebugMode(cd =>
            {
                //var request = System.Text.Encoding.Default.GetString(cd.RequestBodyInBytes);
                Console.WriteLine(cd.DebugInformation);
            });
        kernelBuilder.AddElasticsearchVectorStoreRecordCollection<string, Hotel>("skhotels", elasticsearchClientSettings);

        // Build the host.
        using var host = builder.Build();

        // For demo purposes, we access the services directly without using a DI context.

        var kernel = host.Services.GetService<Kernel>()!;
        var embeddings = host.Services.GetService<ITextEmbeddingGenerationService>()!;
        var vectorStoreCollection = host.Services.GetService<IVectorStoreRecordCollection<string, Hotel>>()!;

        // Register search plugin.
        var textSearch = host.Services.GetService<VectorStoreTextSearch<Hotel>>()!;
        kernel.Plugins.Add(textSearch.CreateWithGetTextSearchResults("SearchPlugin"));

        // Crate collection and ingest a few demo records.
        await vectorStoreCollection.CreateCollectionIfNotExistsAsync();

        // CSV format: ID;Hotel Name;Description;Reference Link
        var hotels = (await File.ReadAllLinesAsync("D:\\elastic\\semantic-kernel-net\\hotels.csv"))
            .Select(x => x.Split(';'));

        foreach (var hotel in hotels)
        {
            await vectorStoreCollection.UpsertAsync(new Hotel
            {
                HotelId = hotel[0],
                HotelName = hotel[1],
                Description = hotel[2],
                DescriptionEmbedding = await embeddings.GenerateEmbeddingAsync(hotel[2]),
                ReferenceLink = hotel[3]
            });
        }

        // Invoke the LLM with a template that uses the search plugin to
        // 1. get related information to the user query from the vector store
        // 2. add the information to the LLM prompt.
        var response = await kernel.InvokePromptAsync(
            promptTemplate: """
                            Please use this information to answer the question:
                            {{#with (SearchPlugin-GetTextSearchResults question)}}
                              {{#each this}}
                                Name: {{Name}}
                                Value: {{Value}}
                                Source: {{Link}}
                                -----------------
                              {{/each}}
                            {{/with}}

                            Include the source of relevant information in the response.

                            Question: {{question}}
                            """,
            arguments: new KernelArguments
            {
                { "question", "Please show me all hotels that have a rooftop bar." },
            },
            templateFormat: "handlebars",
            promptTemplateFactory: new HandlebarsPromptTemplateFactory());

        Console.WriteLine(response.ToString());

        // > Urban Chic Hotel has a rooftop bar with stunning views (Source: https://example.com/stu654).
    }
}

public sealed record Hotel
{
    [VectorStoreRecordKey]
    public required string HotelId { get; set; }

    [TextSearchResultName]
    [VectorStoreRecordData(IsFilterable = true)]
    public required string HotelName { get; set; }

    [TextSearchResultValue]
    [VectorStoreRecordData(IsFullTextSearchable = true)]
    public required string Description { get; set; }

    [VectorStoreRecordVector(Dimensions: 1536, DistanceFunction.CosineSimilarity, IndexKind.Hnsw)]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }

    [TextSearchResultLink]
    [VectorStoreRecordData]
    public string? ReferenceLink { get; set; }
}
