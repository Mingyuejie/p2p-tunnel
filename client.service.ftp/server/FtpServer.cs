using client.plugins.serverPlugins;
using client.service.ftp.plugin;
using client.service.ftp.protocol;
using client.service.ftp.server.plugin;
using common;
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

namespace client.service.ftp.server
{
    public class FtpServer
    {
        public Dictionary<FtpCommand, IFtpServerPlugin> Plugins { get; } = new Dictionary<FtpCommand, IFtpServerPlugin>();
        private readonly ConcurrentDictionary<long, FileSaveInfo> files = new();
        private readonly ConcurrentDictionary<long, string> sends = new();
        private long fileId = 0;

        private readonly ServiceProvider serviceProvider;
        private readonly IServerRequest serverRequest;
        private readonly Config config;
        public FtpServer(ServiceProvider serviceProvider, IServerRequest serverRequest, Config config)
        {
            this.serviceProvider = serviceProvider;
            this.serverRequest = serverRequest;
            this.config = config;

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (!files.IsEmpty)
                    {
                        foreach (var item in files.Values)
                        {
                            SendProgress(item);
                        }
                    }
                    System.Threading.Thread.Sleep(100);
                }
            }, TaskCreationOptions.LongRunning);
        }

        public void LoadPlugins(Assembly[] assemblys)
        {
            var types = assemblys
               .SelectMany(c => c.GetTypes())
               .Where(c => c.GetInterfaces().Contains(typeof(IFtpServerPlugin)));
            foreach (var item in types)
            {
                IFtpServerPlugin obj = (IFtpServerPlugin)serviceProvider.GetService(item);
                if (!Plugins.ContainsKey(obj.Cmd))
                {
                    Plugins.TryAdd(obj.Cmd, obj);
                }
            }
        }

        public void Upload(string path, Socket socket)
        {
            foreach (var item in path.Split(','))
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    var filepath = Path.Combine(config.ServerCurrentPath, item);
                    if (Directory.Exists(filepath))
                    {
                        List<FileUploadInfo> files = new();
                        GetFiles(files, new DirectoryInfo(filepath));

                        var paths = files.Where(c => c.Type == FileType.Folder)
                            .Select(c => c.Path.Replace(config.ServerCurrentPath, "").TrimStart(Path.DirectorySeparatorChar));
                        if (paths.Any())
                        {
                            SendOnlyTcp(new FtpCreateCommand { Path = string.Join(",", paths) }, socket);
                        }
                        foreach (var file in files.Where(c => c.Type == FileType.File))
                        {
                            _Upload(file.Path, config.ServerCurrentPath, socket);
                        }
                    }
                    else if (File.Exists(filepath))
                    {
                        _Upload(filepath, config.ServerCurrentPath, socket);
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
                    sends.TryAdd(cmd.Md5, file.FullName);

                    int packSize = 1024; //每个包大小 
                    int packCount = (int)(file.Length / packSize);
                    long lastPackSize = file.Length - packCount * packSize;

                    using FileStream fs = file.OpenRead();
                    int index = 0;
                    while (index < packCount)
                    {
                        if (!sends.ContainsKey(cmd.Md5))
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

        public bool OnFile(FtpFileCommand cmd, Socket socket)
        {
            files.TryGetValue(cmd.Md5, out FileSaveInfo fs);
            if (fs == null)
            {
                string fullPath = Path.Combine(config.ServerCurrentPath, cmd.Name);
                string cacheFullPath = Path.Combine(config.ServerCurrentPath, $"{cmd.Name}.downloading");

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
                    Socket = socket,
                    IndexLength = 0,
                    TotalLength = cmd.Size,
                    FileName = fullPath,
                    CacheFileName = cacheFullPath,
                    Md5 = cmd.Md5
                };
                files.TryAdd(cmd.Md5, fs);
            }
            fs.Stream.Write(cmd.Data);
            fs.IndexLength += cmd.Data.Length;

            if (fs.IndexLength >= cmd.Size)
            {
                files.TryRemove(cmd.Md5, out _);
                fs.Stream.Close();
                new System.IO.FileInfo(fs.CacheFileName).MoveTo(fs.FileName);

                SendProgress(fs);
                return true;
            }
            return false;
        }
        private void SendProgress(FileSaveInfo save)
        {
            SendOnlyTcp(new FtpFileProgressCommand { Index = save.IndexLength, Md5 = save.Md5 }, save.Socket);
        }

        public void OnSendFileEnd(FtpFileEndCommand cmd)
        {
            sends.TryRemove(cmd.Md5, out _);
        }

        public void SendOnlyTcp<IFtpCommandBase>(IFtpCommandBase data, Socket socket)
        {
            serverRequest.SendOnlyTcp(new SendTcpEventArg<IFtpCommandBase>
            {
                Data = data,
                Path = "ftpclient/excute",
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
        public FileStream Stream { get; set; }
        public Socket Socket { get; set; }
        public long Md5 { get; set; }
        public long TotalLength { get; set; }
        public long IndexLength { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string CacheFileName { get; set; } = string.Empty;
    }

    public class FtpServerPlugin : IPlugin
    {
        private readonly FtpServer ftpServer;
        private readonly Config config;
        public FtpServerPlugin(FtpServer ftpServer, Config config)
        {
            this.ftpServer = ftpServer;
            this.config = config;
        }

        public object Excute(PluginParamWrap data)
        {
            if (!config.Enable)
            {
                data.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, "服务未启用");
            }
            else
            {
                FtpCommandBase cmd = data.Wrap.Content.DeBytes<FtpCommandBase>();
                if (ftpServer.Plugins.ContainsKey(cmd.Cmd))
                {
                    try
                    {
                        return ftpServer.Plugins[cmd.Cmd].Excute(data);
                    }
                    catch (Exception ex)
                    {
                        data.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, ex.Message);
                    }
                }
            }

            return null;
        }
    }
}
