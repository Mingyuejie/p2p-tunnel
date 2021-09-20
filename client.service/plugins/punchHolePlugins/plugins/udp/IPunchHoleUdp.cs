using common;
using ProtoBuf;
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
        public void SendStep1(ConnectParams param);
        public SimplePushSubHandler<OnStep1EventArg> OnStep1Handler { get; }
        public void OnStep1(OnStep1EventArg arg);

        public void SendStep2(SendStep2EventArg arg);
        public SimplePushSubHandler<OnStep2EventArg> OnStep2Handler { get; }
        public void OnStep2(OnStep2EventArg e);

        public void SendStep3(SendStep3EventArg arg);
        public SimplePushSubHandler<OnStep3EventArg> OnStep3Handler { get; }
        public void OnStep3(OnStep3EventArg e);

        public void SendStep4(SendStep4EventArg arg);

        public SimplePushSubHandler<OnStep4EventArg> OnStep4Handler { get; }
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
        public PunchHoleNotifyModel Data { get; set; }
    }

    public class OnStep2EventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public PunchHoleNotifyModel Data { get; set; }
    }

    public class SendStep2EventArg : EventArgs
    {
        /// <summary>
        /// 目标地址
        /// </summary>
        public IPEndPoint Address { get; set; }
        /// <summary>
        /// 我的id
        /// </summary>
        public long ToId { get; set; }
    }

    public class SendStep3EventArg : EventArgs
    {
        /// <summary>
        /// 目标地址
        /// </summary>
        public IPEndPoint Address { get; set; }
        /// <summary>
        /// 我的id
        /// </summary>
        public long Id { get; set; }
    }

    public class OnStep3EventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public Step3Model Data { get; set; }
    }

    public class SendStep4EventArg : EventArgs
    {
        /// <summary>
        /// 目标地址
        /// </summary>
        public IPEndPoint Address { get; set; }
        /// <summary>
        /// 我的id
        /// </summary>
        public long Id { get; set; }
    }

    public class OnStep4EventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public Step4Model Data { get; set; }
    }

    #endregion


    public class ConnectParams
    {
        public long Id { get; set; } = 0;
        public int TryTimes { get; set; } = 10;
        public string Name { get; set; } = string.Empty;
        public int Timeout { get; set; } = 5 * 1000;
        public Action<OnStep4EventArg> Callback { get; set; } = null;
        public Action<ConnectFailModel> FailCallback { get; set; } = null;
    }

    public class ConnectCache
    {
        public long Time { get; set; } = 0;
        public int TryTimes { get; set; } = 10;
        public int Timeout { get; set; } = 5 * 1000;
        public Action<OnStep4EventArg> Callback { get; set; } = null;
        public Action<ConnectFailModel> FailCallback { get; set; } = null;
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

    public enum PunchHoleUdpSteps : short
    {
        STEP_1 = 1,
        STEP_2 = 2,
        STEP_2_1 = 3,
        STEP_3 = 4,
        STEP_4 = 5,
    }

    [ProtoContract]
    public class Step1Model : IPunchHoleMessageBase
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1)]
        public long FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.UDP;

        [ProtoMember(3, IsRequired = true)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [ProtoMember(4)]
        public short PunchStep { get; } = (short)PunchHoleUdpSteps.STEP_1;
    }

    [ProtoContract]
    public class Step2Model : IPunchHoleMessageBase
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1)]
        public long FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.UDP;

        [ProtoMember(3, IsRequired = true)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [ProtoMember(4)]
        public short PunchStep { get; } = (short)PunchHoleUdpSteps.STEP_2;
    }

    [ProtoContract]
    public class Step21Model : IPunchHoleMessageBase
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1)]
        public long FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.UDP;

        [ProtoMember(3, IsRequired = true)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [ProtoMember(4)]
        public short PunchStep { get; } = (short)PunchHoleUdpSteps.STEP_2_1;
    }

    [ProtoContract]
    public class Step3Model : IPunchHoleMessageBase
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1)]
        public long FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.UDP;

        [ProtoMember(3, IsRequired = true)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [ProtoMember(4)]
        public short PunchStep { get; } = (short)PunchHoleUdpSteps.STEP_3;
    }

    [ProtoContract]
    public class Step4Model : IPunchHoleMessageBase
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1)]
        public long FromId { get; set; } = 0;

        [ProtoMember(2, IsRequired = true)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.UDP;

        [ProtoMember(3, IsRequired = true)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [ProtoMember(4)]
        public short PunchStep { get; } = (short)PunchHoleUdpSteps.STEP_4;
    }
}
