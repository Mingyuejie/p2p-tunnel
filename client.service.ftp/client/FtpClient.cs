using client.plugins.serverPlugins;
using client.service.ftp.plugin;
using client.service.ftp.protocol;
using client.service.ftp.server.plugin;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using server.model;
using server.plugin;
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
    public class FtpClient
    {
        public Dictionary<FtpCommand, IFtpPlugin> Plugins { get; } = new Dictionary<FtpCommand, IFtpPlugin>();
        public ConcurrentDictionary<string, FileSaveInfo> Downloads { get; } = new();
        public ConcurrentDictionary<string, FileSaveInfo> Uploads { get; } = new();

        private readonly ServiceProvider serviceProvider;
        private readonly IServerRequest serverRequest;
        private readonly Config config;
        public FtpClient(ServiceProvider serviceProvider, IServerRequest serverRequest, Config config)
        {
            this.serviceProvider = serviceProvider;
            this.serverRequest = serverRequest;
            this.config = config;
        }

        public void LoadPlugins(Assembly[] assemblys)
        {
            var types = assemblys
               .SelectMany(c => c.GetTypes())
               .Where(c => c.GetInterfaces().Contains(typeof(IFtpPlugin)));
            foreach (var item in types)
            {
                IFtpPlugin obj = (IFtpPlugin)serviceProvider.GetService(item);
                if (!Plugins.ContainsKey(obj.Cmd))
                {
                    Plugins.TryAdd(obj.Cmd, obj);
                }
            }
        }

        public void OnFile(FtpFileCommand cmd)
        {
            string fullPath = Path.Combine(config.ClientCurrentPath, cmd.Name);
            Downloads.TryGetValue(cmd.Md5, out FileSaveInfo fs);
            if (fs == null)
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                fs = new FileSaveInfo
                {
                    Stream = new FileStream(fullPath, FileMode.Create & FileMode.Append, FileAccess.Write),
                    IndexLength = 0,
                    TotalLength = cmd.Size,
                    FileName = fullPath,
                };
                Downloads.TryAdd(cmd.Md5, fs);
            }
            fs.Stream.Write(cmd.Data);
            fs.IndexLength += cmd.Data.Length;

            if (fs.Stream.Length >= cmd.Size)
            {
                Downloads.TryRemove(cmd.Md5, out _);
                fs.Stream.Close();
            }
        }
        public void Upload(string path, Socket socket)
        {
            var file = new System.IO.FileInfo(Path.Combine(config.ClientCurrentPath, path));
            Task.Run(() =>
            {
                try
                {
                    FtpFileCommand cmd = new FtpFileCommand
                    {
                        Md5 = file.FullName.Md5(),
                        Size = file.Length,
                        Name = file.Name
                    };
                    Uploads.TryAdd(cmd.Md5, new FileSaveInfo
                    {
                        FileName = file.FullName,
                        IndexLength = 0,
                        TotalLength = file.Length
                    });

                    int packSize = 1024; //每个包大小 

                    int packCount = (int)(file.Length / packSize);
                    long lastPackSize = file.Length - packCount * packSize;

                    using FileStream fs = file.OpenRead();
                    for (int index = 0; index < packCount; index++)
                    {
                        byte[] data = new byte[packSize];
                        fs.Read(data, 0, packSize);
                        cmd.Data = data;
                        SendOnlyTcp(cmd, socket);
                    }
                    if (lastPackSize > 0)
                    {
                        byte[] data = new byte[lastPackSize];
                        fs.Read(data, 0, (int)lastPackSize);
                        cmd.Data = data;
                        SendOnlyTcp(cmd, socket);
                    }
                }
                catch (Exception)
                {
                }
            });
        }
        public void OnSendFileEnd(FtpFileEndCommand cmd)
        {
            Uploads.TryRemove(cmd.Md5, out _);
        }


        public void SetCurrentPath(string path)
        {
            config.ClientRootPath = config.ClientCurrentPath = path;
        }
        public IEnumerable<server.plugin.FileInfo> LocalList(string path)
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
                .Select(c => new server.plugin.FileInfo
                {
                    CreationTime = c.CreationTime,
                    Length = 0,
                    Name = c.Name,
                    LastWriteTime = c.LastWriteTime,
                    LastAccessTime = c.LastAccessTime,
                    Type = FileType.Folder,
                }).Concat(dirInfo.GetFiles()
            .Where(c => (c.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
            .Select(c => new server.plugin.FileInfo
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
        public void LocalCreate(string path)
        {
            string filePath = Path.Combine(config.ClientCurrentPath, path);
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
        }
        public void LocalDelete(string path)
        {
            Clear(Path.Combine(config.ClientCurrentPath, path));
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

        public async Task<CommonTaskResponseModel<bool>> RemoteDelete(string path, Socket socket)
        {
            CommonTaskResponseModel<bool> res = new CommonTaskResponseModel<bool> { Data = true };
            var response = await SendReplyTcp(new FtpDelCommand { Path = path }, socket);
            if (response.Code != ServerMessageResponeCodes.OK)
            {
                res.ErrorMsg = response.ErrorMsg;
                res.Data = false;
            }
            return res;
        }
        public async Task<CommonTaskResponseModel<bool>> RemoteCreate(string path, Socket socket)
        {
            CommonTaskResponseModel<bool> res = new CommonTaskResponseModel<bool> { Data = true };
            var response = await SendReplyTcp(new FtpCreateCommand { Path = path }, socket);
            if (response.Code != ServerMessageResponeCodes.OK)
            {
                res.ErrorMsg = response.ErrorMsg;
                res.Data = false;
            }
            return res;
        }
        public async Task<CommonTaskResponseModel<server.plugin.FileInfo[]>> RemoteList(string path, Socket socket)
        {
            CommonTaskResponseModel<server.plugin.FileInfo[]> res = new CommonTaskResponseModel<server.plugin.FileInfo[]>();
            var response = await SendReplyTcp(new FtpListCommand { Path = path }, socket);
            if (response.Code == ServerMessageResponeCodes.OK)
            {
                res.Data = response.Data.DeBytes<server.plugin.FileInfo[]>();
            }
            else
            {
                res.ErrorMsg = response.ErrorMsg;
            }
            return res;
        }
        public async Task<CommonTaskResponseModel<bool>> Download(string path, Socket socket)
        {
            CommonTaskResponseModel<bool> res = new CommonTaskResponseModel<bool> { Data = true };
            var response = await SendReplyTcp(new FtpDownloadCommand { Path = path }, socket);
            if (response.Code != ServerMessageResponeCodes.OK)
            {
                res.ErrorMsg = response.ErrorMsg;
                res.Data = false;
            }
            return res;
        }

        public void SendOnlyTcp<IFtpCommandBase>(IFtpCommandBase data, Socket socket)
        {
            serverRequest.SendOnlyTcp(new SendTcpEventArg<IFtpCommandBase>
            {
                Data = data,
                Path = "ftpserver/excute",
                Socket = socket
            });
        }
        public async Task<ServerMessageResponeWrap> SendReplyTcp<IFtpCommandBase>(IFtpCommandBase data, Socket socket)
        {
            return await serverRequest.SendReplyTcp(new SendTcpEventArg<IFtpCommandBase>
            {
                Data = data,
                Path = "ftpserver/excute",
                Socket = socket
            });
        }
    }

    public class FileSaveInfo
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public FileStream Stream { get; set; }
        public long TotalLength { get; set; }
        public long IndexLength { get; set; }
        public string FileName { get; set; } = string.Empty;
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
        public FtpClientPlugin(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }

        public object Excute(PluginParamWrap data)
        {
            IFtpCommandBase cmd = data.Wrap.Content.DeBytes<IFtpCommandBase>();
            if (ftpClient.Plugins.ContainsKey(cmd.Cmd))
            {
                try
                {
                    return ftpClient.Plugins[cmd.Cmd].Excute(data);
                }
                catch (Exception ex)
                {
                    data.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, ex.Message);
                }
            }
            return null;
        }
    }
}
