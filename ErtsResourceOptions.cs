using Aspire.Hosting;

namespace WebNet.AspireToolkit.Erlang
{
    [AspireExport(ExposeProperties = true)]
    public sealed class ErtsResourceOptions
    {
        public ErtsResourceOptions()
        {
            Arguments = new List<string>();
            EnvironmentVariables = new Dictionary<string, string>(StringComparer.Ordinal);
            RuntimePackageOptions = CreateDefaultRuntimePackageOptions();
            UseShortName = true;
            EnableRuntimePackageCommands = true;
        }

        public string WorkingDirectory { get; set; }

        public string ExecutableName { get; set; }

        public string NodeName { get; set; }

        public bool UseShortName { get; set; }

        public string Cookie { get; set; }

        public bool EnableRuntimePackageCommands { get; set; }

        public IList<string> Arguments { get; }

        public IDictionary<string, string> EnvironmentVariables { get; }

        public IList<ErtsRuntimePackageOption> RuntimePackageOptions { get; }

        private static IList<ErtsRuntimePackageOption> CreateDefaultRuntimePackageOptions()
        {
            return new List<ErtsRuntimePackageOption>
            {
                new ErtsRuntimePackageOption(
                    ErtsPlatform.Windows,
                    "winget",
                    "winget",
                    "Erlang.ErlangOTP",
                    "winget install Erlang.ErlangOTP",
                    "Install Erlang/OTP from the Windows package catalog."),
                new ErtsRuntimePackageOption(
                    ErtsPlatform.Windows,
                    "chocolatey",
                    "chocolatey",
                    "erlang",
                    "choco install erlang",
                    "Install Erlang/OTP using Chocolatey."),
                new ErtsRuntimePackageOption(
                    ErtsPlatform.Linux,
                    "apt",
                    "apt",
                    "erlang",
                    "sudo apt-get install -y erlang",
                    "Install Erlang/OTP from Debian or Ubuntu repositories."),
                new ErtsRuntimePackageOption(
                    ErtsPlatform.Linux,
                    "dnf",
                    "dnf",
                    "erlang",
                    "sudo dnf install -y erlang",
                    "Install Erlang/OTP from Fedora or RHEL-compatible repositories."),
                new ErtsRuntimePackageOption(
                    ErtsPlatform.MacOS,
                    "homebrew",
                    "homebrew",
                    "erlang",
                    "brew install erlang",
                    "Install Erlang/OTP from Homebrew."),
                new ErtsRuntimePackageOption(
                    ErtsPlatform.MacOS,
                    "pkg",
                    "pkg-installer",
                    "Erlang OTP",
                    "installer -pkg otp.pkg -target /",
                    "Install Erlang/OTP from a macOS package installer.")
            };
        }
    }
}
