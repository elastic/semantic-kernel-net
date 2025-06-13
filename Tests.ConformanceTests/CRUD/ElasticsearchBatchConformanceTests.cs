// Copyright (c) Microsoft. All rights reserved.

using Elasticsearch.ConformanceTests.Support;

using VectorData.ConformanceTests.CRUD;

using Xunit;

namespace Elasticsearch.ConformanceTests.CRUD;

public class ElasticsearchBatchConformanceTests(ElasticsearchSimpleModelFixture fixture)
    : BatchConformanceTests<string>(fixture), IClassFixture<ElasticsearchSimpleModelFixture>
{
}
