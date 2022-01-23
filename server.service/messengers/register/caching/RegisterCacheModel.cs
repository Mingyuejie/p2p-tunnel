using common;
using server.model;
using System.Collections.Concurrent;
using System.Net;

namespace server.service.messengers.register.caching
{
    public class RegisterCacheInfo
    {
        public IConnection TcpConnection { get; private set; } = null;
        public IConnection UdpConnection { get; private set; } = null;

        public ulong Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;

        public string GroupId { get; set; } = string.Empty;
        public string OriginGroupId { get; set; } = string.Empty;

        public long LastTime { get; set; }

        public string LocalIps { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;

        public void UpdateUdpInfo(UpdateUdpParamsInfo model)
        {
            if (model.Connection != null)
            {
                UdpConnection = model.Connection;
                UdpConnection.ConnectId = Id;
            }
        }
        public void UpdateTcpInfo(UpdateTcpParamsInfo model)
        {
            if (model.Connection != null)
            {
                TcpConnection = model.Connection;
                TcpConnection.ConnectId = Id;
            }
        }

        private ConcurrentDictionary<string, TunnelRegisterCacheInfo> tunnels = new ConcurrentDictionary<string, TunnelRegisterCacheInfo>();
        public void AddTunnel(TunnelRegisterCacheInfo model)
        {
            if (!TunnelExists(model.TunnelName))
            {
                tunnels.TryAdd(model.TunnelName, model);
            }
        }
        public bool TunnelExists(string tunnelName)
        {
            return tunnels.ContainsKey(tunnelName);
        }
        public bool GetTunnel(string tunnelName, out TunnelRegisterCacheInfo model)
        {
            return tunnels.TryGetValue(tunnelName, out model);
        }
    }

    public class UpdateUdpParamsInfo
    {
        public IConnection Connection { get; set; } = null;
    }
    public class UpdateTcpParamsInfo
    {
        public IConnection Connection { get; set; } = null;
    }

    public class TunnelRegisterCacheInfo
    {
        public string TunnelName { get; set; } = string.Empty;
        public int Port { get; set; } = 0;
        public int LocalPort { get; set; } = 0;
        public ServerType Servertype { get; set; }
        public bool IsDefault { get; set; } = false;
    }
}
