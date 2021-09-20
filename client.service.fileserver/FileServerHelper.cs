using client.plugins.serverPlugins.clients;
using client.servers.clientServer;
using common.extends;
using ProtoBuf;
using server.plugins.register.caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static System.Environment;

namespace client.service.fileserver
{
    /// <summary>
    /// 流程   
    /// a 请求下载  则 b发送文件给 a  a反馈保存进度给b
    /// a 上传     则 a发送文件给 b  b反馈保存进度
    /// 
    /// 发送文件端为 上传进度 
    /// 保存文件端为 下载进度     FileSaveInfo.FileType
    /// </summary>

    public class FileServerHelper
    {
        private string localCurrentPath = string.Empty;
        private string localRootPath = string.Empty;

        private string remoteCurrentPath = string.Empty;
        private string RemoteCurrentPath
        {
            get => remoteCurrentPath;
            set
            {
                remoteCurrentPath = value;
                //默认显示桌面
                if (string.IsNullOrWhiteSpace(remoteCurrentPath))
                {
                    remoteCurrentPath = GetFolderPath(SpecialFolder.Desktop);
                }
            }
        }

        private readonly ConcurrentDictionary<string, FileSaveInfo> files = new();

        private readonly Config config;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly FileServerEventHandles fileServerEventHandles;

        public FileServerHelper(Config config, IClientInfoCaching clientInfoCaching, FileServerEventHandles fileServerEventHandles)
        {
            this.config = config;
            this.clientInfoCaching = clientInfoCaching;
            this.fileServerEventHandles = fileServerEventHandles;

            RemoteCurrentPath = config.FileServer.Root;
            fileServerEventHandles.OnTcpFile.Sub(OnTcpFile);
            fileServerEventHandles.OnTcpProgress.Sub(OnTcpProgress);
            fileServerEventHandles.OnTcpDownload.Sub(OnTcpDownload);
        }

        private void OnTcpDownload( TcpEventArg<FileServerDownloadModel> e)
        {
            var file = GetRemoteFile(e.Data.Path);
            string md5 = $"{file.FullName}_{e.RawData.FormId}_download".Md5();
            if (!files.ContainsKey(md5))
            {
                files.TryAdd(md5, new FileSaveInfo
                {
                    FileName = file.Name,
                    IndexLength = 0,
                    TotalLength = file.Length,
                    FileType = "DOWNLOAD"
                });
                fileServerEventHandles.SendTcpDownload(new SendTcpEventArg<string>
                {
                    Data = md5,
                    Socket = e.Packet.TcpSocket,
                    ToId = e.RawData.FormId
                }, file);
            }
        }

        private void OnTcpProgress( TcpEventArg<FileServerProgressModel> e)
        {
            if (files.TryGetValue(e.Data.Md5, out FileSaveInfo info))
            {
                if (info != null)
                {
                    info.IndexLength = e.Data.IndexLength;
                    if (info.IndexLength >= info.TotalLength)
                    {
                        files.TryRemove(e.Data.Md5, out _);
                    }
                }
            }
        }

        private void OnTcpFile(TcpEventArg<FileModel> e)
        {
            var file = e.Data;

            string path = file.Type == FileTypes.DOWNLOAD ? localCurrentPath : RemoteCurrentPath;
            if (!Directory.Exists(path))
            {
                _ = Directory.CreateDirectory(path);
            }
            string fullPath = Path.Combine(path, file.Name);

            _ = files.TryGetValue(file.Md5, out FileSaveInfo fs);
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
                    TotalLength = file.Size,
                    FileType = "DOWNLOAD",
                    FileName = file.Name
                };
                _ = files.TryAdd(file.Md5, fs);
            }
            fs.Stream.Write(file.Data);
            fs.IndexLength += file.Data.Length;

            if (fs.Stream.Length >= file.Size)
            {
                _ = files.TryRemove(file.Md5, out _);
                fs.Stream.Close();
            }
            fileServerEventHandles.SendTcpProgress(new SendTcpEventArg<FileServerProgressModel>
            {
                Socket = e.Packet.TcpSocket,
                Data = new FileServerProgressModel { IndexLength = fs.IndexLength, Md5 = file.Md5 },
                ToId = e.RawData.FormId
            });
        }

        public void Start()
        {
            RemoteCurrentPath = config.FileServer.Root;
            config.FileServer.IsStart = true;
        }
        public void Stop()
        {
            config.FileServer.IsStart = false;
        }

        public bool Upload(long toid, string path)
        {
            var socket = GetSocket(toid);
            if (socket != null)
            {
                var file = GetLocalFile(path);
                string md5 = $"{file.FullName}_{toid}_UPLOAD".Md5();
                if (!files.ContainsKey(md5))
                {
                    files.TryAdd(md5, new FileSaveInfo
                    {
                        FileName = file.Name,
                        IndexLength = 0,
                        TotalLength = file.Length,
                        FileType = "UPLOAD"
                    });
                    fileServerEventHandles.SendTcpUpload(new SendTcpEventArg<string>
                    {
                        Data = md5,
                        Socket = socket,
                        ToId = toid
                    }, file);
                    return true;
                }
            }
            return false;
        }

        public bool Download(long toid, string path)
        {
            var socket = GetSocket(toid);
            if (socket != null)
            {
                fileServerEventHandles.SendTcpDownload(new SendTcpEventArg<FileServerDownloadModel>
                {
                    Data = new FileServerDownloadModel { Path = path },
                    Socket = socket,
                    ToId = toid
                });
                return true;
            }
            return false;
        }

        /// <summary>
        /// 请求文件列表
        /// </summary>
        /// <param name="arg"></param>
        public async Task<FileInfo[]> SendTcpFileList(SendTcpEventArg<FileServerListModel> arg)
        {
            arg.Socket = GetSocket(arg.ToId);
            return await fileServerEventHandles.SendTcpFileList(arg);
        }

        /// <summary>
        /// 上传下载中的
        /// </summary>
        /// <returns></returns>
        public IEnumerable<object> GetOnlineList()
        {
            return files.Values.Select(c => new
            {
                c.FileType,
                c.IndexLength,
                c.TotalLength,
                c.FileName
            });
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


        /// <summary>
        /// 根据路径获取本地文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public System.IO.FileInfo GetLocalFile(string path)
        {
            return new System.IO.FileInfo(Path.Combine(localCurrentPath, path));
        }
        /// <summary>
        /// 根据路径获取服务文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public System.IO.FileInfo GetRemoteFile(string path)
        {
            return new System.IO.FileInfo(Path.Combine(RemoteCurrentPath, path));
        }

        public FileInfo[] GetRemoteFiles(string path)
        {
            if (!config.FileServer.IsStart) return Array.Empty<FileInfo>();
            RemoteCurrentPath = Path.Combine(RemoteCurrentPath, path);
            if (new DirectoryInfo(RemoteCurrentPath).FullName.Length < config.FileServer.Root.Length)
            {
                RemoteCurrentPath = config.FileServer.Root;
            }
            return GetFiles(RemoteCurrentPath, false);
        }

        public FileInfo[] GetLocalFiles(string path, bool reset)
        {
            try
            {
                if (reset)
                {
                    localRootPath = localCurrentPath = path;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(localCurrentPath) && !string.IsNullOrWhiteSpace(path))
                    {
                        localCurrentPath = new DirectoryInfo(Path.Combine(localCurrentPath, path)).FullName;
                        if (localCurrentPath.Length < localRootPath.Length)
                        {
                            localCurrentPath = localRootPath;
                        }
                    }
                }
                if (string.IsNullOrWhiteSpace(localCurrentPath))
                {
                    localRootPath = localCurrentPath = GetFolderPath(SpecialFolder.Desktop);
                }
                return GetFiles(localCurrentPath, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + "");
                return Array.Empty<FileInfo>();
            }
        }

        private FileInfo[] GetFiles(string path, bool isIcon)
        {
            if (!Directory.Exists(path)) return Array.Empty<FileInfo>();

            var dir = new DirectoryInfo(path);
            return dir.GetDirectories().Where(c => (c.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden).Select(c => new FileInfo
            {
                CreationTime = c.CreationTime,
                Length = 0,
                FullName = c.FullName,
                Name = c.Name,
                LastWriteTime = c.LastWriteTime,
                LastAccessTime = c.LastAccessTime,
                Type = 0,
                Image = isIcon ? GetDirectoryIcon() : string.Empty
            }).Concat(dir.GetFiles().Where(c => (c.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden).Select(c => new FileInfo
            {
                CreationTime = c.CreationTime,
                Length = c.Length,
                FullName = c.FullName,
                Name = c.Name,
                LastWriteTime = c.LastWriteTime,
                LastAccessTime = c.LastAccessTime,
                Type = 1,
                Image = isIcon ? GetFileIcon(c.Name) : string.Empty
            })).ToArray();
        }

        private Dictionary<string, string> icons = new Dictionary<string, string>();
        public string GetFileIcon(string p_Path)
        {
            string ext = Path.GetExtension(p_Path);
            if (icons.ContainsKey(ext))
            {
                return icons[ext];
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SHFILEINFO _SHFILEINFO = new SHFILEINFO();
                IntPtr _IconIntPtr = SHGetFileInfo(p_Path, 0, ref _SHFILEINFO, (uint)Marshal.SizeOf(_SHFILEINFO), (uint)(SHGFI.SHGFI_ICON | SHGFI.SHGFI_LARGEICON | SHGFI.SHGFI_USEFILEATTRIBUTES));
                if (_IconIntPtr.Equals(IntPtr.Zero)) return null;
                icons.Add(ext, Icon2Base64(System.Drawing.Icon.FromHandle(_SHFILEINFO.hIcon)));
                return icons[ext];
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return string.Empty;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return string.Empty;
            }

            return string.Empty;
        }

        private string dirIcon = string.Empty;
        public string GetDirectoryIcon()
        {
            if (!string.IsNullOrWhiteSpace(dirIcon)) return dirIcon;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SHFILEINFO _SHFILEINFO = new SHFILEINFO();
                IntPtr _IconIntPtr = SHGetFileInfo(@"", 0, ref _SHFILEINFO, (uint)Marshal.SizeOf(_SHFILEINFO), (uint)(SHGFI.SHGFI_ICON | SHGFI.SHGFI_LARGEICON));
                if (_IconIntPtr.Equals(IntPtr.Zero)) return null;
                dirIcon = Icon2Base64(System.Drawing.Icon.FromHandle(_SHFILEINFO.hIcon));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return string.Empty;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return string.Empty;
            }

            return dirIcon;
        }

        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        private string Icon2Base64(System.Drawing.Icon icon)
        {
            using MemoryStream ms = new MemoryStream();
            try
            {
                System.Drawing.Bitmap bmp = icon.ToBitmap();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                byte[] array = new byte[ms.Length];
                ms.Position = 0;
                ms.Read(array, 0, array.Length);

                bmp.Dispose();
                return Convert.ToBase64String(array);
            }
            catch (Exception)
            {
                return string.Empty;
            }
            finally
            {
                ms.Close();
            }
        }

        private Socket GetSocket(long id)
        {
            clientInfoCaching.Get(id, out ClientInfo client);
            return client?.Socket ?? null;
        }

    }


    public class FileServerPushMsgPlugin : IClientServerPushMsgPlugin
    {
        private readonly FileServerHelper fileServerHelper;
        private readonly Config config;
        public FileServerPushMsgPlugin(FileServerHelper fileServerHelper, Config config)
        {
            this.fileServerHelper = fileServerHelper;
            this.config = config;
        }

        public object Online()
        {
            return fileServerHelper.GetOnlineList();
        }
        public object Info()
        {
            return config.FileServer;
        }
    }

    public class FileSaveInfo
    {
        public FileStream Stream { get; set; }
        public long TotalLength { get; set; }
        public long IndexLength { get; set; }
        public string FileType { get; set; } = "DOWNLOAD";
        public string FileName { get; set; } = string.Empty;
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
        public string FullName { get; set; } = string.Empty;
        [ProtoMember(5)]
        public string Name { get; set; } = string.Empty;
        [ProtoMember(6)]
        public long Length { get; set; } = 0;
        [ProtoMember(7)]
        public long FreeLength { get; set; } = 0;
        /// <summary>
        /// // 0文件夹  1文件 -1盘符
        /// </summary>
        [ProtoMember(8)]
        public short Type { get; set; } = 0;
        [ProtoMember(9)]
        public string Image { get; set; } = string.Empty;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SHFILEINFO
    {
        public IntPtr hIcon;
        public IntPtr iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    public enum SHGFI
    {
        SHGFI_ICON = 0x100,
        SHGFI_LARGEICON = 0x0,
        SHGFI_USEFILEATTRIBUTES = 0x10
    }

    public class SpecialFolderInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public SpecialFolderInfo[] Child { get; set; } = Array.Empty<SpecialFolderInfo>();
    }
}
