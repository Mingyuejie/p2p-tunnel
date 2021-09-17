using common.extends;
using server.model;
using server.plugin;
using server.service.cache;
using System.Threading.Tasks;

namespace server.service.plugins
{
    public class HeartPlugin : IPlugin
    {
        private readonly IClientRegisterCache clientRegisterCache;
        public HeartPlugin(IClientRegisterCache clientRegisterCache)
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
