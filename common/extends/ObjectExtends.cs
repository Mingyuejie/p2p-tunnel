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
        /// <summary>
        /// System.Text.Json
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToJson(this object obj)
        {
            return JsonSerializer.Serialize(obj, options: new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All)
            });
        }
        /// <summary>
        /// System.Text.Json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T DeJson<T>(this string json)
        {
            return JsonSerializer.Deserialize<T>(json, options: new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
                PropertyNameCaseInsensitive = true
            });
        }

        /// <summary>
        /// Swifter.Json
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToJsonSwifter(this object obj)
        {
            return Swifter.Json.JsonFormatter.SerializeObject(obj);
        }
        /// <summary>
        /// Swifter.Json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T DeJsonSwifter<T>(this string json)
        {
            return Swifter.Json.JsonFormatter.DeserializeObject<T>(json);
        }
    }
}
