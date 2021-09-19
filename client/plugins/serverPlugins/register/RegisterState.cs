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
    }

    /// <summary>
    /// 远程信息
    /// </summary>
    public class RemoteInfo
    {
        /// <summary>
        /// 客户端在远程的ip
        /// </summary>
        public string Ip { get; set; } = string.Empty;
        /// <summary>
        /// 客户端在远程的TCP端口
        /// </summary>
        public int TcpPort { get; set; } = 0;
        /// <summary>
        /// 客户端连接ID
        /// </summary>
        public long ConnectId { get; set; } = 0;
    }

    /// <summary>
    /// 本地信息
    /// </summary>
    public class LocalInfo
    {
        /// <summary>
        /// 外网距离
        /// </summary>
        public int RouteLevel { get; set; } = 0;
        /// <summary>
        /// 本地mac地址
        /// </summary>
        public string Mac { get; set; } = string.Empty;
        /// <summary>
        /// 本地UDP端口
        /// </summary>
        public int Port { get; set; } = 0;
        /// <summary>
        /// 本地TCP端口
        /// </summary>
        public int TcpPort { get; set; } = 0;

        public string LocalIp { get; set; } = string.Empty;

        /// <summary>
        /// 是否正在连接服务器
        /// </summary>
        public bool IsConnecting { get; set; } = false;
        /// <summary>
        /// UDP是否已连接服务器
        /// </summary>
        public bool Connected { get; set; } = false;
        /// <summary>
        /// TCP是否已连接服务器
        /// </summary>
        public bool TcpConnected { get; set; } = false;
    }
}
