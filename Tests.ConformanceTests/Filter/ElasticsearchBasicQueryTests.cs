// Copyright (c) Microsoft. All rights reserved.

using Elasticsearch.ConformanceTests.Support;

using VectorData.ConformanceTests.Filter;
using VectorData.ConformanceTests.Support;

using Xunit;

namespace Elasticsearch.ConformanceTests.Filter;

public class ElasticsearchBasicQueryTests(ElasticsearchBasicQueryTests.Fixture fixture)
    : BasicQueryTests<string>(fixture), IClassFixture<ElasticsearchBasicQueryTests.Fixture>
{
    public new class Fixture : BasicQueryTests<string>.QueryFixture
    {
        public override TestStore TestStore => ElasticsearchTestStore.Instance;

        public override string CollectionName => "query_tests";
    }
}
