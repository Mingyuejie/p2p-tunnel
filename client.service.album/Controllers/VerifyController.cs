using client.service.album.db;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.album.Controllers
{
    public class VerifyController : BaseController
    {
        private readonly VerifyCaching verifyCaching;
        public VerifyController(VerifyCaching verifyCaching)
        {
            this.verifyCaching = verifyCaching;
        }
        [HttpPost]
        public string Verify([FromQuery] string password)
        {
            return verifyCaching.Sign(password);
        }
    }
}
