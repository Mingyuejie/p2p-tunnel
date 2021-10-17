using common;
using common.extends;
using MessagePack;
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
        public Socket Socket { get; set; } = null;

        [ProtoIgnore, JsonIgnore, IgnoreMember]
        public IPEndPoint Address { get; set; } = null;
        [ProtoMember(5), Key(5)]
        public int Port { get; set; } = 0;
        [ProtoMember(6), Key(6)]
        public int TcpPort { get; set; } = 0;
        [ProtoMember(7), Key(7)]
        public string Name { get; set; } = string.Empty;
        [ProtoMember(8), Key(8)]
        public string Mac { get; set; } = string.Empty;
        [ProtoMember(9), Key(9)]
        public string Ip { get; set; } = string.Empty;
        [ProtoMember(10), Key(10)]
        public long Id { get; set; } = 0;
        [ProtoMember(11), Key(11)]
        public long LastTime { get; set; } = 0;
        [ProtoMember(12), Key(12)]
        public long TcpLastTime { get; set; } = 0;

        [ProtoMember(13), Key(13)]
        public long SelfId { get; set; } = 0;

        public bool IsNeedHeart()
        {
            return (LastTime > 0 && Helper.GetTimeStamp() - LastTime > 5000);
        }
        public bool IsNeedTcpHeart()
        {
            return (TcpLastTime > 0 && Helper.GetTimeStamp() - TcpLastTime > 5000);
        }

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
            if (Connected)
            {
                LastTime = Helper.GetTimeStamp();
            }
            
        }

        public void UpdateTcpLastTime()
        {
            if (TcpConnected)
            {
                TcpLastTime = Helper.GetTimeStamp();
            }
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
            UdpAddressId = address.ToInt64();
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
            var ip = socket.RemoteEndPoint as IPEndPoint;
            TcpConnected = true;
            TcpConnecting = false;
            TcpLastTime = Helper.GetTimeStamp();
            Socket = socket;
            Ip = ip.Address.ToString();
            TcpAddressId = ip.ToInt64();
        }


        [ProtoIgnore, JsonIgnore, IgnoreMember]
        public long UdpAddressId { get; set; } = 0;
        [ProtoIgnore, JsonIgnore, IgnoreMember]
        public long TcpAddressId { get; set; } = 0;
    }
}
