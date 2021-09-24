using client.service.album.db;
using client.service.album.filters;
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
        private readonly VerifyCaching verifyCaching;
        public AlbumController(DBHelper<AlbumInfo> dbHelper, VerifyCaching verifyCaching)
        {
            this.dbHelper = dbHelper;
            this.verifyCaching = verifyCaching;
        }

        [HttpGet]
        public async Task<IEnumerable<AlbumInfo>> List([FromQuery] int cid = 0)
        {
            return await dbHelper.Query($"SELECT * FROM {typeof(AlbumInfo).Name} WHERE CID=@cid", new { cid });
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AlbumInfo model)
        {
            if (!verifyCaching.Verify(Request))
            {
                return new ErrorResult("身份未验证");
            }

            var old = await dbHelper.Get(model.ID) ?? new AlbumInfo { ID = 0 };
            model.FormatObjectAttr();
            if (old.ID == 0)
            {
                model.AddTime = Helper.GetTimeStampSec();
                return new ObjectResult(await dbHelper.Add(model));
            }
            await dbHelper.Update(model);
            return new ObjectResult(old.ID);
        }

        [HttpPost]
        public async Task<IActionResult> Del([FromQuery] string ids)
        {
            if (!verifyCaching.Verify(Request))
            {
                return new ErrorResult("身份未验证");
            }

            await dbHelper.Execute($"DELETE {typeof(AlbumInfo).Name} WHERE ID in @ids", new { ids = ids.ToIntArray() });
            return new ObjectResult(true);
        }
    }
}
