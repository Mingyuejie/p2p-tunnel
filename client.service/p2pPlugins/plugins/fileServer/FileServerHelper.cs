using client.service.serverPlugins.clients;
using common;
using common.extends;
using ProtoBuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Environment;

namespace client.service.p2pPlugins.plugins.fileServer
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
        private static readonly Lazy<FileServerHelper> lazy = new(() => new FileServerHelper());
        public static FileServerHelper Instance => lazy.Value;

        private string localCurrentPath = string.Empty;
        private string localRootPath = string.Empty;
        private string remoteCurrentPath = AppShareData.Instance.FileServerConfig.Root;

        private readonly ConcurrentDictionary<string, FileSaveInfo> files = new();

        private FileServerHelper()
        {
            FileServerEventHandles.Instance.OnTcpFileFileMessageHandler += OnTcpFileFileMessageHandler;
            FileServerEventHandles.Instance.OnTcpFileProgressMessageHandler += OnTcpFileProgressMessageHandler;
            FileServerEventHandles.Instance.OnTcpFileDownloadMessageHandler += OnTcpFileDownloadMessageHandler;
        }

        private void OnTcpFileDownloadMessageHandler(object sender, TcpFileDownloadMessageEventArg e)
        {
            var file = GetLocalFile(e.Data.Path);
            string md5 = Helper.GetMd5Hash($"{file.FullName}_{e.RawData.FormId}_{P2PFileCmdTypes.DOWNLOAD}");
            if (!files.ContainsKey(md5))
            {
                files.TryAdd(md5, new FileSaveInfo
                {
                    FileName = file.Name,
                    IndexLength = 0,
                    TotalLength = file.Length,
                    FileType = P2PFileCmdTypes.UPLOAD.ToString()
                });
                FileServerEventHandles.Instance.SendTcpFileDownloadMessage(new SendTcpFileMessageEventArg<string>
                {
                    Data = md5,
                    Socket = e.Packet.TcpSocket,
                    ToId = e.RawData.FormId
                }, file);
            }
        }

        private void OnTcpFileProgressMessageHandler(object sender, TcpFileProgressMessageEventArg e)
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

        private void OnTcpFileFileMessageHandler(object sender, TcpFileFileMessageEventArg e)
        {
            var file = e.Data;

            string path = file.FileType == P2PFileCmdTypes.DOWNLOAD ? localCurrentPath : remoteCurrentPath;
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
                    FileType = P2PFileCmdTypes.DOWNLOAD.ToString(),
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
            FileServerEventHandles.Instance.SendTcpFileProgressMessage(new SendTcpFileMessageEventArg<P2PFileProgressModel>
            {
                Socket = e.Packet.TcpSocket,
                Data = new P2PFileProgressModel { IndexLength = fs.IndexLength, Md5 = file.Md5 },
                ToId = e.RawData.FormId
            });
        }

        public void Start()
        {
            remoteCurrentPath = AppShareData.Instance.FileServerConfig.Root;
            AppShareData.Instance.FileServerConfig.IsStart = true;
        }
        public void Stop()
        {
            AppShareData.Instance.FileServerConfig.IsStart = false;
        }

        public bool Upload(long toid, string path)
        {
            var socket = GetSocket(toid);
            if (socket != null)
            {
                var file = GetLocalFile(path);
                string md5 = Helper.GetMd5Hash($"{file.FullName}_{toid}_{P2PFileCmdTypes.UPLOAD}");
                if (!files.ContainsKey(md5))
                {
                    files.TryAdd(md5, new FileSaveInfo
                    {
                        FileName = file.Name,
                        IndexLength = 0,
                        TotalLength = file.Length,
                        FileType = P2PFileCmdTypes.UPLOAD.ToString()
                    });
                    FileServerEventHandles.Instance.SendTcpFileUploadMessage(new SendTcpFileMessageEventArg<string>
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
                FileServerEventHandles.Instance.SendTcpFileCmdDownloadMessage(new SendTcpFileMessageEventArg<P2PFileCmdDownloadModel>
                {
                    Data = new P2PFileCmdDownloadModel { Path = path },
                    Socket = socket,
                    ToId = toid
                });
                return true;
            }
            return false;
        }

        public bool RequestRemoteList(long toid, string path, Action<FileInfo[]> callback, Action<string> callback2)
        {
            var socket = GetSocket(toid);
            if (socket != null)
            {
                FileServerEventHandles.Instance.RequestFileListMessage(new SendTcpFileMessageEventArg<P2PFileCmdListModel>
                {
                    ToId = toid,
                    Socket = socket,
                    Data = new P2PFileCmdListModel { Path = path }
                }, (msg) =>
                {
                    for (int i = 0; i < msg.Length; i++)
                    {
                        if (msg[i].Type == 0)
                        {
                            msg[i].Image = GetDirectoryIcon();
                        }
                        else
                        {
                            msg[i].Image = GetFileIcon(msg[i].Name);
                        }
                    }
                    callback(msg);
                }, (msg) =>
                {
                    callback2(msg);
                });
                return true;
            }
            return false;
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
            return new System.IO.FileInfo(Path.Combine(remoteCurrentPath, path));
        }

        public FileInfo[] GetRemoteFiles(string path)
        {
            if (!AppShareData.Instance.FileServerConfig.IsStart) return Array.Empty<FileInfo>();
            remoteCurrentPath = Path.Combine(remoteCurrentPath, path);
            if (new DirectoryInfo(remoteCurrentPath).FullName.Length < AppShareData.Instance.FileServerConfig.Root.Length)
            {
                remoteCurrentPath = AppShareData.Instance.FileServerConfig.Root;
            }
            return GetFiles(remoteCurrentPath, false);
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
            return dir.GetDirectories().Select(c => new FileInfo
            {
                CreationTime = c.CreationTime,
                Length = 0,
                FullName = c.FullName,
                Name = c.Name,
                LastWriteTime = c.LastWriteTime,
                LastAccessTime = c.LastAccessTime,
                Type = 0,
                Image = isIcon ? GetDirectoryIcon() : string.Empty
            }).Concat(dir.GetFiles().Select(c => new FileInfo
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

            SHFILEINFO _SHFILEINFO = new SHFILEINFO();
            IntPtr _IconIntPtr = SHGetFileInfo(p_Path, 0, ref _SHFILEINFO, (uint)Marshal.SizeOf(_SHFILEINFO), (uint)(SHGFI.SHGFI_ICON | SHGFI.SHGFI_LARGEICON | SHGFI.SHGFI_USEFILEATTRIBUTES));
            if (_IconIntPtr.Equals(IntPtr.Zero)) return null;

            icons.Add(ext, Icon2Base64(System.Drawing.Icon.FromHandle(_SHFILEINFO.hIcon)));
            return icons[ext];
        }

        private string dirIcon = string.Empty;
        public string GetDirectoryIcon()
        {
            if (!string.IsNullOrWhiteSpace(dirIcon)) return dirIcon;
            SHFILEINFO _SHFILEINFO = new SHFILEINFO();
            IntPtr _IconIntPtr = SHGetFileInfo(@"", 0, ref _SHFILEINFO, (uint)Marshal.SizeOf(_SHFILEINFO), (uint)(SHGFI.SHGFI_ICON | SHGFI.SHGFI_LARGEICON));
            if (_IconIntPtr.Equals(IntPtr.Zero)) return null;

            dirIcon = Icon2Base64(System.Drawing.Icon.FromHandle(_SHFILEINFO.hIcon));
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
            AppShareData.Instance.Clients.TryGetValue(id, out ClientInfo client);
            return client?.Socket ?? null;
        }

    }

    public class FileSaveInfo
    {
        public FileStream Stream { get; set; }
        public long TotalLength { get; set; }
        public long IndexLength { get; set; }
        public string FileType { get; set; } = P2PFileCmdTypes.DOWNLOAD.ToString();
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
