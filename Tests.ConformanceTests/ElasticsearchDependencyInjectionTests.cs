using System.Globalization;

using Elastic.Clients.Elasticsearch;
using Elastic.SemanticKernel.Connectors.Elasticsearch;
using Elastic.Transport;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using VectorData.ConformanceTests;
using VectorData.ConformanceTests.Models;

using Xunit;

namespace Elasticsearch.ConformanceTests;

public class ElasticsearchDependencyInjectionTests
    : DependencyInjectionTests<ElasticsearchVectorStore, ElasticsearchCollection<string, SimpleRecord<string>>, string, SimpleRecord<string>>
{
    private const string Host = "localhost";
    private const int Port = 8080;
    private const string ApiKey = "fakeKey";

    private static readonly IElasticsearchClientSettings ClientSettings =
        new ElasticsearchClientSettings(new SingleNodePool(new Uri($"https://{Host}:{Port}")))
            .Authentication(new ApiKey(ApiKey));

    protected override void PopulateConfiguration(ConfigurationManager configuration, object? serviceKey = null)
        => configuration.AddInMemoryCollection(
        [
            new(CreateConfigKey("Elasticsearch", serviceKey, "Host"), Host),
            new(CreateConfigKey("Elasticsearch", serviceKey, "Port"), Port.ToString(CultureInfo.InvariantCulture)),
            new(CreateConfigKey("Elasticsearch", serviceKey, "ApiKey"), ApiKey),
        ]);

    private static ElasticsearchClientSettings ClientSettingsProvider(IServiceProvider sp, object? serviceKey = null)
    {
        var host = sp.GetRequiredService<IConfiguration>().GetRequiredSection(CreateConfigKey("Elasticsearch", serviceKey, "Host")).Value!;
        var port = int.Parse(sp.GetRequiredService<IConfiguration>().GetRequiredSection(CreateConfigKey("Elasticsearch", serviceKey, "Port")).Value!);
        var apiKey = sp.GetRequiredService<IConfiguration>().GetRequiredSection(CreateConfigKey("Elasticsearch", serviceKey, "ApiKey")).Value!;

        return new ElasticsearchClientSettings(new SingleNodePool(new Uri($"https://{host}:{port}")))
            .Authentication(new ApiKey(apiKey));
    }

    public override IEnumerable<Func<IServiceCollection, object?, string, ServiceLifetime, IServiceCollection>> CollectionDelegates
    {
        get
        {
            yield return (services, serviceKey, name, lifetime) => serviceKey is null
                ? services
                    .AddSingleton<ElasticsearchClient>(sp => new ElasticsearchClient(ClientSettings))
                    .AddElasticsearchCollection<string, SimpleRecord<string>>(name, lifetime: lifetime)
                : services
                    .AddSingleton<ElasticsearchClient>(sp => new ElasticsearchClient(ClientSettings))
                    .AddKeyedElasticsearchCollection<string, SimpleRecord<string>>(serviceKey, name, lifetime: lifetime);

            yield return (services, serviceKey, name, lifetime) => serviceKey is null
                ? services.AddElasticsearchCollection<string, SimpleRecord<string>>(
                    name, ClientSettings, lifetime: lifetime)
                : services.AddKeyedElasticsearchCollection<string, SimpleRecord<string>>(
                    serviceKey, name, ClientSettings, lifetime: lifetime);

            yield return (services, serviceKey, name, lifetime) => serviceKey is null
                ? services.AddElasticsearchCollection<string, SimpleRecord<string>>(
                    name, sp => new ElasticsearchClient(ClientSettingsProvider(sp)), lifetime: lifetime)
                : services.AddKeyedElasticsearchCollection<string, SimpleRecord<string>>(
                    serviceKey, name, sp => new ElasticsearchClient(ClientSettingsProvider(sp, serviceKey)), lifetime: lifetime);
        }
    }

    public override IEnumerable<Func<IServiceCollection, object?, ServiceLifetime, IServiceCollection>> StoreDelegates
    {
        get
        {
            yield return (services, serviceKey, lifetime) => serviceKey is null
                ? services.AddElasticsearchVectorStore(ClientSettings, lifetime: lifetime)
                : services.AddKeyedElasticsearchVectorStore(serviceKey, ClientSettings, lifetime: lifetime);

            yield return (services, serviceKey, lifetime) => serviceKey is null
                ? services
                    .AddSingleton<ElasticsearchClient>(sp => new ElasticsearchClient(ClientSettings))
                    .AddElasticsearchVectorStore(lifetime: lifetime)
                : services
                    .AddSingleton<ElasticsearchClient>(sp => new ElasticsearchClient(ClientSettings))
                    .AddKeyedElasticsearchVectorStore(serviceKey, lifetime: lifetime);
        }
    }

    [Fact]
    public void ClientSettingsCantBeNull()
    {
        IServiceCollection services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddElasticsearchVectorStore(clientSettings: null!));
        Assert.Throws<ArgumentNullException>(() => services.AddKeyedElasticsearchVectorStore(serviceKey: "notNull", clientSettings: null!));
        Assert.Throws<ArgumentNullException>(() => services.AddElasticsearchCollection<ulong, SimpleRecord<ulong>>(
            name: "notNull", clientSettings: null!));
        Assert.Throws<ArgumentNullException>(() => services.AddKeyedElasticsearchCollection<ulong, SimpleRecord<ulong>>(
            serviceKey: "notNull", name: "notNull", clientSettings: null!));
    }
}
