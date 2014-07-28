using ContactPusher.Core;
using FluentNHibernate.Mapping;
using System;

namespace ContactPusher.Data.NHibernate.Mappings
{
	public class UserMap : ClassMap<User>
	{
		public UserMap()
		{
			Id(x => x.Id)
				.GeneratedBy.Native();
			Map(x => x.Email)
				.Not.Nullable();
			Map(x => x.GoogleId)
				.Not.Nullable();
			Map(x => x.GroupGoogleId)
				.Nullable();
			References(x => x.Authorization)
				.Not.Nullable();
			HasManyToMany(x => x.Contacts)
				.Table("UserContact")
				.AsEntityMap("ContactId", null)
				.Element("GoogleId", x => x.Type<String>());
		}
	}
}