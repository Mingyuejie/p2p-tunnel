﻿using common;
using common.extends;
using server.model;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    public class MessengerResolver
    {

        private readonly Dictionary<ReadOnlyMemory<byte>, MessengerCacheInfo> messengers = new(new MemoryByteDictionaryComparer());

        //private readonly Dictionary<string, MessengerCacheInfo> plugins = new();

        private readonly ITcpServer tcpserver;
        private readonly IUdpServer udpserver;
        private readonly MessengerSender messengerSender;


        public MessengerResolver(IUdpServer udpserver, ITcpServer tcpserver, MessengerSender messengerSender)
        {
            this.tcpserver = tcpserver;
            this.udpserver = udpserver;
            this.messengerSender = messengerSender;

            this.tcpserver.OnPacket.SubAsync(async (IConnection connection) =>
            {
                await InputData(connection);
            });
            this.udpserver.OnPacket.SubAsync(async (IConnection connection) =>
            {
                connection.UpdateTime(messengerSender.LastTime);
                await InputData(connection);
            });
        }
        public void LoadMessenger(Type type, object obj)
        {
            Type voidType = typeof(void);
            string path = type.Name.Replace("Messenger", "");
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                Memory<byte> key = $"{path}/{method.Name}".ToLower().GetBytes().AsMemory();
                if (!messengers.ContainsKey(key))
                {
                    MessengerCacheInfo cache = new MessengerCacheInfo
                    {
                        IsVoid = method.ReturnType == voidType,
                        Method = method,
                        Target = obj,
                        IsTask = method.ReturnType.GetProperty("IsCompleted") != null && method.ReturnType.GetMethod("GetAwaiter") != null,
                        IsTaskResult = method.ReturnType.GetProperty("Result") != null
                    };
                    messengers.TryAdd(key, cache);
                }
            }
        }

        public async Task InputData(IConnection connection)
        {
            var receive = connection.ReceiveDataWrap;
            var wrapResponse = connection.ReceiveResponseWrap;
            var wrapRequest = connection.ReceiveRequestWrap;

            MessageTypes type = (MessageTypes)receive.Data.Span[0];
            //回复的消息
            if (type == MessageTypes.RESPONSE)
            {
                wrapResponse.FromArray(receive.Data);
                if (connection.EncodeEnabled)
                {
                    wrapResponse.Memory = connection.Crypto.Decode(wrapResponse.Memory);
                }

                messengerSender.Response(wrapResponse);
                return;
            }

            wrapRequest.FromArray(receive.Data);
            if (connection.EncodeEnabled)
            {
                wrapRequest.Memory = connection.Crypto.Decode(wrapRequest.Memory);
            }

            try
            {
                //404,没这个插件
                if (!messengers.ContainsKey(wrapRequest.Path))
                {
                    Logger.Instance.Error($"{wrapRequest.Path.Span.GetString()} fot found");
                    await messengerSender.ReplyOnly(new MessageResponseParamsInfo
                    {
                        Connection = connection,
                        RequestId = wrapRequest.RequestId,
                        Code = MessageResponeCodes.NOT_FOUND
                    });
                    return;
                }

                MessengerCacheInfo plugin = messengers[wrapRequest.Path];
                dynamic resultAsync = plugin.Method.Invoke(plugin.Target, new object[] { connection });
                //void的，task的 没有返回值，不回复，需要回复的可以返回任意类型
                if (plugin.IsVoid)
                {
                    return;
                }
                if (plugin.IsTask && !plugin.IsTaskResult)
                {
                    return;
                }

                object resultObject = null;
                if (plugin.IsTask)
                {
                    await resultAsync;
                    if (plugin.IsTaskResult)
                    {
                        resultObject = resultAsync.Result;
                    }
                }
                else
                {
                    resultObject = resultAsync;
                }

                await messengerSender.ReplyOnly(new MessageResponseParamsInfo
                {
                    Connection = connection,
                    Data = resultObject != null ? resultObject.ToBytes() : Array.Empty<byte>(),
                    RequestId = wrapRequest.RequestId
                });
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                await messengerSender.ReplyOnly(new MessageResponseParamsInfo
                {
                    Connection = connection,
                    RequestId = wrapRequest.RequestId,
                    Code = MessageResponeCodes.ERROR
                });
            }
            finally
            {
                wrapRequest.Reset();
            }
        }

        private struct MessengerCacheInfo
        {
            public object Target { get; set; }
            public MethodInfo Method { get; set; }
            public bool IsVoid { get; set; }
            public bool IsTask { get; set; }
            public bool IsTaskResult { get; set; }
        }
    }
}