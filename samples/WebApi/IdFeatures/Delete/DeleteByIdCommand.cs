namespace WebApi.IdFeatures.Delete;

public record DeleteByIdCommand(string Id) : IRequest<bool>;

internal class DeleteByIdCommandHandler(
    IPublisher publisher,
    ILogger<DeleteByIdCommandHandler> logger)
    : IRequestHandler<DeleteByIdCommand, bool>
{
    public async Task<bool> Handle(DeleteByIdCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting ID: {id}", request.Id);

        await Task.Yield(); // Simulate some async work

        await publisher.Publish(new Events.IdDeletedEvent(request.Id), cancellationToken);
    
        return true;
    }
}