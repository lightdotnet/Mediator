using Light.Mediator;
using MediatorClass = Light.Mediator.Mediator;

namespace Mediator.Tests;

[TestFixture]
public class MediatorPublishTests
{
    [Test]
    public async Task Publish_ShouldRouteToHandler()
    {
        var handler = new TrackingNotificationHandler();
        var provider = new FakeServiceProvider()
            .Register<INotificationHandler<SimpleNotification>>(handler);
        var mediator = new MediatorClass(provider);

        await mediator.Publish(new SimpleNotification("Hello"));

        Assert.ShouldHaveCount(handler.Messages, 1);
        Assert.ShouldBe(handler.Messages[0], "Hello");
    }

    [Test]
    public async Task Publish_ShouldRouteToMultipleHandlers()
    {
        var handler1 = new TrackingNotificationHandler();
        var handler2 = new TrackingNotificationHandler();
        var provider = new FakeServiceProvider()
            .Register<INotificationHandler<SimpleNotification>>(handler1)
            .Register<INotificationHandler<SimpleNotification>>(handler2);
        var mediator = new MediatorClass(provider);

        await mediator.Publish(new SimpleNotification("Hi"));

        Assert.ShouldHaveCount(handler1.Messages, 1);
        Assert.ShouldHaveCount(handler2.Messages, 1);
        Assert.ShouldBe(handler1.Messages[0], "Hi");
        Assert.ShouldBe(handler2.Messages[0], "Hi");
    }

    [Test]
    public void Publish_WithNoHandlers_ShouldNotThrow()
    {
        var provider = new FakeServiceProvider();
        var mediator = new MediatorClass(provider);

        Assert.ShouldNotThrowAsync(
            () => mediator.Publish(new SimpleNotification("test")));
    }

    [Test]
    public void Publish_NullNotification_ShouldThrowArgumentNullException()
    {
        var provider = new FakeServiceProvider();
        var mediator = new MediatorClass(provider);

        var ex = Assert.ShouldThrowAsync<ArgumentNullException>(
            () => mediator.Publish(null!));

        Assert.ShouldBe(ex.ParamName, "notification");
    }

    [Test]
    public async Task Publish_OneHandlerThrows_OthersShouldStillExecute()
    {
        var tracking = new TrackingNotificationHandler();
        var throwing = new ThrowingNotificationHandler();
        var provider = new FakeServiceProvider()
            .Register<INotificationHandler<SimpleNotification>>(throwing)
            .Register<INotificationHandler<SimpleNotification>>(tracking);
        var mediator = new MediatorClass(provider);

        var ex = Assert.ShouldThrowAsync<AggregateException>(
            () => mediator.Publish(new SimpleNotification("test")));

        Assert.ShouldHaveCount(ex.InnerExceptions, 1);
        Assert.ShouldBeOfType<InvalidOperationException>(ex.InnerExceptions[0]);

        Assert.ShouldHaveCount(tracking.Messages, 1);
    }

    [Test]
    public void Publish_OperationCanceledException_ShouldPropagateImmediately()
    {
        var cancelling = new CancellingNotificationHandler();
        var tracking = new TrackingNotificationHandler();
        var provider = new FakeServiceProvider()
            .Register<INotificationHandler<SimpleNotification>>(cancelling)
            .Register<INotificationHandler<SimpleNotification>>(tracking);
        var mediator = new MediatorClass(provider);

        Assert.ShouldThrowAsync<OperationCanceledException>(
            () => mediator.Publish(new SimpleNotification("test")));

        Assert.ShouldHaveCount(tracking.Messages, 0);
    }

    [Test]
    public void Publish_AllHandlersThrow_AggregateExceptionContainsAll()
    {
        var throwing1 = new ThrowingNotificationHandler();
        var throwing2 = new ThrowingNotificationHandler();
        var provider = new FakeServiceProvider()
            .Register<INotificationHandler<SimpleNotification>>(throwing1)
            .Register<INotificationHandler<SimpleNotification>>(throwing2);
        var mediator = new MediatorClass(provider);

        var ex = Assert.ShouldThrowAsync<AggregateException>(
            () => mediator.Publish(new SimpleNotification("test")));

        Assert.ShouldHaveCount(ex.InnerExceptions, 2);
        Assert.ShouldBeOfType<InvalidOperationException>(ex.InnerExceptions[0]);
        Assert.ShouldBeOfType<InvalidOperationException>(ex.InnerExceptions[1]);
    }

    [Test]
    public void Publish_TaskCanceledException_ShouldPropagateImmediately()
    {
        var tceHandler = new TaskCancellingNotificationHandler();
        var tracking = new TrackingNotificationHandler();
        var provider = new FakeServiceProvider()
            .Register<INotificationHandler<SimpleNotification>>(tceHandler)
            .Register<INotificationHandler<SimpleNotification>>(tracking);
        var mediator = new MediatorClass(provider);

        Assert.ShouldThrowAsync<TaskCanceledException>(
            () => mediator.Publish(new SimpleNotification("test")));

        Assert.ShouldHaveCount(tracking.Messages, 0);
    }
}
