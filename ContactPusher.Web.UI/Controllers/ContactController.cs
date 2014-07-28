using ContactPusher.Core;
using ContactPusher.Core.Exceptions;
using ContactPusher.Web.UI.Models;
using NLog;
using RestSharp;
using System;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;

namespace ContactPusher.Web.UI.Controllers
{
	public class ContactController : BaseController
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public ContactController(IRepository repository, IGoogleServices googleServices) : base(repository, googleServices)
		{

		}

		public ActionResult Pick()
		{
			return View(new ContactPickModel
			{
				Filter = Convert.ToString(Session["Name"])
			});
		}

		public ActionResult GetContacts()
		{
			return Json(_googleServices.GetAllContacts(GetAuthorizationFromSession()).ToArray(), JsonRequestBehavior.AllowGet);
		}

		public ActionResult NotFound()
		{
			return View();
		}

		public ActionResult Share(string id)
		{
			var contact = _repository.Query<Contact>().SingleOrDefault(x => x.GoogleId == id);

			if (contact == null)
			{
				var googleContact = _googleServices.GetContact(GetAuthorizationFromSession(), id);

				contact = new Contact
				{
					GoogleId = id,
					LastUpdated = googleContact.Updated,
					ExternalId = Guid.NewGuid(),
					User = new User { Id = GetUserIdFromSession() },
				};

				_repository.Add(contact);
			}

			var retrieveUrl = Url.Action("Retrieve", "Contact", new { id = contact.ExternalId }, "http");
			var client = new RestClient("https://www.googleapis.com");
			var shortenerRequest = new RestRequest("urlshortener/v1/url", Method.POST);
			shortenerRequest.RequestFormat = DataFormat.Json;
			shortenerRequest.AddBody(new { longUrl = retrieveUrl, key = ConfigurationManager.AppSettings["Google.ApiKey"] });
			var response = client.Execute<ShortenedUrl>(shortenerRequest);

			if (response.StatusCode == System.Net.HttpStatusCode.OK)
			{
				TempData["ShortUrl"] = response.Data.Id;
			}

			return RedirectToAction("Retrieve", "Contact", new { id = contact.ExternalId });
		}

		public ActionResult Retrieve(Guid id)
		{
			var contact = _repository.Query<Contact>().SingleOrDefault(x => x.ExternalId == id);

			try
			{
				if (contact != null && !contact.Deleted && !contact.User.Authorization.Revoked)
				{
					return View(new ContactRetrieveModel
					{
						Id = id,
						Contact = new GoogleUserInfo
						{
							Id = contact.GoogleId,
							Name = GetNameFromCache(contact)
						}
					});
				}
			}
			catch (AccessRevokedException ex)
			{
				var authorization = _repository.Query<Authorization>().Single(x => x.Id == ex.AuthorizationSource.Id);
				authorization.Revoked = true;

				_repository.Update(authorization);
				logger.WarnException("Caught Google AccessRevokedException", ex);			
			}
			catch(NotFoundException ex)
			{
				contact.Deleted = true;
				_repository.Update(contact);

				logger.WarnException("Caught Google NotFoundException", ex);				
			}

			return View("NotFound");
		}

		public ActionResult Import(Guid id)
		{
			var contact = _repository.Query<Contact>().SingleOrDefault(x => x.ExternalId == id);

			if (contact != null && !contact.Deleted && !contact.User.Authorization.Revoked)
			{
				return RedirectToAction("Request", "Auth", new { state = id });
			}

			return View("NotFound");
		}

		public ActionResult Download(Guid id)
		{
			var contact = _repository.Query<Contact>().SingleOrDefault(x => x.ExternalId == id);

			try
			{
				if (contact != null && !contact.Deleted && !contact.User.Authorization.Revoked)
				{
					return View(new ContactDownloadModel
					{
						Id = id,
						Contact = new GoogleUserInfo
						{
							Id = contact.GoogleId,
							Name = GetNameFromCache(contact)
						}
					});
				}
			}
			catch (AccessRevokedException ex)
			{
				var authorization = _repository.Query<Authorization>().Single(x => x.Id == ex.AuthorizationSource.Id);
				authorization.Revoked = true;

				_repository.Update(authorization);
				logger.WarnException("Caught Google AccessRevokedException", ex);
			}
			catch (NotFoundException ex)
			{
				contact.Deleted = true;
				_repository.Update(contact);

				logger.WarnException("Caught Google NotFoundException", ex);
			}

			return View("NotFound");
		}

		public ActionResult ImportSuccess(Guid id)
		{
			try
			{ 
				var contact = _repository.Query<Contact>().SingleOrDefault(x => x.ExternalId == id);

				if (contact != null && !contact.Deleted && !contact.User.Authorization.Revoked)
				{
					return View(new ContactImportSuccessModel
					{
						Id = id,
						Contact = new GoogleUserInfo
						{
							Id = contact.GoogleId,
							Name = GetNameFromCache(contact)
						}
					});
				}
			}
			catch (GoogleException ex)
			{
				logger.WarnException("Caught Google Exception", ex);
			}

			return View("NotFound");
		}

		public ActionResult ShareSuccess(Guid id, string url)
		{
			try
			{ 
				var contact = _repository.Query<Contact>().SingleOrDefault(x => x.ExternalId == id);

				if (contact != null && !contact.Deleted && !contact.User.Authorization.Revoked)
				{
					return View(new ContactShareSuccessModel
					{
						Id = id,
						Contact = new GoogleUserInfo
						{
							Id = contact.GoogleId,
							Name = GetNameFromCache(contact)
						},
						ShortUrl = url
					});
				}
			}
			catch (GoogleException ex)
			{
				logger.WarnException("Caught Google Exception", ex);
			}

			return View("NotFound");
		}
	}
}