# TODO / Roadmap

Tracked findings and follow-ups from code review. Not yet implemented — check items off as they land.

## Bugs to fix

- [ ] **`AddBehaviors` silently drops a behavior's other closed interfaces** — [src/Mediator/ServiceCollectionExtensions.cs:90-95](src/Mediator/ServiceCollectionExtensions.cs#L90-L95)
  If a single class implements `IPipelineBehavior<,>` for two different closed requests (e.g. `IPipelineBehavior<CreateOrder,int>` and `IPipelineBehavior<DeleteOrder>` on the same type), `FirstOrDefault` registers only whichever interface `GetInterfaces()` happens to return first — the other is silently ignored, with no error. `GetInterfaces()` order isn't a documented contract.
  **Fix:** enumerate all matching closed `IPipelineBehavior<,>` interfaces with `.Where(...)` and register a `ServiceDescriptor` for each, instead of `FirstOrDefault` + single `Add`.

- [x] **`AddBehaviors` has no duplicate-registration protection** — [src/Mediator/ServiceCollectionExtensions.cs:81-109](src/Mediator/ServiceCollectionExtensions.cs#L81-L109)
  Fixed: both the open- and closed-generic branches now guard `services.Add(...)` with `!services.Any(d => d.ServiceType == X && d.ImplementationType == behaviorType)`, mirroring the existing notification-handler duplicate guard in `AddMediatorFromAssemblies`. Verified by `ServiceCollectionExtensionsTests` (real `ServiceCollection`, not `FakeServiceProvider`) covering: same open-generic type registered twice → runs once; same closed type registered twice → runs once; two distinct open-generic types → both still run.
  Fixing this also surfaced a related latent bug caught by the new real-container tests: `AddMediatorFromAssemblies`'s `concreteTypes` filter didn't exclude `IsGenericTypeDefinition` types, so an unbound generic class implementing a handler interface (e.g. a test helper) could be scanned and registered against a closed service type, which a real `ServiceProvider` rejects at resolution (`FakeServiceProvider` never validated this). Fixed alongside by adding `&& !t.IsGenericTypeDefinition` to that filter.

## Test coverage gaps

- [ ] No tests exercise `AddBehaviors` or `AddMediatorFromAssemblies` against a real `IServiceCollection`/`ServiceProvider` — `tests/Mediator.Tests` only tests dispatch logic via `FakeServiceProvider`, which bypasses these extension methods entirely. This is why the two bugs above weren't caught. Add a fixture that builds a real `ServiceCollection`, calls both extension methods, and resolves from the built provider — including a case with duplicate `AddBehaviors` calls and a case with a multi-interface closed behavior.

## Design nits (low priority, not worth fixing proactively)

- Wrapper caches in `Mediator.cs` (`_handlerWrappers`, `_behaviorWrappers`) are keyed only by request runtime `Type`, not `(Type, TResponse)`. Because `IRequest<out TResponse>` is covariant, dispatching the same concrete request type through two different `Send<TResponse>()` static-type instantiations (only possible by deliberately widening the static type) could hit a cached wrapper built for the wrong `TResponse` — it throws `InvalidCastException` rather than misbehaving silently, so this is safe but slightly surprising. Not worth adding overhead to guard against; documenting here in case it ever needs revisiting.

## Reviewed and confirmed correct (no action needed)

- `Mediator.cs` wrapper caching (`TryGetValue` before `GetOrAdd`) and delegate-allocation behavior.
- `BehaviorWrapper.ExecutePipeline`'s zero-behaviors fast path and array-vs-`ToArray()` check.
- `NotificationHandlerWrapper`'s `AggregateException`-collection / cancellation-propagation semantics.
- `VoidRequestHandlerAdapter`'s `IsCompletedSuccessfully` synchronous short-circuit.
