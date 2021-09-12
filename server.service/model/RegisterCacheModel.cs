using System.Net;
using System.Net.Sockets;

namespace server.service.model
{
    public class RegisterCacheModel
    {
        public IPEndPoint Address { get; set; } = null;

        public Socket TcpSocket { get; set; } = null;
        public int TcpPort { get; set; } = 0;

        public int LocalUdpPort { get; set; } = 0;
        public int LocalTcpPort { get; set; } = 0;

        public string Name { get; set; } = string.Empty;

        public long Id { get; set; } = 0;

        public string GroupId { get; set; } = string.Empty;
        public string OriginGroupId { get; set; } = string.Empty;

        public long LastTime { get; set; }

        public string LocalIps { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;

    }
}
