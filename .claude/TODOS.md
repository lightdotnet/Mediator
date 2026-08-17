# TODOS — full codebase review (2026-08-16)

Findings from a full-repo review (`dotnet-reviewer` agent, `dotnet-api-compat-reviewer` agent, and a manual coverage pass via the `check-test-coverage` skill), not tied to a specific diff. Baseline at review time: 42/42 tests passing, 90.9% line / 82.5% branch coverage on `Lightsoft.Mediator` (100% on `Lightsoft.Mediator.Contracts`).

**Status:** all items originally numbered #2, #3, #4, #5, #6 (notification-cancellation exception loss, `AddBehaviors` open-generic validation, `AddMediatorFromAssemblies` null-checking, `VoidRequestHandlerAdapter` async-path test coverage, zero-alloc `Send` fast path) and, after renumbering, #2/#5/#6/#7 (O(n) duplicate-registration scans → `HashSet`-backed, `VoidRequestHandlerAdapter` moved to `Adapters/`, `sp`/`ct` spelled out in `Wrappers/*`, `RequestHandlerDelegate` split into its own file) have been implemented, reviewed (`dotnet-reviewer` PASS, `dotnet-api-compat-reviewer` non-breaking where applicable), and committed (`cefc243`, `23e3c83`, plus the latest reorg/perf/naming batch) — all removed from this list. The two breaking-architectural items (originally #8/#9, renumbered #2/#3) have been **decided against** (2026-08-17) — kept as-is, see below for rationale. Only #1 remains open (deliberately deprioritized, no action needed). **Release housekeeping done (2026-08-17):** `<Version>` bumped to `1.3.0` in both `Mediator.csproj`/`Mediator.Contracts.csproj`, and `PackageReleaseNotes` filled in on `Mediator.csproj` covering the `AddBehaviors`/`AddMediatorFromAssemblies` validation tightening, the `NotificationHandlerWrapper` cancellation fix, and the zero-alloc `Send` fast path — build/pack verified clean, 46/46 tests passing.

Each item is tagged with its blast radius:
- **[safe]** — internal/private implementation detail, zero consumer impact, can be done anytime.
- **[public-additive]** — touches a public method but only adds validation/fail-fast behavior on paths that previously failed anyway; low semver risk (patch/minor).
- **[breaking]** — changes or removes public API surface; requires an explicit major-version decision, not a silent change.

## Correctness / robustness

1. **[safe, already accepted — see below] Wrapper caches keyed only by request type, not `(RequestType, ResponseType)`.**
   `src/Mediator/Mediator.cs:37-49` (`_handlerWrappers`, `_behaviorWrappers`). Because `IRequest<out TResponse>` is covariant, a type implementing `IRequest<T>` for more than one `T` could hit a cached wrapper built for the wrong `TResponse`, throwing `InvalidCastException`.
   **This is the same issue already listed below under "Design nits (low priority, not worth fixing proactively)"** — it was previously reviewed and deliberately deprioritized (fails loudly via `InvalidCastException` rather than misbehaving silently; not worth the overhead of a composite-key cache to guard against a deliberately-contrived call pattern). Re-surfaced here for completeness; no new action implied unless revisited.

## Naming / file organization — decided against (no action)

2. ❌ **DECIDED AGAINST** — **[breaking — architectural] Handler/behavior interfaces (`IRequestHandler`, `ICommandHandler`, `IQueryHandler`, `INotificationHandler`, `IPipelineBehavior`) live in `src/Mediator`, not `src/Mediator.Contracts`.**
   These interfaces have zero dependencies of their own (only `System.Threading`/`System.Threading.Tasks`) — only the dispatch machinery (`Mediator.cs`, `Wrappers/*`, `ServiceCollectionExtensions.cs`) needs `Microsoft.Extensions.DependencyInjection.Abstractions`. As split today, a domain project that wants to *declare and implement* a handler (not just a request/notification shape) must pull in the full `Mediator` package.
   **Decision (2026-08-17): keep as-is.** Moving handler/behavior interfaces into `Mediator.Contracts` would let domain-layer projects implement handlers directly, which breaks the intended domain/application-layer separation this repo relies on — handler *implementation* is deliberately an application-layer concern, gated behind the `Mediator` package (and its DI dependency) on purpose. Domain projects are meant to only *declare* request/notification shapes via `Mediator.Contracts`; needing the full `Mediator` package to implement a handler is the intended friction, not an oversight. Also still breaking (moves public types across assemblies) regardless.

3. ❌ **DECIDED AGAINST** — **[breaking — flag only] `class Mediator` shares its name with the `Light.Mediator` namespace, forcing consumers (including this repo's own tests) to alias it.**
   `src/Mediator/Mediator.cs:9`. Every test file needs `using MediatorClass = Light.Mediator.Mediator;` purely because a namespace containing "Mediator" collides with the unqualified class name — a real ergonomic cost for any consumer namespace containing "Mediator" as a segment.
   **Decision (2026-08-17): keep the name.** MediatR has the identical pattern and it's an accepted trade-off in the .NET mediator-library ecosystem; renaming (e.g. `MediatorDispatcher`) is a breaking public change for a purely cosmetic ergonomics gain. Not revisiting unless a stronger reason emerges.

## Public API surface reference (from `dotnet-api-compat-reviewer` audit)

For quick reference when deciding what's safe to touch:

- **Public / breaking-if-renamed:** everything in `src/Mediator.Contracts` (`IRequest<>`, `ICommand<>`, `IQuery<>`, `INotification`, `Unit`), plus in `src/Mediator`: `IMediator`, `ISender`, `IPublisher`, `IRequestHandler<,>`/`IRequestHandler<>`, `ICommandHandler<,>`/`ICommandHandler<>`, `IQueryHandler<,>`, `INotificationHandler<>`, `IPipelineBehavior<,>`/`IPipelineBehavior<>`, `RequestHandlerDelegate<>`, the concrete `Mediator` class (registered directly under its own type via `TryAddTransient<Mediator>()` — not just exposed through interfaces), and both methods on `ServiceCollectionExtensions`.
- **Internal / safe to rename freely:** everything under `src/Mediator/Wrappers/` (`BehaviorWrapper<,>`, `HandlerWrapper<,>`, `NotificationHandlerWrapper<>`, and their internal wrapper interfaces) and `src/Mediator/Adapters/` (`VoidRequestHandlerAdapter<>`), plus all private members of `Mediator.cs` and `ServiceCollectionExtensions.cs`.
- No accidentally-public implementation details were found — everything that should be `internal` already is.

## Design nits (low priority, not worth fixing proactively)

*(merged from the former root-level `TODO.md`)*

- Same item as #1 above: wrapper caches in `Mediator.cs` (`_handlerWrappers`, `_behaviorWrappers`) are keyed only by request runtime `Type`, not `(Type, TResponse)`. Because `IRequest<out TResponse>` is covariant, dispatching the same concrete request type through two different `Send<TResponse>()` static-type instantiations (only possible by deliberately widening the static type) could hit a cached wrapper built for the wrong `TResponse` — it throws `InvalidCastException` rather than misbehaving silently, so this is safe but slightly surprising. Not worth adding overhead to guard against; documenting here in case it ever needs revisiting.

## Reviewed and confirmed correct (no action needed)

- `Mediator.cs` wrapper caching (`TryGetValue` before `GetOrAdd`) and delegate-allocation behavior.
- `BehaviorWrapper.ExecutePipeline`'s zero-behaviors fast path and array-vs-`ToArray()` check.
- `NotificationHandlerWrapper`'s `AggregateException`-collection / cancellation-propagation semantics.
- `VoidRequestHandlerAdapter`'s `IsCompletedSuccessfully` synchronous short-circuit.
