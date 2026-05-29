namespace WebApi.IdFeatures.Delete;

public class DeleteByIdBehavior(
    ILogger<DeleteByIdBehavior> logger)
    : IPipelineBehavior<DeleteByIdCommand>
{
    public Task<Unit> Handle(
        DeleteByIdCommand request,
        RequestHandlerDelegate<Unit> next,
        CancellationToken cancellationToken)
    {
        logger.LogWarning("DeleteByIdBehavior: Deleting entity with Id: {Id}", request.Id);

        return Unit.Task;

        //return next(cancellationToken);
    }
}
