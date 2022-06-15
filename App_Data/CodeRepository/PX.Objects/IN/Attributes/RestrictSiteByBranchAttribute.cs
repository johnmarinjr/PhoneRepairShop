using PX.Data;
using PX.Data.BQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.Attributes
{
	public class RestrictSiteByBranchAttribute : RestrictorWithParametersAttribute
	{
		public RestrictSiteByBranchAttribute(Type branchField = null, Type where = null)
			: base(GetWhere(branchField, where),
				  Messages.SiteBaseCurrencyDiffers,
				  typeof(INSite.branchID), typeof(INSite.siteCD), typeof(Current2<>).MakeGenericType(branchField))
		{
		}

		private static Type GetWhere(Type branchField, Type where)
		{
			if (where != null)
				return where;

			return BqlTemplate.OfCondition<Where<Current2<BqlPlaceholder.A>, IsNull,
				Or<INSite.baseCuryID, EqualBaseCuryID<Current2<BqlPlaceholder.A>>>>>
					.Replace<BqlPlaceholder.A>(branchField).ToType();
		}
	}
}
