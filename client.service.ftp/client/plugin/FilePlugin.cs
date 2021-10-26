using client.service.ftp.plugin;
using client.service.ftp.protocol;
using common;
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
        private readonly FtpClient ftpClient;
        public FilePlugin(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }
        public FtpCommand Cmd => FtpCommand.FILE;

        public object Excute(FtpPluginParamWrap data)
        {
            Logger.Instance.Error(data.Data.Length.ToString());
            FtpFileCommand cmd = data.Data.DeBytes<FtpFileCommand>();
            ftpClient.OnFile(cmd, data);
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

        public object Excute(FtpPluginParamWrap data)
        {
            FtpFileEndCommand cmd = data.Data.DeBytes<FtpFileEndCommand>();
            ftpClient.OnFileEnd(cmd);
            return null;
        }
    }
}
