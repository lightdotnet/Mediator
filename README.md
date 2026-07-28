# Light.Mediator

A lightweight, high-performance mediator library for .NET — implementing the Mediator pattern with CQRS support, pipeline behaviors, and notification publishing.

[![NuGet](https://img.shields.io/nuget/v/Lightsoft.Mediator.svg?label=Lightsoft.Mediator%20-%20nuget)](https://www.nuget.org/packages/Lightsoft.Mediator)
[![.NET Standard](https://img.shields.io/badge/netstandard-2.1-blue.svg)]()

## ✨ Features

- **CQRS ready** — Separate `ICommand<T>`, `IQuery<T>`, and `IRequest<T>` contracts
- **Clean void commands** — `Task Handle()`, no `Unit` boilerplate needed
- **Pipeline behaviors** — Middleware-style `IPipelineBehavior<TRequest, TResponse>` for cross-cutting concerns
- **Notification publishing** — Fan-out to multiple `INotificationHandler<T>` with error resilience
- **Zero dependencies** on contracts — `Light.Mediator.Contracts` has no external dependencies
- **High performance** — Cached wrapper resolution via `ConcurrentDictionary`, zero-alloc fast-path
- **Safe DI registration** — Assembly scanning with duplicate protection and safe type loading
- **Targets `netstandard2.1`**

## 📦 Installation

```bash
dotnet add package Light.Mediator
```

For projects that only define requests/commands/queries (e.g., shared contracts):

```bash
dotnet add package Light.Mediator.Contracts
```

## 🚀 Quick Start

### 1. Define a request and handler

```csharp
using Light.Mediator;

// Query with response
public record GetOrderById(int Id) : IQuery<OrderDto>;

public class GetOrderByIdHandler : IQueryHandler<GetOrderById, OrderDto>
{
    public Task<OrderDto> Handle(GetOrderById request, CancellationToken ct)
        => Task.FromResult(new OrderDto(request.Id, "Sample Order"));
}

// Void command — clean, no Unit!
public record DeleteOrder(int Id) : ICommand;

public class DeleteOrderHandler : ICommandHandler<DeleteOrder>
{
    public Task Handle(DeleteOrder request, CancellationToken ct)
        => Task.CompletedTask;
}
```

### 2. Register services

```csharp
using Light.Mediator;
using System.Reflection;

builder.Services.AddMediatorFromAssemblies(Assembly.GetExecutingAssembly());
```

### 3. Send requests

```csharp
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;
    public OrdersController(ISender sender) => _sender = sender;

    [HttpGet("{id}")]
    public async Task<OrderDto> Get(int id)
        => await _sender.Send(new GetOrderById(id));

    [HttpDelete("{id}")]
    public async Task Delete(int id)
        => await _sender.Send(new DeleteOrder(id));
}
```

## 📖 Core Concepts

### Contracts (`Light.Mediator.Contracts`)

| Interface | Purpose | Returns |
|---|---|---|
| `IRequest<TResponse>` | Base request with typed response | `TResponse` |
| `IRequest` | Void request (returns `Unit`) | `Unit` |
| `ICommand<TResponse>` | Command with response | `TResponse` |
| `ICommand` | Void command | `Unit` |
| `IQuery<TResponse>` | Query with response | `TResponse` |
| `INotification` | Notification (fan-out) | — |

### Unit Type

`Unit` is a readonly struct used internally for void operations. With the non-generic handler interfaces, **you rarely need to interact with `Unit` directly**:

```csharp
// ✅ Void handler — clean, no Unit!
public class DeleteOrderHandler : ICommandHandler<DeleteOrder>
{
    public Task Handle(DeleteOrder request, CancellationToken ct)
        => Task.CompletedTask;
}
```

## 🔧 Handlers

### Request / Command / Query Handlers

```csharp
// Generic — with response
public class MyHandler : IRequestHandler<MyRequest, MyResponse> { ... }
public class MyHandler : ICommandHandler<MyCommand, int> { ... }
public class MyHandler : IQueryHandler<MyQuery, MyDto> { ... }

// Non-generic — void (Task Handle(), no Unit!)
public class MyHandler : IRequestHandler<MyRequest> { ... }
public class MyHandler : ICommandHandler<MyCommand> { ... }
```

Non-generic handlers use `Task Handle(...)` independently (MediatR-style) — the library automatically bridges to `Task<Unit>` internally via `VoidRequestHandlerAdapter`.

### Notification Handlers

Multiple handlers per notification type — all execute sequentially:

```csharp
public class SendEmailHandler : INotificationHandler<OrderCreated>
{
    public Task Handle(OrderCreated notification, CancellationToken ct)
        => Task.CompletedTask;
}
```

```csharp
await mediator.Publish(new OrderCreated(orderId));
```

## 🔗 Pipeline Behaviors

Add cross-cutting concerns (logging, validation, transactions) as middleware:

```csharp
// Open generic — applies to all requests
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        Console.WriteLine($"Handling {typeof(TRequest).Name}");
        var result = await next(ct);
        Console.WriteLine($"Handled {typeof(TRequest).Name}");
        return result;
    }
}

// Closed — for specific void command (use shorthand IPipelineBehavior<TRequest>)
public class DeleteAuditBehavior : IPipelineBehavior<DeleteOrder>
{
    public Task<Unit> Handle(
        DeleteOrder request, RequestHandlerDelegate<Unit> next, CancellationToken ct)
    {
        Console.WriteLine($"Auditing delete: {request.Id}");
        return next(ct);
    }
}
```

Register behaviors — they execute in registration order (first registered = outermost):

```csharp
builder.Services.AddBehaviors(
    typeof(LoggingBehavior<,>),       // outermost
    typeof(ValidationBehavior<,>),    // innermost
    typeof(DeleteAuditBehavior)       // closed — auto-resolves to IPipelineBehavior<DeleteOrder, Unit>
);
```

**Pipeline execution order:**
```
LoggingBehavior:Before → ValidationBehavior:Before → Handler → ValidationBehavior:After → LoggingBehavior:After
```

## ⚙️ DI Registration

### Assembly Scanning

```csharp
builder.Services.AddMediatorFromAssemblies(
    Assembly.GetExecutingAssembly(),
    typeof(SomeHandler).Assembly
);
```

**What gets registered:**

- `Mediator` as `IMediator`, `ISender`, `IPublisher` — via `TryAddTransient`
- `IRequestHandler<,>` implementations — `TryAddTransient` (single handler per request)
- `IRequestHandler<>` (void) — registers handler + `VoidRequestHandlerAdapter` bridge
- `INotificationHandler<>` implementations — `AddTransient` with duplicate protection

### Behavior Registration

```csharp
builder.Services.AddBehaviors(
    typeof(LoggingBehavior<,>),          // open generic
    typeof(DeleteAuditBehavior)          // closed — auto-detects every IPipelineBehavior<,> interface it implements
);
```

Re-registering the exact same behavior type (same service interface + implementation pair) is a no-op, so calling `AddBehaviors` more than once for the same type — e.g. from two separate composition modules — won't double up the pipeline.

## 🛡️ Error Handling

### Notification Error Resilience

- Non-cancellation exceptions are **collected** and thrown as `AggregateException`
- `OperationCanceledException` / `TaskCanceledException` **propagate immediately**

### Request Error Handling

- Handler exceptions propagate through the pipeline normally
- Behaviors can catch and handle exceptions via standard try/catch around `next()`

## 🏗️ Project Structure

```
Mediator.Contracts/          ← Pure contracts, zero dependencies
├── ICommand.cs                     ICommand<T>, ICommand
├── INotification.cs                INotification
├── IQuery.cs                       IQuery<T>
├── IRequest.cs                     IRequest<T>, IRequest
└── Unit.cs                         Unit struct

Mediator/                    ← Core + DI, depends on Contracts
├── Wrappers/                       Internal handler/behavior wrappers
│   ├── BehaviorWrapper.cs
│   ├── HandlerWrapper.cs
│   ├── NotificationHandlerWrapper.cs
│   └── VoidRequestHandlerAdapter.cs  Task→Task<Unit> bridge
├── ICommandHandler.cs              ICommandHandler<T,R>, ICommandHandler<T>
├── IMediator.cs                    IMediator : ISender, IPublisher
├── INotificationHandler.cs         INotificationHandler<T>
├── IPipelineBehavior.cs            IPipelineBehavior<T,R>, IPipelineBehavior<T>, delegate
├── IPublisher.cs                   IPublisher
├── IQueryHandler.cs                IQueryHandler<T,R>
├── IRequestHandler.cs              IRequestHandler<T,R>, IRequestHandler<T>
├── ISender.cs                      ISender
├── Mediator.cs                     Core mediator implementation
└── ServiceCollectionExtensions.cs  DI registration extensions
```

### Why two packages?

- **Contracts** — Reference only this in shared/domain projects. Zero dependencies, minimal surface.
- **Mediator** — Reference this in your composition root / startup project. Brings in `Microsoft.Extensions.DependencyInjection`.

## 📄 License

MIT
