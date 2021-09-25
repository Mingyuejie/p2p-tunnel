using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace client.service.album.filters
{
    /// <summary>
    /// 异常处理中间件
    /// </summary>
    public class WebApiExceptionMiddleware : IExceptionFilter
    {
        public WebApiExceptionMiddleware()
        {
        }

        public void OnException(ExceptionContext context)
        {
            context.Result = new ObjectResult(new JsonResultModel() { Code = 500, Msg = context.Exception.Message });
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.OK;
            context.ExceptionHandled = true;
        }
    }
}
