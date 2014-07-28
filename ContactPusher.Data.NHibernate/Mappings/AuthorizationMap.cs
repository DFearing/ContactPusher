using ContactPusher.Core;
using FluentNHibernate.Mapping;

namespace ContactPusher.Data.NHibernate.Mappings
{
	public class AuthorizationMap : ClassMap<Authorization>
	{
		public AuthorizationMap()
		{
			Id(x => x.Id)
				.GeneratedBy.Native();
			Map(x => x.AccessToken)
				.Not.Nullable();
			Map(x => x.RefreshToken)
				.Not.Nullable();
			Map(x => x.TokenType)
				.Not.Nullable();
			Map(x => x.ExpirationDate)
				.Not.Nullable();
			Map(x => x.Revoked)
				.Not.Nullable();
		}
	}
}