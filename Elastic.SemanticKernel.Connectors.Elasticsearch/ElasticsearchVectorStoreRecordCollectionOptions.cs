using Microsoft.Extensions.VectorData;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
///     Options when creating a <see cref="ElasticsearchVectorStoreRecordCollectionOptions{TRecord}" />.
/// </summary>
public sealed class ElasticsearchVectorStoreRecordCollectionOptions<TRecord>
{
    /// <summary>
    ///     Gets or sets an optional record definition that defines the schema of the record type.
    /// </summary>
    /// <remarks>
    ///     If not provided, the schema will be inferred from the record model class using reflection.
    ///     In this case, the record model properties must be annotated with the appropriate attributes to indicate their
    ///     usage.
    ///     See <see cref="VectorStoreRecordKeyAttribute" />, <see cref="VectorStoreRecordDataAttribute" /> and
    ///     <see cref="VectorStoreRecordVectorAttribute" />.
    /// </remarks>
    public VectorStoreRecordDefinition? VectorStoreRecordDefinition { get; init; } = null;
}
