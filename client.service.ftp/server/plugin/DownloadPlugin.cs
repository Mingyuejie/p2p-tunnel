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
    public class DownloadPlugin : IFtpPlugin
    {
        public FtpCommand Cmd => FtpCommand.CREATE;

        private readonly Config config;
        private readonly FtpServer  ftpServer;
        public DownloadPlugin(Config config, FtpServer ftpServer)
        {
            this.config = config;
            this.ftpServer = ftpServer;
        }

        public object Excute(PluginParamWrap arg)
        {
            FtpDownloadCommand cmd = arg.Wrap.Content.DeBytes<FtpDownloadCommand>();

            string filePath = Path.Combine(config.ServerCurrentPath, cmd.Path);
            if (!filePath.StartsWith(config.ServerRoot))
            {
                arg.SetCode(ServerMessageResponeCodes.ACCESS, "无目录权限");
            }
            else
            {
                if (!File.Exists(filePath))
                {
                    arg.SetCode(ServerMessageResponeCodes.NOT_FOUND, "文件不存在");
                }
                else
                {
                    ftpServer.SendFile(new System.IO.FileInfo(filePath), arg.TcpSocket);
                }
            }
            return null;
        }
    }
}
