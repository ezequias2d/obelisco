using System.Text.Json;

namespace Obelisco;

internal static class Json
{
    private static readonly JsonSerializerOptions Options;
    static Json()
    {
        Options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public static string Serialize<T>(T? value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    public static T? Deserialize<T>(string value)
    {
        return JsonSerializer.Deserialize<T>(value, Options);
    }
}