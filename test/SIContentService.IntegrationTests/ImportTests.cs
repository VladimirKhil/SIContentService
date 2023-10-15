using System.Net;

namespace SIContentService.IntegrationTests;

[TestFixture]
public sealed class ImportTests : TestsBase
{
    private const string ExternalPackageUri = "https://vladimirkhil.com/sistorage/packages/98658de3-5992-43ed-9655-4ed3c02672a2.siq";

    [Test]
    [Ignore("Test should only be run manually with authorization secret provided")]
    public async Task ImportPackage_Ok()
    {
        var externalUri = new Uri(ExternalPackageUri);
        var packageUri = await SIContentClient.Import.ImportPackageAsync(externalUri);
        Assert.That(packageUri, Is.Not.Null);

        var contentResponse = await SIContentClient.GetAsync(packageUri + "/content.xml");
        Assert.That(contentResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Content status code: {contentResponse.StatusCode}");
    }
}
