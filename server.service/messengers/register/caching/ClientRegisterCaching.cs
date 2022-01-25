using common;
using common.extends;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace server.service.messengers.register.caching
{
    public class ClientRegisterCaching : IClientRegisterCaching
    {
        private readonly ConcurrentDictionary<ulong, RegisterCacheInfo> cache = new();
        private NumberSpace idNs = new NumberSpace(0);

        public SimpleSubPushHandler<string> OnChanged { get; } = new SimpleSubPushHandler<string>();

        public ClientRegisterCaching()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    long time = DateTimeHelper.GetTimeStamp();
                    foreach (RegisterCacheInfo item in cache.Values)
                    {
                        if (time - item.UdpConnection.LastTime > 60 * 1000)
                        {
                            await Remove(item.Id);
                        }
                    }
                    await Task.Delay(100);
                }
            }, TaskCreationOptions.LongRunning);
        }

        public bool Get(ulong id, out RegisterCacheInfo client)
        {
            return cache.TryGetValue(id, out client);
        }

        public RegisterCacheInfo GetBySameGroup(string groupid, string name)
        {
            return cache.Values.FirstOrDefault(c => c.GroupId == groupid.Md5() && c.Name == name);
        }

        public List<RegisterCacheInfo> GetAll()
        {
            return cache.Values.ToList();
        }

        public ulong Add(RegisterCacheInfo model)
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

        public async Task<bool> Remove(ulong id)
        {
            if (cache.TryRemove(id, out RegisterCacheInfo client))
            {
                await OnChanged.PushAsync(client.GroupId);
                return true;
            }
            return false;
        }

        public async Task<bool> Notify(IConnection connection)
        {
            if (Get(connection.ConnectId, out RegisterCacheInfo client))
            {
                await OnChanged.PushAsync(client.GroupId);
            }
            return false;
        }
    }
}
