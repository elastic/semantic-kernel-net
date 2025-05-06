// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

using Elastic.Clients.Elasticsearch;

using Microsoft.Extensions.VectorData;

namespace Elastic.SemanticKernel.Connectors.Elasticsearch;

/// <summary>
/// Interface for constructing <see cref="IVectorStoreRecordCollection{TKey,TRecord}" /> Elasticsearch instances when using <see cref="IVectorStore" /> to retrieve these.
/// </summary>
[Obsolete("To control how collections are instantiated, extend your provider's IVectorStore implementation and override GetCollection()")]
public interface IElasticsearchVectorStoreRecordCollectionFactory
{
    /// <summary>
    /// Constructs a new instance of the <see cref="IVectorStoreRecordCollection{TKey, TRecord}" />.
    /// </summary>
    /// <typeparam name="TKey">The data type of the record key.</typeparam>
    /// <typeparam name="TRecord">The data model to use for adding, updating and retrieving data from storage.</typeparam>
    /// <param name="elasticsearchClient">Elasticsearch client that can be used to manage the collections and documents in an Elasticsearch store.</param>
    /// <param name="name">The name of the collection to connect to.</param>
    /// <param name="vectorStoreRecordDefinition">An optional record definition that defines the schema of the record type. If not present, attributes on <typeparamref name="TRecord" /> will be used.</param>
    /// <returns>The new instance of <see cref="IVectorStoreRecordCollection{TKey, TRecord}" />.</returns>
    public IVectorStoreRecordCollection<TKey, TRecord> CreateVectorStoreRecordCollection<TKey, TRecord>(
        ElasticsearchClient elasticsearchClient, string name, VectorStoreRecordDefinition? vectorStoreRecordDefinition)
        where TKey : notnull
        where TRecord : notnull;
}
