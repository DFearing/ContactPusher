using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContactPusher.Core
{
	public class User
	{
		public virtual int Id { get; set; }
		public virtual string GoogleId { get; set; }
		public virtual string GroupGoogleId { get; set; }
		public virtual string Email { get; set; }
		public virtual Authorization Authorization { get; set; }
		public virtual IDictionary<Contact, string> Contacts { get; set; }

		public User()
		{
			Contacts = new Dictionary<Contact, string>();
		}
	}
}