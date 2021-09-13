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
        Empty,

        /// <summary>
        /// 心跳消息
        /// </summary>
        HEART,

        /// <summary>
        /// 注册消息 客户端在服务器注册信息，方便别人找到
        /// </summary>
        SERVER_REGISTER,
        /// <summary>
        /// 注册反馈消息,告诉客户端，已收到你的注册消息
        /// </summary>
        SERVER_REGISTER_RESULT,
        /// <summary>
        /// 退出注册消息，客户端离开了
        /// </summary>
        SERVER_EXIT,

        /// <summary>
        /// 获取客户端返回值
        /// </summary>
        SERVER_SEND_CLIENTS,

        /// <summary>
        /// UDP B准备 A连接
        /// 1,服务器收到 A->B  给B发个 step_1
        /// 2,B收到 step_1 
        ///     给A发个step_1_ack
        ///     给服务器发个 step_1_result
        /// 3，服务器收到 step_1_result 给A发个 step_2
        /// 4,A收到 step_2 给B发个 step_3
        /// 5,B收到 step_3 给A发个 step_4
        /// 
        /// TCP  A准备  B连接   在UDP收到step_4时给服务器发送TCP连接申请
        /// 1,服务器收到 A->B  给A发个 step_1
        /// 2,A收到 step_1 
        ///     给B发个低TLL 连接
        ///     给服务器发个 step_1_result
        /// 3，服务器收到 step_1_result 给B发个 step_2
        /// 4,B收到 step_2 正常TTL连接A(尝试十次) 连接成功则发个step_3  失败给服务器发送step_2_fail
        /// 5,A收到 step_3 给B发个 step_4
        /// </summary>
        SERVER_P2P,
        SERVER_P2P_REVERSE,
        P2P_STEP_1,
        P2P_STEP_1_ACK, //随便回应个消息
        SERVER_P2P_STEP_1_RESULT,
        P2P_STEP_2,
        SERVER_P2P_STEP_2_STOP,//中断连接
        SERVER_P2P_STEP_2_RETRY,//重试
        SERVER_P2P_STEP_2_FAIL,
        P2P_STEP_3,
        P2P_STEP_4,


        /// <summary>
        /// P2P  客户端之间的直接消息
        /// </summary>
        P2P,

        /// <summary>
        /// 重启
        /// </summary>
        SERVER_RESET,

        /// <summary>
        /// 打洞
        /// </summary>
        PUNCH_HOLE,
    }
}