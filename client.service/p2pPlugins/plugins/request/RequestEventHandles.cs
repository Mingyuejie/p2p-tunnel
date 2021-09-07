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
                                      cache.Tcs?.SetResult(new RequestModel
                                      {
                                          Code = MessageRequestResponseCodes.FAIL,
                                          ErrorMsg = "请求超时"
                                      });
                                  }
                              }
                          }
                      }
                      Thread.Sleep(1);
                  }

              }, TaskCreationOptions.LongRunning);
        }

        //收到请求
        public void OnTcpRequest(OnTcpRequestEventArg arg)
        {
            string path = arg.Data.Path.ToLower();
            if (arg.Data.RequestType == RequestTypes.RESULT)
            {
                OnTcpRequestResponse(arg);
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
                                P2PEventHandles.Instance.SendTcp(new SendP2PTcpArg
                                {
                                    Socket = param.Data.Packet.TcpSocket,
                                    Data = new RequestModel
                                    {
                                        Data = returnResult,
                                        RequestId = param.Data.Data.RequestId,
                                        Code = MessageRequestResponseCodes.OK,
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
                    Logger.Instance.Debug(ex + "");
                    P2PEventHandles.Instance.SendTcp(new SendP2PTcpArg
                    {
                        Socket = arg.Packet.TcpSocket,
                        Data = new RequestModel
                        {
                            ErrorMsg = ex.Message,
                            RequestId = arg.Data.RequestId,
                            Code = MessageRequestResponseCodes.FAIL,
                            Path = arg.Data.Path,
                            RequestType = RequestTypes.RESULT
                        }
                    });
                }
            }
            else
            {
                P2PEventHandles.Instance.SendTcp(new SendP2PTcpArg
                {
                    Socket = arg.Packet.TcpSocket,
                    Data = new RequestModel
                    {
                        ErrorMsg = "没有相应的插件执行你的请求",
                        RequestId = arg.Data.RequestId,
                        Path = arg.Data.Path,
                        Code = MessageRequestResponseCodes.NOTFOUND,
                        RequestType = RequestTypes.RESULT
                    }
                });
            }
        }
        //收到请求回调
        public void OnTcpRequestResponse(OnTcpRequestEventArg arg)
        {
            _ = requestCache.TryRemove(arg.Data.RequestId, out TcpRequestMessageCache cache);
            if (cache != null)
            {
                cache.Tcs?.SetResult(arg.Data);
            }
        }
        //发起请求
        public async Task<RequestModel> TcpRequest(TcpRequestParam param)
        {
            await Task.Yield();
            _ = Interlocked.Increment(ref requestId);
            var tcs = new TaskCompletionSource<RequestModel>();
            _ = requestCache.TryAdd(requestId, new TcpRequestMessageCache
            {
                Tcs = tcs,
                Id = requestId,
                Time = Helper.GetTimeStamp()
            });

            P2PEventHandles.Instance.SendTcp(new SendP2PTcpArg
            {
                Socket = param.Socket,
                Data = new RequestModel
                {
                    Data = param.Data.ToBytes(),
                    RequestId = requestId,
                    Path = param.Path,
                    RequestType = RequestTypes.REQUEST
                }
            });
            return await tcs.Task;
        }
    }

    public class TcpRequestParam
    {
        public Socket Socket { get; set; }
        public string Path { get; set; }
        public IRequestExcuteMessage Data { get; set; }
    }

    public class TcpRequestMessageCache
    {
        public long Id { get; set; } = 0;
        public long Time { get; set; } = 0;
        public int Timeout { get; set; } = 5 * 1000;
        public TaskCompletionSource<RequestModel> Tcs { get; set; } = null;
    }

    public class OnTcpRequestEventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public RequestModel Data { get; set; }
    }

    /// <summary>
    /// 插件执行参数
    /// </summary>
    public class PluginExcuteWrap
    {
        public OnTcpRequestEventArg Data { get; set; }

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

    [ProtoContract]
    public class RequestModel : IP2PMessageBase
    {
        public RequestModel() { }

        [ProtoMember(1, IsRequired = true)]
        public P2PDataTypes Type { get; } = P2PDataTypes.REQUEST;

        [ProtoMember(2)]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        [ProtoMember(3)]
        public long RequestId { get; set; } = 0;

        [ProtoMember(4)]
        public string Path { get; set; } = string.Empty;

        [ProtoMember(6, IsRequired = true)]
        public MessageRequestResponseCodes Code { get; set; } = MessageRequestResponseCodes.OK;

        [ProtoMember(7, IsRequired = true)]
        public RequestTypes RequestType { get; set; } = RequestTypes.REQUEST;

        [ProtoMember(8)]
        public string ErrorMsg { get; set; } = string.Empty;
    }


    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum MessageRequestResponseCodes : short
    {
        OK, NOTFOUND, FAIL
    }

    public interface IRequestExcuteMessage
    {
    }

    public interface IRequestExcutePlugin
    {
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum RequestTypes
    {
        REQUEST, RESULT
    }

    public class RequestPlugin : IP2PPlugin
    {
        public P2PDataTypes Type => P2PDataTypes.REQUEST;

        public void Excute(OnP2PTcpArg arg)
        {
            RequestEventHandlers.Instance.OnTcpRequest(new OnTcpRequestEventArg
            {
                Data = arg.Data.Data.DeBytes<RequestModel>(),
                Packet = arg.Packet,
            });
        }
    }
}
