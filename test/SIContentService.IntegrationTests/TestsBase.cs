using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIContentService.Client;
using SIContentService.Contract;

namespace SIContentService.IntegrationTests;

public abstract class TestsBase
{
    protected ISIContentServiceClient SIContentClient { get; }

    protected static bool TestNginxPart => false;

    public TestsBase()
    {
        var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
        var configuration = builder.Build();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSIContentServiceClient(configuration);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        SIContentClient = serviceProvider.GetRequiredService<ISIContentServiceClient>();
    }
}