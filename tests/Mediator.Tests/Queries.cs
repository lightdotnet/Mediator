using Light.Mediator;

namespace Mediator.Tests;

public record OrderDto(int Id, string Name);

public record GetOrderById(int Id) : IQuery<OrderDto>;

public class GetOrderByIdHandler : IQueryHandler<GetOrderById, OrderDto>
{
    public Task<OrderDto> Handle(GetOrderById request, CancellationToken cancellationToken)
        => Task.FromResult(new OrderDto(request.Id, "Test Order"));
}
