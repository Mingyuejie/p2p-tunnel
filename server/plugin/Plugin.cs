using common;
using server.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace server.plugin
{
    public static class Plugin
    {
        public static readonly Dictionary<string, Tuple<object, MethodInfo>> plugins = new();

        public static long requestId = 0;
        public static ConcurrentDictionary<long, SendCacheModel> sends = new ConcurrentDictionary<long, SendCacheModel>();


        public static void LoadPlugin(Type type, object obj)
        {
            string path = type.Name.Replace("Plugin", "");
            foreach (var method in type.GetMethods())
            {
                string key = $"{path}/{method.Name}".ToLower();
                if (!plugins.ContainsKey(key))
                {
                    plugins.TryAdd(key, new Tuple<object, MethodInfo>(obj, method));
                }
            }
        }
    }

    public class SendCacheModel
    {
        public TaskCompletionSource<ServerResponeMessageWrap> Tcs { get; set; }
        public long Time { get; set; } = Helper.GetTimeStamp();
        public long RequestId { get; set; } = 0;
    }
}
