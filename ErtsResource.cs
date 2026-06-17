using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace WebNet.AspireToolkit.Erlang
{
    [AspireExport(ExposeProperties = true)]
    public sealed class ErtsResource : ExecutableResource
    {
        private readonly string[] startupArguments;
        private readonly KeyValuePair<string, string>[] environmentVariables;
        private readonly ErtsRuntimePackageOption[] runtimePackageOptions;
        private readonly object runtimePackageSync = new();
        private ErtsRuntimePackageSelection selectedRuntimePackage;

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

            ErtsHome = ResolveErtsHome(ertsHome);
            ExecutableName = string.IsNullOrWhiteSpace(options.ExecutableName) ? GetDefaultExecutableName() : options.ExecutableName;
            NodeName = NormalizeOptional(options.NodeName);
            UseShortName = options.UseShortName;
            Cookie = NormalizeOptional(options.Cookie);
            startupArguments = BuildStartupArguments(options, NodeName, UseShortName, Cookie);
            environmentVariables = BuildEnvironmentVariables(options);
            runtimePackageOptions = BuildRuntimePackageOptions(options);
            EnableRuntimePackageCommands = options.EnableRuntimePackageCommands && runtimePackageOptions.Length > 0;
        }

        public string ErtsHome { get; }

        public string ExecutableName { get; }

        public string NodeName { get; }

        public bool UseShortName { get; }

        public string Cookie { get; }

        public bool EnableRuntimePackageCommands { get; }

        public IReadOnlyList<string> StartupArguments => startupArguments;

        public IReadOnlyList<KeyValuePair<string, string>> EnvironmentVariables => environmentVariables;

        public IReadOnlyList<ErtsRuntimePackageOption> RuntimePackageOptions => runtimePackageOptions;

        public ErtsRuntimePackageSelection SelectedRuntimePackage
        {
            get
            {
                lock (runtimePackageSync)
                {
                    return selectedRuntimePackage;
                }
            }
        }

        public IReadOnlyList<ErtsPlatform> SupportedRuntimePackagePlatforms =>
            runtimePackageOptions
                .Select(option => option.Platform)
                .Distinct()
                .ToArray();

        public ErtsRuntimePackageSelection SelectRuntimePackage(ErtsPlatform platform, string optionName)
        {
            var selectedOption = ResolveRuntimePackageOption(platform, optionName);
            var selection = new ErtsRuntimePackageSelection(selectedOption.Platform, selectedOption.OptionName);

            lock (runtimePackageSync)
            {
                selectedRuntimePackage = selection;
            }

            return selection;
        }

        public ErtsRuntimePackageOption ResolveRuntimePackageOption(ErtsPlatform platform, string optionName)
        {
            var normalizedOptionName = ValidateRequired(optionName, nameof(optionName));

            foreach (var runtimePackageOption in runtimePackageOptions)
            {
                if (runtimePackageOption.Platform == platform &&
                    string.Equals(runtimePackageOption.OptionName, normalizedOptionName, StringComparison.OrdinalIgnoreCase))
                {
                    return runtimePackageOption;
                }
            }

            throw new ArgumentException(
                $"Unknown runtime package option '{normalizedOptionName}' for platform '{platform}'.",
                nameof(optionName));
        }

        private static string ResolveCommand(string ertsHome, ErtsResourceOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var root = ResolveErtsHome(ertsHome);
            var executableName = string.IsNullOrWhiteSpace(options.ExecutableName) ? GetDefaultExecutableName() : options.ExecutableName;

            return Path.Combine(root, "bin", executableName);
        }

        private static string ResolveWorkingDirectory(string ertsHome, ErtsResourceOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return string.IsNullOrWhiteSpace(options.WorkingDirectory)
                ? ResolveErtsHome(ertsHome)
                : options.WorkingDirectory;
        }

        private static string ResolveErtsHome(string ertsHome)
        {
            var resolvedHome = NormalizeOptional(ertsHome);

            if (string.IsNullOrWhiteSpace(resolvedHome))
            {
                resolvedHome = NormalizeOptional(Environment.GetEnvironmentVariable("ERTS_HOME")) ??
                    NormalizeOptional(Environment.GetEnvironmentVariable("ERLANG_HOME"));
            }

            resolvedHome = NormalizeOptional(resolvedHome);
            if (string.IsNullOrWhiteSpace(resolvedHome))
            {
                throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(ertsHome));
            }

            var expandedPath = NormalizeOptional(Environment.ExpandEnvironmentVariables(resolvedHome));
            if (string.IsNullOrWhiteSpace(expandedPath))
            {
                throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(ertsHome));
            }

            return Path.GetFullPath(expandedPath);
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

        private static ErtsRuntimePackageOption[] BuildRuntimePackageOptions(ErtsResourceOptions options)
        {
            var packageOptions = new List<ErtsRuntimePackageOption>();
            var knownOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var runtimePackageOption in options.RuntimePackageOptions)
            {
                ArgumentNullException.ThrowIfNull(runtimePackageOption);

                var optionKey = $"{runtimePackageOption.Platform}:{runtimePackageOption.OptionName}";
                if (!knownOptions.Add(optionKey))
                {
                    throw new ArgumentException(
                        $"Duplicate runtime package option '{runtimePackageOption.OptionName}' for platform '{runtimePackageOption.Platform}'.",
                        nameof(options));
                }

                packageOptions.Add(runtimePackageOption);
            }

            return packageOptions.ToArray();
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
