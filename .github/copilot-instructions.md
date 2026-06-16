# Copilot instructions for WebNet.AspireToolkit.Erlang

## Build, test, and lint commands

- Build the library: `dotnet build WebNet.AspireToolkit.Erlang.csproj`
- Restore packages only: `dotnet restore WebNet.AspireToolkit.Erlang.csproj`
- Build the solution including tests: `dotnet build WebNet.AspireToolkit.Erlang.slnx`
- Run the full test project: `dotnet test Tests\WebNet.AspireToolkit.Erlang.Tests\WebNet.AspireToolkit.Erlang.Tests.csproj`
- Run a single test: `dotnet test Tests\WebNet.AspireToolkit.Erlang.Tests\WebNet.AspireToolkit.Erlang.Tests.csproj --filter FullyQualifiedName~WebNet.AspireToolkit.Erlang.Tests.ErtsResourceTests.ConstructorBuildsCommandAndStartupArguments`
- There is currently no dedicated lint or format command checked into this repository.

## High-level architecture

- This repository is an Aspire integration library, not an AppHost or runnable sample. The root project ships the reusable Erlang resource types, and `Tests\WebNet.AspireToolkit.Erlang.Tests` exercises them with xUnit and Aspire's in-memory `DistributedApplication` builder.
- The package targets `net10.0` and references `Aspire.Hosting` plus `MessagePack`, so additions should extend the integration surface area rather than introduce app-specific orchestration code.
- The runtime layer is still `ErtsResource`, but the library now has a second layer for workloads: `ErlangAppResource` models a rebar3-backed Erlang application that compiles and runs on top of an `ErtsResource`.
- `ErtsResourceBuilderExtensions` owns runtime registration and dashboard package-management commands, while `ErlangAppResourceBuilderExtensions` owns rebar3 compile/clean commands, OTEL description, and monitoring description commands.
- Registration is intentionally front-loaded. Both `AddErts(...)` and `AddErlangApp(...)` build validated resource state first, then mirror computed arguments and environment variables into Aspire annotations with `WithArgs(...)`, `WithEnvironment(...)`, `WithCommand(...)`, and `WithProcessCommand(...)`.
- Runtime launch data is computed once from the resource/options pair: `ErtsResource` resolves to `<ertsHome>\bin\<erl or erl.exe>`, while `ErlangAppResource` resolves to `rebar3`/`rebar3.cmd` and computes compile/run arguments, build output path, OTEL environment, and monitored-process metadata up front.
- The tests are the best guide to the intended contract: they assert stored resource state plus the Aspire command/environment/argument annotations added during registration.

## Key conventions

- Keep library code in the root project unless a task explicitly requires test updates; `WebNet.AspireToolkit.Erlang.csproj` explicitly removes `Tests\**` from its compile items.
- Follow the existing C# style: block-scoped namespaces and straightforward public types under the `WebNet.AspireToolkit.Erlang` namespace.
- Preserve the current project settings unless there is a deliberate reason to change them: nullable reference types are disabled, reference assemblies are not produced, and warning level is set to `0` for both Debug and Release.
- Treat `README.md` as the product contract. This package is meant to model Aspire/Erlang integration primitives, so prefer resource types, builder extensions, and Aspire annotations over standalone executables or sample-specific behavior.
- Keep the phase-one configuration flow intact: callers mutate `ErtsResourceOptions` in the `configure` callback, then `ErtsResource` validates and normalizes that data before registration.
- Match the current validation and defaulting behavior when extending the API: blank required values throw, blank optional strings normalize to `null`, `UseShortName` defaults to `true`, environment variables use `StringComparer.Ordinal`, and the default executable name remains OS-specific unless explicitly overridden.
- Keep the runtime/application split intact: ERTS installation, runtime flags, and package-management choices belong on `ErtsResource`; source compilation, rebar3 execution, OTEL wiring, and monitored Erlang-process metadata belong on `ErlangAppResource`.
- For app execution, the first supported workflow is a **rebar3 project**. New compile/run behavior should extend that shape rather than introducing unrelated project models into the same options type.
