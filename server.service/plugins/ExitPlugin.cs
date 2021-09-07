using common.cache;
using common.extends;
using server.model;
using server.models;
using server.plugin;

namespace server.service.plugins
{
    /// <summary>
    /// 退出插件
    /// </summary>
    public class ExitPlugin : IPlugin
    {
        public MessageTypes MsgType => MessageTypes.SERVER_EXIT;

        public void Excute(PluginExcuteModel data, ServerType serverType)
        {
            ExitModel model = data.Packet.Chunk.DeBytes<ExitModel>();
            ClientRegisterCache.Instance.Remove(model.Id);
        }
    }
}
