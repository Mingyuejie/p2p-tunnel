using client.service.ftp.plugin;
using client.service.ftp.protocol;
using common;
using common.extends;
using server.model;
using System.Threading.Tasks;

namespace client.service.ftp.server.plugin
{
    public class FilePlugin : IFtpServerPlugin
    {
        private readonly FtpServer ftpServer;
        public FilePlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }
        public FtpCommand Cmd => FtpCommand.FILE;

        public async Task<FtpResultModel> Execute(FtpPluginParamWrap data)
        {
            FtpFileCommand cmd = new FtpFileCommand();
            cmd.FromBytes(data.Data);
            await ftpServer.OnFile(cmd, data);
            return null;
        }
    }

    public class FileEndPlugin : IFtpServerPlugin
    {
        private readonly FtpServer ftpServer;
        public FileEndPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }
        public FtpCommand Cmd => FtpCommand.FILE_END;

        public async Task<FtpResultModel> Execute(FtpPluginParamWrap arg)
        {
            ftpServer.OnFileEnd(arg.Data.DeBytes<FtpFileEndCommand>(), arg);
            return await Task.FromResult<FtpResultModel>(null);
        }
    }

    public class FileErrorPlugin : IFtpServerPlugin
    {
        private readonly FtpServer ftpServer;
        public FileErrorPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }
        public FtpCommand Cmd => FtpCommand.FILE_ERROR;

        public async Task<FtpResultModel> Execute(FtpPluginParamWrap arg)
        {
            await Task.Yield();
            ftpServer.OnFileError(arg.Data.DeBytes<FtpFileErrorCommand>(), arg);
            return null;
        }
    }
}
