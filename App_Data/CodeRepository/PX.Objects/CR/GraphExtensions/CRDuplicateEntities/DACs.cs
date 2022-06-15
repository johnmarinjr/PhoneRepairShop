using System;
using System.Diagnostics;
using PX.Data;
using PX.SM;

namespace PX.Objects.CR.Extensions.CRDuplicateEntities
{
	#region CRGrams

	/// <exclude/>
	[Serializable]
	[PXHidden]
	[DebuggerDisplay("{GetType().Name,nq} of {EntityID}|{ValidationType}: {FieldName} = {FieldValue} ({Score})")]
	public partial class CRGrams : IBqlTable
	{
		#region GramID
		public abstract class gramID : PX.Data.BQL.BqlInt.Field<gramID> { }

		[PXDBIdentity(IsKey = true)]
		[PXUIField(DisplayName = "Gram ID", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual Int32? GramID { get; set; }
		#endregion

		#region ValidationType
		public abstract class validationType : PX.Data.BQL.BqlString.Field<validationType> { }

		[PXDBString(2)]
		[PXUIField(DisplayName = "Entity Type")]
		[PXDefault]
		[ValidationTypes]
		public virtual String ValidationType { get; set; }
		#endregion

		#region EntityID
		public abstract class entityID : PX.Data.BQL.BqlInt.Field<entityID> { }

		[PXDBInt]
		[PXUIField(DisplayName = "Entity ID")]
		public virtual int? EntityID { get; set; }
		#endregion

		#region FieldName
		public abstract class fieldName : PX.Data.BQL.BqlString.Field<fieldName> { }

		[PXDBString(60)]
		[PXDefault("")]
		[PXUIField(DisplayName = "Field Name", Visibility = PXUIVisibility.Visible)]
		public virtual String FieldName { get; set; }
		#endregion

		#region FieldValue
		public abstract class fieldValue : PX.Data.BQL.BqlString.Field<fieldValue> { }

		[PXDBString(60)]
		[PXDefault("")]
		[PXUIField(DisplayName = "Field Value", Visibility = PXUIVisibility.Visible)]
		public virtual String FieldValue { get; set; }
		#endregion

		#region Score
		public abstract class score : PX.Data.BQL.BqlDecimal.Field<score> { }

		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "1")]
		[PXUIField(DisplayName = "Score")]
		public virtual decimal? Score { get; set; }

		#endregion

		#region CreateOnEntry
		public abstract class createOnEntry : PX.Data.BQL.BqlString.Field<createOnEntry> { }

		[PXString(1)]
		[PXUIField(DisplayName = "Create on Entry")]
		[PXDefault(CreateOnEntryAttribute.Allow, PersistingCheck = PXPersistingCheck.Nothing)]
		[CreateOnEntry]
		public virtual String CreateOnEntry { get; set; }
		#endregion
	}

	#endregion

	#region CRDuplicateGrams

	/// <exclude/>
	[Serializable]
	[PXHidden]
	public partial class CRDuplicateGrams : CRGrams
	{
		public new abstract class gramID : PX.Data.BQL.BqlInt.Field<gramID> { }

		public new abstract class validationType : PX.Data.BQL.BqlString.Field<validationType> { }

		public new abstract class entityID : PX.Data.BQL.BqlInt.Field<entityID> { }

		public new abstract class fieldName : PX.Data.BQL.BqlString.Field<fieldName> { }

		public new abstract class fieldValue : PX.Data.BQL.BqlString.Field<fieldValue> { }

		public new abstract class score : PX.Data.BQL.BqlDecimal.Field<score> { }
	}

	#endregion

	#region DuplicateContact

	/// <exclude/>
	[Serializable]
	[PXHidden]
	public partial class DuplicateContact : Contact
	{
		public new abstract class contactID : PX.Data.BQL.BqlInt.Field<contactID> { }

		#region BAccountID
		public new abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }

		[PXDBInt]
		[PXUIField(DisplayName = "Business Account")]
		[PXSelector(typeof(BAccount.bAccountID), SubstituteKey = typeof(BAccount.acctCD))]
		public override Int32? BAccountID { get; set; }
		#endregion

		#region DisplayName
		public new abstract class displayName : PX.Data.BQL.BqlString.Field<displayName> { }

		[PXUIField(DisplayName = "Display Name", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXDependsOnFields(typeof(Contact.lastName), typeof(Contact.firstName), typeof(Contact.midName), typeof(Contact.title))]
		[PersonDisplayName(typeof(Contact.lastName), typeof(Contact.firstName), typeof(Contact.midName), typeof(Contact.title))]
		[PXDefault]
		[PXUIRequired(typeof(Where<Contact.contactType, Equal<ContactTypesAttribute.lead>, Or<Contact.contactType, Equal<ContactTypesAttribute.person>>>))]
		[PXNavigateSelector(typeof(Search2<Contact.displayName,
			LeftJoin<BAccount, On<BAccount.bAccountID, Equal<Contact.contactID>>>,
			Where2<
				Where<Contact.contactType, Equal<ContactTypesAttribute.lead>,
					Or<Contact.contactType, Equal<ContactTypesAttribute.person>,
					Or<Contact.contactType, Equal<ContactTypesAttribute.employee>>>>,
				And<Where<BAccount.bAccountID, IsNull, Or<Match<BAccount, Current<AccessInfo.userName>>>>>
			>>))]
		[PXPersonalDataField]
		public override String DisplayName { get; set; }
		#endregion

		public new abstract class defAddressID : PX.Data.BQL.BqlInt.Field<defAddressID> { }
		public new abstract class contactPriority : PX.Data.BQL.BqlInt.Field<contactPriority> { }
		public new abstract class duplicateStatus : PX.Data.BQL.BqlString.Field<duplicateStatus> { }
		public new abstract class status : PX.Data.BQL.BqlString.Field<status> { }
		public new abstract class contactType : PX.Data.BQL.BqlString.Field<contactType> { }
		public new abstract class isActive : PX.Data.BQL.BqlBool.Field<isActive> { }
		public new abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
	}

	#endregion

	#region CRDuplicateRecord

	/// <exclude/>
	[PXVirtual]
	[Serializable]
	[PXHidden]
	public partial class CRDuplicateRecord : IBqlTable
	{
		#region ContactID
		public abstract class contactID : PX.Data.BQL.BqlInt.Field<contactID> { }

		[PXDBInt(IsKey = true)]
		[PXUIField(DisplayName = "Contact ID", Visibility = PXUIVisibility.Invisible)]
		public virtual Int32? ContactID { get; set; }
		#endregion

		#region ValidationType
		public abstract class validationType : PX.Data.BQL.BqlString.Field<validationType> { }

		[PXDBString(2, IsKey = true)]
		[PXUIField(DisplayName = "Entity Type")]
		[ValidationTypes]
		public virtual String ValidationType { get; set; }
		#endregion

		#region DuplicateContactID
		public abstract class duplicateContactID : PX.Data.BQL.BqlInt.Field<duplicateContactID> { }

		[PXDBInt(IsKey = true)]
		[PXUIField(DisplayName = "Duplicate Contact ID", Visibility = PXUIVisibility.Invisible)]
		[PXVirtualSelector(typeof(Contact.contactID))]
		public virtual Int32? DuplicateContactID { get; set; }
		#endregion

		#region DuplicateRefContactID
		public abstract class duplicateRefContactID : PX.Data.BQL.BqlInt.Field<duplicateRefContactID> { }

		[PXDBInt]
		[PXUIField(DisplayName = "Contact", Visibility = PXUIVisibility.Invisible)]
		[PXVirtualSelector(typeof(Contact.contactID), DescriptionField = typeof(Contact.displayName))]
		public virtual Int32? DuplicateRefContactID { get; set; }
		#endregion

		#region DuplicateBAccountID
		public abstract class duplicateBAccountID : PX.Data.BQL.BqlInt.Field<duplicateBAccountID> { }

		[PXDBInt]
		[PXUIField(DisplayName = "Duplicate Contact Account ID", Visibility = PXUIVisibility.Invisible)]
		[PXVirtualSelector(typeof(BAccount.bAccountID))]
		public virtual Int32? DuplicateBAccountID { get; set; }
		#endregion

		#region Score
		public abstract class score : PX.Data.BQL.BqlDecimal.Field<score> { }

		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "1")]
		[PXUIField(DisplayName = "Score")]
		public virtual decimal? Score { get; set; }
		#endregion

		#region DuplicateContactType
		public abstract class duplicateContactType : PX.Data.BQL.BqlString.Field<duplicateContactType> { }

		[PXDBString(2, IsFixed = true)]
		[PXUIField(DisplayName = "Duplicate Contact Type", Visible = false)]
		public virtual String DuplicateContactType { get; set; }
		#endregion

		#region Phone1

		// hack: just a field of a main entity of view that is visible in every Deduplication grid to fix grid rendering

		public abstract class phone1 : PX.Data.BQL.BqlString.Field<phone1> { }

		[PXDBString(50, IsUnicode = true)]
		[PXUIField(DisplayName = "Phone 1", Visibility = PXUIVisibility.SelectorVisible)]
		[PhoneValidation()]
		[PXPhone]
		public virtual String Phone1 { get; set; }
		#endregion

		#region CanBeMerged
		public abstract class canBeMerged : PX.Data.BQL.BqlBool.Field<canBeMerged> { }

		[PXBool]
		[PXUIField(DisplayName = "Can Be Merged", Visible = false, Visibility = PXUIVisibility.Invisible, Enabled = false)]
		public bool? CanBeMerged { get; set; }
		#endregion
	}

	[PXVirtual]
	[Serializable]
	[PXHidden]
	public partial class CRDuplicateRecordForLinking : CRDuplicateRecord
	{ }

	#endregion

	#region CRDuplicateResult
	public class CRDuplicateResult : PXResult<CRDuplicateRecord, Contact, DuplicateContact, BAccountR, CRLead, Address, CRActivityStatistics>
	{
		public CRDuplicateResult(CRDuplicateRecord p1, Contact p2, DuplicateContact p3, BAccountR p4, CRLead p5, Address p6, CRActivityStatistics p7)
			: base(p1, p2, p3, p4, p5, p6, p7) { }
	}
	#endregion
}
