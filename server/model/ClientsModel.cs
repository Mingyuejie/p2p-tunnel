using ProtoBuf;
using System.Collections.Generic;
using System.Net.Sockets;

namespace server.model
{

    [ProtoContract]
    public class ClientsModel
    {
        public ClientsModel() { }

        [ProtoMember(1)]
        public IEnumerable<ClientsClientModel> Clients { get; set; }

    }

    [ProtoContract]
    public class ClientsClientModel
    {
        [ProtoMember(1)]
        public string Address { get; set; } = string.Empty;

        [ProtoMember(2)]
        public int Port { get; set; } = 0;

        [ProtoMember(3)]
        public string Name { get; set; } = string.Empty;

        [ProtoMember(4)]
        public long Id { get; set; } = 0;

        [ProtoMember(5)]
        public int TcpPort { get; set; } = 0;

        [ProtoIgnore]
        public Socket TcpSocket { get; set; }
    }
}
