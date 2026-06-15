using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace WebNet.AspireToolkit.Erlang
{
    public static class ErtsResourceBuilderExtensions
    {
        public static IResourceBuilder<ErtsResource> AddErts(this IDistributedApplicationBuilder builder, string name, string ertsHome)
        {
            return AddErts(builder, name, ertsHome, configure: null);
        }

        public static IResourceBuilder<ErtsResource> AddErts(this IDistributedApplicationBuilder builder, string name, string ertsHome, Action<ErtsResourceOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(builder);

            var options = new ErtsResourceOptions();
            configure?.Invoke(options);

            return AddErts(builder, new ErtsResource(name, ertsHome, options));
        }

        public static IResourceBuilder<ErtsResource> AddErts(this IDistributedApplicationBuilder builder, ErtsResource resource)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(resource);

            var resourceBuilder = builder.AddResource(resource);

            if (resource.StartupArguments.Count > 0)
            {
                resourceBuilder.WithArgs(resource.StartupArguments.ToArray());
            }

            foreach (var environmentVariable in resource.EnvironmentVariables)
            {
                resourceBuilder.WithEnvironment(environmentVariable.Key, environmentVariable.Value);
            }

            return resourceBuilder;
        }
    }
}
