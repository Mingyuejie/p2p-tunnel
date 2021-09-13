using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace server.model
{
    /// <summary>
    /// A为源客户端，B为目标客户端
    /// </summary>
    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum MessageTypes
    {
        Empty = 1,

        /// <summary>
        /// 心跳消息
        /// </summary>
        HEART = 2,

        /// <summary>
        /// 注册消息 客户端在服务器注册信息，方便别人找到
        /// </summary>
        SERVER_REGISTER = 3,
        /// <summary>
        /// 注册反馈消息,告诉客户端，已收到你的注册消息
        /// </summary>
        SERVER_REGISTER_RESULT = 4,
        /// <summary>
        /// 退出注册消息，客户端离开了
        /// </summary>
        SERVER_EXIT = 5,

        /// <summary>
        /// 获取客户端返回值
        /// </summary>
        SERVER_SEND_CLIENTS = 6,

        /// <summary>
        /// P2P  客户端之间的直接消息
        /// </summary>
        P2P = 7,

        /// <summary>
        /// 重启
        /// </summary>
        SERVER_RESET = 8,

        /// <summary>
        /// 打洞
        /// </summary>
        SERVER_PUNCH_HOLE = 9,

        /// <summary>
        /// 发送原数据包，用来做IP欺骗
        /// </summary>
        SERVER_RAW_PACKET = 10,
    }
}