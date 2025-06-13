// Copyright (c) Microsoft. All rights reserved.

using Elasticsearch.ConformanceTests.Support;

using VectorData.ConformanceTests.CRUD;

using Xunit;

namespace Elasticsearch.ConformanceTests.CRUD;

public class ElasticsearchRecordConformanceTests(ElasticsearchSimpleModelFixture fixture)
    : RecordConformanceTests<string>(fixture), IClassFixture<ElasticsearchSimpleModelFixture>
{
}
