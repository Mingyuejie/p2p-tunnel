using common.extends;
using MessagePack;
using ProtoBuf;
using server;
using server.model;
using System;
using System.Text.Json.Serialization;

namespace client.messengers.clients
{
    /// <summary>
    /// 客户端信息
    /// </summary>
    [ProtoContract, Serializable, MessagePackObject]
    public class ClientInfo
    {
        [ProtoMember(1), Key(1)]
        public bool UdpConnecting { get; set; } = false;
        [ProtoMember(2), Key(2)]
        public bool TcpConnecting { get; set; } = false;
        [ProtoMember(3), Key(3)]
        public bool UdpConnected { get => UdpConnection != null; }
        [ProtoMember(4), Key(4)]
        public bool TcpConnected { get => TcpConnection != null; }

        [ProtoMember(5), Key(5)]
        public string Name { get; set; } = string.Empty;
        [ProtoMember(6), Key(6)]
        public string Mac { get; set; } = string.Empty;
        [ProtoMember(7), Key(7)]
        public string Ip { get; set; } = string.Empty;
        [ProtoMember(8), Key(8)]
        public ulong Id { get; set; } = 0;

        [ProtoIgnore, JsonIgnore, IgnoreMember]
        public IConnection TcpConnection { get; set; } = null;
        [ProtoIgnore, JsonIgnore, IgnoreMember]
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
                TcpConnection.TcpSocket.SafeClose();
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
                Ip = connection.UdpAddress.Address.ToString();
            }
            else
            {
                TcpConnection = connection;
                Ip = connection.TcpAddress.Address.ToString();
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
