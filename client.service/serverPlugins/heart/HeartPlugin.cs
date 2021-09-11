﻿using common.extends;
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

        public MessageTypes MsgType => MessageTypes.HEART;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            HeartModel data = model.Packet.Chunk.DeBytes<HeartModel>();
            heartEventHandles.OnHeartMessage(new OnHeartEventArg
            {
                Packet = model,
                Data = data
            });
        }
    }
}
