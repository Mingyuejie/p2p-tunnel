using client.plugins.serverPlugins.register;
using client.service.plugins.serverPlugins.register;
using common;
using server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace client.service.plugins.punchHolePlugins.plugins.udp
{
    public class PunchHoleUdpEventHandles : IPunchHoleUdp
    {
        private readonly PunchHoleEventHandles punchHoldEventHandles;
        private readonly RegisterState registerState;
        private readonly IUdpServer udpServer;

        public PunchHoleUdpEventHandles(PunchHoleEventHandles punchHoldEventHandles, RegisterState registerState, IUdpServer udpServer)
        {
            this.punchHoldEventHandles = punchHoldEventHandles;
            this.registerState = registerState;
            this.udpServer = udpServer;
        }

        private IConnection connection => registerState.TcpConnection;
        private ulong ConnectId => registerState.ConnectId;
        public int RouteLevel => registerState.LocalInfo.RouteLevel;

        private readonly ConcurrentDictionary<ulong, ConnectCache> connectCache = new();

        public async Task<ConnectResultModel> Send(ConnectParams param)
        {
            TaskCompletionSource<ConnectResultModel> tcs = new TaskCompletionSource<ConnectResultModel>();

            connectCache.TryAdd(param.Id, new ConnectCache
            {
                Tcs = tcs,
                Time = DateTimeHelper.GetTimeStamp(),
                TryTimes = param.TryTimes
            });

            while (true)
            {
                if (!connectCache.TryGetValue(param.Id, out ConnectCache cache))
                {
                    break;
                }
                if (cache.TryTimes < 0)
                {
                    await punchHoldEventHandles.Send(new SendPunchHoleArg<Step2FailModel>
                    {
                        Connection = connection,
                        TunnelName = param.TunnelName,
                        ToId = param.Id,
                        Data = new Step2FailModel { }
                    });
                    connectCache.TryRemove(param.Id, out _);
                    cache.Tcs.SetResult(new ConnectResultModel
                    {
                        State = false,
                        Result = new ConnectFailModel
                        {
                            Msg = "UDP连接超时",
                            Type = ConnectFailType.TIMEOUT
                        }
                    });
                    break;
                }

                cache.TryTimes--;
                await punchHoldEventHandles.Send(new SendPunchHoleArg<Step1Model>
                {
                    Connection = connection,
                    TunnelName = param.TunnelName,
                    ToId = param.Id,
                    Data = new Step1Model { }
                });

                await Task.Delay(1000);
            }

            return await tcs.Task;
        }

        public SimpleSubPushHandler<OnStep1EventArg> OnStep1Handler { get; } = new SimpleSubPushHandler<OnStep1EventArg>();
        public async Task OnStep1(OnStep1EventArg arg)
        {
            OnStep1Handler.Push(arg);
            foreach (var ip in arg.Data.LocalIps.Split(Helper.SeparatorChar).Where(c => c.Length > 0))
            {
                await punchHoldEventHandles.Send(new SendPunchHoleArg<Step21Model>
                {
                    Connection = udpServer.CreateConnection(new IPEndPoint(IPAddress.Parse(ip), arg.Data.Port)),
                    TunnelName = arg.RawData.TunnelName,
                    Data = new Step21Model { }
                });
            }

            await punchHoldEventHandles.Send(new SendPunchHoleArg<Step21Model>
            {
                Connection = udpServer.CreateConnection(new IPEndPoint(IPAddress.Parse(arg.Data.Ip), arg.Data.Port)),
                TunnelName = arg.RawData.TunnelName,
                Data = new Step21Model { }
            });

            await punchHoldEventHandles.Send(new SendPunchHoleArg<Step2Model>
            {
                Connection = arg.Connection,
                TunnelName = arg.RawData.TunnelName,
                ToId = arg.RawData.FromId,
                Data = new Step2Model { }
            });
        }

        public SimpleSubPushHandler<OnStep2EventArg> OnStep2Handler { get; } = new SimpleSubPushHandler<OnStep2EventArg>();
        public async Task OnStep2(OnStep2EventArg arg)
        {
            OnStep2Handler.Push(arg);
            List<Tuple<string, int>> ips = arg.Data.LocalIps.Split(Helper.SeparatorChar).Where(c => c.Length > 0)
                .Select(c => new Tuple<string, int>(c, arg.Data.LocalPort)).ToList();
            ips.Add(new Tuple<string, int>(arg.Data.Ip, arg.Data.Port));

            foreach (Tuple<string, int> ip in ips)
            {
                await punchHoldEventHandles.Send(new SendPunchHoleArg<Step3Model>
                {
                    Connection = udpServer.CreateConnection(new IPEndPoint(IPAddress.Parse(ip.Item1), ip.Item2)),
                    TunnelName = arg.RawData.TunnelName,
                    Data = new Step3Model
                    {
                        FromId = ConnectId
                    }
                });
            }
        }

        public SimpleSubPushHandler<OnStep2FailEventArg> OnStep2FailHandler { get; } = new SimpleSubPushHandler<OnStep2FailEventArg>();
        public void OnStep2Fail(OnStep2FailEventArg arg)
        {
            OnStep2FailHandler.Push(arg);
        }

        public SimpleSubPushHandler<OnStep3EventArg> OnStep3Handler { get; } = new SimpleSubPushHandler<OnStep3EventArg>();
        public async Task OnStep3(OnStep3EventArg arg)
        {
            OnStep3Handler.Push(arg);

            await punchHoldEventHandles.Send(new SendPunchHoleArg<Step4Model>
            {
                Connection = arg.Connection,
                TunnelName = arg.RawData.TunnelName,
                Data = new Step4Model
                {
                    FromId = ConnectId
                }
            });
        }

        public SimpleSubPushHandler<OnStep4EventArg> OnStep4Handler { get; } = new SimpleSubPushHandler<OnStep4EventArg>();
        public void OnStep4(OnStep4EventArg arg)
        {
            if (connectCache.TryRemove(arg.Data.FromId, out ConnectCache cache))
            {
                cache.Tcs.SetResult(new ConnectResultModel { State = true, Result = arg });
            }
            OnStep4Handler.Push(arg);
        }
    }
}
