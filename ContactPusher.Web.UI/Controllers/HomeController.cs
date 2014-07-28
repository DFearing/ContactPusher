using ContactPusher.Core;
using System.Web.Mvc;

namespace ContactPusher.Web.UI.Controllers
{
	public class HomeController : BaseController
	{
		public HomeController(IRepository repository, IGoogleServices googleServices) : base(repository, googleServices)
		{
		
		}

		public ActionResult Index()
		{
			return View();
		}
		
		public ActionResult Rejected()
		{
			return View();
		}
	}
}