using client.messengers.clients;
using common.extends;
using server;
using System.Threading.Tasks;

namespace client.service.cmd
{
    public class CmdMessenger : CmdBase, IMessenger
    {
        private readonly Config config;
        public CmdMessenger(Config config, IClientInfoCaching clientInfoCaching) : base(clientInfoCaching)
        {
            this.config = config;
        }
        public async Task<CmdResultInfo> Execute(IConnection connection)
        {
            CmdParamsInfo cmd = connection.ReceiveRequestWrap.Memory.DeBytes<CmdParamsInfo>();
            if (!config.Enable)
            {
                return new CmdResultInfo { ErrorMsg = "远程命令服务未开启" };
            }
            return await ExecuteCmd(connection.ConnectId, cmd.Cmd);
        }
    }
}
