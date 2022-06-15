using PX.Common;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.Common.Extensions;
using PX.Objects.CS;
using PX.Objects.CM;
using PX.Objects.IN.Attributes;
using PX.Objects.SO;
using PX.Objects.GL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.GraphExtensions.SOShipmentEntryExt
{
	public class MultipleBaseCurrencyExt : PXGraphExtension<SOShipmentEntry>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictorWithParameters(
			typeof(Where<Current2<SOShipment.shipmentType>, Equal<SOShipmentType.transfer>,
				Or<INSite.baseCuryID, Equal<Current2<Customer.baseCuryID>>,
				Or<Current2<Customer.baseCuryID>, IsNull>>>),
			Messages.CarrierSiteBaseCurrencyDiffers, typeof(INSite.branchID), typeof(INSite.siteCD), typeof(Current<Customer.acctCD>))]
		protected virtual void _(Events.CacheAttached<SOShipment.siteID> e)
		{
		}

		protected virtual void _(Events.FieldDefaulting<CurrencyInfo, CurrencyInfo.baseCuryID> e)
		{
			e.Cancel = true;
			e.NewValue = GetDefaultCuryID(Base.Document.Current);
		}

		protected virtual void _(Events.FieldDefaulting<CurrencyInfo, CurrencyInfo.curyID> e)
		{
			e.Cancel = true;
			e.NewValue = GetDefaultCuryID(Base.Document.Current);
		}

		protected virtual string GetDefaultCuryID(SOShipment shipment)
		{
			var insite = INSite.PK.Find(Base, shipment?.SiteID);
			return insite?.BaseCuryID ?? Base.Accessinfo.BaseCuryID;
		}

		protected virtual void _(Events.FieldUpdated<SOShipment, SOShipment.siteID> e)
			=> Base.RedefaultCurrencyInfo(e.Cache, e.Args);

		protected virtual void _(Events.RowPersisting<SOShipment> e)
		{
			if (e.Operation.Command().IsNotIn(PXDBOperation.Insert, PXDBOperation.Update))
				return;

			e.Cache.VerifyFieldAndRaiseException<SOShipment.siteID>(e.Row);
		}
	}
}
