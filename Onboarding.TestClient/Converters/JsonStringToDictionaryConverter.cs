using System.Text.Json;
using System.Text.Json.Serialization;

namespace Onboarding.TestClient.Converters;

public class JsonStringToDictionaryConverter : JsonConverter<Dictionary<string, object>?>
{
    public override Dictionary<string, object>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var jsonString = reader.GetString();
            
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString, options);
            }
            catch
            {
                return null;
            }
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);
        }

        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, object>? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value, options);
    }
}
