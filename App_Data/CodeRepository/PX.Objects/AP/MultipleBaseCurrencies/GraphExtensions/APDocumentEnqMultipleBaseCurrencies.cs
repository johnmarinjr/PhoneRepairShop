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

namespace PX.Objects.AP
{
	public class APDocumentEnqMultipleBaseCurrencies : PXGraphExtension<APDocumentEnq>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		public delegate IEnumerable documentsDelegate();
		[PXOverride]
		public IEnumerable documents(documentsDelegate baseMethod)
		{
			if (Base.Filter.Current.OrgBAccountID == null)
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
