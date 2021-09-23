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
    public class CategoryController : BaseController
    {
        private readonly DBHelper<CategoryInfo> dbHelper;
        public CategoryController(DBHelper<CategoryInfo> dbHelper)
        {
            this.dbHelper = dbHelper;
        }

        [HttpGet]
        public async Task<IEnumerable<CategoryInfo>> List()
        {
            return await dbHelper.GetAll();
        }

        [HttpPost]
        public async Task<int> Add([FromBody] CategoryInfo model)
        {
            var old = await dbHelper.Get(model.ID) ?? new CategoryInfo { ID = 0 };
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
            await dbHelper.Execute($"DELETE {typeof(CategoryInfo).Name} WHERE ID in @ids", new { ids = ids.ToIntArray() });
            await dbHelper.Execute($"DELETE {typeof(AlbumInfo).Name} WHERE CID in @ids", new { ids = ids.ToIntArray() });
            return true;
        }
    }
}
