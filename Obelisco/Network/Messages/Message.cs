using System.Text.Json.Serialization;

namespace Obelisco.Network;

[JsonConverterAttribute(typeof(InterfaceConverter<Message>))]
public interface Message { }
