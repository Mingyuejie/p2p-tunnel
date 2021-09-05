using ProtoBuf;
using System;

namespace client.service.p2pPlugins
{
    /// <summary>
    /// p2p消息插件，用于区别不同的消息类型
    /// </summary>
    public interface IP2PMessagePlugin
    {
        P2PDataMessageTypes Type { get; }

        void Excute(OnP2PTcpMessageArg arg);
    }

    /// <summary>
    /// p2p消息类型
    /// </summary>
    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum P2PDataMessageTypes
    {
        /// <summary>
        /// 请求
        /// </summary>
        REQUEST, 
        /// <summary>
        /// TCO转发
        /// </summary>
        TCP_FORWARD, 
        /// <summary>
        /// 文件服务器
        /// </summary>
        FILE_SERVER
    }

    public interface IP2PMessageBase
    {
        P2PDataMessageTypes Type { get; }
    }
}
