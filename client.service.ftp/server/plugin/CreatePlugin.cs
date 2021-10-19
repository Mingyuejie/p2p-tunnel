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
    public class CreatePlugin : IFtpServerPlugin
    {
        public FtpCommand Cmd => FtpCommand.CREATE;

        private readonly FtpServer ftpServer;
        public CreatePlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }

        public object Excute(FtpPluginParamWrap arg)
        {
            FtpCreateCommand cmd = arg.Data.DeBytes<FtpCreateCommand>();

            if (string.IsNullOrWhiteSpace(cmd.Path))
            {
                arg.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, "目录不可为空");
            }
            else
            {
                List<string> errs = ftpServer.Create(cmd);
                if (errs.Any())
                {
                    arg.SetCode(ServerMessageResponeCodes.ACCESS, string.Join(",", errs));
                }
            }

            return true;
        }
    }
}
