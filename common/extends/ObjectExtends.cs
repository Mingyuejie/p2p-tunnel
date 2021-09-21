using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;

namespace common.extends
{
    public static class ObjectExtends
    {
        public static string ToJson(this object obj)
        {
            return JsonSerializer.Serialize(obj, options: new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All)
            });
        }
        public static T DeJson<T>(this string json)
        {
            return JsonSerializer.Deserialize<T>(json, options: new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
                PropertyNameCaseInsensitive = true
            });
        }

        public static string ToJsonSwifter(this object obj)
        {
            return Swifter.Json.JsonFormatter.SerializeObject(obj);
        }
        public static T DeJsonSwifter<T>(this string json)
        {
            return Swifter.Json.JsonFormatter.DeserializeObject<T>(json);
        }
    }
}
