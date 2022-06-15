using PX.Commerce.Core;
using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.GL.Attributes;
using PX.Objects.IN;
using PX.Objects.IN.RelatedItems;
using PX.Objects.SO;
using PX.Objects.TX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static PX.Commerce.Objects.BCBindingExt;

namespace PX.Commerce.Objects
{
	#region BCPaymentMethodsMappingSlot
	public  class BCPaymentMethodsMappingSlot : IPrefetchable<Int32>
	{
		protected List<BCPaymentMethods> _paymentMethods = new List<BCPaymentMethods>();
		public static List<BCPaymentMethods> Get(Int32? bindingID)
		{
			if (bindingID == null) return new List<BCPaymentMethods>();

			BCPaymentMethodsMappingSlot slot = PXDatabase.GetSlot<BCPaymentMethodsMappingSlot, Int32>(
				nameof(BCPaymentMethodsMappingSlot) + bindingID.ToString(), bindingID ?? 0, typeof(BCPaymentMethods));
			return slot._paymentMethods;
		}

		public void Prefetch(int binding)
		{
			_paymentMethods.Clear();
			foreach (PXDataRecord row in PXDatabase.SelectMulti<BCPaymentMethods>(
					new PXAliasedDataField<BCPaymentMethods.active>(),
					new PXAliasedDataField<BCPaymentMethods.bindingID>(),
					new PXAliasedDataField<BCPaymentMethods.cashAccountID>(),
					new PXAliasedDataField<BCPaymentMethods.createPaymentFromOrder>(),
					new PXAliasedDataField<BCPaymentMethods.paymentMappingID>(),
					new PXAliasedDataField<BCPaymentMethods.paymentMethodID>(),
					new PXAliasedDataField<BCPaymentMethods.processingCenterID>(),
					new PXAliasedDataField<BCPaymentMethods.processRefunds>(),
					new PXAliasedDataField<BCPaymentMethods.releasePayments>(),
					new PXAliasedDataField<BCPaymentMethods.storeCurrency>(),
					new PXAliasedDataField<BCPaymentMethods.storeOrderPaymentMethod>(),
					new PXAliasedDataField<BCPaymentMethods.storePaymentMethod>(),
					new PXDataFieldValue<BCPaymentMethods.bindingID>(PXDbType.Int, binding)))
			{
				_paymentMethods.Add(new BCPaymentMethods()
				{
					Active = row.GetBoolean(0),
					BindingID = row.GetInt32(1),
					CashAccountID = row.GetInt32(2),
					CreatePaymentFromOrder = row.GetBoolean(3),
					PaymentMappingID = row.GetInt32(4),
					PaymentMethodID = row.GetString(5),
					ProcessingCenterID = row.GetString(6),
					ProcessRefunds = row.GetBoolean(7),
					ReleasePayments = row.GetBoolean(8),
					StoreCurrency = row.GetString(9),
					StoreOrderPaymentMethod = row.GetString(10),
					StorePaymentMethod = row.GetString(11)
				});
			}
		}
	}
	#endregion
	#region BCShippingMethodsMappingSlot
	public class BCShippingMethodsMappingSlot : IPrefetchable<Int32>
	{
		protected List<BCShippingMappings> _paymentMethods = new List<BCShippingMappings>();
		public static List<BCShippingMappings> Get(Int32? bindingID)
		{
			if (bindingID == null) return new List<BCShippingMappings>();

			BCShippingMethodsMappingSlot slot = PXDatabase.GetSlot<BCShippingMethodsMappingSlot, Int32>(
				nameof(BCShippingMethodsMappingSlot) + bindingID.ToString(), bindingID ?? 0, typeof(BCShippingMappings));
			return slot._paymentMethods;
		}

		public void Prefetch(int binding)
		{
			_paymentMethods.Clear();
			foreach (PXDataRecord row in PXDatabase.SelectMulti<BCShippingMappings>(
					new PXAliasedDataField<BCShippingMappings.active>(),
					new PXAliasedDataField<BCShippingMappings.bindingID>(),
					new PXAliasedDataField<BCShippingMappings.carrierID>(),
					new PXAliasedDataField<BCShippingMappings.shippingMappingID>(),
					new PXAliasedDataField<BCShippingMappings.shippingMethod>(),
					new PXAliasedDataField<BCShippingMappings.shippingZone>(),
					new PXAliasedDataField<BCShippingMappings.shipTermsID>(),
					new PXAliasedDataField<BCShippingMappings.zoneID>(),
					new PXDataFieldValue<BCShippingMappings.bindingID>(PXDbType.Int, binding)))
			{
				_paymentMethods.Add(new BCShippingMappings()
				{
					Active = row.GetBoolean(0),
					BindingID = row.GetInt32(1),
					CarrierID = row.GetString(2),
					ShippingMappingID = row.GetInt32(3),
					ShippingMethod = row.GetString(4),
					ShippingZone = row.GetString(5),
					ShipTermsID = row.GetString(6),
					ZoneID = row.GetString(7)
				});
			}
		}
	}
	#endregion

	#region BCLocationSlot
	public class BCLocationSlot : IPrefetchable<Int32>
	{
		protected List<BCLocations> _bcLocations = new List<BCLocations>();
		protected Dictionary<int, PX.Objects.IN.INSite> _inSites = new Dictionary<int, INSite>();
		protected Dictionary<int, PX.Objects.IN.INLocation> _inLocations = new Dictionary<int, INLocation>();
		protected Dictionary<int, Dictionary<int, PX.Objects.IN.INLocation>> _siteLocations = new Dictionary<int, Dictionary<int, PX.Objects.IN.INLocation>>();

		public static BCLocationSlot GetBCLocationSlot(Int32? bindingID)
		{
			return PXDatabase.GetSlot<BCLocationSlot, Int32>(
				nameof(BCLocationSlot) + bindingID.ToString(), bindingID ?? 0, typeof(BCLocations), typeof(INSite), typeof(INLocation), typeof(INSetup));
		}
		public static List<BCLocations> GetBCLocations(Int32? bindingID)
		{
			if (bindingID == null) return new List<BCLocations>();

			return GetBCLocationSlot(bindingID)._bcLocations;
		}

		public static Dictionary<int, PX.Objects.IN.INSite> GetWarehouses(Int32? bindingID)
		{
			if (bindingID == null) return new Dictionary<int, INSite>();

			return GetBCLocationSlot(bindingID)._inSites;
		}

		public static Dictionary<int, PX.Objects.IN.INLocation> GetLocations(Int32? bindingID)
		{
			if (bindingID == null) return new Dictionary<int, INLocation>();

			return GetBCLocationSlot(bindingID)._inLocations;
		}

		public static Dictionary<int, Dictionary<int, PX.Objects.IN.INLocation>> GetWarehouseLocations(Int32? bindingID)
		{
			if (bindingID == null) return new Dictionary<int, Dictionary<int, PX.Objects.IN.INLocation>>();

			return GetBCLocationSlot(bindingID)._siteLocations;
		}

		public void Prefetch(int binding)
		{
			_bcLocations.Clear();
			_inSites.Clear();
			_inLocations.Clear();
			_siteLocations.Clear();
			foreach (PXDataRecord row in PXDatabase.SelectMulti<BCLocations>(
					new PXAliasedDataField<BCLocations.bCLocationsID>(),
					new PXAliasedDataField<BCLocations.bindingID>(),
					new PXAliasedDataField<BCLocations.siteID>(),
					new PXAliasedDataField<BCLocations.locationID>(),
					new PXAliasedDataField<BCLocations.externalLocationID>(),
					new PXAliasedDataField<BCLocations.mappingDirection>(),
					new PXDataFieldValue<BCLocations.bindingID>(PXDbType.Int, binding)))
			{
				_bcLocations.Add(new BCLocations()
				{
					BCLocationsID = row.GetInt32(0),
					BindingID = row.GetInt32(1),
					SiteID = row.GetInt32(2),
					LocationID = row.GetInt32(3),
					ExternalLocationID = row.GetString(4),
					MappingDirection = row.GetString(5)
				});
			}

			int? transitSiteID = null;
			using(PXDataRecord row = PXDatabase.SelectSingle<INSetup>(new PXAliasedDataField<INSetup.transitSiteID>()))
			{
				transitSiteID = row != null ?row.GetInt32(0): null;
			}

			foreach(PXDataRecord row in PXDatabase.SelectMulti<INSite>(
				new PXAliasedDataField<INSite.active>(),
				new PXAliasedDataField<INSite.siteID>(),
				new PXAliasedDataField<INSite.siteCD>(),
				new PXAliasedDataField<INSite.descr>(),
				new PXDataFieldValue<INSite.active>(PXDbType.Bit, 1)))
			{
				int siteId = row.GetInt32(1).Value;
				bool needToSlot = _bcLocations?.Count == 0 || _bcLocations.Any(x => x.SiteID == siteId);

				if (needToSlot && (transitSiteID == null || transitSiteID != siteId))
				{
					_inSites[siteId] = new INSite() {
						Active = row.GetBoolean(0),
						SiteID = siteId,
						SiteCD = row.GetString(2)?.Trim(),
						Descr = row.GetString(3)?.Trim()
					};
					_siteLocations[siteId] = new Dictionary<int, PX.Objects.IN.INLocation>();
				}
			}

			foreach (PXDataRecord row in PXDatabase.SelectMulti<INLocation>(
				new PXAliasedDataField<INLocation.active>(),
				new PXAliasedDataField<INLocation.siteID>(),
				new PXAliasedDataField<INLocation.locationID>(),
				new PXAliasedDataField<INLocation.locationCD>(),
				new PXDataFieldValue<INLocation.active>(PXDbType.Bit, 1)))
			{
				int siteId = row.GetInt32(1).Value;
				int locationId = row.GetInt32(2).Value;
				bool needToSlot = _inSites.ContainsKey(siteId) && (_bcLocations?.Count == 0 || _bcLocations.Any(x => x.SiteID == siteId && (x.LocationID == null || x.LocationID == locationId)));

				if (needToSlot)
				{
					_inLocations[locationId] = new INLocation()
					{
						Active = row.GetBoolean(0),
						SiteID = row.GetInt32(1),
						LocationID = row.GetInt32(2),
						LocationCD = row.GetString(3)?.Trim()
					};
					var locationOfSiteList = _siteLocations[siteId];
					if(locationOfSiteList.ContainsKey(locationId) == false)
					{
						locationOfSiteList[locationId] = _inLocations[locationId];
						_siteLocations[siteId] = locationOfSiteList;
					}
				}
			}

		}
	}
	#endregion BCLocationSlot
}
