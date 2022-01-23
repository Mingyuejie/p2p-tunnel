using client.service.ftp.commands;
using common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.ftp.server.plugin
{
    public class DownloadPlugin : IFtpCommandServerPlugin
    {
        public FtpCommand Cmd => FtpCommand.DOWNLOAD;

        private readonly FtpServer ftpServer;
        public DownloadPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }

        public async Task<FtpResultInfo> Execute(FtpPluginParamWrap arg)
        {
            await Task.Yield();

            FtpDownloadCommand cmd = new FtpDownloadCommand();
            cmd.DeBytes(arg.Connection.ReceiveRequestWrap.Memory);

            IEnumerable<string> error = ftpServer.Upload(cmd, arg);
            if (error.Any())
            {
                return new FtpResultInfo { Code = FtpResultInfo.FtpResultCodes.UNKNOW, Data = $"{string.Join(Helper.SeparatorChar, error)}" };
            }
            return new FtpResultInfo();
        }
    }
}
