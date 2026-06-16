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
            Assert.True(resource.EnableRuntimePackageCommands);
            Assert.Contains(resource.RuntimePackageOptions, option => option.Platform == ErtsPlatform.Windows && option.OptionName == "winget");
            Assert.Contains(resource.RuntimePackageOptions, option => option.Platform == ErtsPlatform.Linux && option.OptionName == "apt");
            Assert.Contains(resource.RuntimePackageOptions, option => option.Platform == ErtsPlatform.MacOS && option.OptionName == "homebrew");
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
            Assert.Contains(resource.Annotations, annotation => annotation is ResourceCommandAnnotation resourceCommandAnnotation && resourceCommandAnnotation.Name == "list-runtime-packages");
            Assert.Contains(resource.Annotations, annotation => annotation is ResourceCommandAnnotation resourceCommandAnnotation && resourceCommandAnnotation.Name == "select-runtime-package");
            Assert.Equal(new[] { "-sname", "webnet", "-noshell" }, resource.StartupArguments);
        }

        [Fact]
        public void DirectOptionsOverloadRegistersCustomRuntimePackages()
        {
            var builder = DistributedApplication.CreateBuilder();
            var options = new ErtsResourceOptions();
            options.RuntimePackageOptions.Clear();
            options.RuntimePackageOptions.Add(new ErtsRuntimePackageOption(
                ErtsPlatform.Linux,
                "apk",
                "apk",
                "erlang",
                "sudo apk add erlang",
                "Install Erlang/OTP using Alpine packages."));

            var resourceBuilder = builder.AddErts("erlang", @"/opt/otp", options);
            var resource = Assert.IsType<ErtsResource>(resourceBuilder.Resource);

            var runtimePackageOption = Assert.Single(resource.RuntimePackageOptions);
            Assert.Equal(ErtsPlatform.Linux, runtimePackageOption.Platform);
            Assert.Equal("apk", runtimePackageOption.OptionName);
        }

        [Fact]
        public void SelectRuntimePackageStoresSelection()
        {
            var resource = new ErtsResource("erlang", @"C:\otp", new ErtsResourceOptions());

            var selection = resource.SelectRuntimePackage(ErtsPlatform.Linux, "apt");

            Assert.Equal(ErtsPlatform.Linux, selection.Platform);
            Assert.Equal("apt", selection.OptionName);
            Assert.NotNull(resource.SelectedRuntimePackage);
            Assert.Equal(ErtsPlatform.Linux, resource.SelectedRuntimePackage.Platform);
            Assert.Equal("apt", resource.SelectedRuntimePackage.OptionName);
        }
    }
}
