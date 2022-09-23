using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Obelisco;

public class TransactionConverter: JsonConverter<Transaction>
{
	public override Transaction? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

		string typeName = readerClone.GetString()!;
		Type entityType = typeName switch 
		{
			"poll" => typeof(PollTransaction),
			// TODO: Add others.	
		};

		if (!typeof(Transaction).IsAssignableFrom(entityType))
			throw new JsonException($"TypeName: {typeName}, target: {typeof(Transaction).AssemblyQualifiedName}");

		var deserialized = JsonSerializer.Deserialize(ref reader, entityType, options);
		return (Transaction)deserialized!;
	}

	public override void Write(Utf8JsonWriter writer, Transaction value, JsonSerializerOptions options)
	{
		switch (value)
		{
			case null:
				JsonSerializer.Serialize(writer, null, typeof(Transaction), options);
				break;
			default:
				{
					var type = value.GetType();
					using var jsonDocument = JsonDocument.Parse(JsonSerializer.Serialize(value, type, options));
					writer.WriteStartObject();
					
					var typeName = value switch 
					{
						PollTransaction => "poll",
						// TODO: Add others.
					};
					
					writer.WriteString("type", typeName);

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