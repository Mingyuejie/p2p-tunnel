using client.messengers.punchHole.tcp;
using client.messengers.register;
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

namespace client.service.messengers.punchHole.tcp.nutssb
{
    public class PunchHoleTcpNutssBMessengerSender : IPunchHoleTcp
    {
        private readonly PunchHoleMessengerSender  punchHoleMessengerClient;
        private readonly ITcpServer tcpServer;
        private readonly RegisterStateInfo registerState;
        private readonly Config config;

        public PunchHoleTcpNutssBMessengerSender(PunchHoleMessengerSender punchHoleMessengerClient, ITcpServer tcpServer,
            RegisterStateInfo registerState, Config config)
        {
            this.punchHoleMessengerClient = punchHoleMessengerClient;
            this.tcpServer = tcpServer;
            this.registerState = registerState;
            this.config = config;
        }

        private IConnection TcpServer => registerState.TcpConnection;
        private ulong ConnectId => registerState.ConnectId;

        public int ClientTcpPort => registerState.LocalInfo.TcpPort;
        public int RouteLevel => registerState.LocalInfo.RouteLevel;

        private readonly ConcurrentDictionary<ulong, ConnectTcpCache> connectTcpCache = new();


        public SimpleSubPushHandler<ConnectTcpParams> OnSendHandler => new SimpleSubPushHandler<ConnectTcpParams>();
        public async Task<ConnectTcpResultModel> Send(ConnectTcpParams param)
        {
            TaskCompletionSource<ConnectTcpResultModel> tcs = new TaskCompletionSource<ConnectTcpResultModel>();
            connectTcpCache.TryAdd(param.Id, new ConnectTcpCache
            {
                TryTimes = param.TryTimes,
                Tcs = tcs,
                TunnelName = param.TunnelName,
            });
            await punchHoleMessengerClient.Send(new SendPunchHoleArg<Step1Model>
            {
                TunnelName = param.TunnelName,
                Connection = TcpServer,
                ToId = param.Id,
                Data = new Step1Model { }
            });

            //OnSendHandler.Push(param);

            return await tcs.Task;
        }

        public SimpleSubPushHandler<OnStep1EventArg> OnStep1Handler { get; } = new SimpleSubPushHandler<OnStep1EventArg>();
        public async Task OnStep1(OnStep1EventArg arg)
        {
            if (arg.Data.IsDefault)
            {
                OnStep1Handler.Push(arg);
            }

            List<Tuple<string, int>> ips = arg.Data.LocalIps.Split(Helper.SeparatorChar).Where(c => c.Length > 0)
                .Select(c => new Tuple<string, int>(c, arg.Data.LocalPort)).ToList();
            ips.Add(new Tuple<string, int>(arg.Data.Ip, arg.Data.Port));

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

            await punchHoleMessengerClient.Send(new SendPunchHoleArg<Step2Model>
            {
                TunnelName = arg.RawData.TunnelName,
                Connection = TcpServer,
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

            if (!connectTcpCache.TryGetValue(arg.RawData.FromId, out ConnectTcpCache cache))
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

                        if (arg.Data.IsDefault)
                        {
                            tcpServer.BindReceive(targetSocket, bufferSize: config.Client.TcpBufferSize);
                            await punchHoleMessengerClient.Send(new SendPunchHoleArg<Step3Model>
                            {
                                TunnelName = arg.RawData.TunnelName,
                                Connection = tcpServer.CreateConnection(targetSocket),
                                Data = new Step3Model
                                {
                                    FromId = ConnectId
                                }
                            });
                        }
                        else
                        {
                            if (connectTcpCache.TryRemove(arg.RawData.FromId, out _))
                            {
                                cache.Tcs.SetResult(new ConnectTcpResultModel { State = true });
                            }
                        }
                        success = true;
                        break;
                    }
                    else
                    {
                        Logger.Instance.Debug($"{ip.Item1}:{ip.Item2} 连接失败");
                        targetSocket.SafeClose();
                        interval = 300;
                        await SendStep2Retry(arg.RawData.FromId, arg.RawData.TunnelName);
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
                        await SendStep2Retry(arg.RawData.FromId, arg.RawData.TunnelName);
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
                await SendStep2Fail(arg);
            }
        }

        private async Task SendStep2Retry(ulong toid, string tunnelName)
        {
            await punchHoleMessengerClient.Send(new SendPunchHoleArg<Step2TryModel>
            {
                TunnelName = tunnelName,
                Connection = TcpServer,
                ToId = toid,
                Data = new Step2TryModel { }
            });
        }
        public SimpleSubPushHandler<OnStep2RetryEventArg> OnStep2RetryHandler { get; } = new SimpleSubPushHandler<OnStep2RetryEventArg>();
        public async Task OnStep2Retry(OnStep2RetryEventArg e)
        {
            OnStep2RetryHandler.Push(e);
            await Task.Run(() =>
            {
                using Socket targetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                targetSocket.Ttl = (short)(RouteLevel + 5);
                targetSocket.ReuseBind(new IPEndPoint(config.Client.BindIp, ClientTcpPort));
                targetSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(e.Data.Ip), e.Data.Port));
                targetSocket.SafeClose();
            });
        }

        public SimpleSubPushHandler<ulong> OnSendStep2FailHandler => new SimpleSubPushHandler<ulong>();
        private async Task SendStep2Fail(OnStep2EventArg arg)
        {

            if (connectTcpCache.TryRemove(arg.RawData.FromId, out ConnectTcpCache cache))
            {
                cache.Canceled = true;
                cache.Tcs.SetResult(new ConnectTcpResultModel
                {
                    State = false,
                    Result = new ConnectTcpFailModel
                    {
                        Msg = "失败",
                        Type = ConnectTcpFailType.ERROR
                    }
                });
            }
            if (arg.Data.IsDefault)
            {
                OnSendStep2FailHandler.Push(arg.RawData.FromId);
                await punchHoleMessengerClient.Send(new SendPunchHoleArg<Step2FailModel>
                {
                    TunnelName = arg.RawData.TunnelName,
                    Connection = TcpServer,
                    ToId = arg.RawData.FromId,
                    Data = new Step2FailModel { }
                });
            }
        }
        public SimpleSubPushHandler<OnStep2FailEventArg> OnStep2FailHandler { get; } = new SimpleSubPushHandler<OnStep2FailEventArg>();
        public async Task OnStep2Fail(OnStep2FailEventArg arg)
        {
            await Task.CompletedTask;
            OnStep2FailHandler.Push(arg);
        }
        public async Task SendStep2Stop(ulong toid)
        {
            if (connectTcpCache.TryGetValue(toid, out ConnectTcpCache cache))
            {
                await punchHoleMessengerClient.Send(new SendPunchHoleArg<Step2StopModel>
                {
                    TunnelName = cache.TunnelName,
                    Connection = TcpServer,
                    ToId = toid,
                    Data = new Step2StopModel { }
                });
                Cancel(toid);
            }
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
                cache.Tcs.SetResult(new ConnectTcpResultModel
                {
                    State = false,
                    Result = new ConnectTcpFailModel
                    {
                        Msg = "取消",
                        Type = ConnectTcpFailType.CANCEL
                    }
                });
            }
        }

        public SimpleSubPushHandler<OnStep3EventArg> OnStep3Handler { get; } = new SimpleSubPushHandler<OnStep3EventArg>();
        public async Task OnStep3(OnStep3EventArg arg)
        {
            OnStep3Handler.Push(arg);
            await punchHoleMessengerClient.Send(new SendPunchHoleArg<Step4Model>
            {
                TunnelName = arg.RawData.TunnelName,
                Connection = arg.Connection,
                Data = new Step4Model
                {
                    FromId = ConnectId
                }
            });
        }

        public SimpleSubPushHandler<OnStep4EventArg> OnStep4Handler { get; } = new SimpleSubPushHandler<OnStep4EventArg>();



        public async Task OnStep4(OnStep4EventArg arg)
        {
            await Task.CompletedTask;
            if (connectTcpCache.TryRemove(arg.Data.FromId, out ConnectTcpCache cache))
            {
                cache.Tcs.SetResult(new ConnectTcpResultModel { State = true });
            }
            OnStep4Handler.Push(arg);
        }

    }
}
