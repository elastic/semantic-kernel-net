// Copyright (c) Microsoft. All rights reserved.

using Elasticsearch.ConformanceTests.Support;

using VectorData.ConformanceTests.CRUD;
using VectorData.ConformanceTests.Support;

using Xunit;

namespace Elasticsearch.ConformanceTests.CRUD;

public class ElasticsearchNoDataConformanceTests(ElasticsearchNoDataConformanceTests.Fixture fixture)
    : NoDataConformanceTests<string>(fixture), IClassFixture<ElasticsearchNoDataConformanceTests.Fixture>
{
    public new class Fixture : NoDataConformanceTests<string>.Fixture
    {
        public override TestStore TestStore => ElasticsearchTestStore.Instance;
    }
}
