using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.ftp.extends
{
    public static class StringExtends
    {
        public static List<string> ClearDir(this string dir, string baseDir, string rootDir = "")
        {
            List<string> errs = new List<string>();
            if (!string.IsNullOrWhiteSpace(dir))
            {
                foreach (var item in dir.Split(','))
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        string filePath = Path.Combine(baseDir, item);
                        if (filePath.StartsWith(rootDir))
                        {
                            Clear(Path.Combine(baseDir, item));
                        }
                        else
                        {
                            errs.Add($"{item} 无目录权限");
                        }
                    }
                }
            }
            return errs;
        }

        public static List<string> CreateDir(this string dir, string baseDir, string rootDir = "")
        {
            List<string> errs = new List<string>();
            if (!string.IsNullOrWhiteSpace(dir))
            {
                foreach (var item in dir.Split(','))
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        string filePath = Path.Combine(baseDir, item);
                        if (filePath.StartsWith(rootDir))
                        {
                            if (!Directory.Exists(filePath))
                            {
                                Directory.CreateDirectory(filePath);
                            }
                        }
                        else
                        {
                            errs.Add($"{item} 无目录权限");
                        }
                    }
                }
            }
            return errs;
        }

        private static void Clear(string path)
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
                    try
                    {
                        FileSystem.DeleteFile(item.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    }
                    catch (Exception)
                    {

                        File.Delete(item.FullName);
                    }
                }
                try
                {
                    FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
                catch (Exception)
                {

                    Directory.Delete(path);
                }
            }
            else if (File.Exists(path))
            {
                try
                {
                    FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
                catch (Exception)
                {

                    File.Delete(path);
                }
            }
        }
    }
}
