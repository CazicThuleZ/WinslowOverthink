using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace DaemonAtorService;
public class CsvParser
{
    public List<DietStatistic> ParseCsv(string filePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = true,
            MissingFieldFound = null, // If not ignored, an exception will be thrown.
            BadDataFound = null
        };

        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, config))
        {
            csv.Context.RegisterClassMap<DietStatisticMap>();
            var records = csv.GetRecords<DietStatistic>().ToList();
            return records;
        }
    }
}