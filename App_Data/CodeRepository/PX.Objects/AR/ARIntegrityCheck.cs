using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using PX.Data;
using PX.Objects.GL;
using PX.Objects.CM;
using PX.Objects.AR.Overrides.ARDocumentRelease;
using PX.Objects.Common.Extensions;
using PX.Objects.GL.FinPeriods;
using PX.Objects.GL.FinPeriods.TableDefinition;

namespace PX.Objects.AR
{
	[Serializable]
	public partial class ARIntegrityCheckFilter : PX.Data.IBqlTable
	{
		#region CustomerClassID
		public abstract class customerClassID : PX.Data.BQL.BqlString.Field<customerClassID> { }
		protected String _CustomerClassID;
		[PXDBString(10, IsUnicode = true)]
		[PXSelector(typeof(CustomerClass.customerClassID), DescriptionField = typeof(CustomerClass.descr), CacheGlobal = true)]
		[PXUIField(DisplayName = "Customer Class")]
		public virtual String CustomerClassID
		{
			get
			{
				return this._CustomerClassID;
			}
			set
			{
				this._CustomerClassID = value;
			}
		}
		#endregion
		#region FinPeriodID
		public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
		protected String _FinPeriodID;
		[FinPeriodNonLockedSelector]
		[PXUIField(DisplayName = "Fin. Period")]
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
		#region RecalcDocumentBalances
		public abstract class recalcDocumentBalances : PX.Data.BQL.BqlBool.Field<recalcDocumentBalances> { }
		[PXDBBool()]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Document Balances")]
		public virtual bool? RecalcDocumentBalances { get; set; }
		#endregion
		#region RecalcCustomerBalancesReleased
		public abstract class recalcCustomerBalancesReleased : PX.Data.BQL.BqlBool.Field<recalcCustomerBalancesReleased> { }
		[PXDBBool()]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Customer Balances by Released Document")]
		public virtual bool? RecalcCustomerBalancesReleased { get; set; }
		#endregion
		#region RecalcCustomerBalancesUnreleased
		public abstract class recalcCustomerBalancesUnreleased : PX.Data.BQL.BqlBool.Field<recalcCustomerBalancesUnreleased> { }
		[PXDBBool()]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Customer Balances by Unreleased Document")]
		public virtual bool? RecalcCustomerBalancesUnreleased { get; set; }
		#endregion
	}

	[TableAndChartDashboardType]
	public class ARIntegrityCheck : PXGraph<ARIntegrityCheck>
	{
		public const string RECALCULATE_CUSTOMER_BALANCES_SCREEN_ID = "AR.50.99.00";

		public PXCancel<ARIntegrityCheckFilter> Cancel;
		public PXFilter<ARIntegrityCheckFilter> Filter;
		public PXSetup<ARSetup> ARSetup;

		[PXFilterable]
		[PX.SM.PXViewDetailsButton(typeof(Customer.acctCD), WindowMode = PXRedirectHelper.WindowMode.NewWindow)]
		public PXFilteredProcessing<Customer, ARIntegrityCheckFilter,
			Where<Match<Current<AccessInfo.userName>>>> ARCustomerList;

		public PXSelect<Customer,
			Where<Customer.customerClassID, Equal<Current<ARIntegrityCheckFilter.customerClassID>>,
			And<Match<Current<AccessInfo.userName>>>>> Customer_ClassID;

		public PXSelect<Customer,
			Where<Match<Current<AccessInfo.userName>>>> Customers;


		protected virtual IEnumerable arcustomerlist()
		{

			if (Filter.Current != null && Filter.Current.CustomerClassID != null)
			{
				using (new PXFieldScope(Customer_ClassID.View,
					typeof(Customer.bAccountID),
					typeof(Customer.acctCD),
					typeof(Customer.customerClassID)))
					return Customer_ClassID.SelectDelegateResult();
			}
			else
			{
				using (new PXFieldScope(Customers.View,
					typeof(Customer.bAccountID),
					typeof(Customer.acctCD),
					typeof(Customer.customerClassID)))
					return Customers.SelectDelegateResult();
			}

		}

		[InjectDependency]
		private ICurrentUserInformationProvider _currentUserInformationProvider { get; set; }

		public ARIntegrityCheck()
		{
			ARSetup setup = ARSetup.Current;

			ARCustomerList.SetProcessTooltip(Messages.RecalculateBalanceTooltip);
			ARCustomerList.SetProcessAllTooltip(Messages.RecalculateBalanceTooltip);
		}

		protected virtual void _(Events.RowSelected<ARIntegrityCheckFilter> e)
		{
			ARIntegrityCheckFilter filter = Filter.Current;

			bool errorsOnForm = PXUIFieldAttribute.GetErrors(e.Cache, null, PXErrorLevel.Error, PXErrorLevel.RowError).Count > 0;

			PXUIFieldAttribute.SetRequired<ARIntegrityCheckFilter.finPeriodID>(e.Cache, filter.RecalcDocumentBalances == true || filter.RecalcCustomerBalancesReleased == true);

			ARCustomerList.SetProcessEnabled(!errorsOnForm);
			ARCustomerList.SetProcessAllEnabled(!errorsOnForm);
			ARCustomerList.SuppressMerge = true;
			ARCustomerList.SuppressUpdate = true;

			if (this.Accessinfo.ScreenID == RECALCULATE_CUSTOMER_BALANCES_SCREEN_ID)
			{
				ARCustomerList.SetProcessDelegate(
					delegate (ARIntegrityCheck integrityCheckGraph, Customer cust)
					{
						integrityCheckGraph.Clear(PXClearOption.PreserveTimeStamp);
						integrityCheckGraph.IntegrityCheckProc(cust, filter);
					}
				);

				ARCustomerList.SetParametersDelegate(delegate (List<Customer> list)
				{
					if (
						filter.RecalcDocumentBalances != true &&
						filter.RecalcCustomerBalancesReleased != true &&
						filter.RecalcCustomerBalancesUnreleased != true)
					{
						throw new PXException(Messages.SelectBalancesToBeRecalculated);
					}
					return true;
				});
			}
			else
			{
				ARCustomerList.SetProcessDelegate<ARReleaseProcess>(
					delegate (ARReleaseProcess arReleaseGraph, Customer cust)
					{
						arReleaseGraph.Clear(PXClearOption.PreserveTimeStamp);
						arReleaseGraph.IntegrityCheckProc(cust, filter.FinPeriodID);
					}
				);

				//For perfomance recomended select not more than maxCustomerCount customers, 
				//becouse the operation is performed for a long time.
				const int maxCustomerCount = 5;
				ARCustomerList.SetParametersDelegate(delegate (List<Customer> list)
				{
					bool processing = true;
					if (PX.Common.PXContext.GetSlot<PX.SM.AUSchedule>() == null && list.Count > maxCustomerCount)
					{
						WebDialogResult wdr = ARCustomerList.Ask(Messages.ContinueValidatingBalancesForMultipleCustomers, MessageButtons.OKCancel);
						processing = (wdr == WebDialogResult.OK);
						if(!processing)
						throw new PXException(Messages.SelectBalancesToBeRecalculated);
					}
					return processing;
				});
			}
		}

		protected virtual void _(Events.RowUpdated<ARIntegrityCheckFilter> e)
		{
			if (e.Row == null) return;

			object finPeriod = e.Row.FinPeriodID;
			object pendingFinPeriod = e.Cache.GetValuePending<ARIntegrityCheckFilter.finPeriodID>(e.Row);

			if (e.Row.RecalcDocumentBalances == false && e.Row.RecalcCustomerBalancesReleased == false)
			{
				var state = e.Cache.GetStateExt<ARIntegrityCheckFilter.finPeriodID>(e.Row) as PXFieldState;

				if (state != null && state.ErrorLevel >= PXErrorLevel.Error && finPeriod != null && pendingFinPeriod == null)
				{
					e.Row.FinPeriodID = null;
					e.Cache.RaiseExceptionHandling<ARIntegrityCheckFilter.finPeriodID>(e.Row, null, null);
				}

				if (e.Row.RecalcCustomerBalancesUnreleased == false)
				{
					e.Cache.RaiseExceptionHandling<ARIntegrityCheckFilter.recalcDocumentBalances>(e.Row, false, new PXSetPropertyException(Messages.SelectBalancesToBeRecalculated));
				}
			}
			else
			{
				try
				{
					e.Cache.RaiseFieldVerifying<ARIntegrityCheckFilter.finPeriodID>(e.Row, ref finPeriod);
				}
				catch (PXSetPropertyException ex)
				{
					e.Cache.RaiseExceptionHandling<ARIntegrityCheckFilter.finPeriodID>(e.Row, e.Row.FinPeriodID, ex);
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<ARIntegrityCheckFilter.recalcDocumentBalances> e)
		{
			if (e.Row != null && e.NewValue != null && (bool)e.NewValue == true)
			{
				((ARIntegrityCheckFilter)e.Row).RecalcCustomerBalancesReleased = true;
			}
		}

		protected virtual void _(Events.FieldVerifying<ARIntegrityCheckFilter.finPeriodID> e)
		{
			if (e.Row == null) return;

			var filter = (ARIntegrityCheckFilter)e.Row;
			if (filter.RecalcDocumentBalances == false && filter.RecalcCustomerBalancesReleased == false)
			{
				e.Cancel = true;
			}
		}

		protected virtual void IntegrityCheckProc(Customer cust, ARIntegrityCheckFilter filter)
		{
			var finPeriod = GetMinPeriod(cust, filter.FinPeriodID);

			if (filter.RecalcDocumentBalances == true)
			{
				UpdateDocumentBalances(cust, finPeriod);
				ReopenRetainageDocuments(cust, finPeriod);
				UpdateARTranRetainageBalance(cust, finPeriod);
				UpdateARTranBalances(cust, finPeriod);
				ReopenPaymentsByLinesDocuments(cust, finPeriod);
				ReopenCreditMemoes(cust, finPeriod);
			}

			if (filter.RecalcCustomerBalancesReleased == true)
			{
				FillMissedARHistory(cust, finPeriod);
				FillMissedCuryARHistory(cust, finPeriod);
				FixPtdARHistory(cust, finPeriod);
				FixPtdCuryARHistory(cust, finPeriod);
				FixYtdARHistory(cust, finPeriod);
				FixYtdCuryARHistory(cust, finPeriod);
				UpdateARBalancesCurrent(cust);
			}

			if (filter.RecalcCustomerBalancesUnreleased == true)
			{
				UpdateARBalancesUnreleased(cust);
				UpdateARBalancesOpenOrders(cust);
			}
		}

		protected virtual string GetMinPeriod(Customer cust, string startPeriod)
		{
			string minPeriod = "190001";

			ARHistoryDetDeleted maxHist = (ARHistoryDetDeleted)
						PXSelectGroupBy<ARHistoryDetDeleted,
							Where<ARHistoryDetDeleted.customerID, Equal<Required<Customer.bAccountID>>>,
						Aggregate<Max<ARHistoryDetDeleted.finPeriodID>>>
						.Select(this, cust.BAccountID);

			var releaseProcGraph = PXGraph.CreateInstance<ARReleaseProcess>();

			if (maxHist != null && maxHist.FinPeriodID != null)
			{
				minPeriod = releaseProcGraph.FinPeriodRepository.GetOffsetPeriodId(maxHist.FinPeriodID, 1, GL.FinPeriods.TableDefinition.FinPeriod.organizationID.MasterValue);
			}

			if (string.IsNullOrEmpty(startPeriod) == false && string.Compare(startPeriod, minPeriod) > 0)
			{
				minPeriod = startPeriod;
			}

			return minPeriod;
		}

		protected virtual void UpdateDocumentBalances(Customer cust, string finPeriod)
		{
			var attr = new PXDBLastChangeDateTimeAttribute(typeof(ARRegister.lastModifiedDateTime));
			DateTime lastModified = attr.GetDate();
			Guid userID = _currentUserInformationProvider.GetUserIdOrDefault();
			string screenID = Accessinfo.ScreenID?.Replace(".", "") ?? "00000000";

			PXUpdateJoin<
					Set<ARRegister.docBal,
						IIf<Where<ARRegisterTranPostGLGrouped.calcCuryBalance, Equal<Zero>>,
							Zero, ARRegisterTranPostGLGrouped.calcBalance>,

					Set<ARRegister.curyDocBal,
						ARRegisterTranPostGLGrouped.calcCuryBalance,

					Set<ARRegister.openDoc,
						IIf<Where<ARRegisterTranPostGLGrouped.calcBalance, Equal<Zero>>,
							False, True>,

					Set<ARRegister.rGOLAmt,
						ARRegisterTranPostGLGrouped.calcRGOL,

					Set<ARRegister.curyRetainageReleased,
						ARRegisterTranPostGLGrouped.calcCuryRetainageReleased,
					Set<ARRegister.retainageReleased,
						ARRegisterTranPostGLGrouped.calcRetainageReleased,
					Set<ARRegister.curyRetainageUnreleasedAmt,
						ARRegisterTranPostGLGrouped.calcCuryRetainageUnreleased,
					Set<ARRegister.retainageUnreleasedAmt,
						ARRegisterTranPostGLGrouped.calcRetainageUnreleased,
					Set<ARRegister.status,
						Switch<
							Case<Where<ARRegisterTranPostGLGrouped.calcBalance, Equal<Zero>, And<ARRegisterTranPostGLGrouped.voided, Equal<True>>>,
								ARDocStatus.voided,
							Case<Where<ARRegisterTranPostGLGrouped.canceled, Equal<True>>,
									ARDocStatus.canceled,	
							Case<Where<ARRegisterTranPostGLGrouped.calcBalance, Equal<Zero>>,
								ARDocStatus.closed,
							Case<Where<ARRegisterTranPostGLGrouped.hold, Equal<True>>,
								ARDocStatus.reserved>>>>,
								// Default
								ARDocStatus.open>,

					Set<ARRegister.closedFinPeriodID,
						IIf<Where<ARRegisterTranPostGLGrouped.calcBalance, Equal<Zero>>,
							ARRegisterTranPostGLGrouped.maxFinPeriodID, Null>,

					Set<ARRegister.closedTranPeriodID,
						IIf<Where<ARRegisterTranPostGLGrouped.calcBalance, Equal<Zero>>,
							ARRegisterTranPostGLGrouped.maxTranPeriodID, Null>,

					Set<ARRegister.closedDate,
						IIf<Where<ARRegisterTranPostGLGrouped.calcBalance, Equal<Zero>>,
							ARRegisterTranPostGLGrouped.maxDocDate, Null>,

					Set<ARRegister.lastModifiedByID, Required<ARRegister.lastModifiedByID>,
					Set<ARRegister.lastModifiedByScreenID, Required<ARRegister.lastModifiedByScreenID>,
					Set<ARRegister.lastModifiedDateTime, Required<ARRegister.lastModifiedDateTime>
					>>>>>>>>>>>>>>>,

				ARRegister,
				InnerJoin<ARRegisterTranPostGLGrouped,
					On<ARRegisterTranPostGLGrouped.docType, Equal<ARRegister.docType>,
					And<ARRegisterTranPostGLGrouped.refNbr, Equal<ARRegister.refNbr>>>>,
				Where<ARRegister.customerID, Equal<Required<ARRegister.customerID>>,
					And<ARRegister.tranPeriodID, GreaterEqual<Required<ARRegister.tranPeriodID>>>>>
				.Update(this, userID, screenID, lastModified, cust.BAccountID, finPeriod);
		}

		protected virtual void ReopenRetainageDocuments(Customer cust, string finPeriod)
		{
			PXUpdateJoin<
					Set<ARRegister.status, ARDocStatus.open,
					Set<ARRegister.openDoc, True,
					Set<ARRegister.closedFinPeriodID, Null,
					Set<ARRegister.closedTranPeriodID, Null,
					Set<ARRegister.closedDate, Null>
					>>>>,

				ARRegister,
				LeftJoin<ARRegisterReport,
					On<ARRegisterReport.isRetainageDocument, Equal<True>,
					And<ARRegisterReport.status, NotEqual<ARDocStatus.closed>,
					And<ARRegisterReport.origDocType, Equal<ARRegister.docType>,
					And<ARRegisterReport.origRefNbr, Equal<ARRegister.refNbr>>>>>>,
				Where<
					ARRegister.retainageApply, Equal<True>,
					And<ARRegister.status, Equal<ARDocStatus.closed>,
					And<ARRegister.customerID, Equal<Required<ARRegister.customerID>>,
					And<ARRegister.tranPeriodID, GreaterEqual<Required<ARRegister.tranPeriodID>>,
					And<Where<ARRegister.curyRetainageUnreleasedAmt, NotEqual<Zero>,
						Or<ARRegisterReport.status, IsNotNull>>>>>>>>
				.Update(this, cust.BAccountID, finPeriod);
		}

		protected virtual void ReopenPaymentsByLinesDocuments(Customer cust, string finPeriod)
		{
			PXUpdate<
					Set<ARRegister.status, ARDocStatus.open,
					Set<ARRegister.openDoc, True,
					Set<ARRegister.closedFinPeriodID, Null,
					Set<ARRegister.closedTranPeriodID, Null,
					Set<ARRegister.closedDate, Null>>>>>,
					ARRegister,
				Where<
					ARRegister.paymentsByLinesAllowed, Equal<True>,
					And<ARRegister.status, Equal<ARDocStatus.closed>,
					And<ARRegister.customerID, Equal<Required<ARRegister.customerID>>,
					And<ARRegister.tranPeriodID, GreaterEqual<Required<ARRegister.tranPeriodID>>,
					And<Exists<Select<ARTran,
						Where<ARTran.tranType, Equal<ARRegister.docType>,
							And<ARTran.refNbr, Equal<ARRegister.refNbr>,
							And<ARTran.curyTranBal, NotEqual<Zero>>>>>
					>>>>>>>
				.Update(this, cust.BAccountID, finPeriod);
		}

		protected virtual void UpdateARTranRetainageBalance(Customer cust, string finPeriod)
		{
			PXUpdateJoin<
					Set<ARTran.retainageBal,
						Switch<
							Case<Where<ARRegister.isRetainageReversing, Equal<True>>, CS.decimal0,
							Case<Where<ARTran.tranType.IsIn<ARDocType.refund, ARDocType.voidRefund, ARDocType.invoice, ARDocType.debitMemo, ARDocType.finCharge, ARDocType.smallCreditWO, ARDocType.cashSale>>,
								Sub<ARTran.origRetainageAmt, IsNull<ARTranRetainageReleasedTotal.retainageReleased, Zero>>>>,
							Add<ARTran.origRetainageAmt, IsNull<ARTranRetainageReleasedTotal.retainageReleased, Zero>>>,
					Set<ARTran.curyRetainageBal,
						Switch<
							Case<Where<ARRegister.isRetainageReversing, Equal<True>>, CS.decimal0,
							Case<Where<ARTran.tranType.IsIn<ARDocType.refund, ARDocType.voidRefund, ARDocType.invoice, ARDocType.debitMemo, ARDocType.finCharge, ARDocType.smallCreditWO, ARDocType.cashSale>>,
								Sub<ARTran.curyOrigRetainageAmt, IsNull<ARTranRetainageReleasedTotal.curyRetainageReleased, Zero>>>>,
							Add<ARTran.curyOrigRetainageAmt, IsNull<ARTranRetainageReleasedTotal.curyRetainageReleased, Zero>>>
					>>,
				ARTran,
				InnerJoin<ARRegister,
					On<ARRegister.docType, Equal<ARTran.tranType>,
					And<ARRegister.refNbr, Equal<ARTran.refNbr>,
					And<ARRegister.retainageApply, Equal<True>,
					And<ARRegister.paymentsByLinesAllowed, Equal<True>,
					And<ARRegister.released, Equal<True>>>>>>,
				LeftJoin<ARTranRetainageReleasedTotal,
					On<ARTranRetainageReleasedTotal.origDocType, Equal<ARTran.tranType>,
					And<ARTranRetainageReleasedTotal.origRefNbr, Equal<ARTran.refNbr>,
					And<ARTranRetainageReleasedTotal.origLineNbr, Equal<ARTran.lineNbr>>>>>>,
				Where<ARTran.customerID, Equal<Required<ARTran.customerID>>,
					And<ARTran.tranPeriodID, GreaterEqual<Required<ARTran.tranPeriodID>>,
					And<ARTran.released, Equal<True>>>>>
				.Update(this, cust.BAccountID, finPeriod);
		}

		protected virtual void UpdateARTranBalances(Customer cust, string finPeriod)
		{
			var attr = new PXDBLastChangeDateTimeAttribute(typeof(ARTran.lastModifiedDateTime));
			DateTime lastModified = attr.GetDate();
			Guid userID = _currentUserInformationProvider.GetUserIdOrDefault();
			string screenID = Accessinfo.ScreenID?.Replace(".", "") ?? "00000000";

			PXUpdateJoin<
					Set<ARTran.curyTranBal, Sub<ARTran.curyOrigTranAmt, IsNull<ARTranApplicationsTotal.curyAppBalanceTotal, Zero>>,
					Set<ARTran.tranBal, Sub<ARTran.origTranAmt, IsNull<ARTranApplicationsTotal.appBalanceTotal, Zero>>,
					Set<ARTran.lastModifiedByID, Required<ARTran.lastModifiedByID>,
					Set<ARTran.lastModifiedByScreenID, Required<ARTran.lastModifiedByScreenID>,
					Set<ARTran.lastModifiedDateTime, Required<ARTran.lastModifiedDateTime>
					>>>>>,
				ARTran,
				LeftJoin<ARTranApplicationsTotal,
					On<ARTranApplicationsTotal.tranType, Equal<ARTran.tranType>,
					And<ARTranApplicationsTotal.refNbr, Equal<ARTran.refNbr>,
					And<ARTranApplicationsTotal.lineNbr, Equal<ARTran.lineNbr>>>>>,
				Where<ARTran.customerID, Equal<Required<ARTran.customerID>>,
					And<ARTran.tranPeriodID, GreaterEqual<Required<ARTran.tranPeriodID>>,
					And<ARTran.released, Equal<True>>>>>
				.Update(this, userID, screenID, lastModified, cust.BAccountID, finPeriod);
		}

		protected virtual void ReopenCreditMemoes(Customer cust, string finPeriod)
		{
			PXUpdate<
					Set<ARRegister.status, ARDocStatus.open,
					Set<ARRegister.openDoc, True>>,

				ARRegister,
				Where<ARRegister.docType, Equal<ARDocType.creditMemo>,
					And<ARRegister.status, Equal<ARDocStatus.closed>,
					And<ARRegister.customerID, Equal<Required<ARRegister.customerID>>,
					And<ARRegister.tranPeriodID, GreaterEqual<Required<ARRegister.tranPeriodID>>,
					And<Exists<Select<ARAdjust, Where<
						ARAdjust.released, Equal<False>,
						And<ARAdjust.voided, Equal<True>,
						And<ARAdjust.adjgDocType, Equal<ARRegister.docType>,
						And<ARAdjust.adjgRefNbr, Equal<ARRegister.refNbr>>>>
						>>>>>>>>>
				.Update(this, cust.BAccountID, finPeriod);
		}

		protected virtual void FixPtdARHistory(Customer cust, string finPeriod)
		{
			PXUpdateJoin<
					Set<ARHistory.finPtdSales, ARHistoryFinGrouped.finPtdSalesSum,
					Set<ARHistory.finPtdPayments, ARHistoryFinGrouped.finPtdPaymentsSum,
					Set<ARHistory.finPtdDrAdjustments, ARHistoryFinGrouped.finPtdDrAdjustmentsSum,
					Set<ARHistory.finPtdCrAdjustments, ARHistoryFinGrouped.finPtdCrAdjustmentsSum,
					Set<ARHistory.finPtdDiscounts, ARHistoryFinGrouped.finPtdDiscountsSum,
					Set<ARHistory.finPtdRGOL, ARHistoryFinGrouped.finPtdRGOLSum,
					Set<ARHistory.finPtdFinCharges, ARHistoryFinGrouped.finPtdFinChargesSum,
					Set<ARHistory.finPtdDeposits, ARHistoryFinGrouped.finPtdDepositsSum,
					Set<ARHistory.finPtdRetainageWithheld, ARHistoryFinGrouped.finPtdRetainageWithheldSum,
					Set<ARHistory.finPtdRetainageReleased, ARHistoryFinGrouped.finPtdRetainageReleasedSum
					>>>>>>>>>>,
				ARHistory,
				LeftJoin<Branch,
					On<ARHistory.branchID, Equal<Branch.branchID>>,
				LeftJoin<FinPeriod,
					On<ARHistory.finPeriodID, Equal<FinPeriod.finPeriodID>,
					And<Branch.organizationID, Equal<FinPeriod.organizationID>>>,
				InnerJoin<ARHistoryFinGrouped,
					On<ARHistoryFinGrouped.branchID, Equal<ARHistory.branchID>,
					And<ARHistoryFinGrouped.customerID, Equal<ARHistory.customerID>,
					And<ARHistoryFinGrouped.accountID, Equal<ARHistory.accountID>,
					And<ARHistoryFinGrouped.subID, Equal<ARHistory.subID>,
					And<ARHistoryFinGrouped.finPeriodID, Equal<ARHistory.finPeriodID>>>>>>>
						>>,
				Where<ARHistory.customerID, Equal<Required<ARHistory.customerID>>,
					And<FinPeriod.masterFinPeriodID, GreaterEqual<Required<FinPeriod.finPeriodID>>>>>

				.Update(this, cust.BAccountID, finPeriod);

			PXUpdateJoin<
					Set<ARHistory.tranPtdSales, ARHistoryTranGrouped.tranPtdSalesSum,
					Set<ARHistory.tranPtdPayments, ARHistoryTranGrouped.tranPtdPaymentsSum,
					Set<ARHistory.tranPtdDrAdjustments, ARHistoryTranGrouped.tranPtdDrAdjustmentsSum,
					Set<ARHistory.tranPtdCrAdjustments, ARHistoryTranGrouped.tranPtdCrAdjustmentsSum,
					Set<ARHistory.tranPtdDiscounts, ARHistoryTranGrouped.tranPtdDiscountsSum,
					Set<ARHistory.tranPtdRGOL, ARHistoryTranGrouped.tranPtdRGOLSum,
					Set<ARHistory.tranPtdFinCharges, ARHistoryTranGrouped.tranPtdFinChargesSum,
					Set<ARHistory.tranPtdDeposits, ARHistoryTranGrouped.tranPtdDepositsSum,
					Set<ARHistory.tranPtdRetainageWithheld, ARHistoryTranGrouped.tranPtdRetainageWithheldSum,
					Set<ARHistory.tranPtdRetainageReleased, ARHistoryTranGrouped.tranPtdRetainageReleasedSum
					>>>>>>>>>>,
				ARHistory,
				InnerJoin<ARHistoryTranGrouped,
					On<ARHistoryTranGrouped.branchID, Equal<ARHistory.branchID>,
					And<ARHistoryTranGrouped.customerID, Equal<ARHistory.customerID>,
					And<ARHistoryTranGrouped.accountID, Equal<ARHistory.accountID>,
					And<ARHistoryTranGrouped.subID, Equal<ARHistory.subID>,
					And<ARHistoryTranGrouped.finPeriodID, Equal<ARHistory.finPeriodID>>>>>>>,
				Where<ARHistory.customerID, Equal<Required<ARHistory.customerID>>,
					And<ARHistory.finPeriodID, GreaterEqual<Required<ARHistory.finPeriodID>>>>>
				.Update(this, cust.BAccountID, finPeriod);
		}

		protected virtual void FixPtdCuryARHistory(Customer cust, string finPeriod)
		{
			PXUpdateJoin<
					Set<CuryARHistory.finPtdSales, CuryARHistoryFinGrouped.finPtdSalesSum,
					Set<CuryARHistory.finPtdPayments, CuryARHistoryFinGrouped.finPtdPaymentsSum,
					Set<CuryARHistory.finPtdDrAdjustments, CuryARHistoryFinGrouped.finPtdDrAdjustmentsSum,
					Set<CuryARHistory.finPtdCrAdjustments, CuryARHistoryFinGrouped.finPtdCrAdjustmentsSum,
					Set<CuryARHistory.finPtdDiscounts, CuryARHistoryFinGrouped.finPtdDiscountsSum,
					Set<CuryARHistory.finPtdRGOL, CuryARHistoryFinGrouped.finPtdRGOLSum,
					Set<CuryARHistory.finPtdFinCharges, CuryARHistoryFinGrouped.finPtdFinChargesSum,
					Set<CuryARHistory.finPtdDeposits, CuryARHistoryFinGrouped.finPtdDepositsSum,
					Set<CuryARHistory.finPtdRetainageWithheld, CuryARHistoryFinGrouped.finPtdRetainageWithheldSum,
					Set<CuryARHistory.finPtdRetainageReleased, CuryARHistoryFinGrouped.finPtdRetainageReleasedSum,
					Set<CuryARHistory.curyFinPtdSales, CuryARHistoryFinGrouped.curyFinPtdSalesSum,
					Set<CuryARHistory.curyFinPtdPayments, CuryARHistoryFinGrouped.curyFinPtdPaymentsSum,
					Set<CuryARHistory.curyFinPtdDrAdjustments, CuryARHistoryFinGrouped.curyFinPtdDrAdjustmentsSum,
					Set<CuryARHistory.curyFinPtdCrAdjustments, CuryARHistoryFinGrouped.curyFinPtdCrAdjustmentsSum,
					Set<CuryARHistory.curyFinPtdFinCharges, CuryARHistoryFinGrouped.curyFinPtdFinChargesSum,
					Set<CuryARHistory.curyFinPtdDiscounts, CuryARHistoryFinGrouped.curyFinPtdDiscountsSum,
					Set<CuryARHistory.curyFinPtdDeposits, CuryARHistoryFinGrouped.curyFinPtdDepositsSum,
					Set<CuryARHistory.curyFinPtdRetainageWithheld, CuryARHistoryFinGrouped.curyFinPtdRetainageWithheldSum,
					Set<CuryARHistory.curyFinPtdRetainageReleased, CuryARHistoryFinGrouped.curyFinPtdRetainageReleasedSum
					>>>>>>>>>>>>>>>>>>>,

				CuryARHistory,
				LeftJoin<Branch,
					On<CuryARHistory.branchID, Equal<Branch.branchID>>,
				LeftJoin<FinPeriod,
					On<CuryARHistory.finPeriodID, Equal<FinPeriod.finPeriodID>,
						And<Branch.organizationID, Equal<FinPeriod.organizationID>>>,
				InnerJoin<CuryARHistoryFinGrouped,
					On<CuryARHistoryFinGrouped.branchID, Equal<CuryARHistory.branchID>,
					And<CuryARHistoryFinGrouped.customerID, Equal<CuryARHistory.customerID>,
					And<CuryARHistoryFinGrouped.accountID, Equal<CuryARHistory.accountID>,
					And<CuryARHistoryFinGrouped.subID, Equal<CuryARHistory.subID>,
					And<CuryARHistoryFinGrouped.curyID, Equal<CuryARHistory.curyID>,
					And<CuryARHistoryFinGrouped.finPeriodID, Equal<CuryARHistory.finPeriodID>>>>>>>>
						>>,
				Where<CuryARHistory.customerID, Equal<Required<CuryARHistory.customerID>>,
					And<FinPeriod.masterFinPeriodID, GreaterEqual<Required<FinPeriod.finPeriodID>>>>>
				.Update(this, cust.BAccountID, finPeriod);

			PXUpdateJoin<
					Set<CuryARHistory.tranPtdSales, CuryARHistoryTranGrouped.tranPtdSalesSum,
					Set<CuryARHistory.tranPtdPayments, CuryARHistoryTranGrouped.tranPtdPaymentsSum,
					Set<CuryARHistory.tranPtdDrAdjustments, CuryARHistoryTranGrouped.tranPtdDrAdjustmentsSum,
					Set<CuryARHistory.tranPtdCrAdjustments, CuryARHistoryTranGrouped.tranPtdCrAdjustmentsSum,
					Set<CuryARHistory.tranPtdDiscounts, CuryARHistoryTranGrouped.tranPtdDiscountsSum,
					Set<CuryARHistory.tranPtdRGOL, CuryARHistoryTranGrouped.tranPtdRGOLSum,
					Set<CuryARHistory.tranPtdFinCharges, CuryARHistoryTranGrouped.tranPtdFinChargesSum,
					Set<CuryARHistory.tranPtdDeposits, CuryARHistoryTranGrouped.tranPtdDepositsSum,
					Set<CuryARHistory.tranPtdRetainageWithheld, CuryARHistoryTranGrouped.tranPtdRetainageWithheldSum,
					Set<CuryARHistory.tranPtdRetainageReleased, CuryARHistoryTranGrouped.tranPtdRetainageReleasedSum,
					Set<CuryARHistory.curyTranPtdSales, CuryARHistoryTranGrouped.curyTranPtdSalesSum,
					Set<CuryARHistory.curyTranPtdPayments, CuryARHistoryTranGrouped.curyTranPtdPaymentsSum,
					Set<CuryARHistory.curyTranPtdDrAdjustments, CuryARHistoryTranGrouped.curyTranPtdDrAdjustmentsSum,
					Set<CuryARHistory.curyTranPtdCrAdjustments, CuryARHistoryTranGrouped.curyTranPtdCrAdjustmentsSum,
					Set<CuryARHistory.curyTranPtdDiscounts, CuryARHistoryTranGrouped.curyTranPtdDiscountsSum,
					Set<CuryARHistory.curyTranPtdFinCharges, CuryARHistoryTranGrouped.curyTranPtdFinChargesSum,
					Set<CuryARHistory.curyTranPtdDeposits, CuryARHistoryTranGrouped.curyTranPtdDepositsSum,
					Set<CuryARHistory.curyTranPtdRetainageWithheld, CuryARHistoryTranGrouped.curyTranPtdRetainageWithheldSum,
					Set<CuryARHistory.curyTranPtdRetainageReleased, CuryARHistoryTranGrouped.curyTranPtdRetainageReleasedSum
					>>>>>>>>>>>>>>>>>>>,
				CuryARHistory,
				InnerJoin<CuryARHistoryTranGrouped,
					On<CuryARHistoryTranGrouped.branchID, Equal<CuryARHistory.branchID>,
					And<CuryARHistoryTranGrouped.customerID, Equal<CuryARHistory.customerID>,
					And<CuryARHistoryTranGrouped.accountID, Equal<CuryARHistory.accountID>,
					And<CuryARHistoryTranGrouped.subID, Equal<CuryARHistory.subID>,
					And<CuryARHistoryTranGrouped.curyID, Equal<CuryARHistory.curyID>,
					And<CuryARHistoryTranGrouped.finPeriodID, Equal<CuryARHistory.finPeriodID>>>>>>>>,
				Where<CuryARHistory.customerID, Equal<Required<CuryARHistory.customerID>>,
					And<CuryARHistory.finPeriodID, GreaterEqual<Required<CuryARHistory.finPeriodID>>>>>
				.Update(this, cust.BAccountID, finPeriod);
		}

		protected virtual void FixYtdARHistory(Customer cust, string finPeriod)
		{
			PXUpdateJoin<
			#region FinBegBalance
					Set<ARHistory.finBegBalance,
						ARHistoryYtdGrouped.finPtdSalesSum
						.Add<ARHistoryYtdGrouped.finPtdDrAdjustmentsSum
						.Add<ARHistoryYtdGrouped.finPtdFinChargesSum>>
						.Subtract
						<
							ARHistoryYtdGrouped.finPtdPaymentsSum
							.Add<ARHistoryYtdGrouped.finPtdCrAdjustmentsSum
							.Add<ARHistoryYtdGrouped.finPtdDiscountsSum
							.Add<ARHistoryYtdGrouped.finPtdRGOLSum>>>
						>
						.Subtract
						<
							ARHistoryYtdGrouped.finPtdSales
							.Add<ARHistoryYtdGrouped.finPtdDrAdjustments
							.Add<ARHistoryYtdGrouped.finPtdFinCharges>>
							.Subtract
							<
								ARHistoryYtdGrouped.finPtdPayments
								.Add<ARHistoryYtdGrouped.finPtdCrAdjustments
								.Add<ARHistoryYtdGrouped.finPtdDiscounts
								.Add<ARHistoryYtdGrouped.finPtdRGOL>>>
							>
						>,
			#endregion
			#region FinYtdBalance
					Set<ARHistory.finYtdBalance,
						ARHistoryYtdGrouped.finPtdSalesSum
						.Add<ARHistoryYtdGrouped.finPtdDrAdjustmentsSum
						.Add<ARHistoryYtdGrouped.finPtdFinChargesSum>>
						.Subtract
						<
							ARHistoryYtdGrouped.finPtdPaymentsSum
							.Add<ARHistoryYtdGrouped.finPtdCrAdjustmentsSum
							.Add<ARHistoryYtdGrouped.finPtdDiscountsSum
							.Add<ARHistoryYtdGrouped.finPtdRGOLSum>>>
						>,
			#endregion
			#region FinYtdDeposits
					Set<ARHistory.finYtdDeposits, ARHistoryYtdGrouped.finYtdDepositsSum,
			#endregion
			#region FinYtdRetainageWithheld
					Set<ARHistory.finYtdRetainageWithheld, ARHistoryYtdGrouped.finYtdRetainageWithheldSum,
			#endregion
			#region FinYtdRetainageReleased
					Set<ARHistory.finYtdRetainageReleased, ARHistoryYtdGrouped.finYtdRetainageReleasedSum
			#endregion
					>>>>>,
				ARHistory,
				LeftJoin<Branch,
					On<ARHistory.branchID, Equal<Branch.branchID>>,
				LeftJoin<FinPeriod,
					On<ARHistory.finPeriodID, Equal<FinPeriod.finPeriodID>,
						And<Branch.organizationID, Equal<FinPeriod.organizationID>,
						And<FinPeriod.masterFinPeriodID, GreaterEqual<Required<FinPeriod.masterFinPeriodID>>>>>,
				InnerJoin<ARHistoryYtdGrouped,
					On<ARHistoryYtdGrouped.branchID, Equal<ARHistory.branchID>,
					And<ARHistoryYtdGrouped.customerID, Equal<ARHistory.customerID>,
					And<ARHistoryYtdGrouped.accountID, Equal<ARHistory.accountID>,
					And<ARHistoryYtdGrouped.subID, Equal<ARHistory.subID>,
					And<ARHistoryYtdGrouped.finPeriodID, Equal<ARHistory.finPeriodID>>>>>>>
					>>,
				Where<ARHistory.customerID, Equal<Required<ARHistory.customerID>>>>
				.Update(this, finPeriod, cust.BAccountID);

			PXUpdateJoin<
			#region TranBegBalance
					Set<ARHistory.tranBegBalance,
						ARHistoryYtdGrouped.tranPtdSalesSum
						.Add<ARHistoryYtdGrouped.tranPtdDrAdjustmentsSum
						.Add<ARHistoryYtdGrouped.tranPtdFinChargesSum>>
						.Subtract
						<
							ARHistoryYtdGrouped.tranPtdPaymentsSum
							.Add<ARHistoryYtdGrouped.tranPtdCrAdjustmentsSum
							.Add<ARHistoryYtdGrouped.tranPtdDiscountsSum
							.Add<ARHistoryYtdGrouped.tranPtdRGOLSum>>>
						>
						.Subtract
						<
							ARHistoryYtdGrouped.tranPtdSales
							.Add<ARHistoryYtdGrouped.tranPtdDrAdjustments
							.Add<ARHistoryYtdGrouped.tranPtdFinCharges>>
							.Subtract
							<
								ARHistoryYtdGrouped.tranPtdPayments
								.Add<ARHistoryYtdGrouped.tranPtdCrAdjustments
								.Add<ARHistoryYtdGrouped.tranPtdDiscounts
								.Add<ARHistoryYtdGrouped.tranPtdRGOL>>>
							>
						>,
			#endregion
			#region TranYtdBalance
					Set<ARHistory.tranYtdBalance,
						ARHistoryYtdGrouped.tranPtdSalesSum
						.Add<ARHistoryYtdGrouped.tranPtdDrAdjustmentsSum
						.Add<ARHistoryYtdGrouped.tranPtdFinChargesSum>>
						.Subtract
						<
							ARHistoryYtdGrouped.tranPtdPaymentsSum
							.Add<ARHistoryYtdGrouped.tranPtdCrAdjustmentsSum
							.Add<ARHistoryYtdGrouped.tranPtdDiscountsSum
							.Add<ARHistoryYtdGrouped.tranPtdRGOLSum>>>
						>,
			#endregion
			#region TranYtdDeposits
					Set<ARHistory.tranYtdDeposits, ARHistoryYtdGrouped.tranYtdDepositsSum,
			#endregion
			#region TranYtdRetainageWithheld
					Set<ARHistory.tranYtdRetainageWithheld, ARHistoryYtdGrouped.tranYtdRetainageWithheldSum,
			#endregion
			#region TranYtdRetainageReleased
					Set<ARHistory.tranYtdRetainageReleased, ARHistoryYtdGrouped.tranYtdRetainageReleasedSum
			#endregion
					>>>>>,
				ARHistory,
				InnerJoin<ARHistoryYtdGrouped,
					On<ARHistoryYtdGrouped.branchID, Equal<ARHistory.branchID>,
					And<ARHistoryYtdGrouped.customerID, Equal<ARHistory.customerID>,
					And<ARHistoryYtdGrouped.accountID, Equal<ARHistory.accountID>,
					And<ARHistoryYtdGrouped.subID, Equal<ARHistory.subID>,
					And<ARHistoryYtdGrouped.finPeriodID, Equal<ARHistory.finPeriodID>>>>>>>,
				Where<ARHistory.customerID, Equal<Required<ARHistory.customerID>>,
					And<ARHistory.finPeriodID, GreaterEqual<Required<ARHistory.finPeriodID>>>>>
				.Update(this, cust.BAccountID, finPeriod);
		}

		protected virtual void FixYtdCuryARHistory(Customer cust, string finPeriod)
		{
			PXUpdateJoin<
			#region FinBegBalance
					Set<CuryARHistory.finBegBalance,
						CuryARHistoryYtdGrouped.finPtdSalesSum
						.Add<CuryARHistoryYtdGrouped.finPtdDrAdjustmentsSum>
						.Add<CuryARHistoryYtdGrouped.finPtdFinChargesSum>
						.Subtract
						<
							CuryARHistoryYtdGrouped.finPtdPaymentsSum
							.Add<CuryARHistoryYtdGrouped.finPtdCrAdjustmentsSum
							.Add<CuryARHistoryYtdGrouped.finPtdDiscountsSum>
							.Add<CuryARHistoryYtdGrouped.finPtdRGOLSum>>
						>
						.Subtract
						<
							CuryARHistoryYtdGrouped.finPtdSales
							.Add<CuryARHistoryYtdGrouped.finPtdDrAdjustments>
							.Add<CuryARHistoryYtdGrouped.finPtdFinCharges>
							.Subtract
							<
								CuryARHistoryYtdGrouped.finPtdPayments
								.Add<CuryARHistoryYtdGrouped.finPtdCrAdjustments
								.Add<CuryARHistoryYtdGrouped.finPtdDiscounts>
								.Add<CuryARHistoryYtdGrouped.finPtdRGOL>>
							>
						>,
			#endregion
			#region FinYtdBalance
					Set<CuryARHistory.finYtdBalance,
						CuryARHistoryYtdGrouped.finPtdSalesSum
						.Add<CuryARHistoryYtdGrouped.finPtdDrAdjustmentsSum>
						.Add<CuryARHistoryYtdGrouped.finPtdFinChargesSum>
						.Subtract
						<
							CuryARHistoryYtdGrouped.finPtdPaymentsSum
							.Add<CuryARHistoryYtdGrouped.finPtdCrAdjustmentsSum
							.Add<CuryARHistoryYtdGrouped.finPtdDiscountsSum>
							.Add<CuryARHistoryYtdGrouped.finPtdRGOLSum>>
						>,
			#endregion
			#region FinYtdDeposits
			Set<CuryARHistory.finYtdDeposits, CuryARHistoryYtdGrouped.finYtdDepositsSum,
			#endregion
			#region FinYtdRetainageWithheld
					Set<CuryARHistory.finYtdRetainageWithheld, CuryARHistoryYtdGrouped.finYtdRetainageWithheldSum,
			#endregion
			#region FinYtdRetainageReleased
					Set<CuryARHistory.finYtdRetainageReleased, CuryARHistoryYtdGrouped.finYtdRetainageReleasedSum,
			#endregion
			#region CuryFinBegBalance
				Set<CuryARHistory.curyFinBegBalance,
					CuryARHistoryYtdGrouped.curyFinPtdSalesSum
					.Add<CuryARHistoryYtdGrouped.curyFinPtdDrAdjustmentsSum>
					.Add<CuryARHistoryYtdGrouped.curyFinPtdFinChargesSum>
					.Subtract
					<
						CuryARHistoryYtdGrouped.curyFinPtdPaymentsSum
						.Add<CuryARHistoryYtdGrouped.curyFinPtdCrAdjustmentsSum
						.Add<CuryARHistoryYtdGrouped.curyFinPtdDiscountsSum>>
					>
					.Subtract
					<
						CuryARHistoryYtdGrouped.curyFinPtdSales
						.Add<CuryARHistoryYtdGrouped.curyFinPtdDrAdjustments>
						.Add<CuryARHistoryYtdGrouped.curyFinPtdFinCharges>
						.Subtract
						<
							CuryARHistoryYtdGrouped.curyFinPtdPayments
							.Add<CuryARHistoryYtdGrouped.curyFinPtdCrAdjustments
							.Add<CuryARHistoryYtdGrouped.curyFinPtdDiscounts>>
						>
					>,
			#endregion
			#region CuryFinYtdBalance
					Set<CuryARHistory.curyFinYtdBalance,
						CuryARHistoryYtdGrouped.curyFinPtdSalesSum
						.Add<CuryARHistoryYtdGrouped.curyFinPtdDrAdjustmentsSum>
						.Add<CuryARHistoryYtdGrouped.curyFinPtdFinChargesSum>
						.Subtract
						<
							CuryARHistoryYtdGrouped.curyFinPtdPaymentsSum
							.Add<CuryARHistoryYtdGrouped.curyFinPtdCrAdjustmentsSum
							.Add<CuryARHistoryYtdGrouped.curyFinPtdDiscountsSum>>
						>,
			#endregion
			#region CuryFinYtdDeposits
			Set<CuryARHistory.curyFinYtdDeposits, CuryARHistoryYtdGrouped.curyFinYtdDepositsSum,
			#endregion
			#region CuryFinYtdRetainageWithheld
					Set<CuryARHistory.curyFinYtdRetainageWithheld, CuryARHistoryYtdGrouped.curyFinYtdRetainageWithheldSum,
			#endregion
			#region CuryFinYtdRetainageReleased
					Set<CuryARHistory.curyFinYtdRetainageReleased, CuryARHistoryYtdGrouped.curyFinYtdRetainageReleasedSum
			#endregion
					>>>>>>>>>>,
				CuryARHistory,
				LeftJoin<Branch,
					On<CuryARHistory.branchID, Equal<Branch.branchID>>,
				LeftJoin<FinPeriod,
					On<CuryARHistory.finPeriodID, Equal<FinPeriod.finPeriodID>,
					And<Branch.organizationID, Equal<FinPeriod.organizationID>,
					And<FinPeriod.masterFinPeriodID, GreaterEqual<Required<FinPeriod.masterFinPeriodID>>>>>,
				InnerJoin<CuryARHistoryYtdGrouped,
					On<CuryARHistoryYtdGrouped.branchID, Equal<CuryARHistory.branchID>,
					And<CuryARHistoryYtdGrouped.customerID, Equal<CuryARHistory.customerID>,
					And<CuryARHistoryYtdGrouped.accountID, Equal<CuryARHistory.accountID>,
					And<CuryARHistoryYtdGrouped.subID, Equal<CuryARHistory.subID>,
					And<CuryARHistoryYtdGrouped.curyID, Equal<CuryARHistory.curyID>,
					And<CuryARHistoryYtdGrouped.finPeriodID, Equal<CuryARHistory.finPeriodID>>>>>>>>
						>>,
				Where<CuryARHistory.customerID, Equal<Required<CuryARHistory.customerID>>>>
				.Update(this, finPeriod, cust.BAccountID);

			PXUpdateJoin<
			#region TranBegBalance
					Set<CuryARHistory.tranBegBalance,
						CuryARHistoryYtdGrouped.tranPtdSalesSum
						.Add<CuryARHistoryYtdGrouped.tranPtdDrAdjustmentsSum>
						.Add<CuryARHistoryYtdGrouped.tranPtdFinChargesSum>
						.Subtract
						<
							CuryARHistoryYtdGrouped.tranPtdPaymentsSum
							.Add<CuryARHistoryYtdGrouped.tranPtdCrAdjustmentsSum
							.Add<CuryARHistoryYtdGrouped.tranPtdDiscountsSum>
							.Add<CuryARHistoryYtdGrouped.tranPtdRGOLSum>>
						>
						.Subtract
						<
							CuryARHistoryYtdGrouped.tranPtdSales
							.Add<CuryARHistoryYtdGrouped.tranPtdDrAdjustments>
							.Add<CuryARHistoryYtdGrouped.tranPtdFinCharges>
							.Subtract
							<
								CuryARHistoryYtdGrouped.tranPtdPayments
								.Add<CuryARHistoryYtdGrouped.tranPtdCrAdjustments
								.Add<CuryARHistoryYtdGrouped.tranPtdDiscounts>
								.Add<CuryARHistoryYtdGrouped.tranPtdRGOL>>
							>
						>,
			#endregion
			#region TranYtdBalance
					Set<CuryARHistory.tranYtdBalance,
						CuryARHistoryYtdGrouped.tranPtdSalesSum
						.Add<CuryARHistoryYtdGrouped.tranPtdDrAdjustmentsSum>
						.Add<CuryARHistoryYtdGrouped.tranPtdFinChargesSum>
						.Subtract
						<
							CuryARHistoryYtdGrouped.tranPtdPaymentsSum
							.Add<CuryARHistoryYtdGrouped.tranPtdCrAdjustmentsSum
							.Add<CuryARHistoryYtdGrouped.tranPtdDiscountsSum>
							.Add<CuryARHistoryYtdGrouped.tranPtdRGOLSum>>
						>,
			#endregion
			#region TranYtdDeposits
			Set<CuryARHistory.tranYtdDeposits, CuryARHistoryYtdGrouped.tranYtdDepositsSum,
			#endregion
			#region TranYtdRetainageWithheld
					Set<CuryARHistory.tranYtdRetainageWithheld, CuryARHistoryYtdGrouped.tranYtdRetainageWithheldSum,
			#endregion
			#region TranYtdRetainageReleased
					Set<CuryARHistory.tranYtdRetainageReleased, CuryARHistoryYtdGrouped.tranYtdRetainageReleasedSum,
			#endregion
			#region CuryTranBegBalance
				Set<CuryARHistory.curyTranBegBalance,
					CuryARHistoryYtdGrouped.curyTranPtdSalesSum
					.Add<CuryARHistoryYtdGrouped.curyTranPtdDrAdjustmentsSum>
					.Add<CuryARHistoryYtdGrouped.curyTranPtdFinChargesSum>
					.Subtract
					<
						CuryARHistoryYtdGrouped.curyTranPtdPaymentsSum
						.Add<CuryARHistoryYtdGrouped.curyTranPtdCrAdjustmentsSum
						.Add<CuryARHistoryYtdGrouped.curyTranPtdDiscountsSum>>
					>
					.Subtract
					<
						CuryARHistoryYtdGrouped.curyTranPtdSales
						.Add<CuryARHistoryYtdGrouped.curyTranPtdDrAdjustments>
						.Add<CuryARHistoryYtdGrouped.curyTranPtdFinCharges>
						.Subtract
						<
							CuryARHistoryYtdGrouped.curyTranPtdPayments
							.Add<CuryARHistoryYtdGrouped.curyTranPtdCrAdjustments
							.Add<CuryARHistoryYtdGrouped.curyTranPtdDiscounts>>
						>
					>,
			#endregion
			#region CuryTranYtdBalance
					Set<CuryARHistory.curyTranYtdBalance,
						CuryARHistoryYtdGrouped.curyTranPtdSalesSum
						.Add<CuryARHistoryYtdGrouped.curyTranPtdDrAdjustmentsSum>
						.Add<CuryARHistoryYtdGrouped.curyTranPtdFinChargesSum>
						.Subtract
						<
							CuryARHistoryYtdGrouped.curyTranPtdPaymentsSum
							.Add<CuryARHistoryYtdGrouped.curyTranPtdCrAdjustmentsSum
							.Add<CuryARHistoryYtdGrouped.curyTranPtdDiscountsSum>>
						>,
			#endregion
			#region CuryTranYtdDeposits
			Set<CuryARHistory.curyTranYtdDeposits, CuryARHistoryYtdGrouped.curyTranYtdDepositsSum,
			#endregion
			#region CuryTranYtdRetainageWithheld
					Set<CuryARHistory.curyTranYtdRetainageWithheld, CuryARHistoryYtdGrouped.curyTranYtdRetainageWithheldSum,
			#endregion
			#region CuryTranYtdRetainageReleased
					Set<CuryARHistory.curyTranYtdRetainageReleased, CuryARHistoryYtdGrouped.curyTranYtdRetainageReleasedSum
			#endregion

				>>>>>>>>>>,
			CuryARHistory,
				InnerJoin<CuryARHistoryYtdGrouped,
					On<CuryARHistoryYtdGrouped.branchID, Equal<CuryARHistory.branchID>,
					And<CuryARHistoryYtdGrouped.customerID, Equal<CuryARHistory.customerID>,
					And<CuryARHistoryYtdGrouped.accountID, Equal<CuryARHistory.accountID>,
					And<CuryARHistoryYtdGrouped.subID, Equal<CuryARHistory.subID>,
					And<CuryARHistoryYtdGrouped.curyID, Equal<CuryARHistory.curyID>,
					And<CuryARHistoryYtdGrouped.finPeriodID, Equal<CuryARHistory.finPeriodID>>>>>>>>,
				Where<CuryARHistory.customerID, Equal<Required<CuryARHistory.customerID>>,
					And<CuryARHistory.finPeriodID, GreaterEqual<Required<CuryARHistory.finPeriodID>>>>>
				.Update(this, cust.BAccountID, finPeriod);
		}

		protected virtual void UpdateARBalancesCurrent(Customer cust)
		{
			var attr = new PXDBLastChangeDateTimeAttribute(typeof(ARBalances.lastModifiedDateTime));
			DateTime lastModified = attr.GetDate();
			Guid userID = _currentUserInformationProvider.GetUserIdOrDefault();
			string screenID = Accessinfo.ScreenID?.Replace(".", "") ?? "00000000";

			PXUpdateJoin<
					Set<ARBalances.currentBal, IsNull<ARCurrentBalanceSum.currentBalSum, Zero>,
					Set<ARBalances.lastModifiedByID, Required<ARBalances.lastModifiedByID>,
					Set<ARBalances.lastModifiedByScreenID, Required<ARBalances.lastModifiedByScreenID>,
					Set<ARBalances.lastModifiedDateTime, Required<ARBalances.lastModifiedDateTime>
					>>>>,
				ARBalances,
				LeftJoin<ARCurrentBalanceSum,
					On<ARCurrentBalanceSum.branchID, Equal<ARBalances.branchID>,
					And<ARCurrentBalanceSum.customerID, Equal<ARBalances.customerID>,
					And<ARCurrentBalanceSum.customerLocationID, Equal<ARBalances.customerLocationID>>>>>,
				Where<ARBalances.customerID, Equal<Required<ARBalances.customerID>>>>
				.Update(this, userID, screenID, lastModified, cust.BAccountID);
		}

		protected virtual void UpdateARBalancesUnreleased(Customer cust)
		{
			var attr = new PXDBLastChangeDateTimeAttribute(typeof(ARBalances.lastModifiedDateTime));
			DateTime lastModified = attr.GetDate();
			Guid userID = _currentUserInformationProvider.GetUserIdOrDefault();
			string screenID = Accessinfo.ScreenID?.Replace(".", "") ?? "00000000";

			PXUpdateJoin<
					Set<ARBalances.unreleasedBal, IsNull<ARCurrentBalanceUnreleasedSum.unreleasedBalSum, Zero>,
					Set<ARBalances.lastModifiedByID, Required<ARBalances.lastModifiedByID>,
					Set<ARBalances.lastModifiedByScreenID, Required<ARBalances.lastModifiedByScreenID>,
					Set<ARBalances.lastModifiedDateTime, Required<ARBalances.lastModifiedDateTime>
					>>>>,
				ARBalances,
				LeftJoin<ARCurrentBalanceUnreleasedSum,
					On<ARCurrentBalanceUnreleasedSum.branchID, Equal<ARBalances.branchID>,
					And<ARCurrentBalanceUnreleasedSum.customerID, Equal<ARBalances.customerID>,
					And<ARCurrentBalanceUnreleasedSum.customerLocationID, Equal<ARBalances.customerLocationID>>>>>,
				Where<ARBalances.customerID, Equal<Required<ARBalances.customerID>>,
					And<ARBalances.unreleasedBal, NotEqual<IsNull<ARCurrentBalanceUnreleasedSum.unreleasedBalSum, Zero>>>>>
				.Update(this, userID, screenID, lastModified, cust.BAccountID);
		}

		protected virtual void UpdateARBalancesOpenOrders(Customer cust)
		{
			PXUpdateJoin<
					Set<ARBalances.totalOpenOrders, IsNull<ARCurrentBalanceOpenOrdersSum.unbilledOrderTotal, Zero>>,
				ARBalances,
				LeftJoin<ARCurrentBalanceOpenOrdersSum,
					On<ARCurrentBalanceOpenOrdersSum.branchID, Equal<ARBalances.branchID>,
					And<ARCurrentBalanceOpenOrdersSum.customerID, Equal<ARBalances.customerID>,
					And<ARCurrentBalanceOpenOrdersSum.customerLocationID, Equal<ARBalances.customerLocationID>>>>>,
				Where<ARBalances.customerID, Equal<Required<ARBalances.customerID>>>>
				.Update(this, cust.BAccountID);
		}

		protected virtual void FillMissedARHistory(Customer cust, string finPeriod)
		{
			PXSelectJoinGroupBy<ARTranPost,
			   LeftJoin<ARHistory,
				   On<ARHistory.branchID, Equal<ARTranPost.branchID>,
				   And<ARHistory.accountID, Equal<ARTranPost.accountID>,
				   And<ARHistory.subID, Equal<ARTranPost.subID>,
				   And<ARHistory.customerID, Equal<ARTranPost.customerID>,
				   And<ARHistory.finPeriodID, Equal<ARTranPost.finPeriodID>>>>>>>,
			   Where<ARTranPost.customerID, Equal<Required<ARTranPost.customerID>>,
				   And<ARTranPost.finPeriodID, GreaterEqual<Required<ARTranPost.finPeriodID>>,
				   And<ARHistory.branchID, IsNull>>>,
			   Aggregate<
				   GroupBy<ARTranPost.branchID,
				   GroupBy<ARTranPost.accountID,
				   GroupBy<ARTranPost.subID,
				   GroupBy<ARTranPost.customerID,
				   GroupBy<ARTranPost.finPeriodID
				   >>>>>>>
				   .Select(this, cust.BAccountID, finPeriod)
				   .RowCast<ARTranPost>()
				   .ForEach((e, i) => AddARHistory(e, e.FinPeriodID));

			PXSelectJoinGroupBy<ARTranPost,
				LeftJoin<ARHistory,
					On<ARHistory.branchID, Equal<ARTranPost.branchID>,
					And<ARHistory.accountID, Equal<ARTranPost.accountID>,
					And<ARHistory.subID, Equal<ARTranPost.subID>,
					And<ARHistory.customerID, Equal<ARTranPost.customerID>,
					And<ARHistory.finPeriodID, Equal<ARTranPost.tranPeriodID>>>>>>>,
				Where<ARTranPost.customerID, Equal<Required<ARTranPost.customerID>>,
					And<ARTranPost.finPeriodID, GreaterEqual<Required<ARTranPost.finPeriodID>>,
					And<ARHistory.branchID, IsNull>>>,
				Aggregate<
					GroupBy<ARTranPost.branchID,
					GroupBy<ARTranPost.accountID,
					GroupBy<ARTranPost.subID,
					GroupBy<ARTranPost.customerID,
					GroupBy<ARTranPost.tranPeriodID
					>>>>>>>
					.Select(this, cust.BAccountID, finPeriod)
					.RowCast<ARTranPost>()
					.ForEach((e, i) => AddARHistory(e, e.TranPeriodID));
		}
		protected void AddARHistory(ARTranPost tranPost, string finPeriodID)
		{
			if (tranPost.AccountID == null || tranPost.SubID == null || finPeriodID == null)
			{
				return;
			}

			PXDatabase.Insert<ARHistory>(
					new PXDataFieldAssign(typeof(ARHistory.branchID).Name, tranPost.BranchID),
					new PXDataFieldAssign(typeof(ARHistory.accountID).Name, tranPost.AccountID),
					new PXDataFieldAssign(typeof(ARHistory.subID).Name, tranPost.SubID),
					new PXDataFieldAssign(typeof(ARHistory.customerID).Name, tranPost.CustomerID),
					new PXDataFieldAssign(typeof(ARHistory.finPeriodID).Name, finPeriodID),

					new PXDataFieldAssign(typeof(ARHistory.detDeleted).Name, false),
					new PXDataFieldAssign(typeof(ARHistory.finPtdDrAdjustments).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.finPtdCrAdjustments).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.finPtdSales).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.finPtdPayments).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.finPtdDiscounts).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.finYtdBalance).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.finBegBalance).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.finPtdCOGS).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.finPtdRGOL).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.finPtdFinCharges).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.finPtdDeposits).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.finYtdDeposits).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.finPtdItemDiscounts).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.finPtdRevalued).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.tranPtdDrAdjustments).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.tranPtdCrAdjustments).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.tranPtdSales).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.tranPtdPayments).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.tranPtdDiscounts).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.tranYtdBalance).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.tranBegBalance).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.tranPtdCOGS).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.tranPtdRGOL).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.tranPtdFinCharges).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.tranPtdDeposits).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.tranYtdDeposits).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.tranPtdItemDiscounts).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.numberInvoicePaid).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.paidInvoiceDays).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.finPtdRetainageWithheld).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.finYtdRetainageWithheld).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.tranPtdRetainageWithheld).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.tranYtdRetainageWithheld).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.finPtdRetainageReleased).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.finYtdRetainageReleased).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.tranPtdRetainageReleased).Name, 0.0),
					new PXDataFieldAssign(typeof(ARHistory.tranYtdRetainageReleased).Name, 0.0)
				);
		}

		protected virtual void FillMissedCuryARHistory(Customer cust, string finPeriod)
		{
			PXSelectJoinGroupBy<ARTranPost,
				InnerJoin<CurrencyInfo,
					On<CurrencyInfo.curyInfoID, Equal<ARTranPost.curyInfoID>>,
				LeftJoin<CuryARHistory,
					On<CuryARHistory.branchID, Equal<ARTranPost.branchID>,
					And<CuryARHistory.accountID, Equal<ARTranPost.accountID>,
					And<CuryARHistory.subID, Equal<ARTranPost.subID>,
					And<CuryARHistory.customerID, Equal<ARTranPost.customerID>,
					And<CuryARHistory.finPeriodID, Equal<ARTranPost.finPeriodID>,
					And<CuryARHistory.curyID, Equal<CurrencyInfo.curyID>>>>>>>>>,
				Where<ARTranPost.customerID, Equal<Required<ARTranPost.customerID>>,
					And<ARTranPost.finPeriodID, GreaterEqual<Required<ARTranPost.finPeriodID>>,
					And<CuryARHistory.branchID, IsNull>>>,
				Aggregate<
					GroupBy<ARTranPost.branchID,
					GroupBy<ARTranPost.accountID,
					GroupBy<ARTranPost.subID,
					GroupBy<ARTranPost.customerID,
					GroupBy<ARTranPost.finPeriodID,
					GroupBy<CurrencyInfo.curyID
					>>>>>>>>
					.Select(this, cust.BAccountID, finPeriod)
					.ForEach((e, i) => AddCuryARHistory(e,
						PXResult.Unwrap<CurrencyInfo>(e)?.CuryID,
						PXResult.Unwrap<ARTranPost>(e).FinPeriodID));

			PXSelectJoinGroupBy<ARTranPost,
				InnerJoin<CurrencyInfo,
					On<CurrencyInfo.curyInfoID, Equal<ARTranPost.curyInfoID>>,
				LeftJoin<CuryARHistory,
					On<CuryARHistory.branchID, Equal<ARTranPost.branchID>,
					And<CuryARHistory.accountID, Equal<ARTranPost.accountID>,
					And<CuryARHistory.subID, Equal<ARTranPost.subID>,
					And<CuryARHistory.customerID, Equal<ARTranPost.customerID>,
					And<CuryARHistory.finPeriodID, Equal<ARTranPost.tranPeriodID>,
					And<CuryARHistory.curyID, Equal<CurrencyInfo.curyID>>>>>>>>>,
				Where<ARTranPost.customerID, Equal<Required<ARTranPost.customerID>>,
					And<ARTranPost.tranPeriodID, GreaterEqual<Required<ARTranPost.finPeriodID>>,
					And<CuryARHistory.branchID, IsNull>>>,
				Aggregate<
					GroupBy<ARTranPost.branchID,
					GroupBy<ARTranPost.accountID,
					GroupBy<ARTranPost.subID,
					GroupBy<ARTranPost.customerID,
					GroupBy<ARTranPost.tranPeriodID,
					GroupBy<CurrencyInfo.curyID
					>>>>>>>>
					.Select(this, cust.BAccountID, finPeriod)
					.ForEach((e, i) => AddCuryARHistory(e,
						PXResult.Unwrap<CurrencyInfo>(e)?.CuryID,
						PXResult.Unwrap<ARTranPost>(e).TranPeriodID));

		}
		protected void AddCuryARHistory(ARTranPost tranPost, string curyID, string finPeriodID)
		{
			if (tranPost.AccountID == null || tranPost.SubID == null || finPeriodID == null || curyID == null)
			{
				return;
			}

			PXDatabase.Insert<CuryARHistory>(
					new PXDataFieldAssign(typeof(CuryARHistory.branchID).Name, tranPost.BranchID),
					new PXDataFieldAssign(typeof(CuryARHistory.accountID).Name, tranPost.AccountID ?? -123),
					new PXDataFieldAssign(typeof(CuryARHistory.subID).Name, tranPost.SubID ?? -123),
					new PXDataFieldAssign(typeof(CuryARHistory.customerID).Name, tranPost.CustomerID),
					new PXDataFieldAssign(typeof(CuryARHistory.finPeriodID).Name, finPeriodID),
					new PXDataFieldAssign(typeof(CuryARHistory.curyID).Name, curyID),

					new PXDataFieldAssign(typeof(CuryARHistory.detDeleted).Name, false),
					new PXDataFieldAssign(typeof(CuryARHistory.finBegBalance).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.finPtdSales).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.finPtdPayments).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.finPtdDrAdjustments).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.finPtdCrAdjustments).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.finPtdDiscounts).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.finPtdCOGS).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.finPtdRGOL).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.finPtdFinCharges).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.finYtdBalance).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.finPtdDeposits).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.finYtdDeposits).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.tranBegBalance).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.tranPtdSales).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.tranPtdPayments).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.tranPtdDrAdjustments).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.tranPtdCrAdjustments).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.tranPtdDiscounts).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.tranPtdRGOL).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.tranPtdCOGS).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.tranPtdFinCharges).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.tranYtdBalance).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.tranPtdDeposits).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.tranYtdDeposits).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyFinBegBalance).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyFinPtdSales).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyFinPtdPayments).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyFinPtdDrAdjustments).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyFinPtdCrAdjustments).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyFinPtdDiscounts).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyFinPtdFinCharges).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyFinYtdBalance).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyFinPtdDeposits).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyFinYtdDeposits).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyTranBegBalance).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyTranPtdSales).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyTranPtdPayments).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyTranPtdDrAdjustments).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyTranPtdCrAdjustments).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyTranPtdDiscounts).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyTranPtdFinCharges).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyTranYtdBalance).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyTranPtdDeposits).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyTranYtdDeposits).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.finPtdRevalued).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyFinPtdRetainageWithheld).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.finPtdRetainageWithheld).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyTranPtdRetainageWithheld).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.tranPtdRetainageWithheld).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyFinYtdRetainageWithheld).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.finYtdRetainageWithheld).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyTranYtdRetainageWithheld).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.tranYtdRetainageWithheld).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyFinPtdRetainageReleased).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.finPtdRetainageReleased).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyTranPtdRetainageReleased).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.tranPtdRetainageReleased).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyFinYtdRetainageReleased).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.finYtdRetainageReleased).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.curyTranYtdRetainageReleased).Name, 0.0),
					new PXDataFieldAssign(typeof(CuryARHistory.tranYtdRetainageReleased).Name, 0.0)
				);
		}
	}
}
