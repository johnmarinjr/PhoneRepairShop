using System.Collections.Generic;
using PX.Common;
using PX.Data.EP;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using System;
using System.Linq;
using System.Text;
using PX.Data;
using PX.Objects.CM.Extensions;
using PX.Objects.CR.MassProcess;
using PX.Objects.CS;
using PX.Objects.PO;
using PX.TM;
using PX.Objects.TX;
using PX.Objects.AR;
using PX.Objects.PM;
using PX.Objects.GL;
using PX.Objects.CR.Workflows;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Data.WorkflowAPI;
using PX.Objects.Common;
using PX.Objects.SO;
using PX.Objects.IN;

namespace PX.Objects.CR
{
	/// <summary>
	/// An opportunity represents a potential, ongoing, or closed deal with a prospective or existing customer.
	/// </summary>
	/// <remarks>
	/// An opportunity record is created on the <i>Opportunities (CR304000)</i> form, which corresponds to the <see cref="OpportunityMaint"/> graph.
	/// Note that this class is a projection of the <see cref="Standalone.CROpportunity"/>, <see cref="Standalone.CROpportunityRevision"/>
	/// and <see cref="Standalone.CRQuote"/> classes.
	/// </remarks>
	[System.SerializableAttribute()]
	[PXCacheName(Messages.Opportunity)]
	[PXPrimaryGraph(typeof(OpportunityMaint))]
	[CREmailContactsView(typeof(Select2<Contact,
		LeftJoin<BAccount, On<BAccount.bAccountID, Equal<Contact.bAccountID>>>,
		Where2<Where<Optional<CROpportunity.bAccountID>, IsNull, And<Contact.contactID, Equal<Optional<CROpportunity.contactID>>>>,
			  Or2<Where<Optional<CROpportunity.bAccountID>, IsNotNull, And<Contact.bAccountID, Equal<Optional<CROpportunity.bAccountID>>>>,
				Or<Contact.contactType, Equal<ContactTypesAttribute.employee>>>>>))]
	[PXEMailSource]//NOTE: for assignment map
	[PXProjection(typeof(Select2<Standalone.CROpportunity,
		InnerJoin<Standalone.CROpportunityRevision,
			On<Standalone.CROpportunityRevision.noteID, Equal<Standalone.CROpportunity.defQuoteID>>,
		LeftJoin<Standalone.CRQuote,
			On<Standalone.CRQuote.quoteID, Equal<Standalone.CROpportunity.defQuoteID>>>>>),
		new Type[]
		{
			typeof(Standalone.CROpportunity),
			typeof(Standalone.CROpportunityRevision),
		})]
	[PXGroupMask(typeof(LeftJoin<BAccount, On<BAccount.bAccountID, Equal<CROpportunity.bAccountID>, And<Match<BAccount, Current<AccessInfo.userName>>>>>),
		WhereRestriction = typeof(Where<BAccount.bAccountID, IsNotNull, Or<CROpportunity.bAccountID, IsNull>>))]
	public partial class CROpportunity : IBqlTable, IAssign, IPXSelectable, INotable
	{
		public const int OpportunityIDLength = 10;

		#region Keys
		public class PK : PrimaryKeyOf<CROpportunity>.By<opportunityID>
		{
			public static CROpportunity Find(PXGraph graph, string opportunityID) => FindBy(graph, opportunityID);
		}
		public static class FK
		{
			public class Class : CR.CRCaseClass.PK.ForeignKeyOf<CROpportunity>.By<classID> { }

			public class Address : CR.CRAddress.PK.ForeignKeyOf<CROpportunity>.By<opportunityAddressID> { }
			public class ContactInfo : CR.CRContact.PK.ForeignKeyOf<CROpportunity>.By<opportunityContactID> { }
			public class ShipToAddress : CR.CRAddress.PK.ForeignKeyOf<CROpportunity>.By<shipAddressID> { }
			public class ShipToContactInfo : CR.CRContact.PK.ForeignKeyOf<CROpportunity>.By<shipContactID> { }
			public class BillToAddress : CR.CRAddress.PK.ForeignKeyOf<CROpportunity>.By<billAddressID> { }
			public class BillToContactInfo : CR.CRContact.PK.ForeignKeyOf<CROpportunity>.By<billContactID> { }

			public class Contact : CR.Contact.PK.ForeignKeyOf<CROpportunity>.By<contactID> { }
			public class BusinessAccount : CR.BAccount.PK.ForeignKeyOf<CROpportunity>.By<bAccountID> { }
			public class ParentBusinessAccount : CR.BAccount.PK.ForeignKeyOf<CROpportunity>.By<parentBAccountID> { }
			public class Location : CR.Location.PK.ForeignKeyOf<CROpportunity>.By<bAccountID, locationID> { }

			public class TaxZone : TX.TaxZone.PK.ForeignKeyOf<CROpportunity>.By<taxZoneID> { }
			public class Currency : CM.Currency.PK.ForeignKeyOf<CROpportunity>.By<curyID> { }
			public class CurrencyInfo : CM.CurrencyInfo.PK.ForeignKeyOf<CROpportunity>.By<curyInfoID> { }

			public class Owner : EP.EPEmployee.PK.ForeignKeyOf<CROpportunity>.By<ownerID> { }
			public class Workgroup : TM.EPCompanyTree.PK.ForeignKeyOf<CROpportunity>.By<workgroupID> { }
		}
		#endregion

		#region Events
		public class Events : PXEntityEvent<CROpportunity>.Container<Events>
		{
			public PXEntityEvent<CROpportunity> OpportunityCreatedFromLead;
			public PXEntityEvent<CROpportunity> OpportunityClosed;
			public PXEntityEvent<CROpportunity> OpportunityLost;
			public PXEntityEvent<CROpportunity> OpportunityWon;
		}
		#endregion

		#region Selected
		public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }

		/// <exclude/>
		[PXBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Selected", Visibility = PXUIVisibility.Service)]
		public virtual bool? Selected { get; set; }
		#endregion

		#region OpportunityID
		public abstract class opportunityID : PX.Data.BQL.BqlString.Field<opportunityID> { }

		/// <summary>
		/// The identifier of the opportunity.
		/// </summary>
		/// <remarks>
		/// This field depends on <see cref="CRSetup.opportunityNumberingID"/>.
		/// </remarks>
		[PXDBString(OpportunityIDLength, IsUnicode = true, IsKey = true, InputMask = ">CCCCCCCCCCCCCCC", BqlField = typeof(Standalone.CROpportunity.opportunityID))]
		[PXUIField(DisplayName = "Opportunity ID", Visibility = PXUIVisibility.SelectorVisible)]
		[AutoNumber(typeof(CRSetup.opportunityNumberingID), typeof(AccessInfo.businessDate))]
		[PXSelector(typeof(Search2<CROpportunity.opportunityID,
			LeftJoin<BAccount, On<BAccount.bAccountID, Equal<CROpportunity.bAccountID>>,
			LeftJoin<Contact, On<Contact.contactID, Equal<CROpportunity.contactID>>>>,
			Where<BAccount.bAccountID, IsNull, Or<Match<BAccount, Current<AccessInfo.userName>>>>,
			OrderBy<Desc<CROpportunity.opportunityID>>>),
			new[] { typeof(CROpportunity.opportunityID),
				typeof(CROpportunity.subject),
				typeof(CROpportunity.status),
				typeof(CROpportunity.curyAmount),
				typeof(CROpportunity.curyID),
				typeof(CROpportunity.closeDate),
				typeof(CROpportunity.stageID),
				typeof(CROpportunity.classID),
				typeof(CROpportunity.isActive),
				typeof(BAccount.acctName),
				typeof(Contact.displayName) },
				Filterable = true)]
		[PXFieldDescription]
		public virtual String OpportunityID { get; set; }
		#endregion

		#region OpportunityAddressID
		public abstract class opportunityAddressID : PX.Data.BQL.BqlInt.Field<opportunityAddressID> { }
		protected Int32? _OpportunityAddressID;

		/// <summary>
		/// The identifier of the <see cref="CRAddress"/> object linked with the current document.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="CRAddress.addressID"/> field.
		/// </value>
		[PXDBInt(BqlField = typeof(Standalone.CROpportunityRevision.opportunityAddressID))]
		[CROpportunityAddress(typeof(Select<Address, Where<True, Equal<False>>>))]
		public virtual Int32? OpportunityAddressID
		{
			get
			{
				return this._OpportunityAddressID;
			}
			set
			{
				this._OpportunityAddressID = value;
			}
		}
		#endregion

		#region OpportunityContactID
		public abstract class opportunityContactID : PX.Data.BQL.BqlInt.Field<opportunityContactID> { }
		protected Int32? _OpportunityContactID;

		/// <summary>
		/// The identifier of the <see cref="CRContact"/> object linked with the current document.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="CRContact.contactID"/> field.
		/// </value>
		[PXDBInt(BqlField = typeof(Standalone.CROpportunityRevision.opportunityContactID))]
		[CROpportunityContact(typeof(Select<Contact, Where<True, Equal<False>>>))]
		public virtual Int32? OpportunityContactID
		{
			get
			{

				return this._OpportunityContactID;
			}
			set
			{
				this._OpportunityContactID = value;
			}
		}
		#endregion

		#region TermsID
		public abstract class termsID : PX.Data.BQL.BqlString.Field<termsID> { }
		/// <summary>
		/// The identifier of the default <see cref="Terms">terms</see>, 
		/// which are applied to the documents of the customer.
		/// </summary>
		[PXDBString(10, IsUnicode = true, BqlField = typeof(Standalone.CROpportunityRevision.termsID))]
		[PXSelector(typeof(Search<Terms.termsID,
			Where<Terms.visibleTo, Equal<TermsVisibleTo.customer>,
				Or<Terms.visibleTo, Equal<TermsVisibleTo.all>>>>),
			DescriptionField = typeof(Terms.descr),
			CacheGlobal = true)]
		[PXDefault(
			typeof(Search<Customer.termsID, Where<Customer.bAccountID, Equal<Current<CROpportunity.bAccountID>>>>)
			, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Default<CROpportunity.bAccountID>))]
		[PXUIField(DisplayName = "Credit Terms")]
		public virtual String TermsID { get; set; }
		#endregion

		#region AllowOverrideContactAddress
		public abstract class allowOverrideContactAddress : PX.Data.BQL.BqlBool.Field<allowOverrideContactAddress> { }
		protected Boolean? _AllowOverrideContactAddress;

		/// <summary>
		/// Specifies whether the <see cref="Contact">contact</see>
		/// and <see cref="Address">address</see> information of this opportunity differs from
		/// the contact and address information
		/// of the <see cref="BAccount">business account</see> associated with this opportunity.
		/// </summary>
		/// <remarks>
		/// The behavior is controlled by the <see cref="OpportunityMaint.ContactAddress"/> graph extension derived from the <see cref="CR.Extensions.CROpportunityContactAddress.CROpportunityContactAddressExt{TGraph}"/>
		/// graph extension.
		/// </remarks>
		[PXDBBool(BqlField = typeof(Standalone.CROpportunityRevision.allowOverrideContactAddress))]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Override")]
		public virtual Boolean? AllowOverrideContactAddress
		{
			get
			{
				return this._AllowOverrideContactAddress;
			}
			set
			{
				this._AllowOverrideContactAddress = value;
			}
		}
		#endregion

		#region BAccountID
		public abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }

		/// <summary>
		/// The identifier of the related <see cref="BAccount">business account</see>.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="CR.BAccount.BAccountID"/> field.
		/// </value>
		[CRMBAccount(bAccountTypes: new Type[]
			{
				typeof(BAccountType.prospectType),
				typeof(BAccountType.customerType),
				typeof(BAccountType.combinedType),
			},
			BqlField = typeof(Standalone.CROpportunityRevision.bAccountID))]
		public virtual Int32? BAccountID { get; set; }
		#endregion

		#region LocationID
		public abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }

		/// <summary>
		/// The identifier of the default location <see cref="Location"/> object linked with the prospective or existing customer selected in the Business Account box.
		/// If no location is selected in this box, the settings on the <b>Shipping</b> tab are empty and available for editing.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="Location.LocationID"/> field.
		/// </value>
		/// <remarks>
		/// Also, the <see cref="Location.BAccountID">Location.BAccountID</see> value must be equal to
		/// the <see cref="CROpportunity.BAccountID">CROpportunity.BAccountID</see> value of the current opportunity.
		/// </remarks>
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[LocationActive(typeof(Where<Location.bAccountID, Equal<Current<CROpportunity.bAccountID>>>),
			DisplayName = "Account Location",
			DescriptionField = typeof(Location.descr),
			BqlField = typeof(Standalone.CROpportunityRevision.locationID))]
		public virtual Int32? LocationID { get; set; }
		#endregion

		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }

		/// <summary>The identifier of the <see cref="Branch" /> that will be used to ship the goods to the customer.</summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="Branch.BranchID" /> field.
		/// </value>
		[Branch(typeof(AccessInfo.branchID), IsDetail = false, TabOrder = 0,
			BqlField = typeof(Standalone.CROpportunityRevision.branchID))]
		[PXFormula(typeof(Switch<
							Case<Where<PendingValue<CROpportunity.branchID>, IsNotNull>, Null,
							Case<Where<CROpportunity.locationID, IsNotNull,
									And<Selector<CROpportunity.locationID, Location.cBranchID>, IsNotNull>>,
								Selector<CROpportunity.locationID, Location.cBranchID>,
							Case<Where<Current2<CROpportunity.branchID>, IsNotNull>,
								Current2<CROpportunity.branchID>>>>,
							Current<AccessInfo.branchID>>))]
		public virtual Int32? BranchID { get; set; }

		#endregion

		#region ContactID
		public abstract class contactID : PX.Data.BQL.BqlInt.Field<contactID> { }

		protected Int32? _ContactID;

		/// <summary>
		/// The identifier of the <see cref="CR.Contact"/>, the representative to be contacted about the opportunity.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="CR.Contact.ContactID"/> field.
		/// </value>
		[ContactRaw(typeof(CROpportunity.bAccountID), WithContactDefaultingByBAccount = true, BqlField = typeof(Standalone.CROpportunityRevision.contactID))]
		[PXRestrictor(typeof(Where2<Where2<
						Where<
							Contact.contactType, Equal<ContactTypesAttribute.person>>,
						And<
							Where<BAccount.type, IsNull,
								Or<BAccount.type, Equal<BAccountType.customerType>,
								Or<BAccount.type, Equal<BAccountType.prospectType>,
								Or<BAccount.type, Equal<BAccountType.combinedType>>>>>>>,
				And<PMQuote.bAccountID.FromCurrent.IsNull.Or<BAccount.bAccountID.IsEqual<PMQuote.bAccountID.FromCurrent>>>>),
			Messages.ContactBAccountOpp,
			typeof(Contact.displayName),
			typeof(Contact.contactID))]
		[PXDBChildIdentity(typeof(Contact.contactID))]
		public virtual Int32? ContactID
		{
			get { return _ContactID; }
			set { _ContactID = value; }
		}
		#endregion

		#region LeadID
		public abstract class leadID : PX.Data.BQL.BqlGuid.Field<leadID> { }

		/// <summary>
		/// The identifier of the <see cref="CR.CRLead">lead</see> that has been converted to this opportunity.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="CRLead.NoteID"/> field.
		/// </value>
		[PXDBGuid(BqlField = typeof(Standalone.CROpportunity.leadID))]
		[PXUIField(DisplayName = "Source Lead", Enabled = false)]
		[PXSelector(typeof(
			Search<Contact.noteID,
				Where<Contact.contactType.IsEqual<ContactTypesAttribute.lead>>>),
			DescriptionField = typeof(Contact.displayName))]
		public virtual Guid? LeadID { get; set; }
		#endregion

		#region CROpportunityClassID
		public abstract class classID : PX.Data.BQL.BqlString.Field<classID> { }

		/// <summary>
		/// The identifier of the <see cref="CROpportunityClass"/>.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="CROpportunityClass.CROpportunityClassID"/> field.
		/// </value>
		[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa", BqlField = typeof(Standalone.CROpportunity.classID))]
		[PXUIField(DisplayName = "Class ID")]
		[PXDefault]
		[PXSelector(typeof(CROpportunityClass.cROpportunityClassID),
			DescriptionField = typeof(CROpportunityClass.description), CacheGlobal = true)]
		[PXMassUpdatableField]
		public virtual String ClassID { get; set; }
		#endregion

		#region Subject
		public abstract class subject : PX.Data.BQL.BqlString.Field<subject> { }
		/// <summary>
		/// The subject or description of the opportunity.
		/// </summary>
		/// <value>
		/// An alphanumeric string of up to 255 characters that describes the opportunity.
		/// </value>
		[PXDBString(255, IsUnicode = true, BqlField = typeof(Standalone.CROpportunity.subject))]
		[PXDefault(PersistingCheck = PXPersistingCheck.NullOrBlank)]
		[PXUIField(DisplayName = "Subject", Visibility = PXUIVisibility.SelectorVisible)]
		[PXFieldDescription]
		public virtual String Subject { get; set; }
		#endregion

		#region Details
		public abstract class details : PX.Data.BQL.BqlString.Field<details> { }

		/// <summary>
		/// The detailed description or any relevant notes of the opportunity
		/// </summary>
		/// <value>
		/// The value is in rich text format.
		/// </value>
		[PXDBText(IsUnicode = true, BqlField = typeof(Standalone.CROpportunity.details))]
		[PXUIField(DisplayName = "Details")]
		public virtual String Details { get; set; }
		#endregion

		#region ParentBAccountID
		public abstract class parentBAccountID : PX.Data.BQL.BqlInt.Field<parentBAccountID> { }

		/// <summary>
		/// The identifier of the parent business account.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="CR.BAccount.BAccountID"/> field of the parent account.
		/// This field is used for consolidating customer account balances on the parent account from child accounts.
		/// </value>
		[CRMParentBAccount(typeof(CROpportunity.bAccountID), BqlField = typeof(Standalone.CROpportunityRevision.parentBAccountID))]
		[PXFormula(typeof(Selector<CROpportunity.bAccountID, BAccount.parentBAccountID>))]
		public virtual Int32? ParentBAccountID { get; set; }
		#endregion

		#region ShipContactID
		public abstract class shipContactID : PX.Data.IBqlField
		{
		}
		protected Int32? _ShipContactID;

		/// <summary>
		/// The identifier of the shipping contact that is associated with this opportunity.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="CRContact.ContactID"/> field.
		/// </value>
		/// <remark>
		/// The initial value of the field is taken from the corresponding shipping contact.
		/// </remark>
		[PXDBInt(BqlField = typeof(Standalone.CROpportunityRevision.shipContactID))]
		[CRShippingContact(typeof(Select<Contact, Where<True, Equal<False>>>))]
		public virtual Int32? ShipContactID
		{
			get
			{
				return this._ShipContactID;
			}
			set
			{
				this._ShipContactID = value;
			}
		}
		#endregion

		#region ShipAddressID
		public abstract class shipAddressID : PX.Data.IBqlField
		{
		}
		protected Int32? _ShipAddressID;

		/// <summary>
		/// The identifier of the shipping address that is associated with this opportunity.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="CRAddress.AddressID"/> field.
		/// </value>
		/// <remark>
		/// The initial value can be taken from the associated Location or BAccount object, or this behavior can be overridden.
		/// </remark>
		[PXDBInt(BqlField = typeof(Standalone.CROpportunityRevision.shipAddressID))]
		[CRShippingAddress(typeof(Select<Address, Where<True, Equal<False>>>))]
		public virtual Int32? ShipAddressID
		{
			get
			{
				return this._ShipAddressID;
			}
			set
			{
				this._ShipAddressID = value;
			}
		}
		#endregion

		#region BillContactID
		public abstract class billContactID : BqlInt.Field<billContactID> {}

		/// <summary>
		/// The identifier of the billing contact that is associated with this opportunity.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="CRContact.ContactID"/> field.
		/// </value>
		/// <remark>
		/// The initial value is copied from the business account specified in the associated BAccount object, although this behavior it can be overridden.
		/// </remark>
		[PXDBInt(BqlField = typeof(Standalone.CROpportunityRevision.billContactID))]
		[CRBillingContact(typeof(Select<Contact, Where<True, Equal<False>>>))]
		public virtual Int32? BillContactID { get; set; }
		#endregion

		#region BillAddressID
		public abstract class billAddressID : BqlInt.Field<billAddressID> {}

		/// <summary>
		/// The identifier of the billing address that is associated with this opportunity.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="CRAddress.AddressID"/> field.
		/// </value>
		/// <remark>
		/// The initial value is copied from the business account specified in the Business Account, although it can be overridden.
		/// </remark>
		[PXDBInt(BqlField = typeof(Standalone.CROpportunityRevision.billAddressID))]
		[CRBillingAddress(typeof(Select<Address, Where<True, Equal<False>>>))]
		public virtual Int32? BillAddressID { get; set; }
		#endregion

		#region AllowOverrideShippingContactAddress
		public abstract class allowOverrideShippingContactAddress : PX.Data.IBqlField
		{
		}
		protected Boolean? _AllowOverrideShippingContactAddress;

		/// <summary>
		/// Specifies whether the shipping <see cref="CRContact">contact</see> of this opportunity differs from
		/// the <see cref="Contact">contact</see> information of the <see cref="BAccount">business account</see>
		/// associated with this opportunity.
		/// </summary>
		/// <remarks>
		/// The behavior is controlled by the <see cref="OpportunityMaint.ContactAddress"/> graph extension derived from the <see cref="CR.Extensions.CROpportunityContactAddress.CROpportunityContactAddressExt{TGraph}"/>
		/// graph extension.
		/// </remarks>
		[PXDBBool(BqlField = typeof(Standalone.CROpportunityRevision.allowOverrideShippingContactAddress))]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Override Shipping Info")]
		public virtual Boolean? AllowOverrideShippingContactAddress
		{
			get
			{
				return this._AllowOverrideShippingContactAddress;
			}
			set
			{
				this._AllowOverrideShippingContactAddress = value;
			}
		}
		#endregion


		#region AllowOverrideBillingContactAddress
		/// <summary>
		/// Virtual field used to set <see cref="CRBillingContact.IsDefaultContact"/>
		/// and <see cref="CRBillingAddress.IsDefaultAddress"/> by the workflow.
		/// The behavior is controlled by <see cref="CR.Extensions.CROpportunityContactAddress.CROpportunityContactAddressExt{TGraph}"/>.
		/// </summary>
		[PXBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual bool? AllowOverrideBillingContactAddress { get; set; }
		public abstract class allowOverrideBillingContactAddress : BqlBool.Field<allowOverrideBillingContactAddress> { }
		#endregion

		#region ProjectID
		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }

		/// <summary>
		/// The <see cref="PX.Objects.PM.PMProject">project</see> with which the item is associated.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="PX.Objects.PM.PMProject.contractID"/> field.
		/// </value>
		/// <remark>
		/// The project that is specified in the Location object of the account location. The system uses the project when it creates a document, such as a sales order.
		/// </remark>
		[ProjectDefault(BatchModule.CR,
			typeof(Search<Location.cDefProjectID,
				Where<Location.bAccountID, Equal<Current<CROpportunity.bAccountID>>,
					And<Location.locationID, Equal<Current<CROpportunity.locationID>>>>>))]
		[PXRestrictor(typeof(Where<PMProject.isActive, Equal<True>>), PM.Messages.InactiveContract, typeof(PMProject.contractCD))]
		[PXRestrictor(typeof(Where<PMProject.visibleInCR, Equal<True>, Or<PMProject.nonProject, Equal<True>>>), PM.Messages.ProjectInvisibleInModule, typeof(PMProject.contractCD))]
		[ProjectBaseAttribute(typeof(CROpportunity.bAccountID), BqlField = typeof(Standalone.CROpportunityRevision.projectID))]
		[PXFormula(typeof(Default<locationID>))]
		public virtual Int32? ProjectID { get; set; }
		#endregion

		#region DocumentDate
		public abstract class documentDate : PX.Data.BQL.BqlDateTime.Field<documentDate> { }

		/// <summary>
		/// The document date.
		/// </summary>
		/// <value>
		/// Date without time.
		/// </value>
		/// <remarks>
		/// After the opportunity is closed, this field is equal to <see cref="CROpportunity.CloseDate">CloseDate</see>.
		/// </remarks>
		[PXDBDate(BqlField = typeof(Standalone.CROpportunityRevision.documentDate))]
		[PXDefault(typeof(AccessInfo.businessDate))]
		[PXUIField(DisplayName = "Document Date", Visibility = PXUIVisibility.Invisible, Visible = false)]
		public virtual DateTime? DocumentDate { get; set; }
		#endregion

		#region CloseDate
		public abstract class closeDate : PX.Data.BQL.BqlDateTime.Field<closeDate> { }

		/// <summary>
		/// The estimated date of closing the deal.
		/// </summary>
		/// <value>
		/// Date value.
		/// </value>
		[PXDBDate(BqlField = typeof(Standalone.CROpportunity.closeDate))]
		[PXDefault(typeof(AccessInfo.businessDate))]
		[PXMassUpdatableField]
		[PXUIField(DisplayName = "Estimated Close Date", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual DateTime? CloseDate { get; set; }
		#endregion

		#region StageID
		public abstract class stageID : PX.Data.BQL.BqlString.Field<stageID> { }

		/// <summary>
		/// The current stage of the opportunity.
		/// </summary>
		/// <value>
		/// Possible values are determined by the settings specified for the
		/// <see cref="ClassID"/> opportunity class. The set of possible values can be changed and extended by using the workflow engine.
		/// </value>
		[PXDBString(2, BqlField = typeof(Standalone.CROpportunity.stageID))]
		[PXUIField(DisplayName = "Stage")]
		[CROpportunityStages(typeof(classID), typeof(stageChangedDate), OnlyActiveStages = true)]
		[PXDefault]
		[PXMassUpdatableField]
		public virtual String StageID { get; set; }
		#endregion

		#region StageChangedDate
		public abstract class stageChangedDate : PX.Data.BQL.BqlDateTime.Field<stageChangedDate> { }

		/// <summary>
		/// The date when the opportunity status or stage was changed.
		/// </summary>
		/// <value>
		/// The value is controlled by the <see cref="CROpportunityStagesAttribute">CROpportunityStages</see> attribute defined for the <see cref="StageID" /> property.
		/// </value>
		[PXDBDate(PreserveTime = true, BqlField = typeof(Standalone.CROpportunity.stageChangedDate))]
		[PXUIField(DisplayName = "Stage Change Date", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual DateTime? StageChangedDate { get; set; }
		#endregion

		#region CampaignSourceID
		public abstract class campaignSourceID : PX.Data.BQL.BqlString.Field<campaignSourceID> { }

		/// <summary>
		/// The marketing campaign that resulted in the creation of the opportunity.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="CRCampaign.CampaignID"/> field.
		/// </value>
		[PXDBString(10, IsUnicode = true, BqlField = typeof(Standalone.CROpportunityRevision.campaignSourceID))]
		[PXUIField(DisplayName = "Source Campaign")]
		[PXSelector(typeof(Search3<CRCampaign.campaignID, OrderBy<Desc<CRCampaign.campaignID>>>),
			DescriptionField = typeof(CRCampaign.campaignName), Filterable = true)]
		public virtual String CampaignSourceID { get; set; }
		#endregion

		#region Status
		public abstract class status : PX.Data.BQL.BqlString.Field<status> { }

		/// <summary>
		/// The current status of the opportunity.
		/// </summary>
		/// <value>
		/// The set of possible values can be changed and extended by using the workflow engine.
		/// </value>
		[PXDBString(1, IsFixed = true, BqlField = typeof(Standalone.CROpportunity.status))]
		[PXUIField(DisplayName = "Status", Visibility = PXUIVisibility.SelectorVisible)]
		[OpportunityStatus.List]
		[PXDefault]
		public virtual string Status { get; set; }

		#endregion

		#region IsActive
		public abstract class isActive : PX.Data.BQL.BqlBool.Field<isActive> { }

		/// <summary>
		/// Indicates whether the opportunity is active.
		/// </summary>
		/// <value>
		/// The default value is <see langword="true"/>.
		/// </value>
		[PXDBBool(BqlField = typeof(Standalone.CROpportunity.isActive))]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Active")]
		public virtual bool? IsActive { get; set; }
		#endregion

		#region Resolution
		public abstract class resolution : PX.Data.BQL.BqlString.Field<resolution> { }

		/// <summary>
		/// The reason why the status of the opportunity has been changed.
		/// </summary>
		/// <value>
		/// The possible values of the field are listed in
		/// the <see cref="OpportunityReason"/> class.
		/// </value>
		[PXDBString(2, IsFixed = true, BqlField = typeof(Standalone.CROpportunity.resolution))]
		[OpportunityReason.List]
		[PXUIField(DisplayName = "Reason")]
		[PXMassUpdatableField]
		public virtual String Resolution { get; set; }
		#endregion

		#region AssignDate
		public abstract class assignDate : PX.Data.BQL.BqlDateTime.Field<assignDate> { }

		/// <summary>
		/// The date of the assignment of the owner.
		/// </summary>
		/// <value>
		/// The date when <see cref="OwnerID"/> was assigned to the opportunity.
		/// </value>
		[PXDBDate(PreserveTime = true, BqlField = typeof(Standalone.CROpportunity.assignDate))]
		[PXUIField(DisplayName = "Assignment Date")]
		public virtual DateTime? AssignDate { get; set; }
		#endregion

		#region ClosingDate
		public abstract class closingDate : PX.Data.BQL.BqlDateTime.Field<closingDate> { }

		/// <summary>
		/// The date of closing the opportunity.
		/// </summary>
		[PXDBDate(PreserveTime = true, BqlField = typeof(Standalone.CROpportunity.closingDate))]
		[PXUIField(DisplayName = "Actual Close Date")]
		public virtual DateTime? ClosingDate { get; set; }
		#endregion

		#region WorkgroupID
		public abstract class workgroupID : PX.Data.BQL.BqlInt.Field<workgroupID> { }

		/// <summary>
		/// The workgroup associated with the opportunity.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="PX.TM.EPCompanyTree.WorkGroupID">EPCompanyTree.WorkGroupID</see> field.
		/// </value>
		[PXDBInt(BqlField = typeof(Standalone.CROpportunityRevision.workgroupID))]
		[PXCompanyTreeSelector]
		[PXUIField(DisplayName = "Workgroup")]
		[PXMassUpdatableField]
		public virtual int? WorkgroupID { get; set; }
		#endregion

		#region OwnerID
		public abstract class ownerID : PX.Data.BQL.BqlInt.Field<ownerID> { }

		/// <inheritdoc/>
		[Owner(typeof(CROpportunity.workgroupID), BqlField = typeof(Standalone.CROpportunityRevision.ownerID))]
		[PXMassUpdatableField]
		public virtual int? OwnerID { get; set; }
		#endregion

		#region CuryID
		public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }

		/// <summary>
		/// The currency of the opportunity.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="Currency" />.
		/// </value>
		[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL", BqlField = typeof(Standalone.CROpportunityRevision.curyID))]
		[PXDefault(typeof(Search<CRSetup.defaultCuryID>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXSelector(typeof(Currency.curyID))]
		[PXUIField(DisplayName = "Currency", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String CuryID { get; set; }
		#endregion

		#region CuryInfoID
		public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }

		/// <summary>
		/// Identifier of the <see cref="PX.Objects.CM.Extensions.CurrencyInfo">CurrencyInfo</see> object associated with the transaction.
		/// </summary>
		/// <value>
		/// Generated automatically. Corresponds to the <see cref="PX.Objects.CM.Extensions.CurrencyInfo.CuryInfoID"/> field.
		/// </value>
		[PXDBLong(BqlField = typeof(Standalone.CROpportunityRevision.curyInfoID))]
		[CurrencyInfo]
		public virtual Int64? CuryInfoID { get; set; }
		#endregion

		#region ExtPriceTotal
		public abstract class extPriceTotal : PX.Data.BQL.BqlDecimal.Field<extPriceTotal> { }
		[PXDBDecimal(4, BqlField = typeof(Standalone.CROpportunityRevision.extPriceTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? ExtPriceTotal { get; set; }
		#endregion

		#region CuryExtPriceTotal
		public abstract class curyExtPriceTotal : PX.Data.BQL.BqlDecimal.Field<curyExtPriceTotal> { }
		[PXUIField(DisplayName = "Subtotal", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXDBCurrency(typeof(curyInfoID), typeof(extPriceTotal), BqlField = typeof(Standalone.CROpportunityRevision.curyExtPriceTotal))]
		public virtual Decimal? CuryExtPriceTotal { get; set; }
		#endregion

		#region LineTotal
		public abstract class lineTotal : PX.Data.BQL.BqlDecimal.Field<lineTotal> { }

		[PXDBDecimal(4, BqlField = typeof(Standalone.CROpportunityRevision.lineTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? LineTotal { get; set; }
		#endregion

		#region CuryLineTotal
		public abstract class curyLineTotal : PX.Data.BQL.BqlDecimal.Field<curyLineTotal> { }

		[PXDBCurrency(typeof(curyInfoID), typeof(lineTotal), BqlField = typeof(Standalone.CROpportunityRevision.curyLineTotal))]
		[PXUIField(DisplayName = "Detail Total", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryLineTotal { get; set; }
		#endregion

		#region LineDiscountTotal
		public abstract class lineDiscountTotal : PX.Data.BQL.BqlDecimal.Field<lineDiscountTotal> { }

		[PXDBDecimal(4, BqlField = typeof(Standalone.CROpportunityRevision.lineDiscountTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? LineDiscountTotal { get; set; }
		#endregion

		#region CuryLineDiscountTotal
		public abstract class curyLineDiscountTotal : PX.Data.BQL.BqlDecimal.Field<curyLineDiscountTotal> { }

		[PXDBCurrency(typeof(curyInfoID), typeof(lineDiscountTotal), BqlField = typeof(Standalone.CROpportunityRevision.curyLineDiscountTotal))]
		[PXUIField(DisplayName = "Discount", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryLineDiscountTotal { get; set; }
		#endregion

		#region LineDocDiscountTotal
		public abstract class lineDocDiscountTotal : PX.Data.BQL.BqlDecimal.Field<lineDocDiscountTotal> { }

		[PXDBDecimal(4, BqlField = typeof(Standalone.CROpportunityRevision.lineDocDiscountTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? LineDocDiscountTotal { get; set; }
		#endregion

		#region CuryLineDocDiscountTotal
		public abstract class curyLineDocDiscountTotal : PX.Data.BQL.BqlDecimal.Field<curyLineDocDiscountTotal> { }

		[PXDBCurrency(typeof(curyInfoID), typeof(lineDocDiscountTotal), BqlField = typeof(Standalone.CROpportunityRevision.curyLineDocDiscountTotal))]
		[PXUIField(Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryLineDocDiscountTotal { get; set; }
		#endregion

		#region IsTaxValid
		public abstract class isTaxValid : PX.Data.BQL.BqlBool.Field<isTaxValid> { }

		/// <summary>
		/// Indicates whether the tax amount calculated with the External Tax Provider is actual and does not require recalculation.
		/// </summary>
		[PXDBBool(BqlField = typeof(Standalone.CROpportunityRevision.isTaxValid))]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Tax Is Up to Date", Enabled = false)]
		public virtual Boolean? IsTaxValid
		{
			get;
			set;
		}
		#endregion

		#region TaxTotal
		public abstract class taxTotal : PX.Data.BQL.BqlDecimal.Field<taxTotal> { }

		/// <inheritdoc cref="PX.Objects.CA.CABankTran.TaxTotal"/>
		[PXDBDecimal(4, BqlField = typeof(Standalone.CROpportunityRevision.taxTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? TaxTotal { get; set; }
		#endregion

		#region CuryTaxTotal
		public abstract class curyTaxTotal : PX.Data.BQL.BqlDecimal.Field<curyTaxTotal> { }

		/// <summary>
		/// The total amount of tax paid on the document in the selected currency.
		/// </summary>
		[PXDBCurrency(typeof(curyInfoID), typeof(taxTotal), BqlField = typeof(Standalone.CROpportunityRevision.curyTaxTotal))]
		[PXUIField(DisplayName = "Tax Total", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryTaxTotal { get; set; }
		#endregion

		#region ManualTotal
		public abstract class manualTotalEntry : PX.Data.BQL.BqlBool.Field<manualTotalEntry> { }

		[PXDBBool(BqlField = typeof(Standalone.CROpportunityRevision.manualTotalEntry))]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Manual Amount")]
		public virtual Boolean? ManualTotalEntry { get; set; }
		#endregion

		#region Amount
		public abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }

		private decimal? _amount;
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXDBBaseCury(BqlField = typeof(Standalone.CROpportunityRevision.amount))]
		public virtual Decimal? Amount
		{
			get { return _amount; }
			set { _amount = value; }
		}

		#endregion

		#region DiscTot
		public abstract class discTot : PX.Data.BQL.BqlDecimal.Field<discTot> { }

		private decimal? _discTot;

		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXDBBaseCury(BqlField = typeof(Standalone.CROpportunityRevision.discTot))]
		public virtual Decimal? DiscTot
		{
			[PXDependsOnFields(typeof(lineDiscountTotal), typeof(manualTotalEntry))]
			get { return _discTot; }
			set { _discTot = value; }
		}
		#endregion

		#region CuryAmount
		public abstract class curyAmount : PX.Data.BQL.BqlDecimal.Field<curyAmount> { }

		private decimal? _curyAmount;

		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXDBCurrency(typeof(curyInfoID), typeof(amount), BqlField = typeof(Standalone.CROpportunityRevision.curyAmount))]
		[PXFormula(typeof(Switch<Case<Where<manualTotalEntry, Equal<True>>, curyAmount>, curyLineTotal>))]
		[PXUIField(DisplayName = "Amount", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual Decimal? CuryAmount
		{
			get { return _curyAmount; }
			set { _curyAmount = value; }
		}

		#endregion

		#region CuryDiscTot
		public abstract class curyDiscTot : PX.Data.BQL.BqlDecimal.Field<curyDiscTot> { }

		private decimal? _curyDiscTot;
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXDBCurrency(typeof(curyInfoID), typeof(discTot), BqlField = typeof(Standalone.CROpportunityRevision.curyDiscTot))]
		[PXUIField(DisplayName = "Discount", Visibility = PXUIVisibility.SelectorVisible)]
		[PXFormula(typeof(Switch<Case<Where<manualTotalEntry, Equal<True>>, curyDiscTot>, curyLineDocDiscountTotal>))]
		public virtual Decimal? CuryDiscTot
		{
			get { return _curyDiscTot; }
			set { _curyDiscTot = value; }
		}
		#endregion

		#region RawAmount
		public abstract class rawAmount : PX.Data.BQL.BqlDecimal.Field<rawAmount> { }

		[PXBaseCury]
		[PXDBCalced(typeof(Switch<
			Case<Where<Standalone.CROpportunityRevision.manualTotalEntry, Equal<True>>, Standalone.CROpportunityRevision.amount>,
			Standalone.CROpportunityRevision.lineTotal>), typeof(decimal))]
		public virtual Decimal? RawAmount
		{
			get;
			set;
		}

		#endregion

		#region CuryRawAmount
		public abstract class curyRawAmount : PX.Data.BQL.BqlDecimal.Field<curyRawAmount> { }

		[PXBaseCury]
		[PXDBCalced(typeof(Switch<
			Case<Where<Standalone.CROpportunityRevision.manualTotalEntry, Equal<True>>, Standalone.CROpportunityRevision.curyAmount>,
			Standalone.CROpportunityRevision.curyLineTotal>), typeof(decimal))]
		public virtual Decimal? CuryRawAmount
		{
			get;
			set;
		}

		#endregion

		#region ProductsAmount
		public abstract class productsAmount : PX.Data.BQL.BqlDecimal.Field<productsAmount> { }
		[PXDependsOnFields(typeof(amount), typeof(discTot), typeof(manualTotalEntry), typeof(lineTotal))]
		[PXDBDecimal(4, BqlField = typeof(Standalone.CROpportunityRevision.productsAmount))]
		[PXUIField(DisplayName = "Products Amount")]
		public virtual Decimal? ProductsAmount { get; set; }
		#endregion

		#region CuryProductsAmount
		public abstract class curyProductsAmount : PX.Data.BQL.BqlDecimal.Field<curyProductsAmount> { }
		[PXDependsOnFields(typeof(curyAmount), typeof(curyDiscTot), typeof(curyTaxTotal))]
		[PXDBCurrency(typeof(curyInfoID), typeof(productsAmount), BqlField = typeof(Standalone.CROpportunityRevision.curyProductsAmount))]
		[PXUIField(DisplayName = "Total", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryProductsAmount { get; set; }
		#endregion

		#region CuryWgtAmount
		public abstract class curyWgtAmount : PX.Data.BQL.BqlDecimal.Field<curyWgtAmount> { }

		[PXDecimal()]
		[PXUIField(DisplayName = "Wgt. Total", Enabled = false)]
		public virtual Decimal? CuryWgtAmount { get; set; }
		#endregion

		#region CuryVatExemptTotal
		public abstract class curyVatExemptTotal : PX.Data.BQL.BqlDecimal.Field<curyVatExemptTotal> { }

		/// <inheritdoc cref="PX.Objects.CA.CABankTran.CuryVatExemptTotal"/>
		[PXDBCurrency(typeof(curyInfoID), typeof(vatExemptTotal), BqlField = typeof(Standalone.CROpportunityRevision.curyVatExemptTotal))]
		[PXUIField(DisplayName = "VAT Exempt Total", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryVatExemptTotal { get; set; }
		#endregion

		#region VatExemptTaxTotal
		public abstract class vatExemptTotal : PX.Data.BQL.BqlDecimal.Field<vatExemptTotal> { }

		/// <inheritdoc cref="PX.Objects.CA.CABankTran.VatExemptTotal"/>
		[PXDBDecimal(4, BqlField = typeof(Standalone.CROpportunityRevision.vatExemptTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? VatExemptTotal { get; set; }
		#endregion

		#region CuryVatTaxableTotal
		public abstract class curyVatTaxableTotal : PX.Data.BQL.BqlDecimal.Field<curyVatTaxableTotal> { }

		/// <inheritdoc cref="PX.Objects.CA.CABankTran.CuryVatTaxableTotal"/>
		[PXDBCurrency(typeof(curyInfoID), typeof(vatTaxableTotal), BqlField = typeof(Standalone.CROpportunityRevision.curyVatTaxableTotal))]
		[PXUIField(DisplayName = "VAT Taxable Total", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryVatTaxableTotal { get; set; }
		#endregion

		#region VatTaxableTotal
		public abstract class vatTaxableTotal : PX.Data.BQL.BqlDecimal.Field<vatTaxableTotal> { }

		/// <inheritdoc cref="PX.Objects.CA.CABankTran.VatTaxableTotal"/>
		[PXDBDecimal(4, BqlField = typeof(Standalone.CROpportunityRevision.vatTaxableTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? VatTaxableTotal { get; set; }
		#endregion

		#region TaxZoneID
		public abstract class taxZoneID : PX.Data.BQL.BqlString.Field<taxZoneID> { }

		/// <inheritdoc cref="PX.Objects.CA.CABankTran.TaxZoneID"/>
		[PXDBString(10, IsUnicode = true, BqlField = typeof(Standalone.CROpportunityRevision.taxZoneID))]
		[PXUIField(DisplayName = "Tax Zone")]
		[PXSelector(typeof(TaxZone.taxZoneID), DescriptionField = typeof(TaxZone.descr), Filterable = true)]
		public virtual String TaxZoneID { get; set; }
		#endregion

		#region TaxCalcMode
		public abstract class taxCalcMode : PX.Data.BQL.BqlString.Field<taxCalcMode> { }

		/// <inheritdoc cref="PX.Objects.CA.CABankTran.TaxCalcMode"/>
		[PXDBString(1, IsFixed = true, BqlField = typeof(Standalone.CROpportunityRevision.taxCalcMode))]
		[PXDefault(TaxCalculationMode.TaxSetting, typeof(Search<Location.cTaxCalcMode, Where<Location.bAccountID, Equal<Current<CROpportunity.bAccountID>>,
			And<Location.locationID, Equal<Current<CROpportunity.locationID>>>>>))]
		[TaxCalculationMode.List]
		[PXUIField(DisplayName = "Tax Calculation Mode")]
		public virtual string TaxCalcMode { get; set; }
		#endregion

		#region TaxRegistrationID
		public abstract class taxRegistrationID : PX.Data.BQL.BqlString.Field<taxRegistrationID> { }

		/// <inheritdoc cref="BAccount.TaxRegistrationID"/>
		[PXDBString(50, IsUnicode = true, BqlField = typeof(Standalone.CROpportunityRevision.taxRegistrationID))]
		[PXUIField(DisplayName = "Tax Registration ID")]
		[PXDefault(
			typeof(Search<Location.taxRegistrationID, 
				Where<Location.bAccountID, Equal<Current<CROpportunity.bAccountID>>,
					And<Location.locationID, Equal<Current<CROpportunity.locationID>>>>>), 
			PersistingCheck = PXPersistingCheck.Nothing)]
		[PXMassMergableField]
		public virtual String TaxRegistrationID { get; set; }
		#endregion

		#region ExternalTaxExemptionNumber
		public abstract class externalTaxExemptionNumber : PX.Data.BQL.BqlString.Field<externalTaxExemptionNumber> { }

		/// <inheritdoc cref="SOOrder.ExternalTaxExemptionNumber"/>
		[PXDBString(30, IsUnicode = true, BqlField = typeof(Standalone.CROpportunityRevision.externalTaxExemptionNumber))]
		[PXUIField(DisplayName = "Tax Exemption Number")]
		[PXDefault(
			typeof(Search<Location.cAvalaraExemptionNumber, 
				Where<Location.bAccountID, Equal<Current<CROpportunity.bAccountID>>,
					And<Location.locationID, Equal<Current<CROpportunity.locationID>>>>>), 
			PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual String ExternalTaxExemptionNumber { get; set; }
		#endregion

		#region AvalaraCustomerUsageType
		public abstract class avalaraCustomerUsageType : PX.Data.BQL.BqlString.Field<avalaraCustomerUsageType> { }

		/// <inheritdoc cref="ARInvoice.AvalaraCustomerUsageType"/>
		[PXDBString(1, IsFixed = true, BqlField = typeof(Standalone.CROpportunityRevision.avalaraCustomerUsageType))]
		[PXUIField(DisplayName = "Entity Usage Type")]
		[PXDefault(
			TXAvalaraCustomerUsageType.Default, 
			typeof(Search<Location.cAvalaraCustomerUsageType, 
				Where<Location.bAccountID, Equal<Current<CROpportunity.bAccountID>>,
					And<Location.locationID, Equal<Current<CROpportunity.locationID>>>>>))]
		[TX.TXAvalaraCustomerUsageType.List]
		public virtual String AvalaraCustomerUsageType { get; set; }
		#endregion

		#region NoteID
		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }

		/// <inheritdoc/>
		[PXSearchable(SM.SearchCategory.CR, Messages.OpportunitySearchTitle, new Type[] { typeof(opportunityID), typeof(bAccountID), typeof(BAccount.acctName) },
		   new Type[] { typeof(subject) },
		   MatchWithJoin = typeof(LeftJoin<BAccount, On<BAccount.bAccountID, Equal<CROpportunity.bAccountID>>>),
		   NumberFields = new Type[] { typeof(opportunityID) },
		   Line1Format = "{0}{1}{2}{3}{5}", Line1Fields = new Type[] { typeof(status), typeof(resolution), typeof(stageID), typeof(source), typeof(contactID), typeof(Contact.displayName) },
		   Line2Format = "{0}", Line2Fields = new Type[] { typeof(subject) }
		)]
		[PXNote(
			DescriptionField = typeof(opportunityID),
			Selector = typeof(opportunityID),
			ShowInReferenceSelector = true,
			BqlField = typeof(Standalone.CROpportunity.noteID))]
		public virtual Guid? NoteID { get; set; }
		#endregion

		#region QuoteNoteID
		public abstract class quoteNoteID : PX.Data.BQL.BqlGuid.Field<quoteNoteID> { }
		[PXExtraKey()]
		[PXDBGuid(true, BqlField = typeof(Standalone.CROpportunityRevision.noteID))]
		public virtual Guid? QuoteNoteID { get { return DefQuoteID; } }
		#endregion

		#region QuoteOpportunityID
		public abstract class quoteOpportunityID : PX.Data.BQL.BqlString.Field<quoteOpportunityID> { }
		[PXExtraKey()]
		[PXDBString(OpportunityIDLength, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC", BqlField = typeof(Standalone.CROpportunityRevision.opportunityID))]
		public virtual string QuoteOpportunityID { get { return OpportunityID; } }
		#endregion

		#region PrimaryQuoteID
		public abstract class primaryQuoteID : PX.Data.BQL.BqlGuid.Field<primaryQuoteID> { }
		[PXExtraKey()]
		[PXDBGuid(true, BqlField = typeof(Standalone.CRQuote.quoteID))]
		public virtual Guid? PrimaryQuoteID { get { return DefQuoteID; } }
		#endregion

		#region PrimaryQuoteNbr
		public abstract class quoteNbr : PX.Data.BQL.BqlString.Field<quoteNbr> { }
		protected String _QuoteNbr;
		[PXDBString(15, IsUnicode = true, BqlField = typeof(Standalone.CRQuote.quoteNbr))]
		[PXUIField(DisplayName = "Primary Quote Nbr.", Visible = false)]
		public virtual string PrimaryQuoteNbr { get; set; }
		#endregion

		#region PrimaryQuoteType
		public abstract class primaryQuoteType : PX.Data.BQL.BqlString.Field<primaryQuoteType> { }

		[PXDBString(1, IsFixed = true, BqlField = typeof(Standalone.CRQuote.quoteType))]
		[PXUIField(DisplayName = "Primary Quote Type", Visible = false)]
		[CRQuoteType]
		[PXDefault(CRQuoteTypeAttribute.Distribution, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual string PrimaryQuoteType { get; set; }
		#endregion

		#region Source
		public abstract class source : PX.Data.BQL.BqlString.Field<source> { }

		[PXDBString(1, IsFixed = true, BqlField = typeof(Standalone.CROpportunity.source))]
		[PXUIField(DisplayName = "Source", Visibility = PXUIVisibility.Visible, Visible = true)]
		[PXMassUpdatableField]
		[CRMSources]
		[PXFormula(typeof(
			CROpportunity.source.FromCurrent
				.When<CROpportunity.source.FromCurrent.IsNotNull>
			.Else<Use<Selector<CROpportunity.contactID, Contact.source>>.AsString>
		))]
		public virtual string Source { get; set; }
		#endregion

		#region ExternalRef
		public abstract class externalRef : PX.Data.BQL.BqlString.Field<externalRef> { }

		[PXDBString(255, IsFixed = true, BqlField = typeof(Standalone.CROpportunity.externalRef))]
		[PXUIField(DisplayName = "External Ref.")]
		public virtual string ExternalRef { get; set; }
		#endregion

		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }

		[PXDBTimestamp(BqlField = typeof(Standalone.CROpportunity.Tstamp))]
		public virtual Byte[] tstamp { get; set; }
		#endregion

		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }

		[PXDBCreatedByScreenID(BqlField = typeof(Standalone.CROpportunity.createdByScreenID))]
		public virtual String CreatedByScreenID { get; set; }
		#endregion

		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }

		[PXDBCreatedByID(BqlField = typeof(Standalone.CROpportunity.createdByID))]
		[PXUIField(DisplayName = "Created By")]
		public virtual Guid? CreatedByID { get; set; }
		#endregion

		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }

		[PXDBCreatedDateTime(BqlField = typeof(Standalone.CROpportunity.createdDateTime))]
		[PXUIField(DisplayName = "Date Created", Enabled = false)]
		public virtual DateTime? CreatedDateTime { get; set; }
		#endregion

		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }

		[PXDBLastModifiedByID(BqlField = typeof(Standalone.CROpportunity.lastModifiedByID))]
		[PXUIField(DisplayName = "Last Modified By")]
		public virtual Guid? LastModifiedByID { get; set; }
		#endregion

		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }

		[PXDBLastModifiedByScreenID(BqlField = typeof(Standalone.CROpportunity.lastModifiedByScreenID))]
		public virtual String LastModifiedByScreenID { get; set; }
		#endregion

		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }

		[PXDBLastModifiedDateTime(BqlField = typeof(Standalone.CROpportunity.lastModifiedDateTime))]
		[PXUIField(DisplayName = "Last Modified Date", Enabled = false)]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		#endregion

		#region Attributes
		public abstract class attributes : BqlAttributes.Field<attributes> { }

		[CRAttributesField(typeof(classID))]
		public virtual string[] Attributes { get; set; }
		#endregion

		#region DefQuoteID
		public abstract class defQuoteID : PX.Data.BQL.BqlGuid.Field<defQuoteID> { }

		[PXDBGuid(BqlField = typeof(Standalone.CROpportunity.defQuoteID))]
		public virtual Guid? DefQuoteID { get; set; }
		#endregion

		#region ProductCntr
		public abstract class productCntr : PX.Data.BQL.BqlInt.Field<productCntr> { }

		[PXDBInt(BqlField = typeof(Standalone.CROpportunityRevision.productCntr))]
		[PXDefault(0)]
		public virtual Int32? ProductCntr { get; set; }

		#endregion

		#region LineCntr
		public abstract class lineCntr : PX.Data.BQL.BqlInt.Field<lineCntr> { }

		[PXDBInt(BqlField = typeof(Standalone.CROpportunityRevision.lineCntr))]
		[PXDefault(0, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Int32? LineCntr { get; set; }
		#endregion

		#region RCreatedByID
		public abstract class rCreatedByID : PX.Data.BQL.BqlGuid.Field<rCreatedByID> { }
		[PXDBCreatedByID(BqlField = typeof(Standalone.CROpportunityRevision.createdByID))]
		public virtual Guid? RCreatedByID
		{
			get;
			set;
		}
		#endregion
		#region RCreatedByScreenID
		public abstract class rCreatedByScreenID : PX.Data.BQL.BqlString.Field<rCreatedByScreenID> { }
		[PXDBCreatedByScreenID(BqlField = typeof(Standalone.CROpportunityRevision.createdByScreenID))]
		public virtual String RCreatedByScreenID
		{
			get;
			set;
		}
		#endregion
		#region RCreatedDateTime
		public abstract class rCreatedDateTime : PX.Data.BQL.BqlDateTime.Field<rCreatedDateTime> { }
		[PXDBCreatedDateTime(BqlField = typeof(Standalone.CROpportunityRevision.createdDateTime))]
		public virtual DateTime? RCreatedDateTime
		{
			get;
			set;
		}
		#endregion
		#region RLastModifiedByID
		public abstract class rLastModifiedByID : PX.Data.BQL.BqlGuid.Field<rLastModifiedByID> { }
		[PXDBLastModifiedByID(BqlField = typeof(Standalone.CROpportunityRevision.lastModifiedByID))]
		public virtual Guid? RLastModifiedByID
		{
			get;
			set;
		}
		#endregion
		#region RLastModifiedByScreenID
		public abstract class rLastModifiedByScreenID : PX.Data.BQL.BqlString.Field<rLastModifiedByScreenID> { }
		[PXDBLastModifiedByScreenID(BqlField = typeof(Standalone.CROpportunityRevision.lastModifiedByScreenID))]
		public virtual String RLastModifiedByScreenID
		{
			get;
			set;
		}
		#endregion
		#region RLastModifiedDateTime
		public abstract class rLastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<rLastModifiedDateTime> { }
		[PXDBLastModifiedDateTime(BqlField = typeof(Standalone.CROpportunityRevision.lastModifiedDateTime))]
		public virtual DateTime? RLastModifiedDateTime
		{
			get;
			set;
		}
		#endregion

		#region CuryViewState
		[System.Obsolete]
		public abstract class curyViewState : PX.Data.BQL.BqlBool.Field<curyViewState> { }
		[PXBool]
		public virtual bool? CuryViewState
		{
			get;
			set;
		}
		#endregion

		#region LanguageID
		public abstract class languageID : PX.Data.BQL.BqlString.Field<languageID> { }

		/// <summary>
		/// The language in which the contact prefers to communicate.
		/// </summary>
		/// <value>
		/// By default, the system fills in the box with the locale specified for the contact's country.
		/// This field is displayed on the form only if there are multiple active locales
		/// configured on the <i>System Locales (SM200550)</i> form
		/// (corresponds to the <see cref="LocaleMaintenance"/> graph).
		/// </value>
		[PXDBString(10, IsUnicode = true, InputMask = "", BqlField = typeof(Standalone.CROpportunityRevision.languageID))]
		[PXUIField(DisplayName = "Language/Locale")]
		[PXSelector(typeof(
			Search<PX.SM.Locale.localeName,
			Where<PX.SM.Locale.isActive, Equal<True>>>),
			DescriptionField = typeof(PX.SM.Locale.description))]
		[ContacLanguageDefault(typeof(CRAddress.countryID))]
		public virtual String LanguageID { get; set; }
		#endregion

		#region SiteID
		public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
		[PXDBInt(BqlField = typeof(Standalone.CROpportunityRevision.siteID))]
		[PXUIField(DisplayName = "Warehouse", Visibility = PXUIVisibility.Visible)]
		[PXDimensionSelector(SiteAttribute.DimensionName, typeof(INSite.siteID), typeof(INSite.siteCD), DescriptionField = typeof(INSite.descr))]
		[PXRestrictor(typeof(Where<INSite.active, Equal<True>>), IN.Messages.InactiveWarehouse, typeof(INSite.siteCD))]
		[PXRestrictor(typeof(Where<INSite.siteID, NotEqual<SiteAttribute.transitSiteID>>), IN.Messages.TransitSiteIsNotAvailable)]
		[PXForeignReference(typeof(Field<siteID>.IsRelatedTo<INSite.siteID>))]
		[PXDefault(typeof(
			SelectFrom<Location>
			.Where<Location.locationID.IsEqual<CROpportunity.locationID.FromCurrent>
				.And<Location.bAccountID.IsEqual<CROpportunity.bAccountID.FromCurrent>>>
			.SearchFor<Location.cSiteID>),
			PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Default<locationID>))]
		public virtual Int32? SiteID { get; set; }
		#endregion
		#region CarrierID
		public abstract class carrierID : PX.Data.BQL.BqlString.Field<carrierID> { }
		[PXDBString(15, IsUnicode = true, InputMask = ">aaaaaaaaaaaaaaa", BqlField = typeof(Standalone.CROpportunityRevision.carrierID))]
		[PXUIField(DisplayName = "Ship Via")]
		[PXSelector(typeof(Search<Carrier.carrierID>),
			typeof(Carrier.carrierID), typeof(Carrier.description), typeof(Carrier.isExternal), typeof(Carrier.confirmationRequired),
			CacheGlobal = true,
			DescriptionField = typeof(Carrier.description))]
		[PXDefault(typeof(
			SelectFrom<Location>
			.Where<Location.locationID.IsEqual<CROpportunity.locationID.FromCurrent>
				.And<Location.bAccountID.IsEqual<CROpportunity.bAccountID.FromCurrent>>>
			.SearchFor<Location.cCarrierID>),
			PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Default<locationID>))]
		public virtual String CarrierID { get; set; }
		#endregion
		#region ShipTermsID
		public abstract class shipTermsID : PX.Data.BQL.BqlString.Field<shipTermsID> { }
		[PXDBString(10, IsUnicode = true, BqlField = typeof(Standalone.CROpportunityRevision.shipTermsID))]
		[PXUIField(DisplayName = "Shipping Terms")]
		[PXSelector(typeof(Search<ShipTerms.shipTermsID>), CacheGlobal = true, DescriptionField = typeof(ShipTerms.description))]
		[PXDefault(typeof(
			SelectFrom<Location>
			.Where<Location.locationID.IsEqual<CROpportunity.locationID.FromCurrent>
				.And<Location.bAccountID.IsEqual<CROpportunity.bAccountID.FromCurrent>>>
			.SearchFor<Location.cShipTermsID>),
			PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Default<locationID>))]
		public virtual String ShipTermsID { get; set; }
		#endregion
		#region ShipZoneID
		public abstract class shipZoneID : PX.Data.BQL.BqlString.Field<shipZoneID> { }
		[PXDBString(15, IsUnicode = true, InputMask = ">aaaaaaaaaaaaaaa", BqlField = typeof(Standalone.CROpportunityRevision.shipZoneID))]
		[PXUIField(DisplayName = "Shipping Zone")]
		[PXSelector(typeof(ShippingZone.zoneID), CacheGlobal = true, DescriptionField = typeof(ShippingZone.description))]
		[PXDefault(typeof(
			SelectFrom<Location>
			.Where<Location.locationID.IsEqual<CROpportunity.locationID.FromCurrent>
				.And<Location.bAccountID.IsEqual<CROpportunity.bAccountID.FromCurrent>>>
			.SearchFor<Location.cShipZoneID>),
			PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Default<locationID>))]
		public virtual String ShipZoneID { get; set; }
		#endregion
		#region FOBPointID
		public abstract class fOBPointID : PX.Data.BQL.BqlString.Field<fOBPointID> { }
		[PXDBString(15, IsUnicode = true, BqlField = typeof(Standalone.CROpportunityRevision.fOBPointID))]
		[PXUIField(DisplayName = "FOB Point")]
		[PXSelector(typeof(FOBPoint.fOBPointID), CacheGlobal = true, DescriptionField = typeof(FOBPoint.description))]
		[PXDefault(typeof(
			SelectFrom<Location>
			.Where<Location.locationID.IsEqual<CROpportunity.locationID.FromCurrent>
				.And<Location.bAccountID.IsEqual<CROpportunity.bAccountID.FromCurrent>>>
			.SearchFor<Location.cFOBPointID>),
			PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Default<locationID>))]
		public virtual String FOBPointID { get; set; }
		#endregion
		#region Resedential
		public abstract class resedential : PX.Data.BQL.BqlBool.Field<resedential> { }
		[PXDBBool(BqlField = typeof(Standalone.CROpportunityRevision.resedential))]
		[PXDefault(false, typeof(
			SelectFrom<Location>
			.Where<Location.locationID.IsEqual<CROpportunity.locationID.FromCurrent>
				.And<Location.bAccountID.IsEqual<CROpportunity.bAccountID.FromCurrent>>>
			.SearchFor<Location.cResedential>)
			, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Residential Delivery")]
		[PXFormula(typeof(Default<locationID>))]
		public virtual Boolean? Resedential { get; set; }
		#endregion
		#region SaturdayDelivery
		public abstract class saturdayDelivery : PX.Data.BQL.BqlBool.Field<saturdayDelivery> { }
		[PXDBBool(BqlField = typeof(Standalone.CROpportunityRevision.saturdayDelivery))]
		[PXDefault(false, typeof(
			SelectFrom<Location>
			.Where<Location.locationID.IsEqual<CROpportunity.locationID.FromCurrent>
				.And<Location.bAccountID.IsEqual<CROpportunity.bAccountID.FromCurrent>>>
			.SearchFor<Location.cSaturdayDelivery>)
			, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Saturday Delivery")]
		[PXFormula(typeof(Default<locationID>))]
		public virtual Boolean? SaturdayDelivery { get; set; }
		#endregion
		#region Insurance
		public abstract class insurance : PX.Data.BQL.BqlBool.Field<insurance> { }
		[PXDBBool(BqlField = typeof(Standalone.CROpportunityRevision.insurance))]
		[PXDefault(false, typeof(
			SelectFrom<Location>
			.Where<Location.locationID.IsEqual<CROpportunity.locationID.FromCurrent>
				.And<Location.bAccountID.IsEqual<CROpportunity.bAccountID.FromCurrent>>>
			.SearchFor<Location.cInsurance>)
			, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Insurance")]
		[PXFormula(typeof(Default<locationID>))]
		public virtual Boolean? Insurance { get; set; }
		#endregion
		#region ShipComplete
		public abstract class shipComplete : PX.Data.BQL.BqlString.Field<shipComplete> { }
		[PXDBString(1, IsFixed = true, BqlField = typeof(Standalone.CROpportunityRevision.shipComplete))]
		[PXDefault(SOShipComplete.CancelRemainder, typeof(
			SelectFrom<Location>
			.Where<Location.locationID.IsEqual<CROpportunity.locationID.FromCurrent>
				.And<Location.bAccountID.IsEqual<CROpportunity.bAccountID.FromCurrent>>>
			.SearchFor<Location.cShipComplete>)
			, PersistingCheck = PXPersistingCheck.Nothing)]
		[SOShipComplete.List()]
		[PXUIField(DisplayName = "Shipping Rule")]
		[PXFormula(typeof(Default<locationID>))]
		public virtual String ShipComplete { get; set; }
		#endregion
	}

	[Obsolete]
	public class Allowed<StringlistValue> : BqlFormulaEvaluator<StringlistValue>, ISwitch
		where StringlistValue : IBqlOperand
	{
		public override object Evaluate(PXCache cache, object item, Dictionary<Type, object> pars)
		{
			if (!cache.GetAttributesReadonly(item, OuterField.Name).Any(attr => attr is PXStringListAttribute)) return null;

			string val = (string)pars[typeof(StringlistValue)];
			PXStringState state = (PXStringState)cache.GetStateExt(item, OuterField.Name);
			return new List<string>(state.AllowedValues).Exists(str => string.CompareOrdinal(str, val) == 0) ? val : null;
		}

		public Type OuterField { get; set; }
	}
}
