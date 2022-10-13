﻿namespace BarionClientLibrary.Helpers;

internal class CultureInfoJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(CultureInfo).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonToken.String)
            throw new JsonSerializationException(FormatErrorMessage(reader, $"Unexpected token {reader.TokenType} when parsing 'System.Globalization.CultureInfo'."));

        try
        {
            var value = reader.Value.ToString();
            return new CultureInfo(value);
        }
        catch (Exception ex)
        {
            throw new JsonSerializationException(FormatErrorMessage(reader, $"Error converting value \"{reader.Value}\" to type 'System.Globalization.CultureInfo'."), ex);
        }
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        var cultureInfo = value as CultureInfo;

        writer.WriteValue(cultureInfo.Name);
    }

    private static string FormatErrorMessage(JsonReader reader, string message)
    {
        var lineInfo = reader as IJsonLineInfo;

        return $"{message} Path '{reader.Path}', line {lineInfo.LineNumber}, position {lineInfo.LinePosition}.";
    }
}
