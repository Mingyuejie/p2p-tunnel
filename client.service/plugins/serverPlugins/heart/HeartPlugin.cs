using common.extends;
using server.model;
using server.plugin;

namespace client.service.plugins.serverPlugins.heart
{
    /// <summary>
    /// 心跳包
    /// </summary>
    public class HeartPlugin : IPlugin
    {
        private readonly HeartMessageHelper heartEventHandles;
        public HeartPlugin(HeartMessageHelper heartEventHandles)
        {
            this.heartEventHandles = heartEventHandles;
        }

        public void Excute(PluginParamWrap model)
        {

            HeartModel data = model.Wrap.Memory.DeBytes<HeartModel>();
            heartEventHandles.OnHeart.Push(new OnHeartEventArg
            {
                Packet = model,
                Data = data
            });
        }
    }
}
