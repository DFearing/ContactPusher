using ContactPusher.Core;
using FluentNHibernate.Mapping;

namespace ContactPusher.Data.NHibernate.Mappings
{
	public class ContactMap : ClassMap<Contact>
	{
		public ContactMap()
		{
			Id(x => x.Id)
				.GeneratedBy.Native();
			Map(x => x.LastUpdated)
				.Not.Nullable();
			Map(x => x.GoogleId)
				.Not.Nullable();
			Map(x => x.ExternalId)
				.Not.Nullable();
			Map(x => x.Deleted)
				.Not.Nullable();
			References(x => x.User)
				.Not.Nullable();
		}
	}
}