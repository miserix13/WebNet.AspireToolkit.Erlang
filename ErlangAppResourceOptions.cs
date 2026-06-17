using Aspire.Hosting;

namespace WebNet.AspireToolkit.Erlang
{
    [AspireDto]
    public sealed class ErlangAppResourceOptions
    {
        public ErlangAppResourceOptions()
        {
            Profile = "default";
            RunCommand = "shell";
            CompileArguments = new List<string>();
            RunArguments = new List<string>();
            HexDependencyArguments = new List<string>();
            EnvironmentVariables = new Dictionary<string, string>(StringComparer.Ordinal);
            MonitoredProcesses = new List<ErlangMonitoredProcess>();
            Otel = new ErlangOtelOptions();
            EnableBuildCommands = true;
            EnableHexCommands = true;
            EnableMonitoringCommands = true;
            EnableTelemetryCommands = true;
        }

        public string Rebar3ExecutablePath { get; set; }

        public string Profile { get; set; }

        public string RunCommand { get; set; }

        public bool EnableBuildCommands { get; set; }

        public bool EnableHexCommands { get; set; }

        public bool EnableMonitoringCommands { get; set; }

        public bool EnableTelemetryCommands { get; set; }

        public IList<string> CompileArguments { get; init; }

        public IList<string> RunArguments { get; init; }

        public IList<string> HexDependencyArguments { get; init; }

        public IDictionary<string, string> EnvironmentVariables { get; init; }

        public IList<ErlangMonitoredProcess> MonitoredProcesses { get; init; }

        public ErlangOtelOptions Otel { get; init; }
    }
}
