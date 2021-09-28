using client.plugins.serverPlugins;
using client.plugins.serverPlugins.clients;
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
                if (Downloads.TryGetValue(client.Id, out ConcurrentDictionary<long, FileSaveInfo> downloads))
                {
                    downloads.Clear();
                    Downloads.TryRemove(client.Id, out _);
                }
                if (Uploads.TryGetValue(client.SelfId, out ConcurrentDictionary<long, FileSaveInfo> uploads))
                {
                    uploads.Clear();
                    Uploads.TryRemove(client.SelfId, out _);
                }
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
        protected void OnFile(string currentPath, FtpFileCommand cmd, ClientInfo client)
        {
            Downloads.TryGetValue(cmd.SessionId, out ConcurrentDictionary<long, FileSaveInfo> ipDic);
            if (ipDic == null)
            {
                ipDic = new ConcurrentDictionary<long, FileSaveInfo>();
                Downloads.TryAdd(cmd.SessionId, ipDic);
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
                    Client = client,
                    Md5 = cmd.Md5,
                    Time = Helper.GetTimeStamp()
                };
                ipDic.TryAdd(cmd.Md5, fs);
            }
            fs.Stream.Write(cmd.Data);
            fs.IndexLength += cmd.Data.Length;
            fs.Time = Helper.GetTimeStamp();
            if (fs.IndexLength >= cmd.Size)
            {
                ipDic.TryRemove(cmd.Md5, out _);
                fs.Stream.Close();
                fs.Stream.Dispose();
                new System.IO.FileInfo(fs.CacheFileName).MoveTo(fs.FileName);
                SendOnlyTcp(new FtpFileEndCommand { SessionId = client.SelfId, Md5 = cmd.Md5 }, client);

                GC.SuppressFinalize(fs.Stream);
            }
        }
        public void OnUploadFileProgress(FtpFileProgressCommand cmd)
        {
            Uploads.TryGetValue(cmd.SessionId, out ConcurrentDictionary<long, FileSaveInfo> ipDic);
            if (ipDic != null)
            {
                for (int i = 0, len = cmd.Values.Length; i < len; i++)
                {
                    ipDic.TryGetValue(cmd.Values[i].Md5, out FileSaveInfo cache);
                    if (cache != null)
                    {
                        cache.IndexLength = cmd.Values[i].Index;
                    }
                }
            }
        }

        public void OnFileEnd(FtpFileEndCommand cmd)
        {
            Uploads.TryGetValue(cmd.SessionId, out ConcurrentDictionary<long, FileSaveInfo> ipDic);
            if (ipDic != null)
            {
                ipDic.TryRemove(cmd.Md5, out _);
            }
        }
        protected void Upload(string currentPath, string path, ClientInfo client)
        {
            Task.Run(() =>
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
                                RemoteCreate(string.Join(",", paths), client).Wait();
                            }
                            foreach (var file in files.Where(c => c.Type == FileType.File))
                            {
                                _Upload(file.Path, currentPath, client);
                            }
                        }
                        else if (File.Exists(filepath))
                        {
                            _Upload(filepath, currentPath, client);
                        }
                    }
                }
            });
        }

        private void _Upload(string path, string currentPath, ClientInfo client)
        {
            var file = new System.IO.FileInfo(path);
            Task.Run(() =>
            {
                try
                {
                    Uploads.TryGetValue(client.Id, out ConcurrentDictionary<long, FileSaveInfo> ipDic);
                    if (ipDic == null)
                    {
                        ipDic = new ConcurrentDictionary<long, FileSaveInfo>();
                        Uploads.TryAdd(client.Id, ipDic);
                    }

                    System.Threading.Interlocked.Increment(ref fileId);
                    FtpFileCommand cmd = new FtpFileCommand
                    {
                        SessionId = client.SelfId,
                        Md5 = fileId,
                        Size = file.Length,
                        Name = file.FullName.Replace(currentPath, "").TrimStart(Path.DirectorySeparatorChar)
                    };

                    var save = new FileSaveInfo
                    {
                        FileName = file.FullName,
                        IndexLength = 0,
                        TotalLength = file.Length,
                        Time = Helper.GetTimeStamp()
                    };
                    ipDic.TryAdd(cmd.Md5, save);

                    int packSize = client.Socket.SendBufferSize - 100; //每个包大小 

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

                        byte[] data = new byte[packSize];
                        fs.Read(data, 0, packSize);
                        cmd.Data = data;
                        if (!SendOnlyTcp(cmd, client))
                        {
                            ipDic.TryRemove(cmd.Md5, out _);
                        }
                        save.Time = Helper.GetTimeStamp();
                        index++;
                    }
                    if (lastPackSize > 0)
                    {
                        byte[] data = new byte[lastPackSize];
                        fs.Read(data, 0, (int)lastPackSize);
                        cmd.Data = data;
                        save.Time = Helper.GetTimeStamp();
                        SendOnlyTcp(cmd, client);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Debug($" Upload {ex}");
                }
            });
        }

        public async Task<CommonTaskResponseModel<bool>> RemoteCreate(string path, ClientInfo client)
        {
            CommonTaskResponseModel<bool> res = new CommonTaskResponseModel<bool> { Data = true };
            var response = await SendReplyTcp(new FtpCreateCommand { SessionId = client.SelfId, Path = path }, client);
            if (response.Code != ServerMessageResponeCodes.OK)
            {
                res.ErrorMsg = response.ErrorMsg;
                res.Data = false;
            }
            return res;
        }
        public async Task<CommonTaskResponseModel<bool>> RemoteDelete(string path, ClientInfo client)
        {
            CommonTaskResponseModel<bool> res = new CommonTaskResponseModel<bool> { Data = true };
            var response = await SendReplyTcp(new FtpDelCommand { SessionId = client.SelfId, Path = path }, client);
            if (response.Code != ServerMessageResponeCodes.OK)
            {
                res.ErrorMsg = response.ErrorMsg;
                res.Data = false;
            }
            return res;
        }

        protected bool SendOnlyTcp<IFtpCommandBase>(IFtpCommandBase data, ClientInfo client)
        {
            return serverRequest.SendOnlyTcp(new SendTcpEventArg<IFtpCommandBase>
            {
                Data = data,
                Path = SocketPath,
                Socket = client.Socket
            });
        }
        protected async Task<ServerMessageResponeWrap> SendReplyTcp<IFtpCommandBase>(IFtpCommandBase data, ClientInfo client)
        {
            return await serverRequest.SendReplyTcp(new SendTcpEventArg<IFtpCommandBase>
            {
                Data = data,
                Path = SocketPath,
                Socket = client.Socket
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
                    if (!Downloads.IsEmpty)
                    {
                        foreach (var item in Downloads)
                        {
                            if (!item.Value.IsEmpty)
                            {
                                var client = item.Value.Values.FirstOrDefault().Client;
                                SendOnlyTcp(new FtpFileProgressCommand
                                {
                                    SessionId = client.SelfId,
                                    Values = item.Value.Values.Select(c => new FtpFileProgressValue
                                    {
                                        Index = c.IndexLength,
                                        Md5 = c.Md5
                                    }).ToArray()
                                }, client);
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
        public ClientInfo Client { get; set; }
        public long Md5 { get; set; }
        public long TotalLength { get; set; }
        public long IndexLength { get; set; }
        public string FileName { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string CacheFileName { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonIgnore]
        public long Time { get; set; } = 0;
    }

    public class FtpPluginParamWrap : PluginParamWrap
    {
        public ClientInfo Client { get; set; }
    }
}
