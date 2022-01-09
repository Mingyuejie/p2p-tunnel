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
using System.Threading.Tasks;

namespace client.service.ftp.server.plugin
{
    public class CancelPlugin : IFtpServerPlugin
    {
        public FtpCommand Cmd => FtpCommand.FILE_CANCEL;

        private readonly FtpServer ftpServer;
        public CancelPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }

        public async Task<FtpResultModel> Execute(FtpPluginParamWrap arg)
        {
            await ftpServer.OnFileUploadCancel(arg.Data.DeBytes<FtpCancelCommand>(), arg);
            return new FtpResultModel();
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

        public async Task<FtpResultModel> Execute(FtpPluginParamWrap arg)
        {
            await Task.Yield();
            ftpServer.OnFileUploadCanceled(arg.Data.DeBytes<FtpCanceledCommand>(), arg);
            return new FtpResultModel();
        }
    }
}
