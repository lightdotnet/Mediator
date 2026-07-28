---
name: bump-package-version
description: Bump the NuGet package version for Light.Mediator ahead of a release, keeping Mediator.csproj and Mediator.Contracts.csproj in sync. Use when the user asks to bump/release a new version, prepare a release, or update the package version number.
---

# Bump package version

Both packages are versioned together (currently `1.2.0.0` in both) even though they publish independently:

- `src/Mediator/Mediator.csproj` — `<Version>` element
- `src/Mediator.Contracts/Mediator.Contracts.csproj` — `<Version>` element

## Steps

1. Update `<Version>` in **both** csproj files to the same new value — do not let them drift, `Mediator` depends on `Mediator.Contracts` and mismatched versions confuse consumers pinning a specific version.
2. If there's meaningful release content, fill in `<PackageReleaseNotes>` in `Mediator.csproj` (currently left empty) — otherwise leave it blank rather than inventing notes.
3. Build to confirm both projects still compile and pack cleanly (each has `GeneratePackageOnBuild=True`, so a plain `dotnet build` already regenerates the `.nupkg`):
   ```
   dotnet build
   ```
4. Do **not** run `dotnet nuget push` or trigger the publish workflow yourself. Actual publishing to NuGet.org is a manual `workflow_dispatch` run of `.github/workflows/publish-mediator-to-nuget.yml`, gated behind a repo secret (`NUGET_API_KEY`) — that's the user's call to trigger from GitHub Actions, not something to automate here.
5. Stop after updating the version files and confirming the build; committing/tagging/pushing is a separate explicit step the user should confirm, not something to chain automatically after a version bump.
