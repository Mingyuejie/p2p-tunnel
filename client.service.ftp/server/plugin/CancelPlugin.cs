using client.service.ftp.extends;
using client.service.ftp.plugin;
using client.service.ftp.protocol;
using common.extends;
using ProtoBuf;
using server.model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace client.service.ftp.server.plugin
{
    public class CancelPlugin : IFtpServerPlugin
    {
        public FtpCommand Cmd => FtpCommand.FILE_CANCEL;

        private readonly FtpServer  ftpServer;
        public CancelPlugin( FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }

        public object Excute(FtpPluginParamWrap arg)
        {
            FtpCancelCommand cmd = arg.Wrap.Content.DeBytes<FtpCancelCommand>();

            ftpServer.OnFileUploadCancel(cmd);

            return true;
        }
    }

    public class CanceledPlugin : IFtpServerPlugin
    {
        public FtpCommand Cmd => FtpCommand.FILE_CANCELED;

        private readonly FtpServer ftpServer;
        public CanceledPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }

        public object Excute(FtpPluginParamWrap arg)
        {
            FtpCanceledCommand cmd = arg.Wrap.Content.DeBytes<FtpCanceledCommand>();

            ftpServer.OnFileUploadCanceled(cmd);

            return true;
        }
    }
}
