using client.service.ftp.plugin;
using client.service.ftp.protocol;
using common.extends;
using System.Threading.Tasks;

namespace client.service.ftp.client.plugin
{
    public class FilePlugin : IFtpClientPlugin
    {
        private readonly FtpClient ftpClient;
        public FilePlugin(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }
        public FtpCommand Cmd => FtpCommand.FILE;

        public async Task<FtpResultModel> Execute(FtpPluginParamWrap data)
        {
            FtpFileCommand cmd = new FtpFileCommand();
            cmd.DeBytes(data.Data);
            await ftpClient.OnFile(cmd, data);
            return null;
        }
    }

    public class FileEndPlugin : IFtpClientPlugin
    {
        private readonly FtpClient ftpClient;
        public FileEndPlugin(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }
        public FtpCommand Cmd => FtpCommand.FILE_END;

        public async Task<FtpResultModel> Execute(FtpPluginParamWrap arg)
        {
            await Task.Yield();
            FtpFileEndCommand cmd = new FtpFileEndCommand();
            cmd.DeBytes(arg.Data);
            ftpClient.OnFileEnd(cmd, arg);
            return null;
        }
    }
}
