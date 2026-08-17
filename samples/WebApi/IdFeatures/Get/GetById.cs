namespace WebApi.IdFeatures.Get;

public class GetById
{
    public record Query(string Id) : IQuery<IdDto>;

    internal class Handler : IQueryHandler<Query, IdDto>
    {
        public Task<IdDto> Handle(Query request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new IdDto(request.Id, $"Name of {request.Id}"));
        }
    }
}
