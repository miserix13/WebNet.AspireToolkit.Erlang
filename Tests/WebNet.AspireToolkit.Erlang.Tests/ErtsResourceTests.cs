using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace WebNet.AspireToolkit.Erlang.Tests
{
    public class ErtsResourceTests
    {
        [Fact]
        public void ConstructorBuildsCommandAndStartupArguments()
        {
            var options = new ErtsResourceOptions
            {
                WorkingDirectory = @"C:\otp\work",
                NodeName = "webnet",
                Cookie = "cookie",
                UseShortName = false
            };

            options.Arguments.Add("-noshell");
            options.Arguments.Add("-detached");
            options.EnvironmentVariables["ERL_FLAGS"] = "+S 2:2";

            var resource = new ErtsResource("erlang", @"C:\otp", options);

            Assert.Equal("erlang", resource.Name);
            Assert.Equal(Path.Combine(@"C:\otp", "bin", OperatingSystem.IsWindows() ? "erl.exe" : "erl"), resource.Command);
            Assert.Equal(@"C:\otp\work", resource.WorkingDirectory);
            Assert.Equal(@"C:\otp", resource.ErtsHome);
            Assert.Equal("webnet", resource.NodeName);
            Assert.Equal("cookie", resource.Cookie);
            Assert.Equal(new[] { "-name", "webnet", "-setcookie", "cookie", "-noshell", "-detached" }, resource.StartupArguments);
            Assert.Single(resource.EnvironmentVariables);
            Assert.Equal("ERL_FLAGS", resource.EnvironmentVariables[0].Key);
            Assert.Equal("+S 2:2", resource.EnvironmentVariables[0].Value);
        }

        [Fact]
        public void AddErtsRegistersResourceAndExecutionAnnotations()
        {
            var builder = DistributedApplication.CreateBuilder();

            var resourceBuilder = builder.AddErts("erlang", @"C:\otp", options =>
            {
                options.NodeName = "webnet";
                options.Arguments.Add("-noshell");
                options.EnvironmentVariables["ERL_FLAGS"] = "+S 2:2";
            });

            var resource = Assert.IsType<ErtsResource>(resourceBuilder.Resource);

            Assert.Contains(builder.Resources, candidate => ReferenceEquals(candidate, resource));
            Assert.Contains(resource.Annotations, annotation => annotation is CommandLineArgsCallbackAnnotation);
            Assert.Contains(resource.Annotations, annotation => annotation is EnvironmentCallbackAnnotation);
            Assert.Equal(new[] { "-sname", "webnet", "-noshell" }, resource.StartupArguments);
        }
    }
}
