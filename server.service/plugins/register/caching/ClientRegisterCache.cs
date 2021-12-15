using common;
using common.extends;
using server.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace server.service.plugins.register.caching
{
    public class ClientRegisterCaching : IClientRegisterCaching
    {
        private readonly ConcurrentDictionary<long, RegisterCacheModel> cache = new();
        private static long Id = 0;

        public SimplePushSubHandler<string> OnChanged => new SimplePushSubHandler<string>();

        public ClientRegisterCaching()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    long time = Helper.GetTimeStamp();

                    foreach (RegisterCacheModel item in cache.Values)
                    {
                        if (time - item.LastTime > 60 * 1000)
                        {
                            _ = cache.TryRemove(item.Id, out _);
                        }
                    }
                    await Task.Delay(100);
                }
            });
        }

        public RegisterCacheModel Get(long id)
        {
            _ = cache.TryGetValue(id, out RegisterCacheModel toReg);
            return toReg;
        }

        public RegisterCacheModel GetBySameGroup(string groupid, string name)
        {
            return cache.Values.FirstOrDefault(c => c.GroupId == groupid.Md5() && c.Name == name);
        }

        public List<RegisterCacheModel> GetAll()
        {
            return cache.Values.ToList();
        }

        public long Add(RegisterCacheModel model)
        {
            if (model.Id == 0)
            {
                Interlocked.Increment(ref Id);
                model.Id = Id;
            }
            if (string.IsNullOrWhiteSpace(model.OriginGroupId))
            {
                model.OriginGroupId = Guid.NewGuid().ToString().Md5();
            }

            model.GroupId = model.OriginGroupId.Md5();
            _ = cache.AddOrUpdate(model.Id, model, (a, b) => model);
            return model.Id;
        }

        public bool UpdateTcpInfo(RegisterCacheUpdateModel model)
        {
            RegisterCacheModel data = Get(model.Id);
            if (data != null && model.GroupId.Md5() == data.GroupId)
            {
                data.LastTime = Helper.GetTimeStamp();
                data.TcpSocket = model.TcpSocket;
                data.TcpPort = model.TcpPort;
                OnChanged.Push(data.GroupId);
                return true;
            }
            return false;
        }

        public void Remove(long id)
        {
            if (cache.TryRemove(id, out RegisterCacheModel model))
            {
                OnChanged.Push(model.GroupId);
            }
        }

        public void UpdateTime(long id)
        {
            RegisterCacheModel model = Get(id);
            if (model != null)
            {
                model.LastTime = Helper.GetTimeStamp();
            }
        }

        public bool Verify(long id, PluginParamWrap data)
        {
            RegisterCacheModel model = Get(id);
            if (model != null)
            {
                switch (data.ServerType)
                {
                    case ServerType.TCP:
                        return model.TcpSocket.RemoteEndPoint.ToString() == data.TcpSocket.RemoteEndPoint.ToString();
                    case ServerType.UDP:
                        return model.Address.Address.Equals(data.SourcePoint.Address);
                    default:
                        break;
                }
            }
            return false;
        }

    }
}
