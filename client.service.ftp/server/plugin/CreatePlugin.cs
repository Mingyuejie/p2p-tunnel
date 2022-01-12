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
    public class CreatePlugin : IFtpServerPlugin
    {
        public FtpCommand Cmd => FtpCommand.CREATE;

        private readonly FtpServer ftpServer;
        public CreatePlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }

        public async Task<FtpResultModel> Execute(FtpPluginParamWrap arg)
        {
            await Task.Yield();
            FtpCreateCommand cmd = new FtpCreateCommand();
            cmd.DeBytes(arg.Data);

            if (string.IsNullOrWhiteSpace(cmd.Path))
            {
                return new FtpResultModel { Code = FtpResultModel.FtpResultCodes.PATH_REQUIRED };
            }
            else
            {
                List<string> errs = ftpServer.Create(cmd, arg);
                if (errs.Any())
                {
                    return new FtpResultModel { Code = FtpResultModel.FtpResultCodes.UNKNOW, Data = string.Join(",", errs) };
                }
            }
            return new FtpResultModel();
        }
    }
}
