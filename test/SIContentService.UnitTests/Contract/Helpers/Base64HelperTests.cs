using SIContentService.Contract.Helpers;

namespace SIContentService.UnitTests.Contract.Helpers;

internal sealed class Base64HelperTests
{
    [Test]
    public void EscapeBase64_Ok()
    {
        var result = Base64Helper.EscapeBase64("abc+d/EE==");

        Assert.That(result, Is.EqualTo("abc-d_EE"));
    }
}
