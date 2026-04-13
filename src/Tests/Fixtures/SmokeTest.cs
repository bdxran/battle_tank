using NUnit.Framework;

namespace BattleTank.Tests.Fixtures;

[TestFixture]
public class SmokeTest
{
    [Test]
    public void TestRunner_IsConfigured()
    {
        Assert.Pass("Setup OK");
    }
}
