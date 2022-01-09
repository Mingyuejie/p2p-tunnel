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
        private readonly ConcurrentDictionary<ulong, RegisterCacheModel> cache = new();
        private NumberSpace idNs = new NumberSpace(0);

        public SimplePushSubHandler<string> OnChanged { get; } = new SimplePushSubHandler<string>();

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
                            cache.TryRemove(item.Id, out _);
                        }
                    }
                    await Task.Delay(100);
                }
            });
        }

        public bool Get(ulong id, out RegisterCacheModel client)
        {
            return cache.TryGetValue(id, out client);
        }

        public RegisterCacheModel GetBySameGroup(string groupid, string name)
        {
            return cache.Values.FirstOrDefault(c => c.GroupId == groupid.Md5() && c.Name == name);
        }

        public List<RegisterCacheModel> GetAll()
        {
            return cache.Values.ToList();
        }

        public ulong Add(RegisterCacheModel model)
        {
            if (model.Id == 0)
            {
                model.Id = idNs.Get();
            }
            if (string.IsNullOrWhiteSpace(model.OriginGroupId))
            {
                model.OriginGroupId = Guid.NewGuid().ToString().Md5();
            }

            model.GroupId = model.OriginGroupId.Md5();
            cache.AddOrUpdate(model.Id, model, (a, b) => model);
            return model.Id;
        }

        public bool UpdateTcpInfo(RegisterCacheUpdateModel model)
        {
            if (Get(model.Id, out RegisterCacheModel client) && model.GroupId.Md5() == client.GroupId)
            {
                client.LastTime = Helper.GetTimeStamp();
                client.TcpConnection = model.TcpConnection;
                return true;
            }
            return false;
        }

        public void Remove(ulong id)
        {
            cache.TryRemove(id, out _);
        }

        public void UpdateTime(ulong id)
        {
            if (Get(id, out RegisterCacheModel client))
            {
                client.LastTime = Helper.GetTimeStamp();
            }
        }

        public bool Notify(IConnection connection)
        {
            if (Get(connection.ConnectId, out RegisterCacheModel client))
            {
                OnChanged.Push(client.GroupId);
            }
            return false;
        }
    }
}
