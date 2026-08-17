namespace WebApi.IdFeatures.RequestsWithOneHandler;

public class Handler
    : ICommandHandler<NewStringIdCommand, string>,
    ICommandHandler<NewIntIdCommand, int>
{
    public Task<string> Handle(NewStringIdCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Guid.NewGuid().ToString());
    }

    public Task<int> Handle(NewIntIdCommand request, CancellationToken cancellationToken)
    {
        Random rdm = new Random();
        return Task.FromResult(rdm.Next(1, 100));
    }
}
