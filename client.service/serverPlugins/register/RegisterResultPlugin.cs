using common.extends;
using server.model;
using server.plugin;

namespace client.service.serverPlugins.register
{
    public class RegisterResultPlugin : IPlugin
    {
        private readonly RegisterEventHandles registerEventHandles;
        public RegisterResultPlugin(RegisterEventHandles registerEventHandles)
        {
            this.registerEventHandles = registerEventHandles;
        }

        public MessageTypes MsgType => MessageTypes.SERVER_REGISTER_RESULT;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            RegisterResultModel res = model.Packet.Chunk.DeBytes<RegisterResultModel>();

            if (serverType == ServerType.UDP)
            {
                registerEventHandles.OnRegisterResult(new OnRegisterResultEventArg
                {
                    Data = res,
                    Packet = model
                });
            }
            else if (serverType == ServerType.TCP)
            {
                registerEventHandles.OnRegisterTcpResult(new OnRegisterResultEventArg
                {
                    Data = res,
                    Packet = model
                });
            }
        }
    }
}
