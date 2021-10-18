using client.service.ftp.plugin;
using client.service.ftp.protocol;
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
            // FtpFileCommand cmd = new FtpFileCommand();
            // cmd.FromBytes(data.Wrap.Content);

            FtpFileCommand cmd = data.Wrap.Content.DeBytes<FtpFileCommand>();
            ftpServer.OnFile(cmd, data);

            return true;
        }
    }

}
