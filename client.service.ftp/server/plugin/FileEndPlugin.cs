using client.plugins.serverPlugins;
using client.service.ftp.plugin;
using client.service.ftp.protocol;
using common.extends;
using ProtoBuf;
using server.model;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.ftp.server.plugin
{
    public class FileEndPlugin : IFtpServerPlugin
    {
        private readonly FtpServer ftpServer;
        public FileEndPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }
        public FtpCommand Cmd => FtpCommand.FILE_END;

        public object Excute(PluginParamWrap data)
        {
            FtpFileEndCommand cmd = data.Wrap.Content.DeBytes<FtpFileEndCommand>();
            ftpServer.OnSendFileEnd(cmd);
            return true;
        }
    }
}
