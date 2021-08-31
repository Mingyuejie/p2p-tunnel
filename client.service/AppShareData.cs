using client.service.config;
using client.service.serverPlugins.clients;
using common;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

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


        //路由网关数，与外网的距离
        public int RouteLevel { get; set; } = 0;

        //外网ip
        public string Ip { get; set; } = string.Empty;

        //客户端名
        public string ClientName { get; set; } = string.Empty;
        //客户端分组编号
        public string GroupId { get; set; } = string.Empty;
        //自动注册
        public bool AutoReg { get; set; } = false;
        public bool UseMac { get; set; } = false;
        public string Mac { get; set; } = string.Empty;


        //UDP是否已连接
        public bool Connected { get; set; } = false;

        //TCP是否已连接
        public bool TcpConnected { get; set; } = false;

        //在线客户端列表
        public ConcurrentDictionary<long, ClientInfo> Clients { get; set; } = new();


        public IPAddress LocalIp { get; set; } = IPAddress.Any;

        //NAT服务地址
        public string ServerIp { get; set; } = string.Empty;

        //NAT服务UDP端口
        public int ServerPort { get; set; } = 0;
        //NAT服务TCP端口
        public int ServerTcpPort { get; set; } = 0;

        //客户端UDP端口
        public int ClientPort { get; set; } = 0;
        //客户端TCP端口
        public int ClientTcpPort { get; set; } = 0;
        public int ClientTcpPort2 { get; set; } = 0;

        //是否正在连接
        public bool IsConnecting { get; set; } = false;

        //连接ID 身份标识
        public long ConnectId { get; set; } = 0;

        public void SaveConfig()
        {
            Config config = Helper.DeJsonSerializer<Config>(System.IO.File.ReadAllText("appsettings.json"));

            config.Client.GroupId = GroupId;
            config.Client.Name = ClientName;
            config.Client.AutoReg = AutoReg;
            config.Client.UseMac = UseMac;

            config.Server.Ip = ServerIp;
            config.Server.Port = ServerPort;
            config.Server.TcpPort = ServerTcpPort;

            System.IO.File.WriteAllText("appsettings.json", Helper.JsonSerializer(config), System.Text.Encoding.UTF8);
        }
    }
}
