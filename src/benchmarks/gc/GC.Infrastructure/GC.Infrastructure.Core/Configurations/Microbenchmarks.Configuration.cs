using GC.Infrastructure.Core.Configurations.GCPerfSim;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GC.Infrastructure.Core.Configurations.Microbenchmarks
{
    public sealed class MicrobenchmarkConfiguration : ConfigurationBase
    {
        public string? microbenchmarks_path { get; set; }
        public Dictionary<string, Run>? Runs { get; set; }
        public MicrobenchmarkConfigurations? MicrobenchmarkConfigurations { get; set; }
        public Environment? Environment { get; set; }
        public Output? Output { get; set; }
        public string? Path { get; set; }
    }

    public sealed class Run : RunBase
    {
        public string? DotnetInstaller { get; set; }
        public string? Name { get; set; }
        public string? corerun { get; set; }
        public bool is_baseline { get; set; }
    }

    public class Environment
    {
        public uint default_max_seconds { get; set; } = 300;
    }

    public sealed class MicrobenchmarkConfigurations
    {
        public string? Filter { get; set; }
        public string? FilterPath { get; set; }
        public string? InvocationCountPath { get; set; }
        public string? DotnetInstaller { get; set; }
        public string? bdn_arguments { get; set; } = null;
    }

    public sealed class Output : OutputBase
    {
        public List<string>? cpu_columns { get; set; }
        public List<string>? additional_report_metrics { get; set; }
        public List<string>? run_comparisons { get; set; }
    }
    public static class MicrobenchmarkConfigurationParser
    {
        private static readonly IDeserializer _deserializer =
            new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();

        public static MicrobenchmarkConfiguration Parse(string path)
        {
            // Check to ensure the path to the file exists.
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                throw new ArgumentNullException($"{nameof(MicrobenchmarkConfigurationParser)}: The path to the Microbenchmark configuration: {nameof(path)} is null/empty or doesn't exist. This path should be specified and must be valid.");
            }

            string serializedConfiguration = File.ReadAllText(path);
            MicrobenchmarkConfiguration? configuration = null; 

            // This try catch is here because the exception from the YamlDotNet isn't helpful and must be imbued with more details.
            try
            {
                configuration = _deserializer.Deserialize<MicrobenchmarkConfiguration>(serializedConfiguration);
            }

            catch (Exception ex)
            {
                throw new ArgumentException($"{nameof(GCPerfSimConfiguration)}: Unable to parse the yaml file because of an error in the syntax. Please use the configurations under: Configuration/Microbenchmark/*.yaml in as an example to ensure the file is formatted correctly. Exception: {ex.Message} \n Call Stack: {ex.StackTrace}");
            }

            // Checks if mandatory arguments are specified in the configuration.
            if (configuration == null)
            {
                throw new ArgumentNullException($"{nameof(MicrobenchmarkConfigurationParser)}: The parsed configuration is null - please check the syntax of the configuration by following an example under: Configuration/Microbenchmark/*.yaml.");
            }

            // Check to see if the Microbenchmark Configurations are specified.
            if (configuration.MicrobenchmarkConfigurations == null)
            {
                throw new ArgumentNullException($"{nameof(MicrobenchmarkConfigurationParser)}: {nameof(configuration.MicrobenchmarkConfigurations)} is null. This is a default node in the yaml that should be specified.");
            }

            // Check to see if either the filter or the filter path is specified. One is mandatory.
            if (string.IsNullOrEmpty(configuration.MicrobenchmarkConfigurations.Filter) && string.IsNullOrEmpty(configuration.MicrobenchmarkConfigurations.FilterPath))
            {
                throw new ArgumentNullException($"{nameof(MicrobenchmarkConfigurationParser)}: Either the filter with a pipe (|) separated collection of benchmarks to filter on or a file with the same must be specified.");
            }

            // Check if the dotnet installer i.e., the framework version is specified.
            if (string.IsNullOrEmpty(configuration.MicrobenchmarkConfigurations.DotnetInstaller))
            {
                throw new ArgumentNullException($"{nameof(MicrobenchmarkConfigurationParser)}: A framework version must be specified e.g. 'net7.0'. The {nameof(configuration.MicrobenchmarkConfigurations.DotnetInstaller)} field is missing on the MicrobenchmarkConfigurations.");
            }

            // Trace Configurations must have a type specified.
            if (configuration.TraceConfigurations != null && string.IsNullOrEmpty(configuration.TraceConfigurations.Type))
            {
                throw new ArgumentNullException($"{nameof(MicrobenchmarkConfigurationParser)}: The type of trace to be collected is null or empty. This value should be specified if the a 'trace_configurations' node is added. Possible values: gc, verbose, cpu, cpu_managed, threadtime, threadtime_managed.");
            }

            // Check to see if the Runs are valid. 
            if (configuration.Runs == null || configuration.Runs.Count == 0)
            {
                throw new ArgumentNullException($"{nameof(MicrobenchmarkConfigurationParser)}: Please specify one or more Runs under the 'runs' field. Currently, either the field doesn't exist or none have been specified.");
            }

            // Check to see if the paths to all the runs are valid.
            foreach (var run in configuration.Runs!)
            {
                if (string.IsNullOrEmpty(run.Value.corerun) || !File.Exists(run.Value.corerun))
                {
                    throw new ArgumentNullException($"{nameof(MicrobenchmarkConfigurationParser)}: For the {run.Key} Run, the corerun path doesn't exist or is empty. The path to the corerun must be valid."); 
                }
            }

            // Check to see if the environment is valid.
            if (configuration.Environment == null)
            {
                throw new ArgumentNullException($"{nameof(MicrobenchmarkConfigurationParser)}: The environment field is missing in the yaml file. Please ensure that the value is added.");
            }

            // Check to see if the output field is added.
            if (configuration.Output == null) 
            {
                throw new ArgumentNullException($"{nameof(MicrobenchmarkConfigurationParser)}: The output field is missing in the yaml file. Please ensure that the value is added.");
            }

            if (string.IsNullOrEmpty(configuration.Output!.Path))
            {
                throw new ArgumentNullException($"{nameof(MicrobenchmarkConfigurationParser)}: The path in the output field is missing in the yaml file. Please ensure that the value is added.");
            }

            return configuration;
        }
    }
}
