using client.service.ftp.plugin;
using client.service.ftp.protocol;
using common;
using common.extends;
using server.model;

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

        public object Excute(FtpPluginParamWrap data)
        {
            FtpFileCommand cmd = new FtpFileCommand();
            cmd.FromBytes(data.Data);
            ftpServer.OnFile(cmd, data);
            return true;
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

        public object Excute(FtpPluginParamWrap data)
        {
            FtpFileEndCommand cmd = data.Data.DeBytes<FtpFileEndCommand>();
            ftpServer.OnFileEnd(cmd);
            return true;
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

        public object Excute(FtpPluginParamWrap data)
        {
            FtpFileErrorCommand cmd = data.Data.DeBytes<FtpFileErrorCommand>();
            ftpServer.OnFileError(cmd);
            return true;
        }
    }
}
