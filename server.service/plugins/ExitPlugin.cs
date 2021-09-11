using common.extends;
using server.model;
using server.models;
using server.plugin;
using server.service.cache;

namespace server.service.plugins
{
    /// <summary>
    /// 退出插件
    /// </summary>
    public class ExitPlugin : IPlugin
    {
        private readonly IClientRegisterCache clientRegisterCache;
        public ExitPlugin(IClientRegisterCache clientRegisterCache)
        {
            this.clientRegisterCache = clientRegisterCache;
        }

        public MessageTypes MsgType => MessageTypes.SERVER_EXIT;

        public void Excute(PluginExcuteModel data, ServerType serverType)
        {
            ExitModel model = data.Packet.Chunk.DeBytes<ExitModel>();

            if (!clientRegisterCache.Verify(model.Id, data)) return;

            clientRegisterCache.Remove(model.Id);
        }
    }
}
