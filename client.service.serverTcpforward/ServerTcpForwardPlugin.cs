using client.servers.clientServer;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using server.model;
using server.plugin;

namespace client.service.serverTcpforward
{
    /// <summary>
    /// 服务器TCP代理转发
    /// </summary>
    public class ServerTcpForwardPlugin : IPlugin
    {
        private readonly ServerTcpForwardHelper serverTcpForwardHelper;
        public ServerTcpForwardPlugin(ServerTcpForwardHelper serverTcpForwardHelper)
        {
            this.serverTcpForwardHelper = serverTcpForwardHelper;
        }

        public void Excute(PluginParamWrap data)
        {
            ServerTcpForwardModel model = data.Wrap.Content.DeBytes<ServerTcpForwardModel>();
            serverTcpForwardHelper.Request(model);
        }
    }


    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddServerTcpForwardPlugin(this ServiceCollection obj)
        {
            ServerTcpForwardRegisterConfig config = ServerTcpForwardRegisterConfig.ReadConfig();
            obj.AddSingleton((e) => config);
            obj.AddSingleton<ServerTcpForwardHelper>();

            return obj;
        }
        public static ServiceProvider UseServerTcpForwardPlugin(this ServiceProvider obj)
        {
            return obj;
        }
    }


    public class ServerTcpForwardSettingPlugin : IClientServiceSettingPlugin
    {
        private readonly ServerTcpForwardRegisterConfig serverTcpForwardRegisterConfig;
        public ServerTcpForwardSettingPlugin(ServerTcpForwardRegisterConfig serverTcpForwardRegisterConfig)
        {
            this.serverTcpForwardRegisterConfig = serverTcpForwardRegisterConfig;
        }

        public string Name => "服务器TCP代理";

        public string Author => "snltty";

        public string Desc => "不打洞，直接服务器TCP代理";

        public bool Enable => serverTcpForwardRegisterConfig.Enable;

        public object LoadSetting()
        {
            return serverTcpForwardRegisterConfig;
        }

        public string SaveSetting(string jsonStr)
        {
            ServerTcpForwardRegisterConfig _config = jsonStr.DeJson<ServerTcpForwardRegisterConfig>();

            serverTcpForwardRegisterConfig.Enable = _config.Enable;
            serverTcpForwardRegisterConfig.Web = _config.Web;
            serverTcpForwardRegisterConfig.Tunnel = _config.Tunnel;
            serverTcpForwardRegisterConfig.SaveConfig();

            return string.Empty;
        }

        public bool SwitchEnable(bool enable)
        {
            serverTcpForwardRegisterConfig.Enable = enable;
            serverTcpForwardRegisterConfig.SaveConfig();
            return true;
        }
    }

}
