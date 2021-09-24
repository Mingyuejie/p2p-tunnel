using common;
using common.extends;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.album.db
{
    public class VerifyCaching
    {
        private readonly IMemoryCache memoryCache;
        private readonly AlbumSettingModel albumSettingModel;
        public VerifyCaching(IMemoryCache memoryCache, AlbumSettingModel albumSettingModel)
        {
            this.memoryCache = memoryCache;
            this.albumSettingModel = albumSettingModel;
        }

        public string Sign(string password)
        {
            string token = string.Empty;
            if (password == albumSettingModel.Password)
            {
                token = $"{Guid.NewGuid()}_{Helper.GetTimeStamp()}".Md5();

                memoryCache.Set(token, 1, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(1)
                });
            }
            return token;
        }
        public bool Verify(string token)
        {
            return memoryCache.Get<int>(token) == 1;
        }

        public bool Verify(HttpRequest request)
        {
            if (request.Headers.ContainsKey("token"))
            {
                return Verify(request.Headers["token"]);
            }
            return false;
        }
    }
}
