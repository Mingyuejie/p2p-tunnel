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
    public class ListPlugin : IFtpServerPlugin
    {
        private readonly Config config;

        public ListPlugin(Config config)
        {
            this.config = config;
        }
        public FtpCommand Cmd => FtpCommand.LIST;

        public object Excute(PluginParamWrap data)
        {
            FtpListCommand cmd = data.Wrap.Content.DeBytes<FtpListCommand>();

            DirectoryInfo dirInfo;
            if (string.IsNullOrWhiteSpace(config.ServerCurrentPath))
            {
                config.ServerCurrentPath = config.ServerRoot;
            }
            if (string.IsNullOrWhiteSpace(cmd.Path))
            {
                dirInfo = new DirectoryInfo(config.ServerCurrentPath);
            }
            else
            {
                dirInfo = new DirectoryInfo(Path.Combine(config.ServerCurrentPath, cmd.Path));
            }
            //不能访问根目录的上级目录
            if (dirInfo.FullName.Length < config.ServerRoot.Length)
            {
                dirInfo = new DirectoryInfo(config.ServerRoot);
            }
            config.ServerCurrentPath = dirInfo.FullName;

            return dirInfo.GetDirectories()
                .Where(c => (c.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                .Select(c => new FileInfo
                {
                    CreationTime = c.CreationTime,
                    Length = 0,
                    Name = c.Name,
                    LastWriteTime = c.LastWriteTime,
                    LastAccessTime = c.LastAccessTime,
                    Type = FileType.Folder,
                }).Concat(dirInfo.GetFiles()
            .Where(c => (c.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
            .Select(c => new FileInfo
            {
                CreationTime = c.CreationTime,
                Length = c.Length,
                Name = c.Name,
                LastWriteTime = c.LastWriteTime,
                LastAccessTime = c.LastAccessTime,
                Type = FileType.File,
            }));
        }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum FileType : byte
    {
        Folder = 0, File = 1
    }

    [ProtoContract]
    public class FileInfo
    {
        [ProtoMember(1)]
        public DateTime LastAccessTime { get; set; } = DateTime.Now;
        [ProtoMember(2)]
        public DateTime CreationTime { get; set; } = DateTime.Now;
        [ProtoMember(3)]
        public DateTime LastWriteTime { get; set; } = DateTime.Now;
        [ProtoMember(4)]
        public string Name { get; set; } = string.Empty;
        [ProtoMember(5)]
        public long Length { get; set; } = 0;
        [ProtoMember(6, IsRequired = true)]
        public FileType Type { get; set; } = FileType.File;
    }
}
