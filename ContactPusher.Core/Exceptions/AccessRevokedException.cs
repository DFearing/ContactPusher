using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContactPusher.Core.Exceptions
{
	public class AccessRevokedException : GoogleException
	{
		public Authorization AuthorizationSource { get; set; }

		public AccessRevokedException(Authorization source)
		{
			AuthorizationSource = source;
		}
	}
}