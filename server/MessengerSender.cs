using common;
using common.extends;
using server.model;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace server
{
    public class MessengerSender
    {
        private NumberSpace requestIdNumberSpace = new NumberSpace(0);
        private ConcurrentDictionary<ulong, SendCacheInfo> sends = new ConcurrentDictionary<ulong, SendCacheInfo>();
        private SimpleObjectPool<MessageRequestWrap> messageRequestWrapPool = new SimpleObjectPool<MessageRequestWrap>();

        public long LastTime { get; private set; } = DateTimeHelper.GetTimeStamp();

        public MessengerSender()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (!sends.IsEmpty)
                    {
                        foreach (SendCacheInfo item in sends.Values)
                        {
                            if (item.Timeout > 0 && LastTime - item.Time > item.Timeout)
                            {
                                if (sends.TryRemove(item.RequestId, out SendCacheInfo cache))
                                {
                                    cache.Tcs.SetResult(new MessageResponeInfo
                                    {
                                        Code = MessageResponeCodes.TIMEOUT
                                    });
                                }
                            }
                        }
                    }
                    LastTime = DateTimeHelper.GetTimeStamp();
                    await Task.Delay(1);
                }

            }, TaskCreationOptions.LongRunning);
        }

        public async Task<MessageResponeInfo> SendReply<T>(MessageRequestParamsInfo<T> msg)
        {
            if (msg.RequestId == 0)
            {
                msg.RequestId = requestIdNumberSpace.Get();
            }
            TaskCompletionSource<MessageResponeInfo> tcs = NewReply(msg.RequestId, msg.Timeout);
            if (!await SendOnly(msg))
            {
                sends.TryRemove(msg.RequestId, out _);
                tcs.SetResult(new MessageResponeInfo { Code = MessageResponeCodes.NOT_CONNECT });
            }
            return await tcs.Task;
        }
        public async Task<MessageResponeInfo> SendReply(MessageRequestParamsInfo<byte[]> msg)
        {
            if (msg.RequestId == 0)
            {
                msg.RequestId = requestIdNumberSpace.Get();
            }
            TaskCompletionSource<MessageResponeInfo> tcs = NewReply(msg.RequestId, msg.Timeout);
            if (!await SendOnly(msg))
            {
                sends.TryRemove(msg.RequestId, out _);
                tcs.SetResult(new MessageResponeInfo { Code = MessageResponeCodes.NOT_CONNECT });
            }
            return await tcs.Task;
        }
        public async Task<bool> SendOnly<T>(MessageRequestParamsInfo<T> msg)
        {
            try
            {
                if (msg.RequestId == 0)
                {
                    msg.RequestId = requestIdNumberSpace.Get();
                }
                if (msg.Connection == null)
                {
                    return false;
                }
                MessageRequestWrap wrap = messageRequestWrapPool.Get();
                wrap.Content = msg.Data.ToBytes();
                wrap.RequestId = msg.RequestId;
                wrap.Path = msg.Path;

                var sendData = wrap.ToArray(msg.Connection.ServerType);
                bool res = await msg.Connection.Send(sendData);

                wrap.Reset();
                messageRequestWrapPool.Restore(wrap);

                if (res && msg.Connection.ServerType == ServerType.UDP)
                {
                    msg.Connection.UpdateTime(LastTime);
                }
                return res;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
            return false;
        }
        public async Task ReplyOnly(MessageResponseParamsInfo msg)
        {
            try
            {
                MessageResponseWrap wrap = new MessageResponseWrap
                {
                    Content = msg.Data.ToBytes(),
                    RequestId = msg.RequestId,
                    Code = msg.Code,
                };

                var sendData = wrap.ToArray(msg.Connection.ServerType);
                bool res = await msg.Connection.Send(sendData);
                if (res && msg.Connection.ServerType == ServerType.UDP)
                {
                    msg.Connection.UpdateTime(LastTime);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Debug(ex);
            }
        }

        public void Response(MessageResponseWrap wrap)
        {
            if (sends.TryRemove(wrap.RequestId, out SendCacheInfo send))
            {
                send.Tcs.SetResult(new MessageResponeInfo { Code = wrap.Code, Data = wrap.Memory });
            }
        }

        private TaskCompletionSource<MessageResponeInfo> NewReply(ulong requestId, int timeout = 15000)
        {
            TaskCompletionSource<MessageResponeInfo> tcs = new TaskCompletionSource<MessageResponeInfo>();
            if (timeout == 0)
            {
                timeout = 15000;
            }
            sends.TryAdd(requestId, new SendCacheInfo { Tcs = tcs, RequestId = requestId, Timeout = timeout });
            return tcs;
        }
    }

    public class MessageResponeInfo
    {
        public MessageResponeCodes Code { get; set; } = MessageResponeCodes.OK;
        public ReadOnlyMemory<byte> Data { get; set; } = Array.Empty<byte>();
    }

    public class SendCacheInfo
    {
        public TaskCompletionSource<MessageResponeInfo> Tcs { get; set; }
        public long Time { get; set; } = DateTimeHelper.GetTimeStamp();
        public ulong RequestId { get; set; } = 0;
        public int Timeout { get; set; } = 15000;
    }
}

