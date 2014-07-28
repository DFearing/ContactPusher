using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContactPusher.Core
{
	public class Contact
	{
		public virtual int Id { get; set; }
		public virtual User User { get; set; }
		public virtual string GoogleId { get; set; }
		public virtual DateTime LastUpdated { get; set; }
		public virtual Guid ExternalId { get; set; }
		public virtual bool Deleted { get; set; }
	}
}