using Aspire.Hosting;

namespace WebNet.AspireToolkit.Erlang
{
    [AspireDto]
    public sealed class ErlangMonitoredProcess
    {
        public ErlangMonitoredProcess(string name, string kind, string description = null)
        {
            Name = ValidateRequired(name, nameof(name));
            Kind = ValidateRequired(kind, nameof(kind));
            Description = NormalizeOptional(description);
        }

        public string Name { get; }

        public string Kind { get; }

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
