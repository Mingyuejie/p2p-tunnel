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
    public class CreatePlugin : IFtpClientPlugin
    {
        public FtpCommand Cmd => FtpCommand.CREATE;

        private readonly FtpClient ftpClient;
        public CreatePlugin(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }

        public async Task<FtpResultModel> Execute(FtpPluginParamWrap arg)
        {
            await Task.Yield();

            FtpCreateCommand cmd = new FtpCreateCommand();
            cmd.DeBytes(arg.Connection.ReceiveRequestWrap.Memory);

            if (string.IsNullOrWhiteSpace(cmd.Path))
            {
                return new FtpResultModel { Code = FtpResultModel.FtpResultCodes.PATH_REQUIRED };
            }
            else
            {
                ftpClient.Create(cmd.Path);
            }

            return new FtpResultModel();
        }
    }
}
