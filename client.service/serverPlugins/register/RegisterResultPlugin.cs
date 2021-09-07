using common.extends;
using server.model;
using server.plugin;

namespace client.service.serverPlugins.register
{
    public class RegisterResultPlugin : IPlugin
    {
        public MessageTypes MsgType => MessageTypes.SERVER_REGISTER_RESULT;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            RegisterResultModel res = model.Packet.Chunk.DeBytes<RegisterResultModel>();

            if (serverType == ServerType.UDP)
            {
                RegisterEventHandles.Instance.OnRegisterResult(new OnRegisterResultEventArg
                {
                    Data = res,
                    Packet = model
                });
            }
            else if (serverType == ServerType.TCP)
            {
                RegisterEventHandles.Instance.OnRegisterTcpResult(new OnRegisterResultEventArg
                {
                    Data = res,
                    Packet = model
                });
            }
        }
    }
}
