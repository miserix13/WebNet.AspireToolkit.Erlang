# Copilot instructions for WebNet.AspireToolkit.Erlang

## Build, test, and lint commands

- Build the library: `dotnet build WebNet.AspireToolkit.Erlang.csproj`
- Restore packages only: `dotnet restore WebNet.AspireToolkit.Erlang.csproj`
- Build the solution including tests: `dotnet build WebNet.AspireToolkit.Erlang.slnx`
- Run the full test project: `dotnet test Tests\WebNet.AspireToolkit.Erlang.Tests\WebNet.AspireToolkit.Erlang.Tests.csproj`
- Run a single test: `dotnet test Tests\WebNet.AspireToolkit.Erlang.Tests\WebNet.AspireToolkit.Erlang.Tests.csproj --filter FullyQualifiedName~WebNet.AspireToolkit.Erlang.Tests.ErtsResourceTests.ConstructorBuildsCommandAndStartupArguments`
- There is currently no dedicated lint or format command checked into this repository.

## High-level architecture

- The repository now contains a .NET class library plus a dedicated xUnit test project; it is still not an Aspire AppHost or runnable sample application.
- The package targets `net10.0` and depends on `Aspire.Hosting` plus `MessagePack`, so new code should fit the shape of an Aspire integration library rather than a standalone executable.
- `README.md` describes the package intent as Erlang language/framework hosting integration for Microsoft Aspire.
- Phase 1 implementation is centered on `ErtsResource`, `ErtsResourceOptions`, and `ErtsResourceBuilderExtensions`, which model an Erlang runtime installation and register it through `AddErts(...)` on `IDistributedApplicationBuilder`.

## Key conventions

- Keep changes inside the library project unless the task explicitly asks for samples, tooling, or additional projects.
- Preserve the current project settings unless there is a deliberate reason to change them: nullable reference types are disabled, reference assemblies are not produced, and warning level is set to `0` for both Debug and Release.
- Follow the existing C# style in this repo: block-scoped namespaces and straightforward public types under the `WebNet.AspireToolkit.Erlang` namespace.
- Treat the README as the product contract: this library is meant to model Aspire/Erlang integration primitives, so prefer resource/integration types over app-specific orchestration code.
- The phase-one API uses immutable-at-registration resource data: configure Erlang runtime behavior through `ErtsResourceOptions` before registration, then let Aspire annotations (`WithArgs`, `WithEnvironment`) carry that configuration into execution.
