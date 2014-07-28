using ContactPusher.Core;
using ContactPusher.Core.Exceptions;
using Google.GData.Client;
using Newtonsoft.Json;
using NLog;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using ContactsRequest = Google.Contacts.ContactsRequest;
using GoogleContact = Google.Contacts.Contact;
using GoogleGroup = Google.Contacts.Group;
using HttpStatusCode = System.Net.HttpStatusCode;
using WebClient = System.Net.WebClient;

namespace ContactPusher.Services
{
	public class GoogleServices : IGoogleServices
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private RequestSettings _settings;
		private RestClient _client;
		protected OAuth2Parameters _parameters;
		private const string GROUPS_FEED_URL = "https://www.google.com/m8/feeds/groups/default/{0}";
		private const string CONTACTS_FEED_URL = "https://www.google.com/m8/feeds/contacts/default/{0}";
		private const string GROUP_MEMBERSHIP_URL = "http://www.google.com/m8/feeds/groups/{0}/base/{1}";
		private const string PHOTO_URL = "https://www.google.com/m8/feeds/photos/media/default/{0}";

		public enum FeedType
		{
			Full, Thin
		}

		public string GetContactsFeedUri(FeedType type)
		{
			return String.Format(CONTACTS_FEED_URL, type.ToString().ToLower());
		}

		public string GetGroupsFeedUri(FeedType type)
		{
			return String.Format(GROUPS_FEED_URL, type.ToString().ToLower());
		}

		public GoogleServices()
		{
			_client = new RestClient();
			_parameters = new OAuth2Parameters
			{
				ClientId = ConfigurationManager.AppSettings["Google.ClientId"],
				ClientSecret = ConfigurationManager.AppSettings["Google.ClientSecret"],
				RedirectUri = ConfigurationManager.AppSettings["Google.RedirectUris"],
				Scope = ConfigurationManager.AppSettings["Google.ContactScope"],
				//ApprovalPrompt = "force",
				AccessType = "offline",
			};
			_settings = new RequestSettings("ContactPusher", _parameters);
		}

		public string CreateGroup(Authorization authorization)
		{
			EnsureValidAccessToken(authorization);
			UpdateParameters(authorization);

			var request = new ContactsRequest(_settings).Insert<GoogleGroup>(new Uri(GetGroupsFeedUri(FeedType.Full)), new GoogleGroup
			{
				Title = "ContactPusher"
			});

			return request.Id.Substring(request.Id.LastIndexOf("/") + 1);
		}

		public string GetFullName(Contact contact)
		{
			EnsureValidAccessToken(contact.User.Authorization);
			UpdateParameters(contact.User.Authorization);

			var googleContact = GetContact(contact.User.Authorization, contact.GoogleId);

			if (googleContact == null)
			{
				throw new NotFoundException();
			}
			else
			{
				return googleContact.Name.FullName;
			}
		}

		public string CreateOAuth2AuthorizationUrl(string state)
		{
			_parameters.State = state;

			return OAuthUtil.CreateOAuth2AuthorizationUrl(_parameters);
		}

		public Authorization GetAuthorization(string code)
		{
			_parameters.AccessCode = code;

			OAuthUtil.GetAccessToken(_parameters);

			return new Authorization
			{
				AccessToken = _parameters.AccessToken,
				RefreshToken = _parameters.RefreshToken,
				ExpirationDate = _parameters.TokenExpiry,
				TokenType = _parameters.TokenType
			};
		}

		public GoogleUserInfo GetUserInfo(Authorization authorization)
		{
			EnsureValidAccessToken(authorization);
			var request = new RestRequest("https://www.googleapis.com/oauth2/v1/userinfo", Method.GET);
			request.AddParameter("access_token", authorization.AccessToken);
			request.RequestFormat = DataFormat.Json;

			var response = _client.Execute(request);

			if (response.StatusCode == HttpStatusCode.OK)
			{
				var data = JsonConvert.DeserializeObject<dynamic>(response.Content);

				return new GoogleUserInfo
				{
					Id = data.id.Value,
					Name = data.name.Value,
					Email = data.email.Value
				};
			}

			return null;
		}

		public IEnumerable<GoogleUserInfo> SearchContacts(Authorization authorization, string term)
		{
			var results = new List<GoogleUserInfo>();
			EnsureValidAccessToken(authorization);

			var request = new RestRequest(GetGroupsFeedUri(FeedType.Thin), Method.GET);
			AddAuthParameters(request, authorization);
			request.AddParameter("q", term);
			request.AddParameter("alt", "json");

			var response = _client.Execute(request);

			if (response.StatusCode == HttpStatusCode.OK)
			{
				var data = JsonConvert.DeserializeObject<dynamic>(response.Content);
			}

			return results;
		}

		public IEnumerable<GoogleUserInfo> GetAllContacts(Authorization authorization)
		{
			UpdateParameters(authorization);

			var request = new ContactsRequest(_settings);
			request.Settings.Maximum = Int32.MaxValue;
			request.Settings.PageSize = Int32.MaxValue;

			return request.GetContacts().Entries.Where(x => x.Name.FullName != null).Select(x => new GoogleUserInfo
			{
				Name = x.Name.FullName,
				Id = x.Id.Substring(x.Id.LastIndexOf("/") + 1),
				Phone = x.PrimaryPhonenumber != null ? x.PrimaryPhonenumber.Value : null,
				Email = x.PrimaryEmail != null ? x.PrimaryEmail.Address : null,
			}).OrderBy(x => x.Name);
		}

		public void AddOrUpdateContact(Contact sourceContact, User destinationUser)
		{
			var newGroup = false;
			var newContact = false;

			EnsureValidAccessToken(sourceContact.User.Authorization);
			EnsureValidAccessToken(destinationUser.Authorization);

			// Check for our Group
			if (String.IsNullOrEmpty(destinationUser.GroupGoogleId))
			{
				destinationUser.GroupGoogleId = CreateGroup(destinationUser.Authorization);
				newGroup = true;
			}
			else
			{
				if (GetGroup(destinationUser.Authorization, destinationUser.GroupGoogleId) == null) // Group was Deleted
				{
					destinationUser.GroupGoogleId = CreateGroup(destinationUser.Authorization);
					newGroup = true;
				}
			}

			// Get Source Contact
			var sourceGoogleContact = GetContact(sourceContact.User.Authorization, sourceContact.GoogleId);

			if (sourceGoogleContact == null) throw new NotFoundException();

			GoogleContact destinationGoogleContact = null;

			if (destinationUser.Contacts.Any(x => x.Key.Id == sourceContact.Id))
			{
				destinationGoogleContact = GetContact(destinationUser.Authorization, destinationUser.Contacts.SingleOrDefault(x => x.Key.Id == sourceContact.Id).Value);
			}

			if (destinationGoogleContact == null)
			{
				destinationGoogleContact = new GoogleContact();
				destinationUser.Contacts.Remove(sourceContact);
				newContact = true;
			}

			if (sourceGoogleContact.Updated > sourceContact.LastUpdated || newContact)
			{
				// Copy all the data from Source to Destination
				CopyData(sourceGoogleContact, destinationGoogleContact);

				string destinationId = null;

				if (newContact || newGroup)
				{
					// Assign to Group
					destinationGoogleContact.GroupMembership.Add(new Google.GData.Contacts.GroupMembership
					{
						HRef = String.Format(GROUP_MEMBERSHIP_URL, destinationUser.Email, destinationUser.GroupGoogleId)
					});
				}

				if (newContact)
				{
					destinationId = CreateContact(destinationUser.Authorization, destinationGoogleContact);
				}
				else
				{
					UpdateContact(destinationUser.Authorization, destinationGoogleContact);
					destinationId = destinationGoogleContact.Id.Substring(destinationGoogleContact.Id.LastIndexOf("/") + 1);
				}

				// Do we have a photo?
				if (sourceGoogleContact.PhotoEtag != null)
				{
					var sourcePhoto = GetPhoto(sourceContact.User.Authorization, sourceContact.GoogleId);

					if (sourcePhoto != null)
					{
						try
						{
							SetPhoto(destinationUser.Authorization, destinationId, sourcePhoto); // Copy the Photo
						}
						catch (Exception ex)
						{
							logger.ErrorException("Updating Photo", ex);
						}
					}
				}

				if (newContact)
				{
					destinationUser.Contacts.Add(sourceContact, destinationId);
				}
			}			
		}

		private void CopyData(GoogleContact source, GoogleContact destination)
		{
			// Copy the Name
			destination.Name.NamePrefix = source.Name.NamePrefix;
			destination.Name.GivenName = source.Name.GivenName;
			destination.Name.FamilyName = source.Name.FamilyName;
			destination.Name.NameSuffix = source.Name.NameSuffix;
			destination.Name.AdditionalName = source.Name.AdditionalName;
			if (!String.IsNullOrEmpty(source.Name.AdditionalNamePhonetics)) destination.Name.AdditionalNamePhonetics = source.Name.AdditionalNamePhonetics;
			if (!String.IsNullOrEmpty(source.Name.FamilyNamePhonetics)) destination.Name.FamilyNamePhonetics = source.Name.FamilyNamePhonetics;
			destination.Name.FullName = source.Name.FullName;
			if (!String.IsNullOrEmpty(source.Name.GivenNamePhonetics)) destination.Name.GivenNamePhonetics = source.Name.GivenNamePhonetics;

			// Copy the Note
			destination.Content = source.Content;

			// Copy Contact Entry
			destination.ContactEntry.Birthday = source.ContactEntry.Birthday;
			destination.ContactEntry.Nickname = source.ContactEntry.Nickname;

			// Copy Phone Numbers
			destination.Phonenumbers.Clear();
			foreach (var sourceNumber in source.Phonenumbers)
			{
				var number = new Google.GData.Extensions.PhoneNumber
				{
					Label = sourceNumber.Label,
					Primary = sourceNumber.Primary,
					Value = sourceNumber.Value
				};

				if (number.Label == null && sourceNumber.Rel != null)
				{
					number.Rel = sourceNumber.Rel;
				}

				destination.Phonenumbers.Add(number);
			}

			// Copy Emails
			destination.Emails.Clear();
			foreach (var sourceEmail in source.Emails)
			{
				var email = new Google.GData.Extensions.EMail
				{
					Address = sourceEmail.Address,
					Label = sourceEmail.Label,
					Primary = sourceEmail.Primary
				};

				if (email.Label == null && sourceEmail.Rel != null)
				{
					email.Rel = sourceEmail.Rel;
				}

				destination.Emails.Add(email);
			}

			// Copy Postal Addresses
			destination.PostalAddresses.Clear();
			foreach (var sourceAddress in source.PostalAddresses)
			{
				var address = new Google.GData.Extensions.StructuredPostalAddress
				{
					Agent = sourceAddress.Agent,
					City = sourceAddress.City,
					Country = sourceAddress.Country,
					FormattedAddress = sourceAddress.FormattedAddress,
					Housename = sourceAddress.Housename,
					Label = sourceAddress.Label,
					MailClass = sourceAddress.MailClass,
					Neighborhood = sourceAddress.Neighborhood,
					Pobox = sourceAddress.Pobox,
					Postcode = sourceAddress.Postcode,
					Primary = sourceAddress.Primary,
					Region = sourceAddress.Region,
					Street = sourceAddress.Street,
					Subregion = sourceAddress.Subregion,
					Usage = sourceAddress.Usage
				};

				if (address.Label == null && sourceAddress.Rel != null)
				{
					address.Rel = sourceAddress.Rel;
				}

				destination.PostalAddresses.Add(address);
			}

			// Copy IM
			destination.IMs.Clear();
			foreach (var sourceIM in source.IMs)
			{
				var im = new Google.GData.Extensions.IMAddress
				{
					Protocol = sourceIM.Protocol,
					Address = sourceIM.Address,
					Label = sourceIM.Label,
					Primary = sourceIM.Primary,
				};

				if (im.Label == null && sourceIM.Rel != null)
				{
					im.Rel = sourceIM.Rel;
				}

				destination.IMs.Add(im);
			}

			// Copy Organization
			destination.Organizations.Clear();
			foreach (var sourceOrganization in source.Organizations)
			{
				var organization = new Google.GData.Extensions.Organization
				{
					Department = sourceOrganization.Department,
					JobDescription = sourceOrganization.JobDescription,
					Location = sourceOrganization.Location,
					Name = sourceOrganization.Name,
					Symbol = sourceOrganization.Symbol,
					Title = sourceOrganization.Title,
					Label = sourceOrganization.Label,
					Primary = sourceOrganization.Primary
				};

				if (organization.Label == null && sourceOrganization.Rel != null)
				{
					organization.Rel = sourceOrganization.Rel;
				}

				destination.Organizations.Add(organization);
			}

			// Copy User Defined Fields
			destination.ContactEntry.UserDefinedFields.Clear();
			foreach (var userProperty in source.ContactEntry.UserDefinedFields)
			{
				destination.ContactEntry.UserDefinedFields.Add(new Google.GData.Contacts.UserDefinedField
				{
					Key = userProperty.Key,
					Value = userProperty.Value
				});
			}

			// Copy Events
			destination.ContactEntry.Events.Clear();
			foreach (var @event in source.ContactEntry.Events)
			{
				destination.ContactEntry.Events.Add(new Google.GData.Contacts.Event
				{
					Label = @event.Label,
					Relation = @event.Relation,
					When = @event.When
				});
			}

			// Copy Websites
			destination.ContactEntry.Websites.Clear();
			foreach (var sourceSite in source.ContactEntry.Websites)
			{
				var site = new Google.GData.Contacts.Website
				{
					Primary = sourceSite.Primary,
					Href = sourceSite.Href,
					Label = sourceSite.Label,
				};

				if (site.Label == null && sourceSite.Rel != null)
				{
					site.Rel = sourceSite.Rel;
				}

				destination.ContactEntry.Websites.Add(site);
			}

			// Copy Relations
			destination.ContactEntry.Relations.Clear();
			foreach (var sourceRelation in source.ContactEntry.Relations)
			{
				var relation = new Google.GData.Contacts.Relation
				{
					Label = sourceRelation.Label,
					Value = sourceRelation.Value
				};

				if (relation.Label == null && sourceRelation.Rel != null)
				{
					relation.Rel = sourceRelation.Rel;
				}

				destination.ContactEntry.Relations.Add(relation);
			}
		}

		public GoogleContact GetContact(Authorization authorization, string id)
		{
			EnsureValidAccessToken(authorization);
			UpdateParameters(authorization);

			var request = new ContactsRequest(_settings).Get<GoogleContact>(new Uri(String.Format("{0}/{1}", GetContactsFeedUri(FeedType.Full), id)));

			if (request.Entries != null)
			{
				try
				{
					return request.Entries.FirstOrDefault();
				}
				catch (GDataRequestException ex)
				{
					if (ex.Response is System.Net.HttpWebResponse)
					{
						var webResponse = ex.Response as System.Net.HttpWebResponse;

						if (webResponse.StatusCode == HttpStatusCode.Unauthorized)
						{
							throw new AccessRevokedException(authorization);
						}
					}
					else if (ex.ResponseString != "Contact not found.")
					{
						throw;
					}
				}
			}

			return null;
		}

		public byte[] GetPhoto(Authorization authorization, string id)
		{
			EnsureValidAccessToken(authorization);

			var request = new RestRequest(String.Format(PHOTO_URL, id), Method.GET);
			request.AddHeader("Accept", "*/*");
			request.AddParameter("access_token", authorization.AccessToken);

			var response = _client.Execute(request);

			if (response.StatusCode == HttpStatusCode.OK)
			{
				return response.RawBytes;
			}

			return null;
		}

		public void SetPhoto(Authorization authorization, string id, byte[] photoData)
		{
			EnsureValidAccessToken(authorization);

			// RestSharp can't handle this correctly
			using (var client = new WebClient())
			{
				client.Headers.Add("GData-Version", "3.0");
				client.Headers.Add("Content-Type", "image/*");
				client.Headers.Add("If-Match", "*");
				client.Headers.Add("access_token", authorization.AccessToken);
				client.Headers.Add("Authorization", String.Format("{0} {1}", authorization.TokenType, authorization.AccessToken));
				client.UploadData(String.Format(PHOTO_URL, id), "PUT", photoData);
			}
		}

		public GoogleGroup GetGroup(Authorization authorization, string id)
		{
			EnsureValidAccessToken(authorization);
			UpdateParameters(authorization);

			var request = new ContactsRequest(_settings).Get<GoogleGroup>(new Uri(String.Format("{0}/{1}", GetGroupsFeedUri(FeedType.Full), id)));

			if (request.Entries != null)
			{
				try
				{
					return request.Entries.FirstOrDefault();
				}
				catch (GDataRequestException ex)
				{
					if (ex.ResponseString != "Group not found.")
					{
						throw;
					}
				}
			}

			return null;
		}

		private string CreateContact(Authorization authorization, GoogleContact contact)
		{
			EnsureValidAccessToken(authorization);
			UpdateParameters(authorization);

			var request = new ContactsRequest(_settings).Insert<GoogleContact>(new Uri(GetContactsFeedUri(FeedType.Full)), contact);

			return request.Id.Substring(request.Id.LastIndexOf("/") + 1);
		}

		private void UpdateContact(Authorization authorization, GoogleContact contact)
		{
			EnsureValidAccessToken(authorization);
			UpdateParameters(authorization);

			new ContactsRequest(_settings).Update<GoogleContact>(contact);
		}

		private void EnsureValidAccessToken(Authorization authorization)
		{
			if (DateTime.UtcNow > authorization.ExpirationDate)
			{
				var request = new RestRequest("https://accounts.google.com/o/oauth2/token", Method.POST);
				request.RequestFormat = DataFormat.Json;
				request.AddParameter("refresh_token", authorization.RefreshToken);
				request.AddParameter("client_id", ConfigurationManager.AppSettings["Google.ClientId"]);
				request.AddParameter("client_secret", ConfigurationManager.AppSettings["Google.ClientSecret"]);
				request.AddParameter("grant_type", "refresh_token");

				var response = _client.Execute(request);

				if (response.StatusCode == HttpStatusCode.OK)
				{
					var data = JsonConvert.DeserializeObject<dynamic>(response.Content);

					authorization.AccessToken = data.access_token.Value;
					authorization.TokenType = data.token_type.Value;
					authorization.ExpirationDate = DateTime.UtcNow.AddSeconds(data.expires_in.Value);
				}
				else if (response.StatusCode == HttpStatusCode.BadRequest)
				{
					var data = JsonConvert.DeserializeObject<dynamic>(response.Content);

					if (data.error == "invalid_grant")
					{
						throw new AccessRevokedException(authorization);
					}
				}
			}
		}

		private void AddAuthParameters(IRestRequest request, Authorization authorization)
		{
			request.AddHeader("Authorization", String.Format("{0} {1}", authorization.TokenType, authorization.AccessToken));
			request.AddHeader("GData-Version", "3.0");
		}

		private void UpdateParameters(Authorization authorization)
		{
			_parameters.AccessToken = authorization.AccessToken;
			_parameters.RefreshToken = authorization.RefreshToken;
		}
	}
}