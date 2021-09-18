using common.extends;
using server.model;
using server.models;
using server.plugin;
using server.service.plugins.register.caching;

namespace server.service.plugins
{
    /// <summary>
    /// 退出插件
    /// </summary>
    public class ExitPlugin : IPlugin
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        public ExitPlugin(IClientRegisterCaching clientRegisterCache)
        {
            this.clientRegisterCache = clientRegisterCache;
        }

        public bool Excute(PluginParamWrap data)
        {
            ExitModel model = data.Wrap.Content.DeBytes<ExitModel>();

            if (!clientRegisterCache.Verify(model.Id, data)) return false;

            clientRegisterCache.Remove(model.Id);

            return true;
        }
    }
}
