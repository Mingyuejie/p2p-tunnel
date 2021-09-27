using client.plugins.serverPlugins;
using client.service.ftp.plugin;
using client.service.ftp.protocol;
using common;
using common.extends;
using ProtoBuf;
using server.model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.ftp.server.plugin
{
    public class DownloadPlugin : IFtpServerPlugin
    {
        public FtpCommand Cmd => FtpCommand.DOWNLOAD;

        private readonly Config config;
        private readonly FtpServer ftpServer;
        public DownloadPlugin(Config config, FtpServer ftpServer)
        {
            this.config = config;
            this.ftpServer = ftpServer;
        }

        public object Excute(PluginParamWrap arg)
        {
            FtpDownloadCommand cmd = arg.Wrap.Content.DeBytes<FtpDownloadCommand>();

            string[] paths = cmd.Path.Split(',');
            IEnumerable<string> accessPaths = paths.Where(c => Path.Combine(config.ServerCurrentPath, cmd.Path).StartsWith(config.ServerRoot));
            IEnumerable<string> notAccessPaths = paths.Except(accessPaths);
            if (notAccessPaths.Any())
            {
                arg.SetCode(ServerMessageResponeCodes.ACCESS, $" {string.Join(',', notAccessPaths)} 无目录权限");
            }
            if (accessPaths.Any())
            {
                ftpServer.Upload(string.Join(',', accessPaths), arg);
            }

            return true;
        }
    }
}
