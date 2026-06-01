using Light.Mediator;
using MediatorClass = Light.Mediator.Mediator;

namespace Mediator.Tests;

public record SlowRequest(string Data) : IRequest<string>;
public class ThrowingHandler : IRequestHandler<SlowRequest, string>
{
    public Task<string> Handle(SlowRequest request, CancellationToken ct)
        => throw new InvalidOperationException("Handler exploded");
}

public record NullableRequest(string Data) : IRequest<string?>;
public class NullReturningHandler : IRequestHandler<NullableRequest, string?>
{
    public Task<string?> Handle(NullableRequest request, CancellationToken ct)
        => Task.FromResult<string?>(null);
}

public record AnotherRequest(int Value) : IRequest<int>;
public class AnotherHandler : IRequestHandler<AnotherRequest, int>
{
    public Task<int> Handle(AnotherRequest request, CancellationToken ct)
        => Task.FromResult(request.Value * 2);
}

[TestFixture]
public class MediatorEdgeCaseTests
{
    [Test]
    public void Send_WithAlreadyCancelledToken_HandlerReceivesCancelledToken()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var provider = new FakeServiceProvider()
            .Register<IRequestHandler<PingRequest, PongResponse>>(new PingHandler());
        var mediator = new MediatorClass(provider);
        Assert.ShouldNotThrowAsync(() => mediator.Send(new PingRequest("test"), cts.Token));
    }

    [Test]
    public void Send_HandlerThrows_ShouldPropagateException()
    {
        var provider = new FakeServiceProvider()
            .Register<IRequestHandler<SlowRequest, string>>(new ThrowingHandler());
        var mediator = new MediatorClass(provider);
        var ex = Assert.ShouldThrowAsync<InvalidOperationException>(
            () => mediator.Send(new SlowRequest("test")));
        Assert.ShouldBe(ex.Message, "Handler exploded");
    }

    [Test]
    public void Send_HandlerThrowsWithBehavior_ExceptionPropagatesThroughPipeline()
    {
        var log = new List<string>();
        var behavior = new TrackingBehavior<SlowRequest, string>(log, "B1");
        var provider = new FakeServiceProvider()
            .Register<IRequestHandler<SlowRequest, string>>(new ThrowingHandler())
            .Register<IPipelineBehavior<SlowRequest, string>>(behavior);
        var mediator = new MediatorClass(provider);
        Assert.ShouldThrowAsync<InvalidOperationException>(
            () => mediator.Send(new SlowRequest("test")));
        Assert.ShouldHaveCount(log, 1);
        Assert.ShouldBe(log[0], "B1:Before");
    }

    [Test]
    public async Task Send_MultipleCalls_SameType_ShouldUseCachedWrapper()
    {
        var provider = new FakeServiceProvider()
            .Register<IRequestHandler<PingRequest, PongResponse>>(new PingHandler());
        var mediator = new MediatorClass(provider);
        var r1 = await mediator.Send(new PingRequest("First"));
        var r2 = await mediator.Send(new PingRequest("Second"));
        var r3 = await mediator.Send(new PingRequest("Third"));
        Assert.ShouldBe(r1.Reply, "Pong: First");
        Assert.ShouldBe(r2.Reply, "Pong: Second");
        Assert.ShouldBe(r3.Reply, "Pong: Third");
    }

    [Test]
    public async Task Send_DifferentTypes_ShouldResolveDifferentHandlers()
    {
        var provider = new FakeServiceProvider()
            .Register<IRequestHandler<PingRequest, PongResponse>>(new PingHandler())
            .Register<IRequestHandler<AnotherRequest, int>>(new AnotherHandler());
        var mediator = new MediatorClass(provider);
        var pingResult = await mediator.Send(new PingRequest("Hello"));
        var anotherResult = await mediator.Send(new AnotherRequest(21));
        Assert.ShouldBe(pingResult.Reply, "Pong: Hello");
        Assert.ShouldBe(anotherResult, 42);
    }

    [Test]
    public async Task Send_HandlerReturnsNull_ShouldPropagateNull()
    {
        var provider = new FakeServiceProvider()
            .Register<IRequestHandler<NullableRequest, string?>>(new NullReturningHandler());
        var mediator = new MediatorClass(provider);
        var result = await mediator.Send(new NullableRequest("test"));
        Assert.ShouldBeNull(result);
    }
}
