using client.servers.clientServer;
using common.extends;
using MessagePack;
using ProtoBuf;
using System.IO;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace client
{
    /// <summary>
    /// 配置信息
    /// </summary>
    public class Config : SettingModelBase
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


        public static async Task<Config> ReadConfig()
        {
            return await FromFile<Config>("appsettings.json") ?? new Config();
        }

        public async Task SaveConfig()
        {
            Config config = await ReadConfig();

            config.Web = Web;
            config.Client = Client;
            config.Server = Server;
            config.FileServer = FileServer;
            config.Websocket = Websocket;

            await ToFile(config, "appsettings.json");
        }
    }

    /// <summary>
    /// 本地web管理端配置
    /// </summary>
    [ProtoContract, MessagePackObject]
    public class WebConfig
    {
        [ProtoMember(1), Key(1)]
        public int Port { get; set; } = 8098;
        [ProtoMember(2), Key(2)]
        public string Root { get; set; } = "./web";
        [ProtoMember(3), Key(3)]
        public bool UseIpv6 { get; set; } = false;

        [JsonIgnore, ProtoIgnore, IgnoreMember]
        public IPAddress BindIp
        {
            get
            {
                return UseIpv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;
            }
        }

    }
    [ProtoContract, MessagePackObject]
    public class WebsocketConfig
    {
        [ProtoMember(1), Key(1)]
        public int Port { get; set; } = 8098;
        [ProtoMember(2), Key(2)]
        public bool UseIpv6 { get; set; } = false;

        [JsonIgnore, ProtoIgnore, IgnoreMember]
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
    [ProtoContract, MessagePackObject]
    public class ClientConfig
    {
        /// <summary>
        /// 分组编号
        /// </summary>
        [ProtoMember(1), Key(1)]
        public string GroupId { get; set; } = string.Empty;
        /// <summary>
        /// 客户端名
        /// </summary>
        [ProtoMember(2), Key(2)]
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 自动注册
        /// </summary>
        [ProtoMember(3), Key(3)]
        public bool AutoReg { get; set; } = false;
        /// <summary>
        /// 上报MAC地址
        /// </summary>
        [ProtoMember(4), Key(4)]
        public bool UseMac { get; set; } = false;
        /// <summary>
        /// 使用ipv6
        /// </summary>
        [ProtoMember(5), Key(5)]
        public bool UseIpv6 { get; set; } = false;

        [ProtoMember(6), Key(6)]
        public int TcpBufferSize { get; set; } = 128 * 1024;


        [JsonIgnore, ProtoIgnore, IgnoreMember]
        public IPAddress BindIp
        {
            get
            {
                return UseIpv6 ? IPAddress.IPv6Any : IPAddress.Any;
            }
        }

        [JsonIgnore, ProtoIgnore, IgnoreMember]
        public IPAddress LoopbackIp
        {
            get
            {
                return UseIpv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;
            }
        }
    }

    /// <summary>
    /// 服务器配置
    /// </summary>
    [ProtoContract, MessagePackObject]
    public class ServerConfig
    {
        [ProtoMember(1), Key(1)]
        public string Ip { get; set; } = string.Empty;
        [ProtoMember(2), Key(2)]
        public int UdpPort { get; set; } = 8099;
        [ProtoMember(3), Key(3)]
        public int TcpPort { get; set; } = 8000;
    }

    [ProtoContract, MessagePackObject]
    public class FileServerConfig
    {
        [ProtoMember(1), Key(1)]
        public bool IsStart { get; set; } = true;
        [ProtoMember(2), Key(2)]
        public string Root { get; set; } = "./ftp";
    }
}
