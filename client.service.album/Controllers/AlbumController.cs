using client.service.album.db;
using common;
using common.extends;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.album.Controllers
{
    public class AlbumController : BaseController
    {
        private readonly DBHelper<AlbumInfo> dbHelper;
        public AlbumController(DBHelper<AlbumInfo> dbHelper)
        {
            this.dbHelper = dbHelper;
        }

        [HttpGet]
        public async Task<IEnumerable<AlbumInfo>> List()
        {
            return await dbHelper.GetAll();
        }

        [HttpPost]
        public async Task<int> Add([FromBody] AlbumInfo model)
        {
            var old = await dbHelper.Get(model.ID) ?? new AlbumInfo { ID = 0 };
            model.FormatObjectAttr();
            if (old.ID == 0)
            {
                model.AddTime = Helper.GetTimeStampSec();
                return await dbHelper.Add(model);
            }
            await dbHelper.Update(model);
            return old.ID;
        }

        [HttpPost]
        public async Task<bool> Del([FromQuery] string ids)
        {
            await dbHelper.Execute($"DELETE {typeof(AlbumInfo).Name} WHERE ID in @ids", new { ids = ids.ToIntArray() });
            return true;
        }
    }
}
