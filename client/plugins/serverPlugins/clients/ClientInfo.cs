using common;
using common.extends;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;

namespace client.plugins.serverPlugins.clients
{

    /// <summary>
    /// 客户端信息
    /// </summary>
    public class ClientInfo
    {
        private bool connecting = false;
        public bool Connecting
        {
            get => connecting;
            set
            {
                connecting = value;
            }
        }

        private bool tcpConnecting = false;
        public bool TcpConnecting
        {
            get => tcpConnecting;
            set
            {
                tcpConnecting = value;
            }
        }

        private bool connected = false;
        public bool Connected
        {
            get => connected;
            set
            {
                connected = value;
            }
        }

        private bool tcpConnected = false;
        public bool TcpConnected
        {
            get => tcpConnected;
            set
            {
                tcpConnected = value;
            }
        }

        [JsonIgnore]
        public Socket Socket { get; set; } = null;
        [JsonIgnore]
        public IPEndPoint Address { get; set; } = null;

        public int Port { get; set; } = 0;
        public int TcpPort { get; set; } = 0;

        public string Name { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;

        public long Id { get; set; } = 0;

        public long LastTime { get; set; } = 0;
        public long TcpLastTime { get; set; } = 0;

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
        }
    }
}
