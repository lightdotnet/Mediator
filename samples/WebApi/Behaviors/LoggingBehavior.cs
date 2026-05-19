namespace WebApi.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var traceId = Guid.NewGuid().ToString("N")[..4];

        var timer = System.Diagnostics.Stopwatch.StartNew();

        logger.LogInformation("[{traceId}] Handling {RequestType} with {@Request}", traceId, typeof(TRequest).FullName, request);

        var response = await next(cancellationToken);

        timer.Stop();

        logger.LogInformation("[{traceId}] Done handling {RequestType} in {ms} with {@Response}", traceId, typeof(TRequest).FullName, timer.ElapsedMilliseconds, response);

        return response;
    }
}