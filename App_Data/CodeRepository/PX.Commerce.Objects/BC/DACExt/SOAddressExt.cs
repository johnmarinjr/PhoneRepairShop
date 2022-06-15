using PX.Commerce.Core;
using PX.Data;
using PX.Objects.SO;
using System;

namespace PX.Commerce.Objects
{
	[Serializable]
	[PXPersonalDataTable(typeof(
		Select<
			SOAddress,
		Where<
			SOAddress.addressID, Equal<Current<SOOrder.billAddressID>>,Or<SOAddress.addressID,Equal<Current<SOOrder.shipAddressID>>>>>))]
	[PXNonInstantiatedExtension]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public sealed class SOAddressExt : PXCacheExtension<SOAddress>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }

		public abstract class addressLine1 : PX.Data.BQL.BqlString.Field<addressLine1> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(SOAddress.isEncrypted), typeof(PX.Objects.GDPR.SOAddressExt.pseudonymizationStatus))]
		public string AddressLine1 { get; set; }

		public abstract class addressLine2 : PX.Data.BQL.BqlString.Field<addressLine2> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(SOAddress.isEncrypted), typeof(PX.Objects.GDPR.SOAddressExt.pseudonymizationStatus))]
		public string AddressLine2 { get; set; }

		public abstract class addressLine3 : PX.Data.BQL.BqlString.Field<addressLine3> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(SOAddress.isEncrypted), typeof(PX.Objects.GDPR.SOAddressExt.pseudonymizationStatus))]
		public string AddressLine3 { get; set; }

		public abstract class city : PX.Data.BQL.BqlString.Field<city> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(SOAddress.isEncrypted), typeof(PX.Objects.GDPR.SOAddressExt.pseudonymizationStatus))]
		public string City { get; set; }

		public abstract class state : PX.Data.BQL.BqlString.Field<state> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXPersonalDataField]
		[BCEncryptPersonalDataAttribute(typeof(SOAddress.isEncrypted), typeof(PX.Objects.GDPR.SOAddressExt.pseudonymizationStatus))]
		public string State { get; set; }

		public abstract class countryID : PX.Data.BQL.BqlString.Field<countryID> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXPersonalDataField]
		[BCEncryptPersonalDataAttribute(typeof(SOAddress.isEncrypted), typeof(PX.Objects.GDPR.SOAddressExt.pseudonymizationStatus))]
		public string CountryID { get; set; }

		public abstract class postalCode : PX.Data.BQL.BqlString.Field<postalCode> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(SOAddress.isEncrypted), typeof(PX.Objects.GDPR.SOAddressExt.pseudonymizationStatus))]
		public  string PostalCode { get; set; }
	}

	[PXHidden]
	public class SOAddress2 : PX.Objects.SO.SOAddress
	{
		public new abstract class addressID : PX.Data.BQL.BqlInt.Field<addressID> { }
		public new abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
	}
}
