using common;
using common.extends;
using MessagePack;
using ProtoBuf;
using server;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;

namespace client.plugins.serverPlugins.clients
{

    /// <summary>
    /// 客户端信息
    /// </summary>
    [ProtoContract, Serializable, MessagePackObject]
    public class ClientInfo
    {
        [ProtoMember(1), Key(1)]
        public bool Connecting { get; set; } = false;
        [ProtoMember(2), Key(2)]
        public bool TcpConnecting { get; set; } = false;
        [ProtoMember(3), Key(3)]
        public bool Connected { get; set; } = false;
        [ProtoMember(4), Key(4)]
        public bool TcpConnected { get; set; } = false;

        [ProtoIgnore, JsonIgnore, IgnoreMember]
        public IConnection TcpConnection { get; set; } = null;

        [ProtoIgnore, JsonIgnore, IgnoreMember]
        public IConnection UdpConnection { get; set; } = null;

        [ProtoMember(5), Key(5)]
        public string Name { get; set; } = string.Empty;
        [ProtoMember(6), Key(6)]
        public string Mac { get; set; } = string.Empty;
        [ProtoMember(7), Key(7)]
        public string Ip { get; set; } = string.Empty;
        [ProtoMember(8), Key(8)]
        public ulong Id { get; set; } = 0;

        public void Offline()
        {
            Connecting = false;
            Connected = false;
            UdpConnection = null;
        }
        public void OfflineTcp()
        {
            TcpConnecting = false;
            TcpConnected = false;
            if (TcpConnection != null)
            {
                TcpConnection.TcpSocket.SafeClose();
            }
            TcpConnection = null;
        }

        public void Offline(IConnection connection)
        {
            if (connection.ServerType == server.model.ServerType.UDP)
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
            if (connection.ServerType == server.model.ServerType.UDP)
            {
                Connected = true;
                UdpConnection = connection;
                Connecting = false;
                Ip = connection.UdpAddress.Address.ToString();
            }
            else
            {
                TcpConnected = true;
                TcpConnecting = false;
                TcpConnection = connection;
                Ip = connection.TcpAddress.Address.ToString();
            }
        }

    }
}
