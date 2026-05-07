namespace WebApi.IdFeatures.Update;

public record UpdateByIdCommand(string Id, string Name) : ICommand<string>;

internal class Handler(ILogger<Handler> logger) : ICommandHandler<UpdateByIdCommand, string>
{
    public Task<string> Handle(UpdateByIdCommand request, CancellationToken cancellationToken)
    {
        // Generate a new unique identifier
        logger.LogInformation("Updating entity with Id: {Id} and Name: {Name}", request.Id, request.Name);
        return Task.FromResult(request.Id);
    }
}