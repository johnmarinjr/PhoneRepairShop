using PX.Commerce.Core;
using PX.Data;
using PX.Objects.SO;
using System;

namespace PX.Commerce.Objects
{
	[Serializable]
	[PXPersonalDataTable(typeof(
		   Select<
			   SOContact,
		   Where<
			   SOContact.contactID, Equal<Current<SOOrder.billContactID>>, Or<SOContact.contactID, Equal<Current<SOOrder.shipContactID>>>>>))]
	[PXNonInstantiatedExtension]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public sealed class SOContactExt: PXCacheExtension<SOContact>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }

		public abstract class attention : PX.Data.BQL.BqlString.Field<attention> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(SOContact.isEncrypted), typeof(PX.Objects.GDPR.SOContactExt.pseudonymizationStatus))]
		public string Attention { get; set; }

		public abstract class salutation : PX.Data.BQL.BqlString.Field<salutation> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(SOContact.isEncrypted), typeof(PX.Objects.GDPR.SOContactExt.pseudonymizationStatus))]
		public string Salutation { get; set; }

		public abstract class email : PX.Data.BQL.BqlString.Field<email> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(SOContact.isEncrypted), typeof(PX.Objects.GDPR.SOContactExt.pseudonymizationStatus))]
		public string Email { get; set; }
		public abstract class phone1 : PX.Data.BQL.BqlString.Field<phone1> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(SOContact.isEncrypted), typeof(PX.Objects.GDPR.SOContactExt.pseudonymizationStatus))]
		public string Phone1 { get; set; }

		public abstract class phone2 : PX.Data.BQL.BqlString.Field<phone2> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(SOContact.isEncrypted), typeof(PX.Objects.GDPR.SOContactExt.pseudonymizationStatus))]
		public string Phone2 { get; set; }

		public abstract class phone3 : PX.Data.BQL.BqlString.Field<phone3> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(SOContact.isEncrypted), typeof(PX.Objects.GDPR.SOContactExt.pseudonymizationStatus))]
		public string Phone3 { get; set; }

		public abstract class fullName : PX.Data.BQL.BqlString.Field<fullName> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(SOContact.isEncrypted), typeof(PX.Objects.GDPR.SOContactExt.pseudonymizationStatus))]
		public string FullName { get; set; }

		public abstract class fax : PX.Data.BQL.BqlString.Field<fax> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(SOContact.isEncrypted), typeof(PX.Objects.GDPR.SOContactExt.pseudonymizationStatus))]
		public string Fax { get; set; }
	}

	[PXHidden]
	public class SOContact2 : PX.Objects.SO.SOContact
	{
		public new abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
		public new abstract class contactID : PX.Data.BQL.BqlInt.Field<contactID> { }
	}
}
