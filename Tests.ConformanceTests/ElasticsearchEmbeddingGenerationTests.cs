// Copyright (c) Microsoft. All rights reserved.

using Elasticsearch.ConformanceTests.Support;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;

using VectorData.ConformanceTests;
using VectorData.ConformanceTests.Support;

using Xunit;

namespace Elasticsearch.ConformanceTests;

public class ElasticsearchEmbeddingGenerationTests(ElasticsearchEmbeddingGenerationTests.StringVectorFixture stringVectorFixture, ElasticsearchEmbeddingGenerationTests.RomOfFloatVectorFixture romOfFloatVectorFixture)
    : EmbeddingGenerationTests<Guid>(stringVectorFixture, romOfFloatVectorFixture), IClassFixture<ElasticsearchEmbeddingGenerationTests.StringVectorFixture>, IClassFixture<ElasticsearchEmbeddingGenerationTests.RomOfFloatVectorFixture>
{
    public new class StringVectorFixture : EmbeddingGenerationTests<Guid>.StringVectorFixture
    {
        public override TestStore TestStore => ElasticsearchTestStore.Instance;

        public override string CollectionName => "embedding_generation_tests";

        public override VectorStore CreateVectorStore(IEmbeddingGenerator? embeddingGenerator)
            => ElasticsearchTestStore.Instance.GetVectorStore(new() { EmbeddingGenerator = embeddingGenerator });

        public override Func<IServiceCollection, IServiceCollection>[] DependencyInjectionStoreRegistrationDelegates =>
        [
            services => services
                .AddSingleton(ElasticsearchTestStore.Instance.Client)
                .AddElasticsearchVectorStore()
        ];

        public override Func<IServiceCollection, IServiceCollection>[] DependencyInjectionCollectionRegistrationDelegates =>
        [
            services => services
                .AddSingleton(ElasticsearchTestStore.Instance.Client)
                .AddElasticsearchCollection<Guid, RecordWithAttributes>(this.CollectionName)
        ];
    }

    public new class RomOfFloatVectorFixture : EmbeddingGenerationTests<Guid>.RomOfFloatVectorFixture
    {
        public override TestStore TestStore => ElasticsearchTestStore.Instance;

        public override string CollectionName => "search_only_embedding_generation_tests";

        public override VectorStore CreateVectorStore(IEmbeddingGenerator? embeddingGenerator)
            => ElasticsearchTestStore.Instance.GetVectorStore(new() { EmbeddingGenerator = embeddingGenerator });

        public override Func<IServiceCollection, IServiceCollection>[] DependencyInjectionStoreRegistrationDelegates =>
        [
            services => services
                .AddSingleton(ElasticsearchTestStore.Instance.Client)
                .AddElasticsearchVectorStore()
        ];

        public override Func<IServiceCollection, IServiceCollection>[] DependencyInjectionCollectionRegistrationDelegates =>
        [
            services => services
                .AddSingleton(ElasticsearchTestStore.Instance.Client)
                .AddElasticsearchCollection<Guid, RecordWithAttributes>(this.CollectionName)
        ];
    }
}
