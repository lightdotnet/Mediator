# TODOS — full codebase review (2026-08-16)

Findings from a full-repo review (`dotnet-reviewer` agent, `dotnet-api-compat-reviewer` agent, and a manual coverage pass via the `check-test-coverage` skill), not tied to a specific diff. Baseline at review time: 42/42 tests passing, 90.9% line / 82.5% branch coverage on `Lightsoft.Mediator` (100% on `Lightsoft.Mediator.Contracts`).

**Status:** the original items #2-#6 (notification-cancellation exception loss, `AddBehaviors` open-generic validation, `AddMediatorFromAssemblies` null-checking, `VoidRequestHandlerAdapter` async-path test coverage, zero-alloc `Send` fast path) have all been implemented, reviewed (`dotnet-reviewer` PASS, `dotnet-api-compat-reviewer` non-breaking), and committed (`cefc243`, `23e3c83`) — removed from this list, items renumbered below. **Still pending from that work: bump `<Version>` in `Mediator.csproj`/`Mediator.Contracts.csproj` and add a `PackageReleaseNotes` entry before the next release**, since `dotnet-api-compat-reviewer` flagged the `AddBehaviors`/`AddMediatorFromAssemblies` validation tightening as an observable behavior change worth calling out even though it's non-breaking. Everything below is still a proposal only, nothing implemented.

Each item is tagged with its blast radius:
- **[safe]** — internal/private implementation detail, zero consumer impact, can be done anytime.
- **[public-additive]** — touches a public method but only adds validation/fail-fast behavior on paths that previously failed anyway; low semver risk (patch/minor).
- **[breaking]** — changes or removes public API surface; requires an explicit major-version decision, not a silent change.

## Correctness / robustness

1. **[safe, already accepted — see below] Wrapper caches keyed only by request type, not `(RequestType, ResponseType)`.**
   `src/Mediator/Mediator.cs:37-49` (`_handlerWrappers`, `_behaviorWrappers`). Because `IRequest<out TResponse>` is covariant, a type implementing `IRequest<T>` for more than one `T` could hit a cached wrapper built for the wrong `TResponse`, throwing `InvalidCastException`.
   **This is the same issue already listed below under "Design nits (low priority, not worth fixing proactively)"** — it was previously reviewed and deliberately deprioritized (fails loudly via `InvalidCastException` rather than misbehaving silently; not worth the overhead of a composite-key cache to guard against a deliberately-contrived call pattern). Re-surfaced here for completeness; no new action implied unless revisited.

## Performance / allocation

2. **[safe, low priority] `AddBehaviors`'s duplicate-registration checks are O(n) `IEnumerable.Any` scans over the growing `IServiceCollection` per behavior/interface.**
   `src/Mediator/ServiceCollectionExtensions.cs:53-58, 83-84, 106-107`. Startup-time only, not the hot dispatch path — fine for typical handler counts, flagged only for very large assemblies. Not urgent.

## Naming / file organization

3. **[breaking — architectural, needs explicit decision] Handler/behavior interfaces (`IRequestHandler`, `ICommandHandler`, `IQueryHandler`, `INotificationHandler`, `IPipelineBehavior`) live in `src/Mediator`, not `src/Mediator.Contracts`.**
   These interfaces have zero dependencies of their own (only `System.Threading`/`System.Threading.Tasks`) — only the dispatch machinery (`Mediator.cs`, `Wrappers/*`, `ServiceCollectionExtensions.cs`) needs `Microsoft.Extensions.DependencyInjection.Abstractions`. As split today, a domain project that wants to *declare and implement* a handler (not just a request/notification shape) must pull in the full `Mediator` package, undercutting CLAUDE.md's "reference Contracts alone" pitch. Moving these into `Mediator.Contracts` would fix that, but moves public types across assemblies — breaking for any consumer referencing `Lightsoft.Mediator.IRequestHandler<,>` etc. today. Needs a deliberate major-version decision, not a quiet refactor.

4. **[breaking — flag only, not recommended] `class Mediator` shares its name with the `Light.Mediator` namespace, forcing consumers (including this repo's own tests) to alias it.**
   `src/Mediator/Mediator.cs:9`. Every test file needs `using MediatorClass = Light.Mediator.Mediator;` purely because a namespace containing "Mediator" collides with the unqualified class name — a real ergonomic cost for any consumer namespace containing "Mediator" as a segment. Renaming (e.g. `MediatorDispatcher`) is a breaking public change. MediatR has the identical pattern, so this may be an accepted trade-off — flagged for awareness, not actively recommended.

5. **[safe, optional] `Wrappers/VoidRequestHandlerAdapter.cs` is organizationally inconsistent with its `Wrappers/` siblings.**
   `HandlerWrapper`, `BehaviorWrapper`, `NotificationHandlerWrapper` are all reflection-cached, per-request-type dispatch strategies built by `Mediator.CreateWrapper<T>`. `VoidRequestHandlerAdapter<TRequest>` is a plain DI-constructed bridge instead (registered via `AddMediatorFromAssemblies`, resolved normally through the container). Consider a separate `Adapters/` folder, or a renamed grouping, for clarity. Internal type — no public API impact.

6. **[safe, cosmetic] Internal `Wrappers/*` classes abbreviate parameter names (`sp`, `ct`) inconsistently with the rest of the codebase's spelled-out convention.**
   `src/Mediator/Wrappers/HandlerWrapper.cs:10,16`, `BehaviorWrapper.cs:13-15,23-25`, `NotificationHandlerWrapper.cs:11,17` use `sp`/`ct`; every public interface (`ISender.cs:8`, `IPublisher.cs:8`, `IRequestHandler.cs:9,15`, `INotificationHandler.cs:9`, `IPipelineBehavior.cs:6,13`) spells `serviceProvider`/`cancellationToken` out in full, and even `VoidRequestHandlerAdapter.cs` (same folder) spells it out — inconsistent within `Wrappers/` itself. Purely cosmetic, internal-only.

7. **[safe, optional] `RequestHandlerDelegate<TResponse>` delegate is defined inline inside `IPipelineBehavior.cs` rather than its own file.**
   `src/Mediator/IPipelineBehavior.cs:6`. Every other public type gets its own file (`ISender.cs`, `IPublisher.cs`, `IMediator.cs`, ...); this delegate is the one exception. Splitting it into `RequestHandlerDelegate.cs` doesn't change its namespace or public identity — safe, zero consumer impact, purely a file-organization nit.

## Public API surface reference (from `dotnet-api-compat-reviewer` audit)

For quick reference when deciding what's safe to touch:

- **Public / breaking-if-renamed:** everything in `src/Mediator.Contracts` (`IRequest<>`, `ICommand<>`, `IQuery<>`, `INotification`, `Unit`), plus in `src/Mediator`: `IMediator`, `ISender`, `IPublisher`, `IRequestHandler<,>`/`IRequestHandler<>`, `ICommandHandler<,>`/`ICommandHandler<>`, `IQueryHandler<,>`, `INotificationHandler<>`, `IPipelineBehavior<,>`/`IPipelineBehavior<>`, `RequestHandlerDelegate<>`, the concrete `Mediator` class (registered directly under its own type via `TryAddTransient<Mediator>()` — not just exposed through interfaces), and both methods on `ServiceCollectionExtensions`.
- **Internal / safe to rename freely:** everything under `src/Mediator/Wrappers/` (`BehaviorWrapper<,>`, `HandlerWrapper<,>`, `VoidRequestHandlerAdapter<>`, `NotificationHandlerWrapper<>`, and their internal wrapper interfaces), plus all private members of `Mediator.cs` and `ServiceCollectionExtensions.cs`.
- No accidentally-public implementation details were found — everything that should be `internal` already is.

## Design nits (low priority, not worth fixing proactively)

*(merged from the former root-level `TODO.md`)*

- Same item as #1 above: wrapper caches in `Mediator.cs` (`_handlerWrappers`, `_behaviorWrappers`) are keyed only by request runtime `Type`, not `(Type, TResponse)`. Because `IRequest<out TResponse>` is covariant, dispatching the same concrete request type through two different `Send<TResponse>()` static-type instantiations (only possible by deliberately widening the static type) could hit a cached wrapper built for the wrong `TResponse` — it throws `InvalidCastException` rather than misbehaving silently, so this is safe but slightly surprising. Not worth adding overhead to guard against; documenting here in case it ever needs revisiting.

## Reviewed and confirmed correct (no action needed)

- `Mediator.cs` wrapper caching (`TryGetValue` before `GetOrAdd`) and delegate-allocation behavior.
- `BehaviorWrapper.ExecutePipeline`'s zero-behaviors fast path and array-vs-`ToArray()` check.
- `NotificationHandlerWrapper`'s `AggregateException`-collection / cancellation-propagation semantics.
- `VoidRequestHandlerAdapter`'s `IsCompletedSuccessfully` synchronous short-circuit.
