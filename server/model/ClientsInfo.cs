using MessagePack;
using ProtoBuf;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace server.model
{

    [ProtoContract, MessagePackObject]
    public class ClientsInfo
    {
        public ClientsInfo() { }

        [ProtoMember(1), Key(0)]
        public IEnumerable<ClientsClientInfo> Clients { get; set; }

    }

    [ProtoContract, MessagePackObject]
    public class ClientsClientInfo
    {
        [ProtoMember(1), Key(1)]
        public string Address { get; set; } = string.Empty;

        [ProtoMember(2), Key(2)]
        public int Port { get; set; } = 0;

        [ProtoMember(3), Key(3)]
        public string Name { get; set; } = string.Empty;

        [ProtoMember(4), Key(4)]
        public ulong Id { get; set; } = 0;

        [ProtoMember(5), Key(5)]
        public int TcpPort { get; set; } = 0;

        [ProtoMember(6), Key(6)]
        public string Mac { get; set; } = string.Empty;

        [ProtoIgnore, IgnoreMember]
        public IConnection TcpConnection { get; set; } = null;

        [ProtoIgnore, IgnoreMember]
        public IConnection UdpConnection { get; set; } = null;
    }
}
