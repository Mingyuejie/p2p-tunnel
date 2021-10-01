using MessagePack;
using ProtoBuf;
using System.Collections.Generic;
using System.Net.Sockets;

namespace server.model
{

    [ProtoContract,MessagePackObject]
    public class ClientsModel
    {
        public ClientsModel() { }

        [ProtoMember(1),Key(0)]
        public IEnumerable<ClientsClientModel> Clients { get; set; }

    }

    [ProtoContract, MessagePackObject]
    public class ClientsClientModel
    {
        [ProtoMember(1),Key(1)]
        public string Address { get; set; } = string.Empty;

        [ProtoMember(2), Key(2)]
        public int Port { get; set; } = 0;

        [ProtoMember(3), Key(3)]
        public string Name { get; set; } = string.Empty;

        [ProtoMember(4), Key(4)]
        public long Id { get; set; } = 0;

        [ProtoMember(5), Key(5)]
        public int TcpPort { get; set; } = 0;

        [ProtoIgnore, IgnoreMember]
        public Socket TcpSocket { get; set; }
    }
}
