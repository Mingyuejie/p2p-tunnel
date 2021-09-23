using client.servers.clientServer;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using server.model;
using server.plugin;
using System;
using System.IO;

namespace client.service.tcpforward
{
    public class TcpForwardPlugin : IPlugin
    {
        private readonly TcpForwardEventHandles tcpForwardEventHandles;
        public TcpForwardPlugin(TcpForwardEventHandles tcpForwardEventHandles)
        {
            this.tcpForwardEventHandles = tcpForwardEventHandles;
        }

        public void Excute(PluginParamWrap arg)
        {
            TcpForwardModel data = arg.Wrap.Content.DeBytes<TcpForwardModel>();
            tcpForwardEventHandles.OnTcpForward(new OnTcpForwardEventArg
            {
                Packet = arg,
                Data = data,
            });
        }
    }


    public class TcpForwardSettingPlugin : IClientServiceSettingPlugin
    {
        private readonly TcpForwardSettingModel tcpForwardSettingModel;


        public TcpForwardSettingPlugin(TcpForwardSettingModel tcpForwardSettingModel)
        {
            this.tcpForwardSettingModel = tcpForwardSettingModel;
        }
        public string Name => "TCP转发";

        public string Author => "snltty";

        public string Desc => "白名单不为空时只允许白名单内端口";

        public object LoadSetting()
        {
            return tcpForwardSettingModel;
        }

        public void SaveSetting(string jsonStr)
        {
            TcpForwardSettingModel model = jsonStr.DeJson<TcpForwardSettingModel>();
            model.SaveConfig();
        }
    }

    public class TcpForwardSettingModel
    {
        public int[] PortWhiteList { get; set; } = Array.Empty<int>();
        public int[] PortBlackList { get; set; } = Array.Empty<int>();

        public static TcpForwardSettingModel ReadConfig()
        {
            TcpForwardSettingModel config = File.ReadAllText("tcpforward-appsettings.json").DeJson<TcpForwardSettingModel>();
            return config;
        }

        public void SaveConfig()
        {
            TcpForwardSettingModel config = File.ReadAllText("tcpforward-appsettings.json").DeJson<TcpForwardSettingModel>();

            config.PortBlackList = PortBlackList;
            config.PortWhiteList = PortWhiteList;

            File.WriteAllText("tcpforward-appsettings.json", config.ToJson(), System.Text.Encoding.UTF8);
        }
    }

    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddTcpForwardPlugin(this ServiceCollection obj)
        {
            TcpForwardSettingModel config = TcpForwardSettingModel.ReadConfig();
            obj.AddSingleton((e) => config);

            obj.AddSingleton<TcpForwardServer>();
            obj.AddSingleton<TcpForwardHelper>();
            obj.AddSingleton<TcpForwardEventHandles>();
            return obj;
        }
        public static ServiceProvider UseTcpForwardPlugin(this ServiceProvider obj)
        {
            obj.GetService<TcpForwardHelper>().Start();

            return obj;
        }
    }
}
