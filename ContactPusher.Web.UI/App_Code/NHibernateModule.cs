using NHibernate;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ContactPusher.Web.UI.App_Code
{
	public class NHibernateModule : IHttpModule
	{
		private ISession _session;

		public void Init(HttpApplication context)
		{
			context.BeginRequest += ContextBeginRequest;
			context.EndRequest += ContextEndRequest;
		}

		private void ContextBeginRequest(object sender, EventArgs e)
		{
			_session = ObjectFactory.GetInstance<ISession>();

			if (!_session.IsOpen)
			{

			}
		}

		private void ContextEndRequest(object sender, EventArgs e)
		{
			Dispose();
		}

		public void Dispose()
		{
			if (_session != null)
			{
				_session.Flush();
				_session.Close();
				_session = null;
			}
		}
	}
}