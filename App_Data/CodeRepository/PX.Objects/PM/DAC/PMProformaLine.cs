using PX.Data;
using PX.Data.EP;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.AP;
using PX.Objects.CM.Extensions;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.TX;
using System;

namespace PX.Objects.PM
{
	/// <summary>Is the base class for the <see cref="PMProforma">pro forma invoice</see> line. The class provides fields common to the <see cref="PMProformaProgressLine" /> and <see cref="PMProformaTransactLine" />
	/// types.</summary>
	[PXCacheName(Messages.ProformaLine)]
	[Serializable]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class PMProformaLine : IBqlTable, ISortOrder, IQuantify, IProjectFilter
	{
		#region Selected
		public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }
		protected bool? _Selected = false;

		[PXBool]
		[PXUnboundDefault(false)]
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

		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
		{
			public const int Length = 15;
		}
		protected String _RefNbr;

		/// <summary>
		/// The reference number of the parent <see cref="PMProforma">pro forma invoice</see>.
		/// </summary>
		[PXDBString(refNbr.Length, IsUnicode = true, IsKey = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXSelector(typeof(Search<PMProforma.refNbr>), Filterable = true)]
		[PXUIField(DisplayName = "Ref. Number", Visibility = PXUIVisibility.SelectorVisible)]
		[PXDBDefault(typeof(PMProforma.refNbr))]
		[PXParent(typeof(Select<PMProforma, Where<PMProforma.refNbr, Equal<Current<PMProformaLine.refNbr>>,
				And<PMProforma.revisionID, Equal<Current<PMProformaLine.revisionID>>>>>))]
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
		#region RevisionID
		public abstract class revisionID : PX.Data.BQL.BqlInt.Field<revisionID> { }

		/// <summary>The revision number of the parent <see cref="PMProforma">pro forma invoice</see>.</summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="PMProforma.RevisionID" /> field.
		/// </value>
		[PXUIField(DisplayName = "Revision", Visible = false)]
		[PXDBInt(IsKey = true)]
		[PXDefault(typeof(PMProforma.revisionID))]
		public virtual Int32? RevisionID
		{
			get;
			set;
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		protected Int32? _LineNbr;

		/// <summary>
		/// The original sequence number of the line among all the pro forma invoice lines.
		/// </summary>
		/// <remarks>The sequence of line numbers of the pro forma invoice lines belonging to a single document can include gaps.</remarks>
		[PXUIField(DisplayName = "Line Number", Visible = false)]
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		[PXLineNbr(typeof(PMProforma.lineCntr))]
		public virtual Int32? LineNbr
		{
			get
			{
				return this._LineNbr;
			}
			set
			{
				this._LineNbr = value;
			}
		}
		#endregion
		#region SortOrder
		public abstract class sortOrder : PX.Data.BQL.BqlInt.Field<sortOrder> { }
		protected Int32? _SortOrder;

		/// <summary>
		/// The sequence number of the line, which is used to sort the lines on the tab.
		/// These numbers are assigned automatically and are changed automatically when reordering lines by dragging them to appropriate positions.
		/// </summary>
		[PXUIField(DisplayName = "Sort Order", Visible = false)]
		[PXDBInt]
		public virtual Int32? SortOrder
		{
			get
			{
				return this._SortOrder;
			}
			set
			{
				this._SortOrder = value;
			}
		}
		#endregion
		#region Type
		public abstract class type : PX.Data.BQL.BqlString.Field<type> { }
		protected string _Type;

		/// <summary>The type of the pro forma invoice line.</summary>
		/// <value>The field can have one of the following values and regulates on which tab (<strong>Progress Billing</strong> or <strong>Time and Material
		/// Billing</strong>) of the Pro Forma Invoices (PM307000) form the invoice line appears: <c>"P"</c>: Progressive, <c>"T"</c>: Transaction</value>
		[PXDBString(1)]
		public virtual string Type
		{
			get
			{
				return this._Type;
			}
			set
			{
				this._Type = value;
			}
		}
		#endregion
		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		protected Int32? _BranchID;

		/// <summary>The identifier of the <see cref="Branch">branch</see> associated with the pro forma invoice line.</summary>
		/// <value>
		/// The branch is provided from the source defined by the <see cref="PMBillingRule.BranchSource">Use Destination Branch from</see> setting of the particular step of the billing rule.
		/// The value of this field corresponds to the value of the <see cref="Branch.BranchID" /> field.
		/// </value>
		[Branch(typeof(PMProforma.branchID))]
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
		#region Description
		public abstract class description : PX.Data.BQL.BqlString.Field<description> { }
		protected String _Description;

		/// <summary>
		/// The description of the line, which is provided by the billing rule and can be manually modified.
		/// </summary>
		[PXDBString(Constants.TranDescLength, IsUnicode = true)]
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
		#region ProjectID
		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
		protected Int32? _ProjectID;

		/// <summary>
		/// The identifier of the <see cref="PMProject">project</see> associated with the pro forma invoice line.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="PMProforma.ProjectID">project</see> of the parent pro forma invoice.
		/// The value of this field corresponds to the value of the <see cref="PMProject.ContractID"/> field.
		/// </value>
		[PXDBInt()]
		[PXDefault(typeof(PMProforma.projectID))]
		[PXForeignReference(typeof(Field<projectID>.IsRelatedTo<PMProject.contractID>))]
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
		#region TaskID
		public abstract class taskID : PX.Data.BQL.BqlInt.Field<taskID> { }
		protected Int32? _TaskID;

		/// <summary>
		/// The identifier of the <see cref="PMTask">task</see> associated with the pro forma invoice line.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="PMTask.TaskID"/> field.
		/// </value>
		[PXDefault(typeof(Search<PMTask.taskID, Where<PMTask.projectID, Equal<Current<projectID>>, And<PMTask.isDefault, Equal<True>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		[ActiveProjectTask(typeof(PMProformaLine.projectID), BatchModule.AR, DisplayName = "Project Task", AllowCompleted = true, Enabled = false)]
		[PXForeignReference(typeof(Field<taskID>.IsRelatedTo<PMTask.taskID>))]
		public virtual Int32? TaskID
		{
			get
			{
				return this._TaskID;
			}
			set
			{
				this._TaskID = value;
			}
		}
		#endregion
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		protected Int32? _InventoryID;

		/// <summary>
		/// The identifier of the <see cref="InventoryItem">inventory item</see> associated with the pro forma invoice line.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="InventoryItem.InventoryID"/> field.
		/// </value>
		[PXUIField(DisplayName = "Inventory ID", Enabled = false)]
		[PXDBInt()]
		[PMInventorySelector]
		public virtual Int32? InventoryID
		{
			get
			{
				return this._InventoryID;
			}
			set
			{
				this._InventoryID = value;
			}
		}
		#endregion
		#region CostCodeID
		public abstract class costCodeID : PX.Data.BQL.BqlInt.Field<costCodeID> { }
		protected Int32? _CostCodeID;

		/// <summary>The identifier of the <see cref="PMCostCode">cost code</see> associated with the pro forma invoice line.</summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="PMCostCode.CostCodeID" /> field.
		/// </value>
		[CostCode(typeof(accountID), typeof(taskID), GL.AccountType.Income, ReleasedField = typeof(released))]
		[PXForeignReference(typeof(Field<costCodeID>.IsRelatedTo<PMCostCode.costCodeID>))]
		public virtual Int32? CostCodeID
		{
			get
			{
				return this._CostCodeID;
			}
			set
			{
				this._CostCodeID = value;
			}
		}
		#endregion
		#region AccountGroupID
		public abstract class accountGroupID : PX.Data.BQL.BqlInt.Field<accountGroupID> { }
		protected Int32? _AccountGroupID;

		/// <summary>The identifier of the <see cref="PMAccountGroup">account group</see> associated with the pro forma invoice line.</summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="PMAccountGroup.GroupID" /> field.
		/// </value>
		[PXDBInt]
		[PXForeignReference(typeof(Field<accountGroupID>.IsRelatedTo<PMAccountGroup.groupID>))]
		public virtual Int32? AccountGroupID
		{
			get
			{
				return this._AccountGroupID;
			}
			set
			{
				this._AccountGroupID = value;
			}
		}
		#endregion
		#region ResourceID
		public abstract class resourceID : PX.Data.BQL.BqlInt.Field<resourceID> { }
		protected Int32? _ResourceID;

		/// <summary>
		/// The identifier of the employee associated with the pro forma invoice line.
		/// </summary>
		[PXEPEmployeeSelector]
		[PXDBInt()]
		[PXUIField(DisplayName = "Employee", Enabled = false)]
		public virtual Int32? ResourceID
		{
			get
			{
				return this._ResourceID;
			}
			set
			{
				this._ResourceID = value;
			}
		}
		#endregion
		#region VendorID
		public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		protected Int32? _VendorID;

		/// <summary>The identifier of the <see cref="Vendor">vendor</see> associated with the pro forma invoice line.</summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="CR.BAccount.BAccountID" /> field.
		/// </value>
		[Vendor(Enabled = false)]
		public virtual Int32? VendorID
		{
			get
			{
				return this._VendorID;
			}
			set
			{
				this._VendorID = value;
			}
		}
		#endregion
		#region Date
		public abstract class date : PX.Data.BQL.BqlDateTime.Field<date> { }
		protected DateTime? _Date;

		/// <summary>
		/// The date of the pro forma invoice line.
		/// </summary>
		/// <value>Defaults to the current <see cref="AccessInfo.BusinessDate">business date</see>.</value>
		[PXDBDate]
		[PXDefault(typeof(AccessInfo.businessDate))]
		[PXUIField(DisplayName = "Date", Visibility = PXUIVisibility.SelectorVisible, Required = true)]
		public virtual DateTime? Date
		{
			get
			{
				return this._Date;
			}
			set
			{
				this._Date = value;
			}
		}
		#endregion
		#region AccountID
		public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }

		/// <summary>The identifier of the sales <see cref="Account">account</see> associated with the pro forma invoice line.</summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="Account.AccountID" /> field.
		/// </value>
		[Account(typeof(PMProformaLine.branchID), typeof(Search<Account.accountID, Where<Account.accountGroupID, IsNotNull>>),
			DisplayName = "Sales Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description))]
		public virtual Int32? AccountID
		{
			get;
			set;
		}
		#endregion
		#region SubID
		public abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }
		protected Int32? _SubID;

		/// <summary>
		/// The identifier of the sales <see cref="Sub">subaccount</see> associated with the pro forma invoice line.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="Sub.SubID"/> field.
		/// </value>
		[SubAccount(typeof(PMProformaLine.accountID), typeof(PMProformaLine.branchID), true, DisplayName = "Sales Subaccount", Visibility = PXUIVisibility.Visible)]
		public virtual Int32? SubID
		{
			get
			{
				return this._SubID;
			}
			set
			{
				this._SubID = value;
			}
		}
		#endregion
		#region TaxCategoryID
		public abstract class taxCategoryID : PX.Data.BQL.BqlString.Field<taxCategoryID> { }

		/// <summary>The identifier of the <see cref="TaxCategory">tax category</see> associated with the pro forma invoice line.</summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="TaxCategory.TaxCategoryID" /> field.
		/// </value>
		[PXDBString(TaxCategory.taxCategoryID.Length, IsUnicode = true)]
		[PXUIField(DisplayName = "Tax Category")]
		[PMTax(typeof(PMProforma), typeof(PMTax), typeof(PMTaxTran))]
		[PMRetainedTax(typeof(PMProforma), typeof(PMTax), typeof(PMTaxTran))]
		[PXSelector(typeof(TaxCategory.taxCategoryID), DescriptionField = typeof(TaxCategory.descr))]
		[PXDefault(typeof(Search<InventoryItem.taxCategoryID,
			Where<InventoryItem.inventoryID, Equal<Current<PMProformaLine.inventoryID>>>>),
			PersistingCheck = PXPersistingCheck.Nothing, SearchOnDefault = false)]
		[PXRestrictor(typeof(Where<TaxCategory.active, Equal<True>>), TX.Messages.InactiveTaxCategory, typeof(TaxCategory.taxCategoryID))]
		public virtual String TaxCategoryID
		{
			get;
			set;
		}
		#endregion
		#region UOM
		public abstract class uOM : PX.Data.BQL.BqlString.Field<uOM> { }
		protected String _UOM;

		/// <summary>The <see cref="INUnit">unit of measure</see> for the <see cref="Qty">quantity</see> associated with the pro forma invoice line.</summary>
		[PXDefault(typeof(Search<InventoryItem.salesUnit, Where<InventoryItem.inventoryID, Equal<Current<inventoryID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PMUnit(typeof(PMProformaLine.inventoryID))]
		public virtual String UOM
		{
			get
			{
				return this._UOM;
			}
			set
			{
				this._UOM = value;
			}
		}
		#endregion
		#region CuryInfoID
		public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
		protected Int64? _CuryInfoID;

		/// <summary>An identifier of the <see cref="CurrencyInfo">currency info</see> object associated with the pro forma invoice line.</summary>
		[PXDBLong()]
		[CurrencyInfo(typeof(PMProforma.curyInfoID))]
		public virtual Int64? CuryInfoID
		{
			get
			{
				return this._CuryInfoID;
			}
			set
			{
				this._CuryInfoID = value;
			}
		}
		#endregion
		#region CuryUnitPrice
		public abstract class curyUnitPrice : PX.Data.BQL.BqlDecimal.Field<curyUnitPrice> { }

		/// <summary>
		/// The price of the item or the rate of the service.
		/// </summary>
		[PXDBCurrencyPriceCost(typeof(curyInfoID), typeof(unitPrice))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Unit Price")]
		public virtual Decimal? CuryUnitPrice
		{
			get; set;
		}
		#endregion
		#region UnitPrice
		public abstract class unitPrice : PX.Data.BQL.BqlDecimal.Field<unitPrice> { }

		/// <summary>
		/// The price of the item or the rate of the service in the base currency.
		/// </summary>
		[PXDBPriceCost]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Unit Price in Base Currency")]
		public virtual Decimal? UnitPrice
		{
			get; set;
		}
		#endregion
		#region CompletedPct
		public abstract class completedPct : PX.Data.BQL.BqlDecimal.Field<completedPct>
		{
			public const int Precision = 2;
		}

		/// <summary>
		/// The percentage of the revised budgeted amount of the revenue budget line of the project
		/// that has been invoiced by all the pro forma invoices of the project, including the current one.
		/// </summary>
		[PXDecimal(completedPct.Precision, MinValue = 0)]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Total Completed (%)")]
		public virtual decimal? CompletedPct
		{
			get;
			set;
		}
		#endregion
		#region CurrentInvoicedPct
		public abstract class currentInvoicedPct : PX.Data.BQL.BqlDecimal.Field<currentInvoicedPct> { }

		/// <summary>
		/// The percentage of the revised budgeted amount of the revenue budget line
		/// of the project that is invoiced by this pro forma invoice line.
		/// </summary>
		[PXDecimal(completedPct.Precision, MinValue = 0)]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Currently Invoiced (%)")]
		public virtual decimal? CurrentInvoicedPct
		{
			get;
			set;
		}
		#endregion
		#region BillableQty
		public abstract class billableQty : PX.Data.BQL.BqlDecimal.Field<billableQty> { }
		protected Decimal? _BillableQty;

		/// <summary>
		/// The quantity to bill the customer provided by the billing rule.
		/// </summary>
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Billed Quantity", Enabled = false)]
		public virtual Decimal? BillableQty
		{
			get
			{
				return this._BillableQty;
			}
			set
			{
				this._BillableQty = value;
			}
		}
		#endregion
		#region CuryBillableAmount
		public abstract class curyBillableAmount : PX.Data.BQL.BqlDecimal.Field<curyBillableAmount> { }

		/// <summary>The amount to bill the customer provided by the billing rule.</summary>
		[PXDBCurrency(typeof(curyInfoID), typeof(billableAmount))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Billed Amount", Enabled = false)]
		public virtual Decimal? CuryBillableAmount
		{
			get;
			set;
		}
		#endregion
		#region BillableAmount
		public abstract class billableAmount : PX.Data.BQL.BqlDecimal.Field<billableAmount> { }

		/// <summary>
		/// The amount to bill the customer provided by the billing rule in the base currency.
		/// </summary>
		protected Decimal? _BillableAmount;
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Billed Amount in Base Currency", Enabled = false)]
		public virtual Decimal? BillableAmount
		{
			get
			{
				return this._BillableAmount;
			}
			set
			{
				this._BillableAmount = value;
			}
		}
		#endregion
		#region Qty
		public abstract class qty : PX.Data.BQL.BqlDecimal.Field<qty> { }
		protected Decimal? _Qty;

		/// <summary>The quantity to bill the customer. The value can be manually modified.</summary>
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Quantity to Invoice")]
		public virtual Decimal? Qty
		{
			get
			{
				return this._Qty;
			}
			set
			{
				this._Qty = value;
			}
		}
		#endregion
		#region CuryAmount
		public abstract class curyAmount : PX.Data.BQL.BqlDecimal.Field<curyAmount> { }

		/// <summary>
		/// The line amount.
		/// </summary>
		[PXDBCurrency(typeof(curyInfoID), typeof(amount))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Amount", FieldClass = nameof(FeaturesSet.Construction))]
		public virtual Decimal? CuryAmount
		{
			get;
			set;
		}
		#endregion
		#region Amount
		public abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }

		/// <summary>
		/// The line amount in the base currency.
		/// </summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Amount", FieldClass = nameof(FeaturesSet.Construction))]
		public virtual Decimal? Amount
		{
			get;
			set;
		}
		#endregion
		#region CuryPrepaidAmount
		public abstract class curyPrepaidAmount : PX.Data.BQL.BqlDecimal.Field<curyPrepaidAmount> { }

		/// <summary>
		/// The field is reserved for a feature that is currently not supported.
		/// </summary>
		/// <exclude />
		[PXDBCurrency(typeof(curyInfoID), typeof(prepaidAmount))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Prepaid Applied")]
		public virtual Decimal? CuryPrepaidAmount
		{
			get;
			set;
		}
		#endregion
		#region PrepaidAmount
		public abstract class prepaidAmount : PX.Data.BQL.BqlDecimal.Field<prepaidAmount> { }

		/// <summary>
		/// The field is reserved for a feature that is currently not supported.
		/// </summary>
		/// <exclude />
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Prepaid Applied in Base Currency")]
		public virtual Decimal? PrepaidAmount
		{
			get;
			set;
		}
		#endregion
		#region CuryMaterialStoredAmount
		public abstract class curyMaterialStoredAmount : PX.Data.BQL.BqlDecimal.Field<curyMaterialStoredAmount> { }

		/// <summary>
		/// The amount of material stored.
		/// </summary>
		[PXDBCurrency(typeof(curyInfoID), typeof(materialStoredAmount))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Stored Material", FieldClass = nameof(FeaturesSet.Construction))]
		public virtual Decimal? CuryMaterialStoredAmount
		{
			get;
			set;
		}
		#endregion
		#region MaterialStoredAmount
		public abstract class materialStoredAmount : PX.Data.BQL.BqlDecimal.Field<materialStoredAmount> { }

		/// <summary>
		/// The amount of material stored in the base currency.
		/// </summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Stored Material in Base Currency", FieldClass = nameof(FeaturesSet.Construction))]
		public virtual Decimal? MaterialStoredAmount
		{
			get;
			set;
		}
		#endregion
		#region CuryLineTotal
		public abstract class curyLineTotal : PX.Data.BQL.BqlDecimal.Field<curyLineTotal> { }

		/// <summary>
		/// The amount to bill the customer. 
		/// </summary>
		[PXDBCurrency(typeof(curyInfoID), typeof(lineTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Amount to Invoice")]
		public virtual Decimal? CuryLineTotal
		{
			get;
			set;
		}
		#endregion
		#region LineTotal
		public abstract class lineTotal : PX.Data.BQL.BqlDecimal.Field<lineTotal> { }

		/// <summary>
		/// The amount to bill the customer in the base currency. 
		/// </summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Amount To Invoice in Base Currency")]
		public virtual Decimal? LineTotal
		{
			get;
			set;
		}
		#endregion
		#region RetainagePct
		public abstract class retainagePct : PX.Data.BQL.BqlDecimal.Field<retainagePct> { }

		/// <summary>
		/// The percent of the invoice line amount to be retained by the customer.
		/// </summary>
		[PXDBDecimal(2, MinValue = 0, MaxValue = 100)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Retainage (%)", FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual decimal? RetainagePct
		{
			get;
			set;
		}
		#endregion
		#region CuryRetainage
		public abstract class curyRetainage : PX.Data.BQL.BqlDecimal.Field<curyRetainage> { }

		/// <summary>
		/// The amount to be retained by the customer.
		/// </summary>
		/// <value>The amount is calculated by multiplying the values of <see cref="CuryLineTotal">Amount to Invoice</see> and <see cref="RetainagePct">Retainage</see>.</value>
		[PXFormula(typeof(Mult<curyLineTotal, Div<retainagePct, decimal100>>))]
		[PXDBCurrency(typeof(curyInfoID), typeof(retainage))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Retainage Amount", FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? CuryRetainage
		{
			get;
			set;
		}
		#endregion
		#region Retainage
		public abstract class retainage : PX.Data.BQL.BqlDecimal.Field<retainage> { }

		/// <summary>
		/// The amount to be retained by the customer in the base currency.
		/// </summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Retained Amount in Base Currency", FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? Retainage
		{
			get;
			set;
		}
		#endregion
		#region CuryAllocatedRetainedAmount
		/// <exclude/>
		public abstract class curyAllocatedRetainedAmount : PX.Data.BQL.BqlDecimal.Field<curyAllocatedRetainedAmount> { }
		/// <summary>The allocated retained amount.</summary>
		[PXDBCurrency(typeof(PMProformaLine.curyInfoID), typeof(PMProformaLine.allocatedRetainedAmount))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Allocated Retained Amount", Enabled = false, FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual Decimal? CuryAllocatedRetainedAmount
		{
			get;
			set;
		}
		#endregion

		#region AllocatedRetainedAmount
		/// <exclude/>
		public abstract class allocatedRetainedAmount : PX.Data.BQL.BqlDecimal.Field<allocatedRetainedAmount> { }
		/// <summary>The allocated retained amount (in the base currency).</summary>
		[PXDBBaseCury()]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Decimal? AllocatedRetainedAmount
		{
			get;
			set;
		}
		#endregion

		#region Option
		public abstract class option : PX.Data.BQL.BqlString.Field<option>
		{
			public const string BillNow = "N";
			public const string WriteOffRemainder = "C";
			public const string HoldRemainder = "U";
			public const string Writeoff = "X";

			public class holdRemainder : PX.Data.BQL.BqlString.Constant<holdRemainder>
			{
				public holdRemainder() : base(HoldRemainder) {; }
			}

			public class writeoff : PX.Data.BQL.BqlString.Constant<writeoff>
			{
				public writeoff() : base(Writeoff) {; }
			}

			public class bill : PX.Data.BQL.BqlString.Constant<bill>
			{
				public bill() : base(BillNow) {; }
			}

			public class writeOffRemainder : PX.Data.BQL.BqlString.Constant<writeOffRemainder>
			{
				public writeOffRemainder() : base(WriteOffRemainder) {; }
			}
		}
		protected string _Option;

		/// <summary>
		/// The status that defines how to bill the line.
		/// </summary>
		/// <value>
		/// The field can have one of the following values:
		/// <c>"N"</c>: Bill,
		/// <c>"C"</c>: Write Off Remainder,
		/// <c>"U"</c>: Hold Remainder,
		/// <c>"X"</c>: Write Off
		/// </value>
		[PXDefault(option.BillNow, PersistingCheck = PXPersistingCheck.Null)]
		[PXDBString()]
		[PXUIField(DisplayName = "Status")]
		public virtual string Option
		{
			get
			{
				return this._Option;
			}
			set
			{
				this._Option = value;
			}
		}
		#endregion
		#region Released
		public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }
		protected Boolean? _Released;

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the parent <see cref="PMProforma">pro forma invoice</see> has been released.
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
		/// Specifies (if set to <see langword="true" />) that the parent <see cref="PMProforma">pro forma invoice</see> has been corrected.
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
		#region IsPrepayment
		public abstract class isPrepayment : PX.Data.BQL.BqlBool.Field<isPrepayment> { }

		/// <summary>
		/// The field is reserved for a feature that is currently not supported.
		/// </summary>
		/// <exclude />
		[PXDBBool()]
		[PXDefault(false)]
		public virtual Boolean? IsPrepayment
		{
			get;
			set;
		}
		#endregion
		#region DefCode
		public abstract class defCode : PX.Data.BQL.BqlString.Field<defCode> { }

		/// <summary>
		/// The deferral code assigned to the stock item or non-stock item specified in this document line.
		/// </summary>
		[PXSelector(typeof(Search<DR.DRDeferredCode.deferredCodeID, Where<DR.DRDeferredCode.accountType, Equal<DR.DeferredAccountType.income>>>), DescriptionField = typeof(DR.DRDeferredCode.description))]
		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Deferral Code", FieldClass = "DEFFERED")]
		public virtual String DefCode
		{
			get;
			set;
		}
		#endregion


		#region ARInvoiceDocType
		public abstract class aRInvoiceDocType : PX.Data.BQL.BqlString.Field<aRInvoiceDocType> { }

		/// <summary>The type of the corresponding <see cref="ARInvoice">accounts receivable document</see> created on the release of the pro forma invoice.</summary>
		[PXDBString(3)]
		public virtual String ARInvoiceDocType
		{
			get; set;
		}
		#endregion
		#region ARInvoiceRefNbr
		public abstract class aRInvoiceRefNbr : PX.Data.BQL.BqlString.Field<aRInvoiceRefNbr> { }

		/// <summary>The reference number of the corresponding <see cref="AR.ARInvoice">accounts receivable document</see> created on the release of the pro forma invoice.</summary>
		[PXDBString(15, IsUnicode = true)]
		public virtual String ARInvoiceRefNbr
		{
			get; set;
		}
		#endregion
		#region ARInvoiceLineNbr
		public abstract class aRInvoiceLineNbr : PX.Data.BQL.BqlInt.Field<aRInvoiceLineNbr> { }

		/// <summary>The <see cref="AR.ARTran.LineNbr">line number</see> of the corresponding accounts receivable document created on the release of the pro forma invoice.</summary>
		[PXDBInt]
		public virtual Int32? ARInvoiceLineNbr
		{
			get; set;
		}
		#endregion

		#region ProgressBillingBase
		public abstract class progressBillingBase : Data.BQL.BqlDecimal.Field<progressBillingBase> { }

		[PXDBString]
		[PXUIField(DisplayName = Messages.ProgressBillingBase, Enabled = false)]
		[ProgressBillingBase.List]
		public string ProgressBillingBase { get; set; }
		#endregion

		#region System Columns
		#region NoteID
		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
		protected Guid? _NoteID;
		[PXNote(DescriptionField = typeof(PMProformaLine.refNbr))]
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

	/// <summary>Represents a pro forma invoice line with the <see cref="PMProformaLineType.Progressive">Progressive</see> type. The records of this type are edited through the <strong>Progress
	/// Billing</strong> tab of the Pro Forma Invoices (PM307000) form. The DAC is based on the <see cref="PMProformaLine" /> DAC and extends it with the fields relevant to the
	/// lines of this type.</summary>
	[PXCacheName(Messages.ProformaLine)]
	[PXBreakInheritance]
	public class PMProformaProgressLine : PMProformaLine
	{
		#region RefNbr
		public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
		{
			public const int Length = 15;
		}

		/// <summary>The reference number of the parent <see cref="PMProforma">pro forma invoice</see>.</summary>
		/// <value>
		/// The value of this field corresponds to the <see cref="PMProforma.RefNbr" /> field.
		/// </value>
		[PXDBString(refNbr.Length, IsUnicode = true, IsKey = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXSelector(typeof(Search<PMProforma.refNbr>), Filterable = true)]
		[PXUIField(DisplayName = "Ref. Number", Visibility = PXUIVisibility.SelectorVisible)]
		[PXDBDefault(typeof(PMProforma.refNbr))]
		[PXParent(typeof(Select<PMProforma, Where<PMProforma.refNbr, Equal<Current<PMProformaProgressLine.refNbr>>,
			And<PMProforma.revisionID, Equal<Current<PMProformaProgressLine.revisionID>>,
				And<Current<PMProformaProgressLine.type>, Equal<PMProformaLineType.progressive>>>>>))]
		public override String RefNbr
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
		public new abstract class revisionID : PX.Data.BQL.BqlInt.Field<revisionID> { }
		public new abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		#region Type
		public new abstract class type : PX.Data.BQL.BqlString.Field<type> { }

		/// <summary>The type of the pro forma invoice line.</summary>
		/// <value>
		/// Defaults to the <see cref="PMProformaLineType.Progressive">Progressive</see> type.
		/// </value>
		[PXDBString(1)]
		[PXDefault(PMProformaLineType.Progressive)]
		public override string Type
		{
			get
			{
				return this._Type;
			}
			set
			{
				this._Type = value;
			}
		}
		#endregion
		public new abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		public new abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		public new abstract class taskID : PX.Data.BQL.BqlInt.Field<taskID> { }
		#region AccountGroupID
		public new abstract class accountGroupID : PX.Data.BQL.BqlInt.Field<accountGroupID> { }

		/// <inheritdoc/>
		[PXDefault]
		[AccountGroup(typeof(Where<PMAccountGroup.type, Equal<GL.AccountType.income>>))]
		[PXForeignReference(typeof(Field<accountGroupID>.IsRelatedTo<PMAccountGroup.groupID>))]
		public override Int32? AccountGroupID
		{
			get
			{
				return this._AccountGroupID;
			}
			set
			{
				this._AccountGroupID = value;
			}
		}
		#endregion
		#region AccountID
		public new abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }

		/// <inheritdoc/>
		[Account(typeof(PMProformaProgressLine.branchID), typeof(Search<Account.accountID, Where<Account.accountGroupID, Equal<Current<PMProformaProgressLine.accountGroupID>>>>),
			DisplayName = "Sales Account", DescriptionField = typeof(Account.description))]
		public override Int32? AccountID
		{
			get;
			set;
		}
		#endregion
		#region SubID
		public new abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }

		/// <inheritdoc/>
		[SubAccount(typeof(PMProformaProgressLine.accountID), typeof(PMProformaProgressLine.branchID), true, DisplayName = "Sales Subaccount", Visibility = PXUIVisibility.Visible)]
		public override Int32? SubID
		{
			get
			{
				return this._SubID;
			}
			set
			{
				this._SubID = value;
			}
		}
		#endregion
		#region TaxCategoryID
		public new abstract class taxCategoryID : PX.Data.BQL.BqlString.Field<taxCategoryID> { }

		/// <summary>The identifier of the <see cref="TaxCategory">tax category</see> associated with the pro forma invoice line.</summary>
		/// <value>
		/// Defaults to the tax category of the corresponding revenue budget line.
		/// The value of this field corresponds to the value of the <see cref="TaxCategory.TaxCategoryID" /> field.
		/// </value>
		[PXDBString(TaxCategory.taxCategoryID.Length, IsUnicode = true)]
		[PXUIField(DisplayName = "Tax Category")]
		[PMTax(typeof(PMProforma), typeof(PMTax), typeof(PMTaxTran))]
		[PMRetainedTax(typeof(PMProforma), typeof(PMTax), typeof(PMTaxTran))]
		[PXSelector(typeof(TaxCategory.taxCategoryID), DescriptionField = typeof(TaxCategory.descr))]
		[PXDefault(typeof(Search<PMBudget.taxCategoryID,
			Where<PMBudget.projectID, Equal<Current<PMProformaProgressLine.projectID>>,
			And<PMBudget.projectTaskID, Equal<Current<PMProformaProgressLine.taskID>>,
			And<PMBudget.accountGroupID, Equal<Current<PMProformaProgressLine.accountGroupID>>,
			And<PMBudget.inventoryID, Equal<Current<PMProformaProgressLine.inventoryID>>,
			And<PMBudget.costCodeID, Equal<Current<PMProformaProgressLine.costCodeID>>>>>>>>),
			PersistingCheck = PXPersistingCheck.Nothing, SearchOnDefault = false)]
		[PXRestrictor(typeof(Where<TaxCategory.active, Equal<True>>), TX.Messages.InactiveTaxCategory, typeof(TaxCategory.taxCategoryID))]
		public override String TaxCategoryID
		{
			get;
			set;
		}
		#endregion
		#region CuryLineTotal
		public new abstract class curyLineTotal : PX.Data.BQL.BqlDecimal.Field<curyLineTotal> { }

		/// <inheritdoc/>
		[PXFormula(typeof(Sub<Add<curyAmount, curyMaterialStoredAmount>, curyPrepaidAmount>), typeof(SumCalc<PMProforma.curyProgressiveTotal>))]
		[PXDBCurrency(typeof(curyInfoID), typeof(lineTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Amount to Invoice")]
		public override Decimal? CuryLineTotal
		{
			get;
			set;
		}
		#endregion
		#region CuryRetainage
		public new abstract class curyRetainage : PX.Data.BQL.BqlDecimal.Field<curyRetainage> { }

		/// <inheritdoc/>
		[PXFormula(typeof(Mult<curyLineTotal, Div<retainagePct, decimal100>>), typeof(SumCalc<PMProforma.curyRetainageDetailTotal>))]
		[PXDBCurrency(typeof(curyInfoID), typeof(retainage))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Retainage Amount", FieldClass = nameof(FeaturesSet.Retainage))]
		public override Decimal? CuryRetainage
		{
			get;
			set;
		}
		#endregion
		#region UOM
		public new abstract class uOM : PX.Data.BQL.BqlString.Field<uOM> { }

		[PMUnit(typeof(inventoryID), Enabled = false)]
		public override string UOM { get; set; }
		#endregion

		public new abstract class isPrepayment : PX.Data.BQL.BqlBool.Field<isPrepayment> { }

		public new abstract class progressBillingBase : PX.Data.BQL.BqlString.Field<progressBillingBase> { }

		public new abstract class curyUnitPrice : PX.Data.BQL.BqlDecimal.Field<curyUnitPrice> { }

		//NON-DB Fields:

		#region CuryPreviouslyInvoiced
		public abstract class curyPreviouslyInvoiced : PX.Data.BQL.BqlDecimal.Field<curyPreviouslyInvoiced>
		{
		}

		/// <summary>The running total of the <see cref="CuryLineTotal">amount to invoice</see> column for all the lines of preceding pro forma invoices that refer to the same revenue budget line.
		/// The preceding pro forma invoices are the pro forma invoices that have a reference number that is less than the reference number of the current pro forma
		/// invoice, and have the same project budget key (that is, the same project task, account group, and optionally inventory item or cost code).</summary>
		[PXCurrency(typeof(curyInfoID), typeof(previouslyInvoiced), BaseCalc = false)]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Previously Invoiced Amount", Enabled = false)]
		public virtual Decimal? CuryPreviouslyInvoiced
		{
			get;
			set;
		}
		#endregion
		#region PreviouslyInvoiced
		public abstract class previouslyInvoiced : PX.Data.BQL.BqlDecimal.Field<previouslyInvoiced> { }

		/// <summary>The running total of the <strong>Amount to I<span>nvoice</span></strong> column in the base currency for all the lines of preceding pro forma invoices that
		/// refer to the same revenue budget line.</summary>
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Previously Invoiced in Base Currency", Enabled = false)]
		public virtual Decimal? PreviouslyInvoiced
		{
			get;
			set;
		}
		#endregion

		#region PreviouslyInvoicedQty
		public abstract class previouslyInvoicedQty : PX.Data.BQL.BqlDecimal.Field<previouslyInvoicedQty> { }

		/// <summary>
		/// The running total of the Quantity to Invoice column
		/// for all the lines of preceding pro forma invoices that refer to the same revenue budget line.
		/// </summary>
		[PXQuantity]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = Messages.PreviouslyInvoicedQuantity, Enabled = false)]
		public virtual Decimal? PreviouslyInvoicedQty
		{
			get;
			set;
		}
		#endregion
		#region ActualQty
		public abstract class actualQty : PX.Data.BQL.BqlDecimal.Field<actualQty> { }

		[PXQuantity]
		[PXUIField(DisplayName = "Actual Quantity", Enabled = false)]
		public virtual Decimal? ActualQty
		{
			get;
			set;
		}
		#endregion
	}

	/// <summary>Represents a pro forma invoice line with the <see cref="PMProformaLineType.Transaction">Transaction</see> type. The records of this type are edited through the <b>Time and Material</b>
	/// tab of the Pro Forma Invoices (PM307000) form. The DAC is based on the <see cref="PMProformaLine" /> DAC and extends it with the fields relevant to the lines of this type.</summary>
	[PXCacheName(Messages.ProformaLine)]
	[PXBreakInheritance]
	public class PMProformaTransactLine : PMProformaLine
	{
		#region RefNbr
		public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
		{
			public const int Length = 15;
		}

		/// <summary>The reference number of the parent <see cref="PMProforma">pro forma invoice</see>.</summary>
		/// <value>
		/// The value of this field corresponds to the <see cref="PMProforma.RefNbr" /> field.
		/// </value>
		[PXDBString(refNbr.Length, IsUnicode = true, IsKey = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXSelector(typeof(Search<PMProforma.refNbr>), Filterable = true)]
		[PXUIField(DisplayName = "Ref. Number", Visibility = PXUIVisibility.SelectorVisible)]
		[PXDBDefault(typeof(PMProforma.refNbr))]
		[PXParent(typeof(Select<PMProforma, Where<PMProforma.refNbr, Equal<Current<PMProformaTransactLine.refNbr>>,
			And<PMProforma.revisionID, Equal<Current<PMProformaTransactLine.revisionID>>,
			And<Current<PMProformaTransactLine.type>, Equal<PMProformaLineType.transaction>>>>>))]
		public override String RefNbr
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
		public new abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		public new abstract class revisionID : PX.Data.BQL.BqlInt.Field<revisionID> { }
		#region Type
		public new abstract class type : PX.Data.BQL.BqlString.Field<type> { }

		/// <summary>The type of the pro forma invoice line.</summary>
		/// <value>
		/// Defaults to the <see cref="PMProformaLineType.Transaction">Transaction</see> type.
		/// </value>
		[PXDBString(1)]
		[PXDefault(PMProformaLineType.Transaction)]
		public override string Type
		{
			get
			{
				return this._Type;
			}
			set
			{
				this._Type = value;
			}
		}
		#endregion
		public new abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		public new abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		public new abstract class taskID : PX.Data.BQL.BqlInt.Field<taskID> { }
		public new abstract class accountGroupID : PX.Data.BQL.BqlInt.Field<accountGroupID> { }
		public new abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		#region CuryAmount
		public new abstract class curyAmount : PX.Data.BQL.BqlDecimal.Field<curyAmount> { }

		/// <summary>
		/// The line amount.
		/// </summary>
		/// <value>
		/// Calculated by multiplying the values of <see cref="PMProformaLine.Qty">Quantity to Invoice</see> and <see cref="PMProformaLine.CuryUnitPrice">Unit Price</see>.
		/// </value>
		[PXFormula(typeof(Mult<qty, curyUnitPrice>))]
		[PXDBCurrency(typeof(curyInfoID), typeof(amount))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Amount")]
		public override Decimal? CuryAmount
		{
			get;
			set;
		}
		#endregion
		#region CuryLineTotal
		public new abstract class curyLineTotal : PX.Data.BQL.BqlDecimal.Field<curyLineTotal> { }

		/// <inheritdoc/>
		[PXFormula(typeof(Sub<Add<curyAmount, curyMaterialStoredAmount>, curyPrepaidAmount>), typeof(SumCalc<PMProforma.curyTransactionalTotal>))]
		[PXDBCurrency(typeof(curyInfoID), typeof(lineTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Amount to Invoice")]
		public override Decimal? CuryLineTotal
		{
			get;
			set;
		}
		#endregion
		#region CuryRetainage
		public new abstract class curyRetainage : PX.Data.BQL.BqlDecimal.Field<curyRetainage> { }

		/// <inheritdoc/>
		[PXFormula(typeof(Mult<curyLineTotal, Div<retainagePct, decimal100>>), typeof(SumCalc<PMProforma.curyRetainageDetailTotal>))]
		[PXDBCurrency(typeof(curyInfoID), typeof(retainage))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Retainage Amount", FieldClass = nameof(FeaturesSet.Retainage))]
		public override Decimal? CuryRetainage
		{
			get;
			set;
		}
		#endregion

		#region AccountID
		public new abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }

		/// <inheritdoc/>
		[Account(typeof(PMProformaTransactLine.branchID), typeof(Search2<Account.accountID,
			InnerJoin<PMAccountGroup, On<Account.accountGroupID, Equal<PMAccountGroup.groupID>>>,
			Where2<Where<Current<PMProformaTransactLine.isPrepayment>, Equal<True>, And<Account.accountGroupID, Equal<Current<PMProformaTransactLine.accountGroupID>>>>,
			Or<Where<Current<PMProformaTransactLine.isPrepayment>, Equal<False>, And<Account.accountGroupID, IsNotNull>>>>>),
			DisplayName = "Sales Account",
			DescriptionField = typeof(Account.description),
			AvoidControlAccounts = true)]
		[PXDefault(typeof(Search2<InventoryItem.salesAcctID,
			InnerJoin<Account, On<InventoryItem.salesAcctID, Equal<Account.accountID>>,
			InnerJoin<PMAccountGroup, On<Account.accountGroupID, Equal<PMAccountGroup.groupID>, And<PMAccountGroup.type, Equal<AccountType.income>>>>>,
			Where<InventoryItem.inventoryID, Equal<Current<PMProformaTransactLine.inventoryID>>>>))]
		public override Int32? AccountID
		{
			get;
			set;
		}
		#endregion
		#region SubID
		public new abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }

		/// <inheritdoc/>
		[SubAccount(typeof(PMProformaTransactLine.accountID), typeof(PMProformaTransactLine.branchID), true, DisplayName = "Sales Subaccount", Visibility = PXUIVisibility.Visible)]
		public override Int32? SubID
		{
			get
			{
				return this._SubID;
			}
			set
			{
				this._SubID = value;
			}
		}
		#endregion

		#region TaxCategoryID
		public new abstract class taxCategoryID : PX.Data.BQL.BqlString.Field<taxCategoryID> { }

		/// <inheritdoc/>
		[PXDBString(TaxCategory.taxCategoryID.Length, IsUnicode = true)]
		[PXUIField(DisplayName = "Tax Category")]
		[PXSelector(typeof(TaxCategory.taxCategoryID), DescriptionField = typeof(TaxCategory.descr))]
		[PMTax(typeof(PMProforma), typeof(PMTax), typeof(PMTaxTran))]
		[PMRetainedTax(typeof(PMProforma), typeof(PMTax), typeof(PMTaxTran))]
		[PXRestrictor(typeof(Where<TaxCategory.active, Equal<True>>), TX.Messages.InactiveTaxCategory, typeof(TaxCategory.taxCategoryID))]
		public override String TaxCategoryID
		{
			get;
			set;
		}
		#endregion
		#region UOM
		public new abstract class uOM : PX.Data.BQL.BqlString.Field<uOM> { }

		/// <inheritdoc/>
		[PXDefault(typeof(Search<InventoryItem.salesUnit, Where<InventoryItem.inventoryID, Equal<Current<inventoryID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PMUnit(typeof(PMProformaLine.inventoryID))]
		public override String UOM
		{
			get
			{
				return this._UOM;
			}
			set
			{
				this._UOM = value;
			}
		}
		#endregion

		public new abstract class isPrepayment : PX.Data.BQL.BqlBool.Field<isPrepayment> { }

		public new abstract class option : PX.Data.BQL.BqlString.Field<option> { }


		#region CuryMaxAmount
		public abstract class curyMaxAmount : PX.Data.BQL.BqlDecimal.Field<curyMaxAmount> { }


		/// <summary>
		/// The billing limit amount (<see cref="PMRevenueBudget.CuryMaxAmount">Maximum Amount</see>)
		/// of the corresponding revenue budget line of the project.
		/// If no billing limit amount is defined for the revenue budget line of the project,
		/// the Max Limit Amount of each corresponding pro forma invoice line is 0.
		/// </summary>
		[PXCurrency(typeof(curyInfoID), typeof(maxAmount))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Max Limit Amount", Enabled = false, Visible = false)]
		public virtual Decimal? CuryMaxAmount
		{
			get;
			set;
		}
		#endregion
		#region MaxAmount
		public abstract class maxAmount : PX.Data.BQL.BqlDecimal.Field<maxAmount> { }

		/// <summary>
		/// The billing limit amount (<see cref="PMRevenueBudget.CuryMaxAmount">Maximum Amount</see>)
		/// of the corresponding revenue budget line of the project in the base currency.
		/// </summary>
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Max Limit Amount in Base Currency", Enabled = false, Visible = false)]
		public virtual Decimal? MaxAmount
		{
			get;
			set;
		}
		#endregion
		#region CuryAvailableAmount
		public abstract class curyAvailableAmount : PX.Data.BQL.BqlDecimal.Field<curyAvailableAmount> { }

		/// <summary>
		/// The maximum amount available to bill the customer based on the billing limit amount
		/// of the corresponding revenue budget line of the project.
		/// </summary>
		[PXCurrency(typeof(curyInfoID), typeof(availableAmount))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Max Available Amount", Enabled = false, Visible = false)]
		public virtual Decimal? CuryAvailableAmount
		{
			get;
			set;
		}
		#endregion
		#region AvailableAmount
		public abstract class availableAmount : PX.Data.BQL.BqlDecimal.Field<availableAmount> { }

		/// <summary>
		/// The maximum amount in the base currency available to bill the customer based on the billing limit amount
		/// of the corresponding revenue budget line of the project.
		/// </summary>
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Max Available Amount in Base Currency", Enabled = false, Visible = false)]
		public virtual Decimal? AvailableAmount
		{
			get;
			set;
		}
		#endregion
		#region CuryOverflowAmount
		public abstract class curyOverflowAmount : PX.Data.BQL.BqlDecimal.Field<curyOverflowAmount> { }

		/// <summary>
		/// The amount that exceeds the billing limit.
		/// </summary>
		/// <value>
		/// The amount is calculated as the difference between the <see cref="CuryLineTotal">Amount to Invoice</see> and
		/// <see cref="CuryAvailableAmount">Max Available Amount</see>.
		/// If this difference is negative - that is, if the <see cref="CuryAvailableAmount">Max Available Amount</see> is greater than the
		/// <see cref="CuryLineTotal">Amount to Invoice</see> - the Over-Limit Amount is 0.
		/// The invoice lines for which the Over-Limit Amount becomes nonzero exceed the limit.
		/// </value>
		[PXCurrency(typeof(curyInfoID), typeof(overflowAmount))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Over-Limit Amount", Enabled = false, Visible = false)]
		public virtual Decimal? CuryOverflowAmount
		{
			get;
			set;
		}
		#endregion
		#region OverflowAmount
		public abstract class overflowAmount : PX.Data.BQL.BqlDecimal.Field<overflowAmount> { }

		/// <summary>The amount that exceeds the billing limit in the base currency.</summary>
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Overflow Amount in Base Currency", Enabled = false, Visible = false)]
		public virtual Decimal? OverflowAmount
		{
			get;
			set;
		}
		#endregion
	}

	public static class PMProformaLineType
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute()
				: base(
				new string[] { Progressive, Transaction },
				new string[] { Messages.Progressive, Messages.Transaction })
			{; }
		}
		public const string Progressive = "P";
		public const string Transaction = "T";

		public class progressive : PX.Data.BQL.BqlString.Constant<progressive>
		{
			public progressive() : base(Progressive) {; }
		}
		public class transaction : PX.Data.BQL.BqlString.Constant<transaction>
		{
			public transaction() : base(Transaction) {; }
		}
	}

	/// <summary>
	/// Used in Reports to calculate Previously invoiced amount.
	/// </summary>
	[PXCacheName(Messages.ProformaLine)]
	[Serializable]
	[PXProjection(typeof(Select4<PMProformaLine,
		Where<PMProformaLine.type, Equal<PMProformaLineType.progressive>,
		And<PMProformaLine.corrected, NotEqual<True>>>,
		Aggregate<GroupBy<PMProformaLine.projectID,
			GroupBy<PMProformaLine.refNbr,
			GroupBy<PMProformaLine.taskID,
			GroupBy<PMProformaLine.accountGroupID,
			GroupBy<PMProformaLine.inventoryID,
			GroupBy<PMProformaLine.costCodeID,
			Sum<PMProformaLine.curyLineTotal,
			Sum<PMProformaLine.lineTotal,
			Sum<PMProformaLine.curyMaterialStoredAmount,
			Sum<PMProformaLine.materialStoredAmount,
			Sum<PMProformaLine.curyRetainage,
			Sum<PMProformaLine.retainage>>>>>>>>>>>>>>), Persistent = false)]
	public class PMProgressLineTotal : IBqlTable
	{
		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		[PXDBString(PMProformaLine.refNbr.Length, IsUnicode = true, IsKey = true, BqlField = typeof(PMProformaLine.refNbr))]
		public virtual String RefNbr
		{
			get;
			set;
		}
		#endregion
		#region ProjectID
		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
		protected Int32? _ProjectID;
		[PXDBInt(IsKey = true, BqlField = typeof(PMProformaLine.projectID))]
		[PXForeignReference(typeof(Field<projectID>.IsRelatedTo<PMProject.contractID>))]
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
		#region TaskID
		public abstract class taskID : PX.Data.BQL.BqlInt.Field<taskID> { }
		protected Int32? _TaskID;
		[PXDBInt(IsKey = true, BqlField = typeof(PMProformaLine.taskID))]
		[PXForeignReference(typeof(Field<taskID>.IsRelatedTo<PMTask.taskID>))]
		public virtual Int32? TaskID
		{
			get
			{
				return this._TaskID;
			}
			set
			{
				this._TaskID = value;
			}
		}
		#endregion
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		protected Int32? _InventoryID;
		[PXDBInt(BqlField = typeof(PMProformaLine.inventoryID))]
		public virtual Int32? InventoryID
		{
			get
			{
				return this._InventoryID;
			}
			set
			{
				this._InventoryID = value;
			}
		}
		#endregion
		#region CostCodeID
		public abstract class costCodeID : PX.Data.BQL.BqlInt.Field<costCodeID> { }
		protected Int32? _CostCodeID;
		[PXDBInt(BqlField = typeof(PMProformaLine.costCodeID))]
		[PXForeignReference(typeof(Field<costCodeID>.IsRelatedTo<PMCostCode.costCodeID>))]
		public virtual Int32? CostCodeID
		{
			get
			{
				return this._CostCodeID;
			}
			set
			{
				this._CostCodeID = value;
			}
		}
		#endregion
		#region AccountGroupID
		public abstract class accountGroupID : PX.Data.BQL.BqlInt.Field<accountGroupID> { }
		protected Int32? _AccountGroupID;
		[PXDBInt(IsKey = true, BqlField = typeof(PMProformaLine.accountGroupID))]
		[PXForeignReference(typeof(Field<accountGroupID>.IsRelatedTo<PMAccountGroup.groupID>))]
		public virtual Int32? AccountGroupID
		{
			get
			{
				return this._AccountGroupID;
			}
			set
			{
				this._AccountGroupID = value;
			}
		}
		#endregion
		#region CuryMaterialStoredAmount
		public abstract class curyMaterialStoredAmount : PX.Data.BQL.BqlDecimal.Field<curyMaterialStoredAmount>
		{
		}
		[PXDBBaseCury(BqlField = typeof(PMProformaLine.curyMaterialStoredAmount))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Stored Material")]
		public virtual Decimal? CuryMaterialStoredAmount
		{
			get; set;
		}
		#endregion
		#region MaterialStoredAmount
		public abstract class materialStoredAmount : PX.Data.BQL.BqlDecimal.Field<materialStoredAmount> { }
		[PXDBBaseCury(BqlField = typeof(PMProformaLine.materialStoredAmount))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Stored Material in Base Currency")]
		public virtual Decimal? MaterialStoredAmount
		{
			get; set;
		}
		#endregion
		#region CuryLineTotal
		public abstract class curyLineTotal : PX.Data.BQL.BqlDecimal.Field<curyLineTotal>
		{
		}
		[PXDBBaseCury(BqlField = typeof(PMProformaLine.curyLineTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Total")]
		public virtual Decimal? CuryLineTotal
		{
			get; set;
		}
		#endregion
		#region LineTotal
		public abstract class lineTotal : PX.Data.BQL.BqlDecimal.Field<lineTotal> { }
		[PXDBBaseCury(BqlField = typeof(PMProformaLine.lineTotal))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Total in Base Currency")]
		public virtual Decimal? LineTotal
		{
			get; set;
		}
		#endregion
		#region CuryRetainage
		public abstract class curyRetainage : PX.Data.BQL.BqlDecimal.Field<curyRetainage>
		{
		}
		[PXDBBaseCury(BqlField = typeof(PMProformaLine.curyRetainage))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Retainage")]
		public virtual Decimal? CuryRetainage
		{
			get; set;
		}
		#endregion
		#region Retainage
		public abstract class retainage : PX.Data.BQL.BqlDecimal.Field<retainage> { }
		[PXDBBaseCury(BqlField = typeof(PMProformaLine.retainage))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Retainage in Base Currency")]
		public virtual Decimal? Retainage
		{
			get; set;
		}
		#endregion
	}

	[PXCacheName(Messages.ProformaLine)]
	[PXBreakInheritance]
	public class PMProformaLineWithPrevious : PMProformaLine
	{
		public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		public new abstract class revisionID : PX.Data.BQL.BqlInt.Field<revisionID> { }
		public new abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		public new abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
		public new abstract class taskID : PX.Data.BQL.BqlInt.Field<taskID> { }
		public new abstract class accountGroupID : PX.Data.BQL.BqlInt.Field<accountGroupID> { }
		public new abstract class costCodeID : PX.Data.BQL.BqlInt.Field<costCodeID> { }
		public new abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		public new abstract class progressBillingBase : Data.BQL.BqlDecimal.Field<progressBillingBase> { }
		public new abstract class uOM : PX.Data.BQL.BqlString.Field<uOM> { }
		public new abstract class qty : PX.Data.BQL.BqlDecimal.Field<qty> { }

		#region CuryPreviouslyInvoiced
		public abstract class curyPreviouslyInvoiced : PX.Data.BQL.BqlDecimal.Field<curyPreviouslyInvoiced>
		{
		}

		[PXDBScalar(typeof(Search4<PMProformaLine.curyLineTotal,
			Where<PMProformaLine.type, Equal<PMProformaLineType.progressive>,
			And<PMProformaLine.refNbr, Less<PMProformaLineWithPrevious.refNbr>,
			And<PMProformaLine.projectID, Equal<PMProformaLineWithPrevious.projectID>,
			And<PMProformaLine.taskID, Equal<PMProformaLineWithPrevious.taskID>,
			And<PMProformaLine.accountGroupID, Equal<PMProformaLineWithPrevious.accountGroupID>,
			And<PMProformaLine.costCodeID, Equal<PMProformaLineWithPrevious.costCodeID>,
			And<PMProformaLine.inventoryID, Equal<PMProformaLineWithPrevious.inventoryID>,
			And<PMProformaLine.released, Equal<True>,
			And<PMProformaLine.corrected, NotEqual<True>>>>>>>>>>,
			Aggregate<Sum<PMProformaLine.curyLineTotal>>>))]
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Previously Invoiced", Enabled = false)]
		public virtual Decimal? CuryPreviouslyInvoiced
		{
			get;
			set;
		}
		#endregion

		#region PreviouslyInvoiced
		public abstract class previouslyInvoiced : PX.Data.BQL.BqlDecimal.Field<previouslyInvoiced> { }


		[PXDBScalar(typeof(Search4<PMProformaLine.lineTotal,
			Where<PMProformaLine.type, Equal<PMProformaLineType.progressive>,
			And<PMProformaLine.refNbr, Less<PMProformaLineWithPrevious.refNbr>,
			And<PMProformaLine.projectID, Equal<PMProformaLineWithPrevious.projectID>,
			And<PMProformaLine.taskID, Equal<PMProformaLineWithPrevious.taskID>,
			And<PMProformaLine.accountGroupID, Equal<PMProformaLineWithPrevious.accountGroupID>,
			And<PMProformaLine.costCodeID, Equal<PMProformaLineWithPrevious.costCodeID>,
			And<PMProformaLine.inventoryID, Equal<PMProformaLineWithPrevious.inventoryID>,
			And<PMProformaLine.released, Equal<True>,
			And<PMProformaLine.corrected, NotEqual<True>>>>>>>>>>,
			Aggregate<Sum<PMProformaLine.lineTotal>>>))]
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Previously Invoiced in Base Currency", Enabled = false)]
		public virtual Decimal? PreviouslyInvoiced
		{
			get;
			set;
		}
		#endregion

		#region PreviouslyInvoicedQty
		public abstract class previouslyInvoicedQty : PX.Data.BQL.BqlDecimal.Field<previouslyInvoicedQty> { }

		/// <summary>
		/// The running total of the Quantity to Invoice column
		/// for all the lines of preceding pro forma invoices that refer to the same revenue budget line.
		/// </summary>
		[PXDBScalar(typeof(Search4<PMProformaLine.qty,
			Where<PMProformaLine.type, Equal<PMProformaLineType.progressive>,
			And<PMProformaLine.refNbr, Less<PMProformaLineWithPrevious.refNbr>,
			And<PMProformaLine.projectID, Equal<PMProformaLineWithPrevious.projectID>,
			And<PMProformaLine.taskID, Equal<PMProformaLineWithPrevious.taskID>,
			And<PMProformaLine.accountGroupID, Equal<PMProformaLineWithPrevious.accountGroupID>,
			And<PMProformaLine.costCodeID, Equal<PMProformaLineWithPrevious.costCodeID>,
			And<PMProformaLine.inventoryID, Equal<PMProformaLineWithPrevious.inventoryID>,
			And<PMProformaLine.uOM, Equal<PMProformaLineWithPrevious.uOM>,
			And<PMProformaLine.released, Equal<True>,
			And<PMProformaLine.corrected, NotEqual<True>>>>>>>>>>>,
			Aggregate<Sum<PMProformaLine.qty>>>))]
		[PXQuantity]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = Messages.PreviouslyInvoicedQuantity, Enabled = false)]
		public virtual Decimal? PreviouslyInvoicedQty
		{
			get;
			set;
		}
		#endregion

		#region QuantityBaseCompletedPct
		public abstract class quantityBaseCompletedPct : PX.Data.BQL.BqlDecimal.Field<quantityBaseCompletedPct> { }

		[PXDecimal]
		[ProgressCompleted]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Total Completed (%)", Enabled = false)]
		public decimal? QuantityBaseCompletedPct { get; set; }
		#endregion
	}
}
