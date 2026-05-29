namespace WebApi.IdFeatures.Delete;

public record DeleteByIdCommand(string Id) : IRequest;

internal class DeleteByIdCommandHandler(
    IPublisher publisher,
    ILogger<DeleteByIdCommandHandler> logger)
    : IRequestHandler<DeleteByIdCommand>
{
    public async Task Handle(DeleteByIdCommand request, CancellationToken cancellationToken)
    {
        logger.LogError("Deleting ID: {id}", request.Id);

        //await Task.Yield(); // Simulate some async work

        await publisher.Publish(new Events.IdDeletedEvent(request.Id), cancellationToken);
    }
}