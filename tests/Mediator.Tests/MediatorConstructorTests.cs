using MediatorClass = Light.Mediator.Mediator;

namespace Mediator.Tests;

[TestFixture]
public class MediatorConstructorTests
{
    [Test]
    public void Constructor_NullServiceProvider_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ShouldThrow<ArgumentNullException>(() => new MediatorClass(null!));
        Assert.ShouldBe(ex.ParamName, "serviceProvider");
    }

    [Test]
    public void Constructor_ValidServiceProvider_ShouldCreateInstance()
    {
        var provider = new FakeServiceProvider();
        var mediator = new MediatorClass(provider);
        Assert.ShouldNotBeNull(mediator);
    }
}
