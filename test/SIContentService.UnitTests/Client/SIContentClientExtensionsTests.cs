using SIContentService.Client;

namespace SIContentService.UnitTests.Client;

internal sealed class SIContentClientExtensionsTests
{
    [Test]
    public void CreateClient_Ok()
    {
        var client = SIContentClientExtensions.CreateSIContentServiceClient(new SIContentClientOptions());
        Assert.That(client, Is.Not.Null);
    }
}
