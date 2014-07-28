using ContactPusher.Core;
using NHibernate;
using NHibernate.Linq;
using System.Linq;

namespace ContactPusher.Data.NHibernate
{
	public class Repository : IRepository
	{
		protected readonly ISession _session;

		public Repository(ISession session)
		{
			_session = session;
		}

		public void Add(object entity)
		{
			_session.Save(entity);
		}

		public void Update(object entity)
		{
			_session.Update(entity);
		}

		public IQueryable<T> Query<T>()
		{
			return _session.Query<T>();
		}

		public void Flush()
		{
			_session.Flush();
		}
	}
}