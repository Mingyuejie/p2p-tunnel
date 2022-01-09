using common.extends;
using server.model;
using server.models;
using server.plugin;
using server.service.plugins.register.caching;
using System;

namespace server.service.plugins
{
    public class ExitPlugin : IPlugin
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        public ExitPlugin(IClientRegisterCaching clientRegisterCache)
        {
            this.clientRegisterCache = clientRegisterCache;
        }

        public bool Execute(PluginParamWrap data)
        {
            clientRegisterCache.Remove(data.Connection.ConnectId);
            return true;
        }
    }
}
