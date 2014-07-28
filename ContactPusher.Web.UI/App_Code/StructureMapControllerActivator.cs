using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace ContactPusher.Web.UI.App_Code
{
	public class StructureMapControllerActivator : IControllerActivator
	{
		private readonly IContainer container;

		public StructureMapControllerActivator(IContainer container)
		{
			this.container = container;
		}

		public IController Create(RequestContext requestContext, Type controllerType)
		{
			return container.GetInstance(controllerType) as IController;
		}
	}
}