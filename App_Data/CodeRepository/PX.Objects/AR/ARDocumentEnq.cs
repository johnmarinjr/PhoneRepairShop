using PX.Api.ContractBased.OptimizedExport;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AR.BQL;
using PX.Objects.AR.Repositories;
using PX.Objects.CA;
using PX.Objects.CM.Extensions;
using PX.Objects.Common;
using PX.Objects.Common.Attributes;
using PX.Objects.Common.MigrationMode;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.GL.Attributes;
using PX.Objects.GL.FinPeriods;
using PX.Objects.Common.Utility;
using PX.Objects.GL.FinPeriods.TableDefinition;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.AR
{
	[TableAndChartDashboardType]
	public class ARDocumentEnq : PXGraph<ARDocumentEnq>
	{
		#region Internal Types
		[Serializable]
		public partial class ARDocumentFilter : IBqlTable
		{
			#region OrganizationID
			public abstract class organizationID : PX.Data.BQL.BqlInt.Field<organizationID> { }

			[Organization(false, Required = false)]
			public int? OrganizationID { get; set; }
			#endregion
			#region BranchID
			public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }

			[BranchOfOrganization(typeof(ARDocumentFilter.organizationID), false)]
			public int? BranchID { get; set; }
			#endregion
			#region OrgBAccountID
			public abstract class orgBAccountID : IBqlField { }

			[OrganizationTree(typeof(organizationID), typeof(branchID), onlyActive: false)]
			public int? OrgBAccountID { get; set; }
			#endregion
			#region CustomerID
			public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
			protected int? _CustomerID;
			[PXDefault()]
			[Customer(DescriptionField = typeof(Customer.acctName))]
			public virtual int? CustomerID
			{
				get
				{
					return this._CustomerID;
				}
				set
				{
					this._CustomerID = value;
				}
			}
			#endregion
			#region ARAcctID
			public abstract class aRAcctID : PX.Data.BQL.BqlInt.Field<aRAcctID> { }
			protected int? _ARAcctID;
			[GL.Account(null, typeof(Search5<Account.accountID,
					InnerJoin<ARHistory, On<Account.accountID, Equal<ARHistory.accountID>>>,
					Where<Match<Current<AccessInfo.userName>>>,
					Aggregate<GroupBy<Account.accountID>>>),
			   DisplayName = "AR Account", DescriptionField = typeof(GL.Account.description))]
			public virtual int? ARAcctID
			{
				get
				{
					return this._ARAcctID;
				}
				set
				{
					this._ARAcctID = value;
				}
			}
			#endregion
			#region ARSubID
			public abstract class aRSubID : PX.Data.BQL.BqlInt.Field<aRSubID> { }
			protected int? _ARSubID;
			[GL.SubAccount(DisplayName = "AR Sub.", DescriptionField = typeof(GL.Sub.description))]
			public virtual int? ARSubID
			{
				get
				{
					return this._ARSubID;
				}
				set
				{
					this._ARSubID = value;
				}
			}
			#endregion
			#region SubCD
			public abstract class subCD : PX.Data.BQL.BqlString.Field<subCD> { }
			protected string _SubCD;
			[PXDBString(30, IsUnicode = true)]
			[PXUIField(DisplayName = "AR Subaccount", Visibility = PXUIVisibility.Invisible, FieldClass = SubAccountAttribute.DimensionName)]
			[PXDimension("SUBACCOUNT", ValidComboRequired = false)]
			public virtual string SubCD
			{
				get
				{
					return this._SubCD;
				}
				set
				{
					this._SubCD = value;
				}
			}
			#endregion
			#region UseMasterCalendar
			public abstract class useMasterCalendar : PX.Data.BQL.BqlBool.Field<useMasterCalendar> { }

			[PXDBBool]
			[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = Common.Messages.UseMasterCalendar)]
			[PXUIVisible(typeof(FeatureInstalled<FeaturesSet.multipleCalendarsSupport>))]
			public bool? UseMasterCalendar { get; set; }
			#endregion

			#region Period
			public abstract class period : PX.Data.BQL.BqlString.Field<period> { }

			[AnyPeriodFilterable(null, null,
				branchSourceType: typeof(ARDocumentFilter.branchID),
				organizationSourceType: typeof(ARDocumentFilter.organizationID),
				useMasterCalendarSourceType: typeof(ARDocumentFilter.useMasterCalendar),
				redefaultOrRevalidateOnOrganizationSourceUpdated: false)]
			[PXUIField(DisplayName = "Period", Visibility = PXUIVisibility.Visible)]
			public virtual string Period
				{
				get;
				set;
			}
			#endregion
			#region MasterFinPeriodID
			public abstract class masterFinPeriodID : PX.Data.BQL.BqlString.Field<masterFinPeriodID> { }
			[Obsolete("This is an absolete field. It will be removed in 2019R2")]
			[PeriodID]
			public virtual string MasterFinPeriodID { get; set; }
			#endregion
			#region DocType
			public abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
			protected string _DocType;
			[PXDBString(3, IsFixed = true)]
			[PXDefault()]
			[ARDocType.List()]
			[PXUIField(DisplayName = "Type")]
			public virtual string DocType
			{
				get
				{
					return this._DocType;
				}
				set
				{
					this._DocType = value;
				}
			}
			#endregion
			#region ShowAllDocs
			public abstract class showAllDocs : PX.Data.BQL.BqlBool.Field<showAllDocs> { }
			protected bool? _ShowAllDocs;
			[PXDBBool()]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Show All Documents")]
			public virtual bool? ShowAllDocs
			{
				get
				{
					return this._ShowAllDocs;
				}
				set
				{
					this._ShowAllDocs = value;
				}
			}
			#endregion
			#region IncludeUnreleased
			public abstract class includeUnreleased : PX.Data.BQL.BqlBool.Field<includeUnreleased> { }
			protected bool? _IncludeUnreleased;
			[PXDBBool()]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Include Unreleased Documents")]
			public virtual bool? IncludeUnreleased
			{
				get
				{
					return this._IncludeUnreleased;
				}
				set
				{
					this._IncludeUnreleased = value;
				}
			}
			#endregion
			#region CuryID
			public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
			protected string _CuryID;
			[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL")]
			[PXSelector(typeof(CM.Currency.curyID), CacheGlobal = true)]
			[PXUIField(DisplayName = "Currency")]
			public virtual string CuryID
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
			#region SubCD Wildcard
			public abstract class subCDWildcard : PX.Data.BQL.BqlString.Field<subCDWildcard> { };
			[PXDBString(30, IsUnicode = true)]
			public virtual string SubCDWildcard
			{
				get
				{
					return SubCDUtils.CreateSubCDWildcard(this._SubCD, SubAccountAttribute.DimensionName);
				}
			}



			#endregion
			#region RefreshTotals
			public abstract class refreshTotals : PX.Data.BQL.BqlBool.Field<refreshTotals> { }
			[PXDBBool]
			[PXDefault(true)]
			public bool? RefreshTotals { get; set; }
			#endregion
			#region CuryBalanceSummary
			public abstract class curyBalanceSummary : PX.Data.BQL.BqlDecimal.Field<curyBalanceSummary> { }
			protected decimal? _CuryBalanceSummary;
			[CM.PXCury(typeof(ARDocumentFilter.curyID))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance by Documents (Currency)", Enabled = false)]
			public virtual decimal? CuryBalanceSummary
			{
				get
				{
					return this._CuryBalanceSummary;
				}
				set
				{
					this._CuryBalanceSummary = value;
				}
			}
			#endregion
			#region BalanceSummary
			public abstract class balanceSummary : PX.Data.BQL.BqlDecimal.Field<balanceSummary> { }
			protected decimal? _BalanceSummary;
			[PXDBBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance by Documents", Enabled = false)]
			public virtual decimal? BalanceSummary
			{
				get
				{
					return this._BalanceSummary;
				}
				set
				{
					this._BalanceSummary = value;
				}
			}
			#endregion
			#region CuryCustomerBalance
			public abstract class curyCustomerBalance : PX.Data.BQL.BqlDecimal.Field<curyCustomerBalance> { }
			protected decimal? _CuryCustomerBalance;
			[CM.PXCury(typeof(ARDocumentFilter.curyID))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Current Balance (Currency)", Enabled = false)]
			public virtual decimal? CuryCustomerBalance
			{
				get
				{
					return this._CuryCustomerBalance;
				}
				set
				{
					this._CuryCustomerBalance = value;
				}
			}
			#endregion
			#region CustomerBalance
			public abstract class customerBalance : PX.Data.BQL.BqlDecimal.Field<customerBalance> { }
			protected decimal? _CustomerBalance;
			[PXDBBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Current Balance", Enabled = false)]
			public virtual decimal? CustomerBalance
			{
				get
				{
					return this._CustomerBalance;
				}
				set
				{
					this._CustomerBalance = value;
				}
			}
			#endregion
			#region CuryVCustomerRetainedBalance
			public abstract class curyCustomerRetainedBalance : PX.Data.BQL.BqlDecimal.Field<curyCustomerRetainedBalance> { }
			[CM.PXCury(typeof(ARDocumentFilter.curyID))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Retained Balance (Currency)", Enabled = false, FieldClass = nameof(FeaturesSet.Retainage))]
			public virtual decimal? CuryCustomerRetainedBalance
			{
				get;
				set;
			}
			#endregion
			#region CustomerRetainedBalance
			public abstract class customerRetainedBalance : PX.Data.BQL.BqlDecimal.Field<customerRetainedBalance> { }

			[PXDBBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Retained Balance", Enabled = false, FieldClass = nameof(FeaturesSet.Retainage))]
			public virtual decimal? CustomerRetainedBalance
			{
				get;
				set;
			}
			#endregion
			#region CuryCustomerDepositsBalance
			public abstract class curyCustomerDepositsBalance : PX.Data.BQL.BqlDecimal.Field<curyCustomerDepositsBalance> { }

			protected decimal? _CuryCustomerDepositsBalance;
			[CM.PXCury(typeof(ARDocumentFilter.curyID))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Prepayments Balance (Currency)", Enabled = false)]
			public virtual decimal? CuryCustomerDepositsBalance
			{
				get
				{
					return this._CuryCustomerDepositsBalance;
				}
				set
				{
					this._CuryCustomerDepositsBalance = value;
				}
			}
			#endregion
			#region CustomerDepositsBalance

			public abstract class customerDepositsBalance : PX.Data.BQL.BqlDecimal.Field<customerDepositsBalance> { }
			protected decimal? _CustomerDepositsBalance;
			[PXDBBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Prepayment Balance", Enabled = false)]
			public virtual decimal? CustomerDepositsBalance
			{
				get
				{
					return this._CustomerDepositsBalance;
				}
				set
				{
					this._CustomerDepositsBalance = value;
				}
			}
			#endregion
			#region CuryDifference
			public abstract class curyDifference : PX.Data.BQL.BqlDecimal.Field<curyDifference> { }

			[CM.PXCury(typeof(ARDocumentFilter.curyID))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance Discrepancy (Currency)", Enabled = false)]
			public virtual decimal? CuryDifference
			{
				[PXDependsOnFields(typeof(ARDocumentFilter.curyCustomerBalance),typeof(ARDocumentFilter.curyBalanceSummary),typeof(ARDocumentFilter.curyCustomerDepositsBalance))]
				get
				{
					return (this._CuryCustomerBalance - this._CuryBalanceSummary + this._CuryCustomerDepositsBalance);
				}
			}
			#endregion
			#region Difference
			public abstract class difference : PX.Data.BQL.BqlDecimal.Field<difference> { }
			[PXDBBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance Discrepancy", Enabled = false)]
			public virtual decimal? Difference
			{
				[PXDependsOnFields(typeof(ARDocumentFilter.customerBalance), typeof(ARDocumentFilter.balanceSummary), typeof(ARDocumentFilter.customerDepositsBalance))]
				get
				{
					return (this._CustomerBalance - this._BalanceSummary + this._CustomerDepositsBalance);
				}
			}
			#endregion
			#region IncludeChildAccounts
			public abstract class includeChildAccounts : PX.Data.BQL.BqlBool.Field<includeChildAccounts> { }

			[PXDBBool]
			[PXDefault(typeof(Search<CS.FeaturesSet.parentChildAccount>))]
			[PXUIField(DisplayName = "Include Child Accounts")]
			public virtual bool? IncludeChildAccounts { get; set; }
			#endregion
			#region IncludeGLTurnover
			public abstract class includeGLTurnover : PX.Data.BQL.BqlBool.Field<includeGLTurnover> { }
			protected bool? _IncludeGLTurnover;
			[PXDBBool()]
			[PXDefault(false)]
			public virtual bool? IncludeGLTurnover
			{
				get
				{
					return this._IncludeGLTurnover;
				}
				set
				{
					this._IncludeGLTurnover = value;
				}
			}
			#endregion

			public virtual void ClearSummary()
			{
				CustomerBalance = decimal.Zero;
				BalanceSummary = decimal.Zero;
				CustomerDepositsBalance = decimal.Zero;
				CuryCustomerBalance = decimal.Zero;
				CuryBalanceSummary = decimal.Zero;
				CuryCustomerDepositsBalance = decimal.Zero;
				CuryCustomerRetainedBalance = decimal.Zero;
				CustomerRetainedBalance = decimal.Zero;
			}

		}

		[Serializable()]
		[PXPrimaryGraph(typeof(ARDocumentEnq), Filter = typeof(ARDocumentFilter))]
		[PXCacheName(Messages.ARHistoryForReport)]
		public partial class ARHistoryForReport : ARHistory { }
		[Serializable()]
		[PXHidden]
		public class ARRegister : IBqlTable
		{
			#region DocType
			public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
			{
				public const int Length = 3;
			}

			/// <summary>
			/// The type of the document.
			/// This field is a part of the compound key of the document.
			/// </summary>
			/// <value>
			/// The field can have one of the values described in <see cref="ARDocType.ListAttribute"/>.
			/// </value>
			[PXDBString(docType.Length, IsKey = true, IsFixed = true, BqlTable = typeof(ARRegister))]
			[PXDefault()]
			[ARDocType.List()]
			[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = true, TabOrder = 0)]
			public virtual string DocType
			{
				get;
				set;
			}
			#endregion
			#region RefNbr

			public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
		{
			}

			/// <summary>
			/// The reference number of the document.
			/// This field is a part of the compound key of the document.
			/// </summary>
			[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "", BqlTable = typeof(ARRegister))]
			[PXDefault()]
			[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible, TabOrder = 1)]
			[PXSelector(typeof(Search<ARRegister.refNbr, Where<ARRegister.docType, Equal<Optional<ARRegister.docType>>>>), Filterable = true)]
			public virtual string RefNbr
			{
				get;
				set;
			}
			#endregion
			#region BranchID
			public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
			/// <summary>
			/// The identifier of the <see cref="Branch">branch</see> to which the document belongs.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Branch.BranchID"/> field.
			/// </value>
			[Branch(BqlTable=typeof(ARRegister))]
			public virtual int? BranchID
			{
				get;
				set;
			}
			#endregion
			#region DocDate
			public abstract class docDate : PX.Data.BQL.BqlDateTime.Field<docDate> { }
			/// <summary>
			/// The date of the document.
			/// </summary>
			/// <value>
			/// Defaults to the current <see cref="AccessInfo.BusinessDate">Business Date</see>.
			/// </value>
			[PXDBDate(BqlTable = typeof(ARRegister))]
			[PXDefault(typeof(AccessInfo.businessDate))]
			[PXUIField(DisplayName = "Date", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual DateTime? DocDate
			{
				get;
				set;
			}
			#endregion
			#region DocDesc
			public abstract class docDesc : PX.Data.BQL.BqlString.Field<docDesc> { }

			/// <summary>
			/// The description of the document.
			/// </summary>
			[PXDBString(Common.Constants.TranDescLength, IsUnicode = true, BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual string DocDesc
			{
				get;
				set;
			}
			#endregion
			#region DueDate
			public abstract class dueDate : PX.Data.BQL.BqlDateTime.Field<dueDate> { }
			[PXDBDate(BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Due Date", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual DateTime? DueDate
			{
				get;
				set;
			}
			#endregion
			#region CuryID
			public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
			/// <summary>
			/// The code of the <see cref="Currency"/> of the document.
			/// </summary>
			/// <value>
			/// Defaults to the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// Corresponds to the <see cref="Currency.CuryID"/> field.
			/// </value>
			[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL", BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Currency", Visibility = PXUIVisibility.SelectorVisible)]
			[PXDefault(typeof(Current<AccessInfo.baseCuryID>))]
			[PXSelector(typeof(Currency.curyID))]
			public virtual string CuryID
			{
				get;
				set;
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
			[PXDBLong(BqlTable = typeof(ARRegister))]
			[CurrencyInfo]
			public virtual long? CuryInfoID
			{
				get;
				set;
			}
			#endregion
			#region OrigDocType
			public abstract class origDocType : PX.Data.BQL.BqlString.Field<origDocType> { }

			/// <summary>
			/// The type of the original (source) document.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="DocType"/> field.
			/// </value>
			[PXDBString(3, IsFixed = true, BqlTable=typeof(ARRegister))]
			[ARDocType.List()]
			[PXUIField(DisplayName = "Orig. Doc. Type")]
			public virtual string OrigDocType
			{
				get;
				set;
			}
			#endregion
			#region OrigRefNbr
			public abstract class origRefNbr : PX.Data.BQL.BqlString.Field<origRefNbr> { }

			/// <summary>
			/// The reference number of the original (source) document.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="RefNbr"/> field.
			/// </value>
			[PXDBString(15, IsUnicode = true, InputMask = "", BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Orig. Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
			public virtual string OrigRefNbr
			{
				get;
				set;
			}
			#endregion
			#region Status
			public abstract class status : PX.Data.BQL.BqlString.Field<status> { }
			/// <summary>
			/// The status of the document.
			/// The value of the field is determined by the values of the status flags,
			/// such as <see cref="Hold"/>, <see cref="Released"/>, <see cref="Voided"/>, <see cref="Scheduled"/>.
			/// </summary>
			/// <value>
			/// The field can have one of the values described in <see cref="ARDocStatus.ListAttribute"/>.
			/// Defaults to <see cref="ARDocStatus.Hold"/>.
			/// </value>
			[PXDBString(1, IsFixed = true, BqlTable=typeof(ARRegister))]
			[PXDefault(ARDocStatus.Hold)]
			[PXUIField(DisplayName = "Status", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
			[ARDocStatus.List]
			public virtual string Status
			{
				get;
				set;
			}
			#endregion
			#region TranPeriodID
			public abstract class tranPeriodID : PX.Data.BQL.BqlString.Field<tranPeriodID> { }

			/// <summary>
			/// <see cref="FinPeriod">Financial Period</see> of the document.
			/// </summary>
			/// <value>
			/// Determined by the <see cref="ARRegister.DocDate">date of the document</see>. Unlike <see cref="ARRegister.FinPeriodID"/>
			/// the value of this field can't be overriden by user.
			/// </value>
			[PeriodID(BqlTable = typeof(ARRegister))]
			public virtual string TranPeriodID
			{
				get;
				set;
			}
			#endregion
			#region FinPeriodID
			public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }

			/// <summary>
			/// <see cref="FinPeriod">Financial Period</see> of the document.
			/// </summary>
			/// <value>
			/// Defaults to the period, to which the <see cref="ARRegister.DocDate"/> belongs, but can be overriden by user.
			/// </value>
			[AROpenPeriod(
				typeof(docDate),
				branchSourceType: typeof(branchID),
				masterFinPeriodIDType: typeof(tranPeriodID),
				IsHeader = true,
				BqlTable=typeof(ARRegister))]
			[PXUIField(DisplayName = "Post Period", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual string FinPeriodID
			{
				get;
				set;
			}
			#endregion
			#region ClosedFinPeriodID
			public abstract class closedFinPeriodID : PX.Data.BQL.BqlString.Field<closedFinPeriodID> { }

			/// <summary>
			/// The <see cref="FinancialPeriod">Financial Period</see>, in which the document was closed.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="FinPeriodID"/> field.
			/// </value>
			[FinPeriodID(
				branchSourceType: typeof(branchID),
				masterFinPeriodIDType: typeof(closedTranPeriodID),
				BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Closed Period", Visibility = PXUIVisibility.Invisible)]
			public virtual string ClosedFinPeriodID
			{
				get;
				set;
			}
			#endregion
			#region ClosedTranPeriodID
			public abstract class closedTranPeriodID : PX.Data.BQL.BqlString.Field<closedTranPeriodID> { }

			/// <summary>
			/// The <see cref="FinancialPeriod">Financial Period</see>, in which the document was closed.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="TranPeriodID"/> field.
			/// </value>
			[PeriodID(BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Closed Period", Visibility = PXUIVisibility.Invisible)]
			public virtual string ClosedTranPeriodID
			{
				get;
				set;
			}
			#endregion
			#region NoteID
			public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID>
			{
				[Obsolete(Objects.Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R1)]
				public class NoteAttribute : PXNoteAttribute
                {
					public NoteAttribute()
                    {
						BqlTable = typeof(ARRegister);
					}
                    protected override bool IsVirtualTable(Type table)
                    {
						return false;
                    }
                }
			}
			protected Guid? _NoteID;

			/// <summary>
			/// Identifier of the <see cref="PX.Data.Note">Note</see> object, associated with the document.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="PX.Data.Note.NoteID">Note.NoteID</see> field.
			/// </value>
			[PXNote(BqlTable=typeof(ARRegister))]
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
			#region OpenDoc
			public abstract class openDoc : PX.Data.BQL.BqlBool.Field<openDoc> { }

			/// <summary>
			/// When set to <c>true</c>, indicates that the document is open.
			/// </summary>
			[PXDBBool(BqlTable=typeof(ARRegister))]
			[PXDefault(true)]
			public virtual bool? OpenDoc
			{
				get;
				set;
			}
			#endregion
			#region Released
			public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }

			/// <summary>
			/// When set to <c>true</c>, indicates that the document has been released.
			/// </summary>
			[PXDBBool(BqlTable=typeof(ARRegister))]
			[PXDefault(false)]
			public virtual bool? Released
			{
				get;
				set;
			}
			#endregion
			#region IsRetainageDocument
			public abstract class isRetainageDocument : PX.Data.BQL.BqlBool.Field<isRetainageDocument> { }

			[PXDBBool(BqlTable=typeof(ARRegister))]
			[PXUIField(DisplayName = "Retainage Document", Enabled = false, FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual bool? IsRetainageDocument
			{
				get;
				set;
			}
			#endregion
			#region ARAccountID
			public abstract class aRAccountID : PX.Data.BQL.BqlInt.Field<aRAccountID> { }

			/// <summary>
			/// The identifier of the <see cref="Account">AR account</see> to which the document should be posted.
			/// The Cash account and Year-to-Date Net Income account cannot be selected as the value of this field.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Account.AccountID"/> field.
			/// </value>
			[PXDefault]
			[Account(typeof(branchID), typeof(Search<Account.accountID,
				Where2<Match<Current<AccessInfo.userName>>,
					And<Account.active, Equal<True>,
						And<Account.isCashAccount, Equal<False>,
							And<Where<Current<GLSetup.ytdNetIncAccountID>, IsNull,
								Or<Account.accountID, NotEqual<Current<GLSetup.ytdNetIncAccountID>>>>>>>>>), DisplayName = "AR Account",
				BqlTable = typeof(ARRegister))]
			public virtual int? ARAccountID
			{
				get;
				set;
			}
			#endregion
			#region ARSubID
			public abstract class aRSubID : PX.Data.BQL.BqlInt.Field<aRSubID> { }

			/// <summary>
			/// The identifier of the <see cref="Sub">subaccount</see> to which the document should be posted.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Sub.SubID"/> field.
			/// </value>
			[PXDefault]
			[SubAccount(typeof(aRAccountID), DescriptionField = typeof(Sub.description), DisplayName = "AR Subaccount", Visibility = PXUIVisibility.Visible,
				BqlTable = typeof(ARRegister))]
			public virtual int? ARSubID
			{
				get;
				set;
			}
			#endregion
			#region IsMigratedRecord
			public abstract class isMigratedRecord : PX.Data.BQL.BqlBool.Field<isMigratedRecord> { }

			/// <summary>
			/// Specifies (if set to <c>true</c>) that the record has been created
			/// in migration mode without affecting GL module.
			/// </summary>
			[MigratedRecord(typeof(ARSetup.migrationMode))]
			public virtual bool? IsMigratedRecord
			{
				get;
				set;
				}
			#endregion
			#region BatchNbr
			public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr> { }

			/// <summary>
			/// The number of the <see cref="Batch"/> created from the document on release.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Batch.BatchNbr"/> field.
			/// </value>
			[PXDBString(15, IsUnicode = true, BqlTable=typeof(ARRegister))]
			[PXUIField(DisplayName = "Batch Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
			[BatchNbr(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAR>>>),
				IsMigratedRecordField = typeof(ARRegister.isMigratedRecord))]
			public virtual string BatchNbr
			{
				get;
				set;
				}
			#endregion
			#region CuryOrigDiscAmt
			public abstract class curyOrigDiscAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDiscAmt> { }

			/// <summary>
			/// The cash discount entered for the document.
			/// Given in the <see cref="CuryID">currency of the document</see>.
			/// </summary>
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCurrency(typeof(curyInfoID), typeof(origDiscAmt), BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Cash Discount", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual decimal? CuryOrigDiscAmt
			{
				get;
				set;
			}
			#endregion
			#region OrigDiscAmt
			public abstract class origDiscAmt : PX.Data.BQL.BqlDecimal.Field<origDiscAmt> { }

			/// <summary>
			/// The cash discount entered for the document.
			/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// </summary>
			[PXDBBaseCury(BqlTable = typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			public virtual decimal? OrigDiscAmt
			{
				get;
				set;
			}
			#endregion

			#region Scheduled
			public abstract class scheduled : PX.Data.BQL.BqlBool.Field<scheduled> { }

			/// <summary>
			/// When set to <c>true</c> indicates that the document is part of a <c>Schedule</c> and serves as a template for generating other documents according to it.
			/// </summary>
			[PXDBBool(BqlTable=typeof(ARRegister))]
			[PXDefault(false)]
			public virtual bool? Scheduled
				{
				get;
				set;
				}
			#endregion
			#region Voided
			public abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }

			/// <summary>
			/// When set to <c>true</c> indicates that the document has been voided.
			/// </summary>
			[PXDBBool(BqlTable = typeof(ARRegister))]
			[PXDefault(false)]
			public virtual bool? Voided
				{
				get;
				set;
				}
			#endregion

			#region CuryOrigDocAmt
			public abstract class curyOrigDocAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmt> { }

			/// <summary>
			/// The amount of the document.
			/// Given in the <see cref="CuryID">currency of the document</see>.
			/// </summary>
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCurrency(typeof(ARRegister.curyInfoID), typeof(ARRegister.origDocAmt),
				BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Currency Origin. Amount", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual decimal? CuryOrigDocAmt
			{
				get;
				set;
			}
			#endregion
			#region OrigDocAmt
			public abstract class origDocAmt : PX.Data.BQL.BqlDecimal.Field<origDocAmt> { }

			/// <summary>
			/// The amount of the document.
			/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// </summary>
			[PXDBBaseCury(BqlTable = typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Origin. Amount", Visibility = PXUIVisibility.SelectorVisible)]

			public virtual decimal? OrigDocAmt
			{
				get;
				set;
			}
			#endregion
			#region CuryDiscTaken
			public abstract class curyDiscTaken : PX.Data.BQL.BqlDecimal.Field<curyDiscTaken> { }

			/// <summary>
			/// The <see cref="ARAdjust.CuryAdjdDiscAmt">cash discount amount</see> actually applied to the document.
			/// Given in the <see cref="CuryID">currency of the document</see>.
			/// </summary>
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCurrency(typeof(ARRegister.curyInfoID), typeof(ARRegister.discTaken),
				BqlTable = typeof(ARRegister))]
			public virtual decimal? CuryDiscTaken
				{
				get;
				set;
				}
			#endregion
			#region DiscTaken
			public abstract class discTaken : PX.Data.BQL.BqlDecimal.Field<discTaken> { }

			/// <summary>
			/// The <see cref="ARAdjust.CuryAdjdDiscAmt">cash discount amount</see> actually applied to the document.
			/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// </summary>
			[PXDBBaseCury(BqlTable = typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			public virtual decimal? DiscTaken
				{
				get;
				set;
				}
			#endregion
			#region CuryDiscBal
			public abstract class curyDiscBal : PX.Data.BQL.BqlDecimal.Field<curyDiscBal> { }

			/// <summary>
			/// The cash discount balance of the document.
			/// Given in the <see cref="CuryID">currency of the document</see>.
			/// </summary>
			[PXUIField(DisplayName = "Cash Discount Balance", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
			[PXDBCurrency(typeof(ARRegister.curyInfoID), typeof(ARRegister.discBal), BaseCalc = false,
				BqlTable = typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			public virtual decimal? CuryDiscBal
			{
				get;
				set;
			}
			#endregion
			#region DiscBal
			public abstract class discBal : PX.Data.BQL.BqlDecimal.Field<discBal> { }

			/// <summary>
			/// The cash discount balance of the document.
			/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// </summary>
			[PXDBBaseCury(BqlTable = typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			public virtual decimal? DiscBal
			{
				get;
				set;
			}
			#endregion
			#region CuryRetainageTotal
			public abstract class curyRetainageTotal : PX.Data.BQL.BqlDecimal.Field<curyRetainageTotal> { }

			[PXDBCurrency(typeof(curyInfoID), typeof(retainageTotal),
				BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Original Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual decimal? CuryRetainageTotal
			{
				get;
				set;
			}
			#endregion

			#region RetainageTotal
			public abstract class retainageTotal : PX.Data.BQL.BqlDecimal.Field<retainageTotal> { }

			[PXDBBaseCury(BqlTable = typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Original Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			public virtual decimal? RetainageTotal
			{
				get;
				set;
			}
			#endregion
			#region CuryRetainageUnreleasedAmt
			public abstract class curyRetainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<curyRetainageUnreleasedAmt> { }

			[PXDBCurrency(typeof(curyInfoID), typeof(retainageUnreleasedAmt),
				BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Unreleased Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual decimal? CuryRetainageUnreleasedAmt
			{
				get;
				set;
			}
			#endregion
			#region RetainageUnreleasedAmt
			public abstract class retainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<retainageUnreleasedAmt> { }

			[PXDBBaseCury(BqlTable = typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Unreleased Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			public virtual decimal? RetainageUnreleasedAmt
			{
				get;
				set;
			}
			#endregion
			#region CuryRetainedTaxTotal
			public abstract class curyRetainedTaxTotal : PX.Data.BQL.BqlDecimal.Field<curyRetainedTaxTotal> { }

			[PXDBCurrency(typeof(curyInfoID), typeof(retainedTaxTotal),
				BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Tax on Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual decimal? CuryRetainedTaxTotal
				{
				get;
				set;
				}
			#endregion
			#region RetainedTaxTotal
			public abstract class retainedTaxTotal : PX.Data.BQL.BqlDecimal.Field<retainedTaxTotal> { }

			[PXDBBaseCury(BqlTable = typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual decimal? RetainedTaxTotal
				{
				get;
				set;
				}
			#endregion
			#region CuryRetainedDiscTotal
			public abstract class curyRetainedDiscTotal : PX.Data.BQL.BqlDecimal.Field<curyRetainedDiscTotal> { }

			[PXDBCurrency(typeof(curyInfoID), typeof(retainedDiscTotal),
				BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Discount on Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual decimal? CuryRetainedDiscTotal
				{
				get;
				set;
				}
			#endregion
			#region RetainedDiscTotal
			public abstract class retainedDiscTotal : PX.Data.BQL.BqlDecimal.Field<retainedDiscTotal> { }

			[PXDBBaseCury(BqlTable = typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual decimal? RetainedDiscTotal
			{
				get;
				set;
			}
			#endregion
			#region CuryOrigDocAmtWithRetainageTotal
			public abstract class curyOrigDocAmtWithRetainageTotal : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmtWithRetainageTotal> { }

			[CM.PXCury(typeof(curyID))]
			[PXUIField(DisplayName = "Total Amount", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXFormula(typeof(Add<curyOrigDocAmt, curyRetainageTotal>))]
			public virtual decimal? CuryOrigDocAmtWithRetainageTotal
			{
				get;
				set;
			}
			#endregion
			#region OrigDocAmtWithRetainageTotal
			public abstract class origDocAmtWithRetainageTotal : PX.Data.BQL.BqlDecimal.Field<origDocAmtWithRetainageTotal> { }

			[PXBaseCury(BqlTable = typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Total Amount", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXFormula(typeof(Add<origDocAmt, retainageTotal>))]
			public virtual decimal? OrigDocAmtWithRetainageTotal
				{
				get;
				set;
				}
			#endregion

			#region CuryDocBal
			public abstract class curyDocBal : PX.Data.BQL.BqlDecimal.Field<curyDocBal> { }

			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCurrency(typeof(curyInfoID), typeof(curyDocBal), BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Currency Balance")]
			public virtual decimal? CuryDocBal
				{
				get;
				set;
				}
			#endregion
			#region DocBal
			public abstract class docBal : PX.Data.BQL.BqlDecimal.Field<docBal> { }

			[PXDBBaseCury(BqlTable=typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance")]
			public virtual decimal? DocBal
			{
				get;
				set;
			}
			#endregion
			#region RGOLAmt
			public abstract class rGOLAmt : PX.Data.BQL.BqlDecimal.Field<rGOLAmt> { }

			[PXDBBaseCury(BqlTable = typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "RGOL Amount")]
			public virtual decimal? RGOLAmt
			{
				get;
				set;
			}
			#endregion
			#region PaymentMethodID
			public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID> { }
			[PXString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
			[PXUIField(DisplayName = "Payment Method")]
			public virtual string PaymentMethodID
			{
				get;
				set;
				}
			#endregion
			#region CustomerID
			public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }

			[Customer(Enabled = false, Visible = false, DescriptionField = typeof(Customer.acctName), BqlTable = typeof(ARRegister))]
			public virtual int? CustomerID
			{
				get;
				set;
			}
			#endregion
			#region CuryDiscActTaken
			public abstract class curyDiscActTaken : PX.Data.BQL.BqlDecimal.Field<curyDiscActTaken> { }
			protected decimal? _CuryDiscActTaken;
			[CM.PXCury(typeof(ARRegister.curyID))]
			[PXUIField(DisplayName = "Currency Cash Discount Taken")]
			public virtual decimal? CuryDiscActTaken
			{
				get;
				set;
			}
			#endregion
			#region DiscActTaken
			public abstract class discActTaken : PX.Data.BQL.BqlDecimal.Field<discActTaken> { }
			protected decimal? _DiscActTaken;
			[PXBaseCury()]
			[PXUIField(DisplayName = "Cash Discount Taken")]
			public virtual decimal? DiscActTaken
			{
				get;
				set;
			}
			#endregion
			#region SignBalance
			public abstract class signBalance : PX.Data.IBqlField { }
			[PXDecimal()]
			[PXDependsOnFields(typeof(docType))]
			[PXDBCalced(typeof(
				Switch<Case<Where<ARRegister.docType.IsIn<ARDocType.refund, ARDocType.voidRefund, ARDocType.invoice, ARDocType.debitMemo, ARDocType.finCharge, ARDocType.smallCreditWO, ARDocType.cashSale>>,
					decimal1>,
					decimal_1>), typeof(decimal))]
			public virtual decimal? SignBalance
			{ get; set; }
			#endregion
		}


		[Serializable()]
		[PXProjection(typeof(Select2<ARRegister,
			LeftJoin<Ref.ARInvoice,
				On<Ref.ARInvoice.docType, Equal<ARRegister.docType>,
					And<Ref.ARInvoice.refNbr, Equal<ARRegister.refNbr>>>,
			LeftJoin<Ref.ARPayment,
				On<Ref.ARPayment.docType, Equal<ARRegister.docType>,
					And<Ref.ARPayment.refNbr, Equal<ARRegister.refNbr>>>>>>))]
		[PXPrimaryGraph(new Type[] {
				typeof(SO.SOInvoiceEntry),
				typeof(ARCashSaleEntry),
				typeof(ARInvoiceEntry),
				typeof(ARPaymentEntry)
			},
			new Type[] {
				typeof(Select<ARInvoice,
					Where<ARInvoice.docType, Equal<Current<ARDocumentResult.docType>>,
						And<ARInvoice.refNbr, Equal<Current<ARDocumentResult.refNbr>>,
						And<ARInvoice.origModule, Equal<BatchModule.moduleSO>,
						And<ARInvoice.released, Equal<False>>>>>>),
				typeof(Select<Standalone.ARCashSale,
					Where<Standalone.ARCashSale.docType, Equal<Current<ARDocumentResult.docType>>,
						And<Standalone.ARCashSale.refNbr, Equal<Current<ARDocumentResult.refNbr>>>>>),
				typeof(Select<ARInvoice,
					Where<ARInvoice.docType, Equal<Current<ARDocumentResult.docType>>,
						And<ARInvoice.refNbr, Equal<Current<ARDocumentResult.refNbr>>>>>),
				typeof(Select<ARPayment,
					Where<ARPayment.docType, Equal<Current<ARDocumentResult.docType>>,
						And<ARPayment.refNbr, Equal<Current<ARDocumentResult.refNbr>>>>>)
			})]
		[PXCacheName("Customer Details")]
		public partial class ARDocumentResult : IBqlTable
		{
			#region DocType
			public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
			{
				public const int Length = 3;
			}

			/// <summary>
			/// The type of the document.
			/// This field is a part of the compound key of the document.
			/// </summary>
			/// <value>
			/// The field can have one of the values described in <see cref="ARDocType.ListAttribute"/>.
			/// </value>
			[PXDBString(docType.Length, IsKey = true, IsFixed = true, BqlTable = typeof(ARRegister))]
			[PXDefault()]
			[ARDocType.List()]
			[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = true, TabOrder = 0)]
			public virtual string DocType
			{
				get;
				set;
			}
			#endregion
			#region RefNbr

			public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
		{
			}

			/// <summary>
			/// The reference number of the document.
			/// This field is a part of the compound key of the document.
			/// </summary>
			[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "", BqlTable = typeof(ARRegister))]
			[PXDefault()]
			[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible, TabOrder = 1)]
			[PXSelector(typeof(Search<ARRegister.refNbr, Where<ARRegister.docType, Equal<Optional<ARRegister.docType>>>>), Filterable = true)]
			public virtual string RefNbr
			{
				get;
				set;
			}
			#endregion
			#region InstallmentCntr

			public abstract class installmentCntr : PX.Data.BQL.BqlShort.Field<installmentCntr>
			{
			}

			/// <summary>
			/// The counter of <see cref="TermsInstallment">installments</see> associated with the document.
			/// </summary>
			[PXDBShort(BqlTable = typeof(Ref.ARInvoice))]
			public virtual short? InstallmentCntr { get; set; }

			#endregion
			#region BranchID
			public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
			/// <summary>
			/// The identifier of the <see cref="Branch">branch</see> to which the document belongs.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Branch.BranchID"/> field.
			/// </value>
			[Branch(BqlTable=typeof(ARRegister))]
			public virtual int? BranchID
			{
				get;
				set;
			}
			#endregion
			#region CustomerID
			public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }

			[Customer(Enabled = false, Visible = false, DescriptionField = typeof(Customer.acctName), BqlTable = typeof(ARRegister))]
			public virtual int? CustomerID
			{
				get;
				set;
			}
			#endregion

			#region DocDate
			public abstract class docDate : PX.Data.BQL.BqlDateTime.Field<docDate> { }
			/// <summary>
			/// The date of the document.
			/// </summary>
			/// <value>
			/// Defaults to the current <see cref="AccessInfo.BusinessDate">Business Date</see>.
			/// </value>
			[PXDBDate(BqlTable = typeof(ARRegister))]
			[PXDefault(typeof(AccessInfo.businessDate))]
			[PXUIField(DisplayName = "Date", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual DateTime? DocDate
			{
				get;
				set;
			}
			#endregion
			#region DocDesc
			public abstract class docDesc : PX.Data.BQL.BqlString.Field<docDesc> { }

			/// <summary>
			/// The description of the document.
			/// </summary>
			[PXDBString(Common.Constants.TranDescLength, IsUnicode = true, BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual string DocDesc
			{
				get;
				set;
			}
			#endregion
			#region DueDate
			public abstract class dueDate : PX.Data.BQL.BqlDateTime.Field<dueDate> { }
			[PXDBDate(BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Due Date", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual DateTime? DueDate
			{
				get;
				set;
			}
			#endregion
			#region CuryID
			public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
			/// <summary>
			/// The code of the <see cref="Currency"/> of the document.
			/// </summary>
			/// <value>
			/// Defaults to the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// Corresponds to the <see cref="Currency.CuryID"/> field.
			/// </value>
			[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL", BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Currency", Visibility = PXUIVisibility.SelectorVisible)]
			[PXDefault(typeof(Current<AccessInfo.baseCuryID>))]
			[PXSelector(typeof(Currency.curyID))]
			public virtual string CuryID
			{
				get;
				set;
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
			[PXDBLong(BqlTable = typeof(ARRegister))]
			[CurrencyInfo]
			public virtual long? CuryInfoID
			{
				get;
				set;
			}
			#endregion
			#region OrigDocType
			public abstract class origDocType : PX.Data.BQL.BqlString.Field<origDocType> { }

			/// <summary>
			/// The type of the original (source) document.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="DocType"/> field.
			/// </value>
			[PXDBString(3, IsFixed = true, BqlTable=typeof(ARRegister))]
			[ARDocType.List()]
			[PXUIField(DisplayName = "Orig. Doc. Type")]
			public virtual string OrigDocType
			{
				get;
				set;
			}
			#endregion
			#region OrigRefNbr
			public abstract class origRefNbr : PX.Data.BQL.BqlString.Field<origRefNbr> { }

			/// <summary>
			/// The reference number of the original (source) document.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="RefNbr"/> field.
			/// </value>
			[PXDBString(15, IsUnicode = true, InputMask = "", BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Orig. Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
			public virtual string OrigRefNbr
			{
				get;
				set;
			}
			#endregion
			#region Status
			public abstract class status : PX.Data.BQL.BqlString.Field<status> { }
			/// <summary>
			/// The status of the document.
			/// The value of the field is determined by the values of the status flags,
			/// such as <see cref="Hold"/>, <see cref="Released"/>, <see cref="Voided"/>, <see cref="Scheduled"/>.
			/// </summary>
			/// <value>
			/// The field can have one of the values described in <see cref="ARDocStatus.ListAttribute"/>.
			/// Defaults to <see cref="ARDocStatus.Hold"/>.
			/// </value>
			[PXDBString(1, IsFixed = true, BqlTable=typeof(ARRegister))]
			[PXDefault(ARDocStatus.Hold)]
			[PXUIField(DisplayName = "Status", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
			[ARDocStatus.List]
			public virtual string Status
			{
				get;
				set;
			}
			#endregion
			#region TranPeriodID
			public abstract class tranPeriodID : PX.Data.BQL.BqlString.Field<tranPeriodID> { }

			/// <summary>
			/// <see cref="FinPeriod">Financial Period</see> of the document.
			/// </summary>
			/// <value>
			/// Determined by the <see cref="ARRegister.DocDate">date of the document</see>. Unlike <see cref="ARRegister.FinPeriodID"/>
			/// the value of this field can't be overriden by user.
			/// </value>
			[PeriodID(BqlTable = typeof(ARRegister))]
			public virtual string TranPeriodID
			{
				get;
				set;
			}
			#endregion
			#region FinPeriodID
			public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }

			/// <summary>
			/// <see cref="FinPeriod">Financial Period</see> of the document.
			/// </summary>
			/// <value>
			/// Defaults to the period, to which the <see cref="ARRegister.DocDate"/> belongs, but can be overriden by user.
			/// </value>
			[AROpenPeriod(
				typeof(docDate),
				branchSourceType: typeof(branchID),
				masterFinPeriodIDType: typeof(tranPeriodID),
				IsHeader = true,
				BqlTable=typeof(ARRegister))]
			[PXUIField(DisplayName = "Post Period", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual string FinPeriodID
			{
				get;
				set;
			}
			#endregion
			#region ClosedFinPeriodID
			public abstract class closedFinPeriodID : PX.Data.BQL.BqlString.Field<closedFinPeriodID> { }

			/// <summary>
			/// The <see cref="FinancialPeriod">Financial Period</see>, in which the document was closed.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="FinPeriodID"/> field.
			/// </value>
			[FinPeriodID(
				branchSourceType: typeof(branchID),
				masterFinPeriodIDType: typeof(closedTranPeriodID),
				BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Closed Period", Visibility = PXUIVisibility.Invisible)]
			public virtual string ClosedFinPeriodID
			{
				get;
				set;
			}
			#endregion
			#region ClosedTranPeriodID
			public abstract class closedTranPeriodID : PX.Data.BQL.BqlString.Field<closedTranPeriodID> { }

			/// <summary>
			/// The <see cref="FinancialPeriod">Financial Period</see>, in which the document was closed.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="TranPeriodID"/> field.
			/// </value>
			[PeriodID(BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Closed Period", Visibility = PXUIVisibility.Invisible)]
			public virtual string ClosedTranPeriodID
			{
				get;
				set;
			}
			#endregion
			#region NoteID
			public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID>
			{
				public class NoteAttribute : PXNoteAttribute
				{
					public NoteAttribute()
					{
						BqlTable = typeof(ARRegister);
					}
					protected override bool IsVirtualTable(Type table)
					{
						return false;
					}
				}
			}
			protected Guid? _NoteID;

			/// <summary>
			/// Identifier of the <see cref="PX.Data.Note">Note</see> object, associated with the document.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="PX.Data.Note.NoteID">Note.NoteID</see> field.
			/// </value>
			[noteID.Note]
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
			#region OpenDoc
			public abstract class openDoc : PX.Data.BQL.BqlBool.Field<openDoc> { }

			/// <summary>
			/// When set to <c>true</c>, indicates that the document is open.
			/// </summary>
			[PXDBBool(BqlTable=typeof(ARRegister))]
			[PXDefault(true)]
			public virtual bool? OpenDoc
			{
				get;
				set;
			}
			#endregion
			#region Released
			public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }

			/// <summary>
			/// When set to <c>true</c>, indicates that the document has been released.
			/// </summary>
			[PXDBBool(BqlTable=typeof(ARRegister))]
			[PXDefault(false)]
			public virtual bool? Released
			{
				get;
				set;
			}
			#endregion
			#region IsRetainageDocument
			public abstract class isRetainageDocument : PX.Data.BQL.BqlBool.Field<isRetainageDocument> { }

			[PXDBBool(BqlTable=typeof(ARRegister))]
			[PXUIField(DisplayName = "Retainage Document", Enabled = false, FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual bool? IsRetainageDocument
			{
				get;
				set;
			}
			#endregion
			#region ARAccountID
			public abstract class aRAccountID : PX.Data.BQL.BqlInt.Field<aRAccountID> { }

			/// <summary>
			/// The identifier of the <see cref="Account">AR account</see> to which the document should be posted.
			/// The Cash account and Year-to-Date Net Income account cannot be selected as the value of this field.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Account.AccountID"/> field.
			/// </value>
			[PXDefault]
			[Account(typeof(branchID), typeof(Search<Account.accountID,
				Where2<Match<Current<AccessInfo.userName>>,
					And<Account.active, Equal<True>,
						And<Account.isCashAccount, Equal<False>,
							And<Where<Current<GLSetup.ytdNetIncAccountID>, IsNull,
								Or<Account.accountID, NotEqual<Current<GLSetup.ytdNetIncAccountID>>>>>>>>>), DisplayName = "AR Account",
				BqlTable = typeof(ARRegister))]
			public virtual int? ARAccountID
			{
				get;
				set;
			}
			#endregion
			#region ARSubID
			public abstract class aRSubID : PX.Data.BQL.BqlInt.Field<aRSubID> { }

			/// <summary>
			/// The identifier of the <see cref="Sub">subaccount</see> to which the document should be posted.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Sub.SubID"/> field.
			/// </value>
			[PXDefault]
			[SubAccount(typeof(aRAccountID), DescriptionField = typeof(Sub.description), DisplayName = "AR Subaccount", Visibility = PXUIVisibility.Visible,
				BqlTable = typeof(ARRegister))]
			public virtual int? ARSubID
			{
				get;
				set;
			}
			#endregion
			#region IsMigratedRecord
			public abstract class isMigratedRecord : PX.Data.BQL.BqlBool.Field<isMigratedRecord> { }

			/// <summary>
			/// Specifies (if set to <c>true</c>) that the record has been created
			/// in migration mode without affecting GL module.
			/// </summary>
			[MigratedRecord(typeof(ARSetup.migrationMode), BqlTable = typeof(ARDocumentResult))]
			public virtual bool? IsMigratedRecord
			{
				get;
				set;
				}
			#endregion
			#region BatchNbr
			public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr> { }

			/// <summary>
			/// The number of the <see cref="Batch"/> created from the document on release.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Batch.BatchNbr"/> field.
			/// </value>
			[PXDBString(15, IsUnicode = true, BqlTable=typeof(ARRegister))]
			[PXUIField(DisplayName = "Batch Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
			[BatchNbr(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAR>>>),
				IsMigratedRecordField = typeof(ARRegister.isMigratedRecord))]
			public virtual string BatchNbr
			{
				get;
				set;
				}
			#endregion
			#region Scheduled
			public abstract class scheduled : PX.Data.BQL.BqlBool.Field<scheduled> { }

			/// <summary>
			/// When set to <c>true</c> indicates that the document is part of a <c>Schedule</c> and serves as a template for generating other documents according to it.
			/// </summary>
			[PXDBBool(BqlTable=typeof(ARRegister))]
			[PXDefault(false)]
			public virtual bool? Scheduled
			{
				get;
				set;
			}
			#endregion
			#region Voided
			public abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }

			/// <summary>
			/// When set to <c>true</c> indicates that the document has been voided.
			/// </summary>
			[PXDBBool(BqlTable = typeof(ARRegister))]
			[PXDefault(false)]
			public virtual bool? Voided
			{
				get;
				set;
			}
			#endregion

			#region ExtRefNbr
			public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }
			[PXString(30, IsUnicode = true)]
			[PXUIField(DisplayName = "Customer Invoice Nbr./Payment Nbr.")]
			[PXDBCalced(typeof(IsNull<
					Ref.ARInvoice.invoiceNbr,
					Ref.ARPayment.extRefNbr>), typeof(string))]
			public virtual string ExtRefNbr
			{
				get;
				set;
			}
			#endregion
			#region PaymentMethodID
			public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID> { }
			[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa", BqlField = typeof(Ref.ARPayment.paymentMethodID))]
			[PXUIField(DisplayName = "Payment Method")]
			public virtual string PaymentMethodID
			{
				get;
				set;
			}
			#endregion
			#region SignBalance
			public abstract class signBalance : PX.Data.IBqlField { }
			[PXDecimal(BqlTable=typeof(ARRegister))]
			public virtual decimal? SignBalance
			{ get; set; }
			#endregion

			#region Original Amounts
			#region CuryOrigDocAmt
			public abstract class curyOrigDocAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmt> { }

			/// <summary>
			/// The amount of the document.
			/// Given in the <see cref="CuryID">currency of the document</see>.
			/// </summary>
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXCurrency(typeof(ARRegister.curyInfoID), typeof(ARRegister.origDocAmt))]
			[PXUIField(DisplayName = "Currency Origin. Amount", Visibility = PXUIVisibility.SelectorVisible)]
			[PXDBCalced(typeof(Mult<ARRegister.signBalance, ARRegister.curyOrigDocAmt>), typeof(decimal))]
			public virtual decimal? CuryOrigDocAmt
			{
				get;
				set;
			}
			#endregion
			#region OrigDocAmt
			public abstract class origDocAmt : PX.Data.BQL.BqlDecimal.Field<origDocAmt> { }

			/// <summary>
			/// The amount of the document.
			/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// </summary>
			[PXBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Origin. Amount", Visibility = PXUIVisibility.SelectorVisible)]
			[PXDBCalced(typeof(Mult<ARRegister.signBalance, ARRegister.origDocAmt>), typeof(decimal))]

			public virtual decimal? OrigDocAmt
			{
				get;
				set;
			}
			#endregion
			#region CuryOrigDiscAmt
			public abstract class curyOrigDiscAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDiscAmt> { }

			/// <summary>
			/// The cash discount entered for the document.
			/// Given in the <see cref="CuryID">currency of the document</see>.
			/// </summary>
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXCurrency(typeof(curyInfoID), typeof(origDiscAmt))]
			[PXUIField(DisplayName = "Cash Discount", Visibility = PXUIVisibility.SelectorVisible)]
			[PXDBCalced(typeof(Mult<ARRegister.signBalance, ARRegister.curyOrigDiscAmt>), typeof(decimal))]
			public virtual decimal? CuryOrigDiscAmt
			{
				get;
				set;
			}
			#endregion
			#region OrigDiscAmt
			public abstract class origDiscAmt : PX.Data.BQL.BqlDecimal.Field<origDiscAmt> { }

			/// <summary>
			/// The cash discount entered for the document.
			/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// </summary>
			[PXBaseCury(BqlTable = typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCalced(typeof(Mult<ARRegister.signBalance, ARRegister.origDiscAmt>), typeof(decimal))]
			public virtual decimal? OrigDiscAmt
			{
				get;
				set;
			}
			#endregion
			#region CuryRetainageTotal
			public abstract class curyRetainageTotal : PX.Data.BQL.BqlDecimal.Field<curyRetainageTotal> { }

			[PXCurrency(typeof(curyInfoID), typeof(retainageTotal))]
			[PXUIField(DisplayName = "Original Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXDBCalced(typeof(Mult<ARRegister.signBalance, ARRegister.curyRetainageTotal>), typeof(decimal))]

			public virtual decimal? CuryRetainageTotal
			{
				get;
				set;
			}
			#endregion
			#region RetainageTotal
			public abstract class retainageTotal : PX.Data.BQL.BqlDecimal.Field<retainageTotal> { }

			[PXBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Original Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDBCalced(typeof(Mult<ARRegister.signBalance, ARRegister.retainageTotal>), typeof(decimal))]
			public virtual decimal? RetainageTotal
			{
				get;
				set;
			}
			#endregion
			#region CuryOrigDocAmtWithRetainageTotal
			public abstract class curyOrigDocAmtWithRetainageTotal : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmtWithRetainageTotal> { }

			[CM.PXCury(typeof(curyID))]
			[PXUIField(DisplayName = "Total Amount", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXDBCalced(typeof(Mult<ARRegister.signBalance, Add<ARRegister.curyOrigDocAmt, ARRegister.curyRetainageTotal>>), typeof(decimal))]
			public virtual decimal? CuryOrigDocAmtWithRetainageTotal
			{
				get;
				set;
			}
			#endregion
			#region OrigDocAmtWithRetainageTotal
			public abstract class origDocAmtWithRetainageTotal : PX.Data.BQL.BqlDecimal.Field<origDocAmtWithRetainageTotal> { }

			[PXBaseCury(BqlTable = typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Total Amount", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDBCalced(typeof(Mult<ARRegister.signBalance, Add<ARRegister.origDocAmt, ARRegister.retainageTotal>>), typeof(decimal))]
			public virtual decimal? OrigDocAmtWithRetainageTotal
			{
				get;
				set;
			}
			#endregion
			#endregion

			#region Begin Balance
			#region CuryBegBalance
			public abstract class curyBegBalance : PX.Data.BQL.BqlDecimal.Field<curyBegBalance> { }
			protected decimal? _CuryBegBalance;
			[CM.PXCury(typeof(curyID))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Currency Period Beg. Balance")]
			[PXDBCalced(typeof(decimal0), typeof(decimal))]
			public virtual decimal? CuryBegBalance
			{
				get;
				set;
			}
			#endregion
			#region BegBalance
			public abstract class begBalance : PX.Data.BQL.BqlDecimal.Field<begBalance> { }
			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Period Beg. Balance")]
			[PXDBCalced(typeof(decimal0), typeof(decimal))]
			public virtual decimal? BegBalance
			{
				get;
				set;
			}
			#endregion
			#endregion

			#region Turns
			#region CuryDiscActTaken
			public abstract class curyDiscActTaken : PX.Data.BQL.BqlDecimal.Field<curyDiscActTaken> { }
			protected decimal? _CuryDiscActTaken;
			[CM.PXCury(typeof(ARRegister.curyID))]
			[PXUIField(DisplayName = "Currency Cash Discount Taken")]
			[PXDBCalced(typeof(
				Mult<ARRegister.signBalance,ARRegister.curyDiscTaken>), typeof(decimal))]
			public virtual decimal? CuryDiscActTaken
			{
				get;
				set;
			}
			#endregion
			#region DiscActTaken
			public abstract class discActTaken : PX.Data.BQL.BqlDecimal.Field<discActTaken> { }
			protected decimal? _DiscActTaken;
			[PXBaseCury()]
			[PXUIField(DisplayName = "Cash Discount Taken")]
			[PXDBCalced(typeof(
				Mult<ARRegister.signBalance,ARRegister.discTaken>), typeof(decimal))]
			public virtual decimal? DiscActTaken
			{
				get;
				set;
			}
			#endregion
			#region CuryWOAmt
			public abstract class curyWOAmt : PX.Data.BQL.BqlDecimal.Field<curyWOAmt> { }
			[PXDefault(TypeCode.Decimal, "0.0")]
			[CM.PXCury(typeof(ARRegister.curyID))]
			[PXUIField(DisplayName = "Currency Write-Off Amount")]
			[PXDBCalced(typeof(decimal0), typeof(decimal))]
			public virtual decimal? CuryWOAmt
			{
				get;
				set;
			}
			#endregion
			#region WOAmt
			public abstract class woAmt : PX.Data.BQL.BqlDecimal.Field<woAmt> { }
			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Write-Off Amount")]
			[PXDBCalced(typeof(decimal0), typeof(decimal))]
			public virtual decimal? WOAmt
			{
				get;
				set;
			}
			#endregion
			#region RGOLAmt
			public abstract class rGOLAmt : PX.Data.BQL.BqlDecimal.Field<rGOLAmt> { }
			[PXBaseCury(BqlTable = typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "RGOL Amount")]
			[PXDBCalced(typeof(
				Mult<decimal_1,ARRegister.rGOLAmt>), typeof(decimal))]
			public virtual decimal? RGOLAmt
			{
				get;
				set;
			}
			#endregion
			#region ARTurnover
			public abstract class aRTurnover : PX.Data.BQL.BqlDecimal.Field<aRTurnover> { }

			/// <summary>
			/// Expected GL turnover for the document.
			/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// </summary>
			[PXBaseCury(BqlTable=typeof(ARRegister))]
			[PXUIField(DisplayName = "AR Turnover")]
			[PXDBCalced(typeof(decimal0), typeof(decimal))]
			public virtual decimal? ARTurnover
			{
				get;
				set;
			}
			#endregion
			#region GLTurnover
			public abstract class gLTurnover : PX.Data.BQL.BqlDecimal.Field<gLTurnover> { }

			/// <summary>
			/// Expected GL turnover for the document.
			/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// </summary>
			[PXBaseCury()]
			[PXUIField(DisplayName = "GL Turnover")]
			public virtual decimal? GLTurnover
			{
				get;
				set;
			}
			#endregion
			#endregion

			#region End Balance
			#region CuryDocBal
			public abstract class curyDocBal : PX.Data.BQL.BqlDecimal.Field<curyDocBal> { }

			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXCurrency(typeof(curyInfoID), typeof(curyDocBal))]
			[PXUIField(DisplayName = "Currency Balance")]
			[PXDBCalced(typeof(
				Switch<Case<Where<ARRegister.voided, NotEqual<True>, And<ARRegister.docType.IsNotIn<ARDocType.cashSale, ARDocType.cashReturn>>>,
						Mult<ARRegister.signBalance, ARRegister.curyDocBal>>,
					decimal0>), typeof(decimal))]
			public virtual decimal? CuryDocBal
			{
				get;
				set;
			}
			#endregion
			#region DocBal
			public abstract class docBal : PX.Data.BQL.BqlDecimal.Field<docBal> { }

			[PXBaseCury(BqlTable=typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance")]
			[PXDBCalced(typeof(
				Switch<Case<Where<ARRegister.voided, NotEqual<True>, And<ARRegister.docType.IsNotIn<ARDocType.cashSale, ARDocType.cashReturn>>>,
						Mult<ARRegister.signBalance,ARRegister.docBal>>,
					decimal0>), typeof(decimal))]
			public virtual decimal? DocBal
			{
				get;
				set;
			}
			#region CuryRetainageUnreleasedAmt
			public abstract class curyRetainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<curyRetainageUnreleasedAmt> { }

			[PXCurrency(typeof(curyInfoID), typeof(retainageUnreleasedAmt),
				BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Unreleased Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXDBCalced(typeof(
				Mult<ARRegister.signBalance, ARRegister.curyRetainageUnreleasedAmt>), typeof(decimal))]
			public virtual decimal? CuryRetainageUnreleasedAmt
			{
				get;
				set;
			}
			#endregion
			#region RetainageUnreleasedAmt
			public abstract class retainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<retainageUnreleasedAmt> { }

			[PXBaseCury(BqlTable = typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Unreleased Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDBCalced(typeof(
				Mult<ARRegister.signBalance,ARRegister.retainageUnreleasedAmt>), typeof(decimal))]
			public virtual decimal? RetainageUnreleasedAmt
			{
				get;
				set;
			}
			#endregion
			#endregion
			#endregion
			#region TranPostPeriodID
			public abstract class tranPostPeriodID : PX.Data.BQL.BqlString.Field<tranPostPeriodID> { }
			[PXString]
			[PXDBCalced(typeof(ARRegister.tranPeriodID), typeof(string))]
			public virtual string TranPostPeriodID
			{
				get;
				set;
			}
			#endregion
			#region FinPostPeriodID
			public abstract class finPostPeriodID : PX.Data.BQL.BqlString.Field<finPostPeriodID> { }
			[PXString]
			[PXDBCalced(typeof(ARRegister.finPeriodID), typeof(string))]
			public virtual string FinPostPeriodID
			{
				get;
				set;
			}
			#endregion
		}

		[Serializable()]
		[PXProjection(typeof(Select2<ARDocumentResult,
			LeftJoin<ARTranPostGL,
				On<ARTranPostGL.docType, Equal<ARDocumentResult.docType>,
				And<ARTranPostGL.refNbr, Equal<ARDocumentResult.refNbr>>>>,
			Where<ARDocumentResult.installmentCntr, IsNull>>))]
		[PXHidden]

		public partial class ARDocumentPeriodResult : IBqlTable
		{
			#region DocType
			public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
			{
				public const int Length = 3;
			}

			/// <summary>
			/// The type of the document.
			/// This field is a part of the compound key of the document.
			/// </summary>
			/// <value>
			/// The field can have one of the values described in <see cref="ARDocType.ListAttribute"/>.
			/// </value>
			[PXDBString(docType.Length, IsKey = true, IsFixed = true, BqlTable = typeof(ARDocumentResult))]
			[PXDefault()]
			[ARDocType.List()]
			[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = true, TabOrder = 0)]
			public virtual string DocType
			{
				get;
				set;
			}
			#endregion
			#region RefNbr

			public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
			{
			}

			/// <summary>
			/// The reference number of the document.
			/// This field is a part of the compound key of the document.
			/// </summary>
			[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "", BqlTable = typeof(ARDocumentResult))]
			[PXDefault()]
			[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible, TabOrder = 1)]
			[PXSelector(typeof(Search<ARRegister.refNbr, Where<ARRegister.docType, Equal<Optional<ARRegister.docType>>>>), Filterable = true)]
			public virtual string RefNbr
			{
				get;
				set;
			}
			#endregion
			#region BranchID
			public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
			/// <summary>
			/// The identifier of the <see cref="Branch">branch</see> to which the document belongs.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Branch.BranchID"/> field.
			/// </value>
			[Branch(BqlTable=typeof(ARDocumentResult))]
			public virtual int? BranchID
			{
				get;
				set;
			}
			#endregion
			#region CustomerID
			public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }

			[Customer(Enabled = false, Visible = false, DescriptionField = typeof(Customer.acctName), BqlTable = typeof(ARDocumentResult))]
			public virtual int? CustomerID
			{
				get;
				set;
			}
			#endregion

			#region DocDate
			public abstract class docDate : PX.Data.BQL.BqlDateTime.Field<docDate> { }
			/// <summary>
			/// The date of the document.
			/// </summary>
			/// <value>
			/// Defaults to the current <see cref="AccessInfo.BusinessDate">Business Date</see>.
			/// </value>
			[PXDBDate(BqlTable = typeof(ARDocumentResult))]
			[PXDefault(typeof(AccessInfo.businessDate))]
			[PXUIField(DisplayName = "Date", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual DateTime? DocDate
			{
				get;
				set;
			}
			#endregion
			#region DocDesc
			public abstract class docDesc : PX.Data.BQL.BqlString.Field<docDesc> { }

			/// <summary>
			/// The description of the document.
			/// </summary>
			[PXDBString(Common.Constants.TranDescLength, IsUnicode = true, BqlTable = typeof(ARDocumentResult))]
			[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual string DocDesc
			{
				get;
				set;
			}
			#endregion
			#region DueDate
			public abstract class dueDate : PX.Data.BQL.BqlDateTime.Field<dueDate> { }
			[PXDBDate(BqlTable = typeof(ARDocumentResult))]
			[PXUIField(DisplayName = "Due Date", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual DateTime? DueDate
			{
				get;
				set;
			}
			#endregion
			#region CuryID
			public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
			/// <summary>
			/// The code of the <see cref="Currency"/> of the document.
			/// </summary>
			/// <value>
			/// Defaults to the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// Corresponds to the <see cref="Currency.CuryID"/> field.
			/// </value>
			[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL", BqlTable = typeof(ARDocumentResult))]
			[PXUIField(DisplayName = "Currency", Visibility = PXUIVisibility.SelectorVisible)]
			[PXDefault(typeof(Current<AccessInfo.baseCuryID>))]
			[PXSelector(typeof(Currency.curyID))]
			public virtual string CuryID
			{
				get;
				set;
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
			[PXDBLong(BqlTable = typeof(ARDocumentResult))]
			[CurrencyInfo]
			public virtual long? CuryInfoID
			{
				get;
				set;
			}
			#endregion
			#region OrigDocType
			public abstract class origDocType : PX.Data.BQL.BqlString.Field<origDocType> { }

			/// <summary>
			/// The type of the original (source) document.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="DocType"/> field.
			/// </value>
			[PXDBString(3, IsFixed = true, BqlTable=typeof(ARDocumentResult))]
			[ARDocType.List()]
			[PXUIField(DisplayName = "Orig. Doc. Type")]
			public virtual string OrigDocType
			{
				get;
				set;
			}
			#endregion
			#region OrigRefNbr
			public abstract class origRefNbr : PX.Data.BQL.BqlString.Field<origRefNbr> { }

			/// <summary>
			/// The reference number of the original (source) document.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="RefNbr"/> field.
			/// </value>
			[PXDBString(15, IsUnicode = true, InputMask = "", BqlTable = typeof(ARDocumentResult))]
			[PXUIField(DisplayName = "Orig. Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
			public virtual string OrigRefNbr
			{
				get;
				set;
			}
			#endregion
			#region Status
			public abstract class status : PX.Data.BQL.BqlString.Field<status> { }
			/// <summary>
			/// The status of the document.
			/// The value of the field is determined by the values of the status flags,
			/// such as <see cref="Hold"/>, <see cref="Released"/>, <see cref="Voided"/>, <see cref="Scheduled"/>.
			/// </summary>
			/// <value>
			/// The field can have one of the values described in <see cref="ARDocStatus.ListAttribute"/>.
			/// Defaults to <see cref="ARDocStatus.Hold"/>.
			/// </value>
			[PXDBString(1, IsFixed = true, BqlTable=typeof(ARDocumentResult))]
			[PXDefault(ARDocStatus.Hold)]
			[PXUIField(DisplayName = "Status", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
			[ARDocStatus.List]
			public virtual string Status
			{
				get;
				set;
			}
			#endregion
			#region TranPeriodID
			public abstract class tranPeriodID : PX.Data.BQL.BqlString.Field<tranPeriodID> { }

			/// <summary>
			/// <see cref="FinPeriod">Financial Period</see> of the document.
			/// </summary>
			/// <value>
			/// Determined by the <see cref="ARRegister.DocDate">date of the document</see>. Unlike <see cref="ARRegister.FinPeriodID"/>
			/// the value of this field can't be virtualn by user.
			/// </value>
			[PeriodID(BqlTable = typeof(ARDocumentResult))]
			public virtual string TranPeriodID
			{
				get;
				set;
			}
			#endregion
			#region FinPeriodID
			public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }

			/// <summary>
			/// <see cref="FinPeriod">Financial Period</see> of the document.
			/// </summary>
			/// <value>
			/// Defaults to the period, to which the <see cref="ARRegister.DocDate"/> belongs, but can be virtualn by user.
			/// </value>
			[AROpenPeriod(
				typeof(docDate),
				branchSourceType: typeof(branchID),
				masterFinPeriodIDType: typeof(tranPeriodID),
				IsHeader = true,
				BqlTable=typeof(ARDocumentResult))]
			[PXUIField(DisplayName = "Post Period", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual string FinPeriodID
			{
				get;
				set;
			}
			#endregion
			#region ClosedFinPeriodID
			public abstract class closedFinPeriodID : PX.Data.BQL.BqlString.Field<closedFinPeriodID> { }

			/// <summary>
			/// The <see cref="FinancialPeriod">Financial Period</see>, in which the document was closed.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="FinPeriodID"/> field.
			/// </value>
			[FinPeriodID(
				branchSourceType: typeof(branchID),
				masterFinPeriodIDType: typeof(closedTranPeriodID),
				BqlTable = typeof(ARDocumentResult))]
			[PXUIField(DisplayName = "Closed Period", Visibility = PXUIVisibility.Invisible)]
			public virtual string ClosedFinPeriodID
			{
				get;
				set;
			}
			#endregion
			#region ClosedTranPeriodID
			public abstract class closedTranPeriodID : PX.Data.BQL.BqlString.Field<closedTranPeriodID> { }

			/// <summary>
			/// The <see cref="FinancialPeriod">Financial Period</see>, in which the document was closed.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="TranPeriodID"/> field.
			/// </value>
			[PeriodID(BqlTable = typeof(ARDocumentResult))]
			[PXUIField(DisplayName = "Closed Period", Visibility = PXUIVisibility.Invisible)]
			public virtual string ClosedTranPeriodID
			{
				get;
				set;
			}
			#endregion
			#region NoteID
			public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
			protected Guid? _NoteID;

			/// <summary>
			/// Identifier of the <see cref="PX.Data.Note">Note</see> object, associated with the document.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="PX.Data.Note.NoteID">Note.NoteID</see> field.
			/// </value>
			[PXNote(BqlTable=typeof(ARDocumentResult))]
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
			#region OpenDoc
			public abstract class openDoc : PX.Data.BQL.BqlBool.Field<openDoc> { }

			/// <summary>
			/// When set to <c>true</c>, indicates that the document is open.
			/// </summary>
			[PXDBBool(BqlTable=typeof(ARDocumentResult))]
			[PXDefault(true)]
			public virtual bool? OpenDoc
			{
				get;
				set;
			}
			#endregion
			#region Released
			public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }

			/// <summary>
			/// When set to <c>true</c>, indicates that the document has been released.
			/// </summary>
			[PXDBBool(BqlTable=typeof(ARDocumentResult))]
			[PXDefault(false)]
			public virtual bool? Released
			{
				get;
				set;
			}
			#endregion
			#region IsRetainageDocument
			public abstract class isRetainageDocument : PX.Data.BQL.BqlBool.Field<isRetainageDocument> { }

			[PXDBBool(BqlTable=typeof(ARDocumentResult))]
			[PXUIField(DisplayName = "Retainage Document", Enabled = false, FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual bool? IsRetainageDocument
			{
				get;
				set;
			}
			#endregion
			#region ARAccountID
			public abstract class aRAccountID : PX.Data.BQL.BqlInt.Field<aRAccountID> { }

			/// <summary>
			/// The identifier of the <see cref="Account">AR account</see> to which the document should be posted.
			/// The Cash account and Year-to-Date Net Income account cannot be selected as the value of this field.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Account.AccountID"/> field.
			/// </value>
			[PXDefault]
			[Account(typeof(branchID), typeof(Search<Account.accountID,
				Where2<Match<Current<AccessInfo.userName>>,
					And<Account.active, Equal<True>,
					And<Account.isCashAccount, Equal<False>,
					And<Where<Current<GLSetup.ytdNetIncAccountID>, IsNull,
						Or<Account.accountID, NotEqual<Current<GLSetup.ytdNetIncAccountID>>>>>>>>>), DisplayName = "AR Account",
				BqlTable = typeof(ARDocumentResult))]
			public virtual int? ARAccountID
			{
				get;
				set;
			}
			#endregion
			#region ARSubID
			public abstract class aRSubID : PX.Data.BQL.BqlInt.Field<aRSubID> { }

			/// <summary>
			/// The identifier of the <see cref="Sub">subaccount</see> to which the document should be posted.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Sub.SubID"/> field.
			/// </value>
			[PXDefault]
			[SubAccount(typeof(aRAccountID), DescriptionField = typeof(Sub.description), DisplayName = "AR Subaccount", Visibility = PXUIVisibility.Visible,
				BqlTable = typeof(ARDocumentResult))]
			public virtual int? ARSubID
			{
				get;
				set;
			}
			#endregion
			#region IsMigratedRecord
			public abstract class isMigratedRecord : PX.Data.BQL.BqlBool.Field<isMigratedRecord> { }

			/// <summary>
			/// Specifies (if set to <c>true</c>) that the record has been created
			/// in migration mode without affecting GL module.
			/// </summary>
			[MigratedRecord(typeof(ARSetup.migrationMode), BqlTable = typeof(ARDocumentResult))]
			public virtual bool? IsMigratedRecord
			{
				get;
				set;
			}
			#endregion
			#region BatchNbr
			public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr> { }

			/// <summary>
			/// The number of the <see cref="Batch"/> created from the document on release.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Batch.BatchNbr"/> field.
			/// </value>
			[PXDBString(15, IsUnicode = true, BqlTable=typeof(ARDocumentResult))]
			[PXUIField(DisplayName = "Batch Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
			[BatchNbr(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAR>>>),
				IsMigratedRecordField = typeof(ARRegister.isMigratedRecord))]
			public virtual string BatchNbr
			{
				get;
				set;
				}
			#endregion
			#region Scheduled
			public abstract class scheduled : PX.Data.BQL.BqlBool.Field<scheduled> { }

			/// <summary>
			/// When set to <c>true</c> indicates that the document is part of a <c>Schedule</c> and serves as a template for generating other documents according to it.
			/// </summary>
			[PXDBBool(BqlTable=typeof(ARDocumentResult))]
			[PXDefault(false)]
			public virtual bool? Scheduled
			{
				get;
				set;
			}
			#endregion
			#region Voided
			public abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }

			/// <summary>
			/// When set to <c>true</c> indicates that the document has been voided.
			/// </summary>
			[PXDBBool(BqlTable = typeof(ARDocumentResult))]
			[PXDefault(false)]
			public virtual bool? Voided
			{
				get;
				set;
			}
			#endregion
			#region ExtRefNbr
			public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }
			[PXDBString(30, IsUnicode = true, BqlTable=typeof(ARDocumentResult))]
			[PXUIField(DisplayName = "Customer Invoice Nbr./Payment Nbr.")]
			public virtual string ExtRefNbr
			{
				get;
				set;
			}
			#endregion
			#region PaymentMethodID
			public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID> { }
			[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa", BqlTable=typeof(ARDocumentResult))]
			[PXUIField(DisplayName = "Payment Method")]
			public virtual string PaymentMethodID
			{
				get;
				set;
			}
			#endregion

			#region Original Amounts
			#region CuryOrigDocAmt
			public abstract class curyOrigDocAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmt> { }

			/// <summary>
			/// The amount of the document.
			/// Given in the <see cref="CuryID">currency of the document</see>.
			/// </summary>
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCurrency(typeof(ARRegister.curyInfoID), typeof(ARRegister.origDocAmt), BqlTable=typeof(ARDocumentResult))]
			[PXUIField(DisplayName = "Currency Origin. Amount", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual decimal? CuryOrigDocAmt
			{
				get;
				set;
			}
			#endregion
			#region OrigDocAmt
			public abstract class origDocAmt : PX.Data.BQL.BqlDecimal.Field<origDocAmt> { }

			/// <summary>
			/// The amount of the document.
			/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// </summary>
			[PXDBBaseCury(BqlTable=typeof(ARDocumentResult))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Origin. Amount", Visibility = PXUIVisibility.SelectorVisible)]

			public virtual decimal? OrigDocAmt
			{
				get;
				set;
			}
			#endregion
			#region CuryOrigDiscAmt
			public abstract class curyOrigDiscAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDiscAmt> { }

			/// <summary>
			/// The cash discount entered for the document.
			/// Given in the <see cref="CuryID">currency of the document</see>.
			/// </summary>
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCurrency(typeof(curyInfoID), typeof(origDiscAmt),BqlTable=typeof(ARDocumentResult))]
			[PXUIField(DisplayName = "Cash Discount", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual decimal? CuryOrigDiscAmt
			{
				get;
				set;
			}
			#endregion
			#region OrigDiscAmt
			public abstract class origDiscAmt : PX.Data.BQL.BqlDecimal.Field<origDiscAmt> { }

			/// <summary>
			/// The cash discount entered for the document.
			/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// </summary>
			[PXDBBaseCury(BqlTable = typeof(ARDocumentResult))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			public virtual decimal? OrigDiscAmt
			{
				get;
				set;
			}
			#endregion
			#region CuryRetainageTotal
			public abstract class curyRetainageTotal : PX.Data.BQL.BqlDecimal.Field<curyRetainageTotal> { }

			[PXDBCurrency(typeof(curyInfoID), typeof(retainageTotal),BqlTable=typeof(ARDocumentResult))]
			[PXUIField(DisplayName = "Original Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual decimal? CuryRetainageTotal
			{
				get;
				set;
			}
			#endregion
			#region RetainageTotal
			public abstract class retainageTotal : PX.Data.BQL.BqlDecimal.Field<retainageTotal> { }

			[PXDBBaseCury(BqlTable=typeof(ARDocumentResult))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Original Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			public virtual decimal? RetainageTotal
			{
				get;
				set;
			}
			#endregion
			#region CuryOrigDocAmtWithRetainageTotal
			public abstract class curyOrigDocAmtWithRetainageTotal : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmtWithRetainageTotal> { }

			[CM.PXDBCury(typeof(curyID), BqlTable=typeof(ARDocumentResult))]
			[PXUIField(DisplayName = "Total Amount", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual decimal? CuryOrigDocAmtWithRetainageTotal
			{
				get;
				set;
			}
			#endregion
			#region OrigDocAmtWithRetainageTotal
			public abstract class origDocAmtWithRetainageTotal : PX.Data.BQL.BqlDecimal.Field<origDocAmtWithRetainageTotal> { }

			[PXDBBaseCury(BqlTable=typeof(ARDocumentResult))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Total Amount", FieldClass = nameof(FeaturesSet.Retainage))]
			public virtual decimal? OrigDocAmtWithRetainageTotal
			{
				get;
				set;
			}
			#endregion
			#endregion


			#region Begin Balances
			#region CuryBegBalance
			public abstract class curyBegBalance : PX.Data.BQL.BqlDecimal.Field<curyBegBalance> { }
			protected decimal? _CuryBegBalance;
			[CM.PXCury(typeof(curyID))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Currency Period Beg. Balance")]
			[PXDBCalced(typeof(
				Switch<
					Case<Where<ARDocumentResult.released, Equal<False>,
								And<CurrentValue<ARDocumentFilter.useMasterCalendar>, Equal<False>,
								And<ARDocumentResult.finPeriodID, Less<CurrentValue<ARDocumentFilter.period>>>>>,
						ARDocumentResult.curyOrigDocAmt,
					Case<Where<ARDocumentResult.released, Equal<False>,
								And<CurrentValue<ARDocumentFilter.useMasterCalendar>, Equal<True>,
								And<ARDocumentResult.tranPeriodID, Less<CurrentValue<ARDocumentFilter.period>>>>>,
						ARDocumentResult.curyOrigDocAmt,
					Case<Where<CurrentValue<ARDocumentFilter.useMasterCalendar>, Equal<False>,
								And<ARTranPostGL.finPeriodID, Less<CurrentValue<ARDocumentFilter.period>>>>,
						ARTranPostGL.curyBalanceAmt,
					Case<Where<CurrentValue<ARDocumentFilter.useMasterCalendar>, Equal<True>,
								And<ARTranPostGL.tranPeriodID, Less<CurrentValue<ARDocumentFilter.period>>>>,
						ARTranPostGL.curyBalanceAmt,
					Case<Where<CurrentValue<ARDocumentFilter.useMasterCalendar>, Equal<False>,
								And<ARTranPostGL.finPeriodID, Less<CurrentValue<ARDocumentFilter.period>>>>,
						ARTranPostGL.curyBalanceAmt>>>>>,
					decimal0>), typeof(decimal))]
			public virtual decimal? CuryBegBalance
			{
				get;
				set;
			}
			#endregion
			#region BegBalance
			public abstract class begBalance : PX.Data.BQL.BqlDecimal.Field<begBalance> { }
			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Period Beg. Balance")]
			[PXDBCalced(typeof(
				Switch<
					Case<Where<ARDocumentResult.released, Equal<False>,
								And<CurrentValue<ARDocumentFilter.useMasterCalendar>, Equal<False>,
								And<ARDocumentResult.finPeriodID, Less<CurrentValue<ARDocumentFilter.period>>>>>,
						ARDocumentResult.origDocAmt,
					Case<Where<ARDocumentResult.released, Equal<False>,
								And<CurrentValue<ARDocumentFilter.useMasterCalendar>, Equal<True>,
								And<ARDocumentResult.tranPeriodID, Less<CurrentValue<ARDocumentFilter.period>>>>>,
						ARDocumentResult.origDocAmt,
					Case<Where<CurrentValue<ARDocumentFilter.useMasterCalendar>, Equal<True>,
								And<ARTranPostGL.tranPeriodID, Less<CurrentValue<ARDocumentFilter.period>>>>,
						ARTranPostGL.balanceAmt,
					Case<Where<CurrentValue<ARDocumentFilter.useMasterCalendar>, Equal<False>,
								And<ARTranPostGL.finPeriodID, Less<CurrentValue<ARDocumentFilter.period>>>>,
						ARTranPostGL.balanceAmt>>>>,
					decimal0>), typeof(decimal))]
			public virtual decimal? BegBalance
			{
				get;
				set;
			}
			#endregion
			#endregion

			#region Turns
			#region Turn
			public abstract class turn : PX.Data.BQL.BqlDecimal.Field<turn> { }
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDecimal]
			[PXDBCalced(typeof(
					Switch<
						Case<Where<CurrentValue<ARDocumentFilter.useMasterCalendar>, Equal<True>,
								And<ARTranPostGL.tranPeriodID, Equal<CurrentValue<ARDocumentFilter.period>>>>,
							decimal1,
						Case<Where<CurrentValue<ARDocumentFilter.useMasterCalendar>, Equal<False>,
								And<ARTranPostGL.finPeriodID, Equal<CurrentValue<ARDocumentFilter.period>>>>,
							decimal1>>,
						decimal0>), typeof(decimal))]
			public virtual decimal? Turn
			{
				get;
				set;
			}
			#endregion
			#region ARTurnover
			public abstract class aRTurnover : PX.Data.BQL.BqlDecimal.Field<aRTurnover> { }

			/// <summary>
			/// Expected AR turnover for the document.
			/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// </summary>
			[PXBaseCury(BqlTable=typeof(ARRegister))]
			[PXUIField(DisplayName = "AR Turnover")]
			public virtual decimal? ARTurnover
			{
				get;
				set;
			}
			#endregion
			#region GLTurnover
			public abstract class gLTurnover : PX.Data.BQL.BqlDecimal.Field<gLTurnover> { }

			/// <summary>
			/// Expected GL turnover for the document.
			/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// </summary>
			[PXBaseCury()]
			[PXUIField(DisplayName = "GL Turnover")]
			public virtual decimal? GLTurnover
			{
				get;
				set;
			}
			#endregion
			#endregion

			#region End Balances
			#region CuryDiscActTaken
			public abstract class curyDiscActTaken : PX.Data.BQL.BqlDecimal.Field<curyDiscActTaken> { }
			[PXDefault(TypeCode.Decimal, "0.0")]
			[CM.PXCury(typeof(ARRegister.curyID))]
			[PXUIField(DisplayName = "Currency Cash Discount Taken")]
			[PXDBCalced(typeof(
				IsNull<ARTranPostGL.curyTurnDiscAmt,decimal0>), typeof(decimal))]
			public virtual decimal? CuryDiscActTaken
			{
				get;
				set;
			}
			#endregion
			#region DiscActTaken
			public abstract class discActTaken : PX.Data.BQL.BqlDecimal.Field<discActTaken> { }
			[PXBaseCury()]
			[PXUIField(DisplayName = "Cash Discount Taken")]
			[PXDBCalced(typeof(
				IsNull<ARTranPostGL.turnDiscAmt,decimal0>), typeof(decimal))]
			public virtual decimal? DiscActTaken
			{
				get;
				set;
			}
			#endregion
			#region CuryWOAmt
			public abstract class curyWOAmt : PX.Data.BQL.BqlDecimal.Field<curyWOAmt> { }
			[PXDefault(TypeCode.Decimal, "0.0")]
			[CM.PXCury(typeof(ARRegister.curyID))]
			[PXUIField(DisplayName = "Currency Write-Off Amount")]
			[PXDBCalced(typeof(
				IsNull<ARTranPostGL.curyTurnWOAmt,decimal0>), typeof(decimal))]
			public virtual decimal? CuryWOAmt
			{
				get;
				set;
			}
			#endregion
			#region WOAmt
			public abstract class woAmt : PX.Data.BQL.BqlDecimal.Field<woAmt> { }
			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Write-Off Amount")]
			[PXDBCalced(typeof(
				IsNull<ARTranPostGL.turnWOAmt,decimal0>), typeof(decimal))]
			public virtual decimal? WOAmt
			{
				get;
				set;
			}
			#endregion
			#region RGOLAmt
			public abstract class rGOLAmt : PX.Data.BQL.BqlDecimal.Field<rGOLAmt> { }
			[PXBaseCury(BqlTable = typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "RGOL Amount")]
			[PXDBCalced(typeof(
				Switch<
					Case<Where<ARTranPostGL.type, NotEqual<ARTranPost.type.application>>, decimal0>,
					IsNull<Data.Minus<ARTranPostGL.rGOLAmt>,decimal0>>), typeof(decimal))]
			public virtual decimal? RGOLAmt
			{
				get;
				set;
			}
			#endregion

			#region CuryDocBal
			public abstract class curyDocBal : PX.Data.BQL.BqlDecimal.Field<curyDocBal> { }

			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXCurrency(typeof(curyInfoID), typeof(curyDocBal))]
			[PXUIField(DisplayName = "Currency Balance")]
			[PXDBCalced(typeof(
				Switch<Case<Where<ARDocumentResult.released, Equal<False>, And<ARDocumentResult.docType.IsNotIn<ARDocType.cashSale, ARDocType.cashReturn>>>,
					ARDocumentResult.curyOrigDocAmt>,
					ARTranPostGL.curyBalanceAmt>), typeof(decimal))]
			public virtual decimal? CuryDocBal
			{
				get;
				set;
			}
			#endregion
			#region DocBal
			public abstract class docBal : PX.Data.BQL.BqlDecimal.Field<docBal> { }

			[PXBaseCury(BqlTable=typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance")]
			[PXDBCalced(typeof(
				Switch<Case<Where<ARDocumentResult.released, Equal<False>, And<ARDocumentResult.docType.IsNotIn<ARDocType.cashSale, ARDocType.cashReturn>>>,
						ARDocumentResult.origDocAmt>,
					ARTranPostGL.balanceAmt>), typeof(decimal))]
			public virtual decimal? DocBal
			{
				get;
				set;
			}
			#endregion
			#region CuryRetainageUnreleasedAmt
			public abstract class curyRetainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<curyRetainageUnreleasedAmt> { }

			[PXCurrency(typeof(curyInfoID), typeof(retainageUnreleasedAmt),
				BqlTable = typeof(ARRegister))]
			[PXUIField(DisplayName = "Unreleased Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXDBCalced(typeof(
				IsNull<ARTranPostGL.curyRetainageUnreleasedAmt,decimal0>), typeof(decimal))]
			public virtual decimal? CuryRetainageUnreleasedAmt
			{
				get;
				set;
			}
			#endregion
			#region RetainageUnreleasedAmt
			public abstract class retainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<retainageUnreleasedAmt> { }

			[PXBaseCury(BqlTable = typeof(ARRegister))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Unreleased Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDBCalced(typeof(
				IsNull<ARTranPostGL.retainageUnreleasedAmt,decimal0>), typeof(decimal))]
			public virtual decimal? RetainageUnreleasedAmt
			{
				get;
				set;
			}
			#endregion
			#endregion
			#region TranPostPeriodID
			public abstract class tranPostPeriodID : PX.Data.BQL.BqlString.Field<tranPostPeriodID> { }
			[PeriodID(BqlField = typeof(ARTranPostGL.tranPeriodID))]
			public virtual string TranPostPeriodID
			{
				get;
				set;
			}
			#endregion
			#region FinPostPeriodID
			public abstract class finPostPeriodID : PX.Data.BQL.BqlString.Field<finPostPeriodID> { }
			/// <summary>
			/// <see cref="FinPeriod">Financial Period</see> of the document.
			/// </summary>
			/// <value>
			/// Defaults to the period, to which the <see cref="ARRegister.DocDate"/> belongs, but can be virtualn by user.
			/// </value>
			[AROpenPeriod(
				typeof(docDate),
				branchSourceType: typeof(branchID),
				masterFinPeriodIDType: typeof(tranPostPeriodID),
				IsHeader = true,
				BqlField=typeof(ARTranPostGL.finPeriodID))]
			public virtual string FinPostPeriodID
			{
				get;
				set;
			}
			#endregion
		}

		[Serializable()]
        [PXProjection(typeof(Select2<ARDocumentResult,
        	LeftJoin<ARTranPostGL,
        		On<ARTranPostGL.tranType, Equal<ARDocumentResult.docType>,
        		And<ARTranPostGL.tranRefNbr, Equal<ARDocumentResult.refNbr>>>>>))]
        [PXHidden]
        public partial class GLDocumentPeriodResult : IBqlTable
        {
        	#region DocType
        	public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
        	{
        		public const int Length = 3;
        	}

        	/// <summary>
        	/// The type of the document.
        	/// This field is a part of the compound key of the document.
        	/// </summary>
        	/// <value>
        	/// The field can have one of the values described in <see cref="ARDocType.ListAttribute"/>.
        	/// </value>
        	[PXDBString(docType.Length, IsKey = true, IsFixed = true, BqlTable = typeof(ARDocumentResult))]
        	[PXDefault()]
        	[ARDocType.List()]
        	[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = true, TabOrder = 0)]
        	public virtual string DocType
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region RefNbr

        	public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
        	{
        	}

        	/// <summary>
        	/// The reference number of the document.
        	/// This field is a part of the compound key of the document.
        	/// </summary>
        	[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "", BqlTable = typeof(ARDocumentResult))]
        	[PXDefault()]
        	[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible, TabOrder = 1)]
        	[PXSelector(typeof(Search<ARRegister.refNbr, Where<ARRegister.docType, Equal<Optional<ARRegister.docType>>>>), Filterable = true)]
        	public virtual string RefNbr
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region BranchID
        	public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
        	/// <summary>
        	/// The identifier of the <see cref="Branch">branch</see> to which the document belongs.
        	/// </summary>
        	/// <value>
        	/// Corresponds to the <see cref="Branch.BranchID"/> field.
        	/// </value>
        	[Branch(BqlTable=typeof(ARTranPostGL))]
        	public virtual int? BranchID
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region CustomerID
        	public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }

        	[Customer(Enabled = false, Visible = false, DescriptionField = typeof(Customer.acctName), BqlField = typeof(ARTranPostGL.referenceID))]
        	public virtual int? CustomerID
        	{
        		get;
        		set;
        	}
        	#endregion

        	#region DocDate
        	public abstract class docDate : PX.Data.BQL.BqlDateTime.Field<docDate> { }
        	/// <summary>
        	/// The date of the document.
        	/// </summary>
        	/// <value>
        	/// Defaults to the current <see cref="AccessInfo.BusinessDate">Business Date</see>.
        	/// </value>
        	[PXDBDate(BqlTable = typeof(ARDocumentResult))]
        	[PXDefault(typeof(AccessInfo.businessDate))]
        	[PXUIField(DisplayName = "Date", Visibility = PXUIVisibility.SelectorVisible)]
        	public virtual DateTime? DocDate
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region DocDesc
        	public abstract class docDesc : PX.Data.BQL.BqlString.Field<docDesc> { }

        	/// <summary>
        	/// The description of the document.
        	/// </summary>
        	[PXDBString(Common.Constants.TranDescLength, IsUnicode = true, BqlTable = typeof(ARDocumentResult))]
        	[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
        	public virtual string DocDesc
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region DueDate
        	public abstract class dueDate : PX.Data.BQL.BqlDateTime.Field<dueDate> { }
        	[PXDBDate(BqlTable = typeof(ARDocumentResult))]
        	[PXUIField(DisplayName = "Due Date", Visibility = PXUIVisibility.SelectorVisible)]
        	public virtual DateTime? DueDate
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region CuryID
        	public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
        	/// <summary>
        	/// The code of the <see cref="Currency"/> of the document.
        	/// </summary>
        	/// <value>
        	/// Defaults to the <see cref="Company.BaseCuryID">base currency of the company</see>.
        	/// Corresponds to the <see cref="Currency.CuryID"/> field.
        	/// </value>
        	[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL", BqlTable = typeof(ARDocumentResult))]
        	[PXUIField(DisplayName = "Currency", Visibility = PXUIVisibility.SelectorVisible)]
        	[PXDefault(typeof(Current<AccessInfo.baseCuryID>))]
        	[PXSelector(typeof(Currency.curyID))]
        	public virtual string CuryID
        	{
        		get;
        		set;
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
        	[PXDBLong(BqlTable = typeof(ARDocumentResult))]
        	[CurrencyInfo]
        	public virtual long? CuryInfoID
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region OrigDocType
        	public abstract class origDocType : PX.Data.BQL.BqlString.Field<origDocType> { }

        	/// <summary>
        	/// The type of the original (source) document.
        	/// </summary>
        	/// <value>
        	/// Corresponds to the <see cref="DocType"/> field.
        	/// </value>
        	[PXDBString(3, IsFixed = true, BqlTable=typeof(ARDocumentResult))]
        	[ARDocType.List()]
        	[PXUIField(DisplayName = "Orig. Doc. Type")]
        	public virtual string OrigDocType
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region OrigRefNbr
        	public abstract class origRefNbr : PX.Data.BQL.BqlString.Field<origRefNbr> { }

        	/// <summary>
        	/// The reference number of the original (source) document.
        	/// </summary>
        	/// <value>
        	/// Corresponds to the <see cref="RefNbr"/> field.
        	/// </value>
        	[PXDBString(15, IsUnicode = true, InputMask = "", BqlTable = typeof(ARDocumentResult))]
        	[PXUIField(DisplayName = "Orig. Ref. Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        	public virtual string OrigRefNbr
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region Status
        	public abstract class status : PX.Data.BQL.BqlString.Field<status> { }
        	/// <summary>
        	/// The status of the document.
        	/// The value of the field is determined by the values of the status flags,
        	/// such as <see cref="Hold"/>, <see cref="Released"/>, <see cref="Voided"/>, <see cref="Scheduled"/>.
        	/// </summary>
        	/// <value>
        	/// The field can have one of the values described in <see cref="ARDocStatus.ListAttribute"/>.
        	/// Defaults to <see cref="ARDocStatus.Hold"/>.
        	/// </value>
        	[PXDBString(1, IsFixed = true, BqlTable=typeof(ARDocumentResult))]
        	[PXDefault(ARDocStatus.Hold)]
        	[PXUIField(DisplayName = "Status", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
        	[ARDocStatus.List]
        	public virtual string Status
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region TranPeriodID
        	public abstract class tranPeriodID : PX.Data.BQL.BqlString.Field<tranPeriodID> { }

        	/// <summary>
        	/// <see cref="FinPeriod">Financial Period</see> of the document.
        	/// </summary>
        	/// <value>
        	/// Determined by the <see cref="ARRegister.DocDate">date of the document</see>. Unlike <see cref="ARRegister.FinPeriodID"/>
        	/// the value of this field can't be virtualn by user.
        	/// </value>
        	[PeriodID(BqlTable = typeof(ARDocumentResult))]
        	public virtual string TranPeriodID
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region FinPeriodID
        	public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }

        	/// <summary>
        	/// <see cref="FinPeriod">Financial Period</see> of the document.
        	/// </summary>
        	/// <value>
        	/// Defaults to the period, to which the <see cref="ARRegister.DocDate"/> belongs, but can be virtualn by user.
        	/// </value>
        	[AROpenPeriod(
        		typeof(docDate),
        		branchSourceType: typeof(branchID),
        		masterFinPeriodIDType: typeof(tranPeriodID),
        		IsHeader = true,
        		BqlTable=typeof(ARDocumentResult))]
        	[PXUIField(DisplayName = "Post Period", Visibility = PXUIVisibility.SelectorVisible)]
        	public virtual string FinPeriodID
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region ClosedFinPeriodID
        	public abstract class closedFinPeriodID : PX.Data.BQL.BqlString.Field<closedFinPeriodID> { }

        	/// <summary>
        	/// The <see cref="FinancialPeriod">Financial Period</see>, in which the document was closed.
        	/// </summary>
        	/// <value>
        	/// Corresponds to the <see cref="FinPeriodID"/> field.
        	/// </value>
        	[FinPeriodID(
        		branchSourceType: typeof(branchID),
        		masterFinPeriodIDType: typeof(closedTranPeriodID),
        		BqlTable = typeof(ARDocumentResult))]
        	[PXUIField(DisplayName = "Closed Period", Visibility = PXUIVisibility.Invisible)]
        	public virtual string ClosedFinPeriodID
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region ClosedTranPeriodID
        	public abstract class closedTranPeriodID : PX.Data.BQL.BqlString.Field<closedTranPeriodID> { }

        	/// <summary>
        	/// The <see cref="FinancialPeriod">Financial Period</see>, in which the document was closed.
        	/// </summary>
        	/// <value>
        	/// Corresponds to the <see cref="TranPeriodID"/> field.
        	/// </value>
        	[PeriodID(BqlTable = typeof(ARDocumentResult))]
        	[PXUIField(DisplayName = "Closed Period", Visibility = PXUIVisibility.Invisible)]
        	public virtual string ClosedTranPeriodID
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region NoteID
        	public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
        	protected Guid? _NoteID;

        	/// <summary>
        	/// Identifier of the <see cref="PX.Data.Note">Note</see> object, associated with the document.
        	/// </summary>
        	/// <value>
        	/// Corresponds to the <see cref="PX.Data.Note.NoteID">Note.NoteID</see> field.
        	/// </value>
        	[PXNote(BqlTable=typeof(ARDocumentResult))]
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
        	#region OpenDoc
        	public abstract class openDoc : PX.Data.BQL.BqlBool.Field<openDoc> { }

        	/// <summary>
        	/// When set to <c>true</c>, indicates that the document is open.
        	/// </summary>
        	[PXDBBool(BqlTable=typeof(ARDocumentResult))]
        	[PXDefault(true)]
        	public virtual bool? OpenDoc
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region Released
        	public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }

        	/// <summary>
        	/// When set to <c>true</c>, indicates that the document has been released.
        	/// </summary>
        	[PXDBBool(BqlTable=typeof(ARDocumentResult))]
        	[PXDefault(false)]
        	public virtual bool? Released
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region IsRetainageDocument
        	public abstract class isRetainageDocument : PX.Data.BQL.BqlBool.Field<isRetainageDocument> { }

        	[PXDBBool(BqlTable=typeof(ARDocumentResult))]
        	[PXUIField(DisplayName = "Retainage Document", Enabled = false, FieldClass = nameof(FeaturesSet.Retainage))]
        	[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        	public virtual bool? IsRetainageDocument
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region ARAccountID
        	public abstract class aRAccountID : PX.Data.BQL.BqlInt.Field<aRAccountID> { }

        	/// <summary>
        	/// The identifier of the <see cref="Account">AR account</see> to which the document should be posted.
        	/// The Cash account and Year-to-Date Net Income account cannot be selected as the value of this field.
        	/// </summary>
        	/// <value>
        	/// Corresponds to the <see cref="Account.AccountID"/> field.
        	/// </value>
        	[PXDefault]
        	[Account(typeof(branchID), typeof(Search<Account.accountID,
        		Where2<Match<Current<AccessInfo.userName>>,
        			And<Account.active, Equal<True>,
        			And<Account.isCashAccount, Equal<False>,
        			And<Where<Current<GLSetup.ytdNetIncAccountID>, IsNull,
        				Or<Account.accountID, NotEqual<Current<GLSetup.ytdNetIncAccountID>>>>>>>>>), DisplayName = "AR Account",
        		BqlField = typeof(ARTranPostGL.accountID))]
        	public virtual int? ARAccountID
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region ARSubID
        	public abstract class aRSubID : PX.Data.BQL.BqlInt.Field<aRSubID> { }

        	/// <summary>
        	/// The identifier of the <see cref="Sub">subaccount</see> to which the document should be posted.
        	/// </summary>
        	/// <value>
        	/// Corresponds to the <see cref="Sub.SubID"/> field.
        	/// </value>
        	[PXDefault]
        	[SubAccount(typeof(aRAccountID), DescriptionField = typeof(Sub.description), DisplayName = "AR Subaccount", Visibility = PXUIVisibility.Visible,
        		BqlField = typeof(ARTranPostGL.subID))]
        	public virtual int? ARSubID
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region IsMigratedRecord
        	public abstract class isMigratedRecord : PX.Data.BQL.BqlBool.Field<isMigratedRecord> { }

        	/// <summary>
        	/// Specifies (if set to <c>true</c>) that the record has been created
        	/// in migration mode without affecting GL module.
        	/// </summary>
        	[MigratedRecord(typeof(ARSetup.migrationMode), BqlTable = typeof(ARDocumentResult))]
        	public virtual bool? IsMigratedRecord
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region BatchNbr
        	public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr> { }

        	/// <summary>
        	/// The number of the <see cref="Batch"/> created from the document on release.
        	/// </summary>
        	/// <value>
        	/// Corresponds to the <see cref="Batch.BatchNbr"/> field.
        	/// </value>
        	[PXDBString(15, IsUnicode = true, BqlTable=typeof(ARDocumentResult))]
        	[PXUIField(DisplayName = "Batch Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
        	[BatchNbr(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAR>>>),
        		IsMigratedRecordField = typeof(ARRegister.isMigratedRecord))]
        	public virtual string BatchNbr
        	{
        		get;
        		set;
        		}
        	#endregion
        	#region Scheduled
        	public abstract class scheduled : PX.Data.BQL.BqlBool.Field<scheduled> { }

        	/// <summary>
        	/// When set to <c>true</c> indicates that the document is part of a <c>Schedule</c> and serves as a template for generating other documents according to it.
        	/// </summary>
        	[PXDBBool(BqlTable=typeof(ARDocumentResult))]
        	[PXDefault(false)]
        	public virtual bool? Scheduled
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region Voided
        	public abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }

        	/// <summary>
        	/// When set to <c>true</c> indicates that the document has been voided.
        	/// </summary>
        	[PXDBBool(BqlTable = typeof(ARDocumentResult))]
        	[PXDefault(false)]
        	public virtual bool? Voided
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region ExtRefNbr
        	public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }
        	[PXDBString(30, IsUnicode = true, BqlTable=typeof(ARDocumentResult))]
        	[PXUIField(DisplayName = "Customer Invoice Nbr./Payment Nbr.")]
        	public virtual string ExtRefNbr
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region PaymentMethodID
        	public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID> { }
        	[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa", BqlTable=typeof(ARDocumentResult))]
        	[PXUIField(DisplayName = "Payment Method")]
        	public virtual string PaymentMethodID
        	{
        		get;
        		set;
        	}
        	#endregion

        	#region Original Amounts
        	#region CuryOrigDocAmt
        	public abstract class curyOrigDocAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmt> { }

        	/// <summary>
        	/// The amount of the document.
        	/// Given in the <see cref="CuryID">currency of the document</see>.
        	/// </summary>
        	[PXDefault(TypeCode.Decimal, "0.0")]
        	[PXDBCurrency(typeof(ARRegister.curyInfoID), typeof(ARRegister.origDocAmt), BqlTable=typeof(ARDocumentResult))]
        	[PXUIField(DisplayName = "Currency Origin. Amount", Visibility = PXUIVisibility.SelectorVisible)]
        	public virtual decimal? CuryOrigDocAmt
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region OrigDocAmt
        	public abstract class origDocAmt : PX.Data.BQL.BqlDecimal.Field<origDocAmt> { }

        	/// <summary>
        	/// The amount of the document.
        	/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
        	/// </summary>
        	[PXDBBaseCury(BqlTable=typeof(ARDocumentResult))]
        	[PXDefault(TypeCode.Decimal, "0.0")]
        	[PXUIField(DisplayName = "Origin. Amount", Visibility = PXUIVisibility.SelectorVisible)]

        	public virtual decimal? OrigDocAmt
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region CuryOrigDiscAmt
        	public abstract class curyOrigDiscAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDiscAmt> { }

        	/// <summary>
        	/// The cash discount entered for the document.
        	/// Given in the <see cref="CuryID">currency of the document</see>.
        	/// </summary>
        	[PXDefault(TypeCode.Decimal, "0.0")]
        	[PXDBCurrency(typeof(curyInfoID), typeof(origDiscAmt),BqlTable=typeof(ARDocumentResult))]
        	[PXUIField(DisplayName = "Cash Discount", Visibility = PXUIVisibility.SelectorVisible)]
        	public virtual decimal? CuryOrigDiscAmt
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region OrigDiscAmt
        	public abstract class origDiscAmt : PX.Data.BQL.BqlDecimal.Field<origDiscAmt> { }

        	/// <summary>
        	/// The cash discount entered for the document.
        	/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
        	/// </summary>
        	[PXDBBaseCury(BqlTable = typeof(ARDocumentResult))]
        	[PXDefault(TypeCode.Decimal, "0.0")]
        	public virtual decimal? OrigDiscAmt
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region CuryRetainageTotal
        	public abstract class curyRetainageTotal : PX.Data.BQL.BqlDecimal.Field<curyRetainageTotal> { }

        	[PXDBCurrency(typeof(curyInfoID), typeof(retainageTotal),BqlTable=typeof(ARDocumentResult))]
        	[PXUIField(DisplayName = "Original Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
        	[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
        	public virtual decimal? CuryRetainageTotal
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region RetainageTotal
        	public abstract class retainageTotal : PX.Data.BQL.BqlDecimal.Field<retainageTotal> { }

        	[PXDBBaseCury(BqlTable=typeof(ARDocumentResult))]
        	[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
        	[PXUIField(DisplayName = "Original Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
        	public virtual decimal? RetainageTotal
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region CuryOrigDocAmtWithRetainageTotal
        	public abstract class curyOrigDocAmtWithRetainageTotal : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmtWithRetainageTotal> { }

        	[CM.PXDBCury(typeof(curyID), BqlTable=typeof(ARDocumentResult))]
        	[PXUIField(DisplayName = "Total Amount", FieldClass = nameof(FeaturesSet.Retainage))]
        	[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
        	public virtual decimal? CuryOrigDocAmtWithRetainageTotal
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region OrigDocAmtWithRetainageTotal
        	public abstract class origDocAmtWithRetainageTotal : PX.Data.BQL.BqlDecimal.Field<origDocAmtWithRetainageTotal> { }

        	[PXDBBaseCury(BqlTable=typeof(ARDocumentResult))]
        	[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
        	[PXUIField(DisplayName = "Total Amount", FieldClass = nameof(FeaturesSet.Retainage))]
        	public virtual decimal? OrigDocAmtWithRetainageTotal
        	{
        		get;
        		set;
        	}
        	#endregion
        	#endregion
        	#region Begin Balance
        	#region CuryBegBalance
        	public abstract class curyBegBalance : PX.Data.BQL.BqlDecimal.Field<curyBegBalance> { }
        	protected decimal? _CuryBegBalance;
        	[CM.PXCury(typeof(curyID))]
        	[PXDefault(TypeCode.Decimal, "0.0")]
        	[PXUIField(DisplayName = "Currency Period Beg. Balance")]
        	[PXDBCalced(typeof(decimal0), typeof(decimal))]
        	public virtual decimal? CuryBegBalance
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region BegBalance
        	public abstract class begBalance : PX.Data.BQL.BqlDecimal.Field<begBalance> { }
        	[PXBaseCury()]
        	[PXDefault(TypeCode.Decimal, "0.0")]
        	[PXUIField(DisplayName = "Period Beg. Balance")]
            [PXDBCalced(typeof(decimal0), typeof(decimal))]
            public virtual decimal? BegBalance
        	{
        		get;
        		set;
        	}
        	#endregion
        	#endregion
        	#region Turns
            #region CuryDiscActTaken
        	public abstract class curyDiscActTaken : PX.Data.BQL.BqlDecimal.Field<curyDiscActTaken> { }
        	[CM.PXCury(typeof(ARRegister.curyID))]
        	[PXUIField(DisplayName = "Currency Cash Discount Taken")]
            [PXDBCalced(typeof(decimal0), typeof(decimal))]
            public virtual decimal? CuryDiscActTaken
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region DiscActTaken
        	public abstract class discActTaken : PX.Data.BQL.BqlDecimal.Field<discActTaken> { }
        	[PXBaseCury()]
        	[PXUIField(DisplayName = "Cash Discount Taken")]
            [PXDBCalced(typeof(decimal0), typeof(decimal))]
            public virtual decimal? DiscActTaken
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region CuryWOAmt
        	public abstract class curyWOAmt : PX.Data.BQL.BqlDecimal.Field<curyWOAmt> { }
        	[PXDefault(TypeCode.Decimal, "0.0")]
        	[CM.PXCury(typeof(ARRegister.curyID))]
        	[PXUIField(DisplayName = "Currency Write-Off Amount")]
            [PXDBCalced(typeof(decimal0), typeof(decimal))]
        	public virtual decimal? CuryWOAmt
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region WOAmt
        	public abstract class woAmt : PX.Data.BQL.BqlDecimal.Field<woAmt> { }
        	[PXBaseCury()]
        	[PXDefault(TypeCode.Decimal, "0.0")]
        	[PXUIField(DisplayName = "Write-Off Amount")]
            [PXDBCalced(typeof(decimal0), typeof(decimal))]
            public virtual decimal? WOAmt
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region RGOLAmt
        	public abstract class rGOLAmt : PX.Data.BQL.BqlDecimal.Field<rGOLAmt> { }
        	[PXBaseCury(BqlTable = typeof(ARRegister))]
        	[PXDefault(TypeCode.Decimal, "0.0")]
        	[PXUIField(DisplayName = "RGOL Amount")]
            [PXDBCalced(typeof(decimal0), typeof(decimal))]
            public virtual decimal? RGOLAmt
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region ARTurnover
        	public abstract class aRTurnover : PX.Data.BQL.BqlDecimal.Field<aRTurnover> { }

        	/// <summary>
        	/// Expected GL turnover for the document.
        	/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
        	/// </summary>
        	[PXBaseCury(BqlTable=typeof(ARRegister))]
        	[PXUIField(DisplayName = "AR Turnover")]
        	[PXDBCalced(typeof(
        		Switch<
        			Case<Where<CurrentValue<ARDocumentFilter.useMasterCalendar>, Equal<True>,
        						And<ARTranPostGL.tranPeriodID, Equal<CurrentValue<ARDocumentFilter.period>>>>,
        				Sub<ARTranPostGL.debitARAmt,ARTranPostGL.creditARAmt>,
        			Case<Where<CurrentValue<ARDocumentFilter.useMasterCalendar>, Equal<False>,
        						And<ARTranPostGL.finPeriodID, Equal<CurrentValue<ARDocumentFilter.period>>>>,
        				Sub<ARTranPostGL.debitARAmt,ARTranPostGL.creditARAmt>>>,
        			decimal0>), typeof(decimal))]
        	public virtual decimal? ARTurnover
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region GLTurnover
        	public abstract class gLTurnover : PX.Data.BQL.BqlDecimal.Field<gLTurnover> { }

        	/// <summary>
        	/// Expected GL turnover for the document.
        	/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
        	/// </summary>
        	[PXBaseCury()]
        	[PXUIField(DisplayName = "GL Turnover")]
        	public virtual decimal? GLTurnover
        	{
        		get;
        		set;
        	}
        	#endregion
        	#endregion
        	#region End Balance
        	#region CuryDocBal
        	public abstract class curyDocBal : PX.Data.BQL.BqlDecimal.Field<curyDocBal> { }

        	[PXDefault(TypeCode.Decimal, "0.0")]
        	[PXCurrency(typeof(curyInfoID), typeof(curyDocBal))]
        	[PXUIField(DisplayName = "Currency Balance")]
            [PXDBCalced(typeof(decimal0), typeof(decimal))]
            public virtual decimal? CuryDocBal
        		{
        		get;
        		set;
        		}
        	#endregion
        	#region DocBal
        	public abstract class docBal : PX.Data.BQL.BqlDecimal.Field<docBal> { }

        	[PXBaseCury(BqlTable=typeof(ARRegister))]
        	[PXDefault(TypeCode.Decimal, "0.0")]
        	[PXUIField(DisplayName = "Balance")]
            [PXDBCalced(typeof(decimal0), typeof(decimal))]
            public virtual decimal? DocBal
        	{
        		get;
        		set;
        	}
        	#region CuryRetainageUnreleasedAmt
        	public abstract class curyRetainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<curyRetainageUnreleasedAmt> { }

        	[PXCurrency(typeof(curyInfoID), typeof(retainageUnreleasedAmt),
        		BqlTable = typeof(ARRegister))]
        	[PXUIField(DisplayName = "Unreleased Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
        	[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
            [PXDBCalced(typeof(
                IsNull<ARTranPostGL.curyRetainageUnreleasedAmt, decimal0>), typeof(decimal))]
            public virtual decimal? CuryRetainageUnreleasedAmt
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region RetainageUnreleasedAmt
        	public abstract class retainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<retainageUnreleasedAmt> { }

        	[PXBaseCury(BqlTable = typeof(ARRegister))]
        	[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
        	[PXUIField(DisplayName = "Unreleased Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
        	[PXDBCalced(typeof(
        		IsNull<ARTranPostGL.retainageUnreleasedAmt, decimal0>), typeof(decimal))]
        	public virtual decimal? RetainageUnreleasedAmt
        	{
        		get;
        		set;
        	}
        	#endregion
        	#endregion
        	#endregion
        	#region TranPostPeriodID
        	public abstract class tranPostPeriodID : PX.Data.BQL.BqlString.Field<tranPostPeriodID> { }
        	[PeriodID(BqlField = typeof(ARTranPostGL.tranPeriodID))]
        	public virtual string TranPostPeriodID
        	{
        		get;
        		set;
        	}
        	#endregion
        	#region FinPostPeriodID
        	public abstract class finPostPeriodID : PX.Data.BQL.BqlString.Field<finPostPeriodID> { }
        	/// <summary>
        	/// <see cref="FinPeriod">Financial Period</see> of the document.
        	/// </summary>
        	/// <value>
        	/// Defaults to the period, to which the <see cref="ARRegister.DocDate"/> belongs, but can be virtualn by user.
        	/// </value>
        	[AROpenPeriod(
        		typeof(docDate),
        		branchSourceType: typeof(branchID),
        		masterFinPeriodIDType: typeof(tranPostPeriodID),
        		IsHeader = true,
        		BqlField=typeof(ARTranPostGL.finPeriodID))]
        	public virtual string FinPostPeriodID
        	{
        		get;
        		set;
        	}
        	#endregion
        }

		[System.SerializableAttribute()]
		[PXHidden()]
		public partial class GLTran : GL.GLTran
		{
			#region BranchID
			public new abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
			#endregion
			#region Module
			public new abstract class module : PX.Data.BQL.BqlString.Field<module> { }
			#endregion
			#region BatchNbr
			public new abstract class batchNbr : IBqlField { }
			#endregion
			#region LineNbr
			public new abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
			#endregion
			#region LedgerID
			public new abstract class ledgerID : PX.Data.BQL.BqlInt.Field<ledgerID> { }
			#endregion
			#region AccountID
			public new abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }
			#endregion
			#region SubID
			public new abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }
			#endregion
			#region CreditAmt
			public new abstract class creditAmt : PX.Data.BQL.BqlDecimal.Field<creditAmt> { }
			#endregion
			#region DebitAmt
			public new abstract class debitAmt : PX.Data.BQL.BqlDecimal.Field<debitAmt> { }
			#endregion
			#region Posted
			public new abstract class posted : PX.Data.BQL.BqlBool.Field<posted> { }
			#endregion
			#region TranType
			public new abstract class tranType : PX.Data.BQL.BqlString.Field<tranType> { }
			#endregion
			#region RefNbr
			public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
			#endregion
			#region ReferenceID
			public new abstract class referenceID : PX.Data.BQL.BqlInt.Field<referenceID> { }
			#endregion
			#region GLTurnover
			public abstract class gLTurnover : PX.Data.BQL.BqlDecimal.Field<gLTurnover> { }
			[PXBaseCury()]
			[PXDBCalced(typeof(
				Mult<
					Switch<
						Case<Where<CurrentValue<ARDocumentFilter.useMasterCalendar>, Equal<True>, And<tranPeriodID, Equal<CurrentValue<ARDocumentFilter.period>>>>, decimal1,
						Case<Where<CurrentValue<ARDocumentFilter.useMasterCalendar>, Equal<False>, And<finPeriodID, Equal<CurrentValue<ARDocumentFilter.period>>>>, decimal1>>,
						decimal0>,
					Sub<debitAmt, creditAmt>>
			), typeof(decimal))]
			public virtual decimal? GLTurnover
				{
				get;
				set;
				}
			#endregion
			}

		public class Ref
		{
			[PXHidden]
			public class ARInvoice : IBqlTable
			{
				#region DocType
				public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
				{
					public const int Length = 3;
				}

				/// <summary>
				/// The type of the document.
				/// This field is a part of the compound key of the document.
				/// </summary>
				/// <value>
				/// The field can have one of the values described in <see cref="ARDocType.ListAttribute"/>.
				/// </value>
				[PXDBString(docType.Length, IsKey = true, IsFixed = true)]
				[PXDefault()]
				[ARDocType.List()]
				[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = true, TabOrder = 0)]
				public virtual string DocType
				{
					get;
					set;
			}
			#endregion
				#region RefNbr

				public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
			{
				}

				/// <summary>
				/// The reference number of the document.
				/// This field is a part of the compound key of the document.
				/// </summary>
				[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "")]
				[PXDefault()]
				[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible, TabOrder = 1)]
				[PXSelector(typeof(Search<ARRegister.refNbr, Where<ARRegister.docType, Equal<Optional<ARRegister.docType>>>>), Filterable = true)]
				public virtual string RefNbr
				{
					get;
					set;
				}
				#endregion
				#region InvoiceNbr
				public abstract class invoiceNbr : PX.Data.BQL.BqlString.Field<invoiceNbr> { }
				protected string _InvoiceNbr;

				/// <summary>
				/// The original reference number or ID assigned by the customer to the customer document.
				/// </summary>
				[PXDBString(40, IsUnicode = true)]
				[PXUIField(DisplayName = "Customer Order", Visibility = PXUIVisibility.SelectorVisible, Required = false)]
				public virtual string InvoiceNbr
					{
					get
					{
						return this._InvoiceNbr;
				}
				set
				{
						this._InvoiceNbr = value;
				}
			}
			#endregion
				#region InstallmentCntr
				public abstract class installmentCntr : PX.Data.BQL.BqlShort.Field<installmentCntr> { }
				protected short? _InstallmentCntr;

				/// <summary>
				/// The counter of <see cref="TermsInstallment">installments</see> associated with the document.
				/// </summary>
				[PXDBShort()]
				public virtual short? InstallmentCntr
			{
				get
				{
						return this._InstallmentCntr;
				}
				set
				{
						this._InstallmentCntr = value;
				}
			}
			#endregion

			}
			[PXHidden]
			public class ARPayment : IBqlTable
			{
				#region DocType
				public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
				{
					public const int Length = 3;
				}

			/// <summary>
				/// The type of the document.
				/// This field is a part of the compound key of the document.
			/// </summary>
				/// <value>
				/// The field can have one of the values described in <see cref="ARDocType.ListAttribute"/>.
				/// </value>
				[PXDBString(docType.Length, IsKey = true, IsFixed = true)]
				[PXDefault()]
				[ARDocType.List()]
				[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = true, TabOrder = 0)]
				public virtual string DocType
			{
				get;
				set;
			}
			#endregion
				#region RefNbr

				public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
				{
				}

			/// <summary>
				/// The reference number of the document.
				/// This field is a part of the compound key of the document.
			/// </summary>
				[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "")]
				[PXDefault()]
				[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible, TabOrder = 1)]
				[PXSelector(typeof(Search<ARRegister.refNbr, Where<ARRegister.docType, Equal<Optional<ARRegister.docType>>>>), Filterable = true)]
				public virtual string RefNbr
			{
				get;
				set;
			}
			#endregion
				#region PaymentMethodID
				public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID> { }
				protected string _PaymentMethodID;
				[PXDBString(10, IsUnicode = true)]
				[PXSelector(typeof(PaymentMethod.paymentMethodID), DescriptionField = typeof(PaymentMethod.descr))]
				[PXUIField(DisplayName = "Payment Method", Enabled = false)]

				public virtual string PaymentMethodID
				{
					get
					{
						return this._PaymentMethodID;
					}
					set
					{
						this._PaymentMethodID = value;
					}
				}
				#endregion
				#region ExtRefNbr
				public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }
				protected string _ExtRefNbr;
				[PXDBString(40, IsUnicode = true)]
				[PXUIField(DisplayName = "Payment Ref.", Visibility = PXUIVisibility.SelectorVisible)]
				public virtual string ExtRefNbr
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
		}
		}

		private sealed class ARDisplayDocType : ARDocType
		{
			public const string CashReturnInvoice = "RCI";
			public const string CashSaleInvoice = "CSI";
			public new class ListAttribute : PXStringListAttribute
			{
				public ListAttribute()
					: base(
					new string[] { Invoice, DebitMemo, CreditMemo, Payment, VoidPayment, Prepayment, Refund, VoidRefund, FinCharge, SmallBalanceWO, SmallCreditWO, CashSale, CashReturn, CashSaleInvoice, CashReturnInvoice },
					new string[] { Messages.Invoice, Messages.DebitMemo, Messages.CreditMemo, Messages.Payment, Messages.VoidPayment, Messages.Prepayment, Messages.Refund, Messages.VoidRefund, Messages.FinCharge, Messages.SmallBalanceWO, Messages.SmallCreditWO, Messages.CashSale, Messages.CashReturn,Messages.CashSaleInvoice,Messages.CashReturnInvoice }) { }
			}
		}
		private sealed class decimalZero : PX.Data.BQL.BqlDecimal.Constant<decimalZero>
		{
			public decimalZero()
				: base(decimal.Zero)
			{
			}
		}

		#endregion

		#region Ctor
		public ARDocumentEnq()
		{
			ARSetup setup = this.ARSetup.Current;
			Company company = this.Company.Current;

			Documents.Cache.AllowDelete = false;
			Documents.Cache.AllowInsert = false;
			Documents.Cache.AllowUpdate = false;

			PXUIFieldAttribute.SetVisibility<ARRegister.finPeriodID>(Documents.Cache, null, PXUIVisibility.Visible);
			PXUIFieldAttribute.SetVisibility<ARRegister.customerID>(Documents.Cache, null, PXUIVisibility.Visible);
			PXUIFieldAttribute.SetVisibility<ARRegister.curyDiscBal>(Documents.Cache, null, PXUIVisibility.Visible);
			PXUIFieldAttribute.SetVisibility<ARRegister.curyOrigDocAmt>(Documents.Cache, null, PXUIVisibility.Visible);
			PXUIFieldAttribute.SetVisibility<ARRegister.curyDiscTaken>(Documents.Cache, null, PXUIVisibility.Visible);
			PXUIFieldAttribute.SetVisible<ARRegister.customerID>(Documents.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.parentChildAccount>());

			this.actionsfolder.MenuAutoOpen = true;
			this.actionsfolder.AddMenuAction(this.createInvoice);
			this.actionsfolder.AddMenuAction(this.createPayment);
			this.actionsfolder.AddMenuAction(this.payDocument);

			this.reportsfolder.MenuAutoOpen = true;
			this.reportsfolder.AddMenuAction(this.aRBalanceByCustomerReport);
			this.reportsfolder.AddMenuAction(this.customerHistoryReport);
			this.reportsfolder.AddMenuAction(this.aRAgedPastDueReport);
			this.reportsfolder.AddMenuAction(this.aRAgedOutstandingReport);
			this.reportsfolder.AddMenuAction(this.aRRegisterReport);

			CustomerRepository = new CustomerRepository(this);
		}
		public override bool IsDirty
		{
			get
			{
				return false;
			}
		}
		[PXHidden]
		public PXSelect<BAccount> dummy_view;
		#endregion

		#region Actions
		public PXAction<ARDocumentFilter> refresh;
		public PXCancel<ARDocumentFilter> Cancel;
		[Obsolete("Will be removed in Acumatica 2019R1")]
		public PXAction<ARDocumentFilter> viewDocument;
		public PXAction<ARDocumentFilter> viewOriginalDocument;
		public PXAction<ARDocumentFilter> previousPeriod;
		public PXAction<ARDocumentFilter> nextPeriod;

		public PXAction<ARDocumentFilter> actionsfolder;

		public PXAction<ARDocumentFilter> createInvoice;
		public PXAction<ARDocumentFilter> createPayment;
		public PXAction<ARDocumentFilter> payDocument;

		public PXAction<ARDocumentFilter> reportsfolder;
		public PXAction<ARDocumentFilter> aRBalanceByCustomerReport;
		public PXAction<ARDocumentFilter> customerHistoryReport;
		public PXAction<ARDocumentFilter> aRAgedPastDueReport;
		public PXAction<ARDocumentFilter> aRAgedOutstandingReport;
		public PXAction<ARDocumentFilter> aRRegisterReport;

		[InjectDependency]
		public IFinPeriodRepository FinPeriodRepository { get; set; }

		#endregion

		protected CustomerRepository CustomerRepository;

		#region Action Delegates
		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = true)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.Refresh, IsLockedOnToolbar = true)]
		public IEnumerable Refresh(PXAdapter adapter)
		{
			this.Filter.Current.RefreshTotals = true;
			return adapter.Get();
		}

		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		[PXEditDetailButton()]
		public virtual IEnumerable ViewDocument(PXAdapter adapter)
		{
			if (this.Documents.Current != null)
			{
				PXRedirectHelper.TryRedirect(Documents.Cache, Documents.Current, "Document", PXRedirectHelper.WindowMode.NewWindow);
			}
			return Filter.Select();
		}

		[PXUIField(Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable ViewOriginalDocument(PXAdapter adapter)
		{
			if (Documents.Current != null)
			{
				ARInvoiceEntry graph = PXGraph.CreateInstance<ARInvoiceEntry>();
				ARRegister origDoc = PXSelect<ARRegister,
					Where<ARRegister.refNbr, Equal<Required<ARRegister.origRefNbr>>,
						And<ARRegister.docType, Equal<Required<ARRegister.origDocType>>>>>
					.SelectSingleBound(graph, null, Documents.Current.OrigRefNbr, Documents.Current.OrigDocType);
				if (origDoc != null)
				{
					PXRedirectHelper.TryRedirect(graph.Document.Cache, origDoc, "Document", PXRedirectHelper.WindowMode.NewWindow);
				}
			}
			return adapter.Get();
		}

		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXPreviousButton]
		public virtual IEnumerable PreviousPeriod(PXAdapter adapter)
		{
			ARDocumentFilter filter = Filter.Current as ARDocumentFilter;

			int? calendarOrganizationID = FinPeriodRepository.GetCalendarOrganizationID(filter.OrganizationID, filter.BranchID, filter.UseMasterCalendar);

			FinPeriod prevPeriod = FinPeriodRepository.FindPrevPeriod(calendarOrganizationID, filter.Period, looped: true);

			filter.Period = prevPeriod?.FinPeriodID;
			filter.RefreshTotals = true;

			return adapter.Get();
		}

		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXNextButton]
		public virtual IEnumerable NextPeriod(PXAdapter adapter)
		{
			ARDocumentFilter filter = Filter.Current as ARDocumentFilter;

			int? calendarOrganizationID = FinPeriodRepository.GetCalendarOrganizationID(filter.OrganizationID, filter.BranchID, filter.UseMasterCalendar);

			FinPeriod nextPeriod = FinPeriodRepository.FindNextPeriod(calendarOrganizationID, filter.Period, looped: true);

			filter.Period = nextPeriod?.FinPeriodID;
			filter.RefreshTotals = true;
			return adapter.Get();
		}

		[PXUIField(DisplayName = "Actions", MapEnableRights = PXCacheRights.Select)]
		[PXButton(SpecialType = PXSpecialButtonType.ActionsFolder)]
		protected virtual IEnumerable Actionsfolder(PXAdapter adapter)
		{
			return adapter.Get();
		}

		[PXUIField(DisplayName = "Reports", MapEnableRights = PXCacheRights.Select)]
		[PXButton(SpecialType = PXSpecialButtonType.ReportsFolder)]
		protected virtual IEnumerable Reportsfolder(PXAdapter adapter)
		{
			return adapter.Get();
		}


		[PXUIField(DisplayName = Messages.NewInvoice, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(Category = ActionCategories.DocumentProcessing)]
		public virtual IEnumerable CreateInvoice(PXAdapter adapter)
		{
			if (this.Filter.Current != null)
			{
				if (this.Filter.Current.CustomerID != null)
				{
					CustomerMaint graph = PXGraph.CreateInstance<CustomerMaint>();
					graph.BAccount.Current = graph.BAccount.Search<BAccount.bAccountID>(this.Filter.Current.CustomerID);
					graph.newInvoiceMemo.Press();
				}
			}
			return Filter.Select();
		}

		[PXUIField(DisplayName = Messages.NewPayment, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(Category = ActionCategories.DocumentProcessing)]
		public virtual IEnumerable CreatePayment(PXAdapter adapter)
		{
			if (this.Filter.Current != null)
			{
				if (this.Filter.Current.CustomerID != null)
				{
					CustomerMaint graph = PXGraph.CreateInstance<CustomerMaint>();
					graph.BAccount.Current = graph.BAccount.Search<BAccount.bAccountID>(this.Filter.Current.CustomerID);
					graph.newPayment.Press();
				}
			}
			return Filter.Select();
		}


		[PXUIField(DisplayName = Messages.EnterPayment, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(Category = ActionCategories.DocumentProcessing)]
		public virtual IEnumerable PayDocument(PXAdapter adapter)
		{
			if (Documents.Current != null)
			{
				if (this.Documents.Current.Status != ARDocStatus.Open)
					throw new PXException(AP.Messages.Only_Open_Documents_MayBe_Processed);

				if (ARDocType.Payable(this.Documents.Current.DocType) == true)
				{
					ARInvoiceEntry graph = PXGraph.CreateInstance<ARInvoiceEntry>();
					ARInvoice inv = FindDoc<ARInvoice>(Documents.Current);
					if (inv != null)
					{
						graph.Document.Current = inv;
						graph.PayInvoice(adapter);
					}
				}
				else
				{
					ARPaymentEntry graph = PXGraph.CreateInstance<ARPaymentEntry>();
					ARPayment payment =
						graph.Document.Search<ARPayment.refNbr>(Documents.Current.RefNbr,Documents.Current.DocType);
					if (payment != null)
					{
						graph.Document.Current = payment;
						throw new PXRedirectRequiredException(graph, AP.Messages.ViewDocument);
					}

				}
			}
			return Filter.Select();
		}

		#endregion

		#region Report Actions Delegates

		[PXUIField(DisplayName = Messages.ARBalanceByCustomerReport, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable ARBalanceByCustomerReport(PXAdapter adapter)
		{
			ARDocumentFilter filter = Filter.Current;

			if (filter != null)
			{
				Customer customer = PXSelect<Customer, Where<Customer.bAccountID, Equal<Current<ARDocumentFilter.customerID>>>>.Select(this);
				Dictionary<string, string> parameters = new Dictionary<string, string>();
				if (!string.IsNullOrEmpty(filter.Period))
				{
					parameters["PeriodID"] = FinPeriodIDFormattingAttribute.FormatForDisplay(filter.Period);
				}
				parameters["CustomerID"] = customer.AcctCD;
				parameters["UseMasterCalendar"] = filter.UseMasterCalendar==true?true.ToString():false.ToString();
				throw new PXReportRequiredException(parameters, "AR632500", PXBaseRedirectException.WindowMode.NewWindow , Messages.ARBalanceByCustomerReport);
			}
			return adapter.Get();
		}


		[PXUIField(DisplayName = Messages.CustomerHistoryReport, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable CustomerHistoryReport(PXAdapter adapter)
		{
			ARDocumentFilter filter = Filter.Current;
			if (filter != null)
			{
				Customer customer = PXSelect<Customer, Where<Customer.bAccountID, Equal<Current<ARDocumentFilter.customerID>>>>.Select(this);
				Dictionary<string, string> parameters = new Dictionary<string, string>();
				if (!string.IsNullOrEmpty(filter.Period))
				{
					parameters["FromPeriodID"] = FinPeriodIDFormattingAttribute.FormatForDisplay(filter.Period);
					parameters["ToPeriodID"] = FinPeriodIDFormattingAttribute.FormatForDisplay(filter.Period);
				}
				parameters["CustomerID"] = customer.AcctCD;
				throw new PXReportRequiredException(parameters, "AR652000",PXBaseRedirectException.WindowMode.NewWindow, Messages.CustomerHistoryReport);
			}
			return adapter.Get();
		}


		[PXUIField(DisplayName = Messages.ARAgedPastDueReport, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable ARAgedPastDueReport(PXAdapter adapter)
		{
			ARDocumentFilter filter = Filter.Current;
			if (filter != null)
			{
				Customer customer = PXSelect<Customer, Where<Customer.bAccountID, Equal<Current<ARDocumentFilter.customerID>>>>.Select(this);
				Dictionary<string, string> parameters = new Dictionary<string, string>();
				parameters["CustomerID"] = customer.AcctCD;
				throw new PXReportRequiredException(parameters, "AR631000", PXBaseRedirectException.WindowMode.NewWindow, Messages.ARAgedPastDueReport);
			}
			return adapter.Get();
		}


		[PXUIField(DisplayName = Messages.ARAgedOutstandingReport, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable ARAgedOutstandingReport(PXAdapter adapter)
		{
			ARDocumentFilter filter = Filter.Current;
			if (filter != null)
			{
				Customer customer = PXSelect<Customer, Where<Customer.bAccountID, Equal<Current<ARDocumentFilter.customerID>>>>.Select(this);
				Dictionary<string, string> parameters = new Dictionary<string, string>();
				parameters["CustomerID"] = customer.AcctCD;
				throw new PXReportRequiredException(parameters, "AR631500", PXBaseRedirectException.WindowMode.NewWindow, Messages.ARAgedOutstandingReport);
			}
			return adapter.Get();
		}


		[PXUIField(DisplayName = Messages.ARRegisterReport, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable ARRegisterReport(PXAdapter adapter)
		{
			ARDocumentFilter filter = Filter.Current;
			if (filter != null)
			{
				Customer customer = PXSelect<Customer, Where<Customer.bAccountID, Equal<Current<ARDocumentFilter.customerID>>>>.Select(this);
				Dictionary<string, string> parameters = new Dictionary<string, string>();
				if (!string.IsNullOrEmpty(filter.Period))
				{
					parameters["StartPeriodID"] = FinPeriodIDFormattingAttribute.FormatForDisplay(filter.Period);
					parameters["EndPeriodID"] = FinPeriodIDFormattingAttribute.FormatForDisplay(filter.Period);
				}
				parameters["CustomerID"] = customer.AcctCD;
				throw new PXReportRequiredException(parameters, "AR621500", PXBaseRedirectException.WindowMode.NewWindow, Messages.ARRegisterReport);
			}
			return adapter.Get();
		}
		#endregion

		#region Selects
		public PXFilter<ARDocumentFilter> Filter;
		[PXFilterable]
		public PXSelectOrderBy<ARDocumentResult, OrderBy<Desc<ARDocumentResult.docDate>>> Documents;
		public PXSetup<ARSetup> ARSetup;
		public PXSetup<Company> Company;

		#endregion

		#region Select Delegates

		protected virtual IEnumerable documents()
		{
			ARDocumentFilter header = Filter.Current;
			if (header == null)
			{
				return new List<object>();
			}



			var result = SelectDetails();
			viewDocument.SetEnabled(result.Count > 0);
			//Filter.Cache.SetValueExt<ARDocumentFilter.filterDetails>(header, result);
			return result;
		}

		protected virtual IEnumerable filter()
		{
			PXCache cache = this.Caches[typeof(ARDocumentFilter)];
			if (cache != null)
			{
				ARDocumentFilter filter = cache.Current as ARDocumentFilter;

				if (filter != null)
				{
					if (filter.RefreshTotals == true)
					{
						filter.ClearSummary();
						foreach (ARDocumentResult it in SelectDetails(true))
						{
							Aggregate(filter, it);
						}

						filter.RefreshTotals = false;
					}

					if (filter.CustomerID != null)
					{
						ARCustomerBalanceEnq balanceBO = PXGraph.CreateInstance<ARCustomerBalanceEnq>();
						ARCustomerBalanceEnq.ARHistoryFilter histFilter = balanceBO.Filter.Current;
                        ARCustomerBalanceEnq.Copy(histFilter, filter);
                        if (histFilter.Period == null)
							histFilter.Period = balanceBO.GetLastActivityPeriod(filter.CustomerID, filter.IncludeChildAccounts == true);

						balanceBO.Filter.Update(histFilter);

						ARCustomerBalanceEnq.ARHistorySummary summary = balanceBO.Summary.Select();
						SetSummary(filter, summary);
					}
				}

				yield return cache.Current;
				cache.IsDirty = false;
			}
		}
		#endregion

		#region CacheAttached
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Original Document")]
		protected virtual void ARDocumentResult_OrigRefNbr_CacheAttached(PXCache sender) { }

		#endregion

		#region Events Handlers
		public virtual void ARDocumentFilter_ARAcctID_ExceptionHandling(PXCache cache, PXExceptionHandlingEventArgs e)
		{
			ARDocumentFilter header = e.Row as ARDocumentFilter;
			if (header != null)
			{
				e.Cancel = true;
				header.ARAcctID = null;
			}
		}

		public virtual void ARDocumentFilter_ARSubID_ExceptionHandling(PXCache cache, PXExceptionHandlingEventArgs e)
		{
			ARDocumentFilter header = e.Row as ARDocumentFilter;
			if (header != null)
			{
				e.Cancel = true;
				header.ARSubID = null;
			}
		}

		public virtual void ARDocumentFilter_CuryID_ExceptionHandling(PXCache cache, PXExceptionHandlingEventArgs e)
		{
			ARDocumentFilter header = e.Row as ARDocumentFilter;
			if (header != null)
			{
				e.Cancel = true;
				header.CuryID = null;
			}
		}

		public virtual void ARDocumentFilter_SubCD_FieldVerifying(PXCache cache, PXFieldVerifyingEventArgs e)
		{
			e.Cancel = true;
		}
		public virtual void ARDocumentFilter_RowUpdated(PXCache cache, PXRowUpdatedEventArgs e)
		{
			if (cache.ObjectsEqual<ARDocumentFilter.orgBAccountID>(e.Row, e.OldRow) &&
				cache.ObjectsEqual<ARDocumentFilter.organizationID>(e.Row, e.OldRow) &&
			    cache.ObjectsEqual<ARDocumentFilter.branchID>(e.Row, e.OldRow) &&
			    cache.ObjectsEqual<ARDocumentFilter.customerID>(e.Row, e.OldRow) &&
			    cache.ObjectsEqual<ARDocumentFilter.period>(e.Row, e.OldRow) &&
			    cache.ObjectsEqual<ARDocumentFilter.masterFinPeriodID>(e.Row, e.OldRow) &&
				cache.ObjectsEqual<ARDocumentFilter.useMasterCalendar>(e.Row, e.OldRow) &&
			    cache.ObjectsEqual<ARDocumentFilter.showAllDocs>(e.Row, e.OldRow) &&
			    cache.ObjectsEqual<ARDocumentFilter.includeUnreleased>(e.Row, e.OldRow) &&
			    cache.ObjectsEqual<ARDocumentFilter.aRAcctID>(e.Row, e.OldRow) &&
			    cache.ObjectsEqual<ARDocumentFilter.aRSubID>(e.Row, e.OldRow) &&
			    cache.ObjectsEqual<ARDocumentFilter.subCD>(e.Row, e.OldRow) &&
				cache.ObjectsEqual<ARDocumentFilter.subCDWildcard>(e.Row, e.OldRow) &&
			    cache.ObjectsEqual<ARDocumentFilter.docType>(e.Row, e.OldRow) &&
			    cache.ObjectsEqual<ARDocumentFilter.includeChildAccounts>(e.Row, e.OldRow) &&
				cache.ObjectsEqual<ARDocumentFilter.curyID>(e.Row, e.OldRow))
			{
				return;
			}

			(e.Row as ARDocumentFilter).RefreshTotals = true;
		}
		public virtual void ARDocumentFilter_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
		{
			ARDocumentFilter row = (ARDocumentFilter)e.Row;
			if (row == null) return;
			PXCache docCache = this.Documents.Cache;

			bool byPeriod = (row.Period != null);

			bool isMCFeatureInstalled = PXAccess.FeatureInstalled<FeaturesSet.multicurrency>();
			bool isForeignCurrencySelected = string.IsNullOrEmpty(row.CuryID) == false && (row.CuryID != this.Company.Current.BaseCuryID);
			bool isBaseCurrencySelected = string.IsNullOrEmpty(row.CuryID) == false && (row.CuryID == this.Company.Current.BaseCuryID);

			PXUIFieldAttribute.SetVisible<ARDocumentFilter.showAllDocs>(cache, row, !byPeriod);
			PXUIFieldAttribute.SetVisible<ARDocumentFilter.includeChildAccounts>(cache, row, PXAccess.FeatureInstalled<CS.FeaturesSet.parentChildAccount>());

			PXUIFieldAttribute.SetVisible<ARDocumentFilter.curyID>(cache, row, isMCFeatureInstalled);
			PXUIFieldAttribute.SetVisible<ARDocumentFilter.curyBalanceSummary>(cache, row, isMCFeatureInstalled && isForeignCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentFilter.curyDifference>(cache, row, isMCFeatureInstalled && isForeignCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentFilter.curyCustomerBalance>(cache, row, isMCFeatureInstalled && isForeignCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentFilter.curyCustomerRetainedBalance>(cache, row, isMCFeatureInstalled && isForeignCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentFilter.curyCustomerDepositsBalance>(cache, row, isMCFeatureInstalled && isForeignCurrencySelected);

			PXUIFieldAttribute.SetVisible<ARDocumentResult.curyID>(docCache, null, isMCFeatureInstalled);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.rGOLAmt>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.curyBegBalance>(docCache, null, byPeriod && isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.begBalance>(docCache, null, byPeriod);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.curyOrigDocAmt>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.curyDocBal>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.curyDiscActTaken>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.curyWOAmt>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected && byPeriod);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.curyRetainageTotal>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.curyOrigDocAmtWithRetainageTotal>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.curyRetainageUnreleasedAmt>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.woAmt>(docCache, null, byPeriod);

			Customer customer = null;

			if (row.CustomerID != null)
			{
				customer = CustomerRepository.FindByID(row.CustomerID);
			}

			createInvoice.SetEnabled(customer != null &&
				(customer.Status == CustomerStatus.Active
				|| customer.Status == CustomerStatus.OneTime));

			bool isPaymentAllowed = customer != null && customer.Status != CustomerStatus.Inactive;

			createPayment.SetEnabled(isPaymentAllowed);
			payDocument.SetEnabled(isPaymentAllowed);

			bool multipleBaseCurrenciesInstalled = PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
			PXUIFieldAttribute.SetRequired<ARDocumentFilter.orgBAccountID>(cache, multipleBaseCurrenciesInstalled);
			bool enableReports = multipleBaseCurrenciesInstalled ? row.CustomerID != null && row.OrgBAccountID != null : row.CustomerID != null;
			aRBalanceByCustomerReport.SetEnabled(enableReports);
			customerHistoryReport.SetEnabled(enableReports);
			aRAgedPastDueReport.SetEnabled(enableReports);
			aRAgedOutstandingReport.SetEnabled(enableReports);
			aRRegisterReport.SetEnabled(enableReports);

		}
		#endregion

		#region Utility Functions - internal

		protected virtual PXDelegateResult SelectDetails(bool summarize = false)
		{
			ARDocumentFilter header = this.Filter.Current;
			int?[] relevantCustomerIDs = null;
			Dictionary<Tuple<string,string>, decimal?> glTurn = null;

			if (Filter.Current?.CustomerID != null && Filter.Current?.IncludeChildAccounts == true)
			{
				relevantCustomerIDs = CustomerFamilyHelper
					.GetCustomerFamily<Override.BAccount.consolidatingBAccountID>(this, Filter.Current.CustomerID)
					.Where(customerInfo => customerInfo.BusinessAccount.BAccountID != null)
					.Select(customerInfo => customerInfo.BusinessAccount.BAccountID)
					.ToArray();
			}
			else if (Filter.Current?.CustomerID != null)
			{
				relevantCustomerIDs = new[] { Filter.Current.CustomerID };
			}

			PXSelectBase<ARDocumentResult> baseSel = new PXSelectReadonly<
				ARDocumentResult,
				Where<ARDocumentResult.customerID, In<Required<ARDocumentResult.customerID>>>,
				OrderBy<Asc<ARDocumentResult.docType, Asc<ARDocumentResult.refNbr>>>>
				(this);
			BqlCommand sel = baseSel.View.BqlSelect;

			if (header.OrgBAccountID != null)
			{
				sel = sel.WhereAnd<Where<ARDocumentResult.branchID, Inside<Current<ARDocumentFilter.orgBAccountID>>>>(); //MatchWithOrg
			}

			if (header.ARAcctID != null)
			{
				sel = sel.WhereAnd<Where<ARDocumentResult.aRAccountID, Equal<Current<ARDocumentFilter.aRAcctID>>>>();
			}

			if (header.ARSubID != null)
			{
				sel = sel.WhereAnd<Where<ARDocumentResult.aRSubID, Equal<Current<ARDocumentFilter.aRSubID>>>>();
			}

			if ((header.IncludeUnreleased ?? false) == false)
			{
				sel = sel.WhereAnd<Where<ARDocumentResult.released, Equal<True>>>();
			}
			else
			{
				sel = sel.WhereAnd<Where<ARDocumentResult.scheduled, Equal<False>,
					And<Where<ARDocumentResult.voided, Equal<False>,
							Or<ARDocumentResult.released, Equal<True>>>>>>();
			}

			if (!SubCDUtils.IsSubCDEmpty(header.SubCD))
			{
				sel = BqlCommand.AppendJoin<InnerJoin<Sub,On<Sub.subID, Equal<ARDocumentResult.aRSubID>>>>(sel);
				sel = sel.WhereAnd<Where<Sub.subCD, Like<Current<ARDocumentFilter.subCDWildcard>>>>();
			}

			if (header.DocType != null)
			{
				sel = sel.WhereAnd<Where<ARDocumentResult.docType, Equal<Current<ARDocumentFilter.docType>>>>();
			}

			if (header.CuryID != null)
			{
				sel = sel.WhereAnd<Where<ARDocumentResult.curyID, Equal<Current<ARDocumentFilter.curyID>>>>();
			}

			bool byPeriod = (header.Period != null);
			var branchIDs = PXAccess.GetChildBranchIDs(header.OrganizationID, false);
			if (header.BranchID != null)
			{
				sel = sel.WhereAnd<Where<ARDocumentResult.branchID, Equal<Current<ARDocumentFilter.branchID>>>>();
			}
			else if (header.OrganizationID != null)
			{
				sel = sel.WhereAnd<Where<ARDocumentResult.branchID, In<Required<ARDocumentResult.branchID>>,
					And<MatchWithBranch<ARDocumentResult.branchID>>>>();
			}

			var restrictedFields = summarize
				? new List<Type>
				{
					typeof(ARDocumentResult.released),
					typeof(ARDocumentResult.curyOrigDocAmt),
					typeof(ARDocumentResult.origDocAmt),
					typeof(ARDocumentResult.curyDocBal),
					typeof(ARDocumentResult.docBal),
					typeof(ARDocumentResult.curyRetainageUnreleasedAmt),
					typeof(ARDocumentResult.retainageUnreleasedAmt)
				}
				: new List<Type>() {typeof(ARDocumentResult)};
			Type queryType = typeof(ARDocumentResult);

			if(byPeriod)
			{
				if (header.UseMasterCalendar == true)
				{
					sel = sel.WhereAnd<Where<ARDocumentResult.tranPeriodID, LessEqual<Current<ARDocumentFilter.period>>>>();
					sel = sel.WhereAnd<Where<ARDocumentResult.tranPostPeriodID, IsNull,
						Or<ARDocumentResult.tranPostPeriodID, LessEqual<Current<ARDocumentFilter.period>>>>>();
				}
				else
				{
					sel = sel.WhereAnd<Where<ARDocumentResult.finPeriodID, LessEqual<Current<ARDocumentFilter.period>>>>();
					sel = sel.WhereAnd<Where<ARDocumentResult.finPostPeriodID, IsNull,
						Or<ARDocumentResult.finPostPeriodID, LessEqual<Current<ARDocumentFilter.period>>>>>();
				}

				queryType = typeof(ARDocumentPeriodResult);

				var aggregate = summarize
					?typeof(Aggregate<
						GroupBy<ARDocumentPeriodResult.released,
						Sum<ARDocumentPeriodResult.curyDocBal,
						Sum<ARDocumentPeriodResult.docBal,
						Sum<ARDocumentPeriodResult.curyRetainageUnreleasedAmt,
						Sum<ARDocumentPeriodResult.retainageUnreleasedAmt>>>>>>)
					:typeof(Aggregate<
						GroupBy<ARDocumentPeriodResult.docType,
						GroupBy<ARDocumentPeriodResult.refNbr,
						Sum<ARDocumentPeriodResult.curyBegBalance,
						Sum<ARDocumentPeriodResult.begBalance,
						Sum<ARDocumentPeriodResult.curyDocBal,
						Sum<ARDocumentPeriodResult.docBal,
						Sum<ARDocumentPeriodResult.curyRetainageUnreleasedAmt,
						Sum<ARDocumentPeriodResult.retainageUnreleasedAmt,
						Sum<ARDocumentPeriodResult.curyDiscActTaken,
						Sum<ARDocumentPeriodResult.discActTaken,
						Sum<ARDocumentPeriodResult.curyWOAmt,
						Sum<ARDocumentPeriodResult.woAmt,
						Sum<ARDocumentPeriodResult.rGOLAmt,
						Sum<ARDocumentPeriodResult.aRTurnover>>>>>>>>>>>>>>,
						Having<ARDocumentPeriodResult.begBalance.Summarized.IsNotEqual<Zero>
							.Or<ARDocumentPeriodResult.docBal.Summarized.IsNotEqual<Zero>>
							.Or<ARDocumentPeriodResult.retainageUnreleasedAmt.Summarized.IsNotEqual<Zero>>
							.Or<ARDocumentPeriodResult.turn.Summarized.IsNotEqual<Zero>
							.Or<ARDocumentPeriodResult.retainageUnreleasedAmt.Summarized.IsNotEqual<Zero>>
							.Or<ARDocumentPeriodResult.released.Maximized.IsEqual<False>>>>
					>);


				if (header.IncludeGLTurnover == true && !summarize)
				{
					glTurn = SelectGLTurn();
					queryType = typeof(GLDocumentPeriodResult);
					aggregate = typeof(Aggregate<
						GroupBy<GLDocumentPeriodResult.docType,
						GroupBy<GLDocumentPeriodResult.refNbr,
						Sum<GLDocumentPeriodResult.aRTurnover>>>>);
				}

				var types = new List<Type>(BqlCommand.Decompose(sel.GetSelectType()));
				for(int i=0; i<types.Count; i++)
				{
					Type t = types[i];
					if (t == typeof(ARDocumentResult))
						types[i] = queryType;
					else if (t.DeclaringType == typeof(ARDocumentResult))
					{
						Type sub = queryType.GetNestedType(t.Name);
						types[i] = sub;
					}
				}

				restrictedFields = new List<Type> {queryType};
				types[0] = types[0].GetGenericArguments().Count() == 3
					? typeof(Select4<,,,>)
					: typeof(Select5<,,,,>);
				types.Insert(types.Count - 5, aggregate);
				if (summarize)
				{
					types[0] = typeof(Select4<,,>);
					types.RemoveRange(types.Count - 5, 5);
				}

				sel = BqlCommand.CreateInstance(BqlCommand.Compose(types.ToArray()));
			}
			else
			{
				if (header.ShowAllDocs == false)
				{
					sel = sel.WhereAnd<Where<ARDocumentResult.openDoc, Equal<True>>>();
				}

				if (summarize)
				{
					var types = new List<Type>(BqlCommand.Decompose(sel.GetSelectType()));
					types[0] = typeof(Select4<,,>);
					var aggregate = typeof(Aggregate<
						GroupBy<ARDocumentResult.released,
						Sum<ARDocumentResult.curyOrigDocAmt, Sum<ARDocumentResult.origDocAmt,
						Sum<ARDocumentResult.curyDocBal, Sum<ARDocumentResult.docBal,
						Sum<ARDocumentResult.curyRetainageUnreleasedAmt, Sum<ARDocumentResult.retainageUnreleasedAmt
						>>>>>>>>);
					types.Insert(types.Count - 5, aggregate);
					types.RemoveRange(types.Count - 5, 5);
					sel = BqlCommand.CreateInstance(BqlCommand.Compose(types.ToArray()));
				}

			}
			var mapper = new PXResultMapper(this,
				new Dictionary<Type, Type> {[typeof(ARDocumentResult)] = queryType},
				typeof(ARDocumentResult));
			if(byPeriod)
				mapper.ExtFilters.Add(
					new Type[]{
					typeof(ARDocumentResult.curyBegBalance),
					typeof(ARDocumentResult.begBalance),
					typeof(ARDocumentResult.curyDocBal),
					typeof(ARDocumentResult.docBal),
					typeof(ARDocumentResult.curyRetainageUnreleasedAmt),
					typeof(ARDocumentResult.retainageUnreleasedAmt),
					typeof(ARDocumentResult.curyDiscActTaken),
					typeof(ARDocumentResult.discActTaken),
					typeof(ARDocumentResult.curyWOAmt),
					typeof(ARDocumentResult.woAmt),
					typeof(ARDocumentResult.rGOLAmt),
					});

			int startRow = PXView.StartRow;
			int totalRows = 0;
			PXDelegateResult list = mapper.CreateDelegateResult(!(summarize || glTurn != null));

			PXView documentView = new PXView(this, true, sel);
			using(new PXFieldScope(documentView, restrictedFields))
				foreach (object row in summarize || glTurn != null
						? documentView.SelectMulti(relevantCustomerIDs, branchIDs)
						: documentView.Select(null, new object[]{relevantCustomerIDs, branchIDs},
							PXView.Searches, mapper.SortColumns, PXView.Descendings, mapper.Filters,
							ref startRow, PXView.MaximumRows, ref totalRows))
				{
					object rec = row is PXResult result
						? result[0]
						: row;

					if (rec is ARDocumentResult)
					{
						list.Add(rec);
					}
					else
					{
						ARDocumentResult item = (ARDocumentResult)Documents.Cache.CreateInstance();
						foreach (string field in Documents.Cache.Fields)
							Documents.Cache.SetValue(item, field, documentView.Cache.GetValue(rec, field));
						item.GLTurnover = 0m;
						if (glTurn != null && glTurn.Count != 0)
						{
							if (glTurn != null &&
							    glTurn.TryGetValue(new Tuple<string, string>(item.DocType, item.RefNbr), out var turn))
								item.GLTurnover = turn ?? 0m;
						}
						list.Add(item);
					}
				}

			return list;
		}

		protected virtual Dictionary<Tuple<string,string>, decimal?> SelectGLTurn()
		{
			BqlCommand selAccounts = new PXSelectGroupBy<ARTranPost,
					Where<ARTranPost.accountID, IsNotNull>,
					Aggregate<GroupBy<ARTranPost.accountID>>>
				(this).View.BqlSelect;

			BqlCommand selectGLTurn = new PXSelect<GLTran,
					Where<GLTran.module.IsIn<BatchModule.moduleGL, BatchModule.moduleAR>.
						And<GLTran.branchID.IsEqual<ARDocumentFilter.branchID.FromCurrent>>.
						And<GLTran.referenceID.IsEqual<ARDocumentFilter.customerID.FromCurrent>>.
						And<GLTran.posted.IsEqual<True>>>>
				(this).View.BqlSelect;

			if (this.Filter.Current.UseMasterCalendar == true)
			{
				selAccounts = selAccounts.WhereAnd<Where<ARTranPost.tranPeriodID, Equal<Current<ARDocumentFilter.period>>>>();
				selectGLTurn = selectGLTurn.WhereAnd<Where<GLTran.tranPeriodID, Equal<Current<ARDocumentFilter.period>>>>();
			}
			else
			{
				selAccounts = selAccounts.WhereAnd<Where<ARTranPost.finPeriodID, Equal<Current<ARDocumentFilter.period>>>>();
				selectGLTurn = selectGLTurn.WhereAnd<Where<GLTran.finPeriodID, Equal<Current<ARDocumentFilter.period>>>>();
			}

			if (this.Filter.Current.ARAcctID == null)
			{
				selectGLTurn = selectGLTurn.WhereAnd<Where<GLTran.accountID.
					IsInSubselect<SelectFrom<ARTranPost>
						.Where<ARTranPost.tranType.IsEqual<GLTran.tranType>
						.And<ARTranPost.tranRefNbr.IsEqual<GLTran.refNbr>>>
					.SearchFor<ARTranPost.accountID>>>>();
			}
			else
			{
				selectGLTurn = selectGLTurn.WhereAnd<Where<GLTran.accountID, Equal<Current<ARDocumentFilter.aRAcctID>>>>();
			}


			selectGLTurn = selectGLTurn.AggregateNew<
				Aggregate<
				GroupBy<GLTran.tranType,
				GroupBy<GLTran.refNbr,
				Sum<GLTran.gLTurnover
				>>>>>();

			var glTurnFields = new List<Type>()
			{
				typeof(GLTran.tranType),
				typeof(GLTran.refNbr),
				typeof(GLTran.gLTurnover)
			};
			PXView glTurnView = new PXView(this, true, selectGLTurn);
			using (new PXFieldScope(glTurnView, glTurnFields))
				return glTurnView.SelectMulti()
					.AsEnumerable()
					.RowCast<GLTran>()
					.ToDictionary(
						t=> new Tuple<string,string>(t.TranType, t.RefNbr),
						t=>t.GLTurnover);
		}
		protected virtual void SetSummary(ARDocumentFilter aDest, ARCustomerBalanceEnq.ARHistorySummary aSrc)
		{
			aDest.CustomerBalance = aSrc.BalanceSummary;
			aDest.CustomerDepositsBalance = aSrc.DepositsSummary;
			aDest.CuryCustomerBalance = aSrc.CuryBalanceSummary;
			aDest.CuryCustomerDepositsBalance = aSrc.CuryDepositsSummary;
		}

		protected virtual void Aggregate(ARDocumentFilter aDest, ARDocumentResult aSrc)
		{
			aDest.BalanceSummary += aSrc.DocBal ?? decimal.Zero;
			aDest.CuryBalanceSummary += aSrc.CuryDocBal ?? decimal.Zero;
			aDest.CustomerRetainedBalance += aSrc.RetainageUnreleasedAmt ?? decimal.Zero;
			aDest.CuryCustomerRetainedBalance += aSrc.CuryRetainageUnreleasedAmt ?? decimal.Zero;
		}

		protected TDoc FindDoc<TDoc>(ARDocumentResult aRes)
			where TDoc : AR.ARRegister, new()
		{
			return FindDoc<TDoc>(this, aRes.DocType, aRes.RefNbr);
		}
		#endregion

		#region Utility Functions - public
		public static TDoc FindDoc<TDoc>(PXGraph aGraph, string aDocType, string apRefNbr)
			where TDoc : AR.ARRegister, new()
		{
			return PXSelect<TDoc,
				Where<AR.ARRegister.docType, Equal<Required<AR.ARRegister.docType>>,
					And<AR.ARRegister.refNbr, Equal<Required<AR.ARRegister.refNbr>>>>>
				.Select(aGraph, aDocType, apRefNbr);
		}
		#endregion
	}
}

