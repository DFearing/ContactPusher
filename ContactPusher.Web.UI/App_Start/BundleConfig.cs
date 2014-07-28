using System.Web;
using System.Web.Optimization;

namespace ContactPusher.Web.UI
{
	public class BundleConfig
	{
		// For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725
		public static void RegisterBundles(BundleCollection bundles)
		{
			bundles.Add(new StyleBundle("~/bundles/master.css").Include(
				"~/Content/css/bootstrap.css",
				"~/Content/css/site.css"));

			bundles.Add(new ScriptBundle("~/bundles/master.js").Include(
				"~/Scripts/jquery-{version}.js",
				"~/Scripts/bootstrap.js",
				"~/Scripts/mustache.js",
				"~/Scripts/site.js"));
		}
	}
}