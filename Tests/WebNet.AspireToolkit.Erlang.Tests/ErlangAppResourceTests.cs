using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace WebNet.AspireToolkit.Erlang.Tests
{
    public class ErlangAppResourceTests
    {
        [Fact]
        public void ConstructorBuildsCompileRunAndTelemetryState()
        {
            var runtimeOptions = new ErtsResourceOptions
            {
                NodeName = "sample",
                Cookie = "cookie"
            };
            runtimeOptions.EnvironmentVariables["ERL_FLAGS"] = "+S 2:2";

            var appOptions = new ErlangAppResourceOptions
            {
                Profile = "prod"
            };
            appOptions.CompileArguments.Add("--verbose");
            appOptions.RunArguments.Add("--config");
            appOptions.RunArguments.Add("sys.config");
            appOptions.HexDependencyArguments.Add("--verbose");
            appOptions.Otel.Enabled = true;
            appOptions.Otel.ServiceName = "sample-app";
            appOptions.Otel.ExporterOtlpEndpoint = "http://localhost:4318";
            appOptions.Otel.Protocol = "http/protobuf";
            appOptions.Otel.ResourceAttributes["deployment.environment"] = "dev";
            appOptions.MonitoredProcesses.Add(new ErlangMonitoredProcess("sample_sup", "supervisor", "Top-level supervisor"));

            var runtime = new ErtsResource("runtime", @"C:\otp", runtimeOptions);
            var resource = new ErlangAppResource("sample-app", runtime, @"C:\src\sample-app", "sample_app", appOptions);

            Assert.Equal(OperatingSystem.IsWindows() ? "rebar3.cmd" : "rebar3", resource.Command);
            Assert.Equal(@"C:\src\sample-app", resource.WorkingDirectory);
            Assert.Equal(Path.Combine(@"C:\src\sample-app", "_build", "prod", "lib", "sample_app"), resource.BuildOutputDirectory);
            Assert.Equal(new[] { "as", "prod", "compile", "--verbose" }, resource.CompileArguments);
            Assert.Equal(new[] { "as", "prod", "shell", "--apps", "sample_app", "--config", "sys.config" }, resource.RunArguments);
            Assert.Equal(new[] { "as", "prod", "deps", "--verbose" }, resource.HexDependencyArguments);
            Assert.True(resource.EnableHexCommands);
            Assert.Contains(resource.EnvironmentVariables, pair => pair.Key == "ERL_FLAGS" && pair.Value == "+S 2:2");
            Assert.Contains(resource.EnvironmentVariables, pair => pair.Key == "ERL_AFLAGS" && pair.Value.Contains("-sname sample", StringComparison.Ordinal));
            Assert.Contains(resource.OtelEnvironmentVariables, pair => pair.Key == "OTEL_EXPORTER_OTLP_ENDPOINT" && pair.Value == "http://localhost:4318");
            Assert.Contains(resource.EnvironmentVariables, pair => pair.Key == "OTEL_SERVICE_NAME" && pair.Value == "sample-app");
            Assert.Single(resource.MonitoredProcesses);
            Assert.Contains("sample_sup", resource.DescribeMonitoring(), StringComparison.Ordinal);
            Assert.Contains("deps --verbose", resource.DescribeHex(), StringComparison.Ordinal);
        }

        [Fact]
        public void AddErlangAppRegistersExecutionAndDashboardCommands()
        {
            var builder = DistributedApplication.CreateBuilder();
            var runtime = new ErtsResource("runtime", @"C:\otp");

            var resourceBuilder = builder.AddErlangApp("sample-app", runtime, @"C:\src\sample-app", "sample_app", options =>
            {
                options.Otel.Enabled = true;
                options.MonitoredProcesses.Add(new ErlangMonitoredProcess("sample_sup", "supervisor"));
            });

            var resource = Assert.IsType<ErlangAppResource>(resourceBuilder.Resource);

            Assert.Contains(builder.Resources, candidate => ReferenceEquals(candidate, resource));
            Assert.Contains(resource.Annotations, annotation => annotation is CommandLineArgsCallbackAnnotation);
            Assert.Contains(resource.Annotations, annotation => annotation is EnvironmentCallbackAnnotation);
            Assert.Contains(resource.Annotations, annotation => annotation is ResourceCommandAnnotation resourceCommandAnnotation && resourceCommandAnnotation.Name == "compile-erlang-app");
            Assert.Contains(resource.Annotations, annotation => annotation is ResourceCommandAnnotation resourceCommandAnnotation && resourceCommandAnnotation.Name == "clean-erlang-app");
            Assert.Contains(resource.Annotations, annotation => annotation is ResourceCommandAnnotation resourceCommandAnnotation && resourceCommandAnnotation.Name == "sync-hex-dependencies");
            Assert.Contains(resource.Annotations, annotation => annotation is ResourceCommandAnnotation resourceCommandAnnotation && resourceCommandAnnotation.Name == "describe-hex");
            Assert.Contains(resource.Annotations, annotation => annotation is ResourceCommandAnnotation resourceCommandAnnotation && resourceCommandAnnotation.Name == "describe-otel");
            Assert.Contains(resource.Annotations, annotation => annotation is ResourceCommandAnnotation resourceCommandAnnotation && resourceCommandAnnotation.Name == "describe-process-monitoring");
        }

        [Fact]
        public void AddErlangAppSkipsHexCommandsWhenDisabled()
        {
            var builder = DistributedApplication.CreateBuilder();
            var runtime = new ErtsResource("runtime", @"C:\otp");

            var resourceBuilder = builder.AddErlangApp("sample-app", runtime, @"C:\src\sample-app", "sample_app", options =>
            {
                options.EnableHexCommands = false;
            });

            var resource = Assert.IsType<ErlangAppResource>(resourceBuilder.Resource);

            Assert.DoesNotContain(resource.Annotations, annotation => annotation is ResourceCommandAnnotation resourceCommandAnnotation && resourceCommandAnnotation.Name == "sync-hex-dependencies");
            Assert.DoesNotContain(resource.Annotations, annotation => annotation is ResourceCommandAnnotation resourceCommandAnnotation && resourceCommandAnnotation.Name == "describe-hex");
        }
    }
}
