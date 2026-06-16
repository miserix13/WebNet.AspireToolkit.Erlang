using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace WebNet.AspireToolkit.Erlang
{
    public static class ErlangAppResourceBuilderExtensions
    {
        public static IResourceBuilder<ErlangAppResource> AddErlangApp(
            this IDistributedApplicationBuilder builder,
            string name,
            ErtsResource runtimeResource,
            string projectDirectory,
            string applicationName)
        {
            return AddErlangApp(builder, name, runtimeResource, projectDirectory, applicationName, configure: null);
        }

        public static IResourceBuilder<ErlangAppResource> AddErlangApp(
            this IDistributedApplicationBuilder builder,
            string name,
            ErtsResource runtimeResource,
            string projectDirectory,
            string applicationName,
            Action<ErlangAppResourceOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(builder);

            var options = new ErlangAppResourceOptions();
            configure?.Invoke(options);

            return AddErlangApp(builder, name, runtimeResource, projectDirectory, applicationName, options);
        }

        /// <summary>
        /// Adds an Erlang application resource backed by a rebar3 project.
        /// </summary>
        /// <ats-summary>
        /// Adds a rebar3-backed Erlang application resource that can compile source, run the application, expose OTEL configuration, and describe monitored Erlang processes.
        /// </ats-summary>
        [AspireExport]
        public static IResourceBuilder<ErlangAppResource> AddErlangApp(
            this IDistributedApplicationBuilder builder,
            string name,
            ErtsResource runtimeResource,
            string projectDirectory,
            string applicationName,
            ErlangAppResourceOptions options)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(options);

            return AddErlangApp(builder, new ErlangAppResource(name, runtimeResource, projectDirectory, applicationName, options));
        }

        public static IResourceBuilder<ErlangAppResource> AddErlangApp(this IDistributedApplicationBuilder builder, ErlangAppResource resource)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(resource);

            var resourceBuilder = builder.AddResource(resource);

            if (resource.RunArguments.Count > 0)
            {
                resourceBuilder.WithArgs(resource.RunArguments.ToArray());
            }

            foreach (var environmentVariable in resource.EnvironmentVariables)
            {
                resourceBuilder.WithEnvironment(environmentVariable.Key, environmentVariable.Value);
            }

            RegisterBuildCommands(resourceBuilder, resource);
            RegisterTelemetryCommands(resourceBuilder, resource);
            RegisterMonitoringCommands(resourceBuilder, resource);

            return resourceBuilder;
        }

        private static void RegisterBuildCommands(IResourceBuilder<ErlangAppResource> resourceBuilder, ErlangAppResource resource)
        {
            if (!resource.EnableBuildCommands)
            {
                return;
            }

            resourceBuilder.WithProcessCommand(
                "compile-erlang-app",
                "Compile Erlang app",
                _ => CreateProcessSpec(resource, resource.CompileArguments),
                new ProcessCommandOptions
                {
                    MaxOutputLineCount = 200,
                    DisplayImmediately = true
                });

            resourceBuilder.WithProcessCommand(
                "clean-erlang-app",
                "Clean Erlang app",
                _ => CreateProcessSpec(resource, "as", resource.Profile, "clean"),
                new ProcessCommandOptions
                {
                    MaxOutputLineCount = 200,
                    DisplayImmediately = true
                });
        }

        private static void RegisterTelemetryCommands(IResourceBuilder<ErlangAppResource> resourceBuilder, ErlangAppResource resource)
        {
            if (!resource.EnableTelemetryCommands)
            {
                return;
            }

            resourceBuilder.WithCommand(
                "describe-otel",
                "Describe OTEL configuration",
                _ => Task.FromResult(CommandResults.Success(
                    "OpenTelemetry configuration for this Erlang application resource.",
                    resource.DescribeOtel(),
                    CommandResultFormat.Text,
                    true)),
                new CommandOptions
                {
                    Description = "Show the OpenTelemetry environment and export settings wired into the Erlang application resource.",
                    Visibility = ResourceCommandVisibility.UI | ResourceCommandVisibility.Api,
                    IconName = "Pulse"
                });
        }

        private static void RegisterMonitoringCommands(IResourceBuilder<ErlangAppResource> resourceBuilder, ErlangAppResource resource)
        {
            if (!resource.EnableMonitoringCommands)
            {
                return;
            }

            resourceBuilder.WithCommand(
                "describe-process-monitoring",
                "Describe process monitoring",
                _ => Task.FromResult(CommandResults.Success(
                    "Configured Erlang process monitoring surface.",
                    resource.DescribeMonitoring(),
                    CommandResultFormat.Text,
                    true)),
                new CommandOptions
                {
                    Description = "Show the monitored Erlang processes and supervision-oriented process groups configured for this resource.",
                    Visibility = ResourceCommandVisibility.UI | ResourceCommandVisibility.Api,
                    IconName = "Eye"
                });
        }

        private static ProcessCommandSpec CreateProcessSpec(ErlangAppResource resource, IReadOnlyList<string> arguments)
        {
            var processCommandSpec = new ProcessCommandSpec(resource.Rebar3ExecutablePath)
            {
                WorkingDirectory = resource.ProjectDirectory,
                KillEntireProcessTree = true,
                Arguments = arguments.ToArray(),
                EnvironmentVariables = resource.EnvironmentVariables.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value,
                    StringComparer.Ordinal)
            };

            return processCommandSpec;
        }

        private static ProcessCommandSpec CreateProcessSpec(ErlangAppResource resource, params string[] arguments)
        {
            return CreateProcessSpec(resource, (IReadOnlyList<string>)arguments);
        }
    }
}
