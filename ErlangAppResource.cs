using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using System.Text;

namespace WebNet.AspireToolkit.Erlang
{
    [AspireExport(ExposeProperties = true)]
    public sealed class ErlangAppResource : ExecutableResource
    {
        private readonly string[] compileArguments;
        private readonly string[] runArguments;
        private readonly KeyValuePair<string, string>[] environmentVariables;
        private readonly ErlangMonitoredProcess[] monitoredProcesses;
        private readonly KeyValuePair<string, string>[] otelEnvironmentVariables;

        public ErlangAppResource(string name, ErtsResource runtimeResource, string projectDirectory, string applicationName)
            : this(name, runtimeResource, projectDirectory, applicationName, new ErlangAppResourceOptions())
        {
        }

        public ErlangAppResource(string name, ErtsResource runtimeResource, string projectDirectory, string applicationName, ErlangAppResourceOptions options)
            : base(
                  ValidateRequired(name, nameof(name)),
                  ResolveRebar3ExecutablePath(options),
                  ValidateRequired(projectDirectory, nameof(projectDirectory)))
        {
            ArgumentNullException.ThrowIfNull(runtimeResource);
            ArgumentNullException.ThrowIfNull(options);

            RuntimeResource = runtimeResource;
            ProjectDirectory = ValidateRequired(projectDirectory, nameof(projectDirectory));
            ApplicationName = ValidateRequired(applicationName, nameof(applicationName));
            Rebar3ExecutablePath = ResolveRebar3ExecutablePath(options);
            Profile = ValidateRequired(options.Profile, nameof(options.Profile));
            RunCommand = ValidateRequired(options.RunCommand, nameof(options.RunCommand));
            BuildOutputDirectory = Path.Combine(ProjectDirectory, "_build", Profile, "lib", ApplicationName);
            compileArguments = BuildCompileArguments(options, Profile);
            runArguments = BuildRunArguments(options, Profile, RunCommand, ApplicationName);
            monitoredProcesses = BuildMonitoredProcesses(options);
            otelEnvironmentVariables = BuildOtelEnvironmentVariables(options, ApplicationName);
            environmentVariables = BuildEnvironmentVariables(runtimeResource, options, otelEnvironmentVariables);
            EnableBuildCommands = options.EnableBuildCommands;
            EnableMonitoringCommands = options.EnableMonitoringCommands;
            EnableTelemetryCommands = options.EnableTelemetryCommands;
        }

        public ErtsResource RuntimeResource { get; }

        public string ProjectDirectory { get; }

        public string ApplicationName { get; }

        public string Rebar3ExecutablePath { get; }

        public string Profile { get; }

        public string RunCommand { get; }

        public string BuildOutputDirectory { get; }

        public bool EnableBuildCommands { get; }

        public bool EnableMonitoringCommands { get; }

        public bool EnableTelemetryCommands { get; }

        public IReadOnlyList<string> CompileArguments => compileArguments;

        public IReadOnlyList<string> RunArguments => runArguments;

        public IReadOnlyList<KeyValuePair<string, string>> EnvironmentVariables => environmentVariables;

        public IReadOnlyList<KeyValuePair<string, string>> OtelEnvironmentVariables => otelEnvironmentVariables;

        public IReadOnlyList<ErlangMonitoredProcess> MonitoredProcesses => monitoredProcesses;

        public string DescribeMonitoring()
        {
            if (monitoredProcesses.Length == 0)
            {
                return "No monitored Erlang processes are configured.";
            }

            var builder = new StringBuilder();

            foreach (var monitoredProcess in monitoredProcesses)
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append("- ");
                builder.Append(monitoredProcess.Name);
                builder.Append(" (");
                builder.Append(monitoredProcess.Kind);
                builder.Append(')');

                if (!string.IsNullOrWhiteSpace(monitoredProcess.Description))
                {
                    builder.Append(": ");
                    builder.Append(monitoredProcess.Description);
                }
            }

            return builder.ToString();
        }

        public string DescribeOtel()
        {
            if (otelEnvironmentVariables.Length == 0)
            {
                return "OpenTelemetry is not enabled for this Erlang application resource.";
            }

            var builder = new StringBuilder();

            foreach (var environmentVariable in otelEnvironmentVariables)
            {
                builder.Append(environmentVariable.Key);
                builder.Append('=');
                builder.AppendLine(environmentVariable.Value);
            }

            return builder.ToString();
        }

        private static string ResolveRebar3ExecutablePath(ErlangAppResourceOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (!string.IsNullOrWhiteSpace(options.Rebar3ExecutablePath))
            {
                return options.Rebar3ExecutablePath;
            }

            return OperatingSystem.IsWindows() ? "rebar3.cmd" : "rebar3";
        }

        private static string[] BuildCompileArguments(ErlangAppResourceOptions options, string profile)
        {
            var arguments = new List<string>
            {
                "as",
                profile,
                "compile"
            };

            AddValidatedArguments(arguments, options.CompileArguments, nameof(options.CompileArguments));
            return arguments.ToArray();
        }

        private static string[] BuildRunArguments(ErlangAppResourceOptions options, string profile, string runCommand, string applicationName)
        {
            var arguments = new List<string>
            {
                "as",
                profile,
                runCommand,
                "--apps",
                applicationName
            };

            AddValidatedArguments(arguments, options.RunArguments, nameof(options.RunArguments));
            return arguments.ToArray();
        }

        private static KeyValuePair<string, string>[] BuildEnvironmentVariables(
            ErtsResource runtimeResource,
            ErlangAppResourceOptions options,
            IReadOnlyList<KeyValuePair<string, string>> otelEnvironmentVariables)
        {
            var variables = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var environmentVariable in runtimeResource.EnvironmentVariables)
            {
                variables[environmentVariable.Key] = environmentVariable.Value;
            }

            foreach (var environmentVariable in options.EnvironmentVariables)
            {
                if (string.IsNullOrWhiteSpace(environmentVariable.Key))
                {
                    throw new ArgumentException("Environment variable names cannot be null, empty, or whitespace.", nameof(options));
                }

                variables[environmentVariable.Key] = environmentVariable.Value;
            }

            foreach (var environmentVariable in otelEnvironmentVariables)
            {
                variables[environmentVariable.Key] = environmentVariable.Value;
            }

            var erlAFlags = BuildErlAFlags(runtimeResource.StartupArguments);
            if (!string.IsNullOrWhiteSpace(erlAFlags))
            {
                if (variables.TryGetValue("ERL_AFLAGS", out var existingFlags) && !string.IsNullOrWhiteSpace(existingFlags))
                {
                    variables["ERL_AFLAGS"] = $"{existingFlags} {erlAFlags}";
                }
                else
                {
                    variables["ERL_AFLAGS"] = erlAFlags;
                }
            }

            return variables.ToArray();
        }

        private static ErlangMonitoredProcess[] BuildMonitoredProcesses(ErlangAppResourceOptions options)
        {
            var monitoredProcesses = new List<ErlangMonitoredProcess>();
            var knownNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var monitoredProcess in options.MonitoredProcesses)
            {
                ArgumentNullException.ThrowIfNull(monitoredProcess);

                if (!knownNames.Add(monitoredProcess.Name))
                {
                    throw new ArgumentException(
                        $"Duplicate monitored process '{monitoredProcess.Name}'.",
                        nameof(options));
                }

                monitoredProcesses.Add(monitoredProcess);
            }

            return monitoredProcesses.ToArray();
        }

        private static KeyValuePair<string, string>[] BuildOtelEnvironmentVariables(ErlangAppResourceOptions options, string applicationName)
        {
            var variables = new List<KeyValuePair<string, string>>();
            var otelOptions = options.Otel ?? new ErlangOtelOptions();

            if (!otelOptions.Enabled)
            {
                return variables.ToArray();
            }

            var serviceName = string.IsNullOrWhiteSpace(otelOptions.ServiceName) ? applicationName : otelOptions.ServiceName;
            variables.Add(new KeyValuePair<string, string>("OTEL_SERVICE_NAME", serviceName));

            if (!string.IsNullOrWhiteSpace(otelOptions.ServiceVersion))
            {
                variables.Add(new KeyValuePair<string, string>("OTEL_SERVICE_VERSION", otelOptions.ServiceVersion));
            }

            if (!string.IsNullOrWhiteSpace(otelOptions.ExporterOtlpEndpoint))
            {
                variables.Add(new KeyValuePair<string, string>("OTEL_EXPORTER_OTLP_ENDPOINT", otelOptions.ExporterOtlpEndpoint));
            }

            if (!string.IsNullOrWhiteSpace(otelOptions.Protocol))
            {
                variables.Add(new KeyValuePair<string, string>("OTEL_EXPORTER_OTLP_PROTOCOL", otelOptions.Protocol));
            }

            if (otelOptions.ResourceAttributes.Count > 0)
            {
                var resourceAttributes = string.Join(
                    ",",
                    otelOptions.ResourceAttributes.Select(pair => $"{ValidateRequired(pair.Key, nameof(options))}={pair.Value}"));
                variables.Add(new KeyValuePair<string, string>("OTEL_RESOURCE_ATTRIBUTES", resourceAttributes));
            }

            return variables.ToArray();
        }

        private static string BuildErlAFlags(IReadOnlyList<string> startupArguments)
        {
            if (startupArguments.Count == 0)
            {
                return null;
            }

            return string.Join(" ", startupArguments.Select(EscapeShellArgument));
        }

        private static string EscapeShellArgument(string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                return argument;
            }

            return argument.IndexOfAny(new[] { ' ', '\t', '"' }) >= 0
                ? $"\"{argument.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
                : argument;
        }

        private static void AddValidatedArguments(List<string> target, IEnumerable<string> arguments, string paramName)
        {
            foreach (var argument in arguments)
            {
                if (string.IsNullOrWhiteSpace(argument))
                {
                    throw new ArgumentException("Arguments cannot contain null, empty, or whitespace values.", paramName);
                }

                target.Add(argument);
            }
        }

        private static string ValidateRequired(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);
            }

            return value;
        }
    }
}
