# Copilot instructions for WebNet.AspireToolkit.Erlang

## Build, test, and lint commands

### .NET commands
- Restore .NET packages: `dotnet restore WebNet.AspireToolkit.Erlang.csproj`
- Build the integration library: `dotnet build WebNet.AspireToolkit.Erlang.csproj`
- Build library + tests: `dotnet build WebNet.AspireToolkit.Erlang.slnx`
- Run the full .NET test suite: `dotnet test Tests\WebNet.AspireToolkit.Erlang.Tests\WebNet.AspireToolkit.Erlang.Tests.csproj`
- Run a single .NET test: `dotnet test Tests\WebNet.AspireToolkit.Erlang.Tests\WebNet.AspireToolkit.Erlang.Tests.csproj --filter FullyQualifiedName~WebNet.AspireToolkit.Erlang.Tests.ErlangAppResourceTests.AddErlangAppRegistersExecutionAndDashboardCommands`
- Refresh TypeScript bindings after C# API changes: `aspire restore` (run from `Samples` folder)

### TypeScript AppHost commands (run from `Samples` folder)
- Lint the TypeScript AppHost file: `npm run lint`
- Type-check/build the TypeScript AppHost file: `npm run build`
- Run AppHost in dev flow (pre-lints, then `aspire run`): `npm run dev`
- Run TypeScript compiler in watch mode: `npm run watch`

## High-level architecture

- This repository’s primary deliverable is a reusable Aspire integration package (`WebNet.AspireToolkit.Erlang.csproj`), not a standalone app. The root C# files define integration primitives; `Tests\WebNet.AspireToolkit.Erlang.Tests` asserts the resource contract via Aspire’s in-memory builder.
- There are two resource layers with a strict split:
  - `ErtsResource` models Erlang runtime installation/launch and optional runtime-package workflows.
  - `ErlangAppResource` models a rebar3-backed application that compiles and runs on top of an `ErtsResource`.
- Builder extensions are the registration surface:
  - `ErtsResourceBuilderExtensions` adds runtime resources and dashboard commands (`list-runtime-packages`, `select-runtime-package`).
  - `ErlangAppResourceBuilderExtensions` adds app resources and dashboard/process commands for compile/clean, Hex dependency sync/description, OTEL description, and monitored-process description.
- Resource construction is front-loaded and normalized before registration. Constructors compute command paths, arguments, environment, and derived metadata first; extension methods then project that state into Aspire annotations (`WithArgs`, `WithEnvironment`, `WithCommand`, `WithProcessCommand`).
- The repository also contains a TypeScript AppHost entrypoint (`apphost.mts`) configured through `aspire.config.json` to consume this local package and run `Samples\HelloErlangRebar3`. This is a usage scaffold, while the core contract remains the library APIs and tests.

### TypeScript AppHost SDK generation
- The `.aspire/modules` folder contains auto-generated TypeScript bindings created by Aspire during `aspire restore`.
- When C# Aspire APIs in `ErtsResourceBuilderExtensions` or `ErlangAppResourceBuilderExtensions` change, regenerate bindings with `aspire restore` from the `Samples` folder.
- The AppHost assumes environment variables `ERTS_HOME` (or `ERLANG_HOME`) and either `REBAR3_PATH` or a local `rebar3.cmd` wrapper (e.g., `Samples\HelloErlangRebar3\tools\rebar3.cmd`).

## Key conventions

- Keep integration source in the root project files; tests live under `Tests\...` and are intentionally excluded from the package compile items.
- Preserve project-level compile settings unless a change requires otherwise: nullable is disabled, reference assemblies are disabled, warning level is `0`.
- Keep the options-first API flow: callers mutate options in the `configure` callback, then resource constructors validate/normalize and throw on invalid required values.
- Preserve normalization and defaults:
  - required string inputs reject null/empty/whitespace,
  - optional strings normalize to `null`,
  - dictionaries use `StringComparer.Ordinal`,
  - default executables are OS-specific (`erl.exe`/`erl`, `rebar3.cmd`/`rebar3`),
  - `ErtsResourceOptions.UseShortName` defaults to `true`.
- Keep concerns separated: runtime installation/package-selection belongs on `ErtsResource`; rebar3 compile/run, OTEL, and monitored-process metadata belong on `ErlangAppResource`.
- Treat tests as the behavioral contract for registration: they validate both persisted resource state and Aspire annotations/commands attached by the builder extensions.
- Keep the TypeScript AppHost assumptions intact when touching sample orchestration: it expects `ERTS_HOME` or `ERLANG_HOME`, and `REBAR3_PATH` or the sample `tools\rebar3.cmd` wrapper.
