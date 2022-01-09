using common;
using common.extends;
using server.model;
using server.plugin;
using server.service.plugins.register.caching;

namespace server.service.plugins
{
    public class HeartPlugin : IPlugin
    {
        public HeartPlugin()
        {
        }

        public bool Execute(PluginParamWrap data)
        {
            return true;
        }
    }
}
