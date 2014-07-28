using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GoogleContact = Google.Contacts.Contact;
using GoogleGroup = Google.Contacts.Group;

namespace ContactPusher.Core
{
	public interface IGoogleServices
	{
		string CreateGroup(Authorization authorization);
		string GetFullName(Contact contact);
		string CreateOAuth2AuthorizationUrl(string state);
		Authorization GetAuthorization(string code);
		GoogleUserInfo GetUserInfo(Authorization authorization);
		IEnumerable<GoogleUserInfo> GetAllContacts(Authorization authorization);
		void AddOrUpdateContact(Contact sourceContact, User destinationUser);
		IEnumerable<GoogleUserInfo> SearchContacts(Authorization authorization, string term);
		GoogleContact GetContact(Authorization authorization, string id);
		byte[] GetPhoto(Authorization authorization, string id);
	}
}