using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.GL;
using PX.Objects.GL.Attributes;
using PX.Objects.CS;
using PX.Objects.CM;

using PX.Objects.GL.FinPeriods;
using PX.Objects.GL.FinPeriods.TableDefinition;
using PX.Objects.Common.Attributes;
using PX.Objects.CR;
using PX.Data.BQL;
using PX.Objects.AP.Standalone;
using PX.Objects.Common.MigrationMode;
using PX.Objects.Common.Utility;

namespace PX.Objects.AP
{
	[TableAndChartDashboardType]
	public class APDocumentEnq : PXGraph<APDocumentEnq>
	{
		#region Internal Types
		[Serializable]
		public partial class APDocumentFilter : IBqlTable
		{
			#region OrganizationID
			public abstract class organizationID : PX.Data.BQL.BqlInt.Field<organizationID> { }
			[Organization(false, Required = false)]
			public int? OrganizationID { get; set; }
			#endregion
			#region BranchID
			public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }

			[BranchOfOrganization(typeof(APDocumentFilter.organizationID), false)]
			public int? BranchID { get; set; }
			#endregion

			#region OrgBAccountID
			public abstract class orgBAccountID : IBqlField { }

			[OrganizationTree(typeof(organizationID), typeof(branchID), onlyActive: false)]
			public int? OrgBAccountID { get; set; }
			#endregion
			#region VendorID
			public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
			[Vendor(DescriptionField = typeof(Vendor.acctName))]
			[PXDefault()]
			public virtual int? VendorID
			{
				get;
				set;
			}
			#endregion
			#region AccountID
			public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }
			[GL.Account(null, typeof(Search5<Account.accountID,
					InnerJoin<APHistory, On<Account.accountID, Equal<APHistory.accountID>>>,
					Where<Match<Current<AccessInfo.userName>>>,
					Aggregate<GroupBy<Account.accountID>>>),
			   DisplayName = "AP Account", DescriptionField = typeof(GL.Account.description))]
			public virtual int? AccountID
			{
				get;
				set;
			}
			#endregion
			#region SubCD
			public abstract class subCD : PX.Data.BQL.BqlString.Field<subCD> { }

			[PXDBString(30, IsUnicode = true)]
			[PXUIField(DisplayName = "AP Subaccount", Visibility = PXUIVisibility.Invisible, FieldClass = SubAccountAttribute.DimensionName)]
			[PXDimension("SUBACCOUNT", ValidComboRequired = false)]
			public virtual string SubCD { get; set; }

			#endregion
			#region UseMasterCalendar
			public abstract class useMasterCalendar : PX.Data.BQL.BqlBool.Field<useMasterCalendar> { }

			[PXBool]
			[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = Common.Messages.UseMasterCalendar)]
			[PXUIVisible(typeof(FeatureInstalled<FeaturesSet.multipleCalendarsSupport>))]
			public bool? UseMasterCalendar { get; set; }
			#endregion
			#region FinPeriodID
			public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }

			[AnyPeriodFilterable(null, null,
				branchSourceType: typeof(APDocumentFilter.branchID),
				organizationSourceType: typeof(APDocumentFilter.organizationID),
				useMasterCalendarSourceType: typeof(APDocumentFilter.useMasterCalendar),
				redefaultOrRevalidateOnOrganizationSourceUpdated: false)]
			[PXUIField(DisplayName = "Period", Visibility = PXUIVisibility.Visible, Required = false)]
			public virtual string FinPeriodID { get; set; }

			#endregion
			#region MasterFinPeriodID
			public abstract class masterFinPeriodID : PX.Data.BQL.BqlString.Field<masterFinPeriodID> { }
			[Obsolete("This is an absolete field. It will be removed in 2019R2")]
			[PeriodID]
			public virtual string MasterFinPeriodID { get; set; }
			#endregion
			#region DocType
			public abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }

            [PXDBString(3, IsFixed = true)]
			[PXDefault()]
			[APDocType.List()]
			[PXUIField(DisplayName = "Type")]
			public virtual string DocType { get; set; }

			#endregion
			#region ShowAllDocs
			public abstract class showAllDocs : PX.Data.BQL.BqlBool.Field<showAllDocs> { }

			[PXDBBool()]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Show All Documents")]
			public virtual bool? ShowAllDocs { get; set; }

			#endregion
			#region IncludeUnreleased
			public abstract class includeUnreleased : PX.Data.BQL.BqlBool.Field<includeUnreleased> { }

			[PXDBBool()]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Include Unreleased Documents")]
			public virtual bool? IncludeUnreleased { get; set; }

			#endregion
			#region CuryID
			public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }

			[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL")]
			[PXSelector(typeof(CM.Currency.curyID), CacheGlobal = true)]
			[PXUIField(DisplayName = "Currency")]
			public virtual string CuryID { get; set; }

			#endregion
			#region SubCD Wildcard
			public abstract class subCDWildcard : PX.Data.BQL.BqlString.Field<subCDWildcard> { };
			[PXDBString(30, IsUnicode = true)]
			public virtual string SubCDWildcard => SubCDUtils.CreateSubCDWildcard(this.SubCD, SubAccountAttribute.DimensionName);

			#endregion
			#region RefreshTotals
			public abstract class refreshTotals : PX.Data.BQL.BqlBool.Field<refreshTotals> { }
			[PXDBBool]
			[PXDefault(true)]
			public bool? RefreshTotals { get; set; }
			#endregion
			#region CuryBalanceSummary
			public abstract class curyBalanceSummary : PX.Data.BQL.BqlDecimal.Field<curyBalanceSummary> { }
			[PXCury(typeof(APDocumentFilter.curyID))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance by Documents (Currency)", Enabled = false)]
			public virtual decimal? CuryBalanceSummary { get; set; }

			#endregion
			#region BalanceSummary
			public abstract class balanceSummary : PX.Data.BQL.BqlDecimal.Field<balanceSummary> { }

			[PXDBBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance by Documents", Enabled = false)]
			public virtual decimal? BalanceSummary { get; set; }

			#endregion
			#region CuryVendorBalance
			public abstract class curyVendorBalance : PX.Data.BQL.BqlDecimal.Field<curyVendorBalance> { }

			[PXCury(typeof(APDocumentFilter.curyID))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Current Balance (Currency)", Enabled = false)]
			public virtual decimal? CuryVendorBalance { get; set; }

			#endregion
			#region VendorBalance
			public abstract class vendorBalance : PX.Data.BQL.BqlDecimal.Field<vendorBalance> { }

			[PXDBBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Current Balance", Enabled = false)]
			public virtual decimal? VendorBalance { get; set; }

			#endregion

			#region CuryVendorRetainedBalance
			public abstract class curyVendorRetainedBalance : PX.Data.BQL.BqlDecimal.Field<curyVendorRetainedBalance> { }
			[PXCury(typeof(APDocumentFilter.curyID))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Retained Balance (Currency)", Enabled = false, FieldClass = nameof(FeaturesSet.Retainage))]
			public virtual decimal? CuryVendorRetainedBalance { get; set; }
			#endregion
			#region VendorRetainedBalance
			public abstract class vendorRetainedBalance : PX.Data.BQL.BqlDecimal.Field<vendorRetainedBalance> { }

			[PXDBBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Retained Balance", Enabled = false, FieldClass = nameof(FeaturesSet.Retainage))]
			public virtual decimal? VendorRetainedBalance { get; set; }
			#endregion

			#region CuryVendorDepositsBalance

			public abstract class curyVendorDepositsBalance : PX.Data.BQL.BqlDecimal.Field<curyVendorDepositsBalance> { }

			[PXCury(typeof(APDocumentFilter.curyID))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Prepayments Balance (Currency)", Enabled = false)]
			public virtual decimal? CuryVendorDepositsBalance { get; set; }

			#endregion
			#region VendorDepositsBalance

			public abstract class vendorDepositsBalance : PX.Data.BQL.BqlDecimal.Field<vendorDepositsBalance> { }

			[PXDBBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Prepayment Balance", Enabled = false)]
			public virtual decimal? VendorDepositsBalance { get; set; }

			#endregion

			#region CuryDifference
			public abstract class curyDifference : PX.Data.BQL.BqlDecimal.Field<curyDifference> { }

			[PXCury(typeof(APDocumentFilter.curyID))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance Discrepancy (Currency)", Enabled = false)]
			public virtual decimal? CuryDifference => (this.CuryBalanceSummary - (this.CuryVendorBalance + this.CuryVendorDepositsBalance));

			#endregion
			#region Difference
			public abstract class difference : PX.Data.BQL.BqlDecimal.Field<difference> { }

			[PXDBBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance Discrepancy", Enabled = false)]
			public virtual decimal? Difference => (this.BalanceSummary - (this.VendorBalance + this.VendorDepositsBalance));

			#endregion
			#region IncludeGLTurnover
			public abstract class includeGLTurnover : PX.Data.BQL.BqlBool.Field<includeGLTurnover> { }

			[PXDBBool()]
			[PXDefault(false)]
			public virtual bool? IncludeGLTurnover { get; set; }

			#endregion
			public virtual void ClearSummary()
			{
				VendorBalance = decimal.Zero;
				VendorDepositsBalance = decimal.Zero;
				BalanceSummary = decimal.Zero;
				CuryVendorBalance = decimal.Zero;
				CuryVendorDepositsBalance = decimal.Zero;
				CuryBalanceSummary = decimal.Zero;
				CuryVendorRetainedBalance = decimal.Zero;
				VendorRetainedBalance = decimal.Zero;
			}
		}

		[Serializable()]
		[PXPrimaryGraph(typeof(APDocumentEnq), Filter = typeof(APDocumentFilter))]
		[PXCacheName(Messages.APHistoryForReport)]
		public partial class APHistoryForReport : APHistory { }

		[PXHidden]
		public class APRegister : IBqlTable
		{
			#region DocType

			public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
			{
			}

			[PXDBString(3, IsKey = true, IsFixed = true)]
			[PXDefault()]
			[APDocType.List()]
			[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = true, TabOrder = 0)]
			public virtual string DocType { get; set; }

			#endregion

			#region RefNbr

			public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
			{
			}

			[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "")]
			[PXDefault()]
			[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible, TabOrder = 1)]
			[PXSelector(typeof(Search<APRegister.refNbr>), Filterable = true)]
			public virtual string RefNbr { get; set; }

			#endregion

			#region CuryID

			public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID>
			{
			}

			/// <summary>
			/// Code of the <see cref="PX.Objects.CM.Currency">Currency</see> of the document.
			/// </summary>
			/// <value>
			/// Defaults to the <see cref="Company.BaseCuryID">company's base currency</see>.
			/// </value>
			[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL")]
			[PXUIField(DisplayName = "Currency", Visibility = PXUIVisibility.SelectorVisible)]
			[PXDefault(typeof(Current<AccessInfo.baseCuryID>))]
			[PXSelector(typeof(Currency.curyID))]
			public virtual string CuryID { get; set; }

			#endregion

			#region BranchID

			public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID>
			{
			}

			/// <summary>
			/// Identifier of the <see cref="PX.Objects.GL.Branch">Branch</see>, to which the document belongs.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="PX.Objects.GL.Branch.BranchID">Branch.BranchID</see> field.
			/// </value>
			[GL.Branch()]
			public virtual int? BranchID { get; set; }

			#endregion

			#region DocDate

			public abstract class docDate : PX.Data.BQL.BqlDateTime.Field<docDate>
			{
			}

			/// <summary>
			/// Date of the document.
			/// </summary>
			[PXDBDate()]
			[PXDefault(typeof(AccessInfo.businessDate))]
			[PXUIField(DisplayName = "Date", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual DateTime? DocDate { get; set; }

			#endregion

			#region TranPeriodID

			public abstract class tranPeriodID : PX.Data.BQL.BqlString.Field<tranPeriodID>
			{
			}

			/// <summary>
			/// <see cref="FinPeriod">Financial Period</see> of the document.
			/// </summary>
			/// <value>
			/// Determined by the <see cref="APRegister.DocDate">date of the document</see>. Unlike <see cref="APRegister.FinPeriodID"/>
			/// the value of this field can't be overriden by user.
			/// </value>
			[PeriodID]
			[PXUIField(DisplayName = "Master Period")]
			public virtual string TranPeriodID { get; set; }

			#endregion

			#region FinPeriodID

			public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID>
			{
			}

			/// <summary>
			/// <see cref="FinPeriod">Financial Period</see> of the document.
			/// </summary>
			/// <value>
			/// Defaults to the period, to which the <see cref="APRegister.DocDate"/> belongs, but can be overriden by user.
			/// </value>
			[APOpenPeriod(
				typeof(APRegister.docDate),
				branchSourceType: typeof(APRegister.branchID),
				masterFinPeriodIDType: typeof(APRegister.tranPeriodID),
				IsHeader = true)]
			[PXUIField(DisplayName = "Post Period", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual string FinPeriodID { get; set; }

			#endregion

			#region VendorID

			public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID>
			{
			}

			/// <summary>
			/// Identifier of the <see cref="Vendor"/>, whom the document belongs to.
			/// </summary>
			[VendorActive(
				Visibility = PXUIVisibility.SelectorVisible,
				DescriptionField = typeof(Vendor.acctName),
				CacheGlobal = true,
				Filterable = true)]
			[PXDefault]
			public virtual int? VendorID { get; set; }

			#endregion

			#region APAccountID

			public abstract class aPAccountID : PX.Data.BQL.BqlInt.Field<aPAccountID>
			{
			}

			/// <summary>
			/// Identifier of the AP account, to which the document belongs.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Account.AccountID"/> field.
			/// </value>
			[PXDefault]
			[Account(typeof(APRegister.branchID), typeof(Search<Account.accountID,
					Where2<Match<Current<AccessInfo.userName>>,
						And<Account.active, Equal<True>,
							And<Where<Current<GLSetup.ytdNetIncAccountID>, IsNull,
								Or<Account.accountID, NotEqual<Current<GLSetup.ytdNetIncAccountID>>>>>>>>),
				DisplayName = "AP Account",
				ControlAccountForModule = ControlAccountModule.AP)]
			public virtual int? APAccountID { get; set; }

			#endregion

			#region APSubID

			public abstract class aPSubID : PX.Data.BQL.BqlInt.Field<aPSubID>
			{
			}

			/// <summary>
			/// Identifier of the AP subaccount, to which the document belongs.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Sub.SubID"/> field.
			/// </value>
			[PXDefault]
			[SubAccount(typeof(APRegister.aPAccountID), typeof(APRegister.branchID), true,
				DescriptionField = typeof(Sub.description), DisplayName = "AP Subaccount",
				Visibility = PXUIVisibility.Visible)]
			public virtual int? APSubID { get; set; }

			#endregion

			#region CuryInfoID

			public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID>
			{
			}

			/// <summary>
			/// Identifier of the <see cref="PX.Objects.CM.CurrencyInfo">CurrencyInfo</see> object associated with the document.
			/// </summary>
			/// <value>
			/// Generated automatically. Corresponds to the <see cref="PX.Objects.CM.CurrencyInfo.CurrencyInfoID"/> field.
			/// </value>
			[PXDBLong()]
			[CurrencyInfo(ModuleCode = BatchModule.AP)]
			public virtual long? CuryInfoID { get; set; }

			#endregion

			#region Released

			public abstract class released : PX.Data.BQL.BqlBool.Field<released>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document was released.
			/// </summary>
			[PXDBBool()]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Released", Visible = false)]
			public virtual bool? Released { get; set; }

			#endregion

			#region ReleasedToVerify

			/// <exclude/>
			public abstract class releasedToVerify : PX.Data.BQL.BqlBool.Field<releasedToVerify>
			{
			}

			/// <summary>
			/// When set, on persist checks, that the document has the corresponded <see cref="Released"/> original value.
			/// When not set, on persist checks, that <see cref="Released"/> value is not changed.
			/// Throws an error otherwise.
			/// </summary>
			[PX.Objects.Common.Attributes.PXDBRestrictionBool(typeof(released))]
			public virtual bool? ReleasedToVerify { get; set; }

			#endregion

			#region OpenDoc

			public abstract class openDoc : PX.Data.BQL.BqlBool.Field<openDoc>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document is open.
			/// </summary>
			[PXDBBool()]
			[PXDefault(true)]
			[PXUIField(DisplayName = "Open", Visible = false)]
			public virtual bool? OpenDoc { get; set; }

			#endregion

			#region Hold

			public abstract class hold : PX.Data.BQL.BqlBool.Field<hold>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document is on hold and thus cannot be released.
			/// </summary>
			[PXDBBool()]
			[PXUIField(DisplayName = "Hold", Visibility = PXUIVisibility.Visible)]
			[PXDefault(true, typeof(APSetup.holdEntry))]
			public virtual bool? Hold { get; set; }

			#endregion

			#region Scheduled

			public abstract class scheduled : PX.Data.BQL.BqlBool.Field<scheduled>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document is part of a <c>Schedule</c> and serves as a template for generating other documents according to it.
			/// </summary>
			[PXDBBool()]
			[PXDefault(false)]
			public virtual bool? Scheduled { get; set; }

			#endregion

			#region Voided

			public abstract class voided : PX.Data.BQL.BqlBool.Field<voided>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document was voided. In this case <see cref="VoidBatchNbr"/> field will hold the number of the voiding <see cref="Batch"/>.
			/// </summary>
			[PXDBBool()]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Void", Visible = false)]
			public virtual bool? Voided { get; set; }

			#endregion

			#region Printed

			public abstract class printed : PX.Data.BQL.BqlBool.Field<printed>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document was printed.
			/// </summary>
			[PXDBBool()]
			[PXDefault(false)]
			public virtual bool? Printed { get; set; }

			#endregion

			#region Prebooked

			public abstract class prebooked : PX.Data.BQL.BqlBool.Field<prebooked>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document was prebooked.
			/// </summary>
			[PXDBBool()]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Prebooked")]
			public virtual bool? Prebooked { get; set; }

			#endregion

			#region Approved

			public abstract class approved : PX.Data.BQL.BqlBool.Field<approved>
			{
			}

			[PXDBBool]
			[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual bool? Approved { get; set; }

			#endregion

			#region Rejected

			public abstract class rejected : PX.Data.BQL.BqlBool.Field<rejected>
			{
			}

			[PXDBBool]
			[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
			public bool? Rejected { get; set; }

			#endregion

			#region NoteID

			public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID>
			{
			}

			/// <summary>
			/// Identifier of the <see cref="PX.Data.Note">Note</see> object, associated with the document.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="PX.Data.Note.NoteID">Note.NoteID</see> field.
			/// </value>
			[PXNote(DescriptionField = typeof(APRegister.refNbr))]
			public virtual Guid? NoteID { get; set; }

			#endregion

			#region RefNoteID

			public abstract class refNoteID : PX.Data.BQL.BqlGuid.Field<refNoteID>
			{
			}

			/// <summary>
			/// !REV!
			/// </summary>
			[PXDBGuid()]
			public virtual Guid? RefNoteID { get; set; }

			#endregion

			#region ClosedDate

			public abstract class closedDate : PX.Data.BQL.BqlDateTime.Field<closedDate>
			{
			}

			/// <summary>
			/// The date of the last application.
			/// </summary>
			[PXDBDate]
			[PXUIField(DisplayName = "Closed Date", Visibility = PXUIVisibility.Invisible)]
			public virtual DateTime? ClosedDate { get; set; }

			#endregion

			#region ClosedFinPeriodID

			public abstract class closedFinPeriodID : PX.Data.BQL.BqlString.Field<closedFinPeriodID>
			{
			}

			/// <summary>
			/// The <see cref="PX.Objects.GL.FinancialPeriod">Financial Period</see>, in which the document was closed.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="FinPeriodID"/> field.
			/// </value>
			[FinPeriodID(
				branchSourceType: typeof(APRegister.branchID),
				masterFinPeriodIDType: typeof(APRegister.closedTranPeriodID))]
			[PXUIField(DisplayName = "Closed Period", Visibility = PXUIVisibility.Invisible)]
			public virtual string ClosedFinPeriodID { get; set; }

			#endregion

			#region ClosedTranPeriodID

			public abstract class closedTranPeriodID : PX.Data.BQL.BqlString.Field<closedTranPeriodID>
			{
			}

			/// <summary>
			/// The <see cref="PX.Objects.GL.FinancialPeriod">Financial Period</see>, in which the document was closed.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="TranPeriodID"/> field.
			/// </value>
			[PeriodID()]
			[PXUIField(DisplayName = "Closed Master Period", Visibility = PXUIVisibility.Invisible)]
			public virtual string ClosedTranPeriodID { get; set; }

			#endregion

			#region IsRetainageDocument
			public abstract class isRetainageDocument : PX.Data.BQL.BqlBool.Field<isRetainageDocument> { }

			[PXDBBool]
			[PXUIField(DisplayName = "Retainage Bill", Enabled = false, FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual bool? IsRetainageDocument { get; set; }
			#endregion

			#region OrigDocType

			public abstract class origDocType : PX.Data.BQL.BqlString.Field<origDocType>
			{
			}

			/// <summary>
			/// Type of the original (source) document.
			/// </summary>
			[PXDBString(3, IsFixed = true)]
			[APDocType.List()]
			[PXUIField(DisplayName = "Orig. Doc. Type")]
			public virtual string OrigDocType { get; set; }

			#endregion

			#region OrigRefNbr

			public abstract class origRefNbr : PX.Data.BQL.BqlString.Field<origRefNbr>
			{
			}

			/// <summary>
			/// Reference number of the original (source) document.
			/// </summary>
			[PXDBString(15, IsUnicode = true, InputMask = "")]
			[PXUIField(DisplayName = "Orig. Ref. Nbr.")]
			public virtual string OrigRefNbr { get; set; }

			#endregion

			#region CuryOrigDocAmt

			public abstract class curyOrigDocAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmt>
			{
			}

			/// <summary>
			/// The amount to be paid for the document in the currency of the document. (See <see cref="CuryID"/>)
			/// </summary>
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCurrency(typeof(APRegister.curyInfoID), typeof(APRegister.origDocAmt))]
			[PXUIField(DisplayName = "Amount", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual decimal? CuryOrigDocAmt { get; set; }

			#endregion

			#region OrigDocAmt

			public abstract class origDocAmt : PX.Data.BQL.BqlDecimal.Field<origDocAmt>
			{
			}

			/// <summary>
			/// The amount to be paid for the document in the base currency of the company. (See <see cref="Company.BaseCuryID"/>)
			/// </summary>
			[PXDBBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Amount")]
			public virtual decimal? OrigDocAmt { get; set; }

			#endregion

			#region CuryDocBal

			public abstract class curyDocBal : PX.Data.BQL.BqlDecimal.Field<curyDocBal>
			{
			}

			protected decimal? _CuryDocBal;

			/// <summary>
			/// The balance of the Accounts Payable document after tax (if inclusive) and the discount in the currency of the document. (See <see cref="CuryID"/>)
			/// </summary>
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCurrency(typeof(APRegister.curyInfoID), typeof(APRegister.docBal), BaseCalc = false)]
			[PXUIField(DisplayName = "Balance", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
			public virtual decimal? CuryDocBal { get; set; }

			#endregion

			#region DocBal

			public abstract class docBal : PX.Data.BQL.BqlDecimal.Field<docBal>
			{
			}

			/// <summary>
			/// The balance of the Accounts Payable document after tax (if inclusive) and the discount in the base currency of the company. (See <see cref="Company.BaseCuryID"/>)
			/// </summary>
			[PXDBBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			public virtual decimal? DocBal { get; set; }

			#endregion

			#region DiscTot

			public abstract class discTot : PX.Data.BQL.BqlDecimal.Field<discTot>
			{
			}

			/// <summary>
			/// Total discount associated with the document in the base currency of the company. (See <see cref="Company.BaseCuryID"/>)
			/// </summary>
			[PXDBBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			public virtual decimal? DiscTot { get; set; }

			#endregion

			#region CuryDiscTot

			public abstract class curyDiscTot : PX.Data.BQL.BqlDecimal.Field<curyDiscTot>
			{
			}

			/// <summary>
			/// Total discount associated with the document in the currency of the document. (See <see cref="CuryID"/>)
			/// </summary>
			[PXDBCurrency(typeof(APRegister.curyInfoID), typeof(APRegister.discTot))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Discount Total", Enabled = true)]
			public virtual decimal? CuryDiscTot { get; set; }

			#endregion

			#region CuryOrigDiscAmt

			public abstract class curyOrigDiscAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDiscAmt>
			{
			}

			/// <summary>
			/// !REV! The amount of the cash discount taken for the original document.
			/// (Presented in the currency of the document, see <see cref="CuryID"/>)
			/// </summary>
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCurrency(typeof(APRegister.curyInfoID), typeof(APRegister.origDiscAmt))]
			[PXUIField(DisplayName = "Cash Discount", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual decimal? CuryOrigDiscAmt { get; set; }

			#endregion

			#region OrigDiscAmt

			public abstract class origDiscAmt : PX.Data.BQL.BqlDecimal.Field<origDiscAmt>
			{
			}

			/// <summary>
			/// The amount of the cash discount taken for the original document.
			/// (Presented in the base currency of the company, see <see cref="Company.BaseCuryID"/>)
			/// </summary>
			[PXDBBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			public virtual decimal? OrigDiscAmt { get; set; }

			#endregion

			#region CuryDiscTaken

			public abstract class curyDiscTaken : PX.Data.BQL.BqlDecimal.Field<curyDiscTaken>
			{
			}

			/// <summary>
			/// !REV! The amount of the cash discount taken.
			/// (Presented in the currency of the document, see <see cref="CuryID"/>)
			/// </summary>
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCurrency(typeof(APRegister.curyInfoID), typeof(APRegister.discTaken))]
			public virtual decimal? CuryDiscTaken { get; set; }

			#endregion

			#region DiscTaken

			public abstract class discTaken : PX.Data.BQL.BqlDecimal.Field<discTaken>
			{
			}

			/// <summary>
			/// The amount of the cash discount taken.
			/// (Presented in the base currency of the company, see <see cref="Company.BaseCuryID"/>)
			/// </summary>
			[PXDBBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			public virtual decimal? DiscTaken { get; set; }

			#endregion

			#region CuryDiscBal

			public abstract class curyDiscBal : PX.Data.BQL.BqlDecimal.Field<curyDiscBal>
			{
			}

			/// <summary>
			/// The difference between the cash discount that was available and the actual amount of cash discount taken.
			/// (Presented in the currency of the document, see <see cref="CuryID"/>)
			/// </summary>
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCurrency(typeof(APRegister.curyInfoID), typeof(APRegister.discBal), BaseCalc = false)]
			[PXUIField(DisplayName = "Cash Discount Balance", Visibility = PXUIVisibility.SelectorVisible,
				Enabled = false)]
			public virtual decimal? CuryDiscBal { get; set; }

			#endregion

			#region DiscBal

			public abstract class discBal : PX.Data.BQL.BqlDecimal.Field<discBal>
			{
			}

			/// <summary>
			/// The difference between the cash discount that was available and the actual amount of cash discount taken.
			/// (Presented in the base currency of the company, see <see cref="Company.BaseCuryID"/>)
			/// </summary>
			[PXDBBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			public virtual decimal? DiscBal { get; set; }

			#endregion

			#region CuryOrigWhTaxAmt

			public abstract class curyOrigWhTaxAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigWhTaxAmt>
			{
			}

			/// <summary>
			/// The amount of withholding tax calculated for the document, if applicable, in the currency of the document. (See <see cref="CuryID"/>)
			/// </summary>
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCurrency(typeof(APRegister.curyInfoID), typeof(APRegister.origWhTaxAmt))]
			[PXUIField(DisplayName = "With. Tax", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
			public virtual decimal? CuryOrigWhTaxAmt { get; set; }

			#endregion

			#region OrigWhTaxAmt

			public abstract class origWhTaxAmt : PX.Data.BQL.BqlDecimal.Field<origWhTaxAmt>
			{
			}

			/// <summary>
			/// The amount of withholding tax calculated for the document, if applicable, in the base currency of the company. (See <see cref="Company.BaseCuryID"/>)
			/// </summary>
			[PXDBBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			public virtual decimal? OrigWhTaxAmt { get; set; }

			#endregion

			#region CuryWhTaxBal

			public abstract class curyWhTaxBal : PX.Data.BQL.BqlDecimal.Field<curyWhTaxBal>
			{
			}

			protected decimal? _CuryWhTaxBal;

			/// <summary>
			/// !REV! The difference between the original amount of withholding tax to be payed and the amount that was actually paid.
			/// (Presented in the currency of the document, see <see cref="CuryID"/>)
			/// </summary>
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCurrency(typeof(APRegister.curyInfoID), typeof(APRegister.whTaxBal), BaseCalc = false)]
			public virtual decimal? CuryWhTaxBal { get; set; }

			#endregion

			#region WhTaxBal

			public abstract class whTaxBal : PX.Data.BQL.BqlDecimal.Field<whTaxBal>
			{
			}

			protected decimal? _WhTaxBal;

			/// <summary>
			/// The difference between the original amount of withholding tax to be payed and the amount that was actually paid.
			/// (Presented in the base currency of the company, see <see cref="Company.BaseCuryID"/>)
			/// </summary>
			[PXDBBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			public virtual decimal? WhTaxBal { get; set; }

			#endregion

			#region CuryTaxWheld

			public abstract class curyTaxWheld : PX.Data.BQL.BqlDecimal.Field<curyTaxWheld>
			{
			}

			/// <summary>
			/// !REV! The amount of tax withheld from the payments to the document.
			/// (Presented in the currency of the document, see <see cref="CuryID"/>)
			/// </summary>
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCurrency(typeof(APRegister.curyInfoID), typeof(APRegister.taxWheld))]
			public virtual decimal? CuryTaxWheld { get; set; }

			#endregion

			#region TaxWheld

			public abstract class taxWheld : PX.Data.BQL.BqlDecimal.Field<taxWheld>
			{
			}

			/// <summary>
			/// The amount of tax withheld from the payments to the document.
			/// (Presented in the base currency of the company, see <see cref="Company.BaseCuryID"/>)
			/// </summary>
			[PXDBBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			public virtual decimal? TaxWheld { get; set; }

			#endregion

			#region CuryRetainageTotal

			public abstract class curyRetainageTotal : PX.Data.BQL.BqlDecimal.Field<curyRetainageTotal>
			{
			}

			[PXDBCurrency(typeof(APRegister.curyInfoID), typeof(APRegister.retainageTotal))]
			[PXUIField(DisplayName = "Original Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual decimal? CuryRetainageTotal { get; set; }

			#endregion

			#region RetainageTotal

			public abstract class retainageTotal : PX.Data.BQL.BqlDecimal.Field<retainageTotal>
			{
			}

			[PXDBBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Original Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			public virtual decimal? RetainageTotal { get; set; }

			#endregion

			#region CuryRetainageUnreleasedAmt

			public abstract class curyRetainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<curyRetainageUnreleasedAmt>
			{
			}

			[PXDBCurrency(typeof(APRegister.curyInfoID), typeof(APRegister.retainageUnreleasedAmt))]
			[PXUIField(DisplayName = "Unreleased Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual decimal? CuryRetainageUnreleasedAmt { get; set; }

			#endregion

			#region RetainageUnreleasedAmt

			public abstract class retainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<retainageUnreleasedAmt>
			{
			}

			[PXDBBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Unreleased Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			public virtual decimal? RetainageUnreleasedAmt { get; set; }

			#endregion

			#region DocDesc

			public abstract class docDesc : PX.Data.BQL.BqlString.Field<docDesc>
			{
			}

			protected string _DocDesc;

			/// <summary>
			/// Description of the document.
			/// </summary>
			[PXDBString(Common.Constants.TranDescLength512, IsUnicode = true)]
			[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual string DocDesc { get; set; }

			#endregion

			#region Status

			public abstract class status : PX.Data.BQL.BqlString.Field<status>
			{
			}

			[PXDBString(1, IsFixed = true)]
			[PXDefault(APDocStatus.Hold)]
			[PXUIField(DisplayName = "Status", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
			[APDocStatus.List]
			public virtual string Status { get; set; }

			#endregion

			#region RGOLAmt

			public abstract class rGOLAmt : PX.Data.BQL.BqlDecimal.Field<rGOLAmt>
			{
			}

			/// <summary>
			/// Realized Gain and Loss amount associated with the document.
			/// </summary>
			[PXDBDecimal(4)]
			[PXDefault(TypeCode.Decimal, "0.0")]
			public virtual decimal? RGOLAmt { get; set; }

			#endregion

			#region BalanceSign

			public abstract class signBalance : PX.Data.BQL.BqlDecimal.Field<signBalance>
			{
			}

			[PXDecimal()]
			[PXDependsOnFields(typeof(docType))]
			[PXDBCalced(typeof(
				Switch<Case<Where<APRegister.docType.IsIn<APDocType.refund, APDocType.voidRefund, APDocType.invoice,
							APDocType.creditAdj, APDocType.quickCheck>>,
						decimal1>,
					decimal_1>), typeof(decimal))]
			public virtual decimal? SignBalance { get; set; }

			#endregion

			#region IsMigratedRecord
			public abstract class isMigratedRecord : PX.Data.BQL.BqlBool.Field<isMigratedRecord> { }

			/// <summary>
			/// Specifies (if set to <c>true</c>) that the record has been created
			/// in migration mode without affecting GL module.
			/// </summary>
			[MigratedRecord(typeof(APSetup.migrationMode))]
			public virtual bool? IsMigratedRecord
			{
				get;
				set;
			}
			#endregion

			#region BatchNbr
			public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr> { }

			/// <summary>
			/// Number of the <see cref="Batch"/>, generated for the document on release.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Batch.BatchNbr">Batch.BatchNbr</see> field.
			/// </value>
			[PXDBString(15, IsUnicode = true)]
			[PXUIField(DisplayName = "Batch Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
			[PXSelector(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAP>>>))]
			[BatchNbr(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAP>>>),
				IsMigratedRecordField = typeof(isMigratedRecord))]
			public virtual string BatchNbr
			{
				get;
				set;
			}
			#endregion
		}

		[Serializable()]
		[PXProjection(typeof(Select2<APRegister,
			LeftJoin<Ref.APInvoice,
				On<Ref.APInvoice.docType, Equal<APRegister.docType>,
				And<Ref.APInvoice.refNbr, Equal<APRegister.refNbr>>>,
			LeftJoin<Ref.APPayment,
				On<Ref.APPayment.docType, Equal<APRegister.docType>,
				And<Ref.APPayment.refNbr, Equal<APRegister.refNbr>>>>>,
			Where<Not<Where<Ref.APInvoice.docType, Equal<APDocType.prepayment>, And<Ref.APPayment.refNbr, IsNull>>>>>))]

		[PXPrimaryGraph(new Type[] {
				typeof(APQuickCheckEntry),
				typeof(TX.TXInvoiceEntry),
				typeof(APInvoiceEntry),
				typeof(APPaymentEntry)
			},
			new Type[] {
				typeof(Select<APQuickCheck,
					Where<APQuickCheck.docType, Equal<Current<APDocumentResult.docType>>,
						And<APQuickCheck.refNbr, Equal<Current<APDocumentResult.refNbr>>>>>),
				typeof(Select<APInvoice,
					Where<APInvoice.docType, Equal<Current<APDocumentResult.docType>>,
						And<APInvoice.refNbr, Equal<Current<APDocumentResult.refNbr>>,
							And<Where<APInvoice.released, Equal<False>, And<APInvoice.origModule, Equal<GL.BatchModule.moduleTX>>>>>>>),
				typeof(Select<APInvoice,
					Where<APInvoice.docType, Equal<Current<APDocumentResult.docType>>,
						And<APInvoice.refNbr, Equal<Current<APDocumentResult.refNbr>>>>>),
				typeof(Select<APPayment,
					Where<APPayment.docType, Equal<Current<APDocumentResult.docType>>,
						And<APPayment.refNbr, Equal<Current<APDocumentResult.refNbr>>>>>)
			})]
		[PXCacheName("Vendor Details")]
		public partial class APDocumentResult : IBqlTable
		{
			#region DocType

			public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
			{
			}

			[PXDBString(3, IsKey = true, IsFixed = true, BqlTable = typeof(APRegister))]
			[PXDefault()]
			[APDocType.List()]
			[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = true, TabOrder = 0)]
			public virtual string DocType { get; set; }

			#endregion

			#region RefNbr

			public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
			{
			}

			[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "", BqlTable = typeof(APRegister))]
			[PXDefault()]
			[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible, TabOrder = 1)]
			[PXSelector(typeof(Search<APRegister.refNbr>), Filterable = true)]
			public virtual string RefNbr { get; set; }

			#endregion

			#region InstallmentCntr

			public abstract class installmentCntr : PX.Data.BQL.BqlShort.Field<installmentCntr>
			{
			}

			/// <summary>
			/// The counter of <see cref="TermsInstallment">installments</see> associated with the document.
			/// </summary>
			[PXDBShort(BqlTable = typeof(Ref.APInvoice))]
			public virtual short? InstallmentCntr { get; set; }

			#endregion

			#region SuppliedByVendorID
			public abstract class suppliedByVendorID : PX.Data.BQL.BqlInt.Field<suppliedByVendorID> { }

			/// <summary>
			/// A reference to the <see cref="Vendor"/>.
			/// </summary>
			/// <value>
			/// An integer identifier of the vendor that supplied the goods.
			/// </value>
			[Vendor(
				DisplayName = "Supplied-by Vendor",
				DescriptionField = typeof(Vendor.acctName),
				FieldClass = nameof(FeaturesSet.VendorRelations),
				CacheGlobal = true,
				Filterable = true,
				BqlTable = typeof(Ref.APInvoice))]
			public virtual int? SuppliedByVendorID { get; set; }
			#endregion

			#region CuryID

			public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID>
			{
			}

			/// <summary>
			/// Code of the <see cref="PX.Objects.CM.Currency">Currency</see> of the document.
			/// </summary>
			/// <value>
			/// Defaults to the <see cref="Company.BaseCuryID">company's base currency</see>.
			/// </value>
			[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL", BqlTable = typeof(APRegister))]
			[PXUIField(DisplayName = "Currency", Visibility = PXUIVisibility.SelectorVisible)]
			[PXDefault(typeof(Current<AccessInfo.baseCuryID>))]
			[PXSelector(typeof(Currency.curyID))]
			public virtual string CuryID { get; set; }

			#endregion

			#region BranchID

			public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID>
			{
			}

			/// <summary>
			/// Identifier of the <see cref="PX.Objects.GL.Branch">Branch</see>, to which the document belongs.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="PX.Objects.GL.Branch.BranchID">Branch.BranchID</see> field.
			/// </value>
			[GL.Branch(BqlTable = typeof(APRegister))]
			public virtual int? BranchID { get; set; }

			#endregion

			#region DocDate

			public abstract class docDate : PX.Data.BQL.BqlDateTime.Field<docDate>
			{
			}

			/// <summary>
			/// Date of the document.
			/// </summary>
			[PXDBDate(BqlTable = typeof(APRegister))]
			[PXDefault(typeof(AccessInfo.businessDate))]
			[PXUIField(DisplayName = "Date", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual DateTime? DocDate { get; set; }

			#endregion

			#region TranPeriodID

			public abstract class tranPeriodID : PX.Data.BQL.BqlString.Field<tranPeriodID>
			{
			}

			/// <summary>
			/// <see cref="FinPeriod">Financial Period</see> of the document.
			/// </summary>
			/// <value>
			/// Determined by the <see cref="APRegister.DocDate">date of the document</see>. Unlike <see cref="APRegister.FinPeriodID"/>
			/// the value of this field can't be overriden by user.
			/// </value>
			[PeriodID(BqlTable = typeof(APRegister))]
			[PXUIField(DisplayName = "Master Period")]
			public virtual string TranPeriodID { get; set; }

			#endregion

			#region FinPeriodID

			public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID>
			{
			}

			/// <summary>
			/// <see cref="FinPeriod">Financial Period</see> of the document.
			/// </summary>
			/// <value>
			/// Defaults to the period, to which the <see cref="APRegister.DocDate"/> belongs, but can be overriden by user.
			/// </value>
			[APOpenPeriod(
				typeof(APRegister.docDate),
				branchSourceType: typeof(APRegister.branchID),
				masterFinPeriodIDType: typeof(APRegister.tranPeriodID),
				IsHeader = true,
				BqlTable = typeof(APRegister))]
			[PXUIField(DisplayName = "Post Period", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual string FinPeriodID { get; set; }

			#endregion

			#region VendorID

			public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID>
			{
			}

			/// <summary>
			/// Identifier of the <see cref="Vendor"/>, whom the document belongs to.
			/// </summary>
			[VendorActive(
				Visibility = PXUIVisibility.SelectorVisible,
				DescriptionField = typeof(Vendor.acctName),
				CacheGlobal = true,
				Filterable = true,
				BqlTable = typeof(APRegister))]
			[PXDefault]
			public virtual int? VendorID { get; set; }

			#endregion

			#region APAccountID

			public abstract class aPAccountID : PX.Data.BQL.BqlInt.Field<aPAccountID>
			{
			}

			/// <summary>
			/// Identifier of the AP account, to which the document belongs.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Account.AccountID"/> field.
			/// </value>
			[PXDefault]
			[Account(typeof(APRegister.branchID), typeof(Search<Account.accountID,
					Where2<Match<Current<AccessInfo.userName>>,
						And<Account.active, Equal<True>,
							And<Where<Current<GLSetup.ytdNetIncAccountID>, IsNull,
								Or<Account.accountID, NotEqual<Current<GLSetup.ytdNetIncAccountID>>>>>>>>),
				DisplayName = "AP Account",
				ControlAccountForModule = ControlAccountModule.AP,
				BqlTable = typeof(APRegister))]
			public virtual int? APAccountID { get; set; }

			#endregion

			#region APSubID

			public abstract class aPSubID : PX.Data.BQL.BqlInt.Field<aPSubID>
			{
			}

			/// <summary>
			/// Identifier of the AP subaccount, to which the document belongs.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Sub.SubID"/> field.
			/// </value>
			[PXDefault]
			[SubAccount(typeof(APRegister.aPAccountID), typeof(APRegister.branchID),
				true, DescriptionField = typeof(Sub.description), DisplayName = "AP Subaccount",
				Visibility = PXUIVisibility.Visible, BqlTable = typeof(APRegister))]
			public virtual int? APSubID { get; set; }

			#endregion

			#region CuryInfoID

			public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID>
			{
			}

			/// <summary>
			/// Identifier of the <see cref="PX.Objects.CM.CurrencyInfo">CurrencyInfo</see> object associated with the document.
			/// </summary>
			/// <value>
			/// Generated automatically. Corresponds to the <see cref="PX.Objects.CM.CurrencyInfo.CurrencyInfoID"/> field.
			/// </value>
			[PXDBLong(BqlTable = typeof(APRegister))]
			[CurrencyInfo(ModuleCode = BatchModule.AP)]
			public virtual long? CuryInfoID { get; set; }

			#endregion

			#region Released

			public abstract class released : PX.Data.BQL.BqlBool.Field<released>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document was released.
			/// </summary>
			[PXDBBool(BqlTable = typeof(APRegister))]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Released", Visible = false)]
			public virtual bool? Released { get; set; }

			#endregion

			#region Prebooked

			public abstract class prebooked : PX.Data.BQL.BqlBool.Field<prebooked>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document was prebooked.
			/// </summary>
			[PXDBBool(BqlTable = typeof(APRegister))]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Prebooked")]
			public virtual bool? Prebooked { get; set; }

			#endregion

			#region OpenDoc

			public abstract class openDoc : PX.Data.BQL.BqlBool.Field<openDoc>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document is open.
			/// </summary>
			[PXDBBool(BqlTable = typeof(APRegister))]
			[PXDefault(true)]
			[PXUIField(DisplayName = "Open", Visible = false)]
			public virtual bool? OpenDoc { get; set; }

			#endregion

			#region Hold

			public abstract class hold : PX.Data.BQL.BqlBool.Field<hold>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document is on hold and thus cannot be released.
			/// </summary>
			[PXDBBool(BqlTable = typeof(APRegister))]
			[PXUIField(DisplayName = "Hold", Visibility = PXUIVisibility.Visible)]
			[PXDefault(true, typeof(APSetup.holdEntry))]
			public virtual bool? Hold { get; set; }

			#endregion

			#region Scheduled

			public abstract class scheduled : PX.Data.BQL.BqlBool.Field<scheduled>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document is part of a <c>Schedule</c> and serves as a template for generating other documents according to it.
			/// </summary>
			[PXDBBool(BqlTable = typeof(APRegister))]
			[PXDefault(false)]
			public virtual bool? Scheduled { get; set; }

			#endregion

			#region Voided

			public abstract class voided : PX.Data.BQL.BqlBool.Field<voided>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document was voided. In this case <see cref="VoidBatchNbr"/> field will hold the number of the voiding <see cref="Batch"/>.
			/// </summary>
			[PXDBBool(BqlTable = typeof(APRegister))]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Void", Visible = false)]
			public virtual bool? Voided { get; set; }

			#endregion

			#region NoteID

			public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID>
			{
				public class NoteAttribute : PXNoteAttribute
				{
					public NoteAttribute()
					{
						BqlTable = typeof(APRegister);
					}
					protected override bool IsVirtualTable(Type table)
					{
						return false;
					}
				}
			}

			/// <summary>
			/// Identifier of the <see cref="PX.Data.Note">Note</see> object, associated with the document.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="PX.Data.Note.NoteID">Note.NoteID</see> field.
			/// </value>
			[noteID.Note]
			public virtual Guid? NoteID { get; set; }

			#endregion

			#region ClosedDate

			public abstract class closedDate : PX.Data.BQL.BqlDateTime.Field<closedDate>
			{
			}

			/// <summary>
			/// The date of the last application.
			/// </summary>
			[PXDBDate(BqlTable = typeof(APRegister))]
			[PXUIField(DisplayName = "Closed Date", Visibility = PXUIVisibility.Invisible)]
			public virtual DateTime? ClosedDate { get; set; }

			#endregion

			#region ClosedFinPeriodID

			public abstract class closedFinPeriodID : PX.Data.BQL.BqlString.Field<closedFinPeriodID>
			{
			}

			/// <summary>
			/// The <see cref="PX.Objects.GL.FinancialPeriod">Financial Period</see>, in which the document was closed.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="FinPeriodID"/> field.
			/// </value>
			[FinPeriodID(
				branchSourceType: typeof(APRegister.branchID),
				masterFinPeriodIDType: typeof(APRegister.closedTranPeriodID),
				BqlTable = typeof(APRegister))]
			[PXUIField(DisplayName = "Closed Period", Visibility = PXUIVisibility.Invisible)]
			public virtual string ClosedFinPeriodID { get; set; }

			#endregion

			#region ClosedTranPeriodID

			public abstract class closedTranPeriodID : PX.Data.BQL.BqlString.Field<closedTranPeriodID>
			{
			}

			/// <summary>
			/// The <see cref="PX.Objects.GL.FinancialPeriod">Financial Period</see>, in which the document was closed.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="TranPeriodID"/> field.
			/// </value>
			[PeriodID(BqlTable = typeof(APRegister))]
			[PXUIField(DisplayName = "Closed Master Period", Visibility = PXUIVisibility.Invisible)]
			public virtual string ClosedTranPeriodID { get; set; }

			#endregion

			#region IsRetainageDocument
			public abstract class isRetainageDocument : PX.Data.BQL.BqlBool.Field<isRetainageDocument> { }

			[PXDBBool(BqlTable = typeof(APRegister))]
			[PXUIField(DisplayName = "Retainage Bill", Enabled = false, FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual bool? IsRetainageDocument { get; set; }
			#endregion

			#region CuryOrigDocAmt

			public abstract class curyOrigDocAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmt>
			{
			}

			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXCurrency(typeof(APRegister.curyInfoID), typeof(APRegister.origDocAmt))]
			[PXUIField(DisplayName = "Currency Origin. Amount")]
			[PXDBCalced(typeof(
				Switch<Case<Where<APRegister.docType.IsEqual<APDocType.prepayment>.And<APInvoice.refNbr.IsNotNull>>,
					decimal0>,
				Mult<APRegister.signBalance, APRegister.curyOrigDocAmt>>), typeof(decimal))]
			public virtual decimal? CuryOrigDocAmt { get; set; }

			#endregion

			#region OrigDocAmt

			public abstract class origDocAmt : PX.Data.BQL.BqlDecimal.Field<origDocAmt>
			{
			}

			[PXBaseCury]
			[PXUIField(DisplayName = "Origin. Amount")]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCalced(typeof(
				Switch<Case<Where<APRegister.docType.IsEqual<APDocType.prepayment>.And<APInvoice.refNbr.IsNotNull>>,
						decimal0>,
					Mult<APRegister.signBalance, APRegister.origDocAmt>>), typeof(decimal))]
			public virtual decimal? OrigDocAmt { get; set; }

			#endregion

			#region CuryRetainageTotal

			public abstract class curyRetainageTotal : PX.Data.BQL.BqlDecimal.Field<curyRetainageTotal>
			{
			}

			[PXCurrency(typeof(curyInfoID), typeof(retainageTotal))]
			[PXUIField(DisplayName = "Original Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXDBCalced(typeof(
				Mult<APRegister.signBalance, APRegister.curyRetainageTotal>), typeof(decimal))]
			public virtual decimal? CuryRetainageTotal { get; set; }

			#endregion

			#region RetainageTotal

			public abstract class retainageTotal : PX.Data.BQL.BqlDecimal.Field<retainageTotal>
			{
			}

			[PXBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Original Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDBCalced(typeof(
				Mult<APRegister.signBalance, APRegister.retainageTotal>), typeof(decimal))]
			public virtual decimal? RetainageTotal { get; set; }

			#endregion

			#region CuryOrigDocAmtWithRetainageTotal
			public abstract class
				curyOrigDocAmtWithRetainageTotal : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmtWithRetainageTotal>
			{
			}

			[PXCury(typeof(curyID))]
			[PXUIField(DisplayName = "Total Amount", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXDBCalced(typeof(APRegister.curyOrigDocAmt.Add<APRegister.curyRetainageTotal>
				.Multiply<APRegister.signBalance>), typeof(decimal))]
			public virtual decimal? CuryOrigDocAmtWithRetainageTotal { get; set; }

			#endregion

			#region OrigDocAmtWithRetainageTotal
			public abstract class origDocAmtWithRetainageTotal : PX.Data.BQL.BqlDecimal.Field<origDocAmtWithRetainageTotal> { }

			[PXBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Total Amount", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDBCalced(typeof(APRegister.origDocAmt.Add<APRegister.retainageTotal>
				.Multiply<APRegister.signBalance>), typeof(decimal))]
			public virtual decimal? OrigDocAmtWithRetainageTotal
			{
				get;
				set;
			}
			#endregion

			#region RGOLAmt

			public abstract class rGOLAmt : PX.Data.BQL.BqlDecimal.Field<rGOLAmt>
			{
			}

			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "RGOL Amount")]
			[PXDBCalced(typeof(Mult<decimal_1, APRegister.rGOLAmt>), typeof(decimal))]
			public virtual decimal? RGOLAmt { get; set; }

			#endregion

			#region OrigDocType

			public abstract class origDocType : PX.Data.BQL.BqlString.Field<origDocType>
			{
			}

			/// <summary>
			/// Type of the original (source) document.
			/// </summary>
			[PXDBString(3, IsFixed = true, BqlTable = typeof(APRegister))]
			[APDocType.List()]
			[PXUIField(DisplayName = "Orig. Doc. Type")]
			public virtual string OrigDocType { get; set; }

			#endregion

			#region OrigRefNbr

			public abstract class origRefNbr : PX.Data.BQL.BqlString.Field<origRefNbr>
			{
			}

			/// <summary>
			/// Reference number of the original (source) document.
			/// </summary>
			[PXDBString(15, IsUnicode = true, InputMask = "", BqlTable = typeof(APRegister))]
			[PXUIField(DisplayName = "Orig. Ref. Nbr.")]
			public virtual string OrigRefNbr { get; set; }

			#endregion

			#region ExtRefNbr

			public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr>
			{
			}

			[PXString(30, IsUnicode = true)]
			[PXUIField(DisplayName = "Vendor Invoice Nbr./Payment Nbr.")]
			[PXDBCalced(typeof(IsNull<
				Ref.APInvoice.invoiceNbr,
				Ref.APPayment.extRefNbr>), typeof(string))]
			public virtual string ExtRefNbr { get; set; }

			#endregion

			#region PaymentMethodID

			public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID>
			{
			}

			[PXDBString(10, IsUnicode = true, BqlField = typeof(Ref.APPayment.paymentMethodID))]
			[PXUIField(DisplayName = "Payment Method")]
			public virtual string PaymentMethodID { get; set; }

			#endregion

			#region Begin Balance

			#region CuryBegBalance

			public abstract class curyBegBalance : PX.Data.BQL.BqlDecimal.Field<curyBegBalance>
			{
			}

			[PXCurrency(typeof(APRegister.curyInfoID), typeof(APDocumentResult.begBalance))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Currency Period Beg. Balance")]
			public virtual decimal? CuryBegBalance { get; set; }

			#endregion

			#region BegBalance

			public abstract class begBalance : PX.Data.BQL.BqlDecimal.Field<begBalance>
			{
			}

			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Period Beg. Balance")]
			public virtual decimal? BegBalance { get; set; }

			#endregion

			#endregion

			#region CuryDiscActTaken

			public abstract class curyDiscActTaken : PX.Data.BQL.BqlDecimal.Field<curyDiscActTaken>
			{
			}

			[PXDefault(TypeCode.Decimal, "0.0")]
			//[PXCury(typeof(APRegister.curyID))]
			[PXCurrency(typeof(APRegister.curyInfoID), typeof(APDocumentResult.discActTaken))]
			[PXUIField(DisplayName = "Currency Cash Discount Taken")]
			[PXDBCalced(typeof(
				Mult<APRegister.signBalance, APRegister.curyDiscTaken>), typeof(decimal))]
			public virtual decimal? CuryDiscActTaken { get; set; }

			#endregion

			#region DiscActTaken

			public abstract class discActTaken : PX.Data.BQL.BqlDecimal.Field<discActTaken>
			{
			}

			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Cash Discount Taken")]
			[PXDBCalced(typeof(
				Mult<APRegister.signBalance, APRegister.discTaken>), typeof(decimal))]
			public virtual decimal? DiscActTaken { get; set; }

			#endregion

			#region CuryTaxWheld

			public abstract class curyTaxWheld : PX.Data.BQL.BqlDecimal.Field<curyTaxWheld>
			{
			}

			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXCurrency(typeof(APRegister.curyInfoID), typeof(APDocumentResult.taxWheld))]
			[PXUIField(DisplayName = "Currency Tax Withheld")]
			[PXDBCalced(typeof(
				Mult<APRegister.signBalance, APRegister.curyTaxWheld>), typeof(decimal))]
			public virtual decimal? CuryTaxWheld { get; set; }

			#endregion

			#region TaxWheld

			public abstract class taxWheld : PX.Data.BQL.BqlDecimal.Field<taxWheld>
			{
			}

			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Tax Withheld")]
			[PXDBCalced(typeof(
				Mult<APRegister.signBalance, APRegister.taxWheld>), typeof(decimal))]
			public virtual decimal? TaxWheld { get; set; }

			#endregion

			#region APTurnover

			public abstract class aPTurnover : PX.Data.BQL.BqlDecimal.Field<aPTurnover>
			{
			}

			/// <summary>
			/// Expected GL turnover for the document.
			/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// </summary>
			[PXBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "AP Turnover")]
			[PXDBCalced(typeof(decimal0), typeof(decimal))]
			public virtual decimal? APTurnover { get; set; }

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

			#region End Balance

			#region CuryDocBal

			public abstract class curyDocBal : PX.Data.BQL.BqlDecimal.Field<curyDocBal>
			{
			}

			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXCurrency(typeof(APRegister.curyInfoID), typeof(APRegister.docBal), BaseCalc = false)]
			[PXUIField(DisplayName = "Currency Balance")]
			[PXDBCalced(typeof(
				Switch<Case<Where<APRegister.voided, NotEqual<True>>,
					Mult<APRegister.signBalance, APRegister.curyDocBal>>,
				decimal0>), typeof(decimal))]
			public virtual decimal? CuryDocBal { get; set; }

			#endregion

			#region DocBal

			public abstract class docBal : PX.Data.BQL.BqlDecimal.Field<docBal>
			{
			}

			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance")]
			[PXDBCalced(typeof(
				Switch<Case<Where<APRegister.voided, NotEqual<True>>,
						Mult<APRegister.signBalance, APRegister.docBal>>,
					decimal0>), typeof(decimal))]
			public virtual decimal? DocBal { get; set; }

			#endregion

			#region CuryWhTaxBal

			public abstract class curyWhTaxBal : PX.Data.BQL.BqlDecimal.Field<curyWhTaxBal>
			{
			}

			protected decimal? _CuryWhTaxBal;

			/// <summary>
			/// !REV! The difference between the original amount of withholding tax to be payed and the amount that was actually paid.
			/// (Presented in the currency of the document, see <see cref="CuryID"/>)
			/// </summary>
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXCurrency(typeof(APRegister.curyInfoID), typeof(APRegister.whTaxBal), BaseCalc = false)]
			[PXDBCalced(typeof(
				Mult<APRegister.signBalance, APRegister.curyWhTaxBal>), typeof(decimal))]
			public virtual decimal? CuryWhTaxBal { get; set; }

			#endregion

			#region WhTaxBal

			public abstract class whTaxBal : PX.Data.BQL.BqlDecimal.Field<whTaxBal>
			{
			}

			protected decimal? _WhTaxBal;

			/// <summary>
			/// The difference between the original amount of withholding tax to be payed and the amount that was actually paid.
			/// (Presented in the base currency of the company, see <see cref="Company.BaseCuryID"/>)
			/// </summary>
			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCalced(typeof(
				Mult<APRegister.signBalance, APRegister.whTaxBal>), typeof(decimal))]
			public virtual decimal? WhTaxBal { get; set; }

			#endregion

			#region CuryRetainageUnreleasedAmt

			public abstract class curyRetainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<curyRetainageUnreleasedAmt>
			{
			}

			[PXCurrency(typeof(APRegister.curyInfoID), typeof(APRegister.retainageUnreleasedAmt))]
			[PXUIField(DisplayName = "Unreleased Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXDBCalced(typeof(
				Mult<APRegister.signBalance, APRegister.curyRetainageUnreleasedAmt>), typeof(decimal))]
			public virtual decimal? CuryRetainageUnreleasedAmt { get; set; }

			#endregion

			#region RetainageUnreleasedAmt

			public abstract class retainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<retainageUnreleasedAmt>
			{
			}

			[PXBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Unreleased Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDBCalced(typeof(
				Mult<APRegister.signBalance, APRegister.retainageUnreleasedAmt>), typeof(decimal))]
			public virtual decimal? RetainageUnreleasedAmt { get; set; }

			#endregion

			#endregion

			#region Status

			public abstract class status : PX.Data.BQL.BqlString.Field<status>
			{
			}

			[PXDBString(1, IsFixed = true, BqlTable = typeof(APRegister))]
			[PXDefault(APDocStatus.Hold)]
			[PXUIField(DisplayName = "Status", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
			[APDocStatus.List]
			public virtual string Status { get; set; }

			#endregion

			#region DocDesc
			public abstract class docDesc : PX.Data.BQL.BqlString.Field<docDesc> { }

			/// <summary>
			/// Description of the document.
			/// </summary>
			[PXDBString(Common.Constants.TranDescLength512, IsUnicode = true, BqlTable = typeof(APRegister))]
			[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual string DocDesc { get; set; }

			#endregion

			#region TranPostPeriodID
			public abstract class tranPostPeriodID : PX.Data.BQL.BqlString.Field<tranPostPeriodID> { }
			[PXString]
			[PXDBCalced(typeof(APRegister.tranPeriodID), typeof(string))]
			public virtual string TranPostPeriodID
			{
				get;
				set;
			}
			#endregion
			#region FinPostPeriodID
			public abstract class finPostPeriodID : PX.Data.BQL.BqlString.Field<finPostPeriodID> { }
			[PXString]
			[PXDBCalced(typeof(APRegister.finPeriodID), typeof(string))]
			public virtual string FinPostPeriodID
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
			[MigratedRecord(typeof(APSetup.migrationMode), BqlTable = typeof(APRegister))]
			public virtual bool? IsMigratedRecord
			{
				get;
				set;
			}
			#endregion
			#region BatchNbr
			public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr> { }

			/// <summary>
			/// Number of the <see cref="Batch"/>, generated for the document on release.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Batch.BatchNbr">Batch.BatchNbr</see> field.
			/// </value>
			[PXDBString(15, IsUnicode = true, BqlTable = typeof(APRegister))]
			[PXUIField(DisplayName = "Batch Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
			[PXSelector(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAP>>>))]
			[BatchNbr(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAP>>>),
				IsMigratedRecordField = typeof(isMigratedRecord))]
			public virtual string BatchNbr
			{
				get;
				set;
			}
			#endregion
		}

		[PXProjection(typeof(Select2<APDocumentResult,
			LeftJoin<APTranPostGL,
				On<APTranPostGL.docType, Equal<APDocumentResult.docType>,
				And<APTranPostGL.refNbr, Equal<APDocumentResult.refNbr>>>>,
			Where<APDocumentResult.installmentCntr, IsNull>>))]
		[PXHidden]

		public partial class APDocumentPeriodResult : IBqlTable
		{
			#region DocType

			public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
			{
			}

			[PXDBString(3, IsKey = true, IsFixed = true, BqlTable = typeof(APDocumentResult))]
			[PXDefault()]
			[APDocType.List()]
			[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = true, TabOrder = 0)]
			public virtual string DocType { get; set; }

			#endregion

			#region RefNbr

			public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
			{
			}

			[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "", BqlTable = typeof(APDocumentResult))]
			[PXDefault()]
			[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible, TabOrder = 1)]
			[PXSelector(typeof(Search<APDocumentResult.refNbr>), Filterable = true)]
			public virtual string RefNbr { get; set; }

			#endregion

			#region SuppliedByVendorID
			public abstract class suppliedByVendorID : PX.Data.BQL.BqlInt.Field<suppliedByVendorID> { }

			/// <summary>
			/// A reference to the <see cref="Vendor"/>.
			/// </summary>
			/// <value>
			/// An integer identifier of the vendor that supplied the goods.
			/// </value>
			[Vendor(
				DisplayName = "Supplied-by Vendor",
				DescriptionField = typeof(Vendor.acctName),
				FieldClass = nameof(FeaturesSet.VendorRelations),
				CacheGlobal = true,
				Filterable = true,
				BqlTable = typeof(APDocumentResult))]
			public virtual int? SuppliedByVendorID { get; set; }
			#endregion

			#region CuryID

			public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID>
			{
			}

			/// <summary>
			/// Code of the <see cref="PX.Objects.CM.Currency">Currency</see> of the document.
			/// </summary>
			/// <value>
			/// Defaults to the <see cref="Company.BaseCuryID">company's base currency</see>.
			/// </value>
			[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL", BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Currency", Visibility = PXUIVisibility.SelectorVisible)]
			[PXDefault(typeof(Current<AccessInfo.baseCuryID>))]
			[PXSelector(typeof(Currency.curyID))]
			public virtual string CuryID { get; set; }

			#endregion

			#region BranchID

			public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID>
			{
			}

			/// <summary>
			/// Identifier of the <see cref="PX.Objects.GL.Branch">Branch</see>, to which the document belongs.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="PX.Objects.GL.Branch.BranchID">Branch.BranchID</see> field.
			/// </value>
			[GL.Branch(BqlTable = typeof(APDocumentResult))]
			public virtual int? BranchID { get; set; }

			#endregion

			#region DocDate

			public abstract class docDate : PX.Data.BQL.BqlDateTime.Field<docDate>
			{
			}

			/// <summary>
			/// Date of the document.
			/// </summary>
			[PXDBDate(BqlTable = typeof(APDocumentResult))]
			[PXDefault(typeof(AccessInfo.businessDate))]
			[PXUIField(DisplayName = "Date", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual DateTime? DocDate { get; set; }

			#endregion

			#region TranPeriodID

			public abstract class tranPeriodID : PX.Data.BQL.BqlString.Field<tranPeriodID>
			{
			}

			/// <summary>
			/// <see cref="FinPeriod">Financial Period</see> of the document.
			/// </summary>
			/// <value>
			/// Determined by the <see cref="APDocumentResult.DocDate">date of the document</see>. Unlike <see cref="APDocumentResult.FinPeriodID"/>
			/// the value of this field can't be overriden by user.
			/// </value>
			[PeriodID(BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Master Period")]
			public virtual string TranPeriodID { get; set; }

			#endregion

			#region FinPeriodID

			public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID>
			{
			}

			/// <summary>
			/// <see cref="FinPeriod">Financial Period</see> of the document.
			/// </summary>
			/// <value>
			/// Defaults to the period, to which the <see cref="APDocumentResult.DocDate"/> belongs, but can be overriden by user.
			/// </value>
			[APOpenPeriod(
				typeof(APDocumentResult.docDate),
				branchSourceType: typeof(APDocumentResult.branchID),
				masterFinPeriodIDType: typeof(APDocumentResult.tranPeriodID),
				IsHeader = true,
				BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Post Period", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual string FinPeriodID { get; set; }

			#endregion

			#region VendorID

			public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID>
			{
			}

			/// <summary>
			/// Identifier of the <see cref="Vendor"/>, whom the document belongs to.
			/// </summary>
			[VendorActive(
				Visibility = PXUIVisibility.SelectorVisible,
				DescriptionField = typeof(Vendor.acctName),
				CacheGlobal = true,
				Filterable = true,
				BqlTable = typeof(APDocumentResult))]
			[PXDefault]
			public virtual int? VendorID { get; set; }

			#endregion

			#region APAccountID

			public abstract class aPAccountID : PX.Data.BQL.BqlInt.Field<aPAccountID>
			{
			}

			/// <summary>
			/// Identifier of the AP account, to which the document belongs.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Account.AccountID"/> field.
			/// </value>
			[PXDefault]
			[Account(typeof(APDocumentResult.branchID), typeof(Search<Account.accountID,
					Where2<Match<Current<AccessInfo.userName>>,
						And<Account.active, Equal<True>,
							And<Where<Current<GLSetup.ytdNetIncAccountID>, IsNull,
								Or<Account.accountID, NotEqual<Current<GLSetup.ytdNetIncAccountID>>>>>>>>),
				DisplayName = "AP Account",
				ControlAccountForModule = ControlAccountModule.AP,
				BqlTable = typeof(APDocumentResult))]
			public virtual int? APAccountID { get; set; }

			#endregion

			#region APSubID

			public abstract class aPSubID : PX.Data.BQL.BqlInt.Field<aPSubID>
			{
			}

			/// <summary>
			/// Identifier of the AP subaccount, to which the document belongs.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Sub.SubID"/> field.
			/// </value>
			[PXDefault]
			[SubAccount(typeof(APDocumentResult.aPAccountID), typeof(APDocumentResult.branchID),
				true, DescriptionField = typeof(Sub.description), DisplayName = "AP Subaccount",
				Visibility = PXUIVisibility.Visible, BqlTable = typeof(APDocumentResult))]
			public virtual int? APSubID { get; set; }

			#endregion
			#region IsMigratedRecord
			public abstract class isMigratedRecord : PX.Data.BQL.BqlBool.Field<isMigratedRecord> { }

			/// <summary>
			/// Specifies (if set to <c>true</c>) that the record has been created
			/// in migration mode without affecting GL module.
			/// </summary>
			[MigratedRecord(typeof(APSetup.migrationMode), BqlTable = typeof(APRegister))]
			public virtual bool? IsMigratedRecord
			{
				get;
				set;
			}
			#endregion

			#region BatchNbr
			public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr> { }

			/// <summary>
			/// Number of the <see cref="Batch"/>, generated for the document on release.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Batch.BatchNbr">Batch.BatchNbr</see> field.
			/// </value>
			[PXDBString(15, IsUnicode = true, BqlTable=typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Batch Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
			[PXSelector(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAP>>>))]
			[BatchNbr(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAP>>>),
				IsMigratedRecordField = typeof(isMigratedRecord))]
			public virtual string BatchNbr
			{
				get;
				set;
			}
			#endregion

			#region CuryInfoID

			public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID>
			{
			}

			/// <summary>
			/// Identifier of the <see cref="PX.Objects.CM.CurrencyInfo">CurrencyInfo</see> object associated with the document.
			/// </summary>
			/// <value>
			/// Generated automatically. Corresponds to the <see cref="PX.Objects.CM.CurrencyInfo.CurrencyInfoID"/> field.
			/// </value>
			[PXDBLong(BqlTable = typeof(APDocumentResult))]
			[CurrencyInfo(ModuleCode = BatchModule.AP)]
			public virtual long? CuryInfoID { get; set; }

			#endregion

			#region Released

			public abstract class released : PX.Data.BQL.BqlBool.Field<released>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document was released.
			/// </summary>
			[PXDBBool(BqlTable = typeof(APDocumentResult))]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Released", Visible = false)]
			public virtual bool? Released { get; set; }

			#endregion

			#region Prebooked

			public abstract class prebooked : PX.Data.BQL.BqlBool.Field<prebooked>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document was prebooked.
			/// </summary>
			[PXDBBool(BqlTable = typeof(APDocumentResult))]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Prebooked")]
			public virtual bool? Prebooked { get; set; }

			#endregion

			#region OpenDoc

			public abstract class openDoc : PX.Data.BQL.BqlBool.Field<openDoc>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document is open.
			/// </summary>
			[PXDBBool(BqlTable = typeof(APDocumentResult))]
			[PXDefault(true)]
			[PXUIField(DisplayName = "Open", Visible = false)]
			public virtual bool? OpenDoc { get; set; }

			#endregion

			#region Hold

			public abstract class hold : PX.Data.BQL.BqlBool.Field<hold>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document is on hold and thus cannot be released.
			/// </summary>
			[PXDBBool(BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Hold", Visibility = PXUIVisibility.Visible)]
			[PXDefault(true, typeof(APSetup.holdEntry))]
			public virtual bool? Hold { get; set; }

			#endregion

			#region Scheduled

			public abstract class scheduled : PX.Data.BQL.BqlBool.Field<scheduled>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document is part of a <c>Schedule</c> and serves as a template for generating other documents according to it.
			/// </summary>
			[PXDBBool(BqlTable = typeof(APDocumentResult))]
			[PXDefault(false)]
			public virtual bool? Scheduled { get; set; }

			#endregion

			#region Voided

			public abstract class voided : PX.Data.BQL.BqlBool.Field<voided>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document was voided. In this case <see cref="VoidBatchNbr"/> field will hold the number of the voiding <see cref="Batch"/>.
			/// </summary>
			[PXDBBool(BqlTable = typeof(APDocumentResult))]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Void", Visible = false)]
			public virtual bool? Voided { get; set; }

			#endregion

			#region NoteID

			public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID>
			{
			}

			/// <summary>
			/// Identifier of the <see cref="PX.Data.Note">Note</see> object, associated with the document.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="PX.Data.Note.NoteID">Note.NoteID</see> field.
			/// </value>
			[PXNote(DescriptionField = typeof(refNbr), BqlTable = typeof(APDocumentResult))]
			public virtual Guid? NoteID { get; set; }

			#endregion

			#region ClosedDate

			public abstract class closedDate : PX.Data.BQL.BqlDateTime.Field<closedDate>
			{
			}

			/// <summary>
			/// The date of the last application.
			/// </summary>
			[PXDBDate(BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Closed Date", Visibility = PXUIVisibility.Invisible)]
			public virtual DateTime? ClosedDate { get; set; }

			#endregion

			#region ClosedFinPeriodID

			public abstract class closedFinPeriodID : PX.Data.BQL.BqlString.Field<closedFinPeriodID>
			{
			}

			/// <summary>
			/// The <see cref="PX.Objects.GL.FinancialPeriod">Financial Period</see>, in which the document was closed.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="FinPeriodID"/> field.
			/// </value>
			[FinPeriodID(
				branchSourceType: typeof(APDocumentResult.branchID),
				masterFinPeriodIDType: typeof(APDocumentResult.closedTranPeriodID),
				BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Closed Period", Visibility = PXUIVisibility.Invisible)]
			public virtual string ClosedFinPeriodID { get; set; }

			#endregion

			#region ClosedTranPeriodID

			public abstract class closedTranPeriodID : PX.Data.BQL.BqlString.Field<closedTranPeriodID>
			{
			}

			/// <summary>
			/// The <see cref="PX.Objects.GL.FinancialPeriod">Financial Period</see>, in which the document was closed.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="TranPeriodID"/> field.
			/// </value>
			[PeriodID(BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Closed Master Period", Visibility = PXUIVisibility.Invisible)]
			public virtual string ClosedTranPeriodID { get; set; }

			#endregion

			#region IsRetainageDocument
			public abstract class isRetainageDocument : PX.Data.BQL.BqlBool.Field<isRetainageDocument> { }

			[PXDBBool(BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Retainage Bill", Enabled = false, FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual bool? IsRetainageDocument { get; set; }
			#endregion

			#region CuryOrigDocAmt

			public abstract class curyOrigDocAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmt>
			{
			}

			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCurrency(typeof(APDocumentResult.curyInfoID), typeof(APDocumentResult.origDocAmt), BqlTable=typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Currency Origin. Amount")]
			public virtual decimal? CuryOrigDocAmt { get; set; }

			#endregion

			#region OrigDocAmt

			public abstract class origDocAmt : PX.Data.BQL.BqlDecimal.Field<origDocAmt>
			{
			}

			[PXDBBaseCury(BqlTable=typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Origin. Amount")]
			[PXDefault(TypeCode.Decimal, "0.0")]
			public virtual decimal? OrigDocAmt { get; set; }

			#endregion

			#region CuryRetainageTotal

			public abstract class curyRetainageTotal : PX.Data.BQL.BqlDecimal.Field<curyRetainageTotal>
			{
			}

			[PXDBCurrency(typeof(curyInfoID), typeof(retainageTotal), BqlTable=typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Original Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual decimal? CuryRetainageTotal { get; set; }

			#endregion

			#region RetainageTotal

			public abstract class retainageTotal : PX.Data.BQL.BqlDecimal.Field<retainageTotal>
			{
			}

			[PXDBBaseCury(BqlTable=typeof(APDocumentResult))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Original Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			public virtual decimal? RetainageTotal { get; set; }

			#endregion

			#region CuryOrigDocAmtWithRetainageTotal
			public abstract class
				curyOrigDocAmtWithRetainageTotal : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmtWithRetainageTotal>
			{
			}

			[PXDBCury(typeof(curyID), BqlTable=typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Total Amount", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual decimal? CuryOrigDocAmtWithRetainageTotal { get; set; }

			#endregion

			#region OrigDocAmtWithRetainageTotal
			public abstract class origDocAmtWithRetainageTotal : PX.Data.BQL.BqlDecimal.Field<origDocAmtWithRetainageTotal> { }

			[PXDBBaseCury(BqlTable=typeof(APDocumentResult))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Total Amount", FieldClass = nameof(FeaturesSet.Retainage))]
			public virtual decimal? OrigDocAmtWithRetainageTotal
			{
				get;
				set;
			}
			#endregion

			#region RGOLAmt

			public abstract class rGOLAmt : PX.Data.BQL.BqlDecimal.Field<rGOLAmt>
			{
			}

			[PXBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "RGOL Amount")]
			[PXDBCalced(typeof(Mult<decimal_1, APTranPostGL.turnRGOLAmt>), typeof(decimal))]
			public virtual decimal? RGOLAmt { get; set; }

			#endregion

			#region OrigDocType

			public abstract class origDocType : PX.Data.BQL.BqlString.Field<origDocType>
			{
			}

			/// <summary>
			/// Type of the original (source) document.
			/// </summary>
			[PXDBString(3, IsFixed = true, BqlTable = typeof(APDocumentResult))]
			[APDocType.List()]
			[PXUIField(DisplayName = "Orig. Doc. Type")]
			public virtual string OrigDocType { get; set; }

			#endregion

			#region OrigRefNbr

			public abstract class origRefNbr : PX.Data.BQL.BqlString.Field<origRefNbr>
			{
			}

			/// <summary>
			/// Reference number of the original (source) document.
			/// </summary>
			[PXDBString(15, IsUnicode = true, InputMask = "", BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Orig. Ref. Nbr.")]
			public virtual string OrigRefNbr { get; set; }

			#endregion

			#region ExtRefNbr

			public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr>
			{
			}

			[PXDBString(30, IsUnicode = true, BqlTable=typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Vendor Invoice Nbr./Payment Nbr.")]
			public virtual string ExtRefNbr { get; set; }

			#endregion

			#region PaymentMethodID

			public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID>
			{
			}

			[PXDBString(10, IsUnicode = true, BqlTable=typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Payment Method")]
			public virtual string PaymentMethodID { get; set; }

			#endregion

			#region Begin Balance

			#region CuryBegBalance

			public abstract class curyBegBalance : PX.Data.BQL.BqlDecimal.Field<curyBegBalance>
			{
			}

			[PXCurrency(typeof(APDocumentResult.curyInfoID), typeof(APDocumentResult.begBalance))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Currency Period Beg. Balance")]
			[PXDBCalced(typeof(
				Switch<
					Case<Where<APDocumentResult.released, Equal<False>,
								And<APDocumentResult.prebooked, Equal<False>,
								And<CurrentValue<APDocumentFilter.useMasterCalendar>, Equal<False>,
								And<APDocumentResult.finPeriodID, Less<CurrentValue<APDocumentFilter.finPeriodID>>>>>>,
						APDocumentResult.curyOrigDocAmt,
					Case<Where<APDocumentResult.released, Equal<False>,
								And<APDocumentResult.prebooked, Equal<False>,
								And<CurrentValue<APDocumentFilter.useMasterCalendar>, Equal<True>,
								And<APDocumentResult.tranPeriodID, Less<CurrentValue<APDocumentFilter.finPeriodID>>>>>>,
						APDocumentResult.curyOrigDocAmt,
					Case<Where<CurrentValue<APDocumentFilter.useMasterCalendar>, Equal<True>,
							And<APTranPostGL.tranPeriodID, Less<CurrentValue<APDocumentFilter.finPeriodID>>>>,
						APTranPostGL.curyBalanceAmt,
					Case<Where<CurrentValue<APDocumentFilter.useMasterCalendar>, Equal<False>,
								And<APTranPostGL.finPeriodID, Less<CurrentValue<APDocumentFilter.finPeriodID>>>>,
							APTranPostGL.curyBalanceAmt>>>>,
					decimal0>), typeof(decimal))]
			public virtual decimal? CuryBegBalance { get; set; }

			#endregion

			#region BegBalance

			public abstract class begBalance : PX.Data.BQL.BqlDecimal.Field<begBalance>
			{
			}

			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Period Beg. Balance")]
			[PXDBCalced(typeof(
				Switch<
					Case<Where<APDocumentResult.released, Equal<False>,
								And<APDocumentResult.prebooked, Equal<False>,
								And<CurrentValue<APDocumentFilter.useMasterCalendar>, Equal<False>,
								And<APDocumentResult.finPeriodID, Less<CurrentValue<APDocumentFilter.finPeriodID>>>>>>,
						APDocumentResult.origDocAmt,
					Case<Where<APDocumentResult.released, Equal<False>,
								And<APDocumentResult.prebooked, Equal<False>,
								And<CurrentValue<APDocumentFilter.useMasterCalendar>, Equal<True>,
								And<APDocumentResult.tranPeriodID, Less<CurrentValue<APDocumentFilter.finPeriodID>>>>>>,
						APDocumentResult.origDocAmt,
					Case<Where<CurrentValue<APDocumentFilter.useMasterCalendar>, Equal<True>,
							And<APTranPostGL.tranPeriodID, Less<CurrentValue<APDocumentFilter.finPeriodID>>>>,
						APTranPostGL.balanceAmt,
					Case<Where<CurrentValue<APDocumentFilter.useMasterCalendar>, Equal<False>,
							And<APTranPostGL.finPeriodID, Less<CurrentValue<APDocumentFilter.finPeriodID>>>>,
						APTranPostGL.balanceAmt>>>>,
					decimal0>), typeof(decimal))]
			public virtual decimal? BegBalance { get; set; }

			#endregion

			#endregion

			#region CuryDiscActTaken

			public abstract class curyDiscActTaken : PX.Data.BQL.BqlDecimal.Field<curyDiscActTaken>
			{
			}

			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXCurrency(typeof(APDocumentResult.curyInfoID), typeof(APDocumentResult.discActTaken))]
			[PXUIField(DisplayName = "Currency Cash Discount Taken")]
			[PXDBCalced(typeof(IsNull<APTranPostGL.curyTurnDiscAmt, decimal0>), typeof(decimal))]
			public virtual decimal? CuryDiscActTaken { get; set; }

			#endregion

			#region DiscActTaken

			public abstract class discActTaken : PX.Data.BQL.BqlDecimal.Field<discActTaken>
			{
			}

			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Cash Discount Taken")]
			[PXDBCalced(typeof(IsNull<APTranPostGL.turnDiscAmt, decimal0>), typeof(decimal))]
			public virtual decimal? DiscActTaken { get; set; }

			#endregion

			#region CuryTaxWheld

			public abstract class curyTaxWheld : PX.Data.BQL.BqlDecimal.Field<curyTaxWheld>
			{
			}

			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXCurrency(typeof(APDocumentResult.curyInfoID), typeof(APDocumentResult.taxWheld))]
			[PXUIField(DisplayName = "Currency Tax Withheld")]
			[PXDBCalced(typeof(
				Switch<
					Case<Where<APTranPostGL.type.IsNotEqual<APTranPost.type.application>>, decimal0>,
					IsNull<APTranPostGL.curyTurnWhTaxAmt, decimal0>>), typeof(decimal))]
			public virtual decimal? CuryTaxWheld { get; set; }

			#endregion

			#region TaxWheld

			public abstract class taxWheld : PX.Data.BQL.BqlDecimal.Field<taxWheld>
			{
			}

			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Tax Withheld")]
			[PXDBCalced(typeof(
				Switch<
					Case<Where<APTranPostGL.type.IsNotEqual<APTranPost.type.application>>, decimal0>,
					IsNull<APTranPostGL.turnWhTaxAmt, decimal0>>), typeof(decimal))]
			public virtual decimal? TaxWheld { get; set; }

			#endregion

			#region APTurnover

			public abstract class aPTurnover : PX.Data.BQL.BqlDecimal.Field<aPTurnover>
			{
			}

			/// <summary>
			/// Expected GL turnover for the document.
			/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// </summary>
			[PXBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "AP Turnover")]
			[PXDBCalced(typeof(decimal0), typeof(decimal))]
			public virtual decimal? APTurnover { get; set; }

			#endregion

			#region Turn
			public abstract class turn : PX.Data.BQL.BqlDecimal.Field<turn> { }
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDecimal]
			[PXDBCalced(typeof(
				Switch<
					Case<Where<CurrentValue<APDocumentFilter.useMasterCalendar>, Equal<True>,
							And<APTranPostGL.tranPeriodID, Equal<CurrentValue<APDocumentFilter.finPeriodID>>>>,
						decimal1,
						Case<Where<CurrentValue<APDocumentFilter.useMasterCalendar>, Equal<False>,
								And<APTranPostGL.finPeriodID, Equal<CurrentValue<APDocumentFilter.finPeriodID>>>>,
							decimal1>>,
					decimal0>), typeof(decimal))]
			public virtual decimal? Turn
			{
				get;
				set;
			}
			#endregion

			#region End Balance

			#region CuryDocBal

			public abstract class curyDocBal : PX.Data.BQL.BqlDecimal.Field<curyDocBal>
			{
			}

			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXCurrency(typeof(APDocumentResult.curyInfoID), typeof(APDocumentResult.docBal), BaseCalc = false)]
			[PXUIField(DisplayName = "Currency Balance")]
			[PXDBCalced(typeof(
				Switch<Case<Where<APDocumentResult.released, Equal<False>, And<APDocumentResult.prebooked, Equal<False>>>,
					APDocumentResult.curyOrigDocAmt>,
				APTranPostGL.curyBalanceAmt>), typeof(decimal))]
			public virtual decimal? CuryDocBal { get; set; }

			#endregion

			#region DocBal

			public abstract class docBal : PX.Data.BQL.BqlDecimal.Field<docBal>
			{
			}

			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance")]
			[PXDBCalced(typeof(
				Switch<Case<Where<APDocumentResult.released, Equal<False>,And<APDocumentResult.prebooked, Equal<False>>>,
						APDocumentResult.curyOrigDocAmt>,
					APTranPostGL.balanceAmt>), typeof(decimal))]
			public virtual decimal? DocBal { get; set; }

			#endregion

			#region CuryRetainageUnreleasedAmt

			public abstract class curyRetainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<curyRetainageUnreleasedAmt>
			{
			}

			[PXCurrency(typeof(APDocumentResult.curyInfoID), typeof(APDocumentResult.retainageUnreleasedAmt))]
			[PXUIField(DisplayName = "Unreleased Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXDBCalced(typeof(
				IsNull<APTranPostGL.curyTurnRetainageAmt,decimal0>), typeof(decimal))]
			public virtual decimal? CuryRetainageUnreleasedAmt { get; set; }

			#endregion

			#region RetainageUnreleasedAmt

			public abstract class retainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<retainageUnreleasedAmt>
			{
			}

			[PXBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Unreleased Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDBCalced(typeof(
				IsNull<APTranPostGL.turnRetainageAmt,decimal0>), typeof(decimal))]
			public virtual decimal? RetainageUnreleasedAmt { get; set; }

			#endregion

			#endregion

			#region Status

			public abstract class status : PX.Data.BQL.BqlString.Field<status>
			{
			}

			[PXDBString(1, IsFixed = true, BqlTable = typeof(APDocumentResult))]
			[PXDefault(APDocStatus.Hold)]
			[PXUIField(DisplayName = "Status", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
			[APDocStatus.List]
			public virtual string Status { get; set; }

			#endregion

			#region DocDesc
			public abstract class docDesc : PX.Data.BQL.BqlString.Field<docDesc> { }

			/// <summary>
			/// Description of the document.
			/// </summary>
			[PXDBString(Common.Constants.TranDescLength512, IsUnicode = true, BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual string DocDesc { get; set; }

			#endregion
			#region TranPostPeriodID
			public abstract class tranPostPeriodID : PX.Data.BQL.BqlString.Field<tranPostPeriodID> { }
			[PeriodID(BqlField = typeof(APTranPostGL.tranPeriodID))]
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
			/// Defaults to the period, to which the <see cref="APRegister.DocDate"/> belongs, but can be virtualn by user.
			/// </value>
			[APOpenPeriod(
				typeof(docDate),
				branchSourceType: typeof(branchID),
				masterFinPeriodIDType: typeof(tranPostPeriodID),
				IsHeader = true,
				BqlField=typeof(APTranPostGL.finPeriodID))]
			public virtual string FinPostPeriodID
			{
				get;
				set;
			}
			#endregion
		}

		[PXProjection(typeof(Select2<APDocumentResult,
			LeftJoin<APTranPostGL,
				On<APTranPostGL.tranType, Equal<APDocumentResult.docType>,
				And<APTranPostGL.tranRefNbr, Equal<APDocumentResult.refNbr>>>>>))]
		[PXHidden]
		public partial class GLDocumentPeriodResult : IBqlTable
		{
			#region DocType

			public abstract class docType : PX.Data.BQL.BqlString.Field<docType>
			{
			}

			[PXDBString(3, IsKey = true, IsFixed = true, BqlTable = typeof(APDocumentResult))]
			[PXDefault()]
			[APDocType.List()]
			[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = true, TabOrder = 0)]
			public virtual string DocType { get; set; }

			#endregion

			#region RefNbr

			public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
			{
			}

			[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "", BqlTable = typeof(APDocumentResult))]
			[PXDefault()]
			[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible, TabOrder = 1)]
			[PXSelector(typeof(Search<APDocumentResult.refNbr>), Filterable = true)]
			public virtual string RefNbr { get; set; }

			#endregion

			#region CuryID

			public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID>
			{
			}

			/// <summary>
			/// Code of the <see cref="PX.Objects.CM.Currency">Currency</see> of the document.
			/// </summary>
			/// <value>
			/// Defaults to the <see cref="Company.BaseCuryID">company's base currency</see>.
			/// </value>
			[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL", BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Currency", Visibility = PXUIVisibility.SelectorVisible)]
			[PXDefault(typeof(Current<AccessInfo.baseCuryID>))]
			[PXSelector(typeof(Currency.curyID))]
			public virtual string CuryID { get; set; }

			#endregion

			#region BranchID

			public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID>
			{
			}

			/// <summary>
			/// Identifier of the <see cref="PX.Objects.GL.Branch">Branch</see>, to which the document belongs.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="PX.Objects.GL.Branch.BranchID">Branch.BranchID</see> field.
			/// </value>
			[GL.Branch(BqlTable = typeof(APTranPostGL))]
			public virtual int? BranchID { get; set; }

			#endregion

			#region DocDate

			public abstract class docDate : PX.Data.BQL.BqlDateTime.Field<docDate>
			{
			}

			/// <summary>
			/// Date of the document.
			/// </summary>
			[PXDBDate(BqlTable = typeof(APDocumentResult))]
			[PXDefault(typeof(AccessInfo.businessDate))]
			[PXUIField(DisplayName = "Date", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual DateTime? DocDate { get; set; }

			#endregion

			#region TranPeriodID

			public abstract class tranPeriodID : PX.Data.BQL.BqlString.Field<tranPeriodID>
			{
			}

			/// <summary>
			/// <see cref="FinPeriod">Financial Period</see> of the document.
			/// </summary>
			/// <value>
			/// Determined by the <see cref="APDocumentResult.DocDate">date of the document</see>. Unlike <see cref="APDocumentResult.FinPeriodID"/>
			/// the value of this field can't be overriden by user.
			/// </value>
			[PeriodID(BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Master Period")]
			public virtual string TranPeriodID { get; set; }

			#endregion

			#region FinPeriodID

			public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID>
			{
			}

			/// <summary>
			/// <see cref="FinPeriod">Financial Period</see> of the document.
			/// </summary>
			/// <value>
			/// Defaults to the period, to which the <see cref="APDocumentResult.DocDate"/> belongs, but can be overriden by user.
			/// </value>
			[APOpenPeriod(
				typeof(APDocumentResult.docDate),
				branchSourceType: typeof(APDocumentResult.branchID),
				masterFinPeriodIDType: typeof(APDocumentResult.tranPeriodID),
				IsHeader = true,
				BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Post Period", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual string FinPeriodID { get; set; }

			#endregion

			#region VendorID

			public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID>
			{
			}

			/// <summary>
			/// Identifier of the <see cref="Vendor"/>, whom the document belongs to.
			/// </summary>
			[VendorActive(
				Visibility = PXUIVisibility.SelectorVisible,
				DescriptionField = typeof(Vendor.acctName),
				CacheGlobal = true,
				Filterable = true,
				BqlTable = typeof(APDocumentResult))]
			[PXDefault]
			public virtual int? VendorID { get; set; }

			#endregion

			#region APAccountID

			public abstract class aPAccountID : PX.Data.BQL.BqlInt.Field<aPAccountID>
			{
			}

			/// <summary>
			/// Identifier of the AP account, to which the document belongs.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Account.AccountID"/> field.
			/// </value>
			[PXDefault]
			[Account(typeof(APDocumentResult.branchID), typeof(Search<Account.accountID,
					Where2<Match<Current<AccessInfo.userName>>,
						And<Account.active, Equal<True>,
							And<Where<Current<GLSetup.ytdNetIncAccountID>, IsNull,
								Or<Account.accountID, NotEqual<Current<GLSetup.ytdNetIncAccountID>>>>>>>>),
				DisplayName = "AP Account",
				ControlAccountForModule = ControlAccountModule.AP,
				BqlField = typeof(APTranPostGL.accountID))]
			public virtual int? APAccountID { get; set; }

			#endregion

			#region APSubID

			public abstract class aPSubID : PX.Data.BQL.BqlInt.Field<aPSubID>
			{
			}

			/// <summary>
			/// Identifier of the AP subaccount, to which the document belongs.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Sub.SubID"/> field.
			/// </value>
			[PXDefault]
			[SubAccount(typeof(APDocumentResult.aPAccountID), typeof(APDocumentResult.branchID),
				true, DescriptionField = typeof(Sub.description), DisplayName = "AP Subaccount",
				Visibility = PXUIVisibility.Visible,
				BqlField = typeof(APTranPostGL.subID))]
			public virtual int? APSubID { get; set; }

			#endregion

			#region IsMigratedRecord
			public abstract class isMigratedRecord : PX.Data.BQL.BqlBool.Field<isMigratedRecord> { }

			/// <summary>
			/// Specifies (if set to <c>true</c>) that the record has been created
			/// in migration mode without affecting GL module.
			/// </summary>
			[MigratedRecord(typeof(APSetup.migrationMode), BqlTable = typeof(APDocumentResult))]
			public virtual bool? IsMigratedRecord
			{
				get;
				set;
			}
			#endregion

			#region BatchNbr
			public abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr> { }

			/// <summary>
			/// Number of the <see cref="Batch"/>, generated for the document on release.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="Batch.BatchNbr">Batch.BatchNbr</see> field.
			/// </value>
			[PXDBString(15, IsUnicode = true, BqlTable=typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Batch Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
			[PXSelector(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAP>>>))]
			[BatchNbr(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAP>>>),
				IsMigratedRecordField = typeof(GLDocumentPeriodResult.isMigratedRecord))]
			public virtual string BatchNbr
			{
				get;
				set;
			}
			#endregion

			#region CuryInfoID

			public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID>
			{
			}

			/// <summary>
			/// Identifier of the <see cref="PX.Objects.CM.CurrencyInfo">CurrencyInfo</see> object associated with the document.
			/// </summary>
			/// <value>
			/// Generated automatically. Corresponds to the <see cref="PX.Objects.CM.CurrencyInfo.CurrencyInfoID"/> field.
			/// </value>
			[PXDBLong(BqlTable = typeof(APDocumentResult))]
			[CurrencyInfo(ModuleCode = BatchModule.AP)]
			public virtual long? CuryInfoID { get; set; }

			#endregion

			#region Released

			public abstract class released : PX.Data.BQL.BqlBool.Field<released>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document was released.
			/// </summary>
			[PXDBBool(BqlTable = typeof(APDocumentResult))]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Released", Visible = false)]
			public virtual bool? Released { get; set; }

			#endregion

			#region Prebooked

			public abstract class prebooked : PX.Data.BQL.BqlBool.Field<prebooked>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document was prebooked.
			/// </summary>
			[PXDBBool(BqlTable = typeof(APDocumentResult))]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Prebooked")]
			public virtual bool? Prebooked { get; set; }

			#endregion

			#region OpenDoc

			public abstract class openDoc : PX.Data.BQL.BqlBool.Field<openDoc>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document is open.
			/// </summary>
			[PXDBBool(BqlTable = typeof(APDocumentResult))]
			[PXDefault(true)]
			[PXUIField(DisplayName = "Open", Visible = false)]
			public virtual bool? OpenDoc { get; set; }

			#endregion

			#region Hold

			public abstract class hold : PX.Data.BQL.BqlBool.Field<hold>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document is on hold and thus cannot be released.
			/// </summary>
			[PXDBBool(BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Hold", Visibility = PXUIVisibility.Visible)]
			[PXDefault(true, typeof(APSetup.holdEntry))]
			public virtual bool? Hold { get; set; }

			#endregion

			#region Scheduled

			public abstract class scheduled : PX.Data.BQL.BqlBool.Field<scheduled>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document is part of a <c>Schedule</c> and serves as a template for generating other documents according to it.
			/// </summary>
			[PXDBBool(BqlTable = typeof(APDocumentResult))]
			[PXDefault(false)]
			public virtual bool? Scheduled { get; set; }

			#endregion

			#region Voided

			public abstract class voided : PX.Data.BQL.BqlBool.Field<voided>
			{
			}

			/// <summary>
			/// When set to <c>true</c> indicates that the document was voided. In this case <see cref="VoidBatchNbr"/> field will hold the number of the voiding <see cref="Batch"/>.
			/// </summary>
			[PXDBBool(BqlTable = typeof(APDocumentResult))]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Void", Visible = false)]
			public virtual bool? Voided { get; set; }

			#endregion

			#region NoteID

			public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID>
			{
			}

			/// <summary>
			/// Identifier of the <see cref="PX.Data.Note">Note</see> object, associated with the document.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="PX.Data.Note.NoteID">Note.NoteID</see> field.
			/// </value>
			[PXNote(DescriptionField = typeof(refNbr), BqlTable = typeof(APDocumentResult))]
			public virtual Guid? NoteID { get; set; }

			#endregion

			#region ClosedDate

			public abstract class closedDate : PX.Data.BQL.BqlDateTime.Field<closedDate>
			{
			}

			/// <summary>
			/// The date of the last application.
			/// </summary>
			[PXDBDate(BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Closed Date", Visibility = PXUIVisibility.Invisible)]
			public virtual DateTime? ClosedDate { get; set; }

			#endregion

			#region ClosedFinPeriodID

			public abstract class closedFinPeriodID : PX.Data.BQL.BqlString.Field<closedFinPeriodID>
			{
			}

			/// <summary>
			/// The <see cref="PX.Objects.GL.FinancialPeriod">Financial Period</see>, in which the document was closed.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="FinPeriodID"/> field.
			/// </value>
			[FinPeriodID(
				branchSourceType: typeof(APDocumentResult.branchID),
				masterFinPeriodIDType: typeof(APDocumentResult.closedTranPeriodID),
				BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Closed Period", Visibility = PXUIVisibility.Invisible)]
			public virtual string ClosedFinPeriodID { get; set; }

			#endregion

			#region ClosedTranPeriodID

			public abstract class closedTranPeriodID : PX.Data.BQL.BqlString.Field<closedTranPeriodID>
			{
			}

			/// <summary>
			/// The <see cref="PX.Objects.GL.FinancialPeriod">Financial Period</see>, in which the document was closed.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="TranPeriodID"/> field.
			/// </value>
			[PeriodID(BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Closed Master Period", Visibility = PXUIVisibility.Invisible)]
			public virtual string ClosedTranPeriodID { get; set; }

			#endregion

			#region IsRetainageDocument
			public abstract class isRetainageDocument : PX.Data.BQL.BqlBool.Field<isRetainageDocument> { }

			[PXDBBool(BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Retainage Bill", Enabled = false, FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual bool? IsRetainageDocument { get; set; }
			#endregion

			#region CuryOrigDocAmt

			public abstract class curyOrigDocAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmt>
			{
			}

			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCurrency(typeof(APDocumentResult.curyInfoID), typeof(APDocumentResult.origDocAmt), BqlTable=typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Currency Origin. Amount")]
			public virtual decimal? CuryOrigDocAmt { get; set; }

			#endregion

			#region OrigDocAmt

			public abstract class origDocAmt : PX.Data.BQL.BqlDecimal.Field<origDocAmt>
			{
			}

			[PXDBBaseCury(BqlTable=typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Origin. Amount")]
			[PXDefault(TypeCode.Decimal, "0.0")]
			public virtual decimal? OrigDocAmt { get; set; }

			#endregion

			#region CuryRetainageTotal

			public abstract class curyRetainageTotal : PX.Data.BQL.BqlDecimal.Field<curyRetainageTotal>
			{
			}

			[PXDBCurrency(typeof(curyInfoID), typeof(retainageTotal), BqlTable=typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Original Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual decimal? CuryRetainageTotal { get; set; }

			#endregion

			#region RetainageTotal

			public abstract class retainageTotal : PX.Data.BQL.BqlDecimal.Field<retainageTotal>
			{
			}

			[PXDBBaseCury(BqlTable=typeof(APDocumentResult))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Original Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			public virtual decimal? RetainageTotal { get; set; }

			#endregion

			#region CuryOrigDocAmtWithRetainageTotal
			public abstract class
				curyOrigDocAmtWithRetainageTotal : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmtWithRetainageTotal>
			{
			}

			[PXDBCury(typeof(curyID), BqlTable=typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Total Amount", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual decimal? CuryOrigDocAmtWithRetainageTotal { get; set; }

			#endregion

			#region OrigDocAmtWithRetainageTotal
			public abstract class origDocAmtWithRetainageTotal : PX.Data.BQL.BqlDecimal.Field<origDocAmtWithRetainageTotal> { }

			[PXDBBaseCury(BqlTable=typeof(APDocumentResult))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Total Amount", FieldClass = nameof(FeaturesSet.Retainage))]
			public virtual decimal? OrigDocAmtWithRetainageTotal
			{
				get;
				set;
			}
			#endregion

			#region RGOLAmt

			public abstract class rGOLAmt : PX.Data.BQL.BqlDecimal.Field<rGOLAmt>
			{
			}

			[PXDBBaseCury(BqlTable = typeof(APDocumentResult))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "RGOL Amount")]
			public virtual decimal? RGOLAmt { get; set; }

			#endregion

			#region OrigDocType

			public abstract class origDocType : PX.Data.BQL.BqlString.Field<origDocType>
			{
			}

			/// <summary>
			/// Type of the original (source) document.
			/// </summary>
			[PXDBString(3, IsFixed = true, BqlTable = typeof(APDocumentResult))]
			[APDocType.List()]
			[PXUIField(DisplayName = "Orig. Doc. Type")]
			public virtual string OrigDocType { get; set; }

			#endregion

			#region OrigRefNbr

			public abstract class origRefNbr : PX.Data.BQL.BqlString.Field<origRefNbr>
			{
			}

			/// <summary>
			/// Reference number of the original (source) document.
			/// </summary>
			[PXDBString(15, IsUnicode = true, InputMask = "", BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Orig. Ref. Nbr.")]
			public virtual string OrigRefNbr { get; set; }

			#endregion

			#region ExtRefNbr

			public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr>
			{
			}

			[PXDBString(30, IsUnicode = true, BqlTable=typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Vendor Invoice Nbr./Payment Nbr.")]
			public virtual string ExtRefNbr { get; set; }

			#endregion

			#region PaymentMethodID

			public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID>
			{
			}

			[PXDBString(10, IsUnicode = true, BqlTable=typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Payment Method")]
			public virtual string PaymentMethodID { get; set; }

			#endregion

			#region Begin Balance

			#region CuryBegBalance

			public abstract class curyBegBalance : PX.Data.BQL.BqlDecimal.Field<curyBegBalance>
			{
			}

			[PXCurrency(typeof(APDocumentResult.curyInfoID), typeof(APDocumentResult.begBalance))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Currency Period Beg. Balance")]
			[PXDBCalced(typeof(decimal0), typeof(decimal))]

			public virtual decimal? CuryBegBalance { get; set; }

			#endregion

			#region BegBalance

			public abstract class begBalance : PX.Data.BQL.BqlDecimal.Field<begBalance>
			{
			}

			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Period Beg. Balance")]
			[PXDBCalced(typeof(decimal0), typeof(decimal))]

			public virtual decimal? BegBalance { get; set; }

			#endregion

			#endregion

			#region CuryDiscActTaken

			public abstract class curyDiscActTaken : PX.Data.BQL.BqlDecimal.Field<curyDiscActTaken>
			{
			}

			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXCurrency(typeof(APDocumentResult.curyInfoID), typeof(APDocumentResult.discActTaken))]
			[PXUIField(DisplayName = "Currency Cash Discount Taken")]
			[PXDBCalced(typeof(decimal0), typeof(decimal))]

			public virtual decimal? CuryDiscActTaken { get; set; }

			#endregion

			#region DiscActTaken
			public abstract class discActTaken : PX.Data.BQL.BqlDecimal.Field<discActTaken>
			{
			}

			[PXBaseCury()]
			[PXUIField(DisplayName = "Cash Discount Taken")]
			[PXDBCalced(typeof(decimal0), typeof(decimal))]

			public virtual decimal? DiscActTaken { get; set; }

			#endregion

			#region CuryTaxWheld

			public abstract class curyTaxWheld : PX.Data.BQL.BqlDecimal.Field<curyTaxWheld>
			{
			}

			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXCurrency(typeof(APDocumentResult.curyInfoID), typeof(APDocumentResult.taxWheld))]
			[PXUIField(DisplayName = "Currency Tax Withheld")]
			[PXDBCalced(typeof(decimal0), typeof(decimal))]

			public virtual decimal? CuryTaxWheld { get; set; }

			#endregion

			#region TaxWheld

			public abstract class taxWheld : PX.Data.BQL.BqlDecimal.Field<taxWheld>
			{
			}

			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Tax Withheld")]
			[PXDBCalced(typeof(decimal0), typeof(decimal))]

			public virtual decimal? TaxWheld { get; set; }

			#endregion

			#region APTurnover

			public abstract class aPTurnover : PX.Data.BQL.BqlDecimal.Field<aPTurnover>
			{
			}

			/// <summary>
			/// Expected GL turnover for the document.
			/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// </summary>
			[PXBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "AP Turnover")]
			[PXDBCalced(typeof(
        		Switch<
	                Case<Where<CurrentValue<APDocumentFilter.branchID>, NotEqual<APTranPostGL.branchID>>,
		                decimal0,
        			Case<Where<CurrentValue<APDocumentFilter.useMasterCalendar>, Equal<True>,
        						And<APTranPostGL.tranPeriodID, Equal<CurrentValue<APDocumentFilter.finPeriodID>>>>,
        				Sub<APTranPostGL.creditAPAmt,APTranPostGL.debitAPAmt>,
        			Case<Where<CurrentValue<APDocumentFilter.useMasterCalendar>, Equal<False>,
        						And<APTranPostGL.finPeriodID, Equal<CurrentValue<APDocumentFilter.finPeriodID>>>>,
        				Sub<APTranPostGL.creditAPAmt,APTranPostGL.debitAPAmt>>>>,
        			decimal0>), typeof(decimal))]
			public virtual decimal? APTurnover { get; set; }

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

			#region End Balance

			#region CuryDocBal

			public abstract class curyDocBal : PX.Data.BQL.BqlDecimal.Field<curyDocBal>
			{
			}

			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXCurrency(typeof(APDocumentResult.curyInfoID), typeof(APDocumentResult.docBal), BaseCalc = false)]
			[PXUIField(DisplayName = "Currency Balance")]
			[PXDBCalced(typeof(decimal0), typeof(decimal))]

			public virtual decimal? CuryDocBal { get; set; }

			#endregion

			#region DocBal

			public abstract class docBal : PX.Data.BQL.BqlDecimal.Field<docBal>
			{
			}

			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance")]
			[PXDBCalced(typeof(decimal0), typeof(decimal))]

			public virtual decimal? DocBal { get; set; }

			#endregion

			#region CuryRetainageUnreleasedAmt

			public abstract class curyRetainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<curyRetainageUnreleasedAmt>
			{
			}

			[PXCurrency(typeof(APDocumentResult.curyInfoID), typeof(APDocumentResult.retainageUnreleasedAmt))]
			[PXUIField(DisplayName = "Unreleased Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXDBCalced(typeof(decimal0), typeof(decimal))]

			public virtual decimal? CuryRetainageUnreleasedAmt { get; set; }

			#endregion

			#region RetainageUnreleasedAmt

			public abstract class retainageUnreleasedAmt : PX.Data.BQL.BqlDecimal.Field<retainageUnreleasedAmt>
			{
			}

			[PXBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
			[PXUIField(DisplayName = "Unreleased Retainage", FieldClass = nameof(FeaturesSet.Retainage))]
			[PXDBCalced(typeof(decimal0), typeof(decimal))]

			public virtual decimal? RetainageUnreleasedAmt { get; set; }

			#endregion

			#endregion

			#region Status

			public abstract class status : PX.Data.BQL.BqlString.Field<status>
			{
			}

			[PXDBString(1, IsFixed = true, BqlTable = typeof(APDocumentResult))]
			[PXDefault(APDocStatus.Hold)]
			[PXUIField(DisplayName = "Status", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
			[APDocStatus.List]
			public virtual string Status { get; set; }

			#endregion

			#region DocDesc
			public abstract class docDesc : PX.Data.BQL.BqlString.Field<docDesc> { }

			/// <summary>
			/// Description of the document.
			/// </summary>
			[PXDBString(Common.Constants.TranDescLength512, IsUnicode = true, BqlTable = typeof(APDocumentResult))]
			[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual string DocDesc { get; set; }

			#endregion
			#region TranPostPeriodID
			public abstract class tranPostPeriodID : PX.Data.BQL.BqlString.Field<tranPostPeriodID> { }
			[PeriodID(BqlField = typeof(APTranPostGL.tranPeriodID))]
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
			/// Defaults to the period, to which the <see cref="APRegister.DocDate"/> belongs, but can be virtualn by user.
			/// </value>
			[APOpenPeriod(
				typeof(docDate),
				branchSourceType: typeof(branchID),
				masterFinPeriodIDType: typeof(tranPostPeriodID),
				IsHeader = true,
				BqlField=typeof(APTranPostGL.finPeriodID))]
			public virtual string FinPostPeriodID
			{
				get;
				set;
			}
			#endregion
		}

		public class Ref
		{
			[PXHidden]
			public class APInvoice : IBqlTable
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
				/// The field can have one of the values described in <see cref="APDocType.ListAttribute"/>.
				/// </value>
				[PXDBString(docType.Length, IsKey = true, IsFixed = true)]
				[PXDefault()]
				[APDocType.List()]
				[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = true,
					TabOrder = 0)]
				public virtual string DocType { get; set; }

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
				[PXSelector(
					typeof(Search<APRegister.refNbr, Where<APRegister.docType, Equal<Optional<APRegister.docType>>>>),
					Filterable = true)]
				public virtual string RefNbr { get; set; }

				#endregion

				#region InvoiceNbr

				public abstract class invoiceNbr : PX.Data.BQL.BqlString.Field<invoiceNbr>
				{
				}

				/// <summary>
				/// The original reference number or ID assigned by the customer to the customer document.
				/// </summary>
				[PXDBString(40, IsUnicode = true)]
				[PXUIField(DisplayName = "Customer Order", Visibility = PXUIVisibility.SelectorVisible,
					Required = false)]
				public virtual string InvoiceNbr { get; set; }

				#endregion

				#region InstallmentCntr

				public abstract class installmentCntr : PX.Data.BQL.BqlShort.Field<installmentCntr>
				{
				}

				/// <summary>
				/// The counter of <see cref="TermsInstallment">installments</see> associated with the document.
				/// </summary>
				[PXDBShort()]
				public virtual short? InstallmentCntr { get; set; }

				#endregion

				#region SuppliedByVendorID
				public abstract class suppliedByVendorID : PX.Data.BQL.BqlInt.Field<suppliedByVendorID> { }

				/// <summary>
				/// A reference to the <see cref="Vendor"/>.
				/// </summary>
				/// <value>
				/// An integer identifier of the vendor that supplied the goods.
				/// </value>
				[Vendor(
					DisplayName = "Supplied-by Vendor",
					DescriptionField = typeof(Vendor.acctName),
					FieldClass = nameof(FeaturesSet.VendorRelations),
					CacheGlobal = true,
					Filterable = true,
					Required = true)]
				public virtual int? SuppliedByVendorID { get; set; }
				#endregion
			}

			[PXHidden]
			public class APPayment : IBqlTable
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
				/// The field can have one of the values described in <see cref="APDocType.ListAttribute"/>.
				/// </value>
				[PXDBString(docType.Length, IsKey = true, IsFixed = true)]
				[PXDefault()]
				[APDocType.List()]
				[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = true,
					TabOrder = 0)]
				public virtual string DocType { get; set; }

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
				[PXSelector(
					typeof(Search<APRegister.refNbr, Where<APRegister.docType, Equal<Optional<APRegister.docType>>>>),
					Filterable = true)]
				public virtual string RefNbr { get; set; }

				#endregion

				#region PaymentMethodID

				public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID>
				{
				}


				[PXDBString(10, IsUnicode = true)]
				[PXUIField(DisplayName = "Payment Method", Enabled = false)]

				public virtual string PaymentMethodID { get; set; }

				#endregion

				#region ExtRefNbr

				public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr>
				{
				}

				[PXDBString(40, IsUnicode = true)]
				[PXUIField(DisplayName = "Payment Ref.", Visibility = PXUIVisibility.SelectorVisible)]
				public virtual string ExtRefNbr { get; set; }

				#endregion
			}
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
			#region FinPeriodID
			public new abstract class finPeriodID : PX.Data.BQL.BqlInt.Field<finPeriodID> { }
			#endregion
			#region TranPeriodID
			public new abstract class tranPeriodID : PX.Data.BQL.BqlInt.Field<tranPeriodID> { }
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
				Sub<creditAmt, debitAmt>
			), typeof(decimal))]
			public virtual decimal? GLTurnover
			{
				get;
				set;
			}
			#endregion
		}
		#endregion

		#region Views/Selects

		public PXSetup<APSetup> APSetup;
		public PXSetup<Company> Company;
		public PXFilter<APDocumentFilter> Filter;

		[PXFilterable]
		public PXSelectJoinOrderBy<
			APDocumentResult,
			LeftJoin<APInvoice,
				On<APDocumentResult.docType, Equal<APInvoice.docType>,
				And<APDocumentResult.refNbr, Equal<APInvoice.refNbr>>>>,
			OrderBy<Desc<APDocumentResult.docDate>>> Documents;

		protected virtual IEnumerable documents()
		{
			var result = SelectDetails();
			viewDocument.SetEnabled(result.Count > 0);
			return result;
		}

		protected virtual PXDelegateResult SelectDetails(bool summarize = false)
		{
			APDocumentFilter header = Filter.Current;
			var documentResult = new PXResultset<APDocumentResult, APInvoice>();
			Dictionary<Tuple<string,string>, decimal?> glTurn = null;
			var restrictedFields = summarize
				? new List<Type>
				{
					typeof(APDocumentResult.released),
					typeof(APDocumentResult.curyOrigDocAmt),
					typeof(APDocumentResult.origDocAmt),
					typeof(APDocumentResult.curyDocBal),
					typeof(APDocumentResult.docBal),
					typeof(APDocumentResult.curyRetainageUnreleasedAmt),
					typeof(APDocumentResult.retainageUnreleasedAmt)
				}
				: new List<Type>() {typeof(APDocumentResult)};

			FinPeriodIDAttribute.SetMasterPeriodID<Batch.finPeriodID>(Filter.Cache, header);
			BqlCommand sel = new PXSelectReadonly<APDocumentResult,
					Where<APDocumentResult.vendorID, Equal<Current<APDocumentFilter.vendorID>>>,
					OrderBy<Asc<APDocumentResult.docType, Asc<APDocumentResult.refNbr>>>>
				(this).View.BqlSelect;

			if (header.OrgBAccountID != null)
			{
				sel = sel.WhereAnd<
					Where<APDocumentResult.branchID, Inside<Current<APDocumentFilter.orgBAccountID>>>>(); //MatchWithOrg
			}

			if (header.AccountID != null)
			{
				sel = sel.WhereAnd<Where<APDocumentResult.aPAccountID, Equal<Current<APDocumentFilter.accountID>>>>();
			}

			if ((header.IncludeUnreleased ?? false) == false)
			{
				sel = sel.WhereAnd<Where<Where<APDocumentResult.released, Equal<True>, Or<APDocumentResult.prebooked, Equal<True>>>>>();
			}
			else
			{
				sel = sel.WhereAnd<Where<APDocumentResult.scheduled, Equal<False>,
					And<Where<APDocumentResult.voided, Equal<False>, Or<APDocumentResult.released, Equal<True>>>>>>();
			}

			if (!SubCDUtils.IsSubCDEmpty(header.SubCD))
			{
				sel = BqlCommand.AppendJoin<InnerJoin<Sub, On<Sub.subID, Equal<APDocumentResult.aPSubID>>>>(sel);
				sel = sel.WhereAnd<Where<Sub.subCD, Like<Current<APDocumentFilter.subCDWildcard>>>>();
			}

			if (header.DocType != null)
			{
				sel = sel.WhereAnd<Where<APDocumentResult.docType, Equal<Current<APDocumentFilter.docType>>>>();
			}

			if (header.CuryID != null)
			{
				sel = sel.WhereAnd<Where<APDocumentResult.curyID, Equal<Current<APDocumentFilter.curyID>>>>();
			}
			Type queryType = typeof(APDocumentResult);

			bool byPeriod = (header.FinPeriodID != null);
			if (!byPeriod)
			{
				if (header.ShowAllDocs == false)
				{
					sel = sel.WhereAnd<Where<APDocumentResult.openDoc, Equal<True>>>();
				}
				if (summarize)
				{
					var types = new List<Type>(BqlCommand.Decompose(sel.GetSelectType()));
					types[0] = typeof(Select4<,,>);
					var aggregate = typeof(Aggregate<
						GroupBy<APDocumentResult.released,
						Sum<APDocumentResult.curyOrigDocAmt, Sum<APDocumentResult.origDocAmt,
						Sum<APDocumentResult.curyDocBal, Sum<APDocumentResult.docBal,
						Sum<APDocumentResult.curyRetainageUnreleasedAmt, Sum<APDocumentResult.retainageUnreleasedAmt
						>>>>>>>>);
					types.Insert(types.Count - 5, aggregate);
					types.RemoveRange(types.Count - 5, 5);
					sel = BqlCommand.CreateInstance(BqlCommand.Compose(types.ToArray()));
				}
			}
			else
			{
				if (header.UseMasterCalendar == true)
				{
					sel = sel.WhereAnd<Where<APDocumentResult.tranPeriodID, LessEqual<Current<APDocumentFilter.finPeriodID>>>>();
					sel = sel.WhereAnd<Where<APDocumentResult.tranPostPeriodID, IsNull,
						Or<APDocumentResult.tranPostPeriodID, LessEqual<Current<APDocumentFilter.finPeriodID>>>>>();
				}
				else
				{
					sel = sel.WhereAnd<Where<APDocumentResult.finPeriodID, LessEqual<Current<APDocumentFilter.finPeriodID>>>>();
					sel = sel.WhereAnd<Where<APDocumentResult.finPostPeriodID, IsNull,
						Or<APDocumentResult.finPostPeriodID, LessEqual<Current<APDocumentFilter.finPeriodID>>>>>();
				}
				queryType = typeof(APDocumentPeriodResult);

				var aggregate = summarize
					?typeof(Aggregate<
						GroupBy<APDocumentPeriodResult.released,
						Sum<APDocumentPeriodResult.curyDocBal,
						Sum<APDocumentPeriodResult.docBal,
						Sum<APDocumentPeriodResult.curyRetainageUnreleasedAmt,
						Sum<APDocumentPeriodResult.retainageUnreleasedAmt>>>>>>)
					:typeof(Aggregate<
						GroupBy<APDocumentPeriodResult.docType,
						GroupBy<APDocumentPeriodResult.refNbr,
						Sum<APDocumentPeriodResult.curyBegBalance,
						Sum<APDocumentPeriodResult.begBalance,
						Sum<APDocumentPeriodResult.curyDocBal,
						Sum<APDocumentPeriodResult.docBal,
						Sum<APDocumentPeriodResult.curyRetainageUnreleasedAmt,
						Sum<APDocumentPeriodResult.retainageUnreleasedAmt,
						Sum<APDocumentPeriodResult.curyDiscActTaken,
						Sum<APDocumentPeriodResult.discActTaken,
						Sum<APDocumentPeriodResult.curyTaxWheld,
						Sum<APDocumentPeriodResult.taxWheld,
						Sum<APDocumentPeriodResult.rGOLAmt,
						Sum<APDocumentPeriodResult.aPTurnover>>>>>>>>>>>>>>,
						Having<APDocumentPeriodResult.begBalance.Summarized.IsNotEqual<Zero>
							.Or<APDocumentPeriodResult.docBal.Summarized.IsNotEqual<Zero>>
							.Or<APDocumentPeriodResult.retainageUnreleasedAmt.Summarized.IsNotEqual<Zero>>
							.Or<APDocumentPeriodResult.turn.Summarized.IsNotEqual<Zero>>
							.Or<APDocumentPeriodResult.retainageUnreleasedAmt.Summarized.IsNotEqual<Zero>>
							.Or<APDocumentPeriodResult.released.Maximized.IsEqual<False>>>>);

				if (header.IncludeGLTurnover == true && !summarize)
				{
					glTurn = SelectGLTurn();
					queryType = typeof(GLDocumentPeriodResult);
					aggregate = typeof(Aggregate<
						GroupBy<GLDocumentPeriodResult.docType,
						GroupBy<GLDocumentPeriodResult.refNbr,
						Sum<GLDocumentPeriodResult.aPTurnover>>>>);
				}

				var types = new List<Type>(BqlCommand.Decompose(sel.GetSelectType()));
				for(int i=0; i<types.Count; i++)
				{
					Type t = types[i];
					if (t == typeof(APDocumentResult))
						types[i] = queryType;
					else if (t.DeclaringType == typeof(APDocumentResult))
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
					types[0] = !SubCDUtils.IsSubCDEmpty(header.SubCD)
						?typeof(Select5<,,,>)
						:typeof(Select4<,,>);
					types.RemoveRange(types.Count - 5, 5);
				}
				sel = BqlCommand.CreateInstance(BqlCommand.Compose(types.ToArray()));
			}

			var mapper = new PXResultMapper(this,
				new Dictionary<Type, Type> {[typeof(APDocumentResult)] = queryType},
				typeof(APDocumentResult));
			if(byPeriod)
				mapper.ExtFilters.Add(
					new Type[]{
						typeof(APDocumentResult.curyBegBalance),
						typeof(APDocumentResult.begBalance),
						typeof(APDocumentResult.curyDocBal),
						typeof(APDocumentResult.docBal),
						typeof(APDocumentResult.curyRetainageUnreleasedAmt),
						typeof(APDocumentResult.retainageUnreleasedAmt),
						typeof(APDocumentResult.curyDiscActTaken),
						typeof(APDocumentResult.discActTaken),
						typeof(APDocumentResult.curyTaxWheld),
						typeof(APDocumentResult.taxWheld),
						typeof(APDocumentResult.rGOLAmt),
					});

			int startRow = PXView.StartRow;
			int totalRows = 0;
			PXDelegateResult list = mapper.CreateDelegateResult(!(summarize || glTurn != null));

			PXView documentView = new PXView(this, true, sel);
			using (new PXFieldScope(documentView, restrictedFields))
				foreach (object row in summarize || glTurn != null
					? documentView.SelectMulti()
					: documentView.Select(null, null,
						PXView.Searches, mapper.SortColumns, PXView.Descendings, mapper.Filters,
						ref startRow, PXView.MaximumRows, ref totalRows))
				{
					object rec = row is PXResult result
						? result[0]
						: row;
					if (rec is APDocumentResult)
						list.Add(rec);
					else
					{
						APDocumentResult item = (APDocumentResult) Documents.Cache.CreateInstance();
						foreach (string field in Documents.Cache.Fields)
						{
							Documents.Cache.SetValue(item, field, documentView.Cache.GetValue(rec, field));
						}
						item.GLTurnover = 0m;
						if (glTurn != null &&
						    glTurn.TryGetValue(new Tuple<string, string>(item.DocType, item.RefNbr), out var turn))
							item.GLTurnover = turn ?? 0m;


						list.Add(item);
					}
				}

			return list;
		}

		protected virtual Dictionary<Tuple<string,string>, decimal?> SelectGLTurn()
		{
			BqlCommand selAccounts = new PXSelectGroupBy<APTranPost,
					Where<APTranPost.accountID, IsNotNull>,
					Aggregate<GroupBy<APTranPost.accountID>>>
				(this).View.BqlSelect;

			BqlCommand selectGLTurn = new PXSelect<GLTran,
					Where<GLTran.module.IsIn<BatchModule.moduleGL, BatchModule.moduleAP>.
						And<GLTran.branchID.IsEqual<APDocumentFilter.branchID.FromCurrent>>.
						And<GLTran.referenceID.IsEqual<APDocumentFilter.vendorID.FromCurrent>>.
						And<GLTran.posted.IsEqual<True>>>>
				(this).View.BqlSelect;

			if (this.Filter.Current.UseMasterCalendar == true)
			{
				selectGLTurn = selectGLTurn.WhereAnd<Where<GLTran.tranPeriodID, Equal<Current<APDocumentFilter.finPeriodID>>>>();
			}
			else
			{
				selectGLTurn = selectGLTurn.WhereAnd<Where<GLTran.finPeriodID, Equal<Current<APDocumentFilter.finPeriodID>>>>();
			}

			if (this.Filter.Current.AccountID == null)
			{
				selectGLTurn = selectGLTurn.WhereAnd<Where<GLTran.accountID.
					IsInSubselect<SelectFrom<APTranPost>
						.Where<APTranPost.tranType.IsEqual<GLTran.tranType>
						.And<APTranPost.tranRefNbr.IsEqual<GLTran.refNbr>>>
						.SearchFor<APTranPost.accountID>>>>();
			}
			else
			{
				selectGLTurn = selectGLTurn.WhereAnd<Where<GLTran.accountID, Equal<Current<APDocumentFilter.accountID>>>>();
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

		protected virtual IEnumerable filter()
		{
			PXCache cache = this.Caches[typeof(APDocumentFilter)];
			if (cache.Current is APDocumentFilter filter)
			{
				if (filter.RefreshTotals == true)
				{
					filter.ClearSummary();
					foreach (APDocumentResult it in SelectDetails(true))
					{
						Aggregate(filter, it);
					}

					if (filter.VendorID != null)
					{
						APVendorBalanceEnq balanceBO = PXGraph.CreateInstance<APVendorBalanceEnq>();
						APVendorBalanceEnq.APHistoryFilter histFilter = balanceBO.Filter.Current;
						APVendorBalanceEnq.Copy(histFilter, filter);
						if (histFilter.FinPeriodID == null)
							histFilter.FinPeriodID = balanceBO.GetLastActivityPeriod(filter.VendorID);
						balanceBO.Filter.Update(histFilter);

						APVendorBalanceEnq.APHistorySummary summary = balanceBO.Summary.Select();
						SetSummary(filter, summary);
					}
				filter.RefreshTotals = false;
				}
			}
			yield return cache.Current;
			cache.IsDirty = false;
		}
		#endregion

		#region Ctor +  Overrides
		public APDocumentEnq()
		{
			APSetup setup = this.APSetup.Current;
			Company company = this.Company.Current;
			Documents.Cache.AllowDelete = false;
			Documents.Cache.AllowInsert = false;
			Documents.Cache.AllowUpdate = false;

			PXUIFieldAttribute.SetVisibility<APRegister.finPeriodID>(Documents.Cache, null, PXUIVisibility.Visible);
			PXUIFieldAttribute.SetVisibility<APRegister.vendorID>(Documents.Cache, null, PXUIVisibility.Visible);
			PXUIFieldAttribute.SetVisibility<APRegister.curyDiscBal>(Documents.Cache, null, PXUIVisibility.Visible);
			PXUIFieldAttribute.SetVisibility<APRegister.curyOrigDocAmt>(Documents.Cache, null, PXUIVisibility.Visible);
			PXUIFieldAttribute.SetVisibility<APRegister.curyDiscTaken>(Documents.Cache, null, PXUIVisibility.Visible);

			this.actionsFolder.MenuAutoOpen = true;
			this.actionsFolder.AddMenuAction(this.createInvoice);
			this.actionsFolder.AddMenuAction(this.createPayment);
			this.actionsFolder.AddMenuAction(this.payDocument);

			this.reportsFolder.MenuAutoOpen = true;
			this.reportsFolder.AddMenuAction(this.aPBalanceByVendorReport);
			this.reportsFolder.AddMenuAction(this.vendorHistoryReport);
			this.reportsFolder.AddMenuAction(this.aPAgedPastDueReport);
			this.reportsFolder.AddMenuAction(this.aPAgedOutstandingReport);
			this.reportsFolder.AddMenuAction(this.aPRegisterReport);
		}

		public override bool IsDirty => false;

		#endregion

		#region CacheAttached
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Original Document")]
		protected virtual void APDocumentResult_OrigRefNbr_CacheAttached(PXCache sender) { }
		#endregion

		#region Actions
		public PXAction<APDocumentFilter> refresh;
		public PXCancel<APDocumentFilter> Cancel;
		[Obsolete(Common.Messages.WillBeRemovedInAcumatica2019R1)]
		public PXAction<APDocumentFilter> viewDocument;
		public PXAction<APDocumentFilter> previousPeriod;
		public PXAction<APDocumentFilter> nextPeriod;

		public PXAction<APDocumentFilter> actionsFolder;
		public PXAction<APDocumentFilter> createInvoice;
		public PXAction<APDocumentFilter> createPayment;
		public PXAction<APDocumentFilter> payDocument;

		public PXAction<APDocumentFilter> reportsFolder;
		public PXAction<APDocumentFilter> aPBalanceByVendorReport;
		public PXAction<APDocumentFilter> vendorHistoryReport;
		public PXAction<APDocumentFilter> aPAgedPastDueReport;
		public PXAction<APDocumentFilter> aPAgedOutstandingReport;
		public PXAction<APDocumentFilter> aPRegisterReport;


		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = true)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.Refresh, IsLockedOnToolbar = true)]
		public IEnumerable Refresh(PXAdapter adapter)
		{
			this.Filter.Current.RefreshTotals = true;
			return adapter.Get();
		}

		[InjectDependency]
		public IFinPeriodRepository FinPeriodRepository { get; set; }

		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		[PXEditDetailButton(ImageKey = PX.Web.UI.Sprite.Main.DataEntry)]
		public virtual IEnumerable ViewDocument(PXAdapter adapter)
		{
			if (this.Documents.Current != null)
			{
				PXRedirectHelper.TryRedirect(Documents.Cache, Documents.Current, "Document", PXRedirectHelper.WindowMode.NewWindow);
			}
			return Filter.Select();
		}

		public PXAction<APDocumentFilter> viewOriginalDocument;
		[PXButton]
		public virtual IEnumerable ViewOriginalDocument(PXAdapter adapter)
		{
			if (Documents.Current != null)
			{
				APInvoiceEntry graph = PXGraph.CreateInstance<APInvoiceEntry>();
				AP.APRegister origDoc = PXSelect<AP.APRegister,
					Where<AP.APRegister.refNbr, Equal<Required<AP.APRegister.origRefNbr>>,
						And<AP.APRegister.docType, Equal<Required<AP.APRegister.origDocType>>>>>
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
			APDocumentFilter filter = Filter.Current as APDocumentFilter;

			filter.UseMasterCalendar = (filter.OrganizationID == null && filter.BranchID == null);
			int? calendarOrganizationID = FinPeriodRepository.GetCalendarOrganizationID(filter.OrganizationID, filter.BranchID, filter.UseMasterCalendar);
			FinPeriod prevPeriod = FinPeriodRepository.FindPrevPeriod(calendarOrganizationID, filter.FinPeriodID, looped: true);
			filter.FinPeriodID = prevPeriod != null ? prevPeriod.FinPeriodID : null;
			filter.RefreshTotals = true;
			return adapter.Get();
		}

		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXNextButton]
		public virtual IEnumerable NextPeriod(PXAdapter adapter)
		{
			APDocumentFilter filter = Filter.Current as APDocumentFilter;

			filter.UseMasterCalendar = (filter.OrganizationID == null && filter.BranchID == null);
			int? calendarOrganizationID = FinPeriodRepository.GetCalendarOrganizationID(filter.OrganizationID, filter.BranchID, filter.UseMasterCalendar);
			FinPeriod nextPeriod = FinPeriodRepository.FindNextPeriod(calendarOrganizationID, filter.FinPeriodID, looped: true);
			filter.FinPeriodID = nextPeriod != null ? nextPeriod.FinPeriodID : null;
			filter.RefreshTotals = true;
			return adapter.Get();

		}

		[PXUIField(DisplayName = "Actions", MapEnableRights = PXCacheRights.Select)]
		[PXButton(SpecialType = PXSpecialButtonType.ActionsFolder)]
		protected virtual IEnumerable Actionsfolder(PXAdapter adapter)
		{
			return adapter.Get();
		}

		[PXUIField(DisplayName = Messages.NewInvoice, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(Category = ActionCategories.DocumentProcessing)]
		public virtual IEnumerable CreateInvoice(PXAdapter adapter)
		{
			if (Filter.Current?.VendorID != null)
			{
				VendorMaint graph = PXGraph.CreateInstance<VendorMaint>();
				graph.BAccount.Current = graph.BAccount.Search<BAccount.bAccountID>(this.Filter.Current.VendorID);
				graph.newBillAdjustment.Press();
			}

			return Filter.Select();
		}

		[PXUIField(DisplayName = Messages.NewPayment, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(Category = ActionCategories.DocumentProcessing)]
		public virtual IEnumerable CreatePayment(PXAdapter adapter)
		{
			if (Filter.Current?.VendorID != null)
			{
				VendorMaint graph = PXGraph.CreateInstance<VendorMaint>();
				graph.BAccount.Current = graph.BAccount.Search<BAccount.bAccountID>(this.Filter.Current.VendorID);
				graph.newManualCheck.Press();
			}

			return Filter.Select();
		}

		[PXUIField(DisplayName = Messages.APPayBill, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(Category = ActionCategories.DocumentProcessing)]
		public virtual IEnumerable PayDocument(PXAdapter adapter)
		{
			if (Documents.Current != null)
			{
				if (this.Documents.Current.Status != APDocStatus.Open)
					throw new PXException(Messages.Only_Open_Documents_MayBe_Processed);

				APInvoice doc = this.FindInvoice(Documents.Current);
				if (doc != null)
				{
					if (APDocType.Payable(this.Documents.Current.DocType) == true)
					{
						APPaymentEntry graph = PXGraph.CreateInstance<APPaymentEntry>();
						graph.Clear();
						try
						{
							graph.CreatePayment(doc);
						}
						catch (AdjustedNotFoundException)
						{
							APAdjust foundAPAdjust = PXSelect<APAdjust,
								Where<APAdjust.adjdDocType, Equal<Required<APAdjust.adjdDocType>>,
									And<APAdjust.adjdRefNbr, Equal<Required<APAdjust.adjdRefNbr>>,
										And<APAdjust.released, Equal<False>>>>>.Select(graph, doc.DocType, doc.RefNbr);
							if (foundAPAdjust != null)
							{
								graph.Clear();
								graph.Document.Current =
									graph.Document.Search<APPayment.refNbr>(foundAPAdjust.AdjgRefNbr,
										foundAPAdjust.AdjgDocType);
								throw new PXRedirectRequiredException(graph, "PayInvoice");
							}
						}

						throw new PXRedirectRequiredException(graph, "PayInvoice");
					}
					else
					{
						APPaymentEntry graph = PXGraph.CreateInstance<APPaymentEntry>();
						APPayment payment =
							graph.Document.Search<APPayment.refNbr>(Documents.Current.RefNbr,Documents.Current.DocType);
						if (payment != null)
						{
							graph.Document.Current = payment;
							throw new PXRedirectRequiredException(graph, AP.Messages.ViewDocument);
						}
					}
				}
			}
			return Filter.Select();
		}

		[PXUIField(DisplayName = "Reports", MapEnableRights = PXCacheRights.Select)]
		[PXButton(SpecialType = PXSpecialButtonType.ReportsFolder)]
		protected virtual IEnumerable Reportsfolder(PXAdapter adapter)
		{
			return adapter.Get();
		}

		[PXUIField(DisplayName = Messages.APBalanceByVendorReport, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable APBalanceByVendorReport(PXAdapter adapter)
		{
			APDocumentFilter filter = Filter.Current;

			if (filter != null)
			{
				Dictionary<string, string> parameters = GetBasicReportParameters(filter);
				if (!string.IsNullOrEmpty(filter.FinPeriodID))
				{
					parameters["PeriodID"] = FinPeriodIDFormattingAttribute.FormatForDisplay(filter.FinPeriodID);
				}
				parameters["UseMasterCalendar"] = filter.UseMasterCalendar == true ? true.ToString() : false.ToString();
				throw new PXReportRequiredException(parameters, "AP632500", Messages.APBalanceByVendorReport);
			}
			return adapter.Get();
		}

		[PXUIField(DisplayName = Messages.VendorHistoryReport, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable VendorHistoryReport(PXAdapter adapter)
		{
			APDocumentFilter filter = Filter.Current;
			if (filter != null)
			{
				Dictionary<string, string> parameters = GetBasicReportParameters(filter);
				if (!string.IsNullOrEmpty(filter.FinPeriodID))
				{
					parameters["FromPeriodID"] = FinPeriodIDFormattingAttribute.FormatForDisplay(filter.FinPeriodID);
					parameters["ToPeriodID"] = FinPeriodIDFormattingAttribute.FormatForDisplay(filter.FinPeriodID);
				}
				throw new PXReportRequiredException(parameters, "AP652000", Messages.VendorHistoryReport);
			}
			return adapter.Get();
		}

		[PXUIField(DisplayName = Messages.APAgedPastDueReport, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable APAgedPastDueReport(PXAdapter adapter)
		{
			APDocumentFilter filter = Filter.Current;
			if (filter != null)
			{
				Dictionary<string, string> parameters = GetBasicReportParameters(filter);
				throw new PXReportRequiredException(parameters, "AP631000", Messages.APAgedPastDueReport);
			}
			return adapter.Get();
		}

		[PXUIField(DisplayName = Messages.APAgedOutstandingReport, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable APAgedOutstandingReport(PXAdapter adapter)
		{
			APDocumentFilter filter = Filter.Current;
			if (filter != null)
			{
				Dictionary<string, string> parameters = GetBasicReportParameters(filter);
				throw new PXReportRequiredException(parameters, "AP631500", Messages.APAgedOutstandingReport);
			}
			return adapter.Get();
		}

		[PXUIField(DisplayName = Messages.APRegisterReport, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable APRegisterReport(PXAdapter adapter)
		{
			APDocumentFilter filter = Filter.Current;
			if (filter != null)
			{
				Dictionary<string, string> parameters = GetBasicReportParameters(filter);
				if (!string.IsNullOrEmpty(filter.FinPeriodID))
				{
					parameters["StartPeriodID"] = FinPeriodIDFormattingAttribute.FormatForDisplay(filter.FinPeriodID);
					parameters["EndPeriodID"] = FinPeriodIDFormattingAttribute.FormatForDisplay(filter.FinPeriodID);
				}
				throw new PXReportRequiredException(parameters, "AP621500", Messages.APRegisterReport);
			}
			return adapter.Get();
		}

		private Dictionary<string, string> GetBasicReportParameters(APDocumentFilter filter)
		{
			BAccountR bAccount = SelectFrom<BAccountR>
				.Where<BAccountR.bAccountID.IsEqual<@P.AsInt>>
				.View
				.Select(this, filter.OrgBAccountID);

			return new Dictionary<string, string>
			{
				{ "VendorID" , VendorMaint.FindByID(this, filter.VendorID)?.AcctCD },
				{ "OrgBAccountID", bAccount?.AcctCD },
			};
		}

		#endregion

		#region Events Handlers
		public virtual void APDocumentFilter_AccountID_ExceptionHandling(PXCache cache, PXExceptionHandlingEventArgs e)
		{
			APDocumentFilter header = e.Row as APDocumentFilter;
			if (header != null)
			{
				e.Cancel = true;
				header.AccountID = null;
			}
		}
		public virtual void APDocumentFilter_CuryID_ExceptionHandling(PXCache cache, PXExceptionHandlingEventArgs e)
		{
			APDocumentFilter header = e.Row as APDocumentFilter;
			if (header != null)
			{
				e.Cancel = true;
				header.CuryID = null;
			}
		}
		public virtual void APDocumentFilter_SubID_FieldVerifying(PXCache cache, PXFieldVerifyingEventArgs e)
		{
			e.Cancel = true;
		}

		public virtual void APDocumentFilter_RowUpdated(PXCache cache, PXRowUpdatedEventArgs e)
		{
			if(cache.ObjectsEqual<APDocumentFilter.organizationID>(e.Row, e.OldRow) &&
			   cache.ObjectsEqual<APDocumentFilter.orgBAccountID>(e.Row, e.OldRow) &&
			   cache.ObjectsEqual<APDocumentFilter.branchID>(e.Row, e.OldRow) &&
			   cache.ObjectsEqual<APDocumentFilter.vendorID>(e.Row, e.OldRow) &&
			   cache.ObjectsEqual<APDocumentFilter.finPeriodID>(e.Row, e.OldRow) &&
			   cache.ObjectsEqual<APDocumentFilter.masterFinPeriodID>(e.Row, e.OldRow) &&
			   cache.ObjectsEqual<APDocumentFilter.showAllDocs>(e.Row, e.OldRow) &&
			   cache.ObjectsEqual<APDocumentFilter.includeUnreleased>(e.Row, e.OldRow) &&
			   cache.ObjectsEqual<APDocumentFilter.accountID>(e.Row, e.OldRow) &&
			   cache.ObjectsEqual<APDocumentFilter.subCD>(e.Row, e.OldRow) &&
			   cache.ObjectsEqual<APDocumentFilter.subCDWildcard>(e.Row, e.OldRow) &&
			   cache.ObjectsEqual<APDocumentFilter.docType>(e.Row, e.OldRow) &&
			   cache.ObjectsEqual<APDocumentFilter.curyID>(e.Row, e.OldRow))
			{
				return;
			}
			(e.Row as APDocumentFilter).RefreshTotals = true;
		}
		public virtual void APDocumentFilter_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
		{
			if (e.Row == null) return;

			APDocumentFilter row = (APDocumentFilter)e.Row;
			bool byPeriod   = (row.FinPeriodID != null);
			PXCache docCache = Documents.Cache;
			bool isMCFeatureInstalled = PXAccess.FeatureInstalled<FeaturesSet.multicurrency>();
			bool isForeignCurrencySelected = string.IsNullOrEmpty(row.CuryID) == false && (row.CuryID != this.Company.Current.BaseCuryID);
			bool isBaseCurrencySelected = string.IsNullOrEmpty(row.CuryID) == false && (row.CuryID == this.Company.Current.BaseCuryID);

			PXUIFieldAttribute.SetVisible<APDocumentFilter.showAllDocs>(cache, row, !byPeriod);
			PXUIFieldAttribute.SetVisible<APDocumentFilter.curyID>(cache, row, isMCFeatureInstalled);
			PXUIFieldAttribute.SetVisible<APDocumentFilter.curyBalanceSummary>(cache, row, isMCFeatureInstalled && isForeignCurrencySelected);
			PXUIFieldAttribute.SetVisible<APDocumentFilter.curyDifference>(cache, row, isMCFeatureInstalled && isForeignCurrencySelected);
			PXUIFieldAttribute.SetVisible<APDocumentFilter.curyVendorBalance>(cache, row, isMCFeatureInstalled && isForeignCurrencySelected);
			PXUIFieldAttribute.SetVisible<APDocumentFilter.curyVendorRetainedBalance>(cache, row, isMCFeatureInstalled && isForeignCurrencySelected);
			PXUIFieldAttribute.SetVisible<APDocumentFilter.curyVendorDepositsBalance>(cache, row, isMCFeatureInstalled && isForeignCurrencySelected);

			PXUIFieldAttribute.SetVisible<APDocumentResult.curyID>(docCache, null, isMCFeatureInstalled);
			PXUIFieldAttribute.SetVisible<APDocumentResult.rGOLAmt>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<APDocumentResult.curyBegBalance>(docCache, null, byPeriod && isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<APDocumentResult.begBalance>(docCache, null, byPeriod);
			PXUIFieldAttribute.SetVisible<APDocumentResult.curyOrigDocAmt>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<APDocumentResult.curyDocBal>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<APDocumentResult.curyDiscActTaken>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<APDocumentResult.curyTaxWheld>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<APDocumentResult.curyRetainageTotal>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<APDocumentResult.curyOrigDocAmtWithRetainageTotal>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<APDocumentResult.curyRetainageUnreleasedAmt>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);

			//PXUIFieldAttribute.SetEnabled<APDocumentResult.status>(docCache, null, true);//???
			//PXUIFieldAttribute.SetEnabled<APDocumentResult.curyDocBal>(docCache, null, true);//???

			bool multipleBaseCurrenciesInstalled = PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
			PXUIFieldAttribute.SetRequired<APDocumentFilter.orgBAccountID>(cache, multipleBaseCurrenciesInstalled);
			bool enableReports = multipleBaseCurrenciesInstalled ? row.VendorID != null && row.OrgBAccountID != null : row.VendorID != null;
			aPBalanceByVendorReport.SetEnabled(enableReports);
			vendorHistoryReport.SetEnabled(enableReports);
			aPAgedPastDueReport.SetEnabled(enableReports);
			aPAgedOutstandingReport.SetEnabled(enableReports);
			aPRegisterReport.SetEnabled(enableReports);
		}
		#endregion

		#region Utility Functions - internal
		protected virtual void SetSummary(APDocumentFilter aDest, APVendorBalanceEnq.APHistorySummary aSrc)
		{
			aDest.VendorBalance = aSrc.BalanceSummary;
			aDest.VendorDepositsBalance = aSrc.DepositsSummary;
			aDest.CuryVendorBalance = aSrc.CuryBalanceSummary;
			aDest.CuryVendorDepositsBalance = aSrc.CuryDepositsSummary;
		}

		protected virtual void Aggregate(APDocumentFilter aDest, APDocumentResult aSrc)
		{
			aDest.BalanceSummary += aSrc.DocBal ?? decimal.Zero;
			aDest.CuryBalanceSummary += aSrc.CuryDocBal ?? decimal.Zero;
			aDest.VendorRetainedBalance += aSrc.RetainageUnreleasedAmt ?? decimal.Zero;
			aDest.CuryVendorRetainedBalance += aSrc.CuryRetainageUnreleasedAmt ?? decimal.Zero;
		}

		protected virtual APInvoice FindInvoice(APDocumentResult aRes)
		{
			APInvoice doc = PXSelect<APInvoice,
					Where<APInvoice.docType, Equal<Required<APInvoice.docType>>,
						And<APInvoice.refNbr, Equal<Required<APInvoice.refNbr>>>>>
				.Select(this, aRes.DocType, aRes.RefNbr);
			return doc;
		}
		protected virtual APPayment FindPayment(APDocumentResult aRes)
		{
			APPayment doc = PXSelect<APPayment,
					Where<APPayment.docType, Equal<Required<APPayment.docType>>,
						And<APPayment.refNbr, Equal<Required<APPayment.refNbr>>>>>
				.Select(this, aRes.DocType, aRes.RefNbr);
			return doc;
		}


		#endregion

	}
}
