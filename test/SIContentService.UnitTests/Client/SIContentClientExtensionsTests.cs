using SIContentService.Client;

namespace SIContentService.UnitTests.Client;

internal sealed class SIContentClientExtensionsTests
{
    [Test]
    public void CreateClient_Ok()
    {
        var client = SIContentClientExtensions.CreateSIContentServiceClient(new SIContentClientOptions { ServiceUri = new Uri("http://fake") });
        Assert.That(client, Is.Not.Null);
        Assert.That(client.ServiceUri, Is.EqualTo(new Uri("http://fake")));
    }
}
