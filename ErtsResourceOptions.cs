namespace WebNet.AspireToolkit.Erlang
{
    public sealed class ErtsResourceOptions
    {
        public ErtsResourceOptions()
        {
            Arguments = new List<string>();
            EnvironmentVariables = new Dictionary<string, string>(StringComparer.Ordinal);
            UseShortName = true;
        }

        public string WorkingDirectory { get; set; }

        public string ExecutableName { get; set; }

        public string NodeName { get; set; }

        public bool UseShortName { get; set; }

        public string Cookie { get; set; }

        public IList<string> Arguments { get; }

        public IDictionary<string, string> EnvironmentVariables { get; }
    }
}
