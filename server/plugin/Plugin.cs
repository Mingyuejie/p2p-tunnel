using server.model;
using System.Collections.Generic;

namespace server.plugin
{
    public static class Plugin
    {
        public static Dictionary<MessageTypes, IPlugin> plugins = new Dictionary<MessageTypes, IPlugin>();

        public static void LoadPlugin(IPlugin plugin)
        {
            if (!plugins.ContainsKey(plugin.MsgType))
            {
                plugins.Add(plugin.MsgType, plugin);
            }
        }
    }
}
