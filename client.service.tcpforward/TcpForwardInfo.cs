using client.messengers.clients;
using MessagePack;
using ProtoBuf;
using System;
using System.ComponentModel;

namespace client.service.tcpforward
{
    [ProtoContract, MessagePackObject]
    public class TcpForwardInfo
    {
        public TcpForwardInfo() { }

        [ProtoMember(1),Key(1)]
        public ulong RequestId { get; set; } = 0;

        [ProtoMember(2), Key(2)]
        public byte[] Buffer { get; set; } = Array.Empty<byte>();

        [ProtoMember(3, IsRequired = true), Key(3)]
        public TcpForwardTypes Type { get; set; } = TcpForwardTypes.REQUEST;

        [ProtoMember(4), Key(4)]
        public string TargetIp { get; set; } = string.Empty;

        [ProtoMember(5), Key(5)]
        public int TargetPort { get; set; } = 0;

        [ProtoMember(6, IsRequired = true), Key(6)]
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.WEB;

        [ProtoMember(7), Key(7)]
        public byte Compress { get; set; } = 0;

        [ProtoMember(8), Key(8)]
        public ulong FromID { get; set; } = 0;
    }


    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum TcpForwardTypes:byte
    {
        [Description("请求")]
        REQUEST,
        [Description("回复")]
        RESPONSE,
        [Description("失败")]
        FAIL,
        [Description("关闭")]
        CLOSE
    }

    [ProtoContract, MessagePackObject]
    public class TcpForwardRecordInfoBase
    {
        [ProtoMember(1),Key(1)]
        public int ID { get; set; } = 0;
        [ProtoMember(2), Key(2)]
        public string SourceIp { get; set; } = "0.0.0.0";
        [ProtoMember(3), Key(3)]
        public int SourcePort { get; set; } = 8080;
        [ProtoMember(4), Key(4)]
        public string TargetName { get; set; } = string.Empty;
        [ProtoMember(5), Key(5)]
        public string TargetIp { get; set; } = "127.0.0.1";
        [ProtoMember(6), Key(6)]
        public int TargetPort { get; set; } = 8080;
        [ProtoMember(7), Key(7)]
        public bool Listening { get; set; } = false;
        [ProtoMember(8), Key(8)]
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.WEB;

        [ProtoMember(9)]
        public string Desc { get; set; } = "";

        [ProtoMember(10)]
        public bool Editable { get; set; } = true;

        [ProtoMember(11)]
        public string Group { get; set; } = "";
    }

    public class TcpForwardRecordInfo : TcpForwardRecordInfoBase
    {
        public ClientInfo Client { get; set; }
    }


    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum TcpForwardAliveTypes : byte
    {
        [Description("长连接")]
        TUNNEL,
        [Description("短连接")]
        WEB
    }
}
