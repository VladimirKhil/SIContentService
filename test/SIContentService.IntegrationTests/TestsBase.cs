using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SIContentService.Client;
using SIContentService.Contract;

namespace SIContentService.IntegrationTests;

public abstract class TestsBase
{
    protected SIContentClientOptions SIContentClientOptions { get; }

    protected ISIContentServiceClient SIContentClient { get; }

    public TestsBase()
    {
        var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
        var configuration = builder.Build();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSIContentServiceClient(configuration);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        SIContentClientOptions = serviceProvider.GetRequiredService<IOptions<SIContentClientOptions>>().Value;
        SIContentClient = serviceProvider.GetRequiredService<ISIContentServiceClient>();
    }
}