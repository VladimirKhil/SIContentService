using SIContentService.Contract.Models;
using System.Net;

namespace SIContentService.IntegrationTests;

[TestFixture]
public sealed class PackagesTests : TestsBase
{
    [Test]
    public async Task UploadPackage_Ok()
    {
        var packageKey = new FileKey("test_" + new Random().Next(10000), new byte[] { 1, 2, 3 });

        var noPackage = await SIContentClient.TryGetPackageUriAsync(packageKey);
        Assert.That(noPackage, Is.Null);

        string packageUri;

        using (var fs = File.OpenRead("TestPackage.siq"))
        {
            packageUri = await SIContentClient.UploadPackageAsync(packageKey, fs);
        }

        var packageUri2 = await SIContentClient.TryGetPackageUriAsync(packageKey);
        Assert.That(packageUri2, Is.EqualTo(packageUri));

        if (!TestNginxPart)
        {
            return;
        }

        var imageResponse = await SIContentClient.GetAsync(packageUri + "/Images/294F815D5DB6E7F7.PNG");

        Assert.That(imageResponse.IsSuccessStatusCode, $"{imageResponse.StatusCode}: {await imageResponse.Content.ReadAsStringAsync()}");

        var data = await imageResponse.Content.ReadAsByteArrayAsync();
        Assert.That(data, Is.Not.Empty);

        var contentResponse = await SIContentClient.GetAsync(packageUri + "/content.xml");
        Assert.That(contentResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), $"Content status code: {contentResponse.StatusCode}");
    }
}
