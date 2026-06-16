using Aspire.Hosting;

namespace WebNet.AspireToolkit.Erlang
{
    [AspireDto]
    public sealed class ErtsRuntimePackageOption
    {
        public ErtsRuntimePackageOption(
            ErtsPlatform platform,
            string optionName,
            string packageManager,
            string packageId,
            string installCommand,
            string description = null)
        {
            Platform = platform;
            OptionName = ValidateRequired(optionName, nameof(optionName));
            PackageManager = ValidateRequired(packageManager, nameof(packageManager));
            PackageId = ValidateRequired(packageId, nameof(packageId));
            InstallCommand = ValidateRequired(installCommand, nameof(installCommand));
            Description = NormalizeOptional(description);
        }

        public ErtsPlatform Platform { get; }

        public string OptionName { get; }

        public string PackageManager { get; }

        public string PackageId { get; }

        public string InstallCommand { get; }

        public string Description { get; }

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
