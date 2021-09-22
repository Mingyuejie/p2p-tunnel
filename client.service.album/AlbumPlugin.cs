using client.servers.clientServer;
using client.service.tcpforward;
using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Linq;
using System.Net;

namespace client.service.album
{
    public class AlbumSettingPlugin : IClientServiceSettingPlugin
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

        public object LoadSetting()
        {
            return albumSettingModel;
        }

        public void SaveSetting(string jsonStr)
        {
            AlbumSettingModel model = jsonStr.DeJson<AlbumSettingModel>();

            var mapping = tcpForwardHelper.Mappings.FirstOrDefault(c => c.SourcePort == albumSettingModel.SourcePort);
            if (mapping != null)
            {
                tcpForwardHelper.Del(mapping.ID);
            }

            albumSettingModel.SourcePort = model.SourcePort;
            albumSettingModel.TargetName = model.TargetName;
            albumSettingModel.TargetPort = model.TargetPort;
            albumSettingModel.SaveConfig();

            tcpForwardHelper.Add(new TcpForwardRecordBaseModel
            {
                AliveType = TcpForwardAliveTypes.UNALIVE,
                ID = 0,
                SourcePort = albumSettingModel.SourcePort,
                SourceIp = IPAddress.Any.ToString(),
                TargetPort = albumSettingModel.TargetPort,
                TargetIp = IPAddress.Loopback.ToString(),
                TargetName = albumSettingModel.TargetName
            });

            Program.Stop();
            Logger.Instance.Info($"图片相册服务已关闭...");
            Program.Run();
            Logger.Instance.Info($"图片相册服务已启动...");
        }
    }

    public class AlbumSettingModel
    {
        public int SourcePort { get; set; } = 5411;
        public string TargetName { get; set; } = string.Empty;
        public int TargetPort { get; set; } = 5412;

        public static AlbumSettingModel ReadConfig()
        {
            AlbumSettingModel config = File.ReadAllText("appsettings.json").DeJson<AlbumSettingModel>();
            return config;
        }

        public void SaveConfig()
        {
            AlbumSettingModel config = File.ReadAllText("appsettings.json").DeJson<AlbumSettingModel>();

            config.SourcePort = SourcePort;
            config.TargetName = TargetName;
            config.TargetPort = TargetPort;

            File.WriteAllText("appsettings.json", config.ToJson(), System.Text.Encoding.UTF8);
        }
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
            TcpForwardHelper helper = obj.GetService<TcpForwardHelper>();
            AlbumSettingModel config = obj.GetService<AlbumSettingModel>();
            helper.Add(new TcpForwardRecordBaseModel
            {
                AliveType = TcpForwardAliveTypes.UNALIVE,
                ID = 0,
                SourcePort = config.SourcePort,
                SourceIp = IPAddress.Any.ToString(),
                TargetPort = config.TargetPort,
                TargetIp = IPAddress.Loopback.ToString(),
                TargetName = config.TargetName
            });

            Program.Run();
            Logger.Instance.Info($"图片相册服务已启动...");
            return obj;
        }
    }


}
