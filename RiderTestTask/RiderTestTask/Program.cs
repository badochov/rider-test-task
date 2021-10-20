using System;
using System.Threading.Tasks;

namespace RiderTestTask
{
    class Program
    {
        public async static Task Main(string[] args)
        {
            CommandLineOptions options = CommandLineParser.Parse(args);
            DirectoryProcessor directoryProcessor = new(options.Directory, options.Masks);
            DirectoryProcessor.Result result = await directoryProcessor.Process();
            result.Save(options.OutputFile);
        }
    }
}