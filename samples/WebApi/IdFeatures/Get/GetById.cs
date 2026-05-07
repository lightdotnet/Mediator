namespace WebApi.IdFeatures.Get;

public class GetById
{
    public record Query(string Id) : ICommand<IdDto>;

    internal class Handler : ICommandHandler<Query, IdDto>
    {
        public Task<IdDto> Handle(Query request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new IdDto(request.Id, $"Name of {request.Id}"));
        }
    }
}
