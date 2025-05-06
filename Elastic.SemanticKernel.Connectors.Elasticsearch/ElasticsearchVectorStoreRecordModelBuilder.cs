using System;
using System.Collections.Generic;

using Elastic.Clients.Elasticsearch;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.VectorData.ConnectorSupport;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

internal sealed class ElasticsearchVectorStoreRecordModelBuilder :
    VectorStoreRecordModelBuilder
{
    private readonly IElasticsearchClientSettings _clientSettings;
    private bool _dynamicMapping;

    public ElasticsearchVectorStoreRecordModelBuilder(VectorStoreRecordModelBuildingOptions options, IElasticsearchClientSettings clientSettings) :
        base(options)
    {
        if (!options.UsesExternalSerializer)
        {
            throw new ArgumentException($"{nameof(options.UsesExternalSerializer)} must be set when using this model builder.", nameof(options));
        }

        _clientSettings = clientSettings;
    }

    public override VectorStoreRecordModel Build(Type type, VectorStoreRecordDefinition? vectorStoreRecordDefinition, IEmbeddingGenerator? defaultEmbeddingGenerator)
    {
        _dynamicMapping = (type == typeof(Dictionary<string, object?>));

        return base.Build(type, vectorStoreRecordDefinition, defaultEmbeddingGenerator);
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
