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

namespace client.service.ftp.client.plugin
{
    public class FileEndPlugin : IFtpPlugin
    {
        private readonly FtpClient ftpClient;
        public FileEndPlugin(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }
        public FtpCommand Cmd => FtpCommand.FILE;

        public object Excute(PluginParamWrap data)
        {
            FtpFileEndCommand cmd = data.Wrap.Content.DeBytes<FtpFileEndCommand>();
            ftpClient.OnSendFileEnd(cmd);
            return null;
        }
    }
}
