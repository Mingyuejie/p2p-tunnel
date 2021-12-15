using client.service.plugins.serverPlugins.register;
using server.model;
using server.plugin;

namespace client.service.plugins.serverPlugins.reset
{
    public class ResetPlugin : IPlugin
    {
        private readonly RegisterHelper registerHelper;
        public ResetPlugin(RegisterHelper registerHelper)
        {
            this.registerHelper = registerHelper;
        }

        public void Excute(PluginParamWrap data)
        {
            registerHelper.Start().Wait();
        }
    }
}
