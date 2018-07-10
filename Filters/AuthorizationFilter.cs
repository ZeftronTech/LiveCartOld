using System;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Globalization;
using LiveKart.Shared.Entities;
using LiveKart.Web.Models;
using LiveKart.Entities;

namespace LiveKart.Web.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class AuthorizationFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpContext ctx = HttpContext.Current;
            if (ctx.Session["ActiveUser"] != null)
				Thread.CurrentThread.CurrentCulture = new CultureInfo(((User)ctx.Session["ActiveUser"]).CultureCode);
            
            // If the browser session or authentication session has expired...
            if (!ctx.Request.IsAuthenticated || ctx.Session["ActiveUser"] == null)
            {
                // For round-trip posts, we're forcing a redirect to Home/TimeoutRedirect/, which
                // simply displays a temporary 5 second notification that they have timed out, and
                // will, in turn, redirect to the logon page.
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary {
                        { "Controller", "Account" },
                        { "Action", "Login" }
                });

            }
            base.OnActionExecuting(filterContext);
        }
    }
}