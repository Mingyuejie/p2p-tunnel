using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.model
{
    /// <summary>
    /// 打洞
    /// </summary>
    [ProtoContract]
    public class PunchHoleModel : IModelBase
    {
        public PunchHoleModel() { }

        /// <summary>
        /// 来源客户端id
        /// </summary>
        [ProtoMember(1)]
        public long Id { get; set; } = 0;

        /// <summary>
        /// 目标客户端id
        /// </summary>
        [ProtoMember(2)]
        public long ToId { get; set; } = 0;

        /// <summary>
        /// 消息类型
        /// </summary>
        [ProtoMember(3, IsRequired = true)]
        public MessageTypes MsgType { get; } = MessageTypes.PUNCH_HOLE;

        /// <summary>
        /// 数据
        /// </summary>
        [ProtoMember(4)]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 打洞类型，客户端根据不同的打洞类型做不同处理
        /// </summary>
        [ProtoMember(5)]
        public short PunchType { get; set; } = 0;

        /// <summary>
        /// 经过服务器的转发类型 决定原数据转发，还是重写为客户端数据
        /// </summary>
        [ProtoMember(6)]
        public PunchForwardTypes PunchForwardType { get; set; } = PunchForwardTypes.NOTIFY;

        /// <summary>
        /// 客户端自定义步骤
        /// </summary>
        [ProtoMember(7)]
        public short PunchStep { get; set; } = 0;
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum PunchForwardTypes
    {
        NOTIFY, //让服务器发送A信息给B
        FORWARD  //纯转发，A发啥就转给B啥
    }

    [ProtoContract]
    public class PunchHoleNotifyModel
    {
        public PunchHoleNotifyModel() { }

        /// <summary>
        /// 来源客户端id
        /// </summary>
        [ProtoMember(1)]
        public long Id { get; set; } = 0;

        /// <summary>
        /// 来源客户端名字
        /// </summary>
        [ProtoMember(2)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 来源客户端ip
        /// </summary>
        [ProtoMember(3)]
        public string Ip { get; set; } = string.Empty;

        /// <summary>
        /// 来源客户端端口
        /// </summary>
        [ProtoMember(4)]
        public int Port { get; set; } = 0;

        [ProtoMember(5)]
        public int TcpPort { get; set; } = 0;

        [ProtoMember(6)]
        public string LocalIps { get; set; } = string.Empty;

        [ProtoMember(7)]
        public int LocalTcpPort { get; set; } = 0;

        [ProtoMember(8)]
        public int LocalUdpPort { get; set; } = 0;

    }
}
