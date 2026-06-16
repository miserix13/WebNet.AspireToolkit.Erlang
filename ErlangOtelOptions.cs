using Aspire.Hosting;

namespace WebNet.AspireToolkit.Erlang
{
    [AspireDto]
    public sealed class ErlangOtelOptions
    {
        public ErlangOtelOptions()
        {
            ResourceAttributes = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        public bool Enabled { get; set; }

        public string ServiceName { get; set; }

        public string ServiceVersion { get; set; }

        public string ExporterOtlpEndpoint { get; set; }

        public string Protocol { get; set; }

        public IDictionary<string, string> ResourceAttributes { get; init; }
    }
}
