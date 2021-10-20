using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RiderTestTask
{
    public class DirectoryProcessor
    {
        public class Result
        {
            public Result()
            {
                Results = new List<FileProcessor.Result>();
            }

            public IList<FileProcessor.Result> Results;

            public void AddFileResult(FileProcessor.Result result)
            {
                Results.Add(result);
            }

            public void Save(string outputFile)
            {
                File.WriteAllText(outputFile, "");
                SaveStructures(outputFile);
                SaveFormats(outputFile);
            }

            public void SaveStructures(string outputFile)
            {
                File.AppendAllText(outputFile,
                    "Encountered file structures: count" + Environment.NewLine + Environment.NewLine);

                foreach (var (key, value) in GetStructureCounts())
                {
                    File.AppendAllText(outputFile,
                        $"({key}): {value}{Environment.NewLine}"
                    );
                }

                File.AppendAllText(outputFile, Environment.NewLine + Environment.NewLine + Environment.NewLine);
            }

            private Dictionary<string, int> GetStructureCounts()
            {
                Dictionary<string, int> structureCounts = new();
                foreach (var result in Results)
                {
                    string structure = GetFileStructure(result);
                    int prevCount = structureCounts.GetValueOrDefault(structure, 0);
                    structureCounts[structure] = prevCount + 1;
                }

                return structureCounts;
            }

            private string GetFileStructure(FileProcessor.Result result)
            {
                List<string> columns = new();

                foreach (var resultColumnType in result.ColumnTypes)
                {
                    columns.Add($"{resultColumnType.Key}: {resultColumnType.Value}");
                }

                columns.Sort();

                return string.Join(", ", columns);
            }

            private Dictionary<string, int> GetFormatsCounts()
            {
                Dictionary<string, int> structureCounts = new();
                foreach (var result in Results)
                {
                    var (thousands, digits) = result.NumberFormat;
                    string structure = ($"`{result.Delimiter}`", $"thousands: `{thousands}`, digits: `{digits}`", result.DateFormat).ToString();
                    int prevCount = structureCounts.GetValueOrDefault(structure, 0);
                    structureCounts[structure] = prevCount + 1;
                }

                return structureCounts;
            }

            public void SaveFormats(string outputFile)
            {
                File.AppendAllText(outputFile,
                    $"Encountered file formats:{Environment.NewLine}(delimiter, number format, date format) : count{Environment.NewLine}{Environment.NewLine}");

                foreach (var (key, value) in GetFormatsCounts())
                {
                    File.AppendAllText(outputFile,
                        $"{key}: {value}{Environment.NewLine}"
                    );
                }

                File.AppendAllText(outputFile, Environment.NewLine + Environment.NewLine + Environment.NewLine);
            }
        }

        private string DirectoryName { get; }
        private IEnumerable<string> FileNameMasks { get; }

        public DirectoryProcessor(string directoryName, IEnumerable<string> fileNameMasks)
        {
            DirectoryName = directoryName;
            FileNameMasks = fileNameMasks;
        }

        public async Task<Result> Process()
        {
            IList<Task<FileProcessor.Result>> fileProcessors = StartFileProcessors();
            return await GetResultFromFileProcessors(fileProcessors);
        }

        private IList<Task<FileProcessor.Result>> StartFileProcessors()
        {
            IList<Task<FileProcessor.Result>> fileProcessors = new List<Task<FileProcessor.Result>>();

            foreach (string mask in FileNameMasks)
            {
                foreach (string fileName in Directory.EnumerateFiles(DirectoryName, mask, SearchOption.AllDirectories))
                {
                    FileProcessor fileProcessor = new(fileName);
                    fileProcessors.Add(fileProcessor.Process());
                }
            }

            return fileProcessors;
        }

        private async Task<Result> GetResultFromFileProcessors(IList<Task<FileProcessor.Result>> fileProcessors)
        {
            Result result = new();
            foreach (Task<FileProcessor.Result> fileProcessorTask in fileProcessors)
            {
                result.AddFileResult(await fileProcessorTask);
            }

            return result;
        }
    }
}