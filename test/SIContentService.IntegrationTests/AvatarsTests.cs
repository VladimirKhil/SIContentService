using SIContentService.Contract.Models;
using System.Text;

namespace SIContentService.IntegrationTests;

[TestFixture]
internal sealed class AvatarsTests : TestsBase
{
    [Test]
    public async Task UploadAvatar_Ok()
    {
        var testName = $"test_{new Random().Next(10000)}.jpg";
        var testBytes = Encoding.UTF8.GetBytes(testName);
        var fileKey = new FileKey(testName, testBytes);

        var noAvatar = await SIContentClient.TryGetAvatarUriAsync(fileKey);
        Assert.That(noAvatar, Is.Null);

        string avatarUri;

        using (var ms = new MemoryStream())
        {
            ms.Write(testBytes);
            ms.Position = 0;

            avatarUri = await SIContentClient.UploadAvatarAsync(fileKey, ms);
        }

        var avatarUri2 = await SIContentClient.TryGetAvatarUriAsync(fileKey);
        Assert.That(avatarUri2, Is.EqualTo(avatarUri));

        if (!TestNginxPart)
        {
            return;
        }

        var contentResponse = await SIContentClient.GetAsync(avatarUri);

        Assert.That(contentResponse.IsSuccessStatusCode, $"{contentResponse.StatusCode}: {await contentResponse.Content.ReadAsStringAsync()}");

        var data = await contentResponse.Content.ReadAsByteArrayAsync();
        CollectionAssert.AreEqual(testBytes, data);
    }
}
