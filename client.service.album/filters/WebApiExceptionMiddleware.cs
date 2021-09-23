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
        readonly IWebHostEnvironment _env;
        readonly ILogger<WebApiExceptionMiddleware> logger;

        public WebApiExceptionMiddleware(IWebHostEnvironment env, ILogger<WebApiExceptionMiddleware> logger)
        {
            _env = env;
            this.logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            try
            {
                JsonResultModel result = new() { Code = 500, Msg = "未知错误，请稍后重试" };

                result.Msg = context.Exception.Message;
                if (_env.IsDevelopment())
                {
                    result.Msg = context.Exception.Message;
                }
                logger.LogError("错误:{0}\n堆栈:{1}",
                    context.Exception.Message,
                    context.Exception.StackTrace
                    );
                context.Result = new ObjectResult(result);
            }
            catch (Exception ex)
            {
                logger.LogError("错误拦截器异常:{0}\n堆栈:{1}", ex.Message, ex.StackTrace);
            }
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.OK;
            context.ExceptionHandled = true;
        }
    }
}
