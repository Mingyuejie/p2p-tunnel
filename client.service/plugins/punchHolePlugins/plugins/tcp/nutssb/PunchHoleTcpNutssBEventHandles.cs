using client.plugins.serverPlugins.register;
using client.service.plugins.serverPlugins.register;
using common;
using common.extends;
using server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.plugins.punchHolePlugins.plugins.tcp.nutssb
{
    public class PunchHoleTcpNutssBEventHandles : IPunchHoleTcp
    {
        private readonly PunchHoleEventHandles punchHoldEventHandles;
        private readonly ITcpServer tcpServer;
        private readonly RegisterState registerState;
        private readonly Config config;

        public PunchHoleTcpNutssBEventHandles(PunchHoleEventHandles punchHoldEventHandles, ITcpServer tcpServer,
            RegisterState registerState, Config config)
        {
            this.punchHoldEventHandles = punchHoldEventHandles;
            this.tcpServer = tcpServer;
            this.registerState = registerState;
            this.config = config;
        }

        private IConnection TcpServer => registerState.TcpConnection;
        private ulong ConnectId => registerState.ConnectId;

        public int ClientTcpPort => registerState.LocalInfo.TcpPort;
        public int RouteLevel => registerState.LocalInfo.RouteLevel;

        private readonly ConcurrentDictionary<ulong, ConnectTcpCache> connectTcpCache = new();


        public async Task<ConnectResultModel> Send(ConnectTcpParams param)
        {
            TaskCompletionSource<ConnectResultModel> tcs = new TaskCompletionSource<ConnectResultModel>();
            connectTcpCache.TryAdd(param.Id, new ConnectTcpCache
            {
                TryTimes = param.TryTimes,
                Tcs = tcs
            });
            await punchHoldEventHandles.Send(new SendPunchHoleArg<Step1Model>
            {
                Connection = TcpServer,
                ToId = param.Id,
                Data = new Step1Model { }
            });

            return await tcs.Task;
        }

        public SimplePushSubHandler<OnStep1EventArg> OnStep1Handler { get; } = new SimplePushSubHandler<OnStep1EventArg>();
        public async Task OnStep1(OnStep1EventArg e)
        {
            OnStep1Handler.Push(e);

            List<Tuple<string, int>> ips = e.Data.LocalIps.Split(Helper.SeparatorChar).Where(c => c.Length > 0)
                .Select(c => new Tuple<string, int>(c, e.Data.LocalTcpPort)).ToList();
            ips.Add(new Tuple<string, int>(e.Data.Ip, e.Data.TcpPort));

            foreach (Tuple<string, int> ip in ips)
            {
                using Socket targetSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    targetSocket.Ttl = (short)(RouteLevel + 2);
                    targetSocket.ReuseBind(new IPEndPoint(config.Client.BindIp, ClientTcpPort));
                    _ = targetSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ip.Item1), ip.Item2));
                }
                catch (Exception)
                {
                }
                targetSocket.SafeClose();
            }

            await punchHoldEventHandles.Send(new SendPunchHoleArg<Step2Model>
            {
                Connection = TcpServer,
                ToId = e.RawData.FromId,
                Data = new Step2Model { }
            });
        }

        public SimplePushSubHandler<OnStep2EventArg> OnStep2Handler { get; } = new SimplePushSubHandler<OnStep2EventArg>();
        public async Task OnStep2(OnStep2EventArg e)
        {
            OnStep2Handler.Push(e);

            List<Tuple<string, int>> ips = e.Data.LocalIps.Split(Helper.SeparatorChar).Where(c => c.Length > 0)
                .Select(c => new Tuple<string, int>(c, e.Data.LocalTcpPort)).ToList();
            ips.Add(new Tuple<string, int>(e.Data.Ip, e.Data.TcpPort));

            if (!connectTcpCache.TryGetValue(e.RawData.FromId, out ConnectTcpCache cache))
            {
                return;
            }


            bool success = false;
            int length = cache.TryTimes, index = 0, interval = 0;
            while (length > 0)
            {
                if (cache.Canceled)
                {
                    break;
                }
                if (interval > 0)
                {
                    await Task.Delay(interval);
                    interval = 0;
                }

                Tuple<string, int> ip = index >= ips.Count ? ips[^1] : ips[index];
                IPEndPoint targetEndpoint = new IPEndPoint(IPAddress.Parse(ip.Item1), ip.Item2);
                Socket targetSocket = new Socket(targetEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    targetSocket.KeepAlive();
                    targetSocket.ReuseBind(new IPEndPoint(config.Client.BindIp, ClientTcpPort));
                    IAsyncResult result = targetSocket.BeginConnect(targetEndpoint, null, null);
                    result.AsyncWaitHandle.WaitOne(2000, false);

                    if (result.IsCompleted)
                    {
                        if (cache.Canceled)
                        {
                            targetSocket.SafeClose();
                            break;
                        }
                        Logger.Instance.Debug($"{ip.Item1}:{ip.Item2} 连接成功");
                        targetSocket.EndConnect(result);
                        tcpServer.BindReceive(targetSocket, bufferSize: config.Client.TcpBufferSize);

                        await punchHoldEventHandles.Send(new SendPunchHoleArg<Step3Model>
                        {
                            Connection = tcpServer.CreateConnection(targetSocket),
                            Data = new Step3Model
                            {
                                FromId = ConnectId
                            }
                        });
                        success = true;
                        break;
                    }
                    else
                    {
                        Logger.Instance.Debug($"{ip.Item1}:{ip.Item2} 连接失败");
                        targetSocket.SafeClose();
                        interval = 300;
                        await SendStep2Retry(e.RawData.FromId);
                        length--;
                    }
                }
                catch (SocketException ex)
                {
                    targetSocket.SafeClose();
                    targetSocket = null;
                    if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    {
                        interval = 2000;
                    }
                    else
                    {
                        interval = 100;
                        await SendStep2Retry(e.RawData.FromId);
                        length--;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }

                index++;
            }
            if (!success)
            {
                await SendStep2Fail(new SendStep2FailEventArg
                {
                    Id = ConnectId,
                    ToId = e.RawData.FromId
                });
            }
        }

        private async Task SendStep2Retry(ulong toid)
        {
            await punchHoldEventHandles.Send(new SendPunchHoleArg<Step2TryModel>
            {
                Connection = TcpServer,
                ToId = toid,
                Data = new Step2TryModel { }
            });
        }
        public SimplePushSubHandler<OnStep2RetryEventArg> OnStep2RetryHandler { get; } = new SimplePushSubHandler<OnStep2RetryEventArg>();
        public async Task OnStep2Retry(OnStep2RetryEventArg e)
        {
            OnStep2RetryHandler.Push(e);
            await Task.Run(() =>
            {
                using Socket targetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                targetSocket.Ttl = (short)(RouteLevel + 5);
                targetSocket.ReuseBind(new IPEndPoint(config.Client.BindIp, ClientTcpPort));
                targetSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(e.Data.Ip), e.Data.TcpPort));
                targetSocket.SafeClose();
            });
        }

        private async Task SendStep2Fail(SendStep2FailEventArg arg)
        {
            if (connectTcpCache.TryRemove(arg.ToId, out ConnectTcpCache cache))
            {
                cache.Canceled = true;
                cache.Tcs.SetResult(new ConnectResultModel
                {
                    State = false,
                    Result = new ConnectFailModel
                    {
                        Msg = "失败",
                        Type = ConnectFailType.ERROR
                    }
                });
            }
            await punchHoldEventHandles.Send(new SendPunchHoleArg<Step2FailModel>
            {
                Connection = TcpServer,
                ToId = arg.ToId,
                Data = new Step2FailModel { }
            });
        }
        public SimplePushSubHandler<OnStep2FailEventArg> OnStep2FailHandler { get; } = new SimplePushSubHandler<OnStep2FailEventArg>();
        public async Task OnStep2Fail(OnStep2FailEventArg arg)
        {
            await Task.CompletedTask;
            OnStep2FailHandler.Push(arg);
        }
        public async Task SendStep2Stop(ulong toid)
        {
            await punchHoldEventHandles.Send(new SendPunchHoleArg<Step2StopModel>
            {
                Connection = TcpServer,
                ToId = toid,
                Data = new Step2StopModel { }
            });
            Cancel(toid);
        }
        public async Task OnStep2Stop(OnStep2StopEventArg e)
        {
            await Task.CompletedTask;
            Cancel(e.RawData.FromId);
        }

        private void Cancel(ulong id)
        {
            if (connectTcpCache.TryRemove(id, out ConnectTcpCache cache))
            {
                cache.Canceled = true;
                cache.Tcs.SetResult(new ConnectResultModel
                {
                    State = false,
                    Result = new ConnectFailModel
                    {
                        Msg = "取消",
                        Type = ConnectFailType.CANCEL
                    }
                });
            }
        }

        public SimplePushSubHandler<OnStep3EventArg> OnStep3Handler { get; } = new SimplePushSubHandler<OnStep3EventArg>();
        public async Task OnStep3(OnStep3EventArg arg)
        {
            OnStep3Handler.Push(arg);
            await punchHoldEventHandles.Send(new SendPunchHoleArg<Step4Model>
            {
                Connection = arg.Packet.Connection,
                Data = new Step4Model
                {
                    FromId = ConnectId
                }
            });
        }

        public SimplePushSubHandler<OnStep4EventArg> OnStep4Handler { get; } = new SimplePushSubHandler<OnStep4EventArg>();
        public async Task OnStep4(OnStep4EventArg arg)
        {
            await Task.CompletedTask;
            if (connectTcpCache.TryRemove(arg.Data.FromId, out ConnectTcpCache cache))
            {
                cache.Tcs.SetResult(new ConnectResultModel { State = true, Result = arg });
            }
            OnStep4Handler.Push(arg);
        }

    }
}
