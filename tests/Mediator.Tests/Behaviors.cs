using Light.Mediator;

namespace Mediator.Tests;

public class TrackingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly List<string> _log;
    private readonly string _name;

    public TrackingBehavior(List<string> log, string name)
    {
        _log = log;
        _name = name;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _log.Add($"{_name}:Before");
        var result = await next(cancellationToken);
        _log.Add($"{_name}:After");
        return result;
    }
}

public class ShortCircuitBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly TResponse _result;

    public ShortCircuitBehavior(TResponse result) => _result = result;

    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Never calls next - short-circuits the pipeline
        return Task.FromResult(_result);
    }
}

public class ThrowingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Behavior exploded");
    }
}
