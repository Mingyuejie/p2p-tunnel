using client.messengers.clients;
using MessagePack;
using ProtoBuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace client.service.cmd
{
    public class CmdBase
    {
        public ConcurrentDictionary<ulong, string> dirs { get; } = new();

        public CmdBase(IClientInfoCaching clientInfoCaching)
        {
            //掉线的，删掉目录缓存
            clientInfoCaching.OnOffline.Sub((client) =>
            {
                dirs.TryRemove(client.Id, out _);
            });
        }

        protected CmdResultInfo ExecuteCmd(ulong id, string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd))
            {
                return new CmdResultInfo();
            }

            //获取当前目录，每个人的都存一份
            if (!dirs.TryGetValue(id, out string workDir))
            {
                workDir = Directory.GetCurrentDirectory();
                dirs.TryAdd(id, workDir);
            }

            Tuple<string, string> cmdParsed = ParseCmd(cmd);
            if (cmdParsed.Item1 == "cd")
            {
                //处理cd 命令
                return new CmdResultInfo { Res = $"当前目录 {cd(id, cmdParsed.Item2)}" };
            }

            Process proc = new();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                proc.StartInfo.FileName = "cmd.exe";
            }
            else
            {
                proc.StartInfo.FileName = "/bin/bash";
            }
            proc.StartInfo.WorkingDirectory = workDir;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.Verb = "runas";
            proc.Start();

            proc.StandardInput.WriteLine(cmd);
            proc.StandardInput.AutoFlush = true;
            proc.StandardInput.WriteLine("exit");
            string res = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!string.IsNullOrWhiteSpace(res))
                {
                    IEnumerable<string> arr = res.Split(Environment.NewLine);
                    res = string.Join(Environment.NewLine, arr.Skip(4).Take(arr.Count() - 6));
                }
            }

            proc.WaitForExit();
            proc.Close();
            proc.Dispose();

            return new CmdResultInfo
            {
                ErrorMsg = error,
                Res = res
            };
        }

        private string cd(ulong id, string path)
        {
            dirs.TryGetValue(id, out string workDir);
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (Directory.Exists(Path.Combine(workDir, path)))
                {
                    workDir = new DirectoryInfo(Path.Combine(workDir, path)).FullName;
                    dirs.AddOrUpdate(id, workDir, (a, b) => workDir);
                }
            }
            return workDir;
        }

        /// <summary>
        /// 处理行返回 命令 和 参数
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        private Tuple<string, string> ParseCmd(string cmd)
        {
            ReadOnlySpan<char> span = cmd.Trim().AsSpan();
            int index = span.IndexOf(Encoding.ASCII.GetString(new byte[] { 32 })[0]);
            if (index > 0)
            {
                string cmdStr = span.Slice(0, index).ToString().Trim();
                string paramStr = span.Slice(index, span.Length - index).ToString().Trim();
                return new Tuple<string, string>(cmdStr, paramStr);
            }
            else
            {
                return new Tuple<string, string>(span.ToString(), string.Empty);
            }
        }
    }


    public class RemoteCmdParamsInfo
    {
        public ulong Id { get; set; }
        public string Cmd { get; set; }
    }
    [ProtoContract, MessagePackObject]
    public class CmdParamsInfo
    {
        [ProtoMember(1), Key(1)]
        public string Cmd { get; set; }
    }
    [ProtoContract, MessagePackObject]
    public class CmdResultInfo
    {
        [ProtoMember(1), Key(1)]
        public string Res { get; set; }

        [ProtoMember(2), Key(2)]
        public string ErrorMsg { get; set; }
    }
}
