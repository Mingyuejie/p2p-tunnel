using client.plugins.serverPlugins.clients;
using common.extends;
using server;
using server.model;
using server.plugin;
using server.plugins.register.caching;

namespace client.service.plugins.serverPlugins.heart
{
    /// <summary>
    /// 心跳包
    /// </summary>
    public class HeartPlugin : IPlugin
    {
        public HeartPlugin()
        {
        }

        public bool Execute(IConnection connection)
        {
            return true;
        }
    }
}
