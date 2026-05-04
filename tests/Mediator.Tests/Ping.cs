using Light.Mediator;

namespace Mediator.Tests;

public record PingRequest(string Message) : IRequest<PongResponse>;

public record PongResponse(string Reply);

public class PingHandler : IRequestHandler<PingRequest, PongResponse>
{
    public Task<PongResponse> Handle(PingRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new PongResponse($"Pong: {request.Message}"));
}