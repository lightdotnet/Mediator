# TODO / Roadmap

Tracked findings and follow-ups from code review. Not yet implemented — check items off as they land.

## Bugs to fix

- [x] **`AddBehaviors` silently drops a behavior's other closed interfaces** — [src/Mediator/ServiceCollectionExtensions.cs:94-114](src/Mediator/ServiceCollectionExtensions.cs#L94-L114)
  Fixed: replaced `FirstOrDefault` with `.Where(...).ToArray()`, throwing only if none match, and looping over every closed `IPipelineBehavior<,>` interface the type implements (each still going through the per-interface duplicate-registration guard). Verified by `AddBehaviors_ClosedTypeImplementingMultipleInterfaces_RegistersAll` — a `MultiInterfaceBehavior` implementing `IPipelineBehavior<PingRequest,PongResponse>` and `IPipelineBehavior<DeleteOrder>` against a real `ServiceCollection`, dispatching both request types and asserting both behavior methods ran.

- [x] **`AddBehaviors` has no duplicate-registration protection** — [src/Mediator/ServiceCollectionExtensions.cs:81-109](src/Mediator/ServiceCollectionExtensions.cs#L81-L109)
  Fixed: both the open- and closed-generic branches now guard `services.Add(...)` with `!services.Any(d => d.ServiceType == X && d.ImplementationType == behaviorType)`, mirroring the existing notification-handler duplicate guard in `AddMediatorFromAssemblies`. Verified by `ServiceCollectionExtensionsTests` (real `ServiceCollection`, not `FakeServiceProvider`) covering: same open-generic type registered twice → runs once; same closed type registered twice → runs once; two distinct open-generic types → both still run.
  Fixing this also surfaced a related latent bug caught by the new real-container tests: `AddMediatorFromAssemblies`'s `concreteTypes` filter didn't exclude `IsGenericTypeDefinition` types, so an unbound generic class implementing a handler interface (e.g. a test helper) could be scanned and registered against a closed service type, which a real `ServiceProvider` rejects at resolution (`FakeServiceProvider` never validated this). Fixed alongside by adding `&& !t.IsGenericTypeDefinition` to that filter.

## Test coverage gaps

- [x] No tests exercised `AddBehaviors` or `AddMediatorFromAssemblies` against a real `IServiceCollection`/`ServiceProvider` — this is why the bugs above weren't caught. `tests/Mediator.Tests/ServiceCollectionExtensionsTests.cs` now covers both extension methods against a real `ServiceCollection`/`BuildServiceProvider()`, resolving `IMediator` and dispatching through `Send` (duplicate `AddBehaviors` calls for both open- and closed-generic, and a closed behavior implementing multiple `IPipelineBehavior<,>` interfaces).

## Design nits (low priority, not worth fixing proactively)

- Wrapper caches in `Mediator.cs` (`_handlerWrappers`, `_behaviorWrappers`) are keyed only by request runtime `Type`, not `(Type, TResponse)`. Because `IRequest<out TResponse>` is covariant, dispatching the same concrete request type through two different `Send<TResponse>()` static-type instantiations (only possible by deliberately widening the static type) could hit a cached wrapper built for the wrong `TResponse` — it throws `InvalidCastException` rather than misbehaving silently, so this is safe but slightly surprising. Not worth adding overhead to guard against; documenting here in case it ever needs revisiting.

## Reviewed and confirmed correct (no action needed)

- `Mediator.cs` wrapper caching (`TryGetValue` before `GetOrAdd`) and delegate-allocation behavior.
- `BehaviorWrapper.ExecutePipeline`'s zero-behaviors fast path and array-vs-`ToArray()` check.
- `NotificationHandlerWrapper`'s `AggregateException`-collection / cancellation-propagation semantics.
- `VoidRequestHandlerAdapter`'s `IsCompletedSuccessfully` synchronous short-circuit.
