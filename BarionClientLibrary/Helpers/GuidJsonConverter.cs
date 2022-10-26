namespace BarionClientLibrary.Helpers;

public class GuidJsonConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetString());

    public override void Write(Utf8JsonWriter writer, Guid typeValue, JsonSerializerOptions options) =>
        writer.WriteStringValue(typeValue.ToString());
}
