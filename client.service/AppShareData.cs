using client.service.config;
using client.service.serverPlugins.clients;
using common;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;

namespace client.service
{
    public class AppShareData
    {
        private static readonly Lazy<AppShareData> lazy = new(() => new AppShareData());
        public static AppShareData Instance => lazy.Value;
        private AppShareData()
        {
        }


        public IPEndPoint UdpServer { get; set; } = null;
        public Socket TcpServer { get; set; } = null;

        /// <summary>
        /// 远程信息
        /// </summary>
        public RemoteInfo RemoteInfo { get; set; } = new RemoteInfo();
        /// <summary>
        /// 本地信息
        /// </summary>
        public LocalInfo LocalInfo { get; set; } = new LocalInfo();

        /// <summary>
        /// 在线客户端列表
        /// </summary>
        public ConcurrentDictionary<long, ClientInfo> Clients { get; set; } = new();
        /// <summary>
        /// 客户端配置
        /// </summary>
        public ClientConfig ClientConfig { get; set; } = new ClientConfig();
        /// <summary>
        /// 服务器配置
        /// </summary>
        public ServerConfig ServerConfig { get; set; } = new ServerConfig();

        /// <summary>
        /// 文件服务配置
        /// </summary>
        public FileServerConfig FileServerConfig { get; set; } = new FileServerConfig();

        /// <summary>
        /// 保存配置
        /// </summary>
        public void SaveConfig()
        {
            Config config = Helper.DeJsonSerializer<Config>(System.IO.File.ReadAllText("appsettings.json"));

            config.Client = ClientConfig;

            config.Server = ServerConfig;

            config.FileServer = FileServerConfig;

            System.IO.File.WriteAllText("appsettings.json", Helper.JsonSerializer(config), System.Text.Encoding.UTF8);
        }
    }

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

        [JsonIgnore]
        public IPAddress LocalIp { get; set; } = IPAddress.Any;
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
