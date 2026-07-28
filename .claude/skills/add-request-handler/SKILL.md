---
name: add-request-handler
description: Scaffold a new request/command/query record and its handler for Light.Mediator, following this repo's conventions (record types, non-generic void handlers with plain Task Handle(), no manual DI registration needed since AddMediatorFromAssemblies scans by assembly). Use when the user asks to add a new command, query, request, or handler in this repo.
---

# Add a request/command/query handler

Light.Mediator handlers are plain classes discovered by assembly scanning (`AddMediatorFromAssemblies` in `src/Mediator/ServiceCollectionExtensions.cs`) — there is no manual registration step to add per handler, only a project reference to `Light.Mediator`/`Light.Mediator.Contracts` if the target project doesn't already have one.

## 1. Pick the right marker interface

- `IQuery<TResponse>` — read-only, returns a value.
- `ICommand<TResponse>` — write, returns a value.
- `ICommand` — write, no return value (void command — do **not** hand-roll a `Unit` return).
- `IRequest<TResponse>` / `IRequest` — use only when the operation is neither clearly a command nor a query.

These are all just `IRequest`/`IRequest<T>` under the hood; the choice is naming/intent only, it has no effect on dispatch.

## 2. Define the request as a `record`

```csharp
public record GetOrderById(int Id) : IQuery<OrderDto>;

public record DeleteOrder(int Id) : ICommand;          // void — no Unit
```

## 3. Write the handler

Generic (with response) — implement `IQueryHandler<TQuery,TResponse>` / `ICommandHandler<TCommand,TResponse>` / `IRequestHandler<TRequest,TResponse>`:

```csharp
public class GetOrderByIdHandler : IQueryHandler<GetOrderById, OrderDto>
{
    public Task<OrderDto> Handle(GetOrderById request, CancellationToken cancellationToken)
        => Task.FromResult(new OrderDto(request.Id, "Sample Order"));
}
```

Void — implement `ICommandHandler<TCommand>` / `IRequestHandler<TRequest>` with a plain `Task Handle()`, no `Unit`:

```csharp
public class DeleteOrderHandler : ICommandHandler<DeleteOrder>
{
    public Task Handle(DeleteOrder request, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
```

The library bridges void handlers to the internal `Task<Unit>` pipeline automatically via `VoidRequestHandlerAdapter<TRequest>` — never implement that adapter or `Unit` yourself in consumer code.

## 4. Registration

Nothing to do manually as long as the handler's assembly is already passed to `AddMediatorFromAssemblies(...)` at startup. Only add a new assembly reference there if the handler lives in a brand-new project.

## 5. Tests (this repo only)

If adding tests under `tests/Mediator.Tests`, mirror the existing style:
- Define request + handler in a plain file matching the naming used by neighbors (`Commands.cs`, `Queries.cs`, `Ping.cs`) — small POCOs/records with an `Executed`/result-tracking property if the test needs to assert the handler ran.
- Wire dependencies through `FakeServiceProvider` (`.Register<IRequestHandler<TRequest,TResponse>>(...)`, or `.RegisterVoidHandler<TRequest>(...)` for void handlers) instead of a real DI container or mocking library.
- Assert with the repo's `Assert` helper (`Assert.ShouldBe`, `Assert.ShouldThrowAsync`, `Assert.ShouldHaveCount`, ...), not raw `NUnit.Framework.Assert.That`.
