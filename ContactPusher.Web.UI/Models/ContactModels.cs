using ContactPusher.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ContactPusher.Web.UI.Models
{
	public class ContactPickModel
	{
		public string Filter { get; set; }
	}

	public class ContactRetrieveModel : ContactBaseModel
	{

	}

	public class ContactDownloadModel : ContactBaseModel
	{

	}

	public class ContactShareSuccessModel : ContactBaseModel
	{
		public string ShortUrl { get; set; }
	}

	public class ContactImportSuccessModel : ContactBaseModel
	{

	}

	public class ContactBaseModel
	{
		public GoogleUserInfo Contact { get; set; }
		public Guid Id { get; set; }
	}
}