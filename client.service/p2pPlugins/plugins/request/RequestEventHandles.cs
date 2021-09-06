using common;
using common.extends;
using ProtoBuf;
using server.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace client.service.p2pPlugins.plugins.request
{
    public class RequestEventHandlers
    {
        private static readonly Lazy<RequestEventHandlers> lazy = new(() => new RequestEventHandlers());
        public static RequestEventHandlers Instance => lazy.Value;

        private readonly ConcurrentDictionary<long, TcpRequestMessageCache> requestCache = new();
        private long requestId = 0;
        private readonly Dictionary<string, Tuple<object, MethodInfo>> plugins = new();

        private RequestEventHandlers()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(c => c.GetTypes())
                .Where(c => c.GetInterfaces().Contains(typeof(IRequestExcutePlugin)));
            foreach (var item in types)
            {
                string path = item.Name.Replace("Plugin", "");
                object obj = Activator.CreateInstance(item);
                foreach (var method in item.GetMethods())
                {
                    string key = $"{path}/{method.Name}".ToLower();
                    if (!plugins.ContainsKey(key))
                    {
                        plugins.TryAdd(key, new Tuple<object, MethodInfo>(obj, method));
                    }
                }
            }

            _ = Task.Factory.StartNew(() =>
              {
                  while (true)
                  {
                      if (!requestCache.IsEmpty)
                      {
                          long time = Helper.GetTimeStamp();
                          foreach (TcpRequestMessageCache item in requestCache.Values)
                          {
                              if (time - item.Time > item.Timeout)
                              {
                                  _ = requestCache.TryRemove(item.Id, out TcpRequestMessageCache cache);
                                  if (cache != null)
                                  {
                                      cache.FailCallback(new TcpRequestMessageFailModel
                                      {
                                          Type = TcpRequestMessageFailType.TIMEOUT,
                                          Msg = "请求超时"
                                      });
                                  }
                              }
                          }
                      }
                      Thread.Sleep(1);
                  }

              }, TaskCreationOptions.LongRunning);
        }

        #region 请求


        //收到请求
        public void OnTcpRequestMsessage(OnTcpRequestMsessageEventArg arg)
        {
            string path = arg.Data.Path.ToLower();
            if (arg.Data.RequestType == RequestTypes.RESULT)
            {
                OnTcpRequestMsessageResult(arg);
            }
            else if (plugins.ContainsKey(path))
            {
               
                var plugin = plugins[path];
                try
                {
                    var param = new PluginExcuteWrap
                    {
                        Data = arg,
                        Callback = (param, returnResult) =>
                        {
                            if (returnResult != null && returnResult.Length > 0)
                            {
                                P2PMessageEventHandles.Instance.SendTcpMessage(new SendP2PTcpMessageArg
                                {
                                    Socket = param.Data.Packet.TcpSocket,
                                    Data = new MessageRequestModel
                                    {
                                        Data = returnResult,
                                        RequestId = param.Data.Data.RequestId,
                                        Code = MessageRequestResultCodes.OK,
                                        Path = param.Data.Data.Path,
                                        RequestType = RequestTypes.RESULT
                                    }
                                });
                            }
                        }
                    };
                    plugin.Item2.Invoke(plugin.Item1, new object[] { param });
                }
                catch (Exception ex)
                {
                    Logger.Instance.Info(ex + "");
                    P2PMessageEventHandles.Instance.SendTcpMessage(new SendP2PTcpMessageArg
                    {
                        Socket = arg.Packet.TcpSocket,
                        Data = new MessageRequestModel
                        {
                            Data = Encoding.UTF8.GetBytes(ex.Message),
                            RequestId = arg.Data.RequestId,
                            Code = MessageRequestResultCodes.FAIL,
                            Path = arg.Data.Path,
                            RequestType = RequestTypes.RESULT
                        }
                    });
                }
            }
            else
            {
                P2PMessageEventHandles.Instance.SendTcpMessage(new SendP2PTcpMessageArg
                {
                    Socket = arg.Packet.TcpSocket,
                    Data = new MessageRequestModel
                    {
                        Data = "没有相应的插件执行你的请求".ProtobufSerialize(),
                        RequestId = arg.Data.RequestId,
                        Path = arg.Data.Path,
                        Code = MessageRequestResultCodes.NOTFOUND,
                        RequestType = RequestTypes.RESULT
                    }
                });
            }
        }
        //收到请求回调
        public void OnTcpRequestMsessageResult(OnTcpRequestMsessageEventArg arg)
        {
            _ = requestCache.TryRemove(arg.Data.RequestId, out TcpRequestMessageCache cache);
            if (cache != null)
            {
                if (arg.Data.Code == MessageRequestResultCodes.OK)
                {
                    cache.Callback(arg.Data);
                }
                else
                {
                    cache.FailCallback(new TcpRequestMessageFailModel
                    {
                        Type = TcpRequestMessageFailType.ERROR,
                        Msg = Encoding.UTF8.GetString(arg.Data.Data)
                    });
                }
            }
        }
        //发起请求
        public void SendTcpRequestMsessage(Socket socket, string path, IRequestExcuteMessage data, Action<MessageRequestModel> callback, Action<TcpRequestMessageFailModel> failCallback)
        {
            _ = Interlocked.Increment(ref requestId);
            _ = requestCache.TryAdd(requestId, new TcpRequestMessageCache
            {
                Callback = callback,
                FailCallback = failCallback,
                Id = requestId,
                Time = Helper.GetTimeStamp()
            });

            P2PMessageEventHandles.Instance.SendTcpMessage(new SendP2PTcpMessageArg
            {
                Socket = socket,
                Data = new MessageRequestModel
                {
                    Data = data.ProtobufSerialize(),
                    RequestId = requestId,
                    Path = path,
                    RequestType = RequestTypes.REQUEST
                }
            });
        }
        #endregion
    }




    #region 请求
    public class TcpRequestMessageCache
    {
        public long Id { get; set; } = 0;
        public long Time { get; set; } = 0;
        public int Timeout { get; set; } = 5 * 1000;
        public Action<MessageRequestModel> Callback { get; set; } = null;
        public Action<TcpRequestMessageFailModel> FailCallback { get; set; } = null;
    }

    [ProtoContract]
    public class TcpRequestMessageFailModel
    {
        [ProtoMember(1, IsRequired = true)]
        public TcpRequestMessageFailType Type { get; set; } = TcpRequestMessageFailType.ERROR;
        [ProtoMember(2)]
        public string Msg { get; set; } = string.Empty;
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum TcpRequestMessageFailType
    {
        ERROR, TIMEOUT
    }

    public class OnTcpRequestMsessageEventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public MessageRequestModel Data { get; set; }
    }

    public class PluginExcuteWrap
    {
        public OnTcpRequestMsessageEventArg Data { get; set; }

        public int Code { get; private set; } = 0;
        public string Message { get; private set; } = string.Empty;
        public void SetResultCode(int code, string msg = "")
        {
            Code = code;
            Message = msg;
        }
        public void SetResultMessage(string msg)
        {
            Message = msg;
        }

        public Action<PluginExcuteWrap, byte[]> Callback { get; set; }

    }

    #endregion
}
