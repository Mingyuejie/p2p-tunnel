using client.service.ftp.plugin;
using client.service.ftp.protocol;
using common.extends;
using ProtoBuf;
using server.model;
using System;
using System.IO;
using System.Linq;

namespace client.service.ftp.server.plugin
{
    public class CreatePlugin : IFtpPlugin
    {
        public FtpCommand Cmd => FtpCommand.CREATE;

        private readonly Config config;
        public CreatePlugin(Config config)
        {
            this.config = config;
        }

        public object Excute(PluginParamWrap arg)
        {
            FtpCreateCommand cmd = arg.Wrap.Content.DeBytes<FtpCreateCommand>();

            string filePath = Path.Combine(config.ServerCurrentPath, cmd.Path);
            if (!filePath.StartsWith(config.ServerRoot))
            {
                arg.SetCode(ServerMessageResponeCodes.ACCESS, "无目录权限");
            }
            else
            {
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }
            }
            return null;
        }
    }
}
