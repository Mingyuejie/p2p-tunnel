using client.plugins.serverPlugins;
using client.plugins.serverPlugins.clients;
using client.servers.clientServer;
using common.extends;
using ProtoBuf;
using server.model;
using server.plugin;
using server.plugins.register.caching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace client.service.cmd
{
    public class CmdsPlugin : CmdBase, IClientServicePlugin
    {
        private readonly IServerRequest serverRequest;
        private readonly IClientInfoCaching clientInfoCaching;
        public CmdsPlugin(IServerRequest serverRequest, IClientInfoCaching clientInfoCaching)
        {
            this.serverRequest = serverRequest;
            this.clientInfoCaching = clientInfoCaching;
        }
        public CmdResultModel Excute(ClientServicePluginExcuteWrap arg)
        {
            RemoteCmdModel model = arg.Content.DeJson<RemoteCmdModel>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client) && client != null && client.TcpConnected)
            {
                var res = serverRequest.SendReplyTcp(new SendTcpEventArg<CmdModel>
                {
                    Path = "cmd/excute",
                    Socket = client.Socket,
                    Timeout = 0,
                    Data = new CmdModel { Cmd = model.Cmd }
                }).Result;
                if (res.Code == ServerMessageResponeCodes.OK)
                {
                    return res.Data.DeBytes<CmdResultModel>();
                }
                return new CmdResultModel { Err = res.ErrorMsg };
            }
            return ExcuteCmd(model.Cmd);
        }
    }


    public class CmdSettingPlugin : IClientServiceSettingPlugin
    {
        private readonly Config config;
        public CmdSettingPlugin(Config config)
        {
            this.config = config;
        }

        public string Name => "远程命令";

        public string Author => "snltty";

        public string Desc => "执行远程客户端的命令行";

        public object LoadSetting()
        {
            return config;
        }

        public string SaveSetting(string jsonStr)
        {
            Config _config = jsonStr.DeJson<Config>();
            config.Password = _config.Password;
            config.Enable = _config.Enable;
            config.SaveConfig();
            return string.Empty;
        }
    }
    public class CmdPlugin : CmdBase, IPlugin
    {
        private readonly Config config;
        public CmdPlugin(Config config)
        {
            this.config = config;
        }
        public CmdResultModel Excute(PluginParamWrap arg)
        {
            CmdModel cmd = arg.Wrap.Content.DeBytes<CmdModel>();
            if (!config.Enable)
            {
                return new CmdResultModel { Err = "远程命令服务未开启" };
            }
            return ExcuteCmd(cmd.Cmd);
        }


    }
    public class CmdBase
    {
        protected CmdResultModel ExcuteCmd(string cmd)
        {
            Process proc = new();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                proc.StartInfo.FileName = "cmd.exe";
            }
            else
            {
                proc.StartInfo.FileName = "/bin/bash";
            }
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

            return new CmdResultModel
            {
                Err = error,
                Res = res
            };
        }
    }

 
    public class RemoteCmdModel
    {
        public long Id { get; set; }
        public string Cmd { get; set; }
    }
    [ProtoContract]
    public class CmdModel
    {
        [ProtoMember(1)]
        public string Cmd { get; set; }
    }
    [ProtoContract]
    public class CmdResultModel
    {
        [ProtoMember(1)]
        public string Res { get; set; }

        [ProtoMember(2)]
        public string Err { get; set; }
    }
}
