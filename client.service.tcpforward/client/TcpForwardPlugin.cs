using client.servers.clientServer;
using common.extends;
using System;
using System.Collections.Generic;
using System.IO;

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

        public object LoadSetting()
        {
            return tcpForwardSettingModel;
        }

        public string SaveSetting(string jsonStr)
        {
            TcpForwardSettingModel model = jsonStr.DeJson<TcpForwardSettingModel>();
            model.SaveConfig();
            return string.Empty;
        }

        public void Add(ClientServicePluginExcuteWrap arg)
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

        public void Del(ClientServicePluginExcuteWrap arg)
        {
            ForwardSettingModel model = arg.Content.DeJson<ForwardSettingModel>();
            string errmsg = tcpForwardHelper.Del(model.ID);
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(-1, errmsg);
            }
        }

        public List<TcpForwardRecordBaseModel> List(ClientServicePluginExcuteWrap arg)
        {
            return tcpForwardHelper.Mappings;
        }

        public void Start(ClientServicePluginExcuteWrap arg)
        {
            ForwardSettingModel model = arg.Content.DeJson<ForwardSettingModel>();
            string errmsg = tcpForwardHelper.Start(model.ID);
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(-1, errmsg);
            }
        }

        public void Stop(ClientServicePluginExcuteWrap arg)
        {
            ForwardSettingModel model = arg.Content.DeJson<ForwardSettingModel>();
            tcpForwardHelper.Stop(model.ID);
        }

        public bool SwitchEnable(bool enable)
        {
            tcpForwardSettingModel.Enable = enable;
            tcpForwardSettingModel.SaveConfig();
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

        public static TcpForwardSettingModel ReadConfig()
        {
            return FromFile<TcpForwardSettingModel>("tcpforward-appsettings.json") ?? new TcpForwardSettingModel();
        }

        public void SaveConfig()
        {
            TcpForwardSettingModel config = ReadConfig();

            config.PortBlackList = PortBlackList;
            config.PortWhiteList = PortWhiteList;
            config.Enable = Enable;
            config.ReceiveBufferSize = ReceiveBufferSize;
            config.NumConnections = NumConnections;

            ToFile(config,"tcpforward-appsettings.json");
        }
    }
}
