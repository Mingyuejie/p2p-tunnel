using client.messengers.clients;
using client.servers.clientServer;
using common.extends;
using server;
using server.model;
using System.Threading.Tasks;

namespace client.service.cmd
{
    public class CmdsClientService : CmdBase, IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly IClientInfoCaching clientInfoCaching;
        public CmdsClientService(MessengerSender messengerSender, IClientInfoCaching clientInfoCaching) : base(clientInfoCaching)
        {
            this.messengerSender = messengerSender;
            this.clientInfoCaching = clientInfoCaching;
        }
        public async Task<CmdResultInfo> Execute(ClientServiceParamsInfo arg)
        {
            RemoteCmdParamsInfo model = arg.Content.DeJson<RemoteCmdParamsInfo>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client))
            {
                var res = await messengerSender.SendReply(new  MessageRequestParamsInfo<CmdParamsInfo>
                {
                    Path = "cmd/Execute",
                    Connection = client.TcpConnection,
                    Timeout = 0,
                    Data = new CmdParamsInfo { Cmd = model.Cmd }
                });
                if (res.Code == MessageResponeCodes.OK)
                {
                    return res.Data.DeBytes<CmdResultInfo>();
                }
                return new CmdResultInfo { ErrorMsg = res.Code.ToString() };
            }
            return await ExecuteCmd(model.Id, model.Cmd);
        }
    }
}
