using Light.Mediator;

namespace Mediator.Tests;

// Void command (returns Unit)
public record DeleteOrder(int Id) : ICommand;

public class DeleteOrderHandler : ICommandHandler<DeleteOrder>
{
    public bool Executed { get; private set; }

    public Task<Unit> Handle(DeleteOrder request, CancellationToken cancellationToken)
    {
        Executed = true;
        return Unit.Task;
    }
}

// Command with response
public record CreateOrder(string Name) : ICommand<int>;

public class CreateOrderHandler : ICommandHandler<CreateOrder, int>
{
    public Task<int> Handle(CreateOrder request, CancellationToken cancellationToken)
        => Task.FromResult(42);
}
