using System;
using System.Collections;
using System.Linq;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Data.BQL.Fluent;
using PX.Data.BQL;

namespace PX.Objects.AR
{
	public class ARRetainageReleaseMultipleBaseCurrencies : PXGraphExtension<ARRetainageRelease>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		public delegate IEnumerable documentListDelegate();
		[PXOverride]
		public IEnumerable documentList(documentListDelegate baseMethod)
		{
			if (Base.Filter.Current.OrgBAccountID == null)
			{
				return null;
			}
			else
			{
				return baseMethod();
			}
		}
	}
}
