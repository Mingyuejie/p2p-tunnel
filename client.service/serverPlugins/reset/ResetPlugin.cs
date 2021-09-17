﻿using client.service.serverPlugins.register;
using server.model;
using server.plugin;

namespace client.service.serverPlugins.reset
{
    public class ResetPlugin : IPlugin
    {
        private readonly RegisterHelper registerHelper;
        public ResetPlugin(RegisterHelper registerHelper)
        {
            this.registerHelper = registerHelper;
        }

        public void Excute(PluginParamWrap data)
        {
            //MessageResetModel model = data.Packet.Chunk.DeBytes<MessageResetModel>();
            _ = registerHelper.Start();
        }
    }
}
