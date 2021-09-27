using client.plugins.serverPlugins;
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
        public Dictionary<FtpCommand, IFtpClientPlugin> Plugins { get; } = new Dictionary<FtpCommand, IFtpClientPlugin>();
        public ConcurrentDictionary<long, FileSaveInfo> Downloads { get; } = new();
        public ConcurrentDictionary<long, FileSaveInfo> Uploads { get; } = new();

        private long fileId = 0;

        private readonly ServiceProvider serviceProvider;
        private readonly IServerRequest serverRequest;
        private readonly Config config;
        public FtpClient(ServiceProvider serviceProvider, IServerRequest serverRequest, Config config)
        {
            this.serviceProvider = serviceProvider;
            this.serverRequest = serverRequest;
            this.config = config;

            //Task.Factory.StartNew(() =>
            //{
            //    while (true)
            //    {
            //        if (!Downloads.IsEmpty)
            //        {
            //            foreach (var item in Downloads.Values)
            //            {
            //                SendProgress(item);
            //            }
            //        }
            //        System.Threading.Thread.Sleep(100);
            //    }
            //}, TaskCreationOptions.LongRunning);
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

        public bool OnFile(FtpFileCommand cmd, Socket socket)
        {
            Downloads.TryGetValue(cmd.Md5, out FileSaveInfo fs);
            if (fs == null)
            {
                string fullPath = Path.Combine(config.ClientCurrentPath, cmd.Name);
                string cacheFullPath = Path.Combine(config.ClientCurrentPath, $"{cmd.Name}.downloading");

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                if (File.Exists(cacheFullPath))
                {
                    File.Delete(cacheFullPath);
                }

                var stream = new FileStream(cacheFullPath, FileMode.Create & FileMode.Append, FileAccess.Write);
                stream.Seek(cmd.Size - 1, SeekOrigin.Begin);
                stream.WriteByte(new byte());
                stream.Seek(0, SeekOrigin.Begin);
                fs = new FileSaveInfo
                {
                    Stream = stream,
                    IndexLength = 0,
                    TotalLength = cmd.Size,
                    FileName = fullPath,
                    CacheFileName = cacheFullPath,
                    Socket = socket,
                    Md5 = cmd.Md5
                };
                Downloads.TryAdd(cmd.Md5, fs);
            }
            fs.Stream.Write(cmd.Data);
            fs.IndexLength += cmd.Data.Length;

            if (fs.IndexLength >= cmd.Size)
            {
                Downloads.TryRemove(cmd.Md5, out _);
                fs.Stream.Close();
                new System.IO.FileInfo(fs.CacheFileName).MoveTo(fs.FileName);
                //SendProgress(fs);
                return true;
            }
            return false;
        }
        public void Upload(string path, Socket socket)
        {
            foreach (var item in path.Split(','))
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    var filepath = Path.Combine(config.ClientCurrentPath, item);
                    if (Directory.Exists(filepath))
                    {
                        List<FileUploadInfo> files = new();
                        GetFiles(files, new DirectoryInfo(filepath));

                        var paths = files.Where(c => c.Type == FileType.Folder)
                            .Select(c => c.Path.Replace(config.ClientCurrentPath, "").TrimStart(Path.DirectorySeparatorChar));
                        if (paths.Any())
                        {
                            RemoteCreate(string.Join(",", paths), socket).Wait();
                        }
                        foreach (var file in files.Where(c => c.Type == FileType.File))
                        {
                            _Upload(file.Path, config.ClientCurrentPath, socket);
                        }
                    }
                    else if (File.Exists(filepath))
                    {
                        _Upload(filepath, config.ClientCurrentPath, socket);
                    }
                }
            }
        }
        private void GetFiles(List<FileUploadInfo> files, DirectoryInfo path)
        {
            files.Add(new FileUploadInfo { Path = path.FullName, Type = FileType.Folder });
            foreach (var dir in path.GetDirectories())
            {
                GetFiles(files, dir);
            }
            files.AddRange(path.GetFiles().Select(c => new FileUploadInfo
            {
                Path = c.FullName,
                Type = FileType.File
            }));
        }
        private void _Upload(string path, string currentPath, Socket socket)
        {
            var file = new System.IO.FileInfo(path);
            Task.Run(() =>
            {
                try
                {
                    System.Threading.Interlocked.Increment(ref fileId);
                    FtpFileCommand cmd = new FtpFileCommand
                    {
                        Md5 = fileId,
                        Size = file.Length,
                        Name = file.FullName.Replace(currentPath, "").TrimStart(Path.DirectorySeparatorChar)
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

                    int index = 0;
                    while (index < packCount)
                    {
                        if (!Uploads.ContainsKey(cmd.Md5))
                        {
                            return;
                        }

                        if (socket != null && socket.Connected)
                        {
                            byte[] data = new byte[packSize];
                            fs.Read(data, 0, packSize);
                            cmd.Data = data;
                            SendOnlyTcp(cmd, socket);
                            index++;
                        }
                        else
                        {
                            System.Threading.Thread.Sleep(10);
                        }
                    }
                    if (lastPackSize > 0)
                    {
                        byte[] data = new byte[lastPackSize];
                        fs.Read(data, 0, (int)lastPackSize);
                        cmd.Data = data;
                        SendOnlyTcp(cmd, socket);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Debug($" Upload {ex.Message}");
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
            path.CreateDir(config.ClientCurrentPath);
        }
        public void LocalDelete(string path)
        {
            path.ClearDir(config.ClientCurrentPath);
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


        private void SendProgress(FileSaveInfo save)
        {
            SendOnlyTcp(new FtpFileProgressCommand { Index = save.IndexLength, Md5 = save.Md5 }, save.Socket);
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

    public class FileUploadInfo
    {
        public string Path { get; set; } = string.Empty;
        public FileType Type { get; set; } = FileType.File;
    }

    public class FileSaveInfo
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public FileStream Stream { get; set; }
        public Socket Socket { get; set; }
        public long Md5 { get; set; }
        public long TotalLength { get; set; }
        public long IndexLength { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string CacheFileName { get; set; } = string.Empty;
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
            FtpCommandBase cmd = data.Wrap.Content.DeBytes<FtpCommandBase>();
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
