// Copyright (c) Microsoft. All rights reserved.

using Elasticsearch.ConformanceTests.Support;

using VectorData.ConformanceTests.Filter;
using VectorData.ConformanceTests.Support;

using Xunit;

namespace Elasticsearch.ConformanceTests.Filter;

public class ElasticsearchBasicFilterTests(ElasticsearchBasicFilterTests.Fixture fixture)
    : BasicFilterTests<string>(fixture), IClassFixture<ElasticsearchBasicFilterTests.Fixture>
{
    public new class Fixture : BasicFilterTests<string>.Fixture
    {
        public override TestStore TestStore => ElasticsearchTestStore.Instance;

        public override string CollectionName => "filter_tests";
    }
}
