namespace WebApi.IdFeatures.Delete;

public class DeleteByIdBehavior(
    ILogger<DeleteByIdBehavior> logger) : IPipelineBehavior<DeleteByIdCommand, bool>
{
    public Task<bool> Handle(
        DeleteByIdCommand request,
        Func<CancellationToken, Task<bool>> next,
        CancellationToken cancellationToken)
    {
        logger.LogWarning("DeleteByIdBehavior: Deleting entity with Id: {Id}", request.Id);

        //return Task.FromResult(true);

        return next(cancellationToken);
    }
}
