﻿using System.Net;
using System.Net.Sockets;

namespace server.service.plugins.register.caching
{
    public class RegisterCacheModel
    {
        public IConnection TcpConnection { get; set; } = null;
        public IConnection UdpConnection { get; set; } = null;

        public int LocalUdpPort { get; set; } = 0;
        public int LocalTcpPort { get; set; } = 0;

        public string Name { get; set; } = string.Empty;

        public ulong Id { get; set; } = 0;

        public string GroupId { get; set; } = string.Empty;
        public string OriginGroupId { get; set; } = string.Empty;

        public long LastTime { get; set; }

        public string LocalIps { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;

    }

    public class RegisterCacheUpdateModel
    {
        public ulong Id { get; set; } = 0;
        public IConnection TcpConnection { get; set; } = null;
        public string GroupId { get; set; } = string.Empty;
        public int LocalTcpPort { get; set; } = 0;
    }
}
