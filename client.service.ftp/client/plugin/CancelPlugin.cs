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

namespace client.service.ftp.client.plugin
{
    public class CanceledPlugin : IFtpClientPlugin
    {
        public FtpCommand Cmd => FtpCommand.FILE_CANCELED;

        private readonly FtpClient ftpClient;
        public CanceledPlugin(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }

        public object Excute(FtpPluginParamWrap arg)
        {
            FtpCanceledCommand cmd = arg.Data.DeBytes<FtpCanceledCommand>();

            ftpClient.OnFileUploadCanceled(cmd);

            return true;
        }
    }
}
