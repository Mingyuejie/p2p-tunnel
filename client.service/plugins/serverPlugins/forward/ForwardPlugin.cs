using common.extends;
using server;
using server.model;
using server.plugin;
using System.Threading.Tasks;

namespace client.service.plugins.serverPlugins.forward
{
    /// <summary>
    /// 来自服务器的消息转发  a->服务器->b
    /// </summary>
    public class ForwardPlugin : IPlugin
    {
        private readonly ServerPluginHelper serverPluginHelper;
        public ForwardPlugin(ServerPluginHelper serverPluginHelper)
        {
            this.serverPluginHelper = serverPluginHelper;
        }

        public async Task Execute(IConnection connection)
        {
            ForwardModel model = connection.ReceiveRequestWrap.Memory.DeBytes<ForwardModel>();
            await serverPluginHelper.InputData(new ServerDataWrap
            {
                Connection = connection,
                Index = 0,
                Length = model.Data.Length,
                Data = model.Data
            });

        }
    }
}
