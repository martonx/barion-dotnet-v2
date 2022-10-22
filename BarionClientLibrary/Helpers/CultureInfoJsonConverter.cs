namespace BarionClientLibrary.Helpers;

public class CultureInfoJsonConverter : JsonConverter<CultureInfo>
{
    public override CultureInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetString());

    public override void Write(Utf8JsonWriter writer, CultureInfo typeValue, JsonSerializerOptions options) =>
        writer.WriteStringValue(typeValue.Name);
}
