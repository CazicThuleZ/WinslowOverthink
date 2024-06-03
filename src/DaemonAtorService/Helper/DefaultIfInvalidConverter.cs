using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace DaemonAtorService;

public class DefaultIfInvalidDoubleConverter : DefaultTypeConverter
{
    private readonly double? _defaultValue;

    public DefaultIfInvalidDoubleConverter(double? defaultValue)
    {
        _defaultValue = defaultValue;
    }

    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
            return _defaultValue;

        // Remove commas from the number
        text = text.Replace(",", "");

        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            return result;

        return _defaultValue;
    }
}

public class DefaultIfInvalidIntConverter : DefaultTypeConverter
{
    private readonly int? _defaultValue;

    public DefaultIfInvalidIntConverter(int? defaultValue)
    {
        _defaultValue = defaultValue;
    }

    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return _defaultValue;
        }

        if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out int result))
        {
            return result;
        }

        return _defaultValue;
    }
}