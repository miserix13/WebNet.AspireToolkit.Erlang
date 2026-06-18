using Aspire.Hosting;

namespace WebNet.AspireToolkit.Erlang
{
    [AspireDto]
    public sealed class ErtsRuntimePackageSelection
    {
        public ErtsRuntimePackageSelection(ErtsPlatform platform, string optionName)
        {
            Platform = platform;
            OptionName = ValidateRequired(optionName, nameof(optionName));
        }

        public ErtsPlatform Platform { get; }

        public string OptionName { get; }

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
