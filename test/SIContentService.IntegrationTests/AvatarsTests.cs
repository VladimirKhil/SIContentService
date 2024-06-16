using SIContentService.Contract.Models;
using System.Security.Cryptography;

namespace SIContentService.IntegrationTests;

[TestFixture]
internal sealed class AvatarsTests : TestsBase
{
    [Test]
    public async Task UploadAvatar_Ok()
    {
        var testName = $"te st_{new Random().Next(10000)}.jpg";
        var testBytes = Enumerable.Range(0, 200).Select(n => (byte)n).ToArray();
        var testHash = MD5.HashData(testBytes);
        var fileKey = new FileKey(testName, testHash);

        var noAvatar = await SIContentClient.TryGetAvatarUriAsync(fileKey);
        Assert.That(noAvatar, Is.Null);

        Uri avatarUri;

        using (var ms = new MemoryStream())
        {
            ms.Write(testBytes);
            ms.Position = 0;

            avatarUri = await SIContentClient.UploadAvatarAsync(fileKey, ms);
        }

        var avatarUri2 = await SIContentClient.TryGetAvatarUriAsync(fileKey);
        Assert.That(avatarUri2, Is.EqualTo(avatarUri));

        var contentResponse = await SIContentClient.GetAsync(avatarUri.ToString());

        Assert.That(contentResponse.IsSuccessStatusCode, $"{contentResponse.StatusCode}: {await contentResponse.Content.ReadAsStringAsync()}");

        var data = await contentResponse.Content.ReadAsByteArrayAsync();
        Assert.That(testBytes, Is.EqualTo(data));
    }
}
