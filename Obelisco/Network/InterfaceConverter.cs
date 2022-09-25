using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Obelisco.Network;

public class InterfaceConverter<T> : JsonConverter<T>
    where T : class
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Utf8JsonReader readerClone = reader;
        if (readerClone.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        readerClone.Read();
        if (readerClone.TokenType != JsonTokenType.PropertyName)
        {
            throw new JsonException();
        }

        string propertyName = readerClone.GetString() ?? string.Empty;
        if (propertyName != "type")
        {
            throw new JsonException();
        }

        readerClone.Read();
        if (readerClone.TokenType != JsonTokenType.String)
            throw new JsonException();

        string typeName = $"Obelisco.Network.{readerClone.GetString()!}";
        Type entityType = Type.GetType(typeName) ?? throw new JsonException($"Fail to find type {typeName}");

        if (!typeof(T).IsAssignableFrom(entityType))
            throw new JsonException($"TypeName: {typeName}, target: {typeof(T).AssemblyQualifiedName}");

        var deserialized = JsonSerializer.Deserialize(ref reader, entityType, options);
        return (T)deserialized!;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case null:
                JsonSerializer.Serialize(writer, null, typeof(T), options);
                break;
            default:
                {
                    var type = value.GetType();
                    using var jsonDocument = JsonDocument.Parse(JsonSerializer.Serialize(value, type, options));
                    writer.WriteStartObject();
                    writer.WriteString("type", type.Name);

                    foreach (var element in jsonDocument.RootElement.EnumerateObject())
                    {
                        element.WriteTo(writer);
                    }

                    writer.WriteEndObject();
                    break;
                }
        }
    }
}
