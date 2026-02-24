// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Elastic.Extensions.AI;

/// <summary>
/// Options for configuring the <see cref="ElasticsearchEmbeddingGenerator{TEmbedding}"/>.
/// </summary>
public class ElasticsearchEmbeddingGeneratorOptions
{
    /// <summary>
    /// Gets or sets the inference endpoint ID configured in Elasticsearch.
    /// This is the ID of a pre-configured inference endpoint that will be used to generate embeddings.
    /// </summary>
    /// <remarks>
    /// The inference endpoint must be configured in Elasticsearch before using this generator.
    /// See https://www.elastic.co/guide/en/elasticsearch/reference/current/put-inference-api.html
    /// </remarks>
    public required string InferenceEndpointId { get; set; }

    // TODO: Check if there is a generic way to override these values in the request.

    ///// <summary>
    ///// Gets or sets the optional model ID to use with the inference endpoint.
    ///// If not specified, the default model configured for the endpoint will be used.
    ///// </summary>
    //public string? ModelId { get; set; }

    ///// <summary>
    ///// Gets or sets the optional number of dimensions for the generated embeddings.
    ///// If not specified, the default dimensions for the model will be used.
    ///// </summary>
    //public int? Dimensions { get; set; }

    /// <summary>
    /// Creates a copy of this options instance.
    /// </summary>
    /// <returns>A new <see cref="ElasticsearchEmbeddingGeneratorOptions"/> with the same values.</returns>
    internal ElasticsearchEmbeddingGeneratorOptions Clone()
    {
        return new ElasticsearchEmbeddingGeneratorOptions
        {
            InferenceEndpointId = InferenceEndpointId
            //ModelId = ModelId,
            //Dimensions = Dimensions
        };
    }
}
