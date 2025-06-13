using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Elastic.Clients.Elasticsearch;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.VectorData.ProviderServices;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

internal sealed class ElasticsearchModelBuilder :
    CollectionModelBuilder
{
    internal const string SupportedVectorTypes =
        "ReadOnlyMemory<float>, " +
        "IEnumerable<float>, " +
        "IReadOnlyCollection<float>, " +
        "ICollection<float>, " +
        "IReadOnlyList<float>, " +
        "IList<float>, " +
        "Embedding<float>, " +
        "or float[]";

    private static CollectionModelBuildingOptions GetModelBuildOptions()
    {
        return new CollectionModelBuildingOptions
        {
            RequiresAtLeastOneVector = false,
            SupportsMultipleKeys = false,
            SupportsMultipleVectors = true,
            UsesExternalSerializer = true
        };
    }

    private readonly IElasticsearchClientSettings _clientSettings;
    private bool _dynamicMapping;

    public ElasticsearchModelBuilder(IElasticsearchClientSettings clientSettings) :
        base(GetModelBuildOptions())
    {
        _clientSettings = clientSettings;
    }

    public override CollectionModel Build(Type type, VectorStoreCollectionDefinition? definition, IEmbeddingGenerator? defaultEmbeddingGenerator)
    {
        _dynamicMapping = false;
        return base.Build(type, definition, defaultEmbeddingGenerator);
    }

    public override CollectionModel BuildDynamic(VectorStoreCollectionDefinition definition, IEmbeddingGenerator? defaultEmbeddingGenerator)
    {
        _dynamicMapping = true;
        return base.BuildDynamic(definition, defaultEmbeddingGenerator);
    }

    protected override bool IsKeyPropertyTypeValid(Type type, [NotNullWhen(false)] out string? supportedTypes)
    {
        supportedTypes = "string, long, Guid";

        return (type == typeof(string)) ||
               (type == typeof(long)) ||
               (type == typeof(Guid));
    }

    protected override bool IsDataPropertyTypeValid(Type type, [NotNullWhen(false)] out string? supportedTypes)
    {
        supportedTypes = null;
        return true;
    }

    protected override bool IsVectorPropertyTypeValid(Type type, [NotNullWhen(false)] out string? supportedTypes)
    {
        return IsVectorPropertyTypeValidCore(type, out supportedTypes);
    }

    internal static bool IsVectorPropertyTypeValidCore(Type type, [NotNullWhen(false)] out string? supportedTypes)
    {
        supportedTypes = SupportedVectorTypes;

        return (type == typeof(ReadOnlyMemory<float>)) ||
               (type == typeof(ReadOnlyMemory<float>?)) ||
               (type == typeof(IEnumerable<float>)) ||
               (type == typeof(IReadOnlyCollection<float>)) ||
               (type == typeof(ICollection<float>)) ||
               (type == typeof(IReadOnlyList<float>)) ||
               (type == typeof(IList<float>)) ||
               (type == typeof(Embedding<float>)) ||
               (type == typeof(float[]));
    }

    protected override void Customize()
    {
        if (_dynamicMapping)
        {
            CustomizeDynamic();
            return;
        }

        CustomizeStatic();
    }

    private void CustomizeDynamic()
    {
        // Prioritize the user provided `StoragePropertyName` or fall-back to using the `DefaultFieldNameInferrer`
        // function of the Elasticsearch client which by default redirects to the
        // `JsonSerializerOptions.PropertyNamingPolicy.Convert() method.

        foreach (var property in Properties)
        {
            if (!ReferenceEquals(property.StorageName, property.ModelName))
            {
                // `StorageName` resolves to `ModelName` if not overwritten.
                continue;
            }

            property.StorageName = _clientSettings.DefaultFieldNameInferrer(property.ModelName);
        }
    }

    private void CustomizeStatic()
    {
        // Use the built-in property name inference of the Elasticsearch client. The default implementation
        // prioritizes `JsonPropertyName` attributes and falls-back to the `DefaultFieldNameInferrer` function,
        // which by default redirects to the `JsonSerializerOptions.PropertyNamingPolicy.Convert() method.

        foreach (var property in Properties)
        {
            if (!ReferenceEquals(property.StorageName, property.ModelName))
            {
                // `StorageName` resolves to `ModelName` if not overwritten.
                continue;
            }

            if (property.PropertyInfo is null)
            {
                continue;
            }

            property.StorageName = _clientSettings.Inferrer.PropertyName(property.PropertyInfo);
        }
    }
}
