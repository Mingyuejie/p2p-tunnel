using client.plugins.serverPlugins.clients;
using ProtoBuf;
using System;

namespace client.service.tcpforward
{
    [ProtoContract]
    public class TcpForwardModel
    {
        public TcpForwardModel() { }

        [ProtoMember(1)]
        public long RequestId { get; set; } = 0;

        [ProtoMember(2)]
        public byte[] Buffer { get; set; } = new byte[0];

        [ProtoMember(3, IsRequired = true)]
        public TcpForwardType Type { get; set; } = TcpForwardType.REQUEST;

        [ProtoMember(4)]
        public string TargetIp { get; set; } = string.Empty;

        [ProtoMember(5)]
        public int TargetPort { get; set; } = 0;

        [ProtoMember(6, IsRequired = true)]
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.UNALIVE;

        [ProtoMember(7)]
        public byte Compress { get; set; } = 0;
    }


    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum TcpForwardType
    {
        REQUEST, RESPONSE, FAIL, RESPONSE_END
    }

    [ProtoContract]
    public class TcpForwardRecordBaseModel
    {
        [ProtoMember(1)]
        public int ID { get; set; } = 0;
        [ProtoMember(2)]
        public string SourceIp { get; set; } = "0.0.0.0";
        [ProtoMember(3)]
        public int SourcePort { get; set; } = 8080;
        [ProtoMember(4)]
        public string TargetName { get; set; } = string.Empty;
        [ProtoMember(5)]
        public string TargetIp { get; set; } = "127.0.0.1";
        [ProtoMember(6)]
        public int TargetPort { get; set; } = 8080;
        [ProtoMember(7)]
        public bool Listening { get; set; } = false;
        [ProtoMember(8)]
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.UNALIVE;

        [ProtoMember(9)]
        public string Desc { get; set; } = "";

        [ProtoMember(10)]
        public bool Editable { get; set; } = true;

        [ProtoMember(11)]
        public string Group { get; set; } = "";
    }

    public class TcpForwardRecordModel : TcpForwardRecordBaseModel
    {
        public ClientInfo Client { get; set; }
    }


    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum TcpForwardAliveTypes : int
    {
        //长连接
        ALIVE,
        //短连接
        UNALIVE
    }
}
