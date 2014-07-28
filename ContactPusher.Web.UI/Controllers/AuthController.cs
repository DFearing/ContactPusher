using ContactPusher.Core;
using ContactPusher.Core.Exceptions;
using NLog;
using System;
using System.Linq;
using System.Web.Mvc;

namespace ContactPusher.Web.UI.Controllers
{
	public class AuthController : BaseController
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public AuthController(IRepository repository, IGoogleServices googleServices) : base(repository, googleServices)
		{

		}

		[HttpGet, ActionName("Request")]
		public ActionResult RequestAuth(string state = null)
		{
			return Redirect(_googleServices.CreateOAuth2AuthorizationUrl(state));
		}

		public ActionResult Callback(string code, string state)
		{
			if (String.IsNullOrEmpty(code))
			{
				return RedirectToAction("Rejected", "Home", new { state });
			}

			var authorization = _googleServices.GetAuthorization(code);
			var userInfo = _googleServices.GetUserInfo(authorization);

			if (userInfo != null)
			{
				var user = _repository.Query<User>().SingleOrDefault(x => x.GoogleId == userInfo.Id);

				if (user == null)
				{					
					user = new User
					{
						Authorization = authorization,
						Email = userInfo.Email,
						GoogleId = userInfo.Id
					};

					_repository.Add(authorization);
					_repository.Add(user);
				}
				else
				{
					if (authorization.ExpirationDate > user.Authorization.ExpirationDate)
					{
						user.Authorization.AccessToken = authorization.AccessToken;
						user.Authorization.ExpirationDate = authorization.ExpirationDate;
						user.Authorization.Revoked = false;
					}
				}

				_repository.Flush();
				
				Session["UserId"] = user.Id;
				Session["Name"] = userInfo.Name;
				Session["Authorization"] = authorization;

				if (!String.IsNullOrEmpty(state)) // This means we are adding a Contact to an existing User
				{
					Guid stateGuid = Guid.Empty;

					if (Guid.TryParse(state, out stateGuid))
					{
						var contact = _repository.Query<Contact>().SingleOrDefault(x => x.ExternalId == stateGuid);

						if (contact != null && !contact.Deleted && !contact.User.Authorization.Revoked)
						{
							try
							{
								_googleServices.AddOrUpdateContact(contact, user);
								TempData.Add("ExternalId", stateGuid);

								return RedirectToAction("Index", "Home");
							}
							catch (AccessRevokedException ex)
							{
								var sourceAuthorization = _repository.Query<Authorization>().Single(x => x.Id == ex.AuthorizationSource.Id);
								sourceAuthorization.Revoked = true;

								_repository.Update(sourceAuthorization);
								logger.WarnException("Caught Google AccessRevokedException", ex);
							}
							catch (NotFoundException ex)
							{
								contact.Deleted = true;
								_repository.Update(contact);

								logger.WarnException("Caught Google NotFoundException", ex);
							}

							return RedirectToAction("NotFound", "Contact");
						}
					}
				}

				return RedirectToAction("Pick", "Contact");
			}

			throw new InvalidOperationException();
		}
	}
}