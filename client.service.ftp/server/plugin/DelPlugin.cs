using client.service.ftp.extends;
using client.service.ftp.plugin;
using client.service.ftp.protocol;
using common.extends;
using Microsoft.VisualBasic.FileIO;
using ProtoBuf;
using server.model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.ftp.server.plugin
{
    public class DelPlugin : IFtpServerPlugin
    {
        private readonly FtpServer ftpServer;
        public DelPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }
        public FtpCommand Cmd => FtpCommand.DELETE;

        public async Task<FtpResultModel> Execute(FtpPluginParamWrap arg)
        {
            await Task.Yield();

            FtpDelCommand cmd = arg.Data.DeBytes<FtpDelCommand>();

            if (string.IsNullOrWhiteSpace(cmd.Path))
            {
                return new FtpResultModel { Code = FtpResultModel.FtpResultCodes.PATH_REQUIRED };
            }
            else
            {
                List<string> errs = ftpServer.Delete(cmd, arg);
                if (errs.Any())
                {
                    return new FtpResultModel { Code = FtpResultModel.FtpResultCodes.UNKNOW,Data = string.Join(",", errs) };
                }
            }
            return new FtpResultModel();
        }
    }
}
