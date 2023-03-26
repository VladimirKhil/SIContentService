using SIContentService.Helpers;

namespace SIContentService.UnitTests.Helpers;

internal sealed class SecurityHelperTests
{
    [TestCase("a.png", ".png")]
    [TestCase("a.verylongextension", ".very")]
    [TestCase("abc.e-*ffjj;;_p", ".ef")]
    [TestCase("a.p", ".p")]
    public void GetSafeExtension_Ok(string input, string output)
    {
        var result = SecurityHelper.GetSafeExtension(input);

        Assert.That(result, Is.EqualTo(output));
    }
}