using System.Collections.Generic;
using System.Linq.Expressions;

using Elastic.Clients.Elasticsearch.QueryDsl;

using Microsoft.Extensions.VectorData.ConnectorSupport;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

internal static class ElasticsearchFilterTranslator
{
    public static ICollection<Query> Translate(LambdaExpression lambdaExpression, VectorStoreRecordModel model)
    {
        return null!;
    }
}
