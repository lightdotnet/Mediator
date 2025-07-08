namespace WebApi.IdFeatures.Add;

public class AddNewId
{
    public record Command : ICommand<string>;

    internal class Handler : ICommandHandler<Command, string>
    {
        public Task<string> Handle(Command request, CancellationToken cancellationToken)
        {
            // Generate a new unique identifier
            var newId = Guid.NewGuid().ToString();
            return Task.FromResult(newId);
        }
    }
}
