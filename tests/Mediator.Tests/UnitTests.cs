using Light.Mediator;

namespace Mediator.Tests;

[TestFixture]
public class UnitTests
{
    [Test] public void Value_ShouldBeDefault() => Assert.ShouldBe(Unit.Value, default(Unit));
    [Test] public async Task Task_ShouldReturnCompletedUnitTask() { var r = await Unit.Task; Assert.ShouldBe(r, Unit.Value); }
    [Test] public void Task_ShouldBeSameInstance() => Assert.ShouldBeSameAs(Unit.Task, Unit.Task);
    [Test] public void Equals_Unit_True() => Assert.ShouldBeTrue(Unit.Value.Equals(new Unit()));
    [Test] public void Equals_BoxedUnit_True() => Assert.ShouldBeTrue(Unit.Value.Equals((object)new Unit()));
    [Test] public void Equals_NonUnit_False() => Assert.ShouldBeFalse(Unit.Value.Equals("not unit"));
    [Test] public void Equals_Null_False() => Assert.ShouldBeFalse(Unit.Value.Equals(null));
    [Test] public void GetHashCode_Zero() => Assert.ShouldBe(Unit.Value.GetHashCode(), 0);
    [Test] public void ToString_Parentheses() => Assert.ShouldBe(Unit.Value.ToString(), "()");
    [Test] public void OperatorEquals_True() => Assert.ShouldBeTrue(Unit.Value == new Unit());
    [Test] public void OperatorNotEquals_False() => Assert.ShouldBeFalse(Unit.Value != new Unit());
}
