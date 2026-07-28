using Light.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator.Tests;

public class CountingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly List<string> _log;
    public CountingBehavior(List<string> log) => _log = log;

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        _log.Add(nameof(CountingBehavior<TRequest, TResponse>));
        return next(ct);
    }
}

public class OtherCountingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly List<string> _log;
    public OtherCountingBehavior(List<string> log) => _log = log;

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        _log.Add(nameof(OtherCountingBehavior<TRequest, TResponse>));
        return next(ct);
    }
}

public class ClosedCountingBehavior : IPipelineBehavior<PingRequest, PongResponse>
{
    private readonly List<string> _log;
    public ClosedCountingBehavior(List<string> log) => _log = log;

    public Task<PongResponse> Handle(
        PingRequest request, RequestHandlerDelegate<PongResponse> next, CancellationToken ct)
    {
        _log.Add(nameof(ClosedCountingBehavior));
        return next(ct);
    }
}

public class MultiInterfaceBehavior : IPipelineBehavior<PingRequest, PongResponse>, IPipelineBehavior<DeleteOrder>
{
    private readonly List<string> _log;
    public MultiInterfaceBehavior(List<string> log) => _log = log;

    public Task<PongResponse> Handle(
        PingRequest request, RequestHandlerDelegate<PongResponse> next, CancellationToken ct)
    {
        _log.Add("Ping");
        return next(ct);
    }

    public Task<Unit> Handle(DeleteOrder request, RequestHandlerDelegate<Unit> next, CancellationToken ct)
    {
        _log.Add("Delete");
        return next(ct);
    }
}

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    private static IServiceProvider BuildProvider(List<string> log, params Type[] behaviorTypes)
    {
        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddMediatorFromAssemblies(typeof(PingHandler).Assembly);
        services.AddBehaviors(behaviorTypes);
        return services.BuildServiceProvider();
    }

    [Test]
    public async Task AddBehaviors_CalledTwiceWithSameOpenGenericType_RegistersOnlyOnce()
    {
        var log = new List<string>();
        var provider = BuildProvider(log, typeof(CountingBehavior<,>), typeof(CountingBehavior<,>));
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new PingRequest("hi"));

        Assert.ShouldHaveCount(log, 1);
    }

    [Test]
    public async Task AddBehaviors_CalledTwiceWithSameClosedType_RegistersOnlyOnce()
    {
        var log = new List<string>();
        var provider = BuildProvider(log, typeof(ClosedCountingBehavior), typeof(ClosedCountingBehavior));
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new PingRequest("hi"));

        Assert.ShouldHaveCount(log, 1);
    }

    [Test]
    public async Task AddBehaviors_WithTwoDistinctOpenGenericTypes_RegistersBoth()
    {
        var log = new List<string>();
        var provider = BuildProvider(log, typeof(CountingBehavior<,>), typeof(OtherCountingBehavior<,>));
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new PingRequest("hi"));

        Assert.ShouldHaveCount(log, 2);
    }

    [Test]
    public async Task AddBehaviors_ClosedTypeImplementingMultipleInterfaces_RegistersAll()
    {
        var log = new List<string>();
        var provider = BuildProvider(log, typeof(MultiInterfaceBehavior));
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new PingRequest("hi"));
        await mediator.Send(new DeleteOrder(1));

        Assert.ShouldHaveCount(log, 2);
        Assert.ShouldBe(log[0], "Ping");
        Assert.ShouldBe(log[1], "Delete");
    }
}
