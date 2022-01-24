using client.messengers.clients;
using client.messengers.punchHole.tcp;
using client.messengers.register;
using client.service.ftp.commands;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using server;
using server.model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static System.Environment;

namespace client.service.ftp.client
{
    public class FtpClient : FtpBase
    {
        protected override string SocketPath => "ftpserver/Execute";
        protected override string RootPath { get { return config.ClientRootPath; } }

        protected string CurrentPath { get { return config.ClientCurrentPath; } }

        public FtpClient(ServiceProvider serviceProvider, MessengerSender messengerSender, Config config, IClientInfoCaching clientInfoCaching, RegisterStateInfo registerState, IPunchHoleTcp punchHoleTcp, IDataTunnelRegister dataTunnelRegister)
            : base(serviceProvider, messengerSender, config, clientInfoCaching, registerState, punchHoleTcp, dataTunnelRegister)
        {
        }

        public void LoadPlugins(Assembly[] assemblys)
        {
            LoadPlugins(assemblys, typeof(IFtpCommandClientPlugin));
        }

        public void SetCurrentPath(string path)
        {
            config.ClientRootPath = config.ClientCurrentPath = path;
        }
        public IEnumerable<FileInfo> LocalList(string path)
        {
            if (string.IsNullOrWhiteSpace(config.ClientCurrentPath))
            {
                config.ClientRootPath = config.ClientCurrentPath = GetFolderPath(SpecialFolder.Desktop);
                if (string.IsNullOrWhiteSpace(config.ClientCurrentPath))
                {
                    config.ClientRootPath = config.ClientCurrentPath = GetFolderPath(SpecialFolder.CommonDesktopDirectory);
                }
            }
            DirectoryInfo dirInfo = new DirectoryInfo(Path.Combine(config.ClientCurrentPath, path));
            if (dirInfo.FullName.Length < config.ClientRootPath.Length)
            {
                dirInfo = new DirectoryInfo(config.ClientRootPath);
            }
            config.ClientCurrentPath = dirInfo.FullName;


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
                }).Concat(dirInfo.GetFiles().Where(c => (c.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
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
        public SpecialFolderInfo GetSpecialFolders()
        {
            List<SpecialFolder> specialFolders = new()
            {
                SpecialFolder.MyPictures,
                SpecialFolder.MyMusic,
                SpecialFolder.MyVideos,
                SpecialFolder.MyDocuments,
                SpecialFolder.Desktop
            };
            string desktop = GetFolderPath(SpecialFolder.Desktop);
            if (string.IsNullOrWhiteSpace(desktop))
            {
                desktop = GetFolderPath(SpecialFolder.CommonDesktopDirectory);
            }

            List<SpecialFolderInfo> child = new()
            {
                new SpecialFolderInfo
                {
                    Name = "this Computer",
                    Child = specialFolders.Select(c =>
                    {
                        string path = GetFolderPath(c);
                        return new SpecialFolderInfo()
                        {
                            FullName = path,
                            Name = Path.GetFileName(path)
                        };
                    }).Concat(DriveInfo.GetDrives().Where(c => c.IsReady).Select(c => new SpecialFolderInfo
                    {
                        FullName = c.Name,
                        Name = string.IsNullOrWhiteSpace(c.VolumeLabel) ? $"磁盘({ c.Name.Substring(0, 2)})" : c.VolumeLabel
                    })).ToArray()
                }
            };
            child.AddRange(new DirectoryInfo(desktop).GetDirectories().Select(c => new SpecialFolderInfo
            {
                FullName = c.FullName,
                Name = c.Name,
            }));

            return new SpecialFolderInfo
            {
                Name = Path.GetFileName(desktop),
                FullName = desktop,
                Child = child.ToArray()
            };
        }
        public List<string> Create(string path)
        {
            return Create(CurrentPath, path);
        }
        public List<string> Delete(string path)
        {
            return Delete(CurrentPath, path);
        }
        public async ValueTask OnFile(FtpFileCommand cmd, FtpPluginParamWrap data)
        {
            await OnFile(CurrentPath, cmd, data);
        }
        public async Task Upload(string path, ClientInfo client)
        {
            await Upload(CurrentPath, path, client);
        }

        public async Task<FileInfo[]> RemoteList(string path, ClientInfo client)
        {
            var response = await SendReplyTcp(new FtpListCommand { Path = path }, client);
            if (response.Code == MessageResponeCodes.OK)
            {
                FtpResultInfo result = FtpResultInfo.FromBytes(response.Data);
                if (result.Code == FtpResultInfo.FtpResultCodes.OK)
                {
                    return result.ReadData.DeBytes<FileInfo[]>();
                }
            }
            return Array.Empty<FileInfo>();
        }
        public async Task<bool> Download(string path, ClientInfo client)
        {
            var response = await SendReplyTcp(new FtpDownloadCommand { Path = path }, client);
            return response.Code == MessageResponeCodes.OK;
        }
    }

    public class SpecialFolderInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public SpecialFolderInfo[] Child { get; set; } = Array.Empty<SpecialFolderInfo>();
    }


}
