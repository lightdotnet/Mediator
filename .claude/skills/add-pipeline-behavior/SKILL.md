---
name: add-pipeline-behavior
description: Scaffold a new IPipelineBehavior (open generic, applying to all requests, or closed, applying to one specific request) for Light.Mediator, and wire its registration order correctly via AddBehaviors. Use when the user asks to add logging, validation, transaction, or any other cross-cutting middleware behavior in this repo.
---

# Add a pipeline behavior

Behaviors implement `IPipelineBehavior<TRequest, TResponse>` (generic form for a value-returning request) or `IPipelineBehavior<TRequest>` (shorthand for `IPipelineBehavior<TRequest, Unit>`, for a closed void command/request).

## 1. Open generic — applies to every request

Use this for cross-cutting concerns like logging, validation, or exception translation that should wrap *all* dispatches:

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // work before the handler
        var result = await next(cancellationToken);
        // work after the handler
        return result;
    }
}
```

## 2. Closed — applies to one specific request type

Use this for a behavior that only makes sense for one command/query (e.g. auditing a specific delete):

```csharp
public class DeleteAuditBehavior : IPipelineBehavior<DeleteOrder>   // DeleteOrder : ICommand (void)
{
    public Task<Unit> Handle(
        DeleteOrder request, RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken)
    {
        // audit before/after
        return next(cancellationToken);
    }
}
```

For a closed behavior on a request with a return value, implement `IPipelineBehavior<TRequest, TResponse>` directly with concrete (non-generic) type arguments instead of the `<TRequest>` shorthand.

## 3. To short-circuit the handler

Don't call `next(cancellationToken)` — return a value (or throw) directly. `BehaviorWrapper.ExecutePipeline` has no special-case for this; it's just normal control flow.

## 4. Register — order is significant

```csharp
builder.Services.AddBehaviors(
    typeof(LoggingBehavior<,>),       // outermost — registered first
    typeof(ValidationBehavior<,>),    // innermost of the open generics
    typeof(DeleteAuditBehavior)       // closed — AddBehaviors auto-detects which IPipelineBehavior<,> it implements via reflection
);
```

`AddBehaviors` (in `src/Mediator/ServiceCollectionExtensions.cs`) registers open generic types against the open `IPipelineBehavior<,>` service type, and closed types against whichever closed `IPipelineBehavior<,>` interface they implement. Execution order is first-registered = outermost, folded right-to-left at dispatch time in `BehaviorWrapper.ExecutePipeline` — get the call order in `AddBehaviors` right, there's no way to override it later per-request.

## 5. Tests (this repo only)

Existing behavior tests (`tests/Mediator.Tests/PipelineBehaviorTests.cs`, `Behaviors.cs`) build a log list and assert on `"Name:Before"`/`"Name:After"` ordering via a small `TrackingBehavior<TRequest,TResponse>` helper, registered directly on `FakeServiceProvider` (no `AddBehaviors`/real DI needed in unit tests — that extension method is only exercised for the app-composition-root path). Follow that pattern for new ordering/short-circuit/exception-propagation tests rather than spinning up a real `IServiceCollection`.
