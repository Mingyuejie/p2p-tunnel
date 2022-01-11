using common.extends;
using server.model;
using server.models;
using server.plugin;
using server.service.plugins.register.caching;
using System;
using System.Threading.Tasks;

namespace server.service.plugins
{
    public class ExitPlugin : IPlugin
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        public ExitPlugin(IClientRegisterCaching clientRegisterCache)
        {
            this.clientRegisterCache = clientRegisterCache;
        }

        public async Task<bool> Execute(PluginParamWrap data)
        {
            return await clientRegisterCache.Remove(data.Connection.ConnectId);
        }
    }
}
