//using System.Text.Json;

//namespace ui.Extensions
//{
//    public class SessionExtension
//    {
//    }
//}

using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace WebApp.Extensions;

public static class SessionExtensions
{
    private static readonly JsonSerializerOptions _json =
        new() { IncludeFields = true };   // сериализуем record-типы без set

    public static void Set<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value, _json));
    }

    public static T? Get<T>(this ISession session, string key)
    {
        var json = session.GetString(key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json, _json);
    }
}