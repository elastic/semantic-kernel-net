// Copyright (c) Microsoft. All rights reserved.

using VectorData.ConformanceTests.Support;

namespace Elasticsearch.ConformanceTests.Support;

public class ElasticsearchDynamicDataModelFixture : DynamicDataModelFixture<string>
{
    public override TestStore TestStore => ElasticsearchTestStore.Instance;
}
