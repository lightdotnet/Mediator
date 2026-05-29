# Light.Mediator

A lightweight, high-performance mediator library for .NET — implementing the Mediator pattern with CQRS support, pipeline behaviors, and notification publishing.

[![NuGet](https://img.shields.io/nuget/v/Light.Mediator.svg)](https://www.nuget.org/packages/Lightsoft.Mediator)
[![.NET Standard](https://img.shields.io/badge/netstandard-2.1-blue.svg)]()

## ✨ Features

- **CQRS ready** — Separate `ICommand<T>`, `IQuery<T>`, and `IRequest<T>` contracts
- **Void commands** — `ICommand` / `IRequest` with built-in `Unit` type (no workarounds needed)
- **Pipeline behaviors** — Middleware-style `IPipelineBehavior<TRequest, TResponse>` for cross-cutting concerns
- **Notification publishing** — Fan-out to multiple `INotificationHandler<T>` with error resilience
- **Zero dependencies** on contracts — `Light.Mediator.Contracts` has no external dependencies
- **High performance** — Cached wrapper resolution via `ConcurrentDictionary`, fast-path for zero-behavior pipelines
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

// Request with response
public record GetOrderById(int Id) : IQuery<OrderDto>;

public class GetOrderByIdHandler : IQueryHandler<GetOrderById, OrderDto>
{
    public Task<OrderDto> Handle(GetOrderById request, CancellationToken cancellationToken)
        => Task.FromResult(new OrderDto(request.Id, "Sample Order"));
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

`Unit` is a readonly struct for void operations — no need for `Task<bool>` or `Task<object>` workarounds:

```csharp
public record DeleteOrder(int Id) : ICommand;

public class DeleteOrderHandler : ICommandHandler<DeleteOrder>
{
    public Task<Unit> Handle(DeleteOrder request, CancellationToken ct)
    {
        // ... delete logic
        return Unit.Task; // cached, zero-allocation
    }
}
```

## 🔧 Handlers

### Request / Command / Query Handlers

```csharp
// Generic — with response
public class MyHandler : IRequestHandler<MyRequest, MyResponse> { ... }
public class MyHandler : ICommandHandler<MyCommand, int> { ... }
public class MyHandler : IQueryHandler<MyQuery, MyDto> { ... }

// Non-generic — void (returns Unit)
public class MyHandler : IRequestHandler<MyRequest> { ... }
public class MyHandler : ICommandHandler<MyCommand> { ... }
```

All handler interfaces inherit from `IRequestHandler<TRequest, TResponse>`, so DI scanning only needs to look for one base type.

### Notification Handlers

Multiple handlers per notification type — all execute sequentially:

```csharp
public class SendEmailHandler : INotificationHandler<OrderCreated>
{
    public Task Handle(OrderCreated notification, CancellationToken ct)
    {
        // send email
        return Task.CompletedTask;
    }
}

public class UpdateCacheHandler : INotificationHandler<OrderCreated>
{
    public Task Handle(OrderCreated notification, CancellationToken ct)
    {
        // update cache
        return Task.CompletedTask;
    }
}
```

```csharp
await mediator.Publish(new OrderCreated(orderId));
// Both handlers execute
```

## 🔗 Pipeline Behaviors

Add cross-cutting concerns (logging, validation, transactions) as middleware:

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Handling {typeof(TRequest).Name}");
        var result = await next(cancellationToken);
        Console.WriteLine($"Handled {typeof(TRequest).Name}");
        return result;
    }
}
```

Register behaviors — they execute in registration order (first registered = outermost):

```csharp
builder.Services.AddBehaviors(
    typeof(LoggingBehavior<,>),       // runs first (outermost)
    typeof(ValidationBehavior<,>)     // runs second (innermost)
);
```

**Pipeline execution order:**

```
LoggingBehavior:Before
  → ValidationBehavior:Before
    → Handler
  → ValidationBehavior:After
→ LoggingBehavior:After
```

Behaviors can **short-circuit** the pipeline by not calling `next`:

```csharp
public class CacheBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (TryGetFromCache(request, out var cached))
            return Task.FromResult(cached); // handler never called

        return next(ct);
    }
}
```

## ⚙️ DI Registration

### Assembly Scanning

```csharp
// Scan one or more assemblies for handlers
builder.Services.AddMediatorFromAssemblies(
    Assembly.GetExecutingAssembly(),
    typeof(SomeHandler).Assembly
);
```

**What gets registered:**

- `Mediator` as `IMediator`, `ISender`, `IPublisher` — via `TryAddTransient` (idempotent)
- `IRequestHandler<,>` implementations — `TryAddTransient` (single handler per request)
- `INotificationHandler<>` implementations — `AddTransient` with duplicate protection (multiple handlers per notification)

**Safe scanning:** Assemblies with unloadable types are handled gracefully — no crash.

### Behavior Registration

```csharp
// Open generic behaviors (applied to all requests)
builder.Services.AddBehaviors(typeof(LoggingBehavior<,>));

// Closed generic behaviors (applied to specific requests)
builder.Services.AddBehaviors(typeof(OrderValidationBehavior));
```

## 🛡️ Error Handling

### Notification Error Resilience

When multiple notification handlers are registered, **all handlers execute** even if some throw:

```
Handler A throws → exception captured
Handler B still executes ✅
Handler C still executes ✅
→ AggregateException thrown with Handler A's exception
```

- Non-cancellation exceptions are **collected** and thrown as `AggregateException`
- `OperationCanceledException` / `TaskCanceledException` **propagate immediately** (cancellation takes priority)

### Request Error Handling

- Handler exceptions propagate through the pipeline normally
- Behaviors can catch and handle exceptions via standard try/catch around `next()`

## 🏗️ Project Structure

```
Light.Mediator.Contracts/          ← Pure contracts, zero dependencies
├── ICommand.cs                     ICommand<T>, ICommand
├── INotification.cs                INotification
├── IQuery.cs                       IQuery<T>
├── IRequest.cs                     IRequest<T>, IRequest
└── Unit.cs                         Unit struct

Light.Mediator/                    ← Core + DI, depends on Contracts
├── Wrappers/                       Internal handler/behavior wrappers
│   ├── BehaviorWrapper.cs
│   ├── HandlerWrapper.cs
│   └── NotificationHandlerWrapper.cs
├── ICommandHandler.cs              ICommandHandler<T,R>, ICommandHandler<T>
├── IMediator.cs                    IMediator : ISender, IPublisher
├── INotificationHandler.cs         INotificationHandler<T>
├── IPipelineBehavior.cs            IPipelineBehavior<T,R>, RequestHandlerDelegate<R>
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
