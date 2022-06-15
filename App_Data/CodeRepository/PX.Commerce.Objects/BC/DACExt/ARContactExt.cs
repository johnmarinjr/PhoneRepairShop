using PX.Commerce.Core;
using PX.Data;
using PX.Objects.AR;
using System;

namespace PX.Commerce.Objects
{
	[Serializable]
	[PXPersonalDataTable(typeof(
		Select<
			ARContact,
		Where<
			ARContact.contactID, Equal<Current<ARInvoice.billContactID>>, Or<ARContact.contactID, Equal<Current<ARInvoice.shipContactID>>>>>))]
	[PXNonInstantiatedExtension]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public sealed class ARContactExt : PXCacheExtension<ARContact>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }

		public abstract class attention : PX.Data.BQL.BqlString.Field<attention> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(ARContact.isEncrypted), typeof(PX.Objects.GDPR.ARContactExt.pseudonymizationStatus))]
		public string Attention { get; set; }

		public abstract class salutation : PX.Data.BQL.BqlString.Field<salutation> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(ARContact.isEncrypted), typeof(PX.Objects.GDPR.ARContactExt.pseudonymizationStatus))]
		public string Salutation { get; set; }

		public abstract class email : PX.Data.BQL.BqlString.Field<email> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(ARContact.isEncrypted), typeof(PX.Objects.GDPR.ARContactExt.pseudonymizationStatus))]
		public string Email { get; set; }
		public abstract class phone1 : PX.Data.BQL.BqlString.Field<phone1> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(ARContact.isEncrypted), typeof(PX.Objects.GDPR.ARContactExt.pseudonymizationStatus))]
		public string Phone1 { get; set; }

		public abstract class phone2 : PX.Data.BQL.BqlString.Field<phone2> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(ARContact.isEncrypted), typeof(PX.Objects.GDPR.ARContactExt.pseudonymizationStatus))]
		public string Phone2 { get; set; }

		public abstract class phone3 : PX.Data.BQL.BqlString.Field<phone3> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(ARContact.isEncrypted), typeof(PX.Objects.GDPR.ARContactExt.pseudonymizationStatus))]
		public string Phone3 { get; set; }

		public abstract class fullName : PX.Data.BQL.BqlString.Field<fullName> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(ARContact.isEncrypted), typeof(PX.Objects.GDPR.ARContactExt.pseudonymizationStatus))]
		public string FullName { get; set; }

		public abstract class fax : PX.Data.BQL.BqlString.Field<fax> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[BCEncryptPersonalDataAttribute(typeof(ARContact.isEncrypted), typeof(PX.Objects.GDPR.ARContactExt.pseudonymizationStatus))]
		public string Fax { get; set; }
	}
	[PXHidden]
	public class ARContact2 : PX.Objects.AR.ARContact
	{
		public new abstract class contactID : PX.Data.BQL.BqlInt.Field<contactID> { }
		public new abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
	}

}
