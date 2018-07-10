using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace LiveKart.Web
{
	public static class WebApiConfig
	{
		public static void Register(HttpConfiguration config)
		{
			// Allow for attribute based routes
			config.MapHttpAttributeRoutes();

			//config.Routes.MapHttpRoute(
			//	name: "DefaultApi",
			//	routeTemplate: "api/{controller}/{id}",
			//	defaults: new { id = RouteParameter.Optional }
			//);
			config.Routes.MapHttpRoute(
				name: "ActionApi",
				routeTemplate: "api/{controller}/{action}/{id}",
				defaults: new { id = RouteParameter.Optional });
			//GlobalConfiguration.Configuration.Formatters.Insert(0, new JsonpFormatter());

			config.EnableQuerySupport();
		}
	}
}
