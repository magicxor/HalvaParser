using CommandLine;

namespace HalvaParser.Models.Application
{
    public class CommandLineOptions
    {
        [Option('o', "output", Required = true, HelpText = @"Output file path, for example: ""C:\Output\out.csv""")]
        public string Output { get; set; }

        [Option('r', "delimiter", Required = false, HelpText = "Delimiter, for example: ,", Default = ";")]
        public string Delimiter { get; set; }
        
        [Option('t', "latitude", Required = false, HelpText = "Latitude", Default = "55.7558")]
        public string Latitude { get; set; }

        [Option('g', "longitude", Required = false, HelpText = "Longitude", Default = "37.6173")]
        public string Longitude { get; set; }
    }
}
