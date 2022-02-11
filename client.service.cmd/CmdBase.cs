using client.messengers.clients;
using MessagePack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace client.service.cmd
{
    public class CmdBase
    {
        public ConcurrentDictionary<ulong, Process> dirs { get; } = new();

        public CmdBase(IClientInfoCaching clientInfoCaching)
        {
            //掉线的，删掉目录缓存
            clientInfoCaching.OnOffline.Sub((client) =>
            {
                dirs.TryRemove(client.Id, out _);
            });
        }

        protected async Task<CmdResultInfo> ExecuteCmd(ulong id, string cmd)
        {
            TaskCompletionSource<CmdResultInfo> tcs = new TaskCompletionSource<CmdResultInfo>();

            if (string.IsNullOrWhiteSpace(cmd))
            {
                tcs.SetResult(new CmdResultInfo());
            }

            //每个人的都存一份
            if (!dirs.TryGetValue(id, out Process proc))
            {
                proc = new();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    proc.StartInfo.FileName = "cmd.exe";
                }
                else
                {
                    proc.StartInfo.FileName = "/bin/bash";
                }
                proc.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;

                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.Verb = "runas";

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                dirs.TryAdd(id, proc);
            }

            DataReceivedEventHandler errorCallback = new DataReceivedEventHandler((object sender, DataReceivedEventArgs e) =>
            {

            });
            DataReceivedEventHandler outputCallback = new DataReceivedEventHandler((object sender, DataReceivedEventArgs e) =>
            {
                Console.WriteLine(e.Data);
            });
            proc.OutputDataReceived += outputCallback;
            proc.ErrorDataReceived += errorCallback;

            StringBuilder sb = new StringBuilder("echo cmd_end");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                sb.AppendLine("&& echo %cd%");
            }
            else
            {
                sb.AppendLine("&& pwd");
            }
            proc.StandardInput.WriteLine(cmd);
            proc.StandardInput.WriteLine(sb.ToString());
            proc.StandardInput.AutoFlush = true;

            return await tcs.Task;
        }
    }


    public class RemoteCmdParamsInfo
    {
        public ulong Id { get; set; }
        public string Cmd { get; set; }
    }
    [MessagePackObject]
    public class CmdParamsInfo
    {
        [Key(1)]
        public string Cmd { get; set; }
    }
    [MessagePackObject]
    public class CmdResultInfo
    {
        [Key(1)]
        public string Res { get; set; }

        [Key(2)]
        public string ErrorMsg { get; set; }

        [Key(3)]
        public string WorkDir { get; set; }
    }
}
