using Light.Mediator;

namespace Mediator.Tests;

[TestFixture]
public class UnitTests
{
    [Test]
    public void Value_ShouldBeDefault()
    {
        Assert.ShouldBe(Unit.Value, default);
    }

    [Test]
    public async Task Task_ShouldReturnCompletedUnitTask()
    {
        var result = await Unit.Task;
        Assert.ShouldBe(result, Unit.Value);
    }

    [Test]
    public void Task_ShouldBeSameInstance()
    {
        var task1 = Unit.Task;
        var task2 = Unit.Task;
        Assert.ShouldBeSameAs(task1, task2);
    }

    [Test]
    public void Equals_Unit_ShouldReturnTrue()
    {
        var a = Unit.Value;
        var b = new Unit();
        Assert.ShouldBeTrue(a.Equals(b));
    }

    [Test]
    public void Equals_BoxedUnit_ShouldReturnTrue()
    {
        object boxed = new Unit();
        Assert.ShouldBeTrue(Unit.Value.Equals(boxed));
    }

    [Test]
    public void Equals_NonUnit_ShouldReturnFalse()
    {
        Assert.ShouldBeFalse(Unit.Value.Equals("not unit"));
    }

    [Test]
    public void Equals_Null_ShouldReturnFalse()
    {
        Assert.ShouldBeFalse(Unit.Value.Equals(null));
    }

    [Test]
    public void GetHashCode_ShouldBeZero()
    {
        Assert.ShouldBe(Unit.Value.GetHashCode(), 0);
    }

    [Test]
    public void ToString_ShouldReturnParentheses()
    {
        Assert.ShouldBe(Unit.Value.ToString(), "()");
    }

    [Test]
    public void OperatorEquals_ShouldReturnTrue()
    {
        Assert.ShouldBeTrue(Unit.Value == new Unit());
    }

    [Test]
    public void OperatorNotEquals_ShouldReturnFalse()
    {
        Assert.ShouldBeFalse(Unit.Value != new Unit());
    }
}
