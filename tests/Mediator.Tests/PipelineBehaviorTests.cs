using Light.Mediator;
using MediatorClass = Light.Mediator.Mediator;

namespace Mediator.Tests;

[TestFixture]
public class PipelineBehaviorTests
{
    [Test]
    public async Task Send_WithSingleBehavior_ShouldExecuteBehavior()
    {
        var log = new List<string>();
        var behavior = new TrackingBehavior<PingRequest, PongResponse>(log, "B1");
        var provider = new FakeServiceProvider()
            .Register<IRequestHandler<PingRequest, PongResponse>>(new PingHandler())
            .Register<IPipelineBehavior<PingRequest, PongResponse>>(behavior);
        var mediator = new MediatorClass(provider);
        var response = await mediator.Send(new PingRequest("Hello"));
        Assert.ShouldBe(response.Reply, "Pong: Hello");
        Assert.ShouldHaveCount(log, 2);
        Assert.ShouldBe(log[0], "B1:Before");
        Assert.ShouldBe(log[1], "B1:After");
    }

    [Test]
    public async Task Send_WithMultipleBehaviors_ShouldExecuteInCorrectOrder()
    {
        var log = new List<string>();
        var outer = new TrackingBehavior<PingRequest, PongResponse>(log, "Outer");
        var inner = new TrackingBehavior<PingRequest, PongResponse>(log, "Inner");
        var provider = new FakeServiceProvider()
            .Register<IRequestHandler<PingRequest, PongResponse>>(new PingHandler())
            .Register<IPipelineBehavior<PingRequest, PongResponse>>(outer)
            .Register<IPipelineBehavior<PingRequest, PongResponse>>(inner);
        var mediator = new MediatorClass(provider);
        await mediator.Send(new PingRequest("test"));
        Assert.ShouldHaveCount(log, 4);
        Assert.ShouldBe(log[0], "Outer:Before");
        Assert.ShouldBe(log[1], "Inner:Before");
        Assert.ShouldBe(log[2], "Inner:After");
        Assert.ShouldBe(log[3], "Outer:After");
    }

    [Test]
    public async Task Send_WithNoBehaviors_ShouldCallHandlerDirectly()
    {
        var provider = new FakeServiceProvider()
            .Register<IRequestHandler<PingRequest, PongResponse>>(new PingHandler());
        var mediator = new MediatorClass(provider);
        var response = await mediator.Send(new PingRequest("fast"));
        Assert.ShouldBe(response.Reply, "Pong: fast");
    }

    [Test]
    public async Task Send_BehaviorCanShortCircuit_HandlerNotCalled()
    {
        var sc = new ShortCircuitBehavior<PingRequest, PongResponse>(new PongResponse("Short-circuited"));
        var provider = new FakeServiceProvider()
            .Register<IRequestHandler<PingRequest, PongResponse>>(new PingHandler())
            .Register<IPipelineBehavior<PingRequest, PongResponse>>(sc);
        var mediator = new MediatorClass(provider);
        var response = await mediator.Send(new PingRequest("ignored"));
        Assert.ShouldBe(response.Reply, "Short-circuited");
    }

    [Test]
    public void Send_BehaviorThrows_ShouldPropagateException()
    {
        var tb = new ThrowingBehavior<PingRequest, PongResponse>();
        var provider = new FakeServiceProvider()
            .Register<IRequestHandler<PingRequest, PongResponse>>(new PingHandler())
            .Register<IPipelineBehavior<PingRequest, PongResponse>>(tb);
        var mediator = new MediatorClass(provider);
        var ex = Assert.ShouldThrowAsync<InvalidOperationException>(
            () => mediator.Send(new PingRequest("test")));
        Assert.ShouldBe(ex.Message, "Behavior exploded");
    }
}
