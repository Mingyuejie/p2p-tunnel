using client.servers.clientServer;
using client.service.tcpforward;
using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace client.service.album
{
    public class AlbumSettingPlugin : IClientServiceSettingPlugin, IClientServicePlugin
    {
        private readonly AlbumSettingModel albumSettingModel;
        private readonly TcpForwardHelper tcpForwardHelper;

        public AlbumSettingPlugin(AlbumSettingModel albumSettingModel, TcpForwardHelper tcpForwardHelper)
        {
            this.albumSettingModel = albumSettingModel;
            this.tcpForwardHelper = tcpForwardHelper;
        }
        public string Name => "图片相册插件";

        public string Author => "snltty";

        public string Desc => $"服务端口:{albumSettingModel.ServerPort}";

        public object LoadSetting()
        {
            return albumSettingModel;
        }

        public void SaveSetting(string jsonStr)
        {
            AlbumSettingModel setting = jsonStr.DeJson<AlbumSettingModel>();

            albumSettingModel.Clients = setting.Clients;
            albumSettingModel.ServerPort = setting.ServerPort;
            albumSettingModel.UseServer = setting.UseServer;
            albumSettingModel.SaveConfig();

            foreach (var item in albumSettingModel.Clients)
            {
                var model = new TcpForwardRecordBaseModel
                {
                    AliveType = TcpForwardAliveTypes.UNALIVE,
                    ID = 0,
                    SourcePort = item.SourcePort,
                    SourceIp = IPAddress.Any.ToString(),
                    TargetPort = item.TargetPort,
                    TargetIp = IPAddress.Loopback.ToString(),
                    TargetName = item.TargetName,
                    Editable = false,
                    Desc = "图片相册插件"
                };
                tcpForwardHelper.DelByPort(item.SourcePort);
                tcpForwardHelper.Add(model);
                tcpForwardHelper.Start(tcpForwardHelper.GetByPort(item.SourcePort));
            }

            Program.Stop();
            Logger.Instance.Info($"图片相册服务已关闭...");
            if (albumSettingModel.UseServer)
            {
                Program.Run();
                Logger.Instance.Info($"图片相册服务已启动...");
            }
        }

        public object Load(ClientServicePluginExcuteWrap arg)
        {
            return albumSettingModel;
        }
    }

    public class AlbumSettingModel
    {
        public AlbumSettingClientModel[] Clients { get; set; } = new AlbumSettingClientModel[]
        {
           new AlbumSettingClientModel{  SourcePort = 5411, TargetPort = 5412,TargetName = string.Empty}
        };
        public int ServerPort { get; set; } = 5412;
        public bool UseServer { get; set; } = false;
        public string AdminPssd { get; set; } = string.Empty;

        public static AlbumSettingModel ReadConfig()
        {
            AlbumSettingModel config = File.ReadAllText("album-appsettings.json").DeJson<AlbumSettingModel>();
            return config;
        }

        public void SaveConfig()
        {
            AlbumSettingModel config = File.ReadAllText("album-appsettings.json").DeJson<AlbumSettingModel>();

            config.Clients = Clients;
            config.ServerPort = ServerPort;

            File.WriteAllText("album-appsettings.json", config.ToJson(), System.Text.Encoding.UTF8);
        }
    }
    public class AlbumSettingClientModel
    {
        public int SourcePort { get; set; } = 5411;
        public string TargetName { get; set; } = string.Empty;
        public int TargetPort { get; set; } = 5412;
    }

    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddAlbumPlugin(this ServiceCollection obj)
        {
            var config = AlbumSettingModel.ReadConfig();
            obj.AddSingleton((e) => config);

            return obj;
        }

        public static ServiceProvider UseAlbumPlugin(this ServiceProvider obj)
        {
            TcpForwardHelper tcpForwardHelper = obj.GetService<TcpForwardHelper>();
            AlbumSettingModel config = obj.GetService<AlbumSettingModel>();

            foreach (var item in config.Clients)
            {
                var model = new TcpForwardRecordBaseModel
                {
                    AliveType = TcpForwardAliveTypes.UNALIVE,
                    ID = 0,
                    SourcePort = item.SourcePort,
                    SourceIp = IPAddress.Any.ToString(),
                    TargetPort = item.TargetPort,
                    TargetIp = IPAddress.Loopback.ToString(),
                    TargetName = item.TargetName,
                    Editable = false,
                    Desc = "图片相册插件"
                };
                tcpForwardHelper.DelByPort(item.SourcePort);
                tcpForwardHelper.Add(model);
                tcpForwardHelper.Start(tcpForwardHelper.GetByPort(item.SourcePort));
            }

            if (config.UseServer)
            {
                Program.Run();
                Logger.Instance.Info($"图片相册服务已启动...");
            }
            return obj;
        }
    }


}
