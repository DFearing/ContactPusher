using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ContactPusher.Web.UI.App_Code
{
	public class SessionData
	{
		public int UserId { get; set; }
		public virtual string AccessToken { get; set; }
		public virtual string RefreshToken { get; set; }
	}
}