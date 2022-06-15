using PX.Common;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.Common.Bql;
using PX.Objects.Common.Extensions;
using PX.Objects.CS;
using PX.Objects.IN.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.GraphExtensions.CarrierPluginMaintExt
{
	public class MultipleBaseCurrencyExt : PXGraphExtension<CarrierPluginMaint>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictorWithParameters(typeof(
			Where<Current2<CarrierPlugin.siteID>, IsNull,
				Or<Customer.baseCuryID, EqualSiteBaseCuryID<Current2<CarrierPlugin.siteID>>,
				Or<Customer.baseCuryID, IsNull>>>),
			Messages.CarrierSiteBaseCurrencyDiffers,
				typeof(Selector<CarrierPlugin.siteID, INSite.branchID>),
				typeof(Current<CarrierPlugin.siteID>), typeof(Customer.acctCD))]
		protected virtual void _(Events.CacheAttached<CarrierPluginCustomer.customerID> e)
		{
		}

		protected virtual void _(Events.RowUpdated<CarrierPlugin> e)
		{
			if (!e.Cache.ObjectsEqual<CarrierPlugin.siteID>(e.OldRow, e.Row))
			{
				var newBranch = (INSite)PXSelectorAttribute.Select<CarrierPlugin.siteID>(e.Cache, e.Row);
				var oldBranch = (INSite)PXSelectorAttribute.Select<CarrierPlugin.siteID>(e.Cache, e.OldRow);
				if (!string.Equals(newBranch?.BaseCuryID, oldBranch?.BaseCuryID, StringComparison.OrdinalIgnoreCase))
				{
					foreach (CarrierPluginCustomer customerAccount in Base.CustomerAccounts.Select())
					{
						Base.CustomerAccounts.Cache.MarkUpdated(customerAccount);
						Base.CustomerAccounts.Cache.VerifyFieldAndRaiseException<CarrierPluginCustomer.customerID>(customerAccount);
					}
				}
			}
		}

		protected virtual void _(Events.RowPersisting<CarrierPluginCustomer> e)
		{
			if (e.Operation.Command().IsNotIn(PXDBOperation.Insert, PXDBOperation.Update))
				return;

			e.Cache.VerifyFieldAndRaiseException<CarrierPluginCustomer.customerID>(e.Row);
		}
	}
}
