using common.extends;
using server.model;
using server.plugin;
using server.service.plugins.register.caching;

namespace server.service.plugins
{
    public class HeartPlugin : IPlugin
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        public HeartPlugin(IClientRegisterCaching clientRegisterCache)
        {
            this.clientRegisterCache = clientRegisterCache;
        }

        public HeartModel Excute(PluginParamWrap data)
        {
            HeartModel model = data.Wrap.Content.DeBytes<HeartModel>();

            if (clientRegisterCache.Verify(model.SourceId, data))
            {
                clientRegisterCache.UpdateTime(model.SourceId);
            };
            return new HeartModel { TargetId = model.SourceId, SourceId = -1 };
        }
    }
}
