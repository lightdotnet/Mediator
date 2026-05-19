namespace WebApi.Behaviors;

public class TestBehavior<TRequest, TResponse>(
    ILogger<TestBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogWarning("Handle request from TEST behavior");

        var response = await next(cancellationToken);

        return response;
    }
}