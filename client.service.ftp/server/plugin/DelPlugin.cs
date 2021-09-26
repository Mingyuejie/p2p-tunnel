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
    public class DelPlugin : IFtpPlugin
    {
        private readonly Config config;

        public DelPlugin(Config config)
        {
            this.config = config;
        }
        public FtpCommand Cmd => FtpCommand.DELETE;

        public object Excute(PluginParamWrap arg)
        {
            FtpDelCommand cmd = arg.Wrap.Content.DeBytes<FtpDelCommand>();

            string filePath = Path.Combine(config.ServerCurrentPath, cmd.Path);
            if (!filePath.StartsWith(config.ServerRoot))
            {
                arg.SetCode(ServerMessageResponeCodes.ACCESS, "无目录权限");
            }
            else
            {
                Clear(filePath);
            }

            return null;
        }

        private void Clear(string path)
        {
            if (Directory.Exists(path))
            {
                var dirs = new DirectoryInfo(path).GetDirectories();
                foreach (var item in dirs)
                {
                    Clear(item.FullName);
                }

                var files = new DirectoryInfo(path).GetFiles();
                foreach (var item in files)
                {
                    item.Delete();
                }
                Directory.Delete(path);
            }
            else
            {
                File.Delete(path);
            }
        }
    }
}
