using client.plugins.serverPlugins.register;
using client.servers.clientServer;
using common;
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
            ServerTcpForwardModel model = data.Wrap.Memory.DeBytes<ServerTcpForwardModel>();
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

            var registerState = obj.GetService<RegisterState>();
            var serverTcpForwardHelper = obj.GetService<ServerTcpForwardHelper>();
            var config = obj.GetService<ServerTcpForwardRegisterConfig>();
            registerState.LocalInfo.TcpConnectedSub.Sub((connected) =>
            {
                if (connected && config.AutoReg)
                {
                    Helper.SetTimeout(() =>
                    {
                        serverTcpForwardHelper.UnRegister();
                        Helper.SetTimeout(() =>
                        {
                            serverTcpForwardHelper.Register();
                        }, 1000);
                    }, 1000);
                }

            });

            Logger.Instance.Info("服务器TCP转发插件已加载");
            return obj;
        }
    }


    public class ServerTcpForwardSettingPlugin : IClientServiceSettingPlugin
    {
        private readonly ServerTcpForwardRegisterConfig serverTcpForwardRegisterConfig;
        private readonly ServerTcpForwardHelper serverTcpForwardHelper;
        public ServerTcpForwardSettingPlugin(ServerTcpForwardRegisterConfig serverTcpForwardRegisterConfig, ServerTcpForwardHelper serverTcpForwardHelper)
        {
            this.serverTcpForwardRegisterConfig = serverTcpForwardRegisterConfig;
            this.serverTcpForwardHelper = serverTcpForwardHelper;
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

            serverTcpForwardHelper.UnRegister();
            var res = serverTcpForwardHelper.Register();
            if (res.Code != ServerMessageResponeCodes.OK)
            {
                return res.ErrorMsg;
            }

            return string.Empty;
        }

        public bool SwitchEnable(bool enable)
        {
            serverTcpForwardRegisterConfig.Enable = enable;
            serverTcpForwardRegisterConfig.SaveConfig();

            serverTcpForwardHelper.UnRegister();
            serverTcpForwardHelper.Register();

            return true;
        }
    }

}
