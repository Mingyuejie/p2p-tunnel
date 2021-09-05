using common.extends;
using ProtoBuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace client.service.p2pPlugins.plugins.fileServer
{

    public class FileServerHelper
    {
        private static readonly Lazy<FileServerHelper> lazy = new(() => new FileServerHelper());
        public static FileServerHelper Instance => lazy.Value;

        private string localCurrentPath = string.Empty;
        private string remoteCurrentPath = AppShareData.Instance.FileServerConfig.Root;

        private readonly ConcurrentDictionary<string, FileSaveInfo> files = new();

        private FileServerHelper()
        {
            FileServerEventHandles.Instance.OnTcpFileFileMessageHandler += OnTcpFileFileMessageHandler;
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
                    FileType = file.FileType.ToString(),
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


        public System.IO.FileInfo GetLocalFile(string path)
        {
            return new System.IO.FileInfo(Path.Combine(localCurrentPath, path));
        }
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

        public FileInfo[] GetLocalFiles(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    localCurrentPath = string.Empty;
                }
                else if (path == "..")
                {
                    localCurrentPath = localCurrentPath.Length <= 3 ? string.Empty : new DirectoryInfo(Path.Combine(localCurrentPath, path)).FullName;
                }
                else
                {
                    localCurrentPath = string.IsNullOrWhiteSpace(localCurrentPath) ? path : new DirectoryInfo(Path.Combine(localCurrentPath, path)).FullName;
                }
                if (string.IsNullOrWhiteSpace(localCurrentPath))
                {
                    return DriveInfo.GetDrives().Where(c => c.IsReady).Select(c => new FileInfo
                    {
                        FullName = c.Name,
                        Length = c.TotalSize,
                        FreeLength = c.TotalFreeSpace,
                        Type = -1,
                        Name = c.VolumeLabel,
                        Image = GetFileIcon(c.VolumeLabel)
                    }).ToArray();
                }
                return GetFiles(localCurrentPath, true);
            }
            catch (Exception)
            {
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
}
