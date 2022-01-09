using client.plugins.serverPlugins;
using client.plugins.serverPlugins.clients;
using client.service.ftp.extends;
using client.service.ftp.plugin;
using client.service.ftp.protocol;
using client.service.ftp.server.plugin;
using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic.FileIO;
using server.model;
using server.plugin;
using server.plugins.register.caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using static System.Environment;

namespace client.service.ftp.client
{
    public class FtpClient : FtpBase
    {
        public Dictionary<FtpCommand, IFtpClientPlugin> Plugins { get; } = new Dictionary<FtpCommand, IFtpClientPlugin>();

        protected override string SocketPath => "ftpserver/Execute";
        protected string CurrentPath { get { return config.ClientCurrentPath; } }
        protected override string RootPath { get { return config.ClientRootPath; } }

        private readonly ServiceProvider serviceProvider;

        public FtpClient(ServiceProvider serviceProvider, IServerRequest serverRequest, Config config, IClientInfoCaching clientInfoCaching)
            : base(serverRequest, config, clientInfoCaching)
        {
            this.serviceProvider = serviceProvider;
        }

        public void LoadPlugins(Assembly[] assemblys)
        {
            IEnumerable<Type> types = assemblys
               .SelectMany(c => c.GetTypes())
               .Where(c => c.GetInterfaces().Contains(typeof(IFtpClientPlugin)));
            foreach (Type item in types)
            {
                IFtpClientPlugin obj = (IFtpClientPlugin)serviceProvider.GetService(item);
                if (!Plugins.ContainsKey(obj.Cmd))
                {
                    Plugins.TryAdd(obj.Cmd, obj);
                }
            }
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
        public async Task OnFile(FtpFileCommand cmd, FtpPluginParamWrap data)
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
            if (response.Code == MessageResponeCode.OK)
            {
                FtpResultModel result = FtpResultModel.FromBytes(response.Data);
                if (result.Code == FtpResultModel.FtpResultCodes.OK)
                {
                    return result.ReadData.DeBytes<FileInfo[]>();
                }
            }
            return Array.Empty<FileInfo>();
        }
        public async Task<bool> Download(string path, ClientInfo client)
        {
            var response = await SendReplyTcp(new FtpDownloadCommand { Path = path }, client);
            return response.Code == MessageResponeCode.OK;
        }
    }

    public class SpecialFolderInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public SpecialFolderInfo[] Child { get; set; } = Array.Empty<SpecialFolderInfo>();
    }

    public class FtpClientPlugin : IPlugin
    {
        private readonly FtpClient ftpClient;
        private readonly IClientInfoCaching clientInfoCaching;
        public FtpClientPlugin(FtpClient ftpClient, IClientInfoCaching clientInfoCaching)
        {
            this.ftpClient = ftpClient;
            this.clientInfoCaching = clientInfoCaching;
        }

        public async Task<byte[]> Execute(PluginParamWrap data)
        {
            FtpCommandBase cmd = ftpClient.ReadAttribute(data.Wrap.Memory);
            FtpPluginParamWrap wrap = new FtpPluginParamWrap
            {
                Packet = data.Packet,
                Connection = data.Connection,
                Wrap = data.Wrap,
                Data = cmd.Data
            };
            if (clientInfoCaching.Get(data.Connection.ConnectId, out ClientInfo client))
            {
                wrap.Client = client;
                if (ftpClient.Plugins.ContainsKey(cmd.Cmd))
                {
                    try
                    {
                        FtpResultModel res = await ftpClient.Plugins[cmd.Cmd].Execute(wrap);
                        if (res != null)
                        {
                            return res.ToBytes();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error(ex);
                        return new FtpResultModel
                        {
                            Code = FtpResultModel.FtpResultCodes.UNKNOW
                        }.ToBytes();
                    }
                }
            }
            return null;
        }
    }
}
