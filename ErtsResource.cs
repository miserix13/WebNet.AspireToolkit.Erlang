using Aspire.Hosting.ApplicationModel;

namespace WebNet.AspireToolkit.Erlang
{
    public sealed class ErtsResource : ExecutableResource
    {
        private readonly string[] startupArguments;
        private readonly KeyValuePair<string, string>[] environmentVariables;

        public ErtsResource(string name, string ertsHome)
            : this(name, ertsHome, new ErtsResourceOptions())
        {
        }

        public ErtsResource(string name, string ertsHome, ErtsResourceOptions options)
            : base(
                  ValidateRequired(name, nameof(name)),
                  ResolveCommand(ertsHome, options),
                  ResolveWorkingDirectory(ertsHome, options))
        {
            ArgumentNullException.ThrowIfNull(options);

            ErtsHome = ValidateRequired(ertsHome, nameof(ertsHome));
            ExecutableName = string.IsNullOrWhiteSpace(options.ExecutableName) ? GetDefaultExecutableName() : options.ExecutableName;
            NodeName = NormalizeOptional(options.NodeName);
            UseShortName = options.UseShortName;
            Cookie = NormalizeOptional(options.Cookie);
            startupArguments = BuildStartupArguments(options, NodeName, UseShortName, Cookie);
            environmentVariables = BuildEnvironmentVariables(options);
        }

        public string ErtsHome { get; }

        public string ExecutableName { get; }

        public string NodeName { get; }

        public bool UseShortName { get; }

        public string Cookie { get; }

        public IReadOnlyList<string> StartupArguments => startupArguments;

        public IReadOnlyList<KeyValuePair<string, string>> EnvironmentVariables => environmentVariables;

        private static string ResolveCommand(string ertsHome, ErtsResourceOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var root = ValidateRequired(ertsHome, nameof(ertsHome));
            var executableName = string.IsNullOrWhiteSpace(options.ExecutableName) ? GetDefaultExecutableName() : options.ExecutableName;

            return Path.Combine(root, "bin", executableName);
        }

        private static string ResolveWorkingDirectory(string ertsHome, ErtsResourceOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return string.IsNullOrWhiteSpace(options.WorkingDirectory)
                ? ValidateRequired(ertsHome, nameof(ertsHome))
                : options.WorkingDirectory;
        }

        private static string[] BuildStartupArguments(ErtsResourceOptions options, string nodeName, bool useShortName, string cookie)
        {
            var args = new List<string>();

            if (!string.IsNullOrWhiteSpace(nodeName))
            {
                args.Add(useShortName ? "-sname" : "-name");
                args.Add(nodeName);
            }

            if (!string.IsNullOrWhiteSpace(cookie))
            {
                args.Add("-setcookie");
                args.Add(cookie);
            }

            foreach (var argument in options.Arguments)
            {
                if (string.IsNullOrWhiteSpace(argument))
                {
                    throw new ArgumentException("Arguments cannot contain null, empty, or whitespace values.", nameof(options));
                }

                args.Add(argument);
            }

            return args.ToArray();
        }

        private static KeyValuePair<string, string>[] BuildEnvironmentVariables(ErtsResourceOptions options)
        {
            var variables = new List<KeyValuePair<string, string>>();

            foreach (var environmentVariable in options.EnvironmentVariables)
            {
                if (string.IsNullOrWhiteSpace(environmentVariable.Key))
                {
                    throw new ArgumentException("Environment variable names cannot be null, empty, or whitespace.", nameof(options));
                }

                variables.Add(environmentVariable);
            }

            return variables.ToArray();
        }

        private static string GetDefaultExecutableName()
        {
            return OperatingSystem.IsWindows() ? "erl.exe" : "erl";
        }

        private static string NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
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
