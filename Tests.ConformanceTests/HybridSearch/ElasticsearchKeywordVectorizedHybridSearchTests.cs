// Copyright (c) Microsoft. All rights reserved.

using Elasticsearch.ConformanceTests.Support;

using VectorData.ConformanceTests.HybridSearch;
using VectorData.ConformanceTests.Support;

using Xunit;

namespace Elasticsearch.ConformanceTests.HybridSearch;

public class ElasticsearchKeywordVectorizedHybridSearchTests(
    ElasticsearchKeywordVectorizedHybridSearchTests.VectorAndStringFixture vectorAndStringFixture,
    ElasticsearchKeywordVectorizedHybridSearchTests.MultiTextFixture multiTextFixture)
    : KeywordVectorizedHybridSearchComplianceTests<string>(vectorAndStringFixture, multiTextFixture),
        IClassFixture<ElasticsearchKeywordVectorizedHybridSearchTests.VectorAndStringFixture>,
        IClassFixture<ElasticsearchKeywordVectorizedHybridSearchTests.MultiTextFixture>
{
    public new class VectorAndStringFixture : KeywordVectorizedHybridSearchComplianceTests<string>.VectorAndStringFixture
    {
        public override TestStore TestStore => ElasticsearchTestStore.Instance;

        public override string CollectionName => "keyword_hybrid_search" + GetUniqueCollectionName();
        protected override string IndexKind => Microsoft.Extensions.VectorData.IndexKind.Hnsw;
    }

    public new class MultiTextFixture : KeywordVectorizedHybridSearchComplianceTests<string>.MultiTextFixture
    {
        public override TestStore TestStore => ElasticsearchTestStore.Instance;

        public override string CollectionName => "keyword_hybrid_search" + GetUniqueCollectionName();
        protected override string IndexKind => Microsoft.Extensions.VectorData.IndexKind.Hnsw;
    }
}
