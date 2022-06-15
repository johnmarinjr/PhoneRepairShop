using System;
using System.Collections.Generic;
using System.Linq;

using PX.Common;
using PX.Data;

using PX.Objects.Common;
using PX.Objects.Common.Extensions;
using PX.Objects.DR.Descriptor;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.EP;
using PX.Objects.GL;
using PX.Objects.IN;
using ComponentAmount = System.Tuple<PX.Objects.DR.Descriptor.InventoryItemComponentInfo, decimal>;
using PX.Objects.CM;
using Amount = PX.Objects.AR.ARReleaseProcess.Amount;
using PX.Objects.GL.FinPeriods;
using PX.Objects.GL.FinPeriods.TableDefinition;

namespace PX.Objects.DR
{
	public class SingleScheduleCreator
	{
	    public IFinPeriodRepository FinPeriodRepository { get; }
	    DRSchedule _schedule;

		private readonly IDREntityStorage _drEntityStorage;
		private readonly ISubaccountProvider _subaccountProvider;
		private readonly IBusinessAccountProvider _businessAccountProvider;
		private readonly IInventoryItemProvider _inventoryItemProvider;
		private readonly ISingleScheduleViewProvider _singleScheduleViewProvider;
		private readonly ISalesPriceProvider _salesPriceProvider;
		private readonly IDRDataProvider _dataProvider;

		private readonly Func<decimal, decimal> _roundingFunction;

		private readonly bool _isDraft;
		private readonly int? _branchID;
		private readonly Location _location;
		private readonly CurrencyInfo _currencyInfo;

		/// <param name="roundingFunction">
		/// The function that should be used for rounding transaction amounts.
		/// </param>
		/// <param name="isDraft">
		/// Indicates whether the schedule to be created or reevaluated is a draft schedule.
		/// In particular, it affects whether credit line / deferral transactions would be
		/// generated for the schedule.
		/// </param>
		public SingleScheduleCreator(
			IDREntityStorage drEntityStorage,
			ISubaccountProvider subaccountProvider,
			IBusinessAccountProvider businessAccountProvider,
			IInventoryItemProvider inventoryItemProvider,
            IFinPeriodRepository finPeriodRepository,
			Func<decimal, decimal> roundingFunction,
			int? branchID,
			bool isDraft,
			Location location,
			CurrencyInfo currencyInfo)
		{
			if (drEntityStorage == null) throw new ArgumentNullException(nameof(drEntityStorage));
			if (subaccountProvider == null) throw new ArgumentNullException(nameof(subaccountProvider));
			if (businessAccountProvider == null) throw new ArgumentNullException(nameof(businessAccountProvider));
			if (inventoryItemProvider == null) throw new ArgumentNullException(nameof(inventoryItemProvider));
			if (roundingFunction == null) throw new ArgumentNullException(nameof(roundingFunction));

		    FinPeriodRepository = finPeriodRepository;

		    _drEntityStorage = drEntityStorage;
			_subaccountProvider = subaccountProvider;
			_businessAccountProvider = businessAccountProvider;
			_inventoryItemProvider = inventoryItemProvider;

			_roundingFunction = roundingFunction;

			_isDraft = isDraft;
			_branchID = branchID;
			_location = location;
			_currencyInfo = currencyInfo;
		}

		public SingleScheduleCreator(
			IDREntityStorage drEntityStorage,
			ISubaccountProvider subaccountProvider,
			IBusinessAccountProvider businessAccountProvider,
			IInventoryItemProvider inventoryItemProvider,
			ISingleScheduleViewProvider singleScheduleViewProvider,
			ISalesPriceProvider salesPriceProvider,
			IDRDataProvider dataProvider,
			IFinPeriodRepository finPeriodRepository,
            Func<decimal, decimal> roundingFunction,
			int? branchID,
			bool isDraft,
			Location location,
			CurrencyInfo currencyInfo)
			: this(drEntityStorage, subaccountProvider, businessAccountProvider, inventoryItemProvider, finPeriodRepository, roundingFunction, branchID, isDraft, location, currencyInfo)
		{
			_salesPriceProvider = salesPriceProvider ?? throw new ArgumentNullException(nameof(salesPriceProvider));
			_singleScheduleViewProvider = singleScheduleViewProvider ?? throw new ArgumentNullException(nameof(singleScheduleViewProvider));
			_dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
		}

		private void ValidateMDAConsistency(InventoryItem inventoryItem, DRDeferredCode deferralCode)
		{
			if (inventoryItem != null &&
				inventoryItem.IsSplitted == true &&
				deferralCode.MultiDeliverableArrangement == false)
			{
				throw new PXException(Messages.DeferralCodeNotMDA);
			}
			else if (
				inventoryItem == null &&
				deferralCode.MultiDeliverableArrangement == true)
			{
				throw new PXException(Messages.MDACodeButNoInventoryItem);
			}
		}

		/// <param name="deferralCode">The item-level deferral code for the inventory item
		/// <param name="transactionAmount">Total transaction amount (with ALL discounts applied).</param>
		/// <param name="fairUnitPrice">The item price from the price list.</param>
		/// <param name="compoundDiscountRate"> Compound discount rate of all discounts
		/// (including line, group and document discounts) that are applicable to deferred components.</param>
		public void CreateOriginalSchedule(
			DRProcess.DRScheduleParameters scheduleParameters,
			Amount lineTotal)
		{
			_schedule = _drEntityStorage.CreateCopy(scheduleParameters);

			_schedule.IsDraft = _isDraft;
			_schedule.IsCustom = false;
			_schedule.IsSuspense = false;

			_schedule = _drEntityStorage.Insert(_schedule);
			CreateScheduleDetails(scheduleParameters, lineTotal);

		}

		private void CreateScheduleDetails(DRProcess.DRScheduleParameters scheduleParameters, Amount lineTotal)
		{
			decimal scheduleFairTotal = 0;
			decimal inventoryFairTotal = 0;
			decimal allocationWeightTotal = 0;
			int lineNbrPrev = 0;

			var errors = new List<string>();

			foreach (PXResult<ARTran, InventoryItem, DRDeferredCode, INComponent,
				DRSingleProcess.ComponentINItem, DRSingleProcess.ComponentDeferredCode> item in _singleScheduleViewProvider.GetParentDocumentDetails())
			{
				ARTran artran = item;
				InventoryItem inventoryItem = item;
				DRDeferredCode deferredCode = item;
				INComponent component = item;
				DRSingleProcess.ComponentINItem componentINItem = item;
				DRSingleProcess.ComponentDeferredCode componentDeferredCode = item;

				if (lineNbrPrev == 0) lineNbrPrev = artran.LineNbr.Value;
				if (artran.LineNbr != lineNbrPrev)
				{
					inventoryFairTotal = 0;
					lineNbrPrev = artran.LineNbr.Value;
				}

				bool isMDA = deferredCode.MultiDeliverableArrangement == true;

				AccountSubaccountPair deferralAccountSubaccount = GetDeferralAccountSubaccount(
					isMDA ? componentDeferredCode : deferredCode,
					isMDA ? componentINItem : inventoryItem,
					scheduleParameters,
					artran.SubID);

				AccountSubaccountPair salesAccountSubaccount = GetSalesAccountSubaccount(deferredCode, inventoryItem, component, artran);

				bool isFlexibleMethod = deferredCode.Method == DeferredMethodType.FlexibleProrateDays ||
				deferredCode.Method == DeferredMethodType.FlexibleExactDays ||
				componentDeferredCode?.Method == DeferredMethodType.FlexibleProrateDays ||
				componentDeferredCode?.Method == DeferredMethodType.FlexibleExactDays;

				try
				{
					//Residual component is always last (sorting in DRSingleProcess.ARTransactionWithItems)
					if (isMDA && component.AmtOptionASC606 == INAmountOptionASC606.Residual)
					{
						decimal compoundDiscountRate = (1.0m - (artran.DiscPctDR ?? 0.0m) * 0.01m);
						decimal allocationWeight = compoundDiscountRate * (artran.CuryUnitPriceDR ?? 0.0m) * artran.Qty.Value - inventoryFairTotal;

						//residual component without DefCode should be recognized immediately
						if (componentDeferredCode == null || componentDeferredCode.DeferredCodeID == null)
						{
							AccountSubaccountPair salesOrExpenseAccountSubaccount =
								GetSalesOrExpenseAccountSubaccount(component, inventoryItem);
							int? salesExpenseSubIDOverride = (inventoryItem.UseParentSubID == true ? null : artran.SubID)
																?? salesOrExpenseAccountSubaccount.SubID;
							AccountSubaccountPair salesOrExpenseAccountSubaccountResidual
								= new AccountSubaccountPair(salesOrExpenseAccountSubaccount.AccountID, salesExpenseSubIDOverride);

							DRScheduleDetail detail = CreateScheduleDetail(
									artran,
									component,
									null,
									salesOrExpenseAccountSubaccountResidual,
									salesOrExpenseAccountSubaccountResidual,
									isFlexibleMethod,
									true);
							detail.ComponentID = component.ComponentID;
							detail = _drEntityStorage.Insert(detail);
							detail.AllocationWeightResidual = allocationWeight;
							detail = _drEntityStorage.Update(detail);
						}
						else //residual component with DefCode should be recognized like a non-residual component
						{
					DRScheduleDetail detail = CreateScheduleDetail(
							artran,
							component,
									componentDeferredCode.DeferredCodeID,
							deferralAccountSubaccount,
							salesAccountSubaccount,
									isFlexibleMethod,
									true);
							detail.ComponentID = component.ComponentID;
							detail = _drEntityStorage.Insert(detail);
							detail.AllocationWeightResidual = allocationWeight;
							detail = _drEntityStorage.Update(detail);

							if (!_isDraft)
							{
								_drEntityStorage.CreateCreditLineTransaction(detail, deferredCode, _branchID);
							}
						}

						allocationWeightTotal += allocationWeight;
					}
					//Non-residual component
					else
					{
						string deferredCodeID = component?.DeferredCode ?? deferredCode.DeferredCodeID;
						DRScheduleDetail detail = CreateScheduleDetail(
								artran,
								component,
								deferredCodeID,
								deferralAccountSubaccount,
								salesAccountSubaccount,
								isFlexibleMethod);
						detail.ComponentID = component?.ComponentID ?? artran.InventoryID;
						detail.ParentInventoryID = component?.ComponentID != null ? artran.InventoryID : null;
					detail = _drEntityStorage.Insert(detail);

						SetFairValuePrice(detail, artran);
					detail = _drEntityStorage.Update(detail);

					if (!_isDraft)
					{
						_drEntityStorage.CreateCreditLineTransaction(detail, deferredCode, _branchID);
					}

						inventoryFairTotal += detail.EffectiveFairValuePrice.Value * detail.Qty.Value;
					scheduleFairTotal += detail.EffectiveFairValuePrice.Value * detail.Qty.Value;
				}
				}
				catch (NoFairValuePriceFoundException e)
				{
					errors.Add(e.Message);
					continue;
				}
			}

			if (errors.Any())
			{
				throw new NoFairValuePricesFoundException(string.Join(Environment.NewLine, errors));
			}

			IEnumerable<DRScheduleDetail> scheduleDetails = _drEntityStorage.GetScheduleDetails(_schedule.ScheduleID)
				.RowCast<DRScheduleDetail>()
				.ToList();

			if (scheduleDetails.IsSingleElement())
			{
				DRScheduleDetail scheduleDetail = scheduleDetails.Single();
				scheduleDetail.CuryTotalAmt = lineTotal.Cury;
				scheduleDetail.CuryDefAmt = lineTotal.Cury;
                scheduleDetail.Percentage = 1m;

				_drEntityStorage.Update(scheduleDetail);
			}
			else if (scheduleDetails.HasAtLeastTwoItems())
			{
				decimal sumPercent = 0m;
				decimal sumResult = 0m;
				decimal sumResidual = 0m;
				DRScheduleDetail maxAmtLine = null;

				var residualScheduleDetails = scheduleDetails.Where(x => x.IsResidual == true);
				foreach (var scheduleDetail in residualScheduleDetails)
				{
					decimal allocationWeight = scheduleDetail.AllocationWeightResidual.Value;

					if ((allocationWeight <= 0 || allocationWeightTotal<=0)
						&& residualScheduleDetails.Count() > 1)
					{
						throw new ScheduleCuryTotalAmtLessOrEqualZeroException(Messages.DRScheduleAllocationWeightIsLessOrEqualZero,
							_inventoryItemProvider.GetInventoryItemByID(scheduleDetail.ComponentID)?.InventoryCD);
					}

					decimal? rawResult = (lineTotal.Cury - scheduleFairTotal) * allocationWeight / allocationWeightTotal;
					decimal? result = _roundingFunction(rawResult.Value);
					scheduleDetail.CuryTotalAmt = result;
					scheduleDetail.CuryDefAmt = scheduleDetail.DefCode == null ? 0 : scheduleDetail.CuryTotalAmt;

					sumResidual += scheduleDetail.CuryTotalAmt.Value;

					var detail = _drEntityStorage.Update(scheduleDetail);
					if ((maxAmtLine?.CuryTotalAmt ?? 0m) < detail.CuryTotalAmt)
					{
						maxAmtLine = detail;
					}
				}

				if (sumResidual <= 0 && residualScheduleDetails.Any())
				{
					throw new ScheduleCuryTotalAmtLessOrEqualZeroException(Messages.DRScheduleCuryTotalAmtIsLessOrEqualZeroSingleResidualComponent);
				}

				var fairValueScheduleDetails = scheduleDetails.Where(x => x.IsResidual != true);

				if (scheduleFairTotal == 0 && fairValueScheduleDetails.Any())
					throw new PXException(Messages.SumOfFairValuePricesEqualToZero);

				foreach (var scheduleDetail in fairValueScheduleDetails)
				{
					scheduleDetail.Percentage = scheduleDetail.EffectiveFairValuePrice * scheduleDetail.Qty / scheduleFairTotal;
					decimal? rawResult = (lineTotal.Cury - sumResidual) * scheduleDetail.Percentage;
					decimal? result = _roundingFunction(rawResult.Value);
					sumResult += result ?? 0m;

					scheduleDetail.CuryTotalAmt = result;
					scheduleDetail.CuryDefAmt = result;

					sumPercent += scheduleDetail.Percentage ?? 0m;

					var detail = _drEntityStorage.Update(scheduleDetail);
					if ((maxAmtLine?.CuryTotalAmt ?? 0m) < detail.CuryTotalAmt)
					{
						maxAmtLine = detail;
					}
				}

				if (sumPercent != 1m || sumResult != (lineTotal.Cury - sumResidual))
				{
					decimal? amtDiff = (lineTotal.Cury - sumResidual) - sumResult;

					maxAmtLine.CuryTotalAmt += amtDiff;
					maxAmtLine.CuryDefAmt += amtDiff;
					maxAmtLine.Percentage += 1m - sumPercent;

					_drEntityStorage.Update(maxAmtLine);
				}
			}

			if (!_isDraft)
			{
				foreach (PXResult<DRScheduleDetail, DRDeferredCode> detail in _dataProvider.GetScheduleDetailsResultset(_schedule.ScheduleID))
				{
					DRScheduleDetail scheduleDetail = detail;
					DRDeferredCode deferralCode = detail;

					IEnumerable<DRScheduleTran> deferralTransactions =
						_drEntityStorage.CreateDeferralTransactions(_schedule, scheduleDetail, deferralCode, _branchID);

					_drEntityStorage.NonDraftDeferralTransactionsPrepared(scheduleDetail, deferralCode, deferralTransactions);
				}
			}
		}

		private AccountSubaccountPair GetSalesOrExpenseAccountSubaccount(INComponent component, InventoryItem componentItem)
		{
			return _schedule.Module == BatchModule.AP
				? new AccountSubaccountPair(componentItem.COGSAcctID, componentItem.COGSSubID)
				: new AccountSubaccountPair(component.SalesAcctID, component.SalesSubID);
		}

		private void SetFairValuePrice(DRScheduleDetail scheduleDetail, ARTran artran)
		{
			_salesPriceProvider.SetFairValueSalesPrice(scheduleDetail, artran, _location, _currencyInfo);
		}

		/// <param name="attachedToOriginalSchedule">
		/// Flag added to handle <see cref="DRScheduleDetail"/>'s status
		/// in the same way as <see cref="DRProcess"/> had done for documents
		/// attached to original schedule.
		/// </param>
		public void ReevaluateSchedule(
			DRSchedule schedule,
			DRProcess.DRScheduleParameters scheduleParameters,
			Amount lineAmount,
			bool attachedToOriginalSchedule)
		{
			if (schedule.IsOverridden == true && _isDraft == false)
			{
				decimal? totalComponentAmt = _drEntityStorage.GetScheduleDetails(schedule.ScheduleID).Sum(s => s.TotalAmt);
				if (totalComponentAmt != lineAmount.Cury)
				{
					throw new PXException(Messages.CannotReleaseOverriden, totalComponentAmt, lineAmount.Base, _currencyInfo.BaseCuryID);
				}
			}

			_dataProvider.DeleteAllDetails(schedule.ScheduleID);

			_schedule = schedule;
			_schedule.DocDate = scheduleParameters.DocDate;
			_schedule.BAccountID = scheduleParameters.BAccountID;
			_schedule.BAccountLocID = scheduleParameters.BAccountLocID;
			_schedule.FinPeriodID = scheduleParameters.FinPeriodID;
			_schedule.TranDesc = scheduleParameters.TranDesc;
			_schedule.IsCustom = false;
			_schedule.IsDraft = _isDraft;
			_schedule.IsSuspense = false;
			_schedule.BAccountType = _schedule.Module == BatchModule.AP ? BAccountType.VendorType : BAccountType.CustomerType;
			_schedule.TermStartDate = scheduleParameters.TermStartDate;
			_schedule.TermEndDate = scheduleParameters.TermEndDate;
			_schedule.ProjectID = scheduleParameters.ProjectID;
			_schedule.TaskID = scheduleParameters.TaskID;

			_schedule = _drEntityStorage.Update(_schedule);
			_schedule = _drEntityStorage.UpdateCuryInfo(_schedule, scheduleParameters.CuryInfoID);

			CreateScheduleDetails(scheduleParameters, lineAmount);

		}


		public static void RecalculateSchedule(DraftScheduleMaint graph)
		{
			try
			{
				graph.CurrentContext = DraftScheduleMaint.Context.Recalculate;
				RecalculateScheduleProc(graph);
			}
			finally
			{
				graph.CurrentContext = DraftScheduleMaint.Context.Normal;
			}
		}

		private static void RecalculateScheduleProc(DraftScheduleMaint graph)
		{
			var schedule = graph.Schedule.Current;

			bool isUnable = graph.Schedule.Cache.GetStatus(graph.Schedule.Current) != PXEntryStatus.Notchanged &&
				graph.Schedule.Cache.GetStatus(graph.Schedule.Current) != PXEntryStatus.Updated;

			if (isUnable || schedule.IsRecalculated == true || graph.Components.Any() == false)
			{
				return;
			}

			ARInvoice document = PXSelect<ARInvoice,
				Where<ARInvoice.docType, Equal<Required<ARRegister.docType>>,
					And<ARInvoice.refNbr, Equal<Required<ARRegister.refNbr>>>>>.Select(graph, schedule.DocType, schedule.RefNbr);

			PXSelectBase<DRSchedule> correspondingScheduleView = new PXSelect<
				DRSchedule,
				Where<
				DRSchedule.module, Equal<BatchModule.moduleAR>,
					And<DRSchedule.docType, Equal<Required<ARTran.tranType>>,
					And<DRSchedule.refNbr, Equal<Required<ARTran.refNbr>>>>>>
				(graph);

			DRSchedule correspondingSchedule = correspondingScheduleView.Select(document.DocType, document.RefNbr);

			var newDetails = new List<DRScheduleDetail>();


			var netLinesAmount = ASC606Helper.CalculateNetAmount(graph, document);
			int? defScheduleID = null;
			DRSchedule scheduleResult = null;

			if (netLinesAmount.Cury != 0m)
			{
				DRSingleProcess process = PXGraph.CreateInstance<DRSingleProcess>();
				process.CreateSingleSchedule(document, netLinesAmount, defScheduleID, true);

				scheduleResult = process.Schedule.Current;

				foreach (DRScheduleDetail detail in process.ScheduleDetail.Cache.Inserted)
				{
					newDetails.Add(detail);
				}
			}

			foreach (PXResult<DRScheduleDetail, DRSchedule> res in PXSelectJoin<DRScheduleDetail,
																	LeftJoin<DRSchedule,
																		On<DRScheduleDetail.scheduleID, Equal<DRSchedule.scheduleID>>>,
																	Where<
																		DRScheduleDetail.scheduleID, Equal<Current<DRSchedule.scheduleID>>>>.Select(graph))
			{
				DRScheduleDetail detail = (DRScheduleDetail)res;
				graph.Components.Delete(detail);
			}

			schedule.DocDate = scheduleResult.DocDate;
			schedule.BAccountID = scheduleResult.BAccountID;
			schedule.BAccountLocID = scheduleResult.BAccountLocID;
			schedule.FinPeriodID = scheduleResult.FinPeriodID;
			schedule.TranDesc = scheduleResult.TranDesc;
			schedule.IsCustom = false;
			schedule.BAccountType = schedule.Module == BatchModule.AP ? BAccountType.VendorType : BAccountType.CustomerType;
			schedule.TermStartDate = scheduleResult.TermStartDate;
			schedule.TermEndDate = scheduleResult.TermEndDate;
			schedule.ProjectID = scheduleResult.ProjectID;
			schedule.TaskID = scheduleResult.TaskID;
			schedule.CuryInfoID = scheduleResult.CuryInfoID;
			schedule.IsRecalculated = true;

			schedule = graph.Schedule.Update(schedule);

			foreach (DRScheduleDetail detail in newDetails)
			{
				graph.Components.Insert(detail);
			}
		}

		/// <summary>
		/// Reevaluates the provided schedule details' amounts, prorating
		/// them in order for their sum to match the given line amount.
		/// </summary>
		private void ReevaluateComponentAmounts(
			IEnumerable<DRScheduleDetail> scheduleDetails,
			decimal? lineAmount)
		{
			decimal? detailsTotal = scheduleDetails.Sum(scheduleDetail => scheduleDetail.TotalAmt);

			if (lineAmount != detailsTotal)
			{
				if (scheduleDetails.IsSingleElement())
				{
					scheduleDetails.Single().CuryTotalAmt = lineAmount;
					scheduleDetails.Single().CuryDefAmt = lineAmount;
				}
				else if (scheduleDetails.HasAtLeastTwoItems())
				{
					decimal correctedTotal = 0;

					scheduleDetails.SkipLast(1).ForEach(scheduleDetail =>
					{
						decimal correctedRaw = scheduleDetail.TotalAmt.Value * lineAmount.Value / detailsTotal.Value;
						decimal corrected = _roundingFunction(correctedRaw);

						correctedTotal += corrected;

						scheduleDetail.CuryTotalAmt = corrected;
						scheduleDetail.CuryDefAmt = corrected;
					});

					scheduleDetails.Last().CuryTotalAmt = lineAmount - correctedTotal;
					scheduleDetails.Last().CuryDefAmt = lineAmount - correctedTotal;
				}
			}
		}

		/// <summary>
		/// For a given schedule detail, reevaluates deferral transactions if they exist.
		/// </summary>
		private void ReevaluateTransactionAmounts(
			DRScheduleDetail scheduleComponent,
			DRDeferredCode componentDeferralCode,
			IEnumerable<DRScheduleTran> componentTransactions)
		{
			decimal? totalTransactionAmount = componentTransactions.Sum(transaction => transaction.Amount);

			if (totalTransactionAmount == scheduleComponent.TotalAmt)
			{
				return;
			}

			if (componentTransactions.IsSingleElement())
			{
				UpdateTransactionAmount(
					scheduleComponent,
					componentDeferralCode,
					componentTransactions.Single(),
					scheduleComponent.TotalAmt);
			}
			else if (componentTransactions.HasAtLeastTwoItems())
			{
				decimal correctedTotal = 0;

				componentTransactions.SkipLast(1).ForEach(transaction =>
				{
					decimal correctedAmountRaw = transaction.Amount.Value * scheduleComponent.TotalAmt.Value / totalTransactionAmount.Value;
					decimal correctedAmount = _roundingFunction(correctedAmountRaw);

					correctedTotal += correctedAmount;

					UpdateTransactionAmount(
						scheduleComponent,
						componentDeferralCode,
						transaction,
						correctedAmount);
				});

				UpdateTransactionAmount(
					scheduleComponent,
					componentDeferralCode,
					componentTransactions.Last(),
					scheduleComponent.TotalAmt - correctedTotal);
			}
		}

		/// <summary>
		/// Updates the transaction amount and performs the callback to
		/// <see cref="IDREntityStorage"/>, notifying about the update.
		/// </summary>
		private void UpdateTransactionAmount(
			DRScheduleDetail scheduleDetail,
			DRDeferredCode detailDeferralCode,
			DRScheduleTran transaction,
			decimal? newAmount)
		{
			DRScheduleTran oldTransaction = _drEntityStorage.CreateCopy(transaction);

			transaction.Amount = newAmount;

			_drEntityStorage.ScheduleTransactionModified(
				scheduleDetail,
				detailDeferralCode,
				oldTransaction,
				transaction);
		}

		private DRScheduleDetail CreateScheduleDetail(
			ARTran artran,
			INComponent component,
			string defCodeID,
			AccountSubaccountPair deferralAccountSubaccount,
			AccountSubaccountPair salesAccountSubaccount,
			bool isFlexibleMethod,
			bool isResidual = false)
		{
			FinPeriod detailFinPeriod = FinPeriodRepository
				.GetFinPeriodByMasterPeriodID(PXAccess.GetParentOrganizationID(artran.BranchID), artran.TranPeriodID).GetValueOrRaiseError();

			DRScheduleDetail scheduleDetail = new DRScheduleDetail();
			scheduleDetail.ScheduleID = _schedule.ScheduleID;
			scheduleDetail.BranchID = artran.BranchID;
			scheduleDetail.Module = _schedule.Module;
			scheduleDetail.DocType = _schedule.DocType;
			scheduleDetail.RefNbr = _schedule.RefNbr;
			scheduleDetail.LineNbr = artran.LineNbr;
			scheduleDetail.FinPeriodID = detailFinPeriod.FinPeriodID;
			scheduleDetail.TranPeriodID = detailFinPeriod.MasterFinPeriodID;
			scheduleDetail.BAccountID = _schedule.BAccountID;
			scheduleDetail.BAccountType = _schedule.Module == BatchModule.AP
																? BAccountType.VendorType
																: BAccountType.CustomerType;
			scheduleDetail.CreditLineNbr = 0;
			scheduleDetail.DocDate = _schedule.DocDate;

			scheduleDetail.AccountID = salesAccountSubaccount.AccountID;
			scheduleDetail.SubID = salesAccountSubaccount.SubID;
			scheduleDetail.DefAcctID = deferralAccountSubaccount.AccountID;
			scheduleDetail.DefSubID = deferralAccountSubaccount.SubID;

			scheduleDetail.IsCustom = false;
			scheduleDetail.IsResidual = isResidual;
			scheduleDetail.DefCode = defCodeID;

			if (String.IsNullOrEmpty(defCodeID))
			{
				scheduleDetail.IsOpen = false;
				scheduleDetail.Status = _isDraft ? DRScheduleStatus.Draft : DRScheduleStatus.Closed;
			}
			else
		{
			string uom = artran.UOM;
			decimal? qty = artran.Qty;
			decimal coTermRate = 1m;
				DateTime? startDate = artran.DRTermStartDate;
				DateTime? endDate = artran.DRTermEndDate;

			if (component?.ComponentID != null)
			{
				uom = component.UOM;
				if (artran.UOM == component.UOM)
				{
					qty = artran.Qty * component.Qty;
				}
				else
				{
					var qtyBase = _salesPriceProvider.GetQuantityInBaseUOMs(artran);
					qty = qtyBase * component.Qty;
				}

					if (isFlexibleMethod == true && component.OverrideDefaultTerm != true)
					{
						var term = new DRTerms.Term(component.DefaultTerm, component.DefaultTermUOM);
						endDate = term.Delay(startDate).Value;
					}
			}

			if (isFlexibleMethod == true)
			{
					coTermRate = ((endDate.Value - startDate.Value).Days + 1.0m) / 365.0m;
			}

				scheduleDetail.UOM = uom;
				scheduleDetail.Qty = qty;
				scheduleDetail.TermStartDate = startDate;
				scheduleDetail.TermEndDate = endDate;
				scheduleDetail.CoTermRate = coTermRate;

				scheduleDetail.IsOpen = true;
				scheduleDetail.Status = _isDraft ? DRScheduleStatus.Draft : DRScheduleStatus.Open;
			}

			return scheduleDetail;
		}

		private AccountSubaccountPair GetDeferralAccountSubaccount(
			DRDeferredCode deferralCode,
			InventoryItem item,
			DRProcess.DRScheduleParameters scheduleParameters,
			int? origSubID)
		{
			int? accountID = deferralCode.AccountID;

			string subaccountCD = null;
			int? subaccountID = null;

			if (deferralCode.AccountSource == DeferralAccountSource.Item)
			{
				accountID = item != null ? item.DeferralAcctID : subaccountID;
				// this is fishy. subID is always null at this point.
			}

			if (deferralCode.CopySubFromSourceTran == true)
			{
				subaccountID = origSubID;
			}
			else if (scheduleParameters.Module == BatchModule.AP)
			{
				int? itemSubID = item?.DeferralSubID;

				Location location = _businessAccountProvider
					.GetLocation(scheduleParameters.BAccountID, scheduleParameters.BAccountLocID);

				int? locationSubID = location?.VExpenseSubID;

				EPEmployee employee = _businessAccountProvider
					.GetEmployee(scheduleParameters.EmployeeID);

				int? employeeSubaccountID = employee?.ExpenseSubID;

				subaccountCD = _subaccountProvider.MakeSubaccount<DRScheduleDetail.subID>(
					deferralCode.DeferralSubMaskAP,
					new object[] { locationSubID, itemSubID, employeeSubaccountID, deferralCode.SubID },
					new[]
					{
						typeof(Location.vExpenseSubID),
						typeof(InventoryItem.deferralSubID),
						typeof(EPEmployee.expenseSubID),
						typeof(DRDeferredCode.subID)
					});
			}
			else if (scheduleParameters.Module == BatchModule.AR)
			{
				int? itemSubID = item?.DeferralSubID;

				Location location = _businessAccountProvider
					.GetLocation(scheduleParameters.BAccountID, scheduleParameters.BAccountLocID);

				int? locationSubaccountID = location?.CSalesSubID;

				EPEmployee employee = _businessAccountProvider
					.GetEmployee(scheduleParameters.EmployeeID);

				int? employeeSubaccountID = employee?.SalesSubID;

				SalesPerson salesPerson = _businessAccountProvider
					.GetSalesPerson(scheduleParameters.SalesPersonID);

				int? salesPersonSubaccountID = salesPerson?.SalesSubID;

				subaccountCD = _subaccountProvider.MakeSubaccount<DRScheduleDetail.subID>(
					deferralCode.DeferralSubMaskAR,
					new object[] { locationSubaccountID, itemSubID, employeeSubaccountID, deferralCode.SubID, salesPersonSubaccountID },
					new[]
					{
						typeof(Location.cSalesSubID),
						typeof(InventoryItem.deferralSubID),
						typeof(EPEmployee.salesSubID),
						typeof(DRDeferredCode.subID),
						typeof(SalesPerson.salesSubID)
					});
			}

			if (subaccountCD != null)
			{
				subaccountID = _subaccountProvider.GetSubaccountID(subaccountCD);
			}

			return new AccountSubaccountPair(accountID, subaccountID);
		}

		private AccountSubaccountPair GetSalesAccountSubaccount(
			DRDeferredCode deferralCode,
			InventoryItem item,
			INComponent component,
			ARTran transaction)
		{
			int? accountID = transaction.AccountID;
			int? subaccountID = transaction.SubID;

			if (deferralCode.MultiDeliverableArrangement == true)
			{
				accountID = component.SalesAcctID;
			}

			if (deferralCode.MultiDeliverableArrangement == true && item.UseParentSubID == true)
			{
				subaccountID = component.SalesSubID;
			}

			return new AccountSubaccountPair(accountID, subaccountID);
		}
	}
}
