using System.Web.Mvc;
using System.Web.Routing;

namespace Escademy
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Home",
                url: "",
                defaults: new { controller = "Home", action = "Index" }
            );
            routes.MapRoute(
                name: "Profiles",
                url: "profile/{id}",
                defaults: new { controller = "Profiles", action = "Profile",  id = UrlParameter.Optional }
    );
        }
    }
}
