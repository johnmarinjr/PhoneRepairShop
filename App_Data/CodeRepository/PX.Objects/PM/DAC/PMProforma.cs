using PX.Data;
using PX.Data.EP;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Data.WorkflowAPI;
using PX.Objects.AR;
using PX.Objects.CM.Extensions;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.TX;
using PX.Objects.Common;
using PX.TM;
using System;

namespace PX.Objects.PM
{
	/// <summary>Contains the main properties of a pro forma invoice. The records of this type are created during the project billing process and edited through the Pro Forma
	/// Invoices (PM307000) form (which corresponds to the <see cref="ProformaEntry" /> graph).</summary>
	[PXCacheName(Messages.Proforma)]
	[PXPrimaryGraph(typeof(ProformaEntry))]
	[Serializable]
	[PXEMailSource]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class PMProforma : PX.Data.IBqlTable, IAssign
	{
		#region Events
		public class Events : PXEntityEvent<PMProforma>.Container<Events>
		{
			public PXEntityEvent<PMProforma> Release;
		}
		#endregion

		#region Selected
		public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }
		protected bool? _Selected = false;

		[PXBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Selected")]
		public virtual bool? Selected
		{
			get
			{
				return _Selected;
			}
			set
			{
				_Selected = value;
			}
		}
		#endregion
		#region RevisionID
		public abstract class revisionID : PX.Data.BQL.BqlInt.Field<revisionID> { }

		/// <summary>
		/// The revision number of the pro forma invoice, which is an integer
		/// that the system assigns sequentially, starting from 1.
		/// </summary>
		[PXUIField(DisplayName = "Revision")]
		[PXDBInt(IsKey = true)]
		[PXDefault(1)]
		public virtual Int32? RevisionID
		{
			get;
			set;
		}
		#endregion
		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
		{
			public const int Length = 15;
		}
		protected String _RefNbr;

		/// <summary>
		/// The reference number of the pro forma invoice.
		/// </summary>
		/// <value>
		/// The number is generated from the <see cref="Numbering">numbering sequence</see>, 
		/// which is specified on the <see cref="PMSetup">Projects Preferences</see> (PM101000) form.
		/// </value>
		[PXDBString(refNbr.Length, IsUnicode = true, IsKey = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXDefault()]
		[PXSelector(typeof(Search<PMProforma.refNbr, Where<PMProforma.corrected, NotEqual<True>>>), Filterable = true)]
		[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
		[ProformaAutoNumber]
		public virtual String RefNbr
		{
			get
			{
				return this._RefNbr;
			}
			set
			{
				this._RefNbr = value;
			}
		}
		#endregion

		#region ProjectNbr
		public abstract class projectNbr : PX.Data.BQL.BqlString.Field<projectNbr>
		{
			public const int Length = 15;
		}
		/// <summary>The application number.</summary>
		[PXDBString(projectNbr.Length, IsUnicode = true)]
		[PXUIField(DisplayName = "Application Nbr.", Visibility = PXUIVisibility.SelectorVisible, FieldClass = nameof(FeaturesSet.Construction))]
		public virtual String ProjectNbr
		{
			get;
			set;
		}
		#endregion
		#region Description
		public abstract class description : PX.Data.BQL.BqlString.Field<description> { }
		protected String _Description;

		/// <summary>
		/// The description of the pro forma invoice, which is provided by the billing rule
		/// and can be manually modified.
		/// </summary>
		[PXDBString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Description")]
		[PXFieldDescription]
		public virtual String Description
		{
			get
			{
				return this._Description;
			}
			set
			{
				this._Description = value;
			}
		}
		#endregion
		#region Status
		public abstract class status : PX.Data.BQL.BqlString.Field<status> { }
		protected String _Status;

		/// <summary>
		/// The read-only status of the document.
		/// </summary>
		/// <value>
		/// The field can have one of the following values:
		/// <c>"H"</c>: On Hold,
		/// <c>"A"</c>: Pending Approval,
		/// <c>"O"</c>: Open,
		/// <c>"C"</c>: Closed,
		/// <c>"R"</c>: Rejected
		/// </value>
		[PXDBString(1, IsFixed = true)]
		[ProformaStatus.List()]
		[PXDefault(ProformaStatus.OnHold)]
		[PXUIField(DisplayName = "Status", Required = true, Enabled = false, Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String Status
		{
			get
			{
				return this._Status;
			}
			set
			{
				this._Status = value;
			}
		}
		#endregion
		#region Hold
		public abstract class hold : PX.Data.BQL.BqlBool.Field<hold> { }
		protected Boolean? _Hold;

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the document is on hold.
		/// </summary>
		[PXDBBool()]
		[PXUIField(DisplayName = "Hold")]
		[PXDefault(true)]
		public virtual Boolean? Hold
		{
			get
			{
				return this._Hold;
			}
			set
			{
				this._Hold = value;
			}
		}
		#endregion
		#region Approved
		public abstract class approved : PX.Data.BQL.BqlBool.Field<approved> { }
		protected Boolean? _Approved;

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the document is approved.
		/// </summary>
		[PXDBBool()]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Boolean? Approved
		{
			get
			{
				return this._Approved;
			}
			set
			{
				this._Approved = value;
			}
		}
		#endregion
		#region Rejected
		public abstract class rejected : PX.Data.BQL.BqlBool.Field<rejected> { }
		protected bool? _Rejected = false;

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the document is rejected.
		/// </summary>
		[PXDBBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public bool? Rejected
		{
			get
			{
				return _Rejected;
			}
			set
			{
				_Rejected = value;
			}
		}
		#endregion
		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }

		/// <summary>The identifier of the <see cref="Branch" /> to which the pro forma invoice belongs.</summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="Branch.BranchID" /> field.
		/// </value>
		[PXDefault]
		[Branch(IsDetail = false)]
		public virtual Int32? BranchID
		{
			get;
			set;
		}
		#endregion
		#region ProjectID
		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
		protected Int32? _ProjectID;

		/// <summary>The identifier of the <see cref="PMProject">project</see> associated with the pro forma invoice.</summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="PMProject.contractID" /> field.
		/// </value>
		[PXDefault]
		[PXForeignReference(typeof(Field<projectID>.IsRelatedTo<PMProject.contractID>))]
		[Project(Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual Int32? ProjectID
		{
			get
			{
				return this._ProjectID;
			}
			set
			{
				this._ProjectID = value;
			}
		}
		#endregion
		#region CustomerID
		public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }

		/// <summary>
		/// The identifier of the <see cref="Customer"/> associated with the pro forma invoice.
		/// </summary>
		/// <value>
		/// Defaults to the customer associated with the project.
		/// The value of this field corresponds to the value of the <see cref="BAccount.BAccountID"/> field.
		/// </value>
		[PXDefault]
		[Customer(DescriptionField = typeof(Customer.acctName), Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual Int32? CustomerID
		{
			get;
			set;
		}
		#endregion
		#region CustomerID_Customer_acctName
		public abstract class customerID_Customer_acctName : PX.Data.BQL.BqlString.Field<customerID_Customer_acctName> { }
		#endregion
		#region BillAddressID
		public abstract class billAddressID : PX.Data.BQL.BqlInt.Field<billAddressID> { }
		protected Int32? _BillAddressID;

		/// <summary>
		/// The identifier of the <see cref="PMAddress">Billing Address object</see>, associated with the customer.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="PMAddress.AddressID"/> field.
		/// </value>
		[PXDBInt()]
		[PMAddress(typeof(Select2<Customer,
			InnerJoin<CR.Standalone.Location, On<CR.Standalone.Location.bAccountID, Equal<Customer.bAccountID>, And<CR.Standalone.Location.locationID, Equal<Customer.defLocationID>>>,
			InnerJoin<Address, On<Address.bAccountID, Equal<Customer.bAccountID>, And<Address.addressID, Equal<Customer.defBillAddressID>>>,
			LeftJoin<PMAddress, On<PMAddress.customerID, Equal<Address.bAccountID>, And<PMAddress.customerAddressID, Equal<Address.addressID>, And<PMAddress.revisionID, Equal<Address.revisionID>, And<PMAddress.isDefaultBillAddress, Equal<True>>>>>>>>,
			Where<Customer.bAccountID, Equal<Current<PMProforma.customerID>>>>), typeof(customerID))]
		public virtual Int32? BillAddressID
		{
			get
			{
				return this._BillAddressID;
			}
			set
			{
				this._BillAddressID = value;
			}
		}
		#endregion
		#region BillContactID
		public abstract class billContactID : PX.Data.BQL.BqlInt.Field<billContactID> { }

		/// <summary>The identifier of the <see cref="ARContact">billing contact</see> associated with the customer.</summary>
		/// <value>
		/// Corresponds to the <see cref="ARContact.ContactID" /> field.
		/// </value>
		[PXDBInt]
		[PXSelector(typeof(PMContact.contactID), ValidateValue = false)]    //Attribute for showing contact email field on Automatic Notifications screen in the list of availible emails for
																			//Invoices and Memos screen. Relies on the work of platform, which uses PXSelector to compose email list
		[PXUIField(DisplayName = "Billing Contact", Visible = false)]       //Attribute for displaying user friendly contact email field on Automatic Notifications screen in the list of availible emails.
		[PMContact(typeof(Select2<Customer,
							InnerJoin<
									  CR.Standalone.Location, On<CR.Standalone.Location.bAccountID, Equal<Customer.bAccountID>,
								  And<CR.Standalone.Location.locationID, Equal<Customer.defLocationID>>>,
							InnerJoin<
									  Contact, On<Contact.bAccountID, Equal<Customer.bAccountID>,
								  And<Contact.contactID, Equal<Customer.defBillContactID>>>,
							LeftJoin<
									 PMContact, On<PMContact.customerID, Equal<Contact.bAccountID>,
								 And<PMContact.customerContactID, Equal<Contact.contactID>,
								 And<PMContact.revisionID, Equal<Contact.revisionID>,
								 And<PMContact.isDefaultContact, Equal<True>>>>>>>>,
							Where<Customer.bAccountID, Equal<Current<PMProforma.customerID>>>>), typeof(customerID))]
		public virtual int? BillContactID
		{
			get;
			set;
		}
		#endregion
		#region ShipAddressID
		public abstract class shipAddressID : PX.Data.BQL.BqlInt.Field<shipAddressID> { }

		/// <summary>The identifier of the <see cref="PMAddress">shipping address</see> associated with the customer.</summary>
		/// <value>
		/// Corresponds to the <see cref="PMAddress.AddressID" /> field.
		/// </value>
		[PXDBInt]
		[PMShippingAddress(typeof(Select2<Customer,
			InnerJoin<CR.Standalone.Location, On<CR.Standalone.Location.bAccountID, Equal<Customer.bAccountID>,
				And<CR.Standalone.Location.locationID, Equal<Current<PMProforma.locationID>>>>,
			InnerJoin<Address, On<Address.bAccountID, Equal<Customer.bAccountID>,
				And<Address.addressID, Equal<Location.defAddressID>>>,
			LeftJoin<PMShippingAddress, On<PMShippingAddress.customerID, Equal<Address.bAccountID>,
				And<PMShippingAddress.customerAddressID, Equal<Address.addressID>,
				And<PMShippingAddress.revisionID, Equal<Address.revisionID>,
				And<PMShippingAddress.isDefaultBillAddress, Equal<True>>>>>>>>,
			Where<Customer.bAccountID, Equal<Current<PMProforma.customerID>>>>), typeof(PMProforma.customerID))]
		public virtual int? ShipAddressID
		{
			get;
			set;
		}
		#endregion
		#region ShipContactID
		public abstract class shipContactID : PX.Data.BQL.BqlInt.Field<shipContactID> { }

		/// <summary>The identifier of the <see cref="PMContact">shipping contact</see> associated with the customer.</summary>
		/// <value>
		/// Corresponds to the <see cref="PMContact.ContactID" /> field.
		/// </value>
		[PXDBInt]
		[PXSelector(typeof(PMShippingContact.contactID), ValidateValue = false)]
		[PXUIField(DisplayName = "Shipping Contact", Visible = false)]
		[PMShippingContact(typeof(Select2<Customer,
			InnerJoin<CR.Standalone.Location, On<CR.Standalone.Location.bAccountID, Equal<Customer.bAccountID>,
				And<CR.Standalone.Location.locationID, Equal<Current<PMProforma.locationID>>>>,
			InnerJoin<Contact, On<Contact.bAccountID, Equal<Customer.bAccountID>,
				And<Contact.contactID, Equal<Location.defContactID>>>,
			LeftJoin<PMShippingContact, On<PMShippingContact.customerID, Equal<Contact.bAccountID>,
				And<PMShippingContact.customerContactID, Equal<Contact.contactID>,
				And<PMShippingContact.revisionID, Equal<Contact.revisionID>,
				And<PMShippingContact.isDefaultContact, Equal<True>>>>>>>>,
			Where<Customer.bAccountID, Equal<Current<PMProforma.customerID>>>>), typeof(PMProforma.customerID))]
		public virtual int? ShipContactID
		{
			get;
			set;
		}
		#endregion
		#region LocationID
		public abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
		protected Int32? _LocationID;

		/// <summary>
		/// The identifier of the <see cref="Location"/> associated with the pro forma invoice.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="Location.LocationID"/> field.
		/// </value>
		[LocationID(typeof(Where<Location.bAccountID, Equal<Current<PMProforma.customerID>>>), Visibility = PXUIVisibility.SelectorVisible, DisplayName = "Location", DescriptionField = typeof(Location.descr))]
		[PXDefault]
		public virtual Int32? LocationID
		{
			get
			{
				return this._LocationID;
			}
			set
			{
				this._LocationID = value;
			}
		}
		#endregion
		#region TaxZoneID
		public abstract class taxZoneID : PX.Data.BQL.BqlString.Field<taxZoneID> { }
		protected String _TaxZoneID;

		/// <summary>
		/// The identifier of the <see cref="TaxZone"/> associated with the document.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="TaxZone.TaxZoneID"/> field.
		/// </value>
		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Customer Tax Zone")]
		[PXRestrictor(typeof(Where<TaxZone.isManualVATZone, Equal<False>>), TX.Messages.CantUseManualVAT)]
		[PXSelector(typeof(TaxZone.taxZoneID), DescriptionField = typeof(TaxZone.descr), Filterable = true)]
		[PXFormula(typeof(Default<PMProforma.locationID>))]
		public virtual String TaxZoneID
		{
			get
			{
				return this._TaxZoneID;
			}
			set
			{
				this._TaxZoneID = value;
			}
		}
		#endregion
		#region ExternalTaxExemptionNumber
		public abstract class externalTaxExemptionNumber : PX.Data.BQL.BqlString.Field<externalTaxExemptionNumber> { }

		/// <summary>The tax exemption number for reporting purposes. The field is used if the system is integrated with an external tax calculation system and the <see cref="FeaturesSet.AvalaraTax">External Tax
		/// Calculation Integration</see> feature is enabled.</summary>
		[PXDefault(typeof(Search<Location.cAvalaraExemptionNumber,
			Where<Location.bAccountID, Equal<Current<customerID>>,
				And<Location.locationID, Equal<Current<locationID>>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXDBString(30, IsUnicode = true)]
		[PXUIField(DisplayName = "Tax Exemption Number")]
		public virtual string ExternalTaxExemptionNumber { get; set; }
		#endregion
		#region AvalaraCustomerUsageType
		public abstract class avalaraCustomerUsageType : PX.Data.BQL.BqlString.Field<avalaraCustomerUsageType> { }
		protected String _AvalaraCustomerUsageType;

		/// <summary>The customer entity type for reporting purposes. The field is used if the system is integrated with an external tax calculation system and the <see cref="FeaturesSet.AvalaraTax">External Tax
		/// Calculation Integration</see> feature is enabled.</summary>
		/// <value>
		/// The field can have one of the values described in <see cref="TXAvalaraCustomerUsageType.ListAttribute" />.
		/// Defaults to the <see cref="Location.CAvalaraCustomerUsageType">customer entity type</see>
		/// that is specified for the <see cref="CustomerLocationID">location of the customer</see>.
		/// </value>
		[PXDefault(
			TXAvalaraCustomerUsageType.Default,
			typeof(Search<Location.cAvalaraCustomerUsageType,
				Where<Location.bAccountID, Equal<Current<customerID>>,
					And<Location.locationID, Equal<Current<locationID>>>>>))]
		[PXDBString(1, IsFixed = true)]
		[PXUIField(DisplayName = "Entity Usage Type")]
		[TX.TXAvalaraCustomerUsageType.List]
		public virtual String AvalaraCustomerUsageType
		{
			get
			{
				return this._AvalaraCustomerUsageType;
			}
			set
			{
				this._AvalaraCustomerUsageType = value;
			}
		}
		#endregion
		#region CuryID
		public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
		protected String _CuryID;

		/// <summary>
		/// The identifier of the pro forma invoice <see cref="Currency">currency</see>.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="Currency.CuryID"/> field.
		/// </value>
		[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL")]
		[PXUIField(DisplayName = "Currency", Visibility = PXUIVisibility.SelectorVisible)]
		[PXDefault(typeof(Current<AccessInfo.baseCuryID>))]
		[PXSelector(typeof(Currency.curyID))]
		public virtual String CuryID
		{
			get
			{
				return this._CuryID;
			}
			set
			{
				this._CuryID = value;
			}
		}
		#endregion
		#region CuryInfoID
		public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }

		/// <summary>
		/// The identifier of the <see cref="CurrencyInfo">CurrencyInfo</see> object associated with the document.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="CurrencyInfoID"/> field.
		/// </value>
		[PXDBLong]
		[CurrencyInfo]
		public virtual Int64? CuryInfoID
		{
			get;
			set;
		}
		#endregion
		#region InvoiceDate
		public abstract class invoiceDate : PX.Data.BQL.BqlDateTime.Field<invoiceDate> { }
		protected DateTime? _InvoiceDate;

		/// <summary>
		/// The date on which the pro forma invoice was created.
		/// </summary>
		[PXDBDate()]
		[PXDefault(TypeCode.DateTime, "01/01/1900")]
		[PXUIField(DisplayName = "Invoice Date", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual DateTime? InvoiceDate
		{
			get
			{
				return this._InvoiceDate;
			}
			set
			{
				this._InvoiceDate = value;
			}
		}
		#endregion
		#region FinPeriodID
		public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
		protected String _FinPeriodID;

		/// <summary>
		/// The financial period that corresponds to the <see cref="InvoiceDate">invoice date</see>.
		/// </summary>
		[AROpenPeriod(
			typeof(PMProforma.invoiceDate),
			branchSourceType: typeof(PMProforma.branchID), IsHeader = true)]
		[PXUIField(DisplayName = "Post Period", Visibility = PXUIVisibility.SelectorVisible, Required = true)]
		public virtual String FinPeriodID
		{
			get
			{
				return this._FinPeriodID;
			}
			set
			{
				this._FinPeriodID = value;
			}
		}
		#endregion
		#region TermsID
		public abstract class termsID : PX.Data.BQL.BqlString.Field<termsID> { }
		protected String _TermsID;

		/// <summary>The identifier of the <see cref="Terms">credit terms</see> object associated with the document.</summary>
		/// <value>Defaults to the <see cref="Customer.TermsID">credit terms</see> that are selected for the <see cref="CustomerID">customer</see>. The value corresponds to the value of the <see cref="Terms.TermsID" />
		/// field.</value>
		[PXDBString(10, IsUnicode = true)]
		[PXDefault(typeof(Search<PMProject.termsID, Where<PMProject.contractID, Equal<Current<PMProforma.projectID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Terms", Visibility = PXUIVisibility.Visible)]
		[PXSelector(typeof(Search<Terms.termsID, Where<Terms.visibleTo, Equal<TermsVisibleTo.all>, Or<Terms.visibleTo, Equal<TermsVisibleTo.customer>>>>), DescriptionField = typeof(Terms.descr), Filterable = true)]
		[Terms(typeof(invoiceDate), typeof(dueDate), typeof(discDate), null, null)]
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
		#region DueDate
		public abstract class dueDate : PX.Data.BQL.BqlDateTime.Field<dueDate> { }

		/// <summary>The date when the payment for the document is due, in accordance with the <see cref="TermsID">credit terms</see>.</summary>
		[PXDBDate()]
		[PXUIField(DisplayName = "Due Date", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual DateTime? DueDate
		{
			get; set;
		}
		#endregion
		#region DiscDate
		public abstract class discDate : PX.Data.BQL.BqlDateTime.Field<discDate> { }
		protected DateTime? _DiscDate;

		/// <summary>
		/// The end date of the cash discount period, which the system calculates by using the <see cref="TermsID">credit terms</see>.
		/// </summary>
		[PXDBDate()]
		[PXUIField(DisplayName = "Cash Discount Date", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual DateTime? DiscDate
		{
			get
			{
				return this._DiscDate;
			}
			set
			{
				this._DiscDate = value;
			}
		}
		#endregion
		#region WorkgroupID
		public abstract class workgroupID : PX.Data.BQL.BqlInt.Field<workgroupID> { }
		protected int? _WorkgroupID;

		/// <summary>The workgroup that is responsible for the document.</summary>
		/// <value>
		/// Corresponds to the <see cref="PX.TM.EPCompanyTree.WorkGroupID">EPCompanyTree.WorkGroupID</see> field.
		/// </value>
		[PXDBInt]
		[PXDefault(typeof(Customer.workgroupID), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXCompanyTreeSelector]
		[PXUIField(DisplayName = "Workgroup", Visibility = PXUIVisibility.Visible)]
		public virtual int? WorkgroupID
		{
			get
			{
				return this._WorkgroupID;
			}
			set
			{
				this._WorkgroupID = value;
			}
		}
		#endregion
		#region OwnerID
		public abstract class ownerID : PX.Data.BQL.BqlInt.Field<ownerID> { }
		protected int? _OwnerID;

		/// <summary>The <see cref="Contact">contact</see> responsible for the document.</summary>
		/// <value>
		/// Corresponds to the <see cref="Contact.ContactID" /> field.
		/// </value>
		[PXDBInt]
		[PXDefault(typeof(Customer.ownerID), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Owner", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual int? OwnerID
		{
			get
			{
				return this._OwnerID;
			}
			set
			{
				this._OwnerID = value;
			}
		}
		#endregion
		#region LineCntr
		public abstract class lineCntr : PX.Data.BQL.BqlInt.Field<lineCntr> { }
		protected Int32? _LineCntr;

		/// <summary>A counter of the document lines, which is used internally to assign <see cref="PMProformaLine.LineNbr">numbers</see> to newly created lines. We do not recommend that you
		/// rely on this field to determine the exact number of lines because it might not reflect the this number under various conditions.</summary>
		[PXDBInt()]
		[PXDefault(0)]
		public virtual Int32? LineCntr
		{
			get
			{
				return this._LineCntr;
			}
			set
			{
				this._LineCntr = value;
			}
		}
		#endregion
		#region Released
		public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }
		protected Boolean? _Released;

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the document has been released.
		/// </summary>
		[PXDBBool()]
		[PXUIField(DisplayName = "Released")]
		[PXDefault(false)]
		public virtual Boolean? Released
		{
			get
			{
				return this._Released;
			}
			set
			{
				this._Released = value;
			}
		}
		#endregion
		#region Corrected
		public abstract class corrected : PX.Data.BQL.BqlBool.Field<corrected> { }

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the document has been corrected.
		/// </summary>
		[PXDBBool()]
		[PXUIField(DisplayName = "Corrected")]
		[PXDefault(false)]
		public virtual Boolean? Corrected
		{
			get;
			set;
		}
		#endregion
		#region EnableProgressive
		public abstract class enableProgressive : PX.Data.BQL.BqlBool.Field<enableProgressive> { }

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the document has
		/// <see cref="PMProformaProgressLine">lines of project progress billing type</see>.
		/// </summary>
		[PXDBBool()]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Enable Progressive Tab")]
		public virtual Boolean? EnableProgressive
		{
			get;
			set;
		}
		#endregion
		#region EnableTransactional
		public abstract class enableTransactional : PX.Data.BQL.BqlBool.Field<enableTransactional> { }

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the document has
		/// <see cref="PMProformaTransactLine">lines of project time and material billing type</see>.
		/// </summary>
		[PXDBBool()]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Enable Transactions Tab")]
		public virtual Boolean? EnableTransactional
		{
			get;
			set;
		}
		#endregion
		#region ExtRefNbr
		public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }
		protected String _ExtRefNbr;

		/// <summary>
		/// The reference number of the external document.
		/// </summary>
		[PXDBString(30, IsUnicode = true)]
		[PXUIField(DisplayName = "External Ref. Nbr")]
		public virtual String ExtRefNbr
		{
			get
			{
				return this._ExtRefNbr;
			}
			set
			{
				this._ExtRefNbr = value;
			}
		}
		#endregion

		#region CuryTransactionalTotal
		public abstract class curyTransactionalTotal : PX.Data.BQL.BqlDecimal.Field<curyTransactionalTotal> { }

		/// <summary>The total <see cref="PMProformaTransactLine.CuryLineTotal">amount to invoice</see> of the <see cref="PMProformaTransactLine">time and material lines</see> of the document.</summary>
		[PXDBCurrency(typeof(curyInfoID), typeof(transactionalTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Time and Material Total")]
		public virtual Decimal? CuryTransactionalTotal
		{
			get; set;
		}
		#endregion
		#region TransactionalTotal
		public abstract class transactionalTotal : PX.Data.BQL.BqlDecimal.Field<transactionalTotal> { }

		/// <summary>The total <see cref="PMProformaTransactLine.CuryLineTotal">amount to invoice</see> of the <see cref="PMProformaTransactLine">time and material lines</see> of the document in the base
		/// currency.</summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Time and Material Total in Base Currency")]
		public virtual Decimal? TransactionalTotal
		{
			get; set;
		}
		#endregion
		#region CuryProgressiveTotal
		public abstract class curyProgressiveTotal : PX.Data.BQL.BqlDecimal.Field<curyProgressiveTotal> { }

		/// <summary>The total <see cref="PMProformaProgressLine.CuryLineTotal">amount to invoice</see> of the <see cref="PMProformaProgressLine">progress billing lines</see> of the document.</summary>
		[PXDBCurrency(typeof(curyInfoID), typeof(progressiveTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Progress Billing Total")]
		public virtual Decimal? CuryProgressiveTotal
		{
			get; set;
		}
		#endregion
		#region ProgressiveTotal
		public abstract class progressiveTotal : PX.Data.BQL.BqlDecimal.Field<progressiveTotal> { }

		/// <summary>The total <see cref="PMProformaProgressLine.CuryLineTotal">amount to invoice</see> of the <see cref="PMProformaProgressLine">progress billing lines</see> of the document in the base currency.</summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Progress Billing Total in Base Currency")]
		public virtual Decimal? ProgressiveTotal
		{
			get; set;
		}
		#endregion
		#region CuryRetainageTotal
		public abstract class curyRetainageTotal : PX.Data.BQL.BqlDecimal.Field<curyRetainageTotal> { }

		/// <summary>
		/// The total retained amount.
		/// </summary>
		/// <value>Calculated as the sum of <see cref="CuryRetainageDetailTotal" /> and <see cref="CuryRetainageTaxTotal" />.</value>
		[PXCurrency(typeof(curyInfoID), typeof(retainageTotal))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Retainage Total", FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? CuryRetainageTotal
		{
			[PXDependsOnFields(typeof(curyRetainageDetailTotal), typeof(curyRetainageTaxTotal))]
			get { return CuryRetainageDetailTotal + CuryRetainageTaxTotal; }
		}
		#endregion
		#region RetainageTotal
		public abstract class retainageTotal : PX.Data.BQL.BqlDecimal.Field<retainageTotal> { }

		/// <summary>The total retained amount in the base currency.</summary>
		/// <value>Calculated as the sum of <see cref="RetainageDetailTotal" /> and <see cref="RetainageTaxTotal" />.</value>
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Retainage Total in Base Currency", FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? RetainageTotal
		{
			[PXDependsOnFields(typeof(retainageDetailTotal), typeof(retainageTaxTotal))]
			get { return RetainageDetailTotal + RetainageTaxTotal; }
		}
		#endregion
		#region CuryRetainageDetailTotal
		public abstract class curyRetainageDetailTotal : PX.Data.BQL.BqlDecimal.Field<curyRetainageDetailTotal> { }

		/// <summary>The total retained amount for the <see cref="PMProformaProgressLine">progress billing lines</see> and <see cref="PMProformaTransactLine">time and material lines</see> of the pro forma
		/// invoice.</summary>
		/// <value>Calculated as the sum of the values in the <see cref="PMProformaProgressLine.CuryRetainage">retainage amount</see> column for the lines with progressive type plus the sum of
		/// the values in the <see cref="PMProformaTransactLine.CuryRetainage">retainage amount</see> column for the lines with transaction type of the pro forma invoice.</value>
		[PXDBCurrency(typeof(curyInfoID), typeof(retainageDetailTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Retainage Detail Total", FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? CuryRetainageDetailTotal
		{
			get; set;
		}
		#endregion
		#region RetainageTotal
		public abstract class retainageDetailTotal : PX.Data.BQL.BqlDecimal.Field<retainageDetailTotal> { }

		/// <summary>The total retained amount for the <see cref="PMProformaProgressLine">progress billing lines</see> and <see cref="PMProformaTransactLine">time and material lines</see> of the pro forma invoice
		/// in the base currency.</summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Retainage Detail Total in Base Currency", FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? RetainageDetailTotal
		{
			get; set;
		}
		#endregion
		#region CuryRetainageTaxTotal
		public abstract class curyRetainageTaxTotal : PX.Data.BQL.BqlDecimal.Field<curyRetainageTaxTotal> { }

		/// <summary>
		/// The total retained tax amount.
		/// </summary>
		[PXDBCurrency(typeof(curyInfoID), typeof(retainageTaxTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Retained Tax Total", FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? CuryRetainageTaxTotal
		{
			get; set;
		}
		#endregion
		#region RetainageTaxTotal
		public abstract class retainageTaxTotal : PX.Data.BQL.BqlDecimal.Field<retainageTaxTotal> { }

		/// <summary>
		/// The total retained tax amount in the base currency.
		/// </summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Retainage Tax Total in Base Currency", FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? RetainageTaxTotal
		{
			get; set;
		}
		#endregion
		#region CuryTaxTotal
		public abstract class curyTaxTotal : PX.Data.BQL.BqlDecimal.Field<curyTaxTotal> { }

		/// <summary>
		/// The tax amount of the document.
		/// </summary>
		[PXDBCurrency(typeof(curyInfoID), typeof(taxTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Tax Total")]
		public virtual Decimal? CuryTaxTotal
		{
			get; set;
		}
		#endregion
		#region TaxTotal
		public abstract class taxTotal : PX.Data.BQL.BqlDecimal.Field<taxTotal> { }

		/// <summary>
		/// The tax amount of the document in the base currency.
		/// </summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Tax Total in Base Currency")]
		public virtual Decimal? TaxTotal
		{
			get; set;
		}
		#endregion
		#region CuryTaxTotalWithRetainage
		public abstract class curyTaxTotalWithRetainage : PX.Data.BQL.BqlDecimal.Field<curyTaxTotalWithRetainage> { }

		/// <summary>
		/// The tax amount of the document.
		/// </summary>
		/// <value>Calculated as the sum of <see cref="CuryTaxTotal" /> plus <see cref="CuryRetainageTaxTotal" />.</value>
		[PXCurrency(typeof(curyInfoID), typeof(taxTotalWithRetainage))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Tax Total")]
		public virtual Decimal? CuryTaxTotalWithRetainage
		{
			[PXDependsOnFields(typeof(curyTaxTotal), typeof(curyRetainageTaxTotal))]
			get { return CuryTaxTotal + CuryRetainageTaxTotal; }
		}
		#endregion
		#region TaxTotalWithRetainage
		public abstract class taxTotalWithRetainage : PX.Data.BQL.BqlDecimal.Field<taxTotalWithRetainage> { }

		/// <summary>
		/// The tax amount of the document in the base currency.
		/// </summary>
		/// <value>Calculated as the sum of <see cref="TaxTotal" /> and <see cref="RetainageTaxTotal" />.</value>
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Amount Due Total in Base Currency", FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? TaxTotalWithRetainage
		{
			[PXDependsOnFields(typeof(taxTotal), typeof(retainageTaxTotal))]
			get { return TaxTotal + RetainageTaxTotal; }
		}
		#endregion

		#region CuryDocTotal
		public abstract class curyDocTotal : PX.Data.BQL.BqlDecimal.Field<curyDocTotal> { }

		/// <summary>The invoice total.</summary>
		/// <value>The sum of the <see cref="CuryProgressiveTotal">progress billing total</see>, <see cref="CuryTransactionalTotal">time and material total</see>, and tax total values.</value>
		[PXFormula(typeof(Add<curyRetainageTaxTotal, Add<curyTaxTotal, Add<curyProgressiveTotal, curyTransactionalTotal>>>))]
		[PXDBCurrency(typeof(curyInfoID), typeof(docTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Invoice Total")]
		public virtual Decimal? CuryDocTotal
		{
			get; set;
		}
		#endregion
		#region Total
		public abstract class docTotal : PX.Data.BQL.BqlDecimal.Field<docTotal> { }

		/// <summary>The invoice total in the base currency.</summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Invoice Total in Base Currency")]
		public virtual Decimal? DocTotal
		{
			get; set;
		}
		#endregion
		#region CuryAmountDue
		public abstract class curyAmountDue : PX.Data.BQL.BqlDecimal.Field<curyAmountDue> { }

		/// <summary>The amount due.</summary>
		/// <value>The difference between the <see cref="CuryDocTotal">Invoice Total</see> and <see cref="CuryRetainageTotal">Retainage Total</see>.</value>
		[PXCurrency(typeof(curyInfoID), typeof(amountDue))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Amount Due", FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? CuryAmountDue
		{
			[PXDependsOnFields(typeof(curyDocTotal), typeof(curyRetainageTotal))]
			get { return CuryDocTotal - CuryRetainageTotal; }
		}
		#endregion
		#region AmountDue
		public abstract class amountDue : PX.Data.BQL.BqlDecimal.Field<amountDue> { }

		/// <summary>The amount due in the base currency.</summary>
		/// <value>The difference between the <see cref="DocTotal">invoice total</see> and <see cref="RetainageTotal">retainage total</see>.</value>
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Amount Due Total in Base Currency", FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? AmountDue
		{
			[PXDependsOnFields(typeof(docTotal), typeof(retainageTotal))]
			get { return DocTotal - RetainageTotal; }
		}
		#endregion

		#region CuryAllocatedRetainedTotal
		/// <exclude/>
		public abstract class curyAllocatedRetainedTotal : PX.Data.BQL.BqlDecimal.Field<curyAllocatedRetainedTotal> { }
		/// <summary>The allocated retained total.</summary>
		[PXDBCurrency(typeof(PMProformaLine.curyInfoID), typeof(PMProforma.allocatedRetainedTotal))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Allocated Retained Total", Enabled = false, FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? CuryAllocatedRetainedTotal
		{
			get;
			set;
		}
		#endregion

		#region AllocatedRetainedTotal
		/// <exclude/>
		public abstract class allocatedRetainedTotal : PX.Data.BQL.BqlDecimal.Field<allocatedRetainedTotal> { }
		/// <summary>The allocated retained total (in the base currency).</summary>
		[PXDBBaseCury()]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Decimal? AllocatedRetainedTotal
		{
			get;
			set;
		}
		#endregion

		#region RetainagePct
		/// <exclude/>
		public abstract class retainagePct : PX.Data.BQL.BqlDecimal.Field<retainagePct>
		{
		}
		/// <summary>The retainage in percents.</summary>
		[PXDBDecimal(2, MinValue = 0, MaxValue = 100)]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Retainage (%)", Visible = false)]
		public virtual Decimal? RetainagePct
		{
			get;
			set;
		}
		#endregion
		#region IsTaxValid
		public abstract class isTaxValid : PX.Data.BQL.BqlBool.Field<isTaxValid> { }

		/// <summary>Specifies (if set to <see langword="true"></see>) that the amount of tax calculated with the external tax engine (such as Avalara) is up to date. If this field equals
		/// <see langword="false"></see>, the document was updated since the last synchronization with the tax Engine and taxes might need recalculation.</summary>
		[PXDBBool()]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Tax Is Up to Date", Enabled = false)]
		public virtual Boolean? IsTaxValid
		{
			get; set;
		}
		#endregion
		#region IsAIAOutdated
		public abstract class isAIAOutdated : PX.Data.BQL.BqlBool.Field<isAIAOutdated> { }

		/// <summary>Specifies (if set to <see langword="true"></see>) that the current version of the AIA report is not up-to-date and should be reprinted.</summary>
		[PXDBBool()]
		[PXDefault(false)]
		[PXUIField(DisplayName = "AIA Is Outdated", Enabled = false)]
		public virtual Boolean? IsAIAOutdated
		{
			get; set;
		}
		#endregion

		#region ARInvoiceDocType
		public abstract class aRInvoiceDocType : PX.Data.BQL.BqlString.Field<aRInvoiceDocType> { }

		/// <summary>
		/// The type of the corresponding <see cref="ARInvoice">accounts receivable document</see> created on release of the pro forma invoice.
		/// </summary>
		/// <value>
		/// The field can have one of the values described in <see cref="ARInvoiceType.ListAttribute"/>.
		/// </value>
		[ARInvoiceType.List()]
		[PXUIField(DisplayName = "AR Doc. Type", Visibility = PXUIVisibility.Visible, Enabled = false)]
		[PXDBString(3)]
		public virtual String ARInvoiceDocType
		{
			get; set;
		}
		#endregion
		#region ARInvoiceRefNbr
		public abstract class aRInvoiceRefNbr : PX.Data.BQL.BqlString.Field<aRInvoiceRefNbr> { }

		/// <summary>
		/// The reference number of the corresponding <see cref="ARInvoice">accounts receivable document</see> created on release of the pro forma invoice.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="ARInvoice.RefNbr"/> field.
		/// </value>
		[PXUIField(DisplayName = "AR Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
		[PXSelector(typeof(Search<ARInvoice.refNbr, Where<ARInvoice.docType, Equal<Current<aRInvoiceDocType>>>>))]
		[PXDBString(15, IsUnicode = true)]
		public virtual String ARInvoiceRefNbr
		{
			get; set;
		}
		#endregion
		#region ReversedARInvoiceDocType
		public abstract class reversedARInvoiceDocType : PX.Data.BQL.BqlString.Field<reversedARInvoiceDocType> { }

		/// <summary>
		/// The type of the  <see cref="ARInvoice">AR document</see> that reverses the AR document specified in the <see cref="ARInvoiceRefNbr">AR Ref. Nbr.</see> field.
		/// </summary>
		/// <value>
		/// The field can have one of the values described in <see cref="ARInvoiceType.ListAttribute"/>.
		/// </value>
		[ARInvoiceType.List()]
		[PXUIField(DisplayName = "Reversing Doc. Type", Visibility = PXUIVisibility.Visible, Enabled = false)]
		[PXDBString(3)]
		public virtual String ReversedARInvoiceDocType
		{
			get; set;
		}
		#endregion
		#region ReversedARInvoiceRefNbr
		public abstract class reversedARInvoiceRefNbr : PX.Data.BQL.BqlString.Field<reversedARInvoiceRefNbr> { }

		/// <summary>
		/// The reference number of the <see cref="ARInvoice">AR document</see> that reverses the AR document specified in the <see cref="ARInvoiceRefNbr">AR Ref. Nbr.</see> field.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="ARInvoice.RefNbr"/> field.
		/// </value>
		[PXUIField(DisplayName = "Reversing Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
		[PXSelector(typeof(Search<ARInvoice.refNbr, Where<ARInvoice.docType, Equal<Current<reversedARInvoiceDocType>>>>))]
		[PXDBString(15, IsUnicode = true)]
		public virtual String ReversedARInvoiceRefNbr
		{
			get; set;
		}
		#endregion

		#region System Columns
		#region NoteID
		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
		protected Guid? _NoteID;
		[PXSearchable(SM.SearchCategory.PM, Messages.ProformaSearchTitle, new Type[] { typeof(PMProforma.refNbr), typeof(PMProforma.customerID), typeof(Customer.acctName) },
			new Type[] { typeof(PMProforma.description), typeof(PMProforma.projectID), typeof(PMProject.contractCD), typeof(PMProject.description) },
			NumberFields = new Type[] { typeof(PMProforma.refNbr) },
			Line1Format = "{0:d}{1}{2}", Line1Fields = new Type[] { typeof(PMProforma.invoiceDate), typeof(PMProforma.status), typeof(PMProforma.projectID) },
			Line2Format = "{0}", Line2Fields = new Type[] { typeof(PMProforma.description) },
			MatchWithJoin = typeof(InnerJoin<Customer, On<Customer.bAccountID, Equal<PMProforma.customerID>>>),
			SelectForFastIndexing = typeof(Select2<PMProforma, InnerJoin<Customer, On<PMProforma.customerID, Equal<Customer.bAccountID>>,
				InnerJoin<PMProject, On<PMProforma.projectID, Equal<PMProject.contractID>>>>>)
		)]
		[PXNote(DescriptionField = typeof(PMProforma.refNbr))]
		public virtual Guid? NoteID
		{
			get
			{
				return this._NoteID;
			}
			set
			{
				this._NoteID = value;
			}
		}
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
		protected Byte[] _tstamp;
		[PXDBTimestamp()]
		public virtual Byte[] tstamp
		{
			get
			{
				return this._tstamp;
			}
			set
			{
				this._tstamp = value;
			}
		}
		#endregion
		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		protected Guid? _CreatedByID;
		[PXDBCreatedByID]
		public virtual Guid? CreatedByID
		{
			get
			{
				return this._CreatedByID;
			}
			set
			{
				this._CreatedByID = value;
			}
		}
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		protected String _CreatedByScreenID;
		[PXDBCreatedByScreenID()]
		public virtual String CreatedByScreenID
		{
			get
			{
				return this._CreatedByScreenID;
			}
			set
			{
				this._CreatedByScreenID = value;
			}
		}
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		protected DateTime? _CreatedDateTime;
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.CreatedDateTime, Enabled = false, IsReadOnly = true)]
		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime
		{
			get
			{
				return this._CreatedDateTime;
			}
			set
			{
				this._CreatedDateTime = value;
			}
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		protected Guid? _LastModifiedByID;
		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID
		{
			get
			{
				return this._LastModifiedByID;
			}
			set
			{
				this._LastModifiedByID = value;
			}
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		protected String _LastModifiedByScreenID;
		[PXDBLastModifiedByScreenID()]
		public virtual String LastModifiedByScreenID
		{
			get
			{
				return this._LastModifiedByScreenID;
			}
			set
			{
				this._LastModifiedByScreenID = value;
			}
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		protected DateTime? _LastModifiedDateTime;
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.LastModifiedDateTime, Enabled = false, IsReadOnly = true)]
		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime
		{
			get
			{
				return this._LastModifiedDateTime;
			}
			set
			{
				this._LastModifiedDateTime = value;
			}
		}
		#endregion
		#endregion

	}

	public static class ProformaStatus
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute()
				: base(
				new string[] { OnHold, PendingApproval, Open, Closed, Rejected },
				new string[] { Messages.OnHold, Messages.PendingApproval, Messages.Open, Messages.Closed, Messages.Rejected })
			{; }
		}
		public const string OnHold = "H";
		public const string PendingApproval = "A";
		public const string Open = "O";
		public const string Closed = "C";
		public const string Rejected = "R";

		public class onHold : PX.Data.BQL.BqlString.Constant<onHold>
		{
			public onHold() : base(OnHold) {; }
		}

		public class pendingApproval : PX.Data.BQL.BqlString.Constant<pendingApproval>
		{
			public pendingApproval() : base(PendingApproval) {; }
		}

		public class open : PX.Data.BQL.BqlString.Constant<open>
		{
			public open() : base(Open) {; }
		}

		public class closed : PX.Data.BQL.BqlString.Constant<closed>
		{
			public closed() : base(Closed) {; }
		}

		public class rejected : PX.Data.BQL.BqlString.Constant<rejected>
		{
			public rejected() : base(Rejected) {; }
		}
	}

	/// <summary>The projection of the <see cref="PMProforma" /> that contains the pro forma invoices that have been corrected.</summary>
	[PXCacheName(Messages.ProformaRevision)]
	[PXProjection(typeof(Select<PMProforma, Where<PMProforma.corrected, Equal<True>>>), Persistent = true)]
	public class PMProformaRevision : IBqlTable
	{
		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
		{
		}

		/// <inheritdoc cref="PMProforma.RefNbr"/>
		[PXDBString(PMProforma.refNbr.Length, IsUnicode = true, IsKey = true, InputMask = ">CCCCCCCCCCCCCCC", BqlField = typeof(PMProforma.refNbr))]
		public virtual String RefNbr
		{
			get;
			set;
		}
		#endregion
		#region RevisionID
		public abstract class revisionID : PX.Data.BQL.BqlInt.Field<revisionID> { }

		/// <inheritdoc cref="PMProforma.RevisionID"/>
		[PXUIField(DisplayName = "Revision")]
		[PXDBInt(IsKey = true, BqlField = typeof(PMProforma.revisionID))]
		public virtual Int32? RevisionID
		{
			get;
			set;
		}
		#endregion
		#region Description
		public abstract class description : PX.Data.BQL.BqlString.Field<description> { }

		/// <inheritdoc cref="PMProforma.Description"/>
		[PXDBString(255, IsUnicode = true, BqlField = typeof(PMProforma.description))]
		[PXUIField(DisplayName = "Description")]
		[PXFieldDescription]
		public virtual String Description
		{
			get;
			set;
		}
		#endregion
		#region CuryInfoID
		public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }

		/// <inheritdoc cref="PMProforma.CuryInfoID"/>
		[PXDBLong(BqlField = typeof(PMProforma.curyInfoID))]
		[CurrencyInfo]
		public virtual Int64? CuryInfoID
		{
			get;
			set;
		}
		#endregion
		#region CuryDocTotal
		public abstract class curyDocTotal : PX.Data.BQL.BqlDecimal.Field<curyDocTotal> { }

		/// <inheritdoc cref="PMProforma.CuryDocTotal"/>
		[PXDBCurrency(typeof(curyInfoID), typeof(docTotal), BqlField = typeof(PMProforma.curyDocTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Invoice Total")]
		public virtual Decimal? CuryDocTotal
		{
			get; set;
		}
		#endregion
		#region Total
		public abstract class docTotal : PX.Data.BQL.BqlDecimal.Field<docTotal> { }

		/// <inheritdoc cref="PMProforma.DocTotal"/>
		[PXDBBaseCury(BqlField = typeof(PMProforma.docTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Invoice Total in Base Currency")]
		public virtual Decimal? DocTotal
		{
			get; set;
		}
		#endregion
		#region CuryRetainageTotal
		public abstract class curyRetainageTotal : PX.Data.BQL.BqlDecimal.Field<curyRetainageTotal> { }

		/// <inheritdoc cref="PMProforma.CuryRetainageTotal"/>
		[PXCurrency(typeof(curyInfoID), typeof(retainageTotal))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Retainage Total", FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? CuryRetainageTotal
		{
			[PXDependsOnFields(typeof(curyRetainageDetailTotal), typeof(curyRetainageTaxTotal))]
			get { return CuryRetainageDetailTotal + CuryRetainageTaxTotal; }
		}
		#endregion
		#region RetainageTotal
		public abstract class retainageTotal : PX.Data.BQL.BqlDecimal.Field<retainageTotal> { }

		/// <inheritdoc cref="PMProforma.RetainageTotal"/>
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Retainage Total in Base Currency", FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? RetainageTotal
		{
			[PXDependsOnFields(typeof(retainageDetailTotal), typeof(retainageTaxTotal))]
			get { return RetainageDetailTotal + RetainageTaxTotal; }
		}
		#endregion
		#region CuryRetainageDetailTotal
		public abstract class curyRetainageDetailTotal : PX.Data.BQL.BqlDecimal.Field<curyRetainageDetailTotal> { }

		/// <inheritdoc cref="PMProforma.CuryRetainageDetailTotal"/>
		[PXDBCurrency(typeof(curyInfoID), typeof(retainageDetailTotal), BqlField = typeof(PMProforma.curyRetainageDetailTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Retainage Detail Total", FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? CuryRetainageDetailTotal
		{
			get; set;
		}
		#endregion
		#region RetainageTotal
		public abstract class retainageDetailTotal : PX.Data.BQL.BqlDecimal.Field<retainageDetailTotal> { }

		/// <inheritdoc cref="PMProforma.RetainageDetailTotal"/>
		[PXDBBaseCury(BqlField = typeof(PMProforma.retainageDetailTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Retainage Detail Total in Base Currency", FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? RetainageDetailTotal
		{
			get; set;
		}
		#endregion
		#region CuryRetainageTaxTotal
		public abstract class curyRetainageTaxTotal : PX.Data.BQL.BqlDecimal.Field<curyRetainageTaxTotal> { }

		/// <inheritdoc cref="PMProforma.CuryRetainageTaxTotal"/>
		[PXDBCurrency(typeof(curyInfoID), typeof(retainageTaxTotal), BqlField = typeof(PMProforma.curyRetainageTaxTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Retained Tax Total", FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? CuryRetainageTaxTotal
		{
			get; set;
		}
		#endregion
		#region RetainageTaxTotal
		public abstract class retainageTaxTotal : PX.Data.BQL.BqlDecimal.Field<retainageTaxTotal> { }

		/// <inheritdoc cref="PMProforma.RetainageTaxTotal"/>
		[PXDBBaseCury(BqlField = typeof(PMProforma.retainageTaxTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Retainage Tax Total in Base Currency", FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? RetainageTaxTotal
		{
			get; set;
		}
		#endregion
		#region CuryTaxTotal
		public abstract class curyTaxTotal : PX.Data.BQL.BqlDecimal.Field<curyTaxTotal> { }

		/// <inheritdoc cref="PMProforma.CuryTaxTotal"/>
		[PXDBCurrency(typeof(curyInfoID), typeof(taxTotal), BqlField = typeof(PMProforma.curyTaxTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Tax Total")]
		public virtual Decimal? CuryTaxTotal
		{
			get; set;
		}
		#endregion
		#region TaxTotal
		public abstract class taxTotal : PX.Data.BQL.BqlDecimal.Field<taxTotal> { }

		/// <inheritdoc cref="PMProforma.TaxTotal"/>
		[PXDBBaseCury(BqlField = typeof(PMProforma.taxTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Tax Total in Base Currency")]
		public virtual Decimal? TaxTotal
		{
			get; set;
		}
		#endregion
		#region ARInvoiceDocType
		public abstract class aRInvoiceDocType : PX.Data.BQL.BqlString.Field<aRInvoiceDocType> { }

		/// <inheritdoc cref="PMProforma.ARInvoiceDocType"/>
		[ARInvoiceType.List()]
		[PXUIField(DisplayName = "AR Doc. Type", Visibility = PXUIVisibility.Visible, Enabled = false)]
		[PXDBString(3, BqlField = typeof(PMProforma.aRInvoiceDocType))]
		public virtual String ARInvoiceDocType
		{
			get; set;
		}
		#endregion
		#region ARInvoiceRefNbr
		public abstract class aRInvoiceRefNbr : PX.Data.BQL.BqlString.Field<aRInvoiceRefNbr> { }

		/// <inheritdoc cref="PMProforma.ARInvoiceRefNbr"/>
		[PXUIField(DisplayName = "AR Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
		[PXSelector(typeof(Search<ARInvoice.refNbr, Where<ARInvoice.docType, Equal<Current<aRInvoiceDocType>>>>))]
		[PXDBString(15, IsUnicode = true, BqlField = typeof(PMProforma.aRInvoiceRefNbr))]
		public virtual String ARInvoiceRefNbr
		{
			get; set;
		}
		#endregion
		#region ReversedARInvoiceDocType
		public abstract class reversedARInvoiceDocType : PX.Data.BQL.BqlString.Field<reversedARInvoiceDocType> { }

		/// <inheritdoc cref="PMProforma.ReversedARInvoiceDocType"/>
		[ARInvoiceType.List()]
		[PXUIField(DisplayName = "Reversing Doc. Type", Visibility = PXUIVisibility.Visible, Enabled = false)]
		[PXDBString(3, BqlField = typeof(PMProforma.reversedARInvoiceDocType))]
		public virtual String ReversedARInvoiceDocType
		{
			get; set;
		}
		#endregion
		#region ReversedARInvoiceRefNbr
		public abstract class reversedARInvoiceRefNbr : PX.Data.BQL.BqlString.Field<reversedARInvoiceRefNbr> { }

		/// <inheritdoc cref="PMProforma.ReversedARInvoiceRefNbr"/>
		[PXUIField(DisplayName = "Reversing Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
		[PXSelector(typeof(Search<ARInvoice.refNbr, Where<ARInvoice.docType, Equal<Current<reversedARInvoiceDocType>>>>))]
		[PXDBString(15, IsUnicode = true, BqlField = typeof(PMProforma.reversedARInvoiceRefNbr))]
		public virtual String ReversedARInvoiceRefNbr
		{
			get; set;
		}
		#endregion
	}
}
