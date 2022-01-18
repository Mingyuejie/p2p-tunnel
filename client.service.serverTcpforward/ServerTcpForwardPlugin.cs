using client.plugins.serverPlugins.register;
using client.servers.clientServer;
using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using server;
using server.model;
using server.plugin;
using System.Threading.Tasks;

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

        public async Task Execute(IConnection connection)
        {
            ServerTcpForwardModel model = connection.ReceiveRequestWrap.Memory.DeBytes<ServerTcpForwardModel>();
            await serverTcpForwardHelper.Request(model);
        }
    }


    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddServerTcpForwardPlugin(this ServiceCollection obj)
        {
            ServerTcpForwardRegisterConfig config = ServerTcpForwardRegisterConfig.ReadConfig().Result;
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
                    TimerIntervalHelper.SetTimeout(() =>
                    {
                        serverTcpForwardHelper.UnRegister().Wait();
                        serverTcpForwardHelper.Register().Wait();
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

        public async Task<object> LoadSetting()
        {
            return await Task.FromResult(serverTcpForwardRegisterConfig);
        }

        public async Task<string> SaveSetting(string jsonStr)
        {
            ServerTcpForwardRegisterConfig _config = jsonStr.DeJson<ServerTcpForwardRegisterConfig>();

            serverTcpForwardRegisterConfig.Enable = _config.Enable;
            serverTcpForwardRegisterConfig.Web = _config.Web;
            serverTcpForwardRegisterConfig.Tunnel = _config.Tunnel;
            await serverTcpForwardRegisterConfig.SaveConfig();

            serverTcpForwardHelper.UnRegister().Wait();
            var res = serverTcpForwardHelper.Register().Result;
            if (res.Code != MessageResponeCode.OK)
            {
                return res.Code.GetDesc((byte)res.Code);
            }

            return string.Empty;
        }

        public async Task<bool> SwitchEnable(bool enable)
        {
            serverTcpForwardRegisterConfig.Enable = enable;
            await serverTcpForwardRegisterConfig.SaveConfig();

            serverTcpForwardHelper.UnRegister().Wait();
            serverTcpForwardHelper.Register().Wait();

            return true;
        }
    }

}
