// Copyright (c) Microsoft. All rights reserved.

using Elasticsearch.ConformanceTests.Support;

using VectorData.ConformanceTests.CRUD;

using Xunit;

namespace Elasticsearch.ConformanceTests.CRUD;

public class ElasticsearchDynamicDataModelConformanceTests(ElasticsearchDynamicDataModelFixture fixture)
    : DynamicDataModelConformanceTests<string>(fixture), IClassFixture<ElasticsearchDynamicDataModelFixture>
{
}
