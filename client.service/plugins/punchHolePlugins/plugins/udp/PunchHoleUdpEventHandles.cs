using client.service.plugins.serverPlugins.register;
using common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace client.service.plugins.punchHolePlugins.plugins.udp
{
    public class PunchHoleUdpEventHandles : IPunchHoleUdp
    {
        private readonly PunchHoleEventHandles punchHoldEventHandles;
        private readonly RegisterState registerState;

        public PunchHoleUdpEventHandles(PunchHoleEventHandles punchHoldEventHandles, RegisterState registerState)
        {
            this.punchHoldEventHandles = punchHoldEventHandles;
            this.registerState = registerState;
        }

        private IPEndPoint UdpServer => registerState.UdpAddress;
        private long ConnectId => registerState.RemoteInfo.ConnectId;
        public int RouteLevel => registerState.LocalInfo.RouteLevel;

        private readonly ConcurrentDictionary<long, ConnectCache> connectCache = new();

        #region 连接客户端  具体流程 看MessageTypes里的描述
        /// <summary>
        /// 发送连接客户端请求消息（给服务器）
        /// </summary>
        public event EventHandler<SendPunchHoleEventArg> OnSendPunchHoleHandler;
        /// <summary>
        /// 发送连接客户端请求消息（给服务器）
        /// </summary>
        /// <param name="toid"></param>
        public void SendStep1(ConnectParams param)
        {
            if (connectCache.ContainsKey(param.Id))
            {
                return;
            }
            connectCache.TryAdd(param.Id, new ConnectCache
            {
                Callback = param.Callback,
                FailCallback = param.FailCallback,
                Time = Helper.GetTimeStamp(),
                Timeout = param.Timeout,
                TryTimes = param.TryTimes
            });

            OnSendPunchHoleHandler?.Invoke(this, new SendPunchHoleEventArg
            {
                Id = param.Id,
                Name = param.Name
            });

            TryConnect(param.Id);
        }
        private void TryConnect(long id)
        {
            if (connectCache.TryGetValue(id, out ConnectCache cache))
            {
                cache.TryTimes--;
                punchHoldEventHandles.Send(new SendPunchHoleArg
                {
                    Address = UdpServer,
                    ToId = id,
                    Data = new Step1Model { FromId = ConnectId }
                });
                Helper.SetTimeout(() =>
                {
                    if (connectCache.TryGetValue(id, out ConnectCache cache))
                    {
                        if (cache.TryTimes > 0)
                        {
                            TryConnect(id);
                        }
                        else
                        {
                            connectCache.TryRemove(id, out _);
                            cache.FailCallback(new ConnectFailModel
                            {
                                Msg = "UDP连接超时",
                                Type = ConnectFailType.TIMEOUT
                            });
                        }
                    }
                }, cache.Timeout);
            }
        }

        /// <summary>
        /// 服务器消息，某个客户端要跟我连接
        /// </summary>
        public event EventHandler<OnStep1EventArg> OnStep1Handler;
        /// <summary>
        /// 服务器消息，某个客户端要跟我连接
        /// </summary>
        /// <param name="toid"></param>
        public void OnStep1(OnStep1EventArg arg)
        {
            OnStep1Handler?.Invoke(this, arg);
            //随便给来源客户端发个消息
            punchHoldEventHandles.Send(new SendPunchHoleArg
            {
                Address = new IPEndPoint(IPAddress.Parse(arg.Data.LocalIps), arg.Data.LocalUdpPort),
                Data = new Step21Model { FromId = ConnectId }
            });
            punchHoldEventHandles.Send(new SendPunchHoleArg
            {
                Address = new IPEndPoint(IPAddress.Parse(arg.Data.Ip), arg.Data.Port),
                Data = new Step21Model { FromId = ConnectId }
            });
            SendStep2(new SendStep2EventArg
            {
                Address = UdpServer,
                ToId = arg.Data.Id
            });
        }


        public event EventHandler<SendStep2EventArg> OnSendStep2Handler;
        public void SendStep2(SendStep2EventArg arg)
        {
            OnSendStep2Handler?.Invoke(this, arg);
            punchHoldEventHandles.Send(new SendPunchHoleArg
            {
                Address = UdpServer,
                ToId = arg.ToId,
                Data = new Step2Model
                {
                    FromId = ConnectId
                }
            });
        }


        /// <summary>
        /// 服务器消息，目标客户端已经准备好
        /// </summary>
        public event EventHandler<OnStep2EventArg> OnStep2Handler;
        /// <summary>
        /// 服务器消息，目标客户端已经准备好
        /// </summary>
        /// <param name="toid"></param>
        public void OnStep2(OnStep2EventArg e)
        {
            OnStep2Handler?.Invoke(this, e);
            List<Tuple<string, int>> ips = new List<Tuple<string, int>> {
                new Tuple<string, int>(e.Data.LocalIps,e.Data.LocalUdpPort),
                new Tuple<string, int>(e.Data.Ip,e.Data.Port),
            };
            foreach (Tuple<string, int> ip in ips)
            {
                SendStep3(new SendStep3EventArg
                {
                    Address = new IPEndPoint(IPAddress.Parse(ip.Item1), ip.Item2),
                    Id = ConnectId
                });
            }
        }

        /// <summary>
        /// 开始连接目标客户端
        /// </summary>
        public event EventHandler<SendStep3EventArg> OnSendStep3Handler;
        /// <summary>
        /// 开始连接目标客户端
        /// </summary>
        /// <param name="toid"></param>
        public void SendStep3(SendStep3EventArg arg)
        {
            OnSendStep3Handler?.Invoke(this, arg);
            punchHoldEventHandles.Send(new SendPunchHoleArg
            {
                Address = arg.Address,
                Data = new Step3Model
                {
                    FromId = arg.Id
                }
            });
        }

        /// <summary>
        /// 来源客户端开始连接我了
        /// </summary>
        public event EventHandler<OnStep3EventArg> OnStep3Handler;
        /// <summary>
        /// 来源客户端开始连接我了
        /// </summary>
        /// <param name="toid"></param>
        public void OnStep3(OnStep3EventArg e)
        {
            OnStep3Handler?.Invoke(this, e);
            SendStep4(new SendStep4EventArg
            {
                Address = e.Packet.SourcePoint,
                Id = ConnectId
            });
        }

        /// <summary>
        /// 回应来源客户端
        /// </summary>
        public event EventHandler<SendStep4EventArg> OnSendStep4Handler;
        /// <summary>
        /// 回应来源客户端
        /// </summary>
        /// <param name="toid"></param>
        public void SendStep4(SendStep4EventArg arg)
        {
            OnSendStep4Handler?.Invoke(this, arg);
            punchHoldEventHandles.Send(new SendPunchHoleArg
            {
                Address = arg.Address,
                Data = new Step4Model
                {
                    FromId = arg.Id
                }
            });
        }
        /// <summary>
        /// 目标客户端回应我了
        /// </summary>
        public event EventHandler<OnStep4EventArg> OnStep4Handler;
        /// <summary>
        /// 目标客户端回应我了
        /// </summary>
        /// <param name="toid"></param>
        public void OnStep4(OnStep4EventArg arg)
        {
            if (connectCache.TryRemove(arg.Data.FromId, out ConnectCache cache))
            {
                cache?.Callback(arg);
            }
            OnStep4Handler?.Invoke(this, arg);
        }

        #endregion
    }

}
