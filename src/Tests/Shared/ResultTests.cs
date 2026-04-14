using NUnit.Framework;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Tests.Shared;

[TestFixture]
public class ResultTests
{
    [Test]
    public void Ok_IsSuccess_True()
    {
        var result = Result<int>.Ok(42);
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void Ok_Value_IsReturned()
    {
        var result = Result<int>.Ok(42);
        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void Ok_Error_IsEmpty()
    {
        var result = Result<int>.Ok(42);
        Assert.That(result.Error, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Fail_IsSuccess_False()
    {
        var result = Result<int>.Fail("something went wrong");
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void Fail_Error_IsReturned()
    {
        var result = Result<int>.Fail("something went wrong");
        Assert.That(result.Error, Is.EqualTo("something went wrong"));
    }

    [Test]
    public void Fail_Value_IsDefault()
    {
        var result = Result<int>.Fail("err");
        Assert.That(result.Value, Is.EqualTo(default(int)));
    }
}
