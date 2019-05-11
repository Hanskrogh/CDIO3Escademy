using System.Web.Optimization;

namespace Escademy
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            /*
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));
            */

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            /*
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js"));
            */

            bundles.Add(new ScriptBundle("~/bundles/vanta").Include(
                        "~/Scripts/three.r92.min.js",
                        "~/Scripts/vanta.waves.min.js"));



            bundles.Add(new StyleBundle("~/Content/cssb").Include(
                      "~/Content/album.css",
                      "~/Content/hover.css",
                      "~/Content/animated-bg.css",
                      "~/Content/site-footer.css"));
        }
    }
}
