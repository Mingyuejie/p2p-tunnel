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

        public object Excute(PluginParamWrap arg)
        {
            FtpDelCommand cmd = arg.Wrap.Content.DeBytes<FtpDelCommand>();

            if (string.IsNullOrWhiteSpace(cmd.Path))
            {
                arg.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, "目录不可为空");
            }
            else
            {
                List<string> errs = ftpServer.Delete(cmd.Path, arg);
                if (errs.Any())
                {
                    arg.SetCode(ServerMessageResponeCodes.ACCESS, string.Join(",", errs));
                }
            }
            return true;
        }
    }
}
