using common;
using common.extends;
using ProtoBuf;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;

namespace client.plugins.serverPlugins.clients
{

    /// <summary>
    /// 客户端信息
    /// </summary>
    [ProtoContract, Serializable]
    public class ClientInfo
    {
        [ProtoMember(1)]
        public bool Connecting { get; set; } = false;
        [ProtoMember(2)]
        public bool TcpConnecting { get; set; } = false;
        [ProtoMember(3)]
        public bool Connected { get; set; } = false;
        [ProtoMember(4)]
        public bool TcpConnected { get; set; } = false;

        [ProtoIgnore, JsonIgnore]
        public Socket Socket { get; set; } = null;

        [ProtoIgnore, JsonIgnore]
        public IPEndPoint Address { get; set; } = null;
        [ProtoMember(5)]
        public int Port { get; set; } = 0;
        [ProtoMember(6)]
        public int TcpPort { get; set; } = 0;
        [ProtoMember(7)]
        public string Name { get; set; } = string.Empty;
        [ProtoMember(8)]
        public string Mac { get; set; } = string.Empty;
        [ProtoMember(9)]
        public string Ip { get; set; } = string.Empty;
        [ProtoMember(10)]
        public long Id { get; set; } = 0;
        [ProtoMember(11)]
        public long LastTime { get; set; } = 0;
        [ProtoMember(12)]
        public long TcpLastTime { get; set; } = 0;

        [ProtoMember(13)]
        public long IpAddress { get; set; } = 0;

        public bool IsTimeout()
        {
            return (LastTime > 0 && Helper.GetTimeStamp() - LastTime > 20000);
        }

        public bool IsTcpTimeout()
        {
            return (TcpLastTime > 0 && Helper.GetTimeStamp() - TcpLastTime > 20000);
        }

        public void UpdateLastTime()
        {
            LastTime = Helper.GetTimeStamp();
        }

        public void UpdateTcpLastTime()
        {
            TcpLastTime = Helper.GetTimeStamp();
        }

        public void Offline()
        {
            Connecting = false;
            Connected = false;
            LastTime = 0;
        }
        public void Online(IPEndPoint address)
        {
            Connected = true;
            LastTime = Helper.GetTimeStamp();
            Address = address;
            Connecting = false;
        }
        public void OfflineTcp()
        {
            TcpConnecting = false;
            TcpConnected = false;
            TcpLastTime = 0;
            if (Socket != null)
            {
                Socket.SafeClose();
            }
        }
        public void OnlineTcp(Socket socket)
        {
            TcpConnected = true;
            TcpConnecting = false;
            TcpLastTime = Helper.GetTimeStamp();
            Socket = socket;
            Ip = IPEndPoint.Parse(socket.RemoteEndPoint.ToString()).Address.ToString();
            IpAddress = ((IPEndPoint)socket.RemoteEndPoint).ToInt64();
        }
    }
}
