// Copyright (c) Microsoft. All rights reserved.

using VectorData.ConformanceTests.Support;

namespace Elasticsearch.ConformanceTests.Support;

public class ElasticsearchSimpleModelFixture : SimpleModelFixture<string>
{
    public override TestStore TestStore => ElasticsearchTestStore.Instance;
}
