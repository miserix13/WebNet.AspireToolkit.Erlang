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
