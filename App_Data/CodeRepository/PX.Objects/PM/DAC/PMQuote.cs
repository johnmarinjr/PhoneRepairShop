using System.Collections.Generic;
using PX.Common;
using PX.Data.EP;
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
using PX.Objects.Common;
using PX.Objects.CR.Standalone;
using PX.Objects.PM;
using PX.Objects.GL;
using PX.Objects.CR;
using PX.Objects.CR.DAC.Standalone;
using PX.Objects.CT;
using PX.Objects.SO;

namespace PX.Objects.PM
{
	/// <summary>
	/// The projection of the <see cref="CR.Standalone.CRQuote"/>, <see cref="CROpportunityRevision"/> and <see cref="CR.Standalone.CROpportunity"/> classes, which
	/// contains the main properties of a project quote.
	/// The records of this type are created and edited through the Project Quotes (PM304500) form
	/// (which corresponds to the <see cref="PMQuoteMaint"/> graph).
	/// </summary>
	[SerializableAttribute()]
	[PXCacheName(CR.Messages.PMQuote)]
	[PXPrimaryGraph(typeof(PMQuoteMaint))]
	[CREmailContactsView(typeof(Select2<Contact,
		LeftJoin<BAccount, On<BAccount.bAccountID, Equal<Contact.bAccountID>>>,
		Where2<Where<Optional<PMQuote.bAccountID>, IsNull, And<Contact.contactID, Equal<Optional<PMQuote.contactID>>>>,
			Or2<Where<Optional<PMQuote.bAccountID>, IsNotNull, And<Contact.bAccountID, Equal<Optional<PMQuote.bAccountID>>>>,
				Or<Contact.contactType, Equal<ContactTypesAttribute.employee>>>>>))]
	[PXEMailSource]//NOTE: for assignment map
	[PXQuoteProjection(typeof(Select2<CR.Standalone.CRQuote,
		InnerJoin<CROpportunityRevision,
			On<CROpportunityRevision.noteID, Equal<CR.Standalone.CRQuote.quoteID>>,
		LeftJoin<CR.Standalone.CROpportunity,
			On<CR.Standalone.CROpportunity.opportunityID, Equal<CROpportunityRevision.opportunityID>>>>,
		Where<CR.Standalone.CRQuote.quoteType, Equal<CRQuoteTypeAttribute.project>>>))]
	[PXBreakInheritance]
	public partial class PMQuote : IBqlTable, IAssign, IPXSelectable
	{
		#region Selected
		public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }

		[PXBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Selected", Visibility = PXUIVisibility.Service)]
		public virtual bool? Selected { get; set; }
		#endregion

		#region QuoteID
		public abstract class quoteID : PX.Data.BQL.BqlGuid.Field<quoteID> { }
		[PXDBGuid(BqlField = typeof(CR.Standalone.CRQuote.quoteID))]
		[PXFormula(typeof(noteID))]
		public virtual Guid? QuoteID { get; set; }
		#endregion

		#region QuoteNbr
		public abstract class quoteNbr : PX.Data.BQL.BqlString.Field<quoteNbr> { }

		/// <summary>
		/// The reference number of the project quote.
		/// </summary>
		/// <value>
		/// The number is generated from the <see cref="Numbering">numbering sequence</see>,
		/// which is specified on the <see cref="PMSetup">Projects Preferences</see> (PM101000) form.
		/// </value>
		[AutoNumber(typeof(PMSetup.quoteNumberingID), typeof(AccessInfo.businessDate))]
		[PXDBString(15, IsKey = true, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC", BqlField = typeof(CR.Standalone.CRQuote.quoteNbr))]
		[PXSelector(typeof(Search2<PMQuote.quoteNbr,
					LeftJoin<BAccount, On<BAccount.bAccountID, Equal<PMQuote.bAccountID>>,
					LeftJoin<Contact, On<Contact.contactID, Equal<PMQuote.contactID>>>>,
				Where<PMQuote.quoteType, Equal<CRQuoteTypeAttribute.project>>,
				OrderBy<Desc<PMQuote.quoteNbr>>>),
			new[] {
				typeof(PMQuote.quoteNbr),
				typeof(PMQuote.status),
				typeof(PMQuote.subject),
				typeof(BAccount.acctCD),
				typeof(BAccount.acctName),
				typeof(PMQuote.documentDate),
				typeof(PMQuote.expirationDate)
			 },
			Filterable = true, DescriptionField = typeof(PMQuote.subject))]
		[PXUIField(DisplayName = "Quote Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
		[PXFieldDescription]
		public virtual String QuoteNbr { get; set; }
		#endregion

		#region QuoteType
		public abstract class quoteType : PX.Data.BQL.BqlString.Field<quoteType> { }

		/// <summary>
		/// The type of the quote.
		/// </summary>
		/// <value>
		/// The field can have one of the following values:
		/// <c>"D"</c>: Sales Quote,
		/// <c>"P"</c>: Project Quote
		/// </value>
		[PXDBString(1, IsFixed = true, BqlField = typeof(CR.Standalone.CRQuote.quoteType))]
		[PXUIField(DisplayName = "Type", Visible = false)]
		[PXMassUpdatableField]
		[CRQuoteType()]
		[PXDefault(CRQuoteTypeAttribute.Project)]
		public virtual string QuoteType { get; set; }
		#endregion

		#region OpportunityID
		public abstract class opportunityID : PX.Data.BQL.BqlString.Field<opportunityID> { }

		/// <summary>
		/// The reference number of the <see cref="CR.CROpportunity">opportunity</see> associated with the project quote.
		/// </summary>
		[PXDBString(CR.Standalone.CROpportunity.OpportunityIDLength, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC", BqlField = typeof(CROpportunityRevision.opportunityID))]
		[PXUIField(DisplayName = "Opportunity ID", Visibility = PXUIVisibility.SelectorVisible, FieldClass = FeaturesSet.customerModule.FieldClass)]
		[PXSelector(typeof(Search2<CR.CROpportunity.opportunityID,
				LeftJoin<BAccount, On<BAccount.bAccountID, Equal<CR.CROpportunity.bAccountID>>,
					LeftJoin<Contact, On<Contact.contactID, Equal<CR.CROpportunity.contactID>>>>,
				Where<True, Equal<True>>,
				OrderBy<Desc<CR.CROpportunity.opportunityID>>>),
			new[] {
				typeof(CR.CROpportunity.opportunityID),
				typeof(CR.CROpportunity.subject),
				typeof(CR.CROpportunity.status),
				typeof(CR.CROpportunity.stageID),
				typeof(CR.CROpportunity.classID),
				typeof(BAccount.acctName),
				typeof(Contact.displayName),
				typeof(CR.CROpportunity.externalRef),
				typeof(CR.CROpportunity.closeDate) },
			Filterable = true)]
		[PXFieldDescription]
		[PXRestrictor(typeof(Where<CR.CROpportunity.bAccountID, Equal<Current<bAccountID>>, Or<Current<bAccountID>, IsNull>>), Messages.OpportunityBAccount)]
		[PXRestrictor(typeof(Where<CR.CROpportunity.isActive.IsEqual<True>>), Messages.QuoteCannotBeLinkedToNotActiveOpportunity)]
		public virtual String OpportunityID { get; set; }
		#endregion

		#region IsActive
		public abstract class opportunityIsActive : PX.Data.BQL.BqlInt.Field<opportunityIsActive> { }

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the opportunity associated with the project quote is active.
		/// </summary>
		[PXDBBool(BqlField = typeof(CR.Standalone.CROpportunity.isActive))]
		[PXUIField(Visible = false, DisplayName = "Opportunity Is Active")]
		public virtual bool? OpportunityIsActive { get; set; }
		#endregion

		#region DefQuoteID
		public abstract class defQuoteID : PX.Data.BQL.BqlGuid.Field<defQuoteID> { }

		/// <inheritdoc cref="CR.Standalone.CROpportunity.DefQuoteID"/>
		[PXDBGuid(BqlField = typeof(CR.Standalone.CROpportunity.defQuoteID))]
		public virtual Guid? DefQuoteID { get; set; }
		#endregion

		#region IsPrimary
		public abstract class isPrimary : PX.Data.BQL.BqlBool.Field<isPrimary> { }

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the project quote is
		/// the primary quote of the opportunity associated with the project quote.
		/// </summary>
		[PXBool()]
		[PXUIField(DisplayName = "Primary", Enabled = false, FieldClass = FeaturesSet.customerModule.FieldClass)]
		[PXFormula(typeof(Switch<Case<Where<defQuoteID, IsNotNull, And<quoteID, Equal<defQuoteID>>>, True>, False>))]
		public virtual Boolean? IsPrimary
		{
			get;
			set;
		}
		#endregion

		#region ManualTotal
		public abstract class manualTotalEntry : PX.Data.BQL.BqlBool.Field<manualTotalEntry> { }

		/// <inheritdoc cref="CROpportunityRevision.ManualTotalEntry"/>
		[PXDBBool(BqlField = typeof(CROpportunityRevision.manualTotalEntry))]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Manual Amount")]
		public virtual Boolean? ManualTotalEntry { get; set; }
		#endregion

		#region TermsID
		public abstract class termsID : PX.Data.BQL.BqlString.Field<termsID> { }
		protected String _TermsID;
		/// <summary>
		/// The identifier of the default <see cref="Terms">terms</see>, 
		/// which are applied to the documents of the customer.
		/// </summary>
		[PXDBString(10, IsUnicode = true, BqlField = typeof(CR.Standalone.CROpportunityRevision.termsID))]
		[PXSelector(typeof(Search<Terms.termsID, Where<Terms.visibleTo, Equal<TermsVisibleTo.customer>, Or<Terms.visibleTo, Equal<TermsVisibleTo.all>>>>), DescriptionField = typeof(Terms.descr), CacheGlobal = true)]
		[PXDefault(
			typeof(Coalesce<
			Search<Customer.termsID, Where<Customer.bAccountID, Equal<Current<PMQuote.bAccountID>>>>,
			Search<CustomerClass.termsID, Where<CustomerClass.customerClassID, Equal<Current<Customer.customerClassID>>>>>),
			PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Default<PMQuote.bAccountID>))]
		[PXUIField(DisplayName = "Credit Terms")]
		public virtual String TermsID
		{
			get
			{
				return this._TermsID;
			}
			set
			{
				this._TermsID = value;
			}
		}
		#endregion

		#region DocumentDate
		public abstract class documentDate : PX.Data.BQL.BqlDateTime.Field<documentDate> { }

		/// <summary>
		/// The date of the project quote.
		/// </summary>
		/// <value>
		/// Defaults to the current <see cref="AccessInfo.BusinessDate">business date</see>.
		/// </value>
		[PXDBDate(BqlField = typeof(CROpportunityRevision.documentDate))]
		[PXDefault(typeof(AccessInfo.businessDate))]
		[PXMassUpdatableField]
		[PXUIField(DisplayName = "Date", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual DateTime? DocumentDate { get; set; }
		#endregion

		#region ExpirationDate
		public abstract class expirationDate : PX.Data.BQL.BqlDateTime.Field<expirationDate> { }

		/// <summary>
		/// The expiration date of the project quote.
		/// </summary>
		[PXDBDate(BqlField = typeof(CR.Standalone.CRQuote.expirationDate))]
		[PXMassUpdatableField]
		[PXUIField(DisplayName = "Expiration Date", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual DateTime? ExpirationDate { get; set; }
		#endregion

		#region Status
		public abstract class status : PX.Data.BQL.BqlString.Field<status> { }

		/// <summary>
		/// The status of the  project quote.
		/// </summary>
		/// <value>
		/// The field can have one of the following values:
		/// <c>"D"</c>: Draft,
		/// <c>"A"</c>: Prepared,
		/// <c>"S"</c>: Sent,
		/// <c>"P"</c>: Pending Approval,
		/// <c>"R"</c>: Rejected,
		/// <c>"T"</c>: Accepted,
		/// <c>"O"</c>: Converted,
		/// <c>"L"</c>: Declined,
		/// <c>"C"</c>: Closed,
		/// <c>"V"</c>: Approved
		/// </value>
		[PXDBString(1, IsFixed = true, BqlField = typeof(CR.Standalone.CRQuote.status))]
		[PXUIField(DisplayName = "Status", Visibility = PXUIVisibility.SelectorVisible)]
		[PXMassUpdatableField]
		[PMQuoteStatusAttribute()]
		[PXDefault(CRQuoteStatusAttribute.Draft)]
		public virtual string Status { get; set; }
		#endregion

		#region OpportunityAddressID
		public abstract class opportunityAddressID : PX.Data.BQL.BqlInt.Field<opportunityAddressID> { }
		protected Int32? _OpportunityAddressID;

		/// <inheritdoc cref="CROpportunityRevision.OpportunityAddressID"/>
		[PXDBInt(BqlField = typeof(CROpportunityRevision.opportunityAddressID))]
		[CROpportunityAddress(typeof(Select<Address,
			Where<True, Equal<False>>>))]
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

		/// <inheritdoc cref="CROpportunityRevision.OpportunityContactID"/>
		[PXDBInt(BqlField = typeof(CROpportunityRevision.opportunityContactID))]
		[CROpportunityContact(typeof(Select<Contact,
			Where<True, Equal<False>>>))]
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

		#region AllowOverrideContactAddress
		public abstract class allowOverrideContactAddress : PX.Data.BQL.BqlBool.Field<allowOverrideContactAddress> { }
		protected Boolean? _AllowOverrideContactAddress;

		/// <inheritdoc cref="CROpportunityRevision.AllowOverrideContactAddress"/>
		[PXDBBool(BqlField = typeof(CROpportunityRevision.allowOverrideContactAddress))]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Override Contact and Address")]
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

		private int? _BAccountID;

		/// <summary>
		/// The customer or prospective customer that is associated with the project quote
		/// and with the project created based on the project quote.
		/// </summary>
		[BAccount(bAccountTypes: new Type[]
			{
				typeof(BAccountType.prospectType),
				typeof(BAccountType.customerType),
				typeof(BAccountType.combinedType),
			},
			BqlField = typeof(CROpportunityRevision.bAccountID))]
		[PXDefault(typeof(Search<CROpportunityRevision.bAccountID, Where<CROpportunityRevision.noteID, Equal<Current<PMQuote.quoteID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Int32? BAccountID
		{
			get
			{
				return _BAccountID;
			}
			set
			{
				_BAccountID = value;
			}
		}
		#endregion

		#region LocationID
		public abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }

		/// <summary>
		/// The location of the <see cref="BAccountID">business account</see>.
		/// </summary>
		[LocationID(typeof(Where<CR.Location.bAccountID, Equal<Current<PMQuote.bAccountID>>, Or<Current<PMQuote.bAccountID>, IsNull>>),
			DisplayName = "Location",
			DescriptionField = typeof(CR.Location.descr),
			BqlField = typeof(CROpportunityRevision.locationID))]
		// add check for features
		[PXDefault(typeof(Search<CR.CROpportunity.locationID, Where<CR.CROpportunity.opportunityID, Equal<Current<PMQuote.opportunityID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Int32? LocationID { get; set; }
		#endregion

		#region ContactID
		public abstract class contactID : PX.Data.BQL.BqlInt.Field<contactID> { }

		protected Int32? _ContactID;

		/// <summary>
		/// The employee of the customer or prospective customer who is the primary contact person
		/// for the project quote and for the project created based on the project quote.
		/// </summary>
		[ContactRaw(typeof(PMQuote.bAccountID), WithContactDefaultingByBAccount = true, BqlField = typeof(CROpportunityRevision.contactID))]
		[PXRestrictor(typeof(Where2<Where2<
				Where<Contact.contactType, Equal<ContactTypesAttribute.person>,
					Or<Contact.contactType, Equal<ContactTypesAttribute.lead>>>,
				And<
					Where<BAccount.type, IsNull,
						Or<BAccount.type, Equal<BAccountType.customerType>,
							Or<BAccount.type, Equal<BAccountType.prospectType>,
								Or<BAccount.type, Equal<BAccountType.combinedType>>>>>>>,
			And<PMQuote.bAccountID.FromCurrent.IsNull.Or<BAccount.bAccountID.IsEqual<PMQuote.bAccountID.FromCurrent>>>>), 
			CR.Messages.ContactBAccountOpp, typeof(Contact.displayName), typeof(Contact.contactID))]
		[PXDBChildIdentity(typeof(Contact.contactID))]
		[PXDefault(typeof(Search<CR.CROpportunity.contactID, Where<CR.CROpportunity.opportunityID, Equal<Current<PMQuote.opportunityID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Int32? ContactID
		{
			get { return _ContactID; }
			set { _ContactID = value; }
		}
		#endregion

		#region ShipContactID
		public abstract class shipContactID : PX.Data.BQL.BqlInt.Field<shipContactID>
		{
		}
		protected Int32? _ShipContactID;

		/// <inheritdoc cref="CROpportunityRevision.ShipContactID"/>
		[PXDBInt(BqlField = typeof(CROpportunityRevision.shipContactID))]
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
		public abstract class shipAddressID : PX.Data.BQL.BqlInt.Field<shipAddressID>
		{
		}
		protected Int32? _ShipAddressID;

		/// <inheritdoc cref="CROpportunityRevision.ShipAddressID"/>
		[PXDBInt(BqlField = typeof(CROpportunityRevision.shipAddressID))]
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

		#region AllowOverrideShippingContactAddress
		public abstract class allowOverrideShippingContactAddress : PX.Data.BQL.BqlBool.Field<allowOverrideShippingContactAddress>
		{
		}
		protected Boolean? _AllowOverrideShippingContactAddress;

		/// <inheritdoc cref="CROpportunityRevision.AllowOverrideShippingContactAddress"/>
		[PXDBBool(BqlField = typeof(CR.Standalone.CROpportunityRevision.allowOverrideShippingContactAddress))]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Override")]
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

		#region Subject
		public abstract class subject : PX.Data.BQL.BqlString.Field<subject> { }

		/// <summary>
		/// The description of the project quote.
		/// </summary>
		[PXDBString(255, IsUnicode = true, BqlField = typeof(CR.Standalone.CRQuote.subject))]
		[PXDefault(PersistingCheck = PXPersistingCheck.NullOrBlank)]
		[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String Subject { get; set; }
		#endregion

		#region ParentBAccountID
		public abstract class parentBAccountID : PX.Data.BQL.BqlInt.Field<parentBAccountID> { }

		/// <inheritdoc cref="CROpportunityRevision.ParentBAccountID"/>
		[ParentBAccount(typeof(PMQuote.bAccountID), BqlField = typeof(CROpportunityRevision.parentBAccountID))]
		[PXFormula(typeof(Selector<CROpportunityRevision.bAccountID, BAccount.parentBAccountID>))]
		[PXDefault(typeof(Search<CROpportunityRevision.parentBAccountID, Where<CROpportunityRevision.noteID, Equal<Current<PMQuote.quoteID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Int32? ParentBAccountID { get; set; }
		#endregion

		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		protected Int32? _BranchID;

		/// <summary>
		/// The branch associated with the project quote.
		/// </summary>
		[Branch(IsDetail = false, TabOrder = 0, BqlField = typeof(CROpportunityRevision.branchID))]
		[PXFormula(typeof(Switch<
				Case<Where<PendingValue<CROpportunityRevision.branchID>, IsNotNull>, Null,
				Case<Where<CROpportunityRevision.locationID, IsNotNull,
					   And<Selector<CROpportunityRevision.locationID, CR.Location.cBranchID>, IsNotNull>>,
					Selector<CROpportunityRevision.locationID, CR.Location.cBranchID>,
				Case<Where<Current2<CROpportunityRevision.branchID>, IsNotNull>,
					Current2<CROpportunityRevision.branchID>>>>,
				Current<AccessInfo.branchID>>))]
		public virtual Int32? BranchID
		{
			get
			{
				return this._BranchID;
			}
			set
			{
				this._BranchID = value;
			}
		}
		#endregion

		#region ProjectID
		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }

		/// <inheritdoc cref="CROpportunityRevision.ProjectID"/>
		[PXDBInt(BqlField = typeof(CROpportunityRevision.projectID))]
		[PXDefault(0)]
		public virtual Int32? ProjectID { get; set; }
		#endregion

		#region QuoteProjectID
		public abstract class quoteProjectID : PX.Data.BQL.BqlInt.Field<quoteProjectID> { }

		/// <summary>
		/// The reference number of the <see cref="PMProject">project</see> that was created based on the project quote.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="PMProject.ContractID" /> field.
		/// </value>
		[PXUIField(DisplayName = "Project ID")]
		[PXDBInt(BqlField = typeof(CROpportunityRevision.quoteProjectID))]
		[PXDimensionSelector(ProjectAttribute.DimensionName, 
			typeof(Search<PMProject.contractID, Where<PMProject.baseType, Equal<PMProject.ProjectBaseType>>>), typeof(PMProject.contractCD) , DescriptionField = typeof(PMProject.description))]
		public virtual Int32? QuoteProjectID { get; set; }
		#endregion

		#region QuoteProjectCD
		public abstract class quoteProjectCD : PX.Data.BQL.BqlString.Field<quoteProjectCD> { }

		/// <summary>
		/// The reference number that will be assigned to a <see cref="PMProject">project</see> created based on the project quote.
		/// </summary>
		[PXDBString(BqlField = typeof(CROpportunityRevision.quoteProjectCD))]
		[PXUIField(DisplayName = "New Project ID")]
		[PXDimension(ProjectAttribute.DimensionName)]
		public virtual string QuoteProjectCD
		{
			get;
			set;
		}
		#endregion

		#region TemplateID
		public abstract class templateID : PX.Data.BQL.BqlInt.Field<templateID> { }

		/// <summary>
		/// The identifier of the <see cref="PMProject">project template</see> associated with the project quote.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="PMProject.ContractID" /> field.
		/// </value>
		[PXUIField(DisplayName = "Project Template", FieldClass = ProjectAttribute.DimensionName)]
		[PXDefault(typeof(Search<PMSetup.quoteTemplateID>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXDimensionSelector(ProjectAttribute.DimensionName,
				typeof(Search2<PMProject.contractID,
						LeftJoin<ContractBillingSchedule, On<ContractBillingSchedule.contractID, Equal<PMProject.contractID>>>,
							Where<PMProject.baseType, Equal<CTPRType.projectTemplate>, And<PMProject.isActive, Equal<True>>>>),
				typeof(PMProject.contractCD),
				typeof(PMProject.contractCD),
				typeof(PMProject.description),
				typeof(PMProject.budgetLevel),
				typeof(PMProject.billingID),
				typeof(ContractBillingSchedule.type),
				typeof(PMProject.ownerID),
				DescriptionField = typeof(PMProject.description))]
		[PXDBInt(BqlField = typeof(CROpportunityRevision.templateID))]
		public virtual Int32? TemplateID { get; set; }
		#endregion
		

		#region ProjectManager
		public abstract class projectManager : PX.Data.BQL.BqlInt.Field<projectManager> { }

		/// <summary>
		/// The identifier of the <see cref="EPEmployee">employee</see> who is responsible for the
		/// estimation of the project quote and who will be the project manager.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="EPEmployee.bAccountID" /> field.
		/// </value>
		[PXDBInt(BqlField = typeof(CROpportunityRevision.projectManager))]
		[EP.PXEPEmployeeSelector]
		[PXUIField(DisplayName = "Project Manager")]
		public virtual Int32? ProjectManager
		{
			get;
			set;
		}
		#endregion

		#region ExternalRef
		public abstract class externalRef : PX.Data.BQL.BqlString.Field<externalRef> { }

		/// <summary>
		/// The external reference number of the project quote.
		/// </summary>
		[PXDBString(255, IsFixed = true, BqlField = typeof(CROpportunityRevision.externalRef))]
		[PXUIField(DisplayName = "External Ref.")]
		public virtual string ExternalRef { get; set; }
		#endregion
				
		#region CampaignSourceID
		public abstract class campaignSourceID : PX.Data.BQL.BqlString.Field<campaignSourceID> { }

		/// <inheritdoc cref="CROpportunityRevision.CampaignSourceID"/>
		[PXDBString(10, IsUnicode = true, BqlField = typeof(CROpportunityRevision.campaignSourceID))]
		[PXUIField(DisplayName = "Source Campaign")]
		[PXSelector(typeof(Search3<CR.CRCampaign.campaignID, OrderBy<Desc<CR.CRCampaign.campaignID>>>),
			DescriptionField = typeof(CR.CRCampaign.campaignName), Filterable = true)]
		[PXDefault(typeof(Search<CROpportunityRevision.campaignSourceID, Where<CROpportunityRevision.noteID, Equal<Current<PMQuote.quoteID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual String CampaignSourceID { get; set; }
		#endregion

		#region WorkgroupID
		public abstract class workgroupID : PX.Data.BQL.BqlInt.Field<workgroupID> { }

		/// <inheritdoc cref="CROpportunityRevision.WorkgroupID"/>
		[PXDBInt(BqlField = typeof(CROpportunityRevision.workgroupID))]
		[PXCompanyTreeSelector]
		[PXUIField(DisplayName = "Workgroup")]
		[PXMassUpdatableField]
		public virtual int? WorkgroupID { get; set; }
		#endregion

		#region OwnerID
		public abstract class ownerID : PX.Data.BQL.BqlInt.Field<ownerID> { }

		/// <inheritdoc cref="CROpportunityRevision.OwnerID"/>
		[Owner(typeof(CROpportunityRevision.workgroupID), BqlField = typeof(CROpportunityRevision.ownerID))]
		[PXMassUpdatableField]
		public virtual int? OwnerID { get; set; }
		#endregion

		#region Approved
		public abstract class approved : PX.Data.BQL.BqlBool.Field<approved> { }

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the document has been approved.
		/// </summary>
		[PXDBBool(BqlField = typeof(CROpportunityRevision.approved))]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(Visible = false)]
		public virtual Boolean? Approved { get; set; }
		#endregion
		#region Rejected

		public abstract class rejected : PX.Data.BQL.BqlBool.Field<rejected> { }

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the document has been rejected.
		/// </summary>
		[PXDBBool(BqlField = typeof(CROpportunityRevision.rejected))]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(Visible = false)]
		public virtual Boolean? Rejected { get; set; }
		#endregion
		#region SubmitCancelled
		public abstract class submitCancelled : PX.Data.BQL.BqlBool.Field<submitCancelled> { }

		/// <summary>
		/// Specifies (if set to <see langword="true" />)
		/// that the submission of the document was canceled due to errors.
		/// This is a service field. 
		/// </summary>
		[PXBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual bool? SubmitCancelled { get; set; }
		#endregion

		#region IsSetupApprovalRequired
		public abstract class isSetupApprovalRequired : PX.Data.BQL.BqlBool.Field<isSetupApprovalRequired> { }

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the approval workflow is enabled for the project quotes.
		/// </summary>
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Switch<Case<Where<Current<PMSetup.quoteApprovalMapID>, IsNotNull>, True>, False>))]
		[PXUIField(DisplayName = "Approvable Setup", Visible = false, Enabled = false)]
		public virtual bool? IsSetupApprovalRequired { get; set; }
		#endregion

		#region IsDisabled
		public abstract class isDisabled : PX.Data.BQL.BqlBool.Field<isDisabled> { }

		/// <summary>
		/// A service field, which is used to configure the availability of the project quote fields.
		/// </summary>
		[PXBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Disabled", Visible = false)]
		public virtual bool? IsDisabled =>
			this.Status == CRQuoteStatusAttribute.PendingApproval ||
			this.Status == CRQuoteStatusAttribute.Approved ||
			this.Status == CRQuoteStatusAttribute.QuoteApproved ||
			this.Status == CRQuoteStatusAttribute.Rejected ||
			this.Status == CRQuoteStatusAttribute.Sent;

		#endregion

		#region CuryID
		public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }

		/// <summary>
		/// The identifier of the project quote <see cref="Currency">currency</see>.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="Currency.CuryID"/> field.
		/// </value>
		[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL", BqlField = typeof(CROpportunityRevision.curyID))]
		[PXDefault(typeof(Search<CRSetup.defaultCuryID>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXSelector(typeof(Currency.curyID))]
		[PXUIField(DisplayName = "Currency", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String CuryID { get; set; }
		#endregion

		#region CuryInfoID
		public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }

		/// <summary>
		/// The identifier of the <see cref="CurrencyInfo">CurrencyInfo</see> record associated with the project quote.
		/// </summary>
		[PXDBLong(BqlField = typeof(CROpportunityRevision.curyInfoID))]
		[CurrencyInfo]
		public virtual Int64? CuryInfoID { get; set; }
		#endregion

		#region ExtPriceTotal
		public abstract class extPriceTotal : PX.Data.BQL.BqlDecimal.Field<extPriceTotal> { }

		/// <inheritdoc cref="CROpportunityRevision.ExtPriceTotal"/>
		[PXDBDecimal(4, BqlField = typeof(CROpportunityRevision.extPriceTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? ExtPriceTotal { get; set; }
		#endregion

		#region CuryExtPriceTotal
		public abstract class curyExtPriceTotal : PX.Data.BQL.BqlDecimal.Field<curyExtPriceTotal> { }

		/// <inheritdoc cref="CROpportunityRevision.CuryExtPriceTotal"/>
		[PXUIField(DisplayName = "Subtotal", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXDBCurrency(typeof(curyInfoID), typeof(extPriceTotal), BqlField = typeof(CROpportunityRevision.curyExtPriceTotal))]
		public virtual Decimal? CuryExtPriceTotal { get; set; }
		#endregion

		#region LineTotal
		public abstract class lineTotal : PX.Data.BQL.BqlDecimal.Field<lineTotal> { }

		/// <inheritdoc cref="CROpportunityRevision.LineTotal"/>
		[PXDBDecimal(4, BqlField = typeof(CROpportunityRevision.lineTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? LineTotal { get; set; }
		#endregion

		#region CuryLineTotal
		public abstract class curyLineTotal : PX.Data.BQL.BqlDecimal.Field<curyLineTotal> { }

		/// <inheritdoc cref="CROpportunityRevision.CuryLineTotal"/>
		[PXDBCurrency(typeof(curyInfoID), typeof(lineTotal), BqlField = typeof(CROpportunityRevision.curyLineTotal))]
		[PXUIField(DisplayName = "Detail Total", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryLineTotal { get; set; }
		#endregion

		#region CostTotal
		public abstract class costTotal : PX.Data.BQL.BqlDecimal.Field<costTotal> { }

		/// <summary>
		/// The total estimated cost of the project quote in the base currency of the tenant.
		/// </summary>
		[PXDBDecimal(4, BqlField = typeof(CROpportunityRevision.costTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CostTotal { get; set; }
		#endregion

		#region CuryCostTotal
		public abstract class curyCostTotal : PX.Data.BQL.BqlDecimal.Field<curyCostTotal> { }

		/// <summary>
		/// The total estimated cost of the project quote.
		/// </summary>
		[PXDBCurrency(typeof(curyInfoID), typeof(costTotal), BqlField = typeof(CROpportunityRevision.curyCostTotal))]
		[PXUIField(DisplayName = "Total Cost", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryCostTotal { get; set; }
		#endregion
			

		#region GrossMarginAmount
		public abstract class grossMarginAmount : PX.Data.BQL.BqlDecimal.Field<grossMarginAmount> { }

		/// <summary>
		/// The estimated gross margin of the project quote in the base currency of the tenant.
		/// </summary>
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Gross Margin")]
		public virtual Decimal? GrossMarginAmount
		{
			[PXDependsOnFields(typeof(amount), typeof(costTotal))]
			get
			{
				return Amount - CostTotal;
			}

		}
		#endregion

		#region CuryGrossMarginAmount
		public abstract class curyGrossMarginAmount : PX.Data.BQL.BqlDecimal.Field<curyGrossMarginAmount> { }

		/// <summary>
		/// The estimated gross margin of the project quote.
		/// </summary>
		/// <value>
		/// Calculated as the difference between <see cref="CuryAmount">Total Sales</see> and <see cref="CuryCostTotal">Total Cost</see>.
		/// </value>
		[PXCurrency(typeof(curyInfoID), typeof(grossMarginAmount))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Gross Margin")]
		public virtual Decimal? CuryGrossMarginAmount
		{
			[PXDependsOnFields(typeof(curyAmount), typeof(curyCostTotal))]
			get
			{ 
				return CuryAmount - CuryCostTotal;
			}

		}
		#endregion
		
		#region GrossMarginPct
		public abstract class grossMarginPct : PX.Data.BQL.BqlDecimal.Field<grossMarginPct> { }

		/// <summary>
		/// The percentage of the estimated gross margin of the project quote.
		/// </summary>
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Gross Margin %")]
		public virtual Decimal? GrossMarginPct
		{
			[PXDependsOnFields(typeof(amount), typeof(costTotal))]
			get
			{
				if (Amount != 0)
				{
					return 100 * (Amount - CostTotal) / Amount;
				}
				else
					return 0;
			}
		}
		#endregion

		#region QuoteTotal
		public abstract class quoteTotal : PX.Data.BQL.BqlDecimal.Field<quoteTotal> { }

		/// <summary>
		/// The overall total of the project quote in the base currency of the tenant.
		/// </summary>
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Quote Total")]
		public virtual Decimal? QuoteTotal
		{
			[PXDependsOnFields(typeof(amount), typeof(taxTotal))]
			get
			{
				return Amount + TaxTotal;
			}

		}
		#endregion

		#region CuryQuoteTotal
		public abstract class curyQuoteTotal : PX.Data.BQL.BqlDecimal.Field<curyQuoteTotal> { }

		/// <summary>
		/// The overall total of the project quote.
		/// </summary>
		/// <value>
		/// Calculated as the sum of the <see cref="CuryAmount">Total Sales</see> and <see cref="CuryTaxTotal">Tax Total</see> amounts.
		/// </value>
		[PXCurrency(typeof(curyInfoID), typeof(quoteTotal))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Quote Total")]
		public virtual Decimal? CuryQuoteTotal
		{
			[PXDependsOnFields(typeof(curyAmount), typeof(curyTaxTotal))]
			get
			{
				return CuryAmount + CuryTaxTotal;
			}

		}
		#endregion

		#region LineDiscountTotal
		public abstract class lineDiscountTotal : PX.Data.BQL.BqlDecimal.Field<lineDiscountTotal> { }

		/// <inheritdoc cref="CROpportunityRevision.LineDiscountTotal"/>
		[PXDBDecimal(4, BqlField = typeof(CROpportunityRevision.lineDiscountTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? LineDiscountTotal { get; set; }
		#endregion

		#region CuryLineDiscountTotal
		public abstract class curyLineDiscountTotal : PX.Data.BQL.BqlDecimal.Field<curyLineDiscountTotal> { }

		/// <inheritdoc cref="CROpportunityRevision.CuryLineDiscountTotal"/>
		[PXDBCurrency(typeof(curyInfoID), typeof(lineDiscountTotal), BqlField = typeof(CROpportunityRevision.curyLineDiscountTotal))]
		[PXUIField(DisplayName = "Discount", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryLineDiscountTotal { get; set; }
		#endregion

		#region LineDocDiscountTotal
		public abstract class lineDocDiscountTotal : PX.Data.BQL.BqlDecimal.Field<lineDocDiscountTotal> { }

		/// <inheritdoc cref="CROpportunityRevision.LineDocDiscountTotal"/>
		[PXDBDecimal(4, BqlField = typeof(CROpportunityRevision.lineDocDiscountTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? LineDocDiscountTotal { get; set; }
		#endregion

		#region CuryLineDocDiscountTotal
		public abstract class curyLineDocDiscountTotal : PX.Data.BQL.BqlDecimal.Field<curyLineDocDiscountTotal> { }

		/// <inheritdoc cref="CROpportunityRevision.CuryLineDocDiscountTotal"/>
		[PXDBCurrency(typeof(curyInfoID), typeof(lineDocDiscountTotal), BqlField = typeof(CROpportunityRevision.curyLineDocDiscountTotal))]
		[PXUIField(Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryLineDocDiscountTotal { get; set; }
		#endregion

		#region TextForProductsGrid
		public abstract class textForProductsGrid : PX.Data.BQL.BqlString.Field<textForProductsGrid> { }

		[PXUIField(DisplayName = "  ", Enabled = false)]
		[PXString()]
		public virtual String TextForProductsGrid
		{
			get
			{
				return String.Format(CR.Messages.QuoteGridProductText, CuryExtPriceTotal.ToString(), CuryLineDiscountTotal.ToString());
			}
		}
		#endregion

		#region IsTaxValid
		public abstract class isTaxValid : PX.Data.BQL.BqlBool.Field<isTaxValid> { }

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the amount of tax calculated with the external tax engine, such as Avalara, is up to date.
		/// If the value of this field is <see langword="false"/>, the document was updated since last synchronization with the tax engine.
		/// Taxes might need recalculation.
		/// </summary>
		[PXDBBool(BqlField = typeof(CROpportunityRevision.isTaxValid))]
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

		/// <summary>
		/// The total tax of the project quote in the base currency of the tenant.
		/// </summary>
		[PXDBDecimal(4, BqlField = typeof(CROpportunityRevision.taxTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? TaxTotal { get; set; }
		#endregion

		#region CuryTaxTotal
		public abstract class curyTaxTotal : PX.Data.BQL.BqlDecimal.Field<curyTaxTotal> { }

		/// <summary>
		/// The total tax of the project quote in the project quote currency.
		/// </summary>
		[PXDBCurrency(typeof(CROpportunityRevision.curyInfoID), typeof(CROpportunityRevision.taxTotal), BqlField = typeof(CROpportunityRevision.curyTaxTotal))]
		[PXUIField(DisplayName = "Tax Total", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryTaxTotal { get; set; }
		#endregion

		#region Amount
		public abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }

		private decimal? _amount;

		/// <summary>
		/// The total estimated sale of the project quote in the base currency of the tenant.
		/// </summary>
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXDBBaseCury(BqlField = typeof(CROpportunityRevision.amount))]
		public virtual Decimal? Amount
		{
			get { return _amount; }
			set { _amount = value; }
		}

		#endregion

		#region CuryAmount
		public abstract class curyAmount : PX.Data.BQL.BqlDecimal.Field<curyAmount> { }

		private decimal? _curyAmount;

		/// <summary>
		/// The total estimated sale of the project quote in the project quote currency.
		/// </summary>
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXDBCurrency(typeof(curyInfoID), typeof(amount), BqlField = typeof(CROpportunityRevision.curyAmount))]
		[PXFormula(typeof(Switch<Case<Where<manualTotalEntry, Equal<True>>, curyAmount>, curyLineTotal>))]
		[PXUIField(DisplayName = "Total Sales", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual Decimal? CuryAmount
		{
			get { return _curyAmount; }
			set { _curyAmount = value; }
		}

		#endregion

		#region DiscTot
		public abstract class discTot : PX.Data.BQL.BqlDecimal.Field<discTot> { }

		/// <inheritdoc cref="CROpportunityRevision.DiscTot"/>
		[PXDBBaseCury(BqlField = typeof(CROpportunityRevision.discTot))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? DiscTot { get; set; }
		#endregion

		#region CuryDiscTot
		public abstract class curyDiscTot : PX.Data.BQL.BqlDecimal.Field<curyDiscTot> { }

		/// <inheritdoc cref="CROpportunityRevision.CuryDiscTot"/>
		[PXDBCurrency(typeof(CROpportunityRevision.curyInfoID), typeof(CROpportunityRevision.discTot), BqlField = typeof(CROpportunityRevision.curyDiscTot))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Discount")]
		[PXFormula(typeof(Switch<Case<Where<manualTotalEntry, Equal<True>>, curyDiscTot>, curyLineDocDiscountTotal>))]
		public virtual Decimal? CuryDiscTot { get; set; }
		#endregion

		#region CuryProductsAmount
		public abstract class curyProductsAmount : PX.Data.BQL.BqlDecimal.Field<curyProductsAmount> { }

		private decimal? _CuryProductsAmount;

		/// <inheritdoc cref="CROpportunityRevision.CuryProductsAmount"/>
		[PXDBCurrency(typeof(CROpportunityRevision.curyInfoID), typeof(CROpportunityRevision.productsAmount), BqlField = typeof(CROpportunityRevision.curyProductsAmount))]
		[PXUIField(DisplayName = "Total", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryProductsAmount
		{
			set { _CuryProductsAmount = value; }
			get { return _CuryProductsAmount; }
		}
		#endregion

		#region ProductsAmount
		public abstract class productsAmount : PX.Data.BQL.BqlDecimal.Field<productsAmount> { }

		private decimal? _ProductsAmount;

		/// <inheritdoc cref="CROpportunityRevision.ProductsAmount"/>
		[PXDBDecimal(4, BqlField = typeof(CROpportunityRevision.productsAmount))]
		public virtual Decimal? ProductsAmount
		{
			set { _ProductsAmount = value; }
			get
			{
				return _ProductsAmount;
			}
		}
		#endregion

		#region CuryWgtAmount
		public abstract class curyWgtAmount : PX.Data.BQL.BqlDecimal.Field<curyWgtAmount> { }

		[PXDecimal()]
		[PXUIField(DisplayName = "Wgt. Total", Enabled = false)]
		public virtual Decimal? CuryWgtAmount { get; set; }
		#endregion

		#region CuryVatExemptTotal
		public abstract class curyVatExemptTotal : PX.Data.BQL.BqlDecimal.Field<curyVatExemptTotal> { }

		/// <inheritdoc cref="CROpportunityRevision.CuryVatExemptTotal"/>
		[PXDBCurrency(typeof(CROpportunityRevision.curyInfoID), typeof(CROpportunityRevision.vatExemptTotal), BqlField = typeof(CROpportunityRevision.curyVatExemptTotal))]
		[PXUIField(DisplayName = "VAT Exempt Total", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryVatExemptTotal { get; set; }
		#endregion

		#region VatExemptTaxTotal
		public abstract class vatExemptTotal : PX.Data.BQL.BqlDecimal.Field<vatExemptTotal> { }

		/// <inheritdoc cref="CROpportunityRevision.VatExemptTotal"/>
		[PXDBDecimal(4, BqlField = typeof(CROpportunityRevision.vatExemptTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? VatExemptTotal { get; set; }
		#endregion

		#region CuryVatTaxableTotal
		public abstract class curyVatTaxableTotal : PX.Data.BQL.BqlDecimal.Field<curyVatTaxableTotal> { }

		/// <inheritdoc cref="CROpportunityRevision.CuryVatTaxableTotal"/>
		[PXDBCurrency(typeof(CROpportunityRevision.curyInfoID), typeof(CROpportunityRevision.vatTaxableTotal), BqlField = typeof(CROpportunityRevision.curyVatTaxableTotal))]
		[PXUIField(DisplayName = "VAT Taxable Total", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? CuryVatTaxableTotal { get; set; }
		#endregion

		#region VatTaxableTotal
		public abstract class vatTaxableTotal : PX.Data.BQL.BqlDecimal.Field<vatTaxableTotal> { }

		/// <inheritdoc cref="CROpportunityRevision.VatTaxableTotal"/>
		[PXDBDecimal(4, BqlField = typeof(CROpportunityRevision.vatTaxableTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? VatTaxableTotal { get; set; }
		#endregion

		#region TaxZoneID
		public abstract class taxZoneID : PX.Data.BQL.BqlString.Field<taxZoneID> { }

		/// <summary>
		/// The tax zone.
		/// </summary>
		[PXDBString(10, IsUnicode = true, BqlField = typeof(CROpportunityRevision.taxZoneID))]
		[PXUIField(DisplayName = "Tax Zone")]
		[PXSelector(typeof(TaxZone.taxZoneID), DescriptionField = typeof(TaxZone.descr), Filterable = true)]
		[PXFormula(typeof(Default<PMQuote.branchID>))]
		[PXFormula(typeof(Default<PMQuote.locationID>))]
		[PXDefault(typeof(Search<CROpportunityRevision.taxZoneID, Where<CROpportunityRevision.noteID, Equal<Current<PMQuote.quoteID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual String TaxZoneID { get; set; }
		#endregion

		#region TaxCalcMode
		public abstract class taxCalcMode : PX.Data.BQL.BqlString.Field<taxCalcMode> { }

		/// <inheritdoc cref="CROpportunityRevision.TaxCalcMode"/>
		[PXDBString(1, IsFixed = true, BqlField = typeof(CROpportunityRevision.taxCalcMode))]
		[PXDefault(TaxCalculationMode.TaxSetting, typeof(Search<CROpportunityRevision.taxCalcMode, Where<CROpportunityRevision.opportunityID, Equal<Current<PMQuote.opportunityID>>>>))]
		[TaxCalculationMode.List]
		[PXUIField(DisplayName = "Tax Calculation Mode")]
		public virtual string TaxCalcMode { get; set; }
		#endregion

		#region TaxRegistrationID
		public abstract class taxRegistrationID : PX.Data.BQL.BqlString.Field<taxRegistrationID> { }

		/// <inheritdoc cref="CROpportunityRevision.TaxRegistrationID"/>
		[PXDBString(50, IsUnicode = true, BqlField = typeof(CROpportunityRevision.taxRegistrationID))]
		[PXUIField(DisplayName = "Tax Registration ID")]
		[PXDefault(
			typeof(Search<CR.CROpportunity.taxRegistrationID,
				Where<CR.CROpportunity.opportunityID, Equal<Current<PMQuote.opportunityID>>>>),
			PersistingCheck = PXPersistingCheck.Nothing)]
		[PXMassMergableField]
		[PXPersonalDataField]
		public virtual String TaxRegistrationID { get; set; }
		#endregion

		#region ExternalTaxExemptionNumber
		public abstract class externalTaxExemptionNumber : PX.Data.BQL.BqlString.Field<externalTaxExemptionNumber> { }

		/// <inheritdoc cref="CROpportunityRevision.ExternalTaxExemptionNumber"/>
		[PXDBString(30, IsUnicode = true, BqlField = typeof(CROpportunityRevision.externalTaxExemptionNumber))]
		[PXUIField(DisplayName = "Tax Exemption Number")]
		[PXDefault(
			typeof(Search<CR.CROpportunity.externalTaxExemptionNumber,
				Where<CR.CROpportunity.opportunityID, Equal<Current<PMQuote.opportunityID>>>>),
			PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual String ExternalTaxExemptionNumber { get; set; }
		#endregion

		#region AvalaraCustomerUsageType
		public abstract class avalaraCustomerUsageType : PX.Data.BQL.BqlString.Field<avalaraCustomerUsageType> { }

		/// <inheritdoc cref="CROpportunityRevision.AvalaraCustomerUsageType"/>
		[PXDBString(1, IsFixed = true, BqlField = typeof(CROpportunityRevision.avalaraCustomerUsageType))]
		[PXUIField(DisplayName = "Entity Usage Type")]
		[PXDefault(
			TXAvalaraCustomerUsageType.Default, 
			typeof(Search<CR.CROpportunity.avalaraCustomerUsageType, 
				Where<CR.CROpportunity.opportunityID, Equal<Current<PMQuote.opportunityID>>>>))]
		[TX.TXAvalaraCustomerUsageType.List]
		public virtual String AvalaraCustomerUsageType { get; set; }
		#endregion

		#region NoteID
		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }

		[PXSearchable(SM.SearchCategory.PM, CR.Messages.ProjectQuotesSearchTitle, new Type[] { typeof(quoteNbr), typeof(bAccountID), typeof(BAccount.acctName) },
			new Type[] { typeof(subject) },
			NumberFields = new Type[] { typeof(quoteNbr) },
			Line1Format = "{0:d}{1}{2}", Line1Fields = new Type[] { typeof(documentDate), typeof(status), typeof(externalRef) },
			Line2Format = "{0}", Line2Fields = new Type[] { typeof(subject) }
		)]
		[PXNote(
			DescriptionField = typeof(quoteNbr),
			Selector = typeof(quoteNbr),
			BqlField = typeof(CR.Standalone.CRQuote.noteID),
			ShowInReferenceSelector = true)]
		public virtual Guid? NoteID { get; set; }
		#endregion

		#region RNoteID
		public abstract class rNoteID : PX.Data.BQL.BqlGuid.Field<rNoteID> { }
		[PXExtraKey]
		[PXDBGuid(BqlField = typeof(CR.Standalone.CROpportunityRevision.noteID))]
		public virtual Guid? RNoteID { get { return QuoteID; } }
		#endregion

		#region Attributes
		public abstract class attributes : BqlAttributes.Field<attributes> { }

		/// <summary>
		/// Provides the values of attributes associated with the project quote.
		/// The field is reserved for internal use.
		/// </summary>
		[CRAttributesField(typeof(PMProject.classID), typeof(quoteID))]
		public virtual string[] Attributes { get; set; }

		#region ClassID
		public abstract class classID : PX.Data.BQL.BqlString.Field<classID> { }
		/// <summary>
		/// The class ID for the attributes. The field always returns the current <see cref="GroupTypes.Project"/>.
		/// </summary>
		[PXString(20)]
		public virtual string ClassID
		{
			get { return GroupTypes.Project; }
		}

		#endregion

		#endregion

		#region ProductCntr
		public abstract class productCntr : PX.Data.BQL.BqlInt.Field<productCntr> { }

		/// <inheritdoc cref="CROpportunityRevision.ProductCntr"/>
		[PXDBInt(BqlField = typeof(CROpportunityRevision.productCntr))]
		[PXDefault(0)]
		public virtual Int32? ProductCntr { get; set; }

		#endregion

		#region LineCntr
		public abstract class lineCntr : PX.Data.BQL.BqlInt.Field<lineCntr> { }

		/// <inheritdoc cref="CROpportunityRevision.LineCntr"/>
		[PXDBInt(BqlField = typeof(CROpportunityRevision.lineCntr))]
		[PXDefault(0, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Int32? LineCntr { get; set; }
		#endregion

		#region RefOpportunityID
		public abstract class refOpportunityID : PX.Data.BQL.BqlString.Field<refOpportunityID> { }

		/// <inheritdoc cref="CR.Standalone.CROpportunity.OpportunityID"/>
		[PXDBString(CR.Standalone.CROpportunity.OpportunityIDLength, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC", BqlField = typeof(CR.Standalone.CROpportunity.opportunityID))]
		[PXExtraKey()]
		public virtual String RefOpportunityID { get { return OpportunityID; } }
		#endregion

		#region FormCaptionDescription
		[PXString]
		[PXFormula(typeof(Selector<bAccountID, BAccount.acctName>))]
		public string FormCaptionDescription { get; set; }
		#endregion

		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }

		[PXDBTimestamp(BqlField = typeof(CR.Standalone.CRQuote.Tstamp))]
		public virtual Byte[] tstamp { get; set; }
		#endregion

		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }

		[PXDBCreatedByScreenID(BqlField = typeof(CR.Standalone.CRQuote.createdByScreenID))]
		public virtual String CreatedByScreenID { get; set; }
		#endregion

		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }

		[PXDBCreatedByID(BqlField = typeof(CR.Standalone.CRQuote.createdByID))]
		[PXUIField(DisplayName = "Created By")]
		public virtual Guid? CreatedByID { get; set; }
		#endregion

		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }

		[PXDBCreatedDateTime(BqlField = typeof(CR.Standalone.CRQuote.createdDateTime))]
		[PXUIField(DisplayName = "Date Created", Enabled = false)]
		public virtual DateTime? CreatedDateTime { get; set; }
		#endregion

		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }

		[PXDBLastModifiedByID(BqlField = typeof(CR.Standalone.CRQuote.lastModifiedByID))]
		[PXUIField(DisplayName = "Last Modified By")]
		public virtual Guid? LastModifiedByID { get; set; }
		#endregion

		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }

		[PXDBLastModifiedByScreenID(BqlField = typeof(CR.Standalone.CRQuote.lastModifiedByScreenID))]
		public virtual String LastModifiedByScreenID { get; set; }
		#endregion

		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }

		[PXDBLastModifiedDateTime(BqlField = typeof(CR.Standalone.CRQuote.lastModifiedDateTime))]
		[PXUIField(DisplayName = "Last Modified Date", Enabled = false)]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		#endregion


		#region RCreatedByID
		public abstract class rCreatedByID : PX.Data.BQL.BqlGuid.Field<rCreatedByID> { }
		[PXDBCreatedByID(BqlField = typeof(CROpportunityRevision.createdByID))]
		public virtual Guid? RCreatedByID
		{
			get;
			set;
		}
		#endregion

		#region RCreatedByScreenID
		public abstract class rCreatedByScreenID : PX.Data.BQL.BqlString.Field<rCreatedByScreenID> { }
		[PXDBCreatedByScreenID(BqlField = typeof(CROpportunityRevision.createdByScreenID))]
		public virtual String RCreatedByScreenID
		{
			get;
			set;
		}
		#endregion

		#region RCreatedDateTime
		public abstract class rCreatedDateTime : PX.Data.BQL.BqlDateTime.Field<rCreatedDateTime> { }
		[PXDBCreatedDateTime(BqlField = typeof(CROpportunityRevision.createdDateTime))]
		public virtual DateTime? RCreatedDateTime
		{
			get;
			set;
		}
		#endregion

		#region RLastModifiedByID
		public abstract class rLastModifiedByID : PX.Data.BQL.BqlGuid.Field<rLastModifiedByID> { }
		[PXDBLastModifiedByID(BqlField = typeof(CROpportunityRevision.lastModifiedByID))]
		public virtual Guid? RLastModifiedByID
		{
			get;
			set;
		}
		#endregion

		#region RLastModifiedByScreenID
		public abstract class rLastModifiedByScreenID : PX.Data.BQL.BqlString.Field<rLastModifiedByScreenID> { }
		[PXDBLastModifiedByScreenID(BqlField = typeof(CROpportunityRevision.lastModifiedByScreenID))]
		public virtual String RLastModifiedByScreenID
		{
			get;
			set;
		}
		#endregion

		#region RLastModifiedDateTime
		public abstract class rLastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<rLastModifiedDateTime> { }
		[PXDBLastModifiedDateTime(BqlField = typeof(CROpportunityRevision.lastModifiedDateTime))]
		public virtual DateTime? RLastModifiedDateTime
		{
			get;
			set;
		}
		#endregion

		#region CarrierID
		public abstract class carrierID : PX.Data.BQL.BqlString.Field<carrierID> { }

		/// <inheritdoc cref="CROpportunityRevision.CarrierID"/>
		[PXDBString(15, IsUnicode = true, InputMask = ">aaaaaaaaaaaaaaa", BqlField = typeof(CROpportunityRevision.carrierID))]
		[PXUIField(DisplayName = "Ship Via")]
		[PXSelector(typeof(Search<Carrier.carrierID>),
			typeof(Carrier.carrierID), typeof(Carrier.description), typeof(Carrier.isExternal), typeof(Carrier.confirmationRequired),
			CacheGlobal = true,
			DescriptionField = typeof(Carrier.description))]
		public virtual String CarrierID { get; set; }
		#endregion
		#region ShipTermsID
		public abstract class shipTermsID : PX.Data.BQL.BqlString.Field<shipTermsID> { }

		/// <inheritdoc cref="CROpportunityRevision.ShipTermsID"/>
		[PXDBString(10, IsUnicode = true, BqlField = typeof(CROpportunityRevision.shipTermsID))]
		[PXUIField(DisplayName = "Shipping Terms")]
		[PXSelector(typeof(Search<ShipTerms.shipTermsID>), CacheGlobal = true, DescriptionField = typeof(ShipTerms.description))]
		public virtual String ShipTermsID { get; set; }
		#endregion
		#region ShipZoneID
		public abstract class shipZoneID : PX.Data.BQL.BqlString.Field<shipZoneID> { }

		/// <inheritdoc cref="CROpportunityRevision.ShipZoneID"/>
		[PXDBString(15, IsUnicode = true, InputMask = ">aaaaaaaaaaaaaaa", BqlField = typeof(CROpportunityRevision.shipZoneID))]
		[PXUIField(DisplayName = "Shipping Zone")]
		[PXSelector(typeof(ShippingZone.zoneID), CacheGlobal = true, DescriptionField = typeof(ShippingZone.description))]
		public virtual String ShipZoneID { get; set; }
		#endregion
		#region FOBPointID
		public abstract class fOBPointID : PX.Data.BQL.BqlString.Field<fOBPointID> { }

		/// <inheritdoc cref="CROpportunityRevision.FOBPointID"/>
		[PXDBString(15, IsUnicode = true, BqlField = typeof(CROpportunityRevision.fOBPointID))]
		[PXUIField(DisplayName = "FOB Point")]
		[PXSelector(typeof(FOBPoint.fOBPointID), CacheGlobal = true, DescriptionField = typeof(FOBPoint.description))]
		public virtual String FOBPointID { get; set; }
		#endregion
		#region Resedential
		public abstract class resedential : PX.Data.BQL.BqlBool.Field<resedential> { }

		/// <inheritdoc cref="CROpportunityRevision.Resedential"/>
		[PXDBBool(BqlField = typeof(CROpportunityRevision.resedential))]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Residential Delivery")]
		public virtual Boolean? Resedential { get; set; }
		#endregion
		#region SaturdayDelivery
		public abstract class saturdayDelivery : PX.Data.BQL.BqlBool.Field<saturdayDelivery> { }

		/// <inheritdoc cref="CROpportunityRevision.SaturdayDelivery"/>
		[PXDBBool(BqlField = typeof(CROpportunityRevision.saturdayDelivery))]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Saturday Delivery")]
		public virtual Boolean? SaturdayDelivery { get; set; }
		#endregion
		#region Insurance
		public abstract class insurance : PX.Data.BQL.BqlBool.Field<insurance> { }

		/// <inheritdoc cref="CROpportunityRevision.Insurance"/>
		[PXDBBool(BqlField = typeof(CROpportunityRevision.insurance))]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Insurance")]
		public virtual Boolean? Insurance { get; set; }
		#endregion
		#region ShipComplete
		public abstract class shipComplete : PX.Data.BQL.BqlString.Field<shipComplete> { }

		/// <inheritdoc cref="CROpportunityRevision.ShipComplete"/>
		[PXDBString(1, IsFixed = true, BqlField = typeof(CROpportunityRevision.shipComplete))]
		[PXDefault(SOShipComplete.CancelRemainder)]
		[SOShipComplete.List()]
		[PXUIField(DisplayName = "Shipping Rule")]
		public virtual String ShipComplete { get; set; }
		#endregion
	}

	public class PMQuoteStatusAttribute : CRQuoteStatusAttribute
	{
		public const string Closed = "C";

		public PMQuoteStatusAttribute()
			: base(
				new[] {
					Draft,
					Approved,
					Sent,
					PendingApproval,
					Rejected,
					Accepted,
					Converted,
					Declined,
					Closed,
					QuoteApproved},
				new[] {
					CR.Messages.Draft,
					CR.Messages.Prepared,
					CR.Messages.Sent,
					CR.Messages.PendingApproval,
					CR.Messages.Rejected,
					CR.Messages.Accepted,
					CR.Messages.Converted,
					CR.Messages.Declined,
					Messages.Closed,
					CR.Messages.Approved
				})
		{ }
				
		public sealed class closed : PX.Data.BQL.BqlString.Constant<closed>
		{
			public closed() : base(Closed) { }
		}
	}
}
