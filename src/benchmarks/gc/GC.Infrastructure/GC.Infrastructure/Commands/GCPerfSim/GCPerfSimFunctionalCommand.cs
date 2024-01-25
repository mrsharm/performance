using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.GCPerfSim;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Serialization;

namespace GC.Infrastructure.Commands.GCPerfSim
{
    internal sealed class GCPerfSimFunctionalCommand : Command<GCPerfSimFunctionalCommand.GCPerfSimFunctionalSettings>
    {
        private static readonly string _baseSuitePath   = Path.Combine("Commands", "RunCommand", "BaseSuite"); 
        private static readonly string _gcPerfSimBase   = Path.Combine(_baseSuitePath, "GCPerfSim_Normal_Workstation.yaml");
        private static readonly ISerializer _serializer = Common.Serializer;

        public sealed class GCPerfSimFunctionalSettings : CommandSettings
        {
            [Description("Path to Configuration.")]
            [CommandOption("-c|--configuration")]
            public string? ConfigurationPath { get; init; }
        }

        internal static void SaveConfiguration(ConfigurationBase configuration, string outputPath, string fileName)
        {
            var serializedResult = _serializer.Serialize(configuration);
            File.WriteAllText(Path.Combine(outputPath, fileName), serializedResult);
        }


        public override int Execute([NotNull] CommandContext context, [NotNull] GCPerfSimFunctionalSettings settings)
        {
            // I. Extract the configuration path.
            string configurationPath = settings.ConfigurationPath;

            // Parse out the yaml file -> Memory as a C# object make use of that.
            // Precondition checks.
            ConfigurationChecker.VerifyFile(configurationPath, $"{nameof(GCPerfSimFunctionalCommand)}");
            GCPerfSimFunctionalConfiguration configuration = GCPerfSimFunctionalConfigurationParser.Parse(configurationPath);

            // II. Create the test suite for gcperfsim functional tests.
            string suitePath = Path.Combine(configuration.output_path, "Suites");
            string gcPerfSimSuitePath = Path.Combine(suitePath, "GCPerfSim_Functional");

            Core.Utilities.TryCreateDirectory(gcPerfSimSuitePath);

            string gcPerfSimOutputPath = Path.Combine(configuration.output_path, "GCPerfSim");
            Core.Utilities.TryCreateDirectory(gcPerfSimOutputPath);
            GCPerfSimConfiguration gcPerfSimBaseConfiguration = GCPerfSimConfigurationParser.Parse(_gcPerfSimBase);

            // For each of the scenarios below:
            // a. Add the coreruns. 
            // b. Add the gcperfsim parameters that are pertinent to that run.
            // c. Add any environment variables that will be related to that run.

            // 1. Normal Server


            // 2. Normal Workstation.

            // 3. LowMemoryContainer.

            // 4. HighMemoryLoad.


            // III. Execute all the functional tests.
            string[] gcperfsimConfigurations = Directory.GetFiles(gcPerfSimOutputPath, "*.yaml");
            // Look at the RunCommand from 99 onwards to see how these are generated.

            // IV. Based on the results of the functional tests, we'd want to generate a report.
            // Looking at all the runs data, we'd want to aggregate and create a markdown table of the following type:
            // Output should live in Results.md in the output folder.
            // TODO: If the run fails, what are the commands, failures and any debugging information that'll be helpful.
            // | Yaml | Scenario | Pass / Fail  |
            // | ---- | -------- | ------------ |
            // | Normal_Server.yaml | 0gb | Pass |
            // | Normal_Workstation.yaml | 2gb_pinning | Fail |

            return 0;
        }
    }
}
