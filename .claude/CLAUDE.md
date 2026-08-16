# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Light.Mediator — a lightweight mediator library for .NET implementing the Mediator/CQRS pattern (MediatR-style), published to NuGet as `Lightsoft.Mediator` and `Lightsoft.Mediator.Contracts`.

## Commands

Build (from repo root, uses `Mediator.sln`):
```
dotnet build
```

Run all tests:
```
dotnet test
```

Run a single test (NUnit via `dotnet test --filter`):
```
dotnet test --filter "FullyQualifiedName~MediatorSendTests.MethodName"
```

Pack NuGet packages (mirrors the release workflow):
```
dotnet pack --no-build src/Mediator.Contracts/Mediator.Contracts.csproj --configuration Release --output nuget
dotnet pack --no-build src/Mediator/Mediator.csproj --configuration Release --output nuget
```

Run the sample API:
```
dotnet run --project samples/WebApi/WebApi.csproj
```

Publishing to NuGet.org is a manual `workflow_dispatch` GitHub Action (`.github/workflows/publish-mediator-to-nuget.yml`), not run locally — never invoke `dotnet nuget push` yourself.

## Project layout

- `src/Mediator.Contracts` — zero-dependency contracts (`IRequest<T>`, `ICommand`, `IQuery<T>`, `INotification`, `Unit`), targets `netstandard2.0`. Meant to be referenced alone by shared/domain projects that only need to declare requests/handlers' shapes.
- `src/Mediator` — core implementation + DI registration, targets `netstandard2.1`, depends on `Mediator.Contracts` and `Microsoft.Extensions.DependencyInjection.Abstractions`.
- `tests/Mediator.Tests` — NUnit tests (net10.0), uses a custom `Assert` helper class (`ShouldBe`, `ShouldThrowAsync`, etc. in `Assert.cs`) instead of calling `NUnit.Framework.Assert` directly — follow this convention in new tests. Dispatch/pipeline tests use a hand-rolled `FakeServiceProvider` rather than a mocking library; `ServiceCollectionExtensionsTests.cs` is the exception — it exercises `AddMediatorFromAssemblies`/`AddBehaviors` against a real `ServiceCollection`/`BuildServiceProvider()`, which is necessary since a real container enforces registration rules (e.g. rejecting an open-generic implementation type) that `FakeServiceProvider` doesn't validate.
- `samples/WebApi` — ASP.NET Core sample demonstrating vertical-slice-style feature folders (`IdFeatures/Add`, `Get`, `Update`, `Delete`, `Events`) with behaviors.

Both `src` projects have `GeneratePackageOnBuild=True`, so every build also produces a `.nupkg`.

## Architecture

The core `Mediator` class (`src/Mediator/Mediator.cs`) resolves handlers/behaviors through **wrapper classes** (`src/Mediator/Wrappers/`) because generic handler/behavior interfaces (`IRequestHandler<TRequest,TResponse>`, `IPipelineBehavior<TRequest,TResponse>`) can't be invoked directly without knowing `TRequest` at compile time. `Send<TResponse>` only knows `TResponse` and the request's runtime type, so:

1. `HandlerWrapper<TRequest,TResponse>` and `BehaviorWrapper<TRequest,TResponse>` are closed generics constructed via reflection (`Activator.CreateInstance` + `MakeGenericType`) the first time a given request type is seen, then cached forever in static `ConcurrentDictionary<Type, object>` keyed by request type — this is the "cached wrapper resolution" / zero-alloc fast path mentioned in the README.
2. `BehaviorWrapper.ExecutePipeline` builds the middleware chain by folding registered `IPipelineBehavior<TRequest,TResponse>` instances right-to-left into a `RequestHandlerDelegate<TResponse>`, with `finalHandler` (from `HandlerWrapper`) as the innermost call. First-registered behavior ends up outermost.
3. Non-generic (void) request handlers (`IRequestHandler<TRequest>`, `ICommandHandler<TRequest>`) are bridged to the generic pipeline via `VoidRequestHandlerAdapter<TRequest>`, which wraps `Task Handle()` into `Task<Unit> Handle()`. `ServiceCollectionExtensions.AddMediatorFromAssemblies` registers this adapter automatically whenever it finds an `IRequestHandler<>` implementation — so void handlers still flow through `IRequestHandler<TRequest, Unit>` internally, and pipeline behaviors for void requests are written against `IPipelineBehavior<TRequest>` (shorthand for `IPipelineBehavior<TRequest, Unit>`).
4. `NotificationHandlerWrapper<TNotification>` follows the same lazy-cache pattern for `Publish`, but fans out to *all* registered `INotificationHandler<TNotification>` sequentially, collecting non-cancellation exceptions into an `AggregateException` while letting `OperationCanceledException`/`TaskCanceledException` propagate immediately.

`ServiceCollectionExtensions.AddMediatorFromAssemblies` scans assemblies for concrete types implementing `IRequestHandler<,>`, `IRequestHandler<>`, or `INotificationHandler<>` and registers them with `TryAddTransient`/`AddTransient` (with duplicate protection for notification handlers, since a type can implement the same notification interface multiple times only via inheritance). `AddBehaviors` distinguishes open generic behavior types (registered against the open `IPipelineBehavior<,>` service type) from closed behavior types (registered against whichever closed `IPipelineBehavior<,>` interface they implement, auto-detected via reflection) — registration order determines outermost-to-innermost pipeline order.

`ICommand`/`ICommand<T>` and `IQuery<T>` in Contracts are marker interfaces over `IRequest`/`IRequest<T>` for CQRS naming; there is no behavioral difference in the mediator pipeline between a command, a query, and a plain request — dispatch is driven entirely by the request's shape (generic vs. non-generic `IRequest<T>`), not by which marker interface it uses.
