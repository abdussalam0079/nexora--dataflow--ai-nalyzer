using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using ExcelDataReader;

namespace DataFlow.WebApi.Services;

public record ParsedFile(List<string> Headers, List<Dictionary<string, object?>> Rows);

public static class FileParserService
{
    public static ParsedFile Parse(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".csv" or ".tsv" => ParseCsv(path, ext == ".tsv" ? '\t' : ','),
            ".xlsx" or ".xls" => ParseExcel(path),
            ".json" => ParseJson(path),
            _ => throw new NotSupportedException($"Unsupported file type: {ext}")
        };
    }

    private static ParsedFile ParseCsv(string path, char delimiter)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter.ToString(),
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord?.ToList() ?? [];

        var rows = new List<Dictionary<string, object?>>();
        while (csv.Read())
        {
            var row = new Dictionary<string, object?>();
            foreach (var h in headers)
            {
                var val = csv.GetField(h);
                row[h] = TryParseNumber(val, out var num) ? num : val;
            }
            rows.Add(row);
        }
        return new ParsedFile(headers, rows);
    }

    private static ParsedFile ParseExcel(string path)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var ds = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
        });

        var table = ds.Tables[0];
        var headers = table.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName).ToList();
        var rows = new List<Dictionary<string, object?>>();
        foreach (System.Data.DataRow r in table.Rows)
        {
            var row = new Dictionary<string, object?>();
            foreach (var h in headers)
            {
                var val = r[h];
                row[h] = val == DBNull.Value ? null : val is double or float or int or long ? val : val?.ToString();
            }
            rows.Add(row);
        }
        return new ParsedFile(headers, rows);
    }

    private static ParsedFile ParseJson(string path)
    {
        var json = File.ReadAllText(path);
        var list = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(json)
                   ?? throw new InvalidDataException("JSON must be an array of objects.");
        var headers = list.FirstOrDefault()?.Keys.ToList() ?? [];
        return new ParsedFile(headers, list);
    }

    private static bool TryParseNumber(string? s, out object num)
    {
        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
        { num = d; return true; }
        num = null!;
        return false;
    }
}
