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

        public object Excute(PluginParamWrap data)
        {
            FtpFileCommand cmd = data.Wrap.Content.DeBytes<FtpFileCommand>();
            if (ftpServer.OnFile(cmd, data.TcpSocket))
            {
                ftpServer.SendOnlyTcp(new FtpFileEndCommand { Md5 = cmd.Md5 }, data.TcpSocket);
            }

            return true;
        }
    }

}
