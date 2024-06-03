using CsvHelper.Configuration;

namespace DaemonAtorService;

public class DietStatisticMap : ClassMap<DietStatistic>
{
    public DietStatisticMap()
    {
        Map(m => m.Date).Index(0).TypeConverterOption.Format("MM/dd/yyyy");
        Map(m => m.Name).Index(1);
        Map(m => m.Icon).Index(2);
        Map(m => m.Type).Index(3);
        Map(m => m.Quantity).Index(4).TypeConverter(new DefaultIfInvalidDoubleConverter(0));
        Map(m => m.Units).Index(5);
        Map(m => m.Calories).Index(6).TypeConverter(new DefaultIfInvalidDoubleConverter(0));
        Map(m => m.Deleted).Index(7).TypeConverter(new DefaultIfInvalidIntConverter(0));
        Map(m => m.Fat).Index(8).Optional().TypeConverter(new DefaultIfInvalidDoubleConverter(0));
        Map(m => m.Protein).Index(9).Optional().TypeConverter(new DefaultIfInvalidDoubleConverter(0));
        Map(m => m.Carbohydrates).Index(10).Optional().TypeConverter(new DefaultIfInvalidDoubleConverter(0));
        Map(m => m.SaturatedFat).Index(11).Optional().TypeConverter(new DefaultIfInvalidDoubleConverter(0));
        Map(m => m.Sugars).Index(12).Optional().TypeConverter(new DefaultIfInvalidDoubleConverter(0));
        Map(m => m.Fiber).Index(13).Optional().TypeConverter(new DefaultIfInvalidDoubleConverter(0));
        Map(m => m.Cholesterol).Index(14).Optional().TypeConverter(new DefaultIfInvalidDoubleConverter(0));
        Map(m => m.Sodium).Index(15).Optional().TypeConverter(new DefaultIfInvalidDoubleConverter(0));
    }
}
