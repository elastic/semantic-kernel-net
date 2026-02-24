// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Elastic.Extensions.AI.Tests;

public class ElasticsearchEmbeddingGeneratorOptionsTests
{
    [Fact]
    public void Clone_CreatesNewInstanceWithSameValues()
    {
        var original = new ElasticsearchEmbeddingGeneratorOptions
        {
            InferenceEndpointId = "test-endpoint",
            //ModelId = "test-model",
            //Dimensions = 1536
        };

        var cloned = original.Clone();

        Assert.NotSame(original, cloned);
        Assert.Equal(original.InferenceEndpointId, cloned.InferenceEndpointId);
        //Assert.Equal(original.ModelId, cloned.ModelId);
        //Assert.Equal(original.Dimensions, cloned.Dimensions);
    }

    [Fact]
    public void Clone_ModifyingClone_DoesNotAffectOriginal()
    {
        var original = new ElasticsearchEmbeddingGeneratorOptions
        {
            InferenceEndpointId = "test-endpoint",
            //ModelId = "test-model",
            //Dimensions = 1536
        };

        var cloned = original.Clone();
        cloned.InferenceEndpointId = "modified-endpoint";
        //cloned.ModelId = "modified-model";
        //cloned.Dimensions = 768;

        Assert.Equal("test-endpoint", original.InferenceEndpointId);
        //Assert.Equal("test-model", original.ModelId);
        //Assert.Equal(1536, original.Dimensions);
    }

    //[Fact]
    //public void Options_AllowsNullModelId()
    //{
    //    var options = new ElasticsearchEmbeddingGeneratorOptions
    //    {
    //        InferenceEndpointId = "test-endpoint",
    //        ModelId = null
    //    };

    //    Assert.Null(options.ModelId);
    //}

    //[Fact]
    //public void Options_AllowsNullDimensions()
    //{
    //    var options = new ElasticsearchEmbeddingGeneratorOptions
    //    {
    //        InferenceEndpointId = "test-endpoint",
    //        Dimensions = null
    //    };

    //    Assert.Null(options.Dimensions);
    //}
}
