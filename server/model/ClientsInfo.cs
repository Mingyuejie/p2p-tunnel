using MessagePack;
using System.Collections.Generic;

namespace server.model
{

    [ MessagePackObject]
    public class ClientsInfo
    {
        public ClientsInfo() { }

        [ Key(0)]
        public IEnumerable<ClientsClientInfo> Clients { get; set; }

    }

    [ MessagePackObject]
    public class ClientsClientInfo
    {
        [ Key(1)]
        public string Address { get; set; } = string.Empty;

        [ Key(2)]
        public int Port { get; set; } = 0;

        [ Key(3)]
        public string Name { get; set; } = string.Empty;

        [ Key(4)]
        public ulong Id { get; set; } = 0;

        [Key(5)]
        public int TcpPort { get; set; } = 0;

        [Key(6)]
        public string Mac { get; set; } = string.Empty;

        [ IgnoreMember]
        public IConnection TcpConnection { get; set; } = null;

        [ IgnoreMember]
        public IConnection UdpConnection { get; set; } = null;
    }
}
