using client.servers.clientServer;
using common.extends;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace client.service.tcpforward.client
{
    public class TcpForwardPlugin : IClientServicePlugin, IClientServiceSettingPlugin
    {
        private readonly TcpForwardSettingModel tcpForwardSettingModel;
        private readonly TcpForwardHelper tcpForwardHelper;
        public TcpForwardPlugin(TcpForwardHelper tcpForwardHelper, TcpForwardSettingModel tcpForwardSettingModel)
        {
            this.tcpForwardHelper = tcpForwardHelper;
            this.tcpForwardSettingModel = tcpForwardSettingModel;
        }

        public string Name => "TCP转发";

        public string Author => "snltty";

        public string Desc => "白名单不为空时只允许白名单内端口";

        public bool Enable => tcpForwardSettingModel.Enable;

        public async Task<object> LoadSetting()
        {
            return await Task.FromResult(tcpForwardSettingModel);
        }

        public async Task<string> SaveSetting(string jsonStr)
        {
            TcpForwardSettingModel model = jsonStr.DeJson<TcpForwardSettingModel>();
            await model.SaveConfig();
            return string.Empty;
        }

        public void Add(ClientServicePluginExecuteWrap arg)
        {
            ForwardSettingModel model = arg.Content.DeJson<ForwardSettingModel>();
            TcpForwardRecordBaseModel fmodel = model.Content.DeJson<TcpForwardRecordBaseModel>();
            string errmsg = tcpForwardHelper.Add(new TcpForwardRecordBaseModel
            {
                AliveType = fmodel.AliveType,
                SourceIp = fmodel.SourceIp,
                SourcePort = fmodel.SourcePort,
                TargetIp = fmodel.TargetIp,
                TargetName = fmodel.TargetName,
                TargetPort = fmodel.TargetPort,
                ID = model.ID,
                Desc = fmodel.Desc
            });
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(-1, errmsg);
            }

        }

        public void Del(ClientServicePluginExecuteWrap arg)
        {
            ForwardSettingModel model = arg.Content.DeJson<ForwardSettingModel>();
            string errmsg = tcpForwardHelper.Del(model.ID);
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(-1, errmsg);
            }
        }

        public List<TcpForwardRecordBaseModel> List(ClientServicePluginExecuteWrap arg)
        {
            return tcpForwardHelper.Mappings;
        }

        public void Start(ClientServicePluginExecuteWrap arg)
        {
            ForwardSettingModel model = arg.Content.DeJson<ForwardSettingModel>();
            string errmsg = tcpForwardHelper.Start(model.ID);
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(-1, errmsg);
            }
        }

        public void Stop(ClientServicePluginExecuteWrap arg)
        {
            ForwardSettingModel model = arg.Content.DeJson<ForwardSettingModel>();
            tcpForwardHelper.Stop(model.ID);
        }

        public async Task<bool> SwitchEnable(bool enable)
        {
            tcpForwardSettingModel.Enable = enable;
            await tcpForwardSettingModel.SaveConfig();
            return true;
        }
    }

    public class TcpForwardPushMsgPlugin : IClientServerPushMsgPlugin
    {
        private readonly TcpForwardHelper tcpForwardHelper;
        public TcpForwardPushMsgPlugin(TcpForwardHelper tcpForwardHelper)
        {
            this.tcpForwardHelper = tcpForwardHelper;
        }

        public IEnumerable<TcpForwardRecordBaseModel> List()
        {
            return tcpForwardHelper.Mappings;
        }
    }

    public class ForwardSettingModel
    {
        public int ID { get; set; } = 0;
        public string Content { get; set; } = string.Empty;
    }

    public class TcpForwardSettingModel : SettingModelBase
    {
        public int[] PortWhiteList { get; set; } = Array.Empty<int>();
        public int[] PortBlackList { get; set; } = Array.Empty<int>();

        public bool Enable { get; set; } = true;
        public int ReceiveBufferSize { get; set; } = 1024;
        public int NumConnections { get; set; } = 3000;

        public static async Task<TcpForwardSettingModel> ReadConfig()
        {
            return await FromFile<TcpForwardSettingModel>("tcpforward-appsettings.json") ?? new TcpForwardSettingModel();
        }

        public async Task SaveConfig()
        {
            TcpForwardSettingModel config = await ReadConfig();

            config.PortBlackList = PortBlackList;
            config.PortWhiteList = PortWhiteList;
            config.Enable = Enable;
            config.ReceiveBufferSize = ReceiveBufferSize;
            config.NumConnections = NumConnections;

            await ToFile(config, "tcpforward-appsettings.json");
        }
    }
}
