using client.service.serverPlugins.register;
using common;
using common.extends;
using server.model;
using server.plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.serverPlugins.reset
{
    public class ResetPlugin : IPlugin
    {
        private readonly RegisterHelper registerHelper;
        public ResetPlugin(RegisterHelper registerHelper)
        {
            this.registerHelper = registerHelper;
        }

        public MessageTypes MsgType => MessageTypes.SERVER_RESET;

        public void Excute(PluginExcuteModel data, ServerType serverType)
        {
            //MessageResetModel model = data.Packet.Chunk.DeBytes<MessageResetModel>();
            _ = registerHelper.Start();
        }
    }
}
