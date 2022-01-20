using MessagePack;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.model
{
    /// <summary>
    /// 打洞
    /// </summary>
    [ProtoContract, MessagePackObject]
    public class PunchHoleModel
    {
        public PunchHoleModel() { }


        [ProtoMember(1), Key(1)]
        public ulong FromId { get; set; } = 0;

        [ProtoMember(2), Key(2)]
        public ulong ToId { get; set; } = 0;

        /// <summary>
        /// 数据
        /// </summary>
        [ProtoMember(3), Key(3)]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 打洞类型，客户端根据不同的打洞类型做不同处理
        /// </summary>
        [ProtoMember(4), Key(4)]
        public byte PunchType { get; set; } = 0;

        /// <summary>
        /// 经过服务器的转发类型 决定原数据转发，还是重写为客户端数据
        /// </summary>
        [ProtoMember(5), Key(5)]
        public PunchForwardTypes PunchForwardType { get; set; } = PunchForwardTypes.NOTIFY;

        [ProtoMember(6), Key(6)]
        public string TunnelName { get; set; } = string.Empty;

        /// <summary>
        /// 客户端自定义步骤
        /// </summary>
        [ProtoMember(7), Key(7)]
        public byte PunchStep { get; set; } = 0;
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum PunchForwardTypes : byte
    {
        [Description("通知A的数据给B")]
        NOTIFY,
        [Description("原样转发")]
        FORWARD
    }

    [ProtoContract, MessagePackObject]
    public class PunchHoleNotifyModel
    {
        public PunchHoleNotifyModel() { }

        [ProtoMember(1), Key(1)]
        public string Ip { get; set; } = string.Empty;
        [ProtoMember(2), Key(2)]
        public int Port { get; set; } = 0;

        [ProtoMember(3), Key(3)]
        public string LocalIps { get; set; } = string.Empty;
        [ProtoMember(4), Key(4)]
        public int LocalPort { get; set; } = 0;

        [ProtoMember(5), Key(5)]
        public bool IsDefault { get; set; } = false;
    }
}
