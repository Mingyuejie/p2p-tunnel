using common.extends;
using server.model;
using server.plugin;

namespace client.service.serverPlugins.heart
{
    /// <summary>
    /// 心跳包
    /// </summary>
    public class HeartPlugin : IPlugin
    {
        private readonly HeartEventHandles heartEventHandles;
        public HeartPlugin(HeartEventHandles heartEventHandles)
        {
            this.heartEventHandles = heartEventHandles;
        }

        public void Excute(PluginParamWrap model)
        {
            HeartModel data = model.Wrap.Content.DeBytes<HeartModel>();
            heartEventHandles.OnHeartMessage(new OnHeartEventArg
            {
                Packet = model,
                Data = data
            });
        }
    }
}
