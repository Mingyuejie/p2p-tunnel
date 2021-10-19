using client.service.ftp.plugin;
using client.service.ftp.protocol;
using common.extends;
using server.model;
using System.Collections.Generic;
using System.Linq;

namespace client.service.ftp.server.plugin
{
    public class DownloadPlugin : IFtpServerPlugin
    {
        public FtpCommand Cmd => FtpCommand.DOWNLOAD;

        private readonly FtpServer ftpServer;
        public DownloadPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }

        public object Excute(FtpPluginParamWrap arg)
        {
            FtpDownloadCommand cmd = arg.Data.DeBytes<FtpDownloadCommand>();

            IEnumerable<string> error = ftpServer.Upload(cmd, arg);
            if (error.Any())
            {
                arg.SetCode(ServerMessageResponeCodes.ACCESS, $" {string.Join(',', error)}");
            }
            return true;
        }
    }
}
