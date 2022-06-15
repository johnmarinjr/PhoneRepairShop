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
	public class ARDocumentEnqMultipleBaseCurrencies : PXGraphExtension<ARDocumentEnq>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		public delegate IEnumerable documentsDelegate();
		[PXOverride]
		public IEnumerable documents(documentsDelegate baseMethod)
		{
			if (Base.Filter.Current.OrgBAccountID == null && !PXSiteMap.IsPortal)
			{
				return new PXDelegateResult();
			}
			else
			{
				return baseMethod();
			}
		}
	}
}
