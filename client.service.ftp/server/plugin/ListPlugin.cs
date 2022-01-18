using client.service.ftp.plugin;
using client.service.ftp.protocol;
using common;
using common.extends;
using System;
using System.Threading.Tasks;

namespace client.service.ftp.server.plugin
{
    public class ListPlugin : IFtpServerPlugin
    {
        private readonly FtpServer ftpServer;

        public ListPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }
        public FtpCommand Cmd => FtpCommand.LIST;

        public async Task<FtpResultModel> Execute(FtpPluginParamWrap arg)
        {
            await Task.Yield();

            FtpListCommand cmd = new FtpListCommand();
            cmd.DeBytes(arg.Connection.ReceiveRequestWrap.Memory);
            return new FtpResultModel { Data = ftpServer.GetFiles(cmd, arg) };
        }
    }
}