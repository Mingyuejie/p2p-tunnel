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
    public class CreatePlugin : IFtpClientPlugin
    {
        public FtpCommand Cmd => FtpCommand.CREATE;

        private readonly FtpClient ftpClient;
        public CreatePlugin(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }

        public object Excute(PluginParamWrap arg)
        {
            FtpCreateCommand cmd = arg.Wrap.Content.DeBytes<FtpCreateCommand>();

            if (string.IsNullOrWhiteSpace(cmd.Path))
            {
                arg.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, "目录不可为空");
            }
            else
            {
                ftpClient.Create(cmd.Path);
            }

            return true;
        }
    }
}
