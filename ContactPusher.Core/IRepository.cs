using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContactPusher.Core
{
	public interface IRepository
	{
		void Add(object entity);
		IQueryable<T> Query<T>();
		void Update(Object entity);
		void Flush();
	}
}