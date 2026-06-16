# WebNet.AspireToolkit.Erlang

## The Erlang language and framework hosting integration for Microsoft Aspire

### - Includes support for both language and runtime resouces as well as the AppHost in Erlang

## Current status

The package now provides a first-phase Erlang runtime resource primitive for Aspire AppHost projects. The initial surface is intentionally small: it models an Erlang runtime installation, exposes a typed `ErtsResource`, and adds an `AddErts(...)` builder extension for registering that resource with Aspire.

## Phase 1 usage

```csharp
using Aspire.Hosting;
using WebNet.AspireToolkit.Erlang;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddErts("erlang", @"C:\Program Files\Erlang OTP")
    .WithHttpEndpoint(port: 8080, targetPort: 8080);
```

To customize the Erlang node startup flags, use the options overload:

```csharp
builder.AddErts("erlang", @"C:\Program Files\Erlang OTP", options =>
{
    options.NodeName = "webnet";
    options.UseShortName = true;
    options.Cookie = "aspire-cookie";
    options.Arguments.Add("-noshell");
    options.EnvironmentVariables["ERL_FLAGS"] = "+S 2:2";
});
```

## Runtime package management

`ErtsResourceOptions` now carries platform-specific ERTS runtime package options for Windows, Linux, and macOS. By default, the resource registers dashboard commands that let you inspect the available package options and select a runtime package from the Aspire dashboard.

```csharp
builder.AddErts("erlang", @"C:\Program Files\Erlang OTP", options =>
{
    options.RuntimePackageOptions.Clear();
    options.RuntimePackageOptions.Add(new ErtsRuntimePackageOption(
        ErtsPlatform.Windows,
        "winget",
        "winget",
        "Erlang.ErlangOTP",
        "winget install Erlang.ErlangOTP"));
});
```

The `list-runtime-packages` dashboard command shows the registered options by platform. The `select-runtime-package` dashboard command accepts a target platform and option name, then returns the selected install command for the chosen ERTS package workflow.

## Phase 2 usage

The next surface adds a rebar3-backed Erlang application resource on top of an `ErtsResource`. This lets the library model compile/run concerns separately from runtime installation.

```csharp
using Aspire.Hosting;
using WebNet.AspireToolkit.Erlang;

var builder = DistributedApplication.CreateBuilder(args);

var runtime = builder.AddErts("erlang", @"C:\Program Files\Erlang OTP");

builder.AddErlangApp("sample-app", runtime.Resource, @"C:\src\sample-app", "sample_app", options =>
{
    options.Profile = "prod";
    options.Otel.Enabled = true;
    options.Otel.ServiceName = "sample-app";
    options.Otel.ExporterOtlpEndpoint = "http://localhost:4318";
    options.MonitoredProcesses.Add(new ErlangMonitoredProcess("sample_sup", "supervisor", "Top-level supervisor"));
});
```

`AddErlangApp(...)` runs the resource through `rebar3 shell --apps <app>` and registers dashboard commands for:

- compiling the rebar3 project,
- cleaning the project build output,
- describing OTEL configuration, and
- describing monitored Erlang process groups.

## TypeScript AppHost sample

This repository now includes a minimal rebar3 sample app at `Samples\HelloErlangRebar3` and wires the local integration package into the TypeScript AppHost through `aspire.config.json`.

The sample AppHost expects:

- `ERTS_HOME` (or `ERLANG_HOME`) to point at the Erlang installation root, such as `D:\Erlang OTP`
- either `REBAR3_PATH` on `PATH`, or `REBAR3_ESCRIPT` pointing at a local `rebar3` escript file

The sample wrapper at `Samples\HelloErlangRebar3\tools\rebar3.cmd` uses `REBAR3_ESCRIPT` with `%ERTS_HOME%\bin\escript.exe` when `rebar3` is not already installed on `PATH`.

With those variables set, the TypeScript AppHost can start the sample with:

```powershell
$env:ERTS_HOME = 'D:\Erlang OTP'
$env:REBAR3_ESCRIPT = 'C:\path\to\rebar3'
aspire start --isolated --non-interactive
```
