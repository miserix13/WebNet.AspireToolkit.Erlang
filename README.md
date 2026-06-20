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
    options.HexDependencyArguments.Add("--verbose");
    options.Otel.Enabled = true;
    options.Otel.ServiceName = "sample-app";
    options.Otel.ExporterOtlpEndpoint = "http://localhost:4318";
    options.MonitoredProcesses.Add(new ErlangMonitoredProcess("sample_sup", "supervisor", "Top-level supervisor"));
});
```

`AddErlangApp(...)` runs the resource through `rebar3 shell --apps <app>` and registers dashboard commands for:

- compiling the rebar3 project,
- cleaning the project build output,
- synchronizing Hex-backed dependencies (`rebar3 as <profile> deps`),
- describing the configured Hex dependency command,
- describing OTEL configuration, and
- describing monitored Erlang process groups.

## TypeScript AppHost samples

This repository includes multiple rebar3 sample applications in `Samples\` to demonstrate different OTP patterns. All samples are wired into the TypeScript AppHost through `aspire.config.json`.

### TypeScript AppHost usage

The main AppHost entrypoint lives at `Samples\apphost.mts`. It loads the local package declared in `Samples\aspire.config.json`, creates an Erlang runtime resource, and then attaches the selected rebar3 application resource:

```ts
import { join, resolve } from 'node:path';
import { createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();
const ertsHome = process.env.ERTS_HOME ?? process.env.ERLANG_HOME;
const sampleAppPath = resolve('.\\Samples\\HelloErlangRebar3');
const rebar3Path = process.env.REBAR3_PATH ?? join(sampleAppPath, 'tools', 'rebar3.cmd');

const erlangRuntime = await builder.addErts('erlang-runtime', ertsHome, {
    enableRuntimePackageCommands: true
});

await builder.addErlangApp('hello_rebar3-app', erlangRuntime, sampleAppPath, 'hello_erlang', {
    rebar3ExecutablePath: rebar3Path,
    enableHexCommands: true
});
```

Before running the AppHost from `Samples\`, restore the generated Aspire TypeScript SDK and install the existing npm dependencies:

```powershell
cd Samples
npm ci
aspire restore
```

`aspire restore` generates the local `Samples\.aspire\modules\aspire.mjs` bindings consumed by `apphost.mts`. Without that step, `npm run build` cannot resolve `createBuilder`.

### Available samples

The AppHost supports sample selection via the `ASPIRE_SAMPLE` environment variable:

#### 1. `hello_rebar3` (default)
**Path**: `Samples\HelloErlangRebar3`

A basic Erlang application demonstrating:
- Standard OTP application structure
- Supervisor tree (`hello_erlang_sup`)
- Gen_server behavior (`hello_erlang_server`)
- Monitored process groups tracked by Aspire

**When to use**: Learning the basics of OTP, understanding application startup/shutdown.

**Run with**:
```powershell
$env:ASPIRE_SAMPLE = 'hello_rebar3'
aspire start --isolated --non-interactive
```

#### 2. `hello_pool`
**Path**: `Samples\HelloErlangPool`

A gen_server worker pool pattern demonstrating:
- Pool manager state machine (`hello_pool_manager`)
- Dynamic worker spawning and lifecycle management
- Task distribution across available workers
- Pool exhaustion handling

**When to use**: Building systems with request/task distribution, worker patterns, resource pooling.

**OTP patterns**: Supervisor trees, gen_server state management, worker lifecycle.

**Run with**:
```powershell
$env:ASPIRE_SAMPLE = 'hello_pool'
aspire start --isolated --non-interactive
```

#### 3. `hello_statem`
**Path**: `Samples\HelloErlangStatem`

A gen_statem finite state machine pattern demonstrating:
- State machine definition and transitions
- State-driven event handling (idle → processing → complete/error)
- Registry tracking active state machines
- Event-based state control

**When to use**: Protocol implementation, workflow engines, business state machines, game loops.

**OTP patterns**: Gen_statem callback module, state enter/exit handlers, conditional transitions.

**Run with**:
```powershell
$env:ASPIRE_SAMPLE = 'hello_statem'
aspire start --isolated --non-interactive
```

### Running samples

All samples expect:

- `ERTS_HOME` (or `ERLANG_HOME`) to point at the Erlang installation root, such as `D:\Erlang OTP`
- either `REBAR3_PATH` pointing at a `rebar3` executable, or the sample-local `tools\rebar3.cmd` wrapper with `REBAR3_ESCRIPT` pointing at a local `rebar3` escript file

The sample wrappers at `Samples\*\tools\rebar3.cmd` use `REBAR3_ESCRIPT` with `%ERTS_HOME%\bin\escript.exe` when `rebar3` is not already installed on `PATH`.

**Setup and run a sample**:

```powershell
cd Samples
$env:ASPIRE_SAMPLE = 'hello_pool'  # or 'hello_statem', 'hello_rebar3'
$env:ERTS_HOME = 'D:\Erlang OTP'
$env:REBAR3_ESCRIPT = 'C:\path\to\rebar3'
aspire restore
npm run build
aspire start --isolated --non-interactive
```

The TypeScript AppHost will:
1. Start the Erlang runtime resource
2. Load and compile the selected sample application
3. Start the application in `rebar3 shell`
4. Register monitored processes with the Aspire dashboard
5. Enable OTEL tracing for the application
