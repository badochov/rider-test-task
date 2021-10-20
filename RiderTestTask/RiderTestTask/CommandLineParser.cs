using System;
using System.Collections.Generic;
using CommandLine;

namespace RiderTestTask
{
    public record CommandLineOptions
    {
        [Value(index: 0, Required = true, HelpText = "Directory to analyze.")]
        public string Directory { get; set; }
        
        
        [Value(index: 1, Required = true, HelpText = "Output file.")]
        public string OutputFile { get; set; }

        [Value(index: 2, Required = true, HelpText = "File masks.")]
        public IEnumerable<string> Masks { get; set; }
    }

    public static class CommandLineParser
    {
        public static CommandLineOptions Parse(string[] args)
        {
            ParserResult<CommandLineOptions> result = Parser.Default.ParseArguments<CommandLineOptions>(args);
            if (result.Tag == ParserResultType.Parsed)
            {
                return result.Value;
            }

            Console.Error.WriteLine("Error while parsing arguments.");
            Environment.Exit(1);

            return default;
        }
    }
}