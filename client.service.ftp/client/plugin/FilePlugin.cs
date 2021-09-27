using client.service.ftp.plugin;
using client.service.ftp.protocol;
using common.extends;
using ProtoBuf;
using server.model;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace client.service.ftp.client.plugin
{
    public class FilePlugin : IFtpClientPlugin
    {
        private readonly FtpClient  ftpClient;
        public FilePlugin(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }
        public FtpCommand Cmd => FtpCommand.FILE;

        public object Excute(PluginParamWrap data)
        {
            FtpFileCommand cmd = data.Wrap.Content.DeBytes<FtpFileCommand>();
            if (ftpClient.OnFile(cmd,data.TcpSocket))
            {
                ftpClient.SendOnlyTcp(new FtpFileEndCommand { Md5 = cmd.Md5 }, data.TcpSocket);
            }
            return null;
        }
    }
}
