using System;
using PX.Data;
using PX.Objects.CR.Workflows;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.SM;
using PX.TM;

namespace PX.Objects.CR.Standalone
{
	/// <summary>
	/// A standalone version of the <see cref="PX.Objects.CR.CRLead"/> class
	/// used to save changes in the <tt>CRLead</tt> table.
	/// Represents a marketing lead or a sales lead.</summary>
	/// <remarks>A marketing lead is a person or a company
	/// that has potential interest in a product your organization offers.
	/// A sales lead is a person or a company that expresses interest in products your organization offers.
	/// The <see cref="PX.Objects.CR.CRLead"/> records are created and edited on the <i>Leads (CR.30.10.00)</i> form,
	/// which corresponds to the <see cref="LeadMaint"/> graph.
	/// </remarks>
	[PXCacheName(Messages.Lead)]
	[PXPrimaryGraph(typeof(LeadMaint))]
	[Serializable]
	public class CRLead : IBqlTable
	{
		#region ContactID
		public abstract class contactID : PX.Data.BQL.BqlInt.Field<contactID> { }

		/// <summary>
		/// The identifier of the lead.
		/// This is the key field. At the same time, it is the identifier of the
		/// <see cref="Contact"/> object.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="Contact.ContactID"/> field.
		/// </value>
		[PXDBInt(IsKey = true, BqlField = typeof(contactID))]
		[PXUIField(DisplayName = "Contact ID", Visibility = PXUIVisibility.Invisible)]
		[PXDBChildIdentity(typeof(CRLead.contactID))]
		public virtual Int32? ContactID { get; set; }
		#endregion

		#region Status
		public abstract class status : PX.Data.BQL.BqlString.Field<status> { }

		/// <summary>
		/// The status of the lead.
		/// </summary>
		/// <value>
		/// The field values are controlled by the workflow engine.
		/// The possible default values of the field are listed in
		/// the <see cref="LeadWorkflow.States"/> class.
		/// </value>
		[PXDBString(1, IsFixed = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Status", Visibility = PXUIVisibility.SelectorVisible)]
		[LeadWorkflow.States.List]
		[PXUIEnabled(typeof(Where<EntryStatus, NotEqual<EntryStatus.inserted>>))]
		public virtual String Status { get; set; }
		#endregion

		#region Resolution
		public abstract class resolution : PX.Data.BQL.BqlString.Field<resolution> { }

		/// <summary>
		/// The reason why the <see cref="Status"/> field of this lead has been changed.
		/// </summary>
		/// <value>
		/// The field values are controlled by the workflow engine, and the field is not used by the application logic directly.
		/// </value>
		[PXDBString(2, IsFixed = true)]
		[PXUIField(DisplayName = "Reason", Visibility = PXUIVisibility.SelectorVisible)]
		[PXStringList(new string[0], new string[0], BqlTable = typeof(CRLead))]
		public virtual String Resolution { get; set; }
		#endregion

		#region ClassID
		public abstract class classID : PX.Data.BQL.BqlString.Field<classID> { }

		/// <summary>
		/// The identifier of the class.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="CRLeadClass.ClassID"/> field.
		/// </value>
		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Lead Class")]
		[PXDefault(typeof(Search<CRSetup.defaultLeadClassID>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXSelector(typeof(CRLeadClass.classID), DescriptionField = typeof(CRLeadClass.description), CacheGlobal = true)]
		public virtual String ClassID { get; set; }
		#endregion

		#region RefContactID
		public abstract class refContactID : PX.Data.BQL.BqlInt.Field<refContactID> { }

		/// <summary>
		/// The identifier of the contact that is associated with this lead.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="Contact.ContactID"/> field.
		/// </value>
		[PXDBInt]
		[PXUIField(DisplayName = "Contact")]
		[PXSelector(typeof(Search<
				ContactAccount.contactID,
				Where<
					ContactAccount.contactType, Equal<ContactTypesAttribute.person>,
					And<WhereEqualNotNull<ContactAccount.bAccountID, Contact.bAccountID>>>>),
			DescriptionField = typeof(Contact.displayName), Filterable = true, DirtyRead = true)]
		[PXDBChildIdentity(typeof(Contact.contactID))]
		public virtual Int32? RefContactID { get; set; }
		#endregion

		#region OverrideRefContact
		public abstract class overrideRefContact : PX.Data.BQL.BqlBool.Field<overrideRefContact> { }

		/// <summary>
		/// Specifies whether the <see cref="Contact">contact</see>
		/// and <see cref="Address">address</see> information of this lead differs from
		/// the contact and address information
		/// of the <see cref="BAccount">business account</see> associated with this lead.
		/// </summary>
		/// <remarks>
		/// The behavior is controlled by the <see cref="LeadMaint.LeadBAccountSharedAddressOverrideGraphExt"/>
		/// graph extension.
		/// </remarks>
		[PXDBBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Null)]
		[PXUIField(DisplayName = "Override")]
		public virtual bool? OverrideRefContact { get; set; }
		#endregion

		#region Description
		public abstract class description : PX.Data.BQL.BqlString.Field<description> { }

		/// <summary>
		/// An alphanumeric string of up to 255 characters that describes the lead.
		/// This field is used to add any additional information about the lead.
		/// </summary>
		[PXDBString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String Description { get; set; }
		#endregion

		#region QualificationDate
		public abstract class qualificationDate : PX.Data.BQL.BqlDateTime.Field<qualificationDate> { }

		/// <summary>
		/// The date when the lead was converted to the <see cref="CROpportunity">opportunity</see>.
		/// </summary>
		/// <value>
		/// The value is filled by the <see cref="LeadMaint.CreateOpportunityAllFromLeadGraphExt"/>
		/// graph extension.
		/// </value>
		[PXDBDate(PreserveTime = true)]
		[PXUIField(DisplayName = "Qualification Date")]
		public virtual DateTime? QualificationDate { get; set; }
		#endregion

		#region ConvertedBy
		public abstract class convertedBy : PX.Data.BQL.BqlGuid.Field<convertedBy> { }

		/// <summary>
		/// The identifier of the <see cref="Users">user</see> who converted the lead to the <see cref="CROpportunity">opportunity</see>.
		/// </summary>
		/// <value>
		/// The value is filled by the <see cref="LeadMaint.CreateOpportunityAllFromLeadGraphExt"/>
		/// graph extension.
		/// </value>
		[PXDBGuid]
		[PXSelector(typeof(Users.pKID), SubstituteKey = typeof(Users.username), DescriptionField = typeof(Users.fullName), CacheGlobal = true, DirtyRead = true, ValidateValue = false)]
		[PXUIField(DisplayName = "Converted By")]
		public virtual Guid? ConvertedBy { get; set; }
		#endregion
	}
}
