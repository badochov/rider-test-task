using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;

namespace RiderTestTask
{
    public class FileProcessor
    {
        private readonly IEnumerable<string> PossibleDelimiters = new[] {",", "\t", " "};

        public class FileFormatException : Exception
        {
        }

        public enum DataType
        {
            String,
            Date,
            Number
        }

        public record Result
        {
            public Result(
                IList<string> header,
                IList<DataType> columnTypes,
                string delimiter,
                (string, string) numberFormat,
                string dateFormat)
            {
                ColumnTypes = new Dictionary<string, DataType>();
                for (int i = 0; i < header.Count; i++)
                {
                    ColumnTypes[header[i]] = columnTypes[i];
                }

                Delimiter = delimiter;
                NumberFormat = numberFormat;
                DateFormat = dateFormat;
            }

            public Dictionary<string, DataType> ColumnTypes { get; }
            public string Delimiter { get; }
            public (string, string) NumberFormat { get; }
            public string DateFormat { get; }
        }

        public readonly struct ColumnsData
        {
            public ColumnsData(IList<string> header, IList<IList<string>> columns, string delimiter)
            {
                Header = header;
                Columns = columns;
                Delimiter = delimiter;
            }

            public IList<string> Header { get; }
            public IList<IList<string>> Columns { get; }
            public string Delimiter { get; }
        }

        private string FileName { get; }

        public FileProcessor(string fileName)
        {
            FileName = fileName;
        }

        public Task<Result> Process()
        {
            return Task.Run(() =>
            {
                ColumnsData data = GetColumnsData();
                IList<Result> resultsFromDifferentConfigurations = GetResultsFromDifferentConfigurations(data);
                Result result = resultsFromDifferentConfigurations.First();
                int minCount = data.Header.Count + 1;
                foreach (Result res in resultsFromDifferentConfigurations)
                {
                    int count = CountStringType(res);
                    if (count < minCount)
                    {
                        result = res;
                        minCount = count;
                    }
                }

                return result;
            });
        }

        private IList<Result> GetResultsFromDifferentConfigurations(ColumnsData data)
        {
            IList<Result> results = new List<Result>();
            foreach (var (dateFormat, numberFormat) in GetFormatsEnumerator())
            {
                results.Add(GetResultInFormat(data, dateFormat, numberFormat));
            }

            return results;
        }

        private Result GetResultInFormat(ColumnsData data, string dateFormat, (string, string) numberFormat)
        {
            IList<DataType> columnTypes = data.Columns
                .Select(dataColumn => DeduceColumnType(dataColumn, dateFormat, numberFormat)).ToList();

            return new Result(data.Header, columnTypes, data.Delimiter, numberFormat, dateFormat);
        }

        private IEnumerable<(string, (string, string))> GetFormatsEnumerator()
        {
            IEnumerable<string> digitalSeparators = new[]
            {
                ".",
                ",",
            };
            IEnumerable<string> thousandsSeparators = new[]
            {
                ".",
                ",",
                " ",
            };
            IEnumerable<string> formats = new[]
            {
                "yyyy.MM.dd",
                "dd.MM.yyyy",
                "MM.dd.yyyy",
                "yyyy/MM/dd",
                "dd/MM/yyyy",
                "MM/dd/yyyy",
            };

            return from format in formats
                from digitalSeparator in digitalSeparators
                from thousandsSeparator in thousandsSeparators
                where digitalSeparator != thousandsSeparator
                select (format, (thousandsSeparator, digitalSeparator));
        }

        private int CountStringType(Result res)
        {
            return res.ColumnTypes.Count(resColumnType => resColumnType.Value is DataType.String);
        }

        private DataType DeduceColumnType(IList<string> columnValues, string dateFormat, (string, string) numberFormat)
        {
            return IsNumberColumn(columnValues, numberFormat)
                ? DataType.Number
                : (IsDateColumn(columnValues, dateFormat) ? DataType.Date : DataType.String);
        }

        private bool IsNumberColumn(IEnumerable<string> columnValues, (string, string) numberFormat)
        {
            var (thousands, digital) = numberFormat;

            return columnValues.All(value => IsNumberWithGivenSeparators(value, thousands, digital));
        }

        private bool IsNumberWithGivenSeparators(string number, string thousandsSeparator, string digitalSeparator)
        {
            Regex regex = new($"^((\\d{{1,3}}(\\{thousandsSeparator}\\d{{3}})+)|(\\d+))(\\{digitalSeparator}\\d+)?$");
            return regex.IsMatch(number);
        }

        private bool IsDateColumn(IEnumerable<string> columnValues, string dateFormat)
        {
            return columnValues.All(value => IsGoodDateInFormat(value, dateFormat));
        }

        private bool IsGoodDateInFormat(string value, string format)
        {
            return DateTime.TryParseExact(
                value,
                format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _);
        }


        private ColumnsData GetColumnsData()
        {
            foreach (string delimiter in PossibleDelimiters)
            {
                try
                {
                    using TextFieldParser parser = new(FileName);
                    parser.TextFieldType = FieldType.Delimited;
                    parser.HasFieldsEnclosedInQuotes = true;
                    parser.Delimiters = new[] {delimiter};
                    string[] headers = parser.ReadFields() ?? throw new FileFormatException();
                    IList<IList<string>> columns = new List<IList<string>>();
                    foreach (string _ in headers)
                    {
                        columns.Add(new List<string>());
                    }

                    while (!parser.EndOfData)
                    {
                        string[]? row = parser.ReadFields();
                        if (row is null || row.Length != headers.Length)
                        {
                            throw new FileFormatException();
                        }

                        for (int i = 0; i < headers.Length; i++)
                        {
                            columns[i].Add(row[i]);
                        }
                    }

                    return new ColumnsData(headers, columns, delimiter);
                }
                catch (FileFormatException)
                {
                }
                catch (MalformedLineException)
                {
                }
            }

            throw new FileFormatException();
        }
    }
}