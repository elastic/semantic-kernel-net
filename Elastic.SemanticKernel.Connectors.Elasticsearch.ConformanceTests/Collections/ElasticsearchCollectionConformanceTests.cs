// Copyright (c) Microsoft. All rights reserved.

using Elasticsearch.ConformanceTests.Support;

using VectorData.ConformanceTests.Collections;

using Xunit;

namespace Elasticsearch.ConformanceTests.Collections;

public class ElasticsearchCollectionConformanceTests(ElasticsearchVectorStoreFixture fixture)
    : CollectionConformanceTests<string>(fixture), IClassFixture<ElasticsearchVectorStoreFixture>
{
    public override string CollectionName => "collection_tests";
}
