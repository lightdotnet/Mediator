# TODOS — full codebase review (2026-08-16)

Findings from a full-repo review (`dotnet-reviewer` agent, `dotnet-api-compat-reviewer` agent, and a manual coverage pass via the `check-test-coverage` skill), not tied to a specific diff. Nothing here has been implemented yet — this is a proposal list for future work. Baseline at review time: 42/42 tests passing, 90.9% line / 82.5% branch coverage on `Lightsoft.Mediator` (100% on `Lightsoft.Mediator.Contracts`).

Each item is tagged with its blast radius:
- **[safe]** — internal/private implementation detail, zero consumer impact, can be done anytime.
- **[public-additive]** — touches a public method but only adds validation/fail-fast behavior on paths that previously failed anyway; low semver risk (patch/minor).
- **[breaking]** — changes or removes public API surface; requires an explicit major-version decision, not a silent change.

## Correctness / robustness

1. **[safe, already accepted — see below] Wrapper caches keyed only by request type, not `(RequestType, ResponseType)`.**
   `src/Mediator/Mediator.cs:37-49` (`_handlerWrappers`, `_behaviorWrappers`). Because `IRequest<out TResponse>` is covariant, a type implementing `IRequest<T>` for more than one `T` could hit a cached wrapper built for the wrong `TResponse`, throwing `InvalidCastException`.
   **This is the same issue already documented in `TODO.md` under "Design nits (low priority, not worth fixing proactively)"** — it was previously reviewed and deliberately deprioritized (fails loudly via `InvalidCastException` rather than misbehaving silently; not worth the overhead of a composite-key cache to guard against a deliberately-contrived call pattern). Re-surfaced here for completeness; no new action implied unless revisited.

2. **[decision needed] `NotificationHandlerWrapper.Publish` silently drops already-buffered exceptions when a later handler throws `OperationCanceledException`/`TaskCanceledException`.**
   `src/Mediator/Wrappers/NotificationHandlerWrapper.cs:22-38`. If handler #1 throws a normal exception (caught and buffered for the eventual `AggregateException`) and handler #2 subsequently throws `OperationCanceledException` (which bypasses the buffering catch filter), the OCE propagates immediately and handler #1's failure is lost — no `AggregateException` is ever thrown, so the caller never learns handler #1 failed.
   Needs a decision, not just a fix: either (a) merge buffered exceptions into the thrown result when an OCE cuts the loop short (changes the thrown exception's shape — a consumer catching a bare `OperationCanceledException` today would instead need to unwrap something else), or (b) keep current behavior and explicitly document the trade-off in CLAUDE.md/README. Internal type (`NotificationHandlerWrapper<TNotification>` is `internal`), so no public API break either way — but it does change observable exception-throwing behavior, which is a contract change for consumers regardless of accessibility.

3. **[public-additive] `AddBehaviors`'s open-generic branch has no validation that the type implements `IPipelineBehavior<,>`, unlike the closed-type branch.**
   `src/Mediator/ServiceCollectionExtensions.cs:81-91` vs. the closed branch's explicit check at lines 100-102. A caller mistake (e.g. `AddBehaviors(typeof(SomeUnrelatedOpenGeneric<>))`) registers silently and only fails later with a confusing `MakeGenericType` exception during DI resolution instead of a clear message at registration time. Suggested fix: validate before registering and throw the same `ArgumentException` style used in the closed branch.

4. **[public-additive] `AddMediatorFromAssemblies` doesn't null-check individual array elements, unlike `AddBehaviors`.**
   `src/Mediator/ServiceCollectionExtensions.cs:19-30`. `AddBehaviors` checks each `behaviorType == null` (lines 77-79); `AddMediatorFromAssemblies` only checks the array itself, not each `Assembly`. A `null` element flows into `GetLoadableTypes(null)` → unfriendly `NullReferenceException` instead of a clear `ArgumentNullException`.

5. **[safe, test-only] `VoidRequestHandlerAdapter.Awaited`'s truly-async continuation path has 0% test coverage.**
   `src/Mediator/Wrappers/VoidRequestHandlerAdapter.cs:27-31`. All current tests exercise only the synchronous-completion short-circuit (`IsCompletedSuccessfully` branch, line 21-22); the genuinely-awaited path (handler's `Task` not yet complete when checked) is never hit. This is the counterpart to a documented hot-path optimization (CLAUDE.md: "`VoidRequestHandlerAdapter`'s `IsCompletedSuccessfully` synchronous short-circuit") and deserves a test that forces a truly-async handler (e.g. one that awaits `Task.Yield()` or a `TaskCompletionSource`).

## Performance / allocation

6. **[safe] The documented "zero-alloc fast path" isn't actually zero-alloc — `Send<TResponse>` always allocates a closure + delegate for `FinalHandler`, even with no behaviors registered.**
   `src/Mediator/Mediator.cs:51-58`. `FinalHandler` captures `request` and `handlerWrapper` (both call-specific), so the compiler can't cache the delegate — every `Send` allocates a closure object + delegate *before* `BehaviorWrapper.ExecutePipeline`'s `array.Length == 0` check (`src/Mediator/Wrappers/BehaviorWrapper.cs:33-34`) even runs. This contradicts the explicit "zero-alloc fast-path" claim in `README.md:15` and CLAUDE.md's framing.
   Fix: change `BehaviorWrapper<TRequest,TResponse>.ExecutePipeline` to accept `IHandlerWrapper<TResponse>` + `IServiceProvider` directly instead of a pre-built `RequestHandlerDelegate<TResponse>`, and only construct the delegate/closure chain when `array.Length > 0`. Both `IBehaviorWrapper<TResponse>`/`IHandlerWrapper<TResponse>` are `internal` — no public API impact.

7. **[safe, low priority] `AddBehaviors`'s duplicate-registration checks are O(n) `IEnumerable.Any` scans over the growing `IServiceCollection` per behavior/interface.**
   `src/Mediator/ServiceCollectionExtensions.cs:53-58, 83-84, 106-107`. Startup-time only, not the hot dispatch path — fine for typical handler counts, flagged only for very large assemblies. Not urgent.

## Naming / file organization

8. **[breaking — architectural, needs explicit decision] Handler/behavior interfaces (`IRequestHandler`, `ICommandHandler`, `IQueryHandler`, `INotificationHandler`, `IPipelineBehavior`) live in `src/Mediator`, not `src/Mediator.Contracts`.**
   These interfaces have zero dependencies of their own (only `System.Threading`/`System.Threading.Tasks`) — only the dispatch machinery (`Mediator.cs`, `Wrappers/*`, `ServiceCollectionExtensions.cs`) needs `Microsoft.Extensions.DependencyInjection.Abstractions`. As split today, a domain project that wants to *declare and implement* a handler (not just a request/notification shape) must pull in the full `Mediator` package, undercutting CLAUDE.md's "reference Contracts alone" pitch. Moving these into `Mediator.Contracts` would fix that, but moves public types across assemblies — breaking for any consumer referencing `Lightsoft.Mediator.IRequestHandler<,>` etc. today. Needs a deliberate major-version decision, not a quiet refactor.

9. **[breaking — flag only, not recommended] `class Mediator` shares its name with the `Light.Mediator` namespace, forcing consumers (including this repo's own tests) to alias it.**
   `src/Mediator/Mediator.cs:9`. Every test file needs `using MediatorClass = Light.Mediator.Mediator;` purely because a namespace containing "Mediator" collides with the unqualified class name — a real ergonomic cost for any consumer namespace containing "Mediator" as a segment. Renaming (e.g. `MediatorDispatcher`) is a breaking public change. MediatR has the identical pattern, so this may be an accepted trade-off — flagged for awareness, not actively recommended.

10. **[safe, optional] `Wrappers/VoidRequestHandlerAdapter.cs` is organizationally inconsistent with its `Wrappers/` siblings.**
    `HandlerWrapper`, `BehaviorWrapper`, `NotificationHandlerWrapper` are all reflection-cached, per-request-type dispatch strategies built by `Mediator.CreateWrapper<T>`. `VoidRequestHandlerAdapter<TRequest>` is a plain DI-constructed bridge instead (registered via `AddMediatorFromAssemblies`, resolved normally through the container). Consider a separate `Adapters/` folder, or a renamed grouping, for clarity. Internal type — no public API impact.

11. **[safe, cosmetic] Internal `Wrappers/*` classes abbreviate parameter names (`sp`, `ct`) inconsistently with the rest of the codebase's spelled-out convention.**
    `src/Mediator/Wrappers/HandlerWrapper.cs:10,16`, `BehaviorWrapper.cs:13-15,23-25`, `NotificationHandlerWrapper.cs:11,17` use `sp`/`ct`; every public interface (`ISender.cs:8`, `IPublisher.cs:8`, `IRequestHandler.cs:9,15`, `INotificationHandler.cs:9`, `IPipelineBehavior.cs:6,13`) spells `serviceProvider`/`cancellationToken` out in full, and even `VoidRequestHandlerAdapter.cs` (same folder) spells it out — inconsistent within `Wrappers/` itself. Purely cosmetic, internal-only.

12. **[safe, optional] `RequestHandlerDelegate<TResponse>` delegate is defined inline inside `IPipelineBehavior.cs` rather than its own file.**
    `src/Mediator/IPipelineBehavior.cs:6`. Every other public type gets its own file (`ISender.cs`, `IPublisher.cs`, `IMediator.cs`, ...); this delegate is the one exception. Splitting it into `RequestHandlerDelegate.cs` doesn't change its namespace or public identity — safe, zero consumer impact, purely a file-organization nit.

## Public API surface reference (from `dotnet-api-compat-reviewer` audit)

For quick reference when deciding what's safe to touch:

- **Public / breaking-if-renamed:** everything in `src/Mediator.Contracts` (`IRequest<>`, `ICommand<>`, `IQuery<>`, `INotification`, `Unit`), plus in `src/Mediator`: `IMediator`, `ISender`, `IPublisher`, `IRequestHandler<,>`/`IRequestHandler<>`, `ICommandHandler<,>`/`ICommandHandler<>`, `IQueryHandler<,>`, `INotificationHandler<>`, `IPipelineBehavior<,>`/`IPipelineBehavior<>`, `RequestHandlerDelegate<>`, the concrete `Mediator` class (registered directly under its own type via `TryAddTransient<Mediator>()` — not just exposed through interfaces), and both methods on `ServiceCollectionExtensions`.
- **Internal / safe to rename freely:** everything under `src/Mediator/Wrappers/` (`BehaviorWrapper<,>`, `HandlerWrapper<,>`, `VoidRequestHandlerAdapter<>`, `NotificationHandlerWrapper<>`, and their internal wrapper interfaces), plus all private members of `Mediator.cs` and `ServiceCollectionExtensions.cs`.
- No accidentally-public implementation details were found — everything that should be `internal` already is.
