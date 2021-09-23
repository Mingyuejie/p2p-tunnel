using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

        public static void TryUpdateModel<T>(this object _object, T fromModel)
        {
            if (_object == null)
            {
                return;
            }
            Type obj = _object.GetType();
            Type fromObj = fromModel.GetType();

            foreach (PropertyInfo item in obj.GetProperties())
            {
                if (fromObj.GetProperty(item.Name) != null)
                {
                    try
                    {
                        item.SetValue(_object, Convert.ChangeType(fromObj.GetProperty(item.Name).GetValue(fromModel), (Nullable.GetUnderlyingType(item.PropertyType) ?? item.PropertyType)));
                    }
                    catch (Exception)
                    {
                    }
                }
            };
        }

        public static void FormatObjectAttr(this object _object)
        {
            foreach (PropertyInfo item in (_object.GetType().GetProperties()))
            {
                if (item.PropertyType == typeof(string))
                {
                    if (string.IsNullOrWhiteSpace((item.GetValue(_object, null) as string)))
                    {
                        item.SetValue(_object, string.Empty, null);
                    }
                }
                else if (item.PropertyType == typeof(DateTime))
                {
                    if (item.GetValue(_object, null) == null)
                    {
                        item.SetValue(_object, DateTime.Now, null);
                    }
                }
            }
        }

        /// <summary>
        /// Swifter.Json
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        //public static string ToJsonSwifter(this object obj)
        //{
        //    return Swifter.Json.JsonFormatter.SerializeObject(obj);
        //}
        /// <summary>
        /// Swifter.Json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        //public static T DeJsonSwifter<T>(this string json)
        //{
        //    return Swifter.Json.JsonFormatter.DeserializeObject<T>(json);
        //}
    }
}
