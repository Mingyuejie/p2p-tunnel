using MessagePack;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace server.model
{
    [ProtoContract, MessagePackObject]
    public class ServerTcpForwardRegisterModel
    {
        [ProtoMember(1), Key(1)]
        public ServerForwardWebModel[] Web { get; set; } = Array.Empty<ServerForwardWebModel>();
        [ProtoMember(2), Key(2)]
        public ServerTcpForwardTunnelModel[] Tunnel { get; set; } = Array.Empty<ServerTcpForwardTunnelModel>();

        [ProtoMember(3), Key(3), JsonIgnore]
        public long Id { get; set; } = 0;
    }
    [ProtoContract, MessagePackObject]
    public class ServerForwardWebModel
    {
        [ProtoMember(1), Key(1)]
        public int Port { get; set; } = 0;
        [ProtoMember(2), Key(2)]
        public Dictionary<string, ServerTcpForwardWebItem> Forwards { get; set; } = new Dictionary<string, ServerTcpForwardWebItem>();
    }
    [ProtoContract, MessagePackObject]
    public class ServerTcpForwardWebItem
    {
        [ProtoMember(2), Key(2)]
        public int TargetPort { get; set; } = 0;
        [ProtoMember(3), Key(3)]
        public string TargetIp { get; set; } = string.Empty;
    }
    [ProtoContract, MessagePackObject]
    public class ServerTcpForwardTunnelModel
    {
        [ProtoMember(1), Key(1)]
        public int Port { get; set; } = 0;
        [ProtoMember(2), Key(2)]
        public int TargetPort { get; set; } = 0;
        [ProtoMember(3), Key(3)]
        public string TargetIp { get; set; } = string.Empty;
    }

    [ProtoContract, MessagePackObject]
    public class ServerTcpForwardModel
    {
        public ServerTcpForwardModel() { }

        [ProtoMember(1), Key(1)]
        public long RequestId { get; set; } = 0;

        [ProtoMember(2), Key(2)]
        public byte[] Buffer { get; set; } = new byte[0];

        [ProtoMember(3, IsRequired = true), Key(3)]
        public ServerTcpForwardType Type { get; set; } = ServerTcpForwardType.REQUEST;

        [ProtoMember(4), Key(4)]
        public string TargetIp { get; set; } = string.Empty;

        [ProtoMember(5), Key(5)]
        public int TargetPort { get; set; } = 0;

        [ProtoMember(6, IsRequired = true), Key(6)]
        public ServerTcpForwardAliveTypes AliveType { get; set; } = ServerTcpForwardAliveTypes.WEB;

        [ProtoMember(7), Key(7)]
        public byte Compress { get; set; } = 0;
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum ServerTcpForwardAliveTypes : int
    {
        TUNNEL,
        //短连接
        WEB
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum ServerTcpForwardType
    {
        REQUEST, RESPONSE, FAIL, RESPONSE_END,CLOSE
    }
}
