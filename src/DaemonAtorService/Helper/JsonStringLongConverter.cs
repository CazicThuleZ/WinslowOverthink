using System.Text.Json;
using System.Text.Json.Serialization;

namespace DaemonAtorService;

public class JsonStringLongConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            if (long.TryParse(reader.GetString(), out long value))
                return value;
            else
                return 0; // don't care

        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return 0;  // don't care 
        }

        return 0;  // don't care
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
