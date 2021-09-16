using common.extends;
using System.IO;
using System.Net;
using System.Text.Json.Serialization;

namespace client.service.config
{
    /// <summary>
    /// 配置信息
    /// </summary>
    public class Config
    {
        /// <summary>
        /// 本地websocket
        /// </summary>
        public WebsocketConfig Websocket { get; set; } = new WebsocketConfig();
        /// <summary>
        /// 客户端配置
        /// </summary>
        public ClientConfig Client { get; set; } = new ClientConfig();
        /// <summary>
        /// 服务器配置
        /// </summary>
        public ServerConfig Server { get; set; } = new ServerConfig();
        /// <summary>
        /// 本地web管理端配置
        /// </summary>
        public WebConfig Web { get; set; } = new WebConfig();
        /// <summary>
        /// 文件服务器配置
        /// </summary>
        public FileServerConfig FileServer { get; set; } = new FileServerConfig();


        public static Config ReadConfig()
        {
            Config config = File.ReadAllText("appsettings.json").DeJson<Config>();
            return config;
        }

        public void SaveConfig()
        {
            Config config = File.ReadAllText("appsettings.json").DeJson<Config>();

            config.Web = Web;
            config.Client = Client;
            config.Server = Server;
            config.FileServer = FileServer;
            config.Websocket = Websocket;

            File.WriteAllText("appsettings.json", config.ToJson(), System.Text.Encoding.UTF8);
        }
    }

    /// <summary>
    /// 本地web管理端配置
    /// </summary>
    public class WebConfig
    {
        public int Port { get; set; } = 8098;
        public string Root { get; set; } = "./web";
        public bool UseIpv6 { get; set; } = false;

        [JsonIgnore]
        public IPAddress BindIp
        {
            get
            {
                return UseIpv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;
            }
        }

    }

    public class WebsocketConfig
    {
        public int Port { get; set; } = 8098;

        public bool UseIpv6 { get; set; } = false;

        [JsonIgnore]
        public IPAddress BindIp
        {
            get
            {
                return UseIpv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;
            }
        }
    }

    /// <summary>
    /// 客户端配置
    /// </summary>
    public class ClientConfig
    {
        /// <summary>
        /// 分组编号
        /// </summary>
        public string GroupId { get; set; } = string.Empty;
        /// <summary>
        /// 客户端名
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 自动注册
        /// </summary>
        public bool AutoReg { get; set; } = false;
        /// <summary>
        /// 上报MAC地址
        /// </summary>
        public bool UseMac { get; set; } = false;

        public bool UseIpv6 { get; set; } = false;

        [JsonIgnore]
        public IPAddress BindIp
        {
            get
            {
                return UseIpv6 ? IPAddress.IPv6Any : IPAddress.Any;
            }
        }
    }

    /// <summary>
    /// 服务器配置
    /// </summary>
    public class ServerConfig
    {
        public string Ip { get; set; } = string.Empty;

        public int Port { get; set; } = 8099;
        public int TcpPort { get; set; } = 8000;
    }

    public class FileServerConfig
    {
        public bool IsStart { get; set; } = true;
        public string Root { get; set; } = "./ftp";
    }
}
