using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContactPusher.Core
{
	public class Authorization
	{
		public virtual int Id { get; set; }
		public virtual string AccessToken { get; set; }
		public virtual string RefreshToken { get; set; }
		public virtual string TokenType { get; set; }
		public virtual DateTime ExpirationDate { get; set; }
		public virtual bool Revoked { get; set; }
	}
}