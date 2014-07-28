using ContactPusher.Core;
using System;
using System.Web;
using System.Web.Mvc;

namespace ContactPusher.Web.UI.Controllers
{
	public class BaseController : Controller
	{
		protected IRepository _repository;
		protected IGoogleServices _googleServices;

		public BaseController(IRepository repository, IGoogleServices googleServices)
		{
			_repository = repository;
			_googleServices = googleServices;
		}

		protected string GetNameFromCache(Contact contact)
		{
			var itemFromCache = HttpContext.Cache[contact.GoogleId];

			if (itemFromCache == null)
			{
				itemFromCache = _googleServices.GetFullName(contact);
				HttpContext.Cache[contact.GoogleId] = itemFromCache;
			}

			return Convert.ToString(itemFromCache);
		}

		protected Authorization GetAuthorizationFromSession()
		{
			return (Authorization)Session["Authorization"];
		}

		protected int GetUserIdFromSession()
		{
			return (int)Session["UserId"];
		}
	}
}