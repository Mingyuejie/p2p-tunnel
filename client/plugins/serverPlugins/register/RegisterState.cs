using common;
using MessagePack;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace client.plugins.serverPlugins.register
{
    /// <summary>
    /// 本地注册状态
    /// </summary>
    public class RegisterState
    {
        /// <summary>
        /// TCP连接对象
        /// </summary>
        public Socket TcpSocket { get; set; }
        /// <summary>
        /// UDP地址
        /// </summary>
        public IPEndPoint UdpAddress { get; set; }
        /// <summary>
        /// 远程信息
        /// </summary>
        public RemoteInfo RemoteInfo { get; set; } = new RemoteInfo();
        /// <summary>
        /// 本地信息
        /// </summary>
        public LocalInfo LocalInfo { get; set; } = new LocalInfo();

        public void Offline()
        {
            LocalInfo.IsConnecting = false;
            LocalInfo.Connected = false;
            LocalInfo.TcpConnected = false;
            RemoteInfo.Ip = string.Empty;
            RemoteInfo.ConnectId = 0;
            RemoteInfo.TcpPort = 0;
        }
        public void Online(long id,string ip,int tcpPort)
        {
            LocalInfo.IsConnecting = false;
            RemoteInfo.Ip = ip;
            RemoteInfo.ConnectId = id;
            LocalInfo.Connected = true;
            LocalInfo.TcpConnected = true;
            RemoteInfo.TcpPort = tcpPort;
        }
    }

    /// <summary>
    /// 远程信息
    /// </summary>
    [ProtoContract, MessagePackObject]
    public class RemoteInfo
    {
        /// <summary>
        /// 客户端在远程的ip
        /// </summary>
        [ProtoMember(1), Key(1)]
        public string Ip { get; set; } = string.Empty;
        /// <summary>
        /// 客户端在远程的TCP端口
        /// </summary>
        [ProtoMember(2), Key(2)]
        public int TcpPort { get; set; } = 0;
        /// <summary>
        /// 客户端连接ID
        /// </summary>
        [ProtoMember(3), Key(3)]
        public long ConnectId { get; set; } = 0;
    }

    /// <summary>
    /// 本地信息
    /// </summary>
    [ProtoContract, MessagePackObject]
    public class LocalInfo
    {
        /// <summary>
        /// 外网距离
        /// </summary>
        [ProtoMember(1), Key(1)]
        public int RouteLevel { get; set; } = 0;
        /// <summary>
        /// 本地mac地址
        /// </summary>
        [ProtoMember(2), Key(2)]
        public string Mac { get; set; } = string.Empty;
        /// <summary>
        /// 本地UDP端口
        /// </summary>
        [ProtoMember(3), Key(3)]
        public int Port { get; set; } = 0;
        /// <summary>
        /// 本地TCP端口
        /// </summary>
        [ProtoMember(4), Key(4)]
        public int TcpPort { get; set; } = 0;

        [ProtoMember(5), Key(5)]
        public string LocalIp { get; set; } = string.Empty;

        /// <summary>
        /// 是否正在连接服务器
        /// </summary>
        [ProtoMember(6), Key(6)]
        public bool IsConnecting { get; set; } = false;
        /// <summary>
        /// UDP是否已连接服务器
        /// </summary>
        [ProtoMember(7), Key(7)]
        public bool Connected { get; set; } = false;


        public SimplePushSubHandler<bool> TcpConnectedSub { get; } = new SimplePushSubHandler<bool>();

        private bool tcpConnected = false;
        /// <summary>
        /// TCP是否已连接服务器
        /// </summary>
        [ProtoMember(8), Key(8)]
        public bool TcpConnected
        {
            get
            {
                return tcpConnected;
            }
            set
            {
                tcpConnected = value;
                TcpConnectedSub.Push(value);
            }
        }
    }
}
