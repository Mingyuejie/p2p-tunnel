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

        protected override string SocketPath => "ftpserver/excute";
        protected string CurrentPath { get { return config.ClientCurrentPath; } }
        protected override string RootPath { get { return config.ClientRootPath; } }

        private readonly ServiceProvider serviceProvider;
        private readonly Config config;

        public FtpClient(ServiceProvider serviceProvider, IServerRequest serverRequest, Config config, IClientInfoCaching clientInfoCaching)
            : base(serverRequest, clientInfoCaching)
        {
            this.serviceProvider = serviceProvider;
            this.config = config;
        }

        public void LoadPlugins(Assembly[] assemblys)
        {
            var types = assemblys
               .SelectMany(c => c.GetTypes())
               .Where(c => c.GetInterfaces().Contains(typeof(IFtpClientPlugin)));
            foreach (var item in types)
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
        public SpecialFolderInfo GetSpecialFolders()
        {
            string desktop = GetFolderPath(SpecialFolder.Desktop);
            List<SpecialFolder> specialFolders = new()
            {
                SpecialFolder.MyPictures,
                SpecialFolder.MyMusic,
                SpecialFolder.MyVideos,
                SpecialFolder.MyDocuments,
                SpecialFolder.Desktop
            };

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
        public void OnFile(FtpFileCommand cmd, FtpPluginParamWrap data)
        {
            OnFile(CurrentPath, cmd, data.Client);
        }
        public void Upload(string path, ClientInfo client)
        {
            Upload(CurrentPath, path, client);
        }

        public async Task<CommonTaskResponseModel<FileInfo[]>> RemoteList(string path, ClientInfo client)
        {
            Logger.Instance.Info($" {client.Id}");
            CommonTaskResponseModel<FileInfo[]> res = new CommonTaskResponseModel<FileInfo[]>();
            var response = await SendReplyTcp(new FtpListCommand { SessionId = client.SelfId, Path = path }, client);
            if (response.Code == ServerMessageResponeCodes.OK)
            {
                res.Data = response.Data.DeBytes<FileInfo[]>();
            }
            else
            {
                res.ErrorMsg = response.ErrorMsg;
            }
            return res;
        }
        public async Task<CommonTaskResponseModel<bool>> Download(string path, ClientInfo client)
        {
            CommonTaskResponseModel<bool> res = new CommonTaskResponseModel<bool> { Data = true };
            var response = await SendReplyTcp(new FtpDownloadCommand { SessionId = client.SelfId, Path = path }, client);
            if (response.Code != ServerMessageResponeCodes.OK)
            {
                res.ErrorMsg = response.ErrorMsg;
                res.Data = false;
            }
            return res;
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

        public object Excute(PluginParamWrap data)
        {
            FtpCommandBase cmd = data.Wrap.Content.DeBytes<FtpCommandBase>();

            FtpPluginParamWrap wrap = new FtpPluginParamWrap
            {
                Code = data.Code,
                Packet = data.Packet,
                ServerType = data.ServerType,
                SourcePoint = data.SourcePoint,
                TcpSocket = data.TcpSocket,
                Wrap = data.Wrap
            };
            wrap.SetErrorMessage(data.ErrorMessage);
            if (clientInfoCaching.Get(cmd.SessionId, out ClientInfo client))
            {
                wrap.Client = client;
            }
            if (wrap.Client == null)
            {
                data.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, "未找到来源客户端信息");
            }
            else
            {
                if (ftpClient.Plugins.ContainsKey(cmd.Cmd))
                {
                    try
                    {
                        return ftpClient.Plugins[cmd.Cmd].Excute(wrap);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error(ex + "");
                        data.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, ex.Message);
                    }
                }
            }
            return null;
        }
    }
}
