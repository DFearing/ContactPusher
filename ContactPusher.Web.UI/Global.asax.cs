using ContactPusher.Data.NHibernate.Mappings;
using ContactPusher.Web.UI.App_Code;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Conventions.Helpers;
using NHibernate;
using NLog;
using StructureMap;
using StructureMap.Pipeline;
using System;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace ContactPusher.Web.UI
{
	public class MvcApplication : System.Web.HttpApplication
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();

			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);

			InitializeIocContainer();

			DependencyResolver.SetResolver(new StructureMapDependencyResolver(ObjectFactory.Container));
			GlobalConfiguration.Configuration.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;	
		}

		private void InitializeIocContainer()
		{
			ObjectFactory.Initialize(x =>
			{
				// NHibernate ISessionFactory
				x.For<ISessionFactory>()
					.LifecycleIs(new SingletonLifecycle())
					.Use(Fluently.Configure()
						//.ExposeConfiguration(cfg => cfg.Properties.Add("use_proxy_validator", "false"))
						.Database(MsSqlConfiguration.MsSql2008
							.ConnectionString(c => c.FromConnectionStringWithKey("Primary")))
						.Mappings(m => m.FluentMappings.AddFromAssemblyOf<UserMap>()
						.Conventions.Add(ForeignKey.Format((p, t) => p == null ? t.Name + "Id" : p.Name + "Id")))
						.BuildSessionFactory);

				// NHibernate ISession
				x.For<ISession>()
					.LifecycleIs(new HybridLifecycle())
					.Use(context => context.GetInstance<ISessionFactory>().OpenSession());

				x.For<IControllerActivator>()
					.Use<StructureMapControllerActivator>();
				x.For<HttpSessionStateBase>()
					.Use(() => new HttpSessionStateWrapper(HttpContext.Current.Session));
				x.For<HttpRequestBase>()
					.Use(() => new HttpRequestWrapper(HttpContext.Current.Request));
				x.For<HttpServerUtilityBase>()
					.Singleton()
					.Use<HttpServerUtilityWrapper>()
					.Ctor<HttpServerUtility>().Is(HttpContext.Current.Server);
				x.Scan(y =>
				{
					y.Assembly("ContactPusher.Core");
					y.Assembly("ContactPusher.Data.NHibernate");
					y.Assembly("ContactPusher.Services");
					y.TheCallingAssembly();
					y.WithDefaultConventions();
				});
			});
		}

		protected void Application_Error(object sender, EventArgs e)
		{
			var ex = Server.GetLastError().GetBaseException();

			if (ex is HttpRequestValidationException)
			{
				return;
			}

			if (ex is HttpException)
			{
				var httpException = ex as HttpException;
				var code = httpException.GetHttpCode();

				if (code == 404 || code == 400 || ex.Message == "Path 'OPTIONS' is forbidden." || ex.Message.StartsWith("No Controller found at ") || ex.Message.StartsWith("A public action method"))
				{
					return;
				}
			}

			logger.ErrorException("Application_Error", ex);
		}
	}
}