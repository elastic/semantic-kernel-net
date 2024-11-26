using System;
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

        await vectorStoreCollection.UpsertAsync(new Hotel
        {
            HotelId = "1",
            HotelName = "First Hotel",
            Description = "The blue hotel.",
            DescriptionEmbedding = await embeddings.GenerateEmbeddingAsync("The blue hotel."),
            ReferenceLink = "Global Hotel Database, Entry 1337"
        });

        await vectorStoreCollection.UpsertAsync(new Hotel
        {
            HotelId = "2",
            HotelName = "Second Hotel",
            Description = "The green hotel.",
            DescriptionEmbedding = await embeddings.GenerateEmbeddingAsync("The green hotel."),
            ReferenceLink = "Global Hotel Database, Entry 4242"
        });

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
                { "question", "What is the name of the hotel that has the same color as grass?" },
            },
            templateFormat: "handlebars",
            promptTemplateFactory: new HandlebarsPromptTemplateFactory());

        Console.WriteLine(response.ToString());

        // > The name of the hotel that has the same color as grass is "Second Hotel."
        // > This hotel is described as the green hotel. (Source: Global Hotel Database, Entry 4242)
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
