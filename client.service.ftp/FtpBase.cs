using client.plugins.serverPlugins;
using client.service.ftp.extends;
using client.service.ftp.protocol;
using client.service.ftp.server.plugin;
using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf;
using server.model;
using server.plugins.register.caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace client.service.ftp
{
    public class FtpBase
    {
        public ConcurrentDictionary<long, ConcurrentDictionary<long, FileSaveInfo>> Downloads { get; } = new();
        public ConcurrentDictionary<long, ConcurrentDictionary<long, FileSaveInfo>> Uploads { get; } = new();
        protected virtual string SocketPath { get; set; }
        protected virtual string RootPath { get; set; }

        private long fileId = 0;

        protected readonly IServerRequest serverRequest;
        public FtpBase(IServerRequest serverRequest, IClientInfoCaching clientInfoCaching)
        {
            this.serverRequest = serverRequest;

            clientInfoCaching.OnTcpOffline.Sub((client) =>
            {
                Downloads.TryRemove(client.IpAddress, out _);
                Downloads.TryRemove(client.IpAddress, out _);
            });

            LoopProgress();
        }

        protected List<string> Create(string currentPath, string path)
        {
            return path.CreateDir(currentPath, RootPath);
        }
        protected List<string> Delete(string currentPath, string path)
        {
            return path.ClearDir(currentPath, RootPath);
        }
        protected void OnFile(string currentPath, FtpFileCommand cmd, PluginParamWrap data)
        {
            long id = data.SourcePoint.ToInt64();
            Downloads.TryGetValue(id, out ConcurrentDictionary<long, FileSaveInfo> ipDic);
            if (ipDic == null)
            {
                ipDic = new ConcurrentDictionary<long, FileSaveInfo>();
                Downloads.TryAdd(id, ipDic);
            }
            ipDic.TryGetValue(cmd.Md5, out FileSaveInfo fs);
            if (fs == null)
            {
                string fullPath = Path.Combine(currentPath, cmd.Name);
                string cacheFullPath = Path.Combine(currentPath, $"{cmd.Name}.downloading");

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
                    Socket = data.TcpSocket,
                    Md5 = cmd.Md5
                };
                ipDic.TryAdd(cmd.Md5, fs);
            }
            fs.Stream.Write(cmd.Data);
            fs.IndexLength += cmd.Data.Length;

            if (fs.IndexLength >= cmd.Size)
            {
                Downloads.TryRemove(cmd.Md5, out _);
                fs.Stream.Close();
                new System.IO.FileInfo(fs.CacheFileName).MoveTo(fs.FileName);
                SendOnlyTcp(new FtpFileEndCommand { Md5 = cmd.Md5 }, data.TcpSocket);
            }
        }
        public void OnFileEnd(FtpFileEndCommand cmd, PluginParamWrap data)
        {
            long id = data.SourcePoint.ToInt64();
            Uploads.TryGetValue(id, out ConcurrentDictionary<long, FileSaveInfo> ipDic);
            if (ipDic != null)
            {
                ipDic.TryRemove(cmd.Md5, out _);
            }
        }
        protected void Upload(string currentPath, string path, Socket socket)
        {
            foreach (var item in path.Split(','))
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    var filepath = Path.Combine(currentPath, item);
                    if (Directory.Exists(filepath))
                    {
                        List<FileUploadInfo> files = new();
                        GetFiles(files, new DirectoryInfo(filepath));

                        var paths = files.Where(c => c.Type == FileType.Folder)
                            .Select(c => c.Path.Replace(currentPath, "").TrimStart(Path.DirectorySeparatorChar));
                        if (paths.Any())
                        {
                            RemoteCreate(string.Join(",", paths), socket).Wait();
                        }
                        foreach (var file in files.Where(c => c.Type == FileType.File))
                        {
                            _Upload(file.Path, currentPath, socket);
                        }
                    }
                    else if (File.Exists(filepath))
                    {
                        _Upload(filepath, currentPath, socket);
                    }
                }
            }
        }

        private void _Upload(string path, string currentPath, Socket socket)
        {
            var file = new System.IO.FileInfo(path);
            Task.Run(() =>
            {
                try
                {
                    long ip = BitConverter.ToInt64((socket.RemoteEndPoint as IPEndPoint).Address.GetAddressBytes());
                    Uploads.TryGetValue(ip, out ConcurrentDictionary<long, FileSaveInfo> ipDic);
                    if (ipDic == null)
                    {
                        ipDic = new ConcurrentDictionary<long, FileSaveInfo>();
                        Uploads.TryAdd(ip, ipDic);
                    }

                    System.Threading.Interlocked.Increment(ref fileId);
                    FtpFileCommand cmd = new FtpFileCommand
                    {
                        Md5 = fileId,
                        Size = file.Length,
                        Name = file.FullName.Replace(currentPath, "").TrimStart(Path.DirectorySeparatorChar)
                    };
                    ipDic.TryAdd(cmd.Md5, new FileSaveInfo
                    {
                        FileName = file.FullName,
                        IndexLength = 0,
                        TotalLength = file.Length
                    });

                    int packSize = socket.SendBufferSize - 100; //每个包大小 

                    int packCount = (int)(file.Length / packSize);
                    long lastPackSize = file.Length - packCount * packSize;

                    using FileStream fs = file.OpenRead();

                    int index = 0;
                    while (index < packCount)
                    {
                        if (!ipDic.ContainsKey(cmd.Md5))
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
        public void RemoteProgress(FileSaveInfo save)
        {
            SendOnlyTcp(new FtpFileProgressCommand { Index = save.IndexLength, Md5 = save.Md5 }, save.Socket);
        }

        protected void SendOnlyTcp<IFtpCommandBase>(IFtpCommandBase data, Socket socket)
        {
            serverRequest.SendOnlyTcp(new SendTcpEventArg<IFtpCommandBase>
            {
                Data = data,
                Path = SocketPath,
                Socket = socket
            });
        }
        protected async Task<ServerMessageResponeWrap> SendReplyTcp<IFtpCommandBase>(IFtpCommandBase data, Socket socket)
        {
            return await serverRequest.SendReplyTcp(new SendTcpEventArg<IFtpCommandBase>
            {
                Data = data,
                Path = SocketPath,
                Socket = socket
            });
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

        private void LoopProgress()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (!Uploads.IsEmpty)
                    {
                        foreach (var item in Uploads.Values)
                        {
                            if (!item.IsEmpty)
                            {
                                foreach (var f in item.Values)
                                {
                                    RemoteProgress(f);
                                }
                            }
                        }
                    }

                    System.Threading.Thread.Sleep(1000);
                }
            }, TaskCreationOptions.LongRunning);
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

    public struct FileUploadInfo
    {
        public string Path { get; set; }
        public FileType Type { get; set; }
    }

    public class FileSaveInfo
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public FileStream Stream { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Socket Socket { get; set; }
        public long Md5 { get; set; }
        public long TotalLength { get; set; }
        public long IndexLength { get; set; }
        public string FileName { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string CacheFileName { get; set; } = string.Empty;
    }
}
