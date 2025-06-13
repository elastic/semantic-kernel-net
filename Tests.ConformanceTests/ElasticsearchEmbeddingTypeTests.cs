// Copyright (c) Microsoft. All rights reserved.

using Elasticsearch.ConformanceTests.Support;

using Microsoft.Extensions.AI;

using VectorData.ConformanceTests;
using VectorData.ConformanceTests.Support;

using Xunit;

#pragma warning disable CA2000 // Dispose objects before losing scope

namespace Elasticsearch.ConformanceTests;

public class ElasticsearchEmbeddingTypeTests(ElasticsearchEmbeddingTypeTests.Fixture fixture)
    : EmbeddingTypeTests<Guid>(fixture), IClassFixture<ElasticsearchEmbeddingTypeTests.Fixture>
{
    public new class Fixture : EmbeddingTypeTests<Guid>.Fixture
    {
        public override TestStore TestStore => ElasticsearchTestStore.Instance;

        public override string CollectionName => "embedding_type_tests";
    }
}
