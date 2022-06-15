// Decompiled

using System;
using System.Text.RegularExpressions;
using PX.Objects.Localizations.CA.Messages;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.Localizations.CA
{
	public class T5018VendorMaintExt : PXGraphExtension<VendorMaint>
	{
		#region IsActive

		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.canadianLocalization>();
		}

		#endregion
		protected virtual void Vendor_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
		{
			Vendor vendor = (Vendor) e.Row;
			T5018BAccountExt extension = PXCache<BAccount>.GetExtension<T5018BAccountExt>(vendor);
			if (vendor.Vendor1099 == true)
			{
				PXUIFieldAttribute.SetEnabled<T5018BAccountExt.vendorT5018>(cache, null, isEnabled: false);
				PXUIFieldAttribute.SetEnabled<Vendor.box1099>(cache, null, isEnabled: true);
			}
			else
			{
				PXUIFieldAttribute.SetEnabled<T5018BAccountExt.vendorT5018>(cache, null, isEnabled: true);
			}

			if (extension.VendorT5018 == true)
			{
				PXUIFieldAttribute.SetEnabled<Vendor.vendor1099>(cache, null, isEnabled: false);
				PXUIFieldAttribute.SetEnabled<T5018BAccountExt.boxT5018>(cache, null, true);
				if (extension.BoxT5018 == 3)
				{
					extension.BusinessNum = null;
					PXUIFieldAttribute.SetEnabled<T5018BAccountExt.socialInsNum>(cache, null, isEnabled: true);
					PXDefaultAttribute.SetPersistingCheck<T5018BAccountExt.socialInsNum>(cache, vendor, PXPersistingCheck.NullOrBlank);
					PXUIFieldAttribute.SetEnabled<T5018BAccountExt.businessNum>(cache, null, isEnabled: false);
				}

				if (extension.BoxT5018 == 1 || extension.BoxT5018 == 2)
				{
					extension.SocialInsNum = null;
					PXUIFieldAttribute.SetEnabled<T5018BAccountExt.businessNum>(cache, null, isEnabled: true);
					PXDefaultAttribute.SetPersistingCheck<T5018BAccountExt.businessNum>(cache, vendor, PXPersistingCheck.NullOrBlank);
					PXUIFieldAttribute.SetEnabled<T5018BAccountExt.socialInsNum>(cache, null, isEnabled: false);
				}
			}
			else
			{
				PXUIFieldAttribute.SetEnabled<Vendor.vendor1099>(cache, null, isEnabled: true);
				PXUIFieldAttribute.SetEnabled<T5018BAccountExt.boxT5018>(cache, null, false);
				PXUIFieldAttribute.SetEnabled<T5018BAccountExt.businessNum>(cache, null, isEnabled: false);
				PXUIFieldAttribute.SetEnabled<T5018BAccountExt.socialInsNum>(cache, null, isEnabled: false);
			}
		}

		protected void VendorR_RowPersisting(PXCache cache, PXRowPersistingEventArgs e)
		{
			VendorR item = (VendorR) e.Row;
			T5018BAccountExt extension = PXCache<BAccount>.GetExtension<T5018BAccountExt>(item);

			PXCache PrimaryContactCache = this.Base.GetExtension<VendorMaint.PrimaryContactGraphExt>().PrimaryContactCurrent.Cache;
			bool ContactCacheIsEmpty = this.Base.GetExtension<VendorMaint.ContactDetailsExt>().Contacts.SelectSingle() == null;
			Contact PrimaryContact = (Contact)PrimaryContactCache.Current;
			if ((extension.VendorT5018 ?? false) && (extension.BoxT5018 ?? 0) == 3)
			{
				if (PrimaryContact == null)
				{
					if (ContactCacheIsEmpty)
						throw new PXRowPersistingException(nameof(PrimaryContact.LastName),
							null,
							T5018Messages.T5018IndividualEmptyPrimary);
					else
						throw new PXRowPersistingException(nameof(item.PrimaryContactID),
							item.PrimaryContactID,
							T5018Messages.T5018IndividualEmptyPrimary);

				}
			}

			if (!String.IsNullOrEmpty(extension.SocialInsNum) && !IsValidSIN(extension.SocialInsNum))
			{
				throw new PXRowPersistingException(nameof(extension.SocialInsNum),
					extension.SocialInsNum,
					T5018Messages.SNFormat);
			}

			if (!String.IsNullOrEmpty(extension.BusinessNum) && !IsValidAccountNumber(extension.BusinessNum))
			{
				throw new PXRowPersistingException(nameof(extension.BusinessNum),
					extension.BusinessNum,
					T5018Messages.BNFormat);
			}
		}

		protected void VendorR_SocialInsNum_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
		{
			Vendor item = (Vendor) e.Row;
			T5018BAccountExt extension = PXCache<BAccount>.GetExtension<T5018BAccountExt>(item);
			if (!String.IsNullOrEmpty(extension.SocialInsNum) && !IsValidSIN(extension.SocialInsNum))
			{
				throw new PXSetPropertyException(T5018Messages.SNFormat);
			}
		}

		private bool IsValidSIN(string sin)
		{
			Regex regex = new Regex("^[0-9]{9}$");
			return (regex.IsMatch(sin));
		}

		protected void VendorR_BusinessNum_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
		{
			Vendor item = (Vendor) e.Row;
			T5018BAccountExt extension = PXCache<BAccount>.GetExtension<T5018BAccountExt>(item);
			if (!IsValidAccountNumber(extension.BusinessNum))
			{
				throw new PXSetPropertyException(T5018Messages.BNFormat);
			}
		}

		private bool IsValidAccountNumber(string accountNumber)
		{
			Regex regex = new Regex("^[0-9]{9}[A-Z]{2}[0-9]{4}$");
			return (accountNumber != null && regex.IsMatch(accountNumber));
		}

		protected void VendorR_BoxT5018_FieldUpdating(PXCache cache, PXFieldUpdatingEventArgs e)
		{
			VendorR item = (VendorR)e.Row;
			Contact primary = Base.GetExtension<VendorMaint.PrimaryContactGraphExt>().PrimaryContactCurrent.Current;
			if (((item.PrimaryContactID == null ||
				item.PrimaryContactID < 0) &&
				String.IsNullOrEmpty(primary?.LastName)) &&
				(int?)e.NewValue == 3)
			{
				throw new PXSetPropertyException<T5018BAccountExt.boxT5018>(
					  T5018Messages.T5018IndividualEmptyPrimary,
					  PXErrorLevel.Error);
			}
		}
	}
}

