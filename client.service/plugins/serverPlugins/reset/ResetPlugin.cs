using client.service.plugins.serverPlugins.register;
using server.model;
using server.plugin;
using System.Threading.Tasks;

namespace client.service.plugins.serverPlugins.reset
{
    public class ResetPlugin : IPlugin
    {
        private readonly RegisterHelper registerHelper;
        public ResetPlugin(RegisterHelper registerHelper)
        {
            this.registerHelper = registerHelper;
        }

        public async Task Execute(PluginParamWrap data)
        {
            await registerHelper.Register();
        }
    }
}
