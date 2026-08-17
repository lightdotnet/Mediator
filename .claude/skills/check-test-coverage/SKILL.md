---
name: check-test-coverage
description: Run the NUnit suite with coverlet code coverage collection and report which lines/branches in src/Mediator or src/Mediator.Contracts are untested. Use when the user asks for coverage numbers, "what's untested", or before adding tests for a specific class to see what's already covered.
---

# Check test coverage

`tests/Mediator.Tests.csproj` already references `coverlet.collector` — no extra install needed, just invoke it through `dotnet test`.

## 1. Collect coverage

```
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

This drops a `coverage.cobertura.xml` under a GUID-named subfolder of `TestResults/` per run. Coverage is computed only for assemblies exercised by the test run (`Light.Mediator`, `Light.Mediator.Contracts`) — `samples/WebApi` isn't referenced by the test project and won't appear.

## 2. Read the report

Find the newest file:
```
find TestResults -name coverage.cobertura.xml | sort | tail -1
```
Parse the Cobertura XML directly (it's small — line-rate/branch-rate per `<class filename=...>`), or if the user wants a human-readable summary and has `reportgenerator` available (`dotnet tool run reportgenerator`), use that instead of hand-parsing. Don't install new global tools without asking first — this repo doesn't currently depend on one.

## 3. Report gaps meaningfully, not just percentages

A raw "87% line coverage" number is low-value on its own. Instead:
- Name the specific uncovered lines/branches by file:line (e.g. "`BehaviorWrapper.ExecutePipeline`'s exception path when a behavior throws before calling `next` is untested").
- Cross-reference against the architecture in CLAUDE.md — the wrapper-caching fast path, the `AggregateException` fan-out logic in `NotificationHandlerWrapper`, and `VoidRequestHandlerAdapter`'s synchronous short-circuit are the highest-value paths to have covered given this library's hot-path/correctness sensitivity; flag gaps there ahead of gaps in rarely-hit branches.
- Don't chase 100%. A defensive `throw new ArgumentNullException` on a parameter the public API already makes non-null via non-nullable reference types is a reasonable line to leave uncovered — say so rather than treating every red line as an action item.

## 4. Clean up

`TestResults/` is generated output — check whether it's already gitignored before leaving it around; don't commit coverage result files.
