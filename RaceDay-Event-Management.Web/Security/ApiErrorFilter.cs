using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;
namespace RaceDay.Web.Security;
public class ApiErrorFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is ApiException api)
        {
            if (api.StatusCode == HttpStatusCode.Unauthorized)
            {
                context.HttpContext.Session.Clear();
                context.Result = new RedirectToActionResult("Login", "Account", new { expired = true });
            }
            else
            {
                context.HttpContext.Response.StatusCode = (int)api.StatusCode;
                context.Result = new ViewResult { ViewName = "~/Views/Home/Message.cshtml", ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<string>(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), context.ModelState) { Model = api.Message } };
            }
        }
        else if (context.Exception is HttpRequestException or TaskCanceledException)
        {
            context.HttpContext.Response.StatusCode = 503;
            context.Result = new ViewResult { ViewName = "~/Views/Home/Message.cshtml", ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<string>(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), context.ModelState) { Model = "RaceDay's service is temporarily unavailable. Please try again shortly." } };
        }
        else return;
        context.ExceptionHandled = true;
    }
}
