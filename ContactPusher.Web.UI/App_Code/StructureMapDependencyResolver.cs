using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ContactPusher.Web.UI.App_Code
{
	public class StructureMapDependencyResolver : IDependencyResolver
	{
		private readonly IContainer container;

		public StructureMapDependencyResolver(IContainer container)
		{
			this.container = container;
		}

		public object GetService(Type serviceType)
		{
			if (serviceType.IsClass)
			{
				return GetConcreteService(serviceType);
			}
			else
			{
				return GetInterfaceService(serviceType);
			}
		}

		private object GetConcreteService(Type serviceType)
		{
			try
			{
				// Can't use TryGetInstance here because it won’t create concrete types
				return container.GetInstance(serviceType);
			}
			catch (StructureMapException)
			{
				return null;
			}
		}

		private object GetInterfaceService(Type serviceType)
		{
			return container.TryGetInstance(serviceType);
		}

		public IEnumerable<object> GetServices(Type serviceType)
		{
			return container.GetAllInstances(serviceType).Cast<object>();
		}
	}
}