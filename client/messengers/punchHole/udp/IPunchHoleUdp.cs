using common;
using MessagePack;
using server;
using server.model;
using System;
using System.Threading.Tasks;

namespace client.messengers.punchHole.udp
{
    public interface IPunchHoleUdp
    {
        public SimpleSubPushHandler<ConnectUdpParams> OnSendHandler { get; }
        public Task<ConnectUdpResultModel> Send(ConnectUdpParams param);
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
        public IConnection Connection { get; set; }
        public PunchHoleParamsInfo RawData { get; set; }
        public PunchHoleNotifyInfo Data { get; set; }
    }

    public class OnStep2EventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public PunchHoleParamsInfo RawData { get; set; }
        public PunchHoleNotifyInfo Data { get; set; }
    }
    public class OnStep2FailEventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public PunchHoleParamsInfo RawData { get; set; }
        public PunchHoleNotifyInfo Data { get; set; }
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
        public IConnection Connection { get; set; }
        public PunchHoleParamsInfo RawData { get; set; }
        public Step3Model Data { get; set; }
    }

    public class SendStep4EventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public ulong Id { get; set; }
    }

    public class OnStep4EventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public PunchHoleParamsInfo RawData { get; set; }
        public Step4Model Data { get; set; }
    }

    #endregion


    public class ConnectUdpParams
    {
        public ulong Id { get; set; } = 0;
        public int TryTimes { get; set; } = 5;
        public string TunnelName { get; set; } = string.Empty;
    }

    public class ConnectCache
    {
        public long Time { get; set; } = 0;
        public int TryTimes { get; set; } = 5;
        public TaskCompletionSource<ConnectUdpResultModel> Tcs { get; set; }
    }

    public class ConnectUdpResultModel
    {
        public bool State { get; set; }
        public object Result { get; set; }
    }

    public class ConnectUdpFailModel
    {
        public ConnectUdpFailType Type { get; set; } = ConnectUdpFailType.ERROR;
        public string Msg { get; set; } = string.Empty;
    }

    [Flags]
    public enum ConnectUdpFailType
    {
        ERROR, TIMEOUT
    }

    [Flags]
    public enum PunchHoleUdpSteps : byte
    {
        STEP_1 = 1,
        STEP_2 = 2,
        STEP_2_1 = 3,
        STEP_2_Fail = 4,
        STEP_3 = 5,
        STEP_4 = 6,
    }

    [MessagePackObject]
    public class Step1Model : IPunchHoleStepInfo
    {

        [Key(1)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.UDP;

        [Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [Key(3)]
        public byte PunchStep { get; } = (byte)PunchHoleUdpSteps.STEP_1;
    }

    [MessagePackObject]
    public class Step2Model : IPunchHoleStepInfo
    {
        [Key(1)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.UDP;

        [Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [Key(3)]
        public byte PunchStep { get; } = (byte)PunchHoleUdpSteps.STEP_2;
    }

    [MessagePackObject]
    public class Step21Model : IPunchHoleStepInfo
    {
        [Key(1)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.UDP;

        [Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [Key(3)]
        public byte PunchStep { get; } = (byte)PunchHoleUdpSteps.STEP_2_1;
    }
    [MessagePackObject]
    public class Step2FailModel : IPunchHoleStepInfo
    {
        [Key(1)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.UDP;

        [Key(2)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.NOTIFY;

        [Key(3)]
        public byte PunchStep { get; } = (byte)PunchHoleUdpSteps.STEP_2_Fail;
    }

    [MessagePackObject]
    public class Step3Model : IPunchHoleStepInfo
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [Key(1)]
        public ulong FromId { get; set; } = 0;

        [Key(2)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.UDP;

        [Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [Key(4)]
        public byte PunchStep { get; } = (byte)PunchHoleUdpSteps.STEP_3;
    }

    [MessagePackObject]
    public class Step4Model : IPunchHoleStepInfo
    {
        /// <summary>
        /// 我的id
        /// </summary>
        [Key(1)]
        public ulong FromId { get; set; } = 0;

        [Key(2)]
        public PunchHoleTypes PunchType { get; } = PunchHoleTypes.UDP;

        [Key(3)]
        public PunchForwardTypes PunchForwardType { get; } = PunchForwardTypes.FORWARD;

        [Key(4)]
        public byte PunchStep { get; } = (byte)PunchHoleUdpSteps.STEP_4;
    }
}
