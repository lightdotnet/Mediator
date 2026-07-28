# TODO / Roadmap

Tracked findings and follow-ups from code review. Check items off as they land; the AddBehaviors duplicate-registration and multi-interface bugs (plus their test coverage) have been fixed and folded into CLAUDE.md/README.md — see git history for detail.

## Design nits (low priority, not worth fixing proactively)

- Wrapper caches in `Mediator.cs` (`_handlerWrappers`, `_behaviorWrappers`) are keyed only by request runtime `Type`, not `(Type, TResponse)`. Because `IRequest<out TResponse>` is covariant, dispatching the same concrete request type through two different `Send<TResponse>()` static-type instantiations (only possible by deliberately widening the static type) could hit a cached wrapper built for the wrong `TResponse` — it throws `InvalidCastException` rather than misbehaving silently, so this is safe but slightly surprising. Not worth adding overhead to guard against; documenting here in case it ever needs revisiting.

## Reviewed and confirmed correct (no action needed)

- `Mediator.cs` wrapper caching (`TryGetValue` before `GetOrAdd`) and delegate-allocation behavior.
- `BehaviorWrapper.ExecutePipeline`'s zero-behaviors fast path and array-vs-`ToArray()` check.
- `NotificationHandlerWrapper`'s `AggregateException`-collection / cancellation-propagation semantics.
- `VoidRequestHandlerAdapter`'s `IsCompletedSuccessfully` synchronous short-circuit.
