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

        public async Task<FtpResultModel> Execute(FtpPluginParamWrap arg)
        {
            await Task.Yield();

            FtpCanceledCommand cmd = new FtpCanceledCommand();
            cmd.DeBytes(arg.Connection.ReceiveRequestWrap.Memory);

            ftpClient.OnFileUploadCanceled(cmd, arg);

            return new FtpResultModel();
        }
    }
}
