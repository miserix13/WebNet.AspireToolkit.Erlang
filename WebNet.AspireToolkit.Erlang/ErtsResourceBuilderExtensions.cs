using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;

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

            return AddErts(builder, name, ertsHome, options);
        }

        /// <summary>
        /// Adds an Erlang runtime resource using a concrete options object.
        /// </summary>
        /// <ats-summary>
        /// Adds an Erlang runtime resource and configures platform-specific ERTS runtime package options for dashboard-driven management.
        /// </ats-summary>
        [AspireExport]
        public static IResourceBuilder<ErtsResource> AddErts(this IDistributedApplicationBuilder builder, string name, string ertsHome, ErtsResourceOptions options)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(options);

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

            RegisterRuntimePackageCommands(resourceBuilder, resource);

            return resourceBuilder;
        }

        private static void RegisterRuntimePackageCommands(IResourceBuilder<ErtsResource> resourceBuilder, ErtsResource resource)
        {
            if (!resource.EnableRuntimePackageCommands)
            {
                return;
            }

            resourceBuilder.WithCommand(
                "list-runtime-packages",
                "List ERTS runtime packages",
                _ => Task.FromResult(BuildRuntimePackageCatalogResult(resource)),
                new CommandOptions
                {
                    Description = "Show the platform-specific ERTS package options available for this runtime resource.",
                    Visibility = ResourceCommandVisibility.UI | ResourceCommandVisibility.Api,
                    IconName = "BoxMultiple"
                });

            resourceBuilder.WithCommand(
                "select-runtime-package",
                "Select ERTS runtime package",
                context => ExecuteRuntimePackageSelectionAsync(resource, context),
                new CommandOptions
                {
                    Description = "Choose a platform-specific ERTS runtime package option and surface the install command in the dashboard.",
                    Arguments = CreateRuntimePackageInputs(resource),
                    ValidateArguments = validationContext =>
                    {
                        ValidateRuntimePackageArguments(resource, validationContext);
                        return Task.CompletedTask;
                    },
                    Visibility = ResourceCommandVisibility.UI | ResourceCommandVisibility.Api,
                    ConfirmationMessage = "Select this runtime package option for the Erlang runtime resource?",
                    IconName = "Package",
                    IsHighlighted = true
                });
        }

        private static ExecuteCommandResult BuildRuntimePackageCatalogResult(ErtsResource resource)
        {
            return CommandResults.Success(
                "Available ERTS runtime package options.",
                BuildRuntimePackageCatalog(resource),
                CommandResultFormat.Text,
                true);
        }

        private static async Task<ExecuteCommandResult> ExecuteRuntimePackageSelectionAsync(ErtsResource resource, ExecuteCommandContext context)
        {
            try
            {
                var arguments = context.Arguments;
                var platformText = ReadArgument(arguments, "platform");
                var optionName = ReadArgument(arguments, "optionName");

                if (string.IsNullOrWhiteSpace(platformText) || string.IsNullOrWhiteSpace(optionName))
                {
                    var interactionService = context.ServiceProvider.GetService<IInteractionService>();

                    if (interactionService is not null && interactionService.IsAvailable)
                    {
                        var promptResult = await interactionService.PromptInputsAsync(
                            "Select ERTS runtime package",
                            "Choose the target platform and package option for this Erlang runtime.",
                            CreateRuntimePackageInputs(resource),
                            new InputsDialogInteractionOptions(),
                            context.CancellationToken).ConfigureAwait(false);

                        if (promptResult.Canceled)
                        {
                            return CommandResults.Canceled();
                        }

                        platformText = ReadArgument(promptResult.Data, "platform");
                        optionName = ReadArgument(promptResult.Data, "optionName");
                    }
                }

                var selection = SelectRuntimePackage(resource, platformText, optionName);
                var selectedOption = resource.ResolveRuntimePackageOption(selection.Platform, selection.OptionName);

                return CommandResults.Success(
                    $"Selected the {selectedOption.Platform} runtime package option '{selectedOption.OptionName}'.",
                    BuildRuntimePackageSelectionResult(selectedOption),
                    CommandResultFormat.Text,
                    true);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                context.Logger.LogError(ex, "Failed to select an ERTS runtime package option.");
                return CommandResults.Failure(ex.Message);
            }
        }

        private static IReadOnlyList<InteractionInput> CreateRuntimePackageInputs(ErtsResource resource)
        {
            return new[]
            {
                new InteractionInput
                {
                    Name = "platform",
                    Label = "Platform",
                    Description = $"Supported values: {string.Join(", ", resource.SupportedRuntimePackagePlatforms.Select(FormatPlatform))}",
                    InputType = InputType.Text,
                    Placeholder = "windows | linux | macos",
                    Required = true
                },
                new InteractionInput
                {
                    Name = "optionName",
                    Label = "Package option",
                    Description = BuildOptionPromptDescription(resource),
                    InputType = InputType.Text,
                    Placeholder = "winget | apt | homebrew",
                    Required = true
                }
            };
        }

        private static void ValidateRuntimePackageArguments(ErtsResource resource, InputsDialogValidationContext validationContext)
        {
            var platformText = ReadArgument(validationContext.Inputs, "platform");
            var optionName = ReadArgument(validationContext.Inputs, "optionName");

            if (string.IsNullOrWhiteSpace(platformText))
            {
                validationContext.AddValidationError("platform", "A target platform is required.");
            }

            if (string.IsNullOrWhiteSpace(optionName))
            {
                validationContext.AddValidationError("optionName", "A runtime package option is required.");
            }

            if (string.IsNullOrWhiteSpace(platformText) || string.IsNullOrWhiteSpace(optionName))
            {
                return;
            }

            if (!TryParsePlatform(platformText, out var platform))
            {
                validationContext.AddValidationError("platform", "Use one of: windows, linux, macos.");
                return;
            }

            try
            {
                resource.ResolveRuntimePackageOption(platform, optionName);
            }
            catch (ArgumentException ex)
            {
                validationContext.AddValidationError("optionName", ex.Message);
            }
        }

        private static ErtsRuntimePackageSelection SelectRuntimePackage(ErtsResource resource, string platformText, string optionName)
        {
            if (string.IsNullOrWhiteSpace(platformText))
            {
                throw new InvalidOperationException("A target platform is required to select an ERTS runtime package.");
            }

            if (string.IsNullOrWhiteSpace(optionName))
            {
                throw new InvalidOperationException("A runtime package option is required to select an ERTS runtime package.");
            }

            if (!TryParsePlatform(platformText, out var platform))
            {
                throw new ArgumentException("Use one of the supported platform values: windows, linux, macos.", nameof(platformText));
            }

            return resource.SelectRuntimePackage(platform, optionName);
        }

        private static string BuildRuntimePackageCatalog(ErtsResource resource)
        {
            var builder = new StringBuilder();

            foreach (var platformGroup in resource.RuntimePackageOptions.GroupBy(option => option.Platform))
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine();
                }

                builder.Append(FormatPlatform(platformGroup.Key));
                builder.AppendLine(":");

                foreach (var option in platformGroup)
                {
                    builder.Append("- ");
                    builder.Append(option.OptionName);
                    builder.Append(" via ");
                    builder.Append(option.PackageManager);
                    builder.Append(" (");
                    builder.Append(option.PackageId);
                    builder.AppendLine(")");
                    builder.Append("  ");
                    builder.AppendLine(option.InstallCommand);

                    if (!string.IsNullOrWhiteSpace(option.Description))
                    {
                        builder.Append("  ");
                        builder.AppendLine(option.Description);
                    }
                }
            }

            return builder.ToString();
        }

        private static string BuildRuntimePackageSelectionResult(ErtsRuntimePackageOption option)
        {
            var builder = new StringBuilder();
            builder.Append("Platform: ");
            builder.AppendLine(FormatPlatform(option.Platform));
            builder.Append("Option: ");
            builder.AppendLine(option.OptionName);
            builder.Append("Package manager: ");
            builder.AppendLine(option.PackageManager);
            builder.Append("Package: ");
            builder.AppendLine(option.PackageId);
            builder.Append("Install command: ");
            builder.AppendLine(option.InstallCommand);

            if (!string.IsNullOrWhiteSpace(option.Description))
            {
                builder.Append("Description: ");
                builder.AppendLine(option.Description);
            }

            return builder.ToString();
        }

        private static string BuildOptionPromptDescription(ErtsResource resource)
        {
            return string.Join(
                "; ",
                resource.RuntimePackageOptions
                    .GroupBy(option => option.Platform)
                    .Select(group => $"{FormatPlatform(group.Key)}={string.Join("|", group.Select(option => option.OptionName))}"));
        }

        private static string ReadArgument(InteractionInputCollection arguments, string name)
        {
            if (arguments is null || !arguments.ContainsName(name))
            {
                return null;
            }

            return NormalizeOptional(arguments.GetString(name));
        }

        private static string FormatPlatform(ErtsPlatform platform)
        {
            return platform switch
            {
                ErtsPlatform.Windows => "Windows",
                ErtsPlatform.Linux => "Linux",
                ErtsPlatform.MacOS => "macOS",
                _ => platform.ToString()
            };
        }

        private static bool TryParsePlatform(string value, out ErtsPlatform platform)
        {
            if (string.Equals(value, "macos", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "mac-os", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "osx", StringComparison.OrdinalIgnoreCase))
            {
                platform = ErtsPlatform.MacOS;
                return true;
            }

            return Enum.TryParse(value, ignoreCase: true, out platform);
        }

        private static string NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
