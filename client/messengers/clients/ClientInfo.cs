using common.extends;
using MessagePack;
using server;
using server.model;
using System;
using System.Text.Json.Serialization;

namespace client.messengers.clients
{
    /// <summary>
    /// 客户端信息
    /// </summary>
    [Serializable, MessagePackObject]
    public class ClientInfo
    {
        [Key(1)]
        public bool UdpConnecting { get; set; } = false;
        [Key(2)]
        public bool TcpConnecting { get; set; } = false;
        [Key(3)]
        public bool UdpConnected { get => UdpConnection != null; }
        [Key(4)]
        public bool TcpConnected { get => TcpConnection != null; }

        [Key(5)]
        public string Name { get; set; } = string.Empty;
        [Key(6)]
        public string Mac { get; set; } = string.Empty;
        [Key(7)]
        public string Ip { get; set; } = string.Empty;
        [Key(8)]
        public ulong Id { get; set; } = 0;

        [JsonIgnore, IgnoreMember]
        public IConnection TcpConnection { get; set; } = null;
        [JsonIgnore, IgnoreMember]
        public IConnection UdpConnection { get; set; } = null;

        public void Offline()
        {
            UdpConnecting = false;
            UdpConnection = null;
        }
        public void OfflineTcp()
        {
            TcpConnecting = false;
            if (TcpConnection != null)
            {
                TcpConnection.Disponse();
            }
            TcpConnection = null;
        }

        public void Offline(ServerType serverType)
        {
            if (serverType == ServerType.UDP)
            {
                Offline();
            }
            else
            {
                OfflineTcp();
            }
        }

        public void Online(IConnection connection)
        {
            if (connection.ServerType == ServerType.UDP)
            {
                UdpConnection = connection;
                Ip = connection.Address.Address.ToString();
            }
            else
            {
                TcpConnection = connection;
                Ip = connection.Address.Address.ToString();
            }
            Connecting(false, connection.ServerType);
        }

        public void Connecting(bool val, IConnection connection)
        {
            Connecting(val, connection.ServerType);
        }

        public void Connecting(bool val, ServerType serverType)
        {
            if (serverType == ServerType.UDP)
            {
                UdpConnecting = val;
            }
            else
            {
                TcpConnecting = val;
            }
        }
    }
}
