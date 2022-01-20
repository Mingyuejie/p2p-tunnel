using common;
using server.model;
using System.Collections.Concurrent;
using System.Net;

namespace server.service.plugins.register.caching
{
    public class RegisterCacheModel
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

        public void UpdateUdpInfo(RegisterCacheUpdateUdpModel model)
        {
            if (model.Connection != null)
            {
                UdpConnection = model.Connection;
                UdpConnection.ConnectId = Id;
            }
        }
        public void UpdateTcpInfo(RegisterCacheUpdateTcpModel model)
        {
            if (model.Connection != null)
            {
                TcpConnection = model.Connection;
                TcpConnection.ConnectId = Id;
            }
        }

        private ConcurrentDictionary<string, TunnelRegisterCacheModel> tunnels = new ConcurrentDictionary<string, TunnelRegisterCacheModel>();
        public void AddTunnel(TunnelRegisterCacheModel model)
        {
            if (!TunnelExists(model))
            {
                tunnels.TryAdd(model.TunnelName, model);
            }
        }
        public bool TunnelExists(TunnelRegisterCacheModel model)
        {
            return tunnels.ContainsKey(model.TunnelName);
        }

        public bool GetTunnel(string name, out TunnelRegisterCacheModel model)
        {
            return tunnels.TryGetValue(name, out model);
        }
    }

    public class RegisterCacheUpdateUdpModel
    {
        public IConnection Connection { get; set; } = null;
    }
    public class RegisterCacheUpdateTcpModel
    {
        public IConnection Connection { get; set; } = null;
    }

    public class TunnelRegisterCacheModel
    {
        public string TunnelName { get; set; } = string.Empty;
        public int Port { get; set; } = 0;
        public int LocalPort { get; set; } = 0;
        public ServerType Servertype { get; set; }
        public bool IsDefault { get; set; } = false;
    }
}
