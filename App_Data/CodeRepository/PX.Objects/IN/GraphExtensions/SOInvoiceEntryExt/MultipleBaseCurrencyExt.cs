using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.Common.Extensions;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN.Attributes;
using PX.Objects.IN.Matrix.DAC.Unbound;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.GraphExtensions.SOInvoiceEntryExt
{
	public class MultipleBaseCurrencyExt :
		MultipleBaseCurrencyExtBase<SOInvoiceEntry, ARInvoice, ARTran,
			ARInvoice.branchID, ARTran.branchID, ARTran.siteID>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictSiteByBranch(typeof(ARInvoice.branchID))]
		protected virtual void _(Events.CacheAttached<ARTran.siteID> e)
		{
		}

		protected override PXSelectBase<ARTran> GetTransactionView()
			=> Base.Transactions;
	}
}
