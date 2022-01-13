using common;
using MessagePack;
using ProtoBuf;
using server;
using server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace client.service.plugins.punchHolePlugins.plugins.udp
{
    public interface IPunchHoleUdp
    {
        public Task<ConnectResultModel> Send(ConnectParams param);
        public SimpleSubPushHandler<OnStep1EventArg> OnStep1Handler { get; }
        public Task OnStep1(OnStep1EventArg arg);

        public SimpleSubPushHandler<OnStep2EventArg> OnStep2Handler { get; }
        public Task OnStep2(OnStep2EventArg e);

        public SimpleSubPushHandler<OnStep2FailEventArg> OnStep2FailHandler { get; }
        public void OnStep2Fail(OnStep2FailEventArg e);

        public SimpleSubPushHandler<OnStep3EventArg> OnStep3Handler { get; }
        public Task OnStep3(OnStep3EventArg e);

        public SimpleSubPushHandler<OnStep4EventArg> OnStep4Handler { get; }
        public void OnStep4(OnStep4EventArg arg);
    }


    #region 连接客户端model

    public class SendPunchHoleEventArg : EventArgs
    {
        public string Name { get; set; }
        public long Id { get; set; }
    }

    public class OnStep1EventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public PunchHoleModel RawData { get; set; }
        public PunchHoleNotifyModel Data { get; set; }
    }

    public class OnStep2EventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public PunchHoleModel RawData { get; set; }
        public PunchHoleNotifyModel Data { get; set; }
    }
    public class OnStep2FailEventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public PunchHoleModel RawData { get; set; }
        public PunchHoleNotifyModel Data { get; set; }
    }

    public class SendStep2EventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public ulong ToId { get; set; }
    }

    public class SendStep3EventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public ulong Id { get; set; }
    }

    public class OnStep3EventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public PunchHoleModel RawData { get; set; }
        public Step3Model Data { get; set; }
    }

    public class SendStep4EventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public ulong Id { get; set; }
    }

    public class OnStep4EventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public PunchHoleModel RawData { get; set; }
        public Step4Model Data { get; set; }
    }

    #endregion


    public class ConnectParams
    {
        public ulong Id { get; set; } = 0;
        public int TryTimes { get; set; } = 5;
        public string Name { get; set; } = string.Empty;
    }

    public class ConnectCache
    {
        public long Time { get; set; } = 0;
        public int TryTimes { get; set; } = 5;
        public TaskCompletionSource<ConnectResultModel> Tcs { get; set; }
    }

    public class ConnectResultModel
    {
        public bool State { get; set; }
        public object Result { get; set; }
    }

    public class ConnectFailModel
    {
        public ConnectFailType Type { get; set; } = ConnectFailType.ERROR;
        public string Msg { get; set; } = string.Empty;
    }

    public enum ConnectFailType
    {
        ERROR, TIMEOUT
    }

    public enum PunchHoleUdpSteps : byte
    {
        STEP_1 = 1,
        STEP_2 = 2,
        STEP_2_1 = 3,
        STEP_2_Fail = 4,
        STEP_3 = 5,
        STEP_4 = 6,
    }

    [ProtoContract, MessagePackObject]
    public class Step1Model : IPunchHoleMessageBase
    {

        [ProtoMember(1, IsRequired = true), Key(1)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.UDP;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [ProtoMember(3), Key(3)]
        public byte PunchStep { get; } = (byte)PunchHoleUdpSteps.STEP_1;
    }

    [ProtoContract, MessagePackObject]
    public class Step2Model : IPunchHoleMessageBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.UDP;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [ProtoMember(3), Key(3)]
        public byte PunchStep { get; } = (byte)PunchHoleUdpSteps.STEP_2;
    }

    [ProtoContract, MessagePackObject]
    public class Step21Model : IPunchHoleMessageBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.UDP;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [ProtoMember(3), Key(3)]
        public byte PunchStep { get; } = (byte)PunchHoleUdpSteps.STEP_2_1;
    }
    [ProtoContract, MessagePackObject]
    public class Step2FailModel : IPunchHoleMessageBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.UDP;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [ProtoMember(3), Key(3)]
        public byte PunchStep { get; } = (byte)PunchHoleUdpSteps.STEP_2_Fail;
    }

    [ProtoContract, MessagePackObject]
    public class Step3Model : IPunchHoleMessageBase
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1), Key(1)]
        public ulong FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.UDP;

        [ProtoMember(3, IsRequired = true), Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [ProtoMember(4), Key(4)]
        public byte PunchStep { get; } = (byte)PunchHoleUdpSteps.STEP_3;
    }

    [ProtoContract, MessagePackObject]
    public class Step4Model : IPunchHoleMessageBase
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1), Key(1)]
        public ulong FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true), Key(2)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.UDP;

        [ProtoMember(3, IsRequired = true), Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [ProtoMember(4), Key(4)]
        public byte PunchStep { get; } = (byte)PunchHoleUdpSteps.STEP_4;
    }
}
