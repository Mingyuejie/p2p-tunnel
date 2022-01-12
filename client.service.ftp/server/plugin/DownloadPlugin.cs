using client.service.ftp.plugin;
using client.service.ftp.protocol;
using common;
using common.extends;
using server.model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<FtpResultModel> Execute(FtpPluginParamWrap arg)
        {
            await Task.Yield();

            FtpDownloadCommand cmd = new FtpDownloadCommand();
            cmd.DeBytes(arg.Data);

            IEnumerable<string> error = ftpServer.Upload(cmd, arg);
            if (error.Any())
            {
                return new FtpResultModel { Code = FtpResultModel.FtpResultCodes.UNKNOW, Data = $"{string.Join(Helper.SeparatorChar, error)}" };
            }
            return new FtpResultModel();
        }
    }
}
