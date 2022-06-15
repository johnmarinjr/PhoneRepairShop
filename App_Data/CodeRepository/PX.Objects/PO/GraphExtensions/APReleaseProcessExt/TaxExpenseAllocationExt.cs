using System;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PO.LandedCosts;
using PX.Objects.TX;
using PX.Objects.Common;
using PX.Objects.PO.Services.AmountDistribution;
using Amount = PX.Objects.AR.ARReleaseProcess.Amount;

namespace PX.Objects.PO.GraphExtensions.APReleaseProcessExt
{
	public class TaxExpenseAllocationExt : PXGraphExtension<UpdatePOOnRelease, APReleaseProcess.MultiCurrency, APReleaseProcess>
	{
		#region Types

		public delegate List<APRegister> releaseInvoiceDelegate(
			JournalEntry je,
			ref APRegister doc,
			PXResult<APInvoice, CurrencyInfo, Terms, Vendor> res,
			bool isPrebooking,
			out List<INRegister> inDocs);

		public delegate void getItemCostTaxAccountDelegate(APRegister apdoc, Tax tax, APTran apTran, APTaxTran apTaxTran, out int? accountID, out int? subID);

		protected class TaxByPOReceiptLine : IAmountItem
		{
			public class CalculationParameter
			{
				public bool UseBaseUom { get; set; }
			}

			public POReceiptLine POReceiptLine { get; set; }
			public POAccrualSplit POAccrualSplit { get; set; }
			public CalculationParameter CalcParameter { get; set; }

			#region IAmountItem members
			public decimal Weight => (CalcParameter.UseBaseUom ? POAccrualSplit.BaseAccruedQty : POAccrualSplit.AccruedQty) ?? 0m;
			public decimal? Amount
			{
				get => POAccrualSplit.TaxAccruedCost;
				set => POAccrualSplit.TaxAccruedCost = value;
			}
			public decimal? CuryAmount
			{
				get;
				set;
			}
			#endregion
		}

		protected class TaxByLine
		{
			public TaxByLine(APTran apTran, InventoryItem item, Amount taxAmt)
			{
				APTran = apTran;
				Item = item;
				TaxAmt = taxAmt;
			}
			public APTran APTran { get; }
			public InventoryItem Item { get; }
			public Amount TaxAmt { get; set; }
			public List<TaxByPOReceiptLine> Splits { get; set; }
		}
		#endregion

		#region Properties and Fields

		public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

		[InjectDependency]
		public AmountDistributionFactory AmountDistributionFactory { get; set; }
		#endregion

		#region Override

		[PXOverride]
		public virtual void GetItemCostTaxAccount(APRegister apdoc, Tax tax, APTran apTran, APTaxTran taxTran, out int? accountID, out int? subID, getItemCostTaxAccountDelegate baseMethod)
		{
			if (!IsExtensionEnabled(apdoc) || !IsItemCostTax(tax))
			{
				baseMethod(apdoc, tax, apTran, taxTran, out accountID, out subID);
				return;
			}
			
			InventoryItem item = InventoryItem.PK.Find(Base, apTran.InventoryID);

			if (POLineType.IsStock(apTran.LineType) && !POLineType.IsProjectDropShip(apTran.LineType) && (item?.ValMethod == INValMethod.Standard || apTran.ReceiptType == POReceiptType.POReturn))
				GetTaxReasonCodeAccount(apTran, item, out accountID, out subID);
			else if ((POLineType.IsNonStock(apTran.LineType) || POLineType.IsProjectDropShip(apTran.LineType)) && !POLineType.IsService(apTran.LineType) && apTran.POAccrualRefNoteID != null)
				GetCOGSAccount(apTran, item, out accountID, out subID);
			else
				baseMethod(apdoc, tax, apTran, taxTran, out accountID, out subID);
		}

		[PXOverride]
		public virtual List<APRegister> ReleaseInvoice(
			JournalEntry je,
			ref APRegister doc,
			PXResult<APInvoice, CurrencyInfo, Terms, Vendor> res,
			bool isPrebooking,
			out List<INRegister> inDocs,
			releaseInvoiceDelegate baseMethod)
		{
			var result = baseMethod(je, ref doc, res, isPrebooking, out inDocs);
			CalculateTaxExpenseAllocation(doc, inDocs);
			return result;
		}

		#endregion

		#region Implementation

		protected virtual bool IsExtensionEnabled(APRegister apdoc)
		{
			return apdoc.Released != true &&
				apdoc.DocType.IsIn(APDocType.DebitAdj, APDocType.Invoice) == true;
		}

		protected virtual bool IsItemCostTax(Tax tax)
		{
			return tax.ReportExpenseToSingleAccount != true &&
				(tax.TaxType.IsIn(CSTaxType.Sales, CSTaxType.Use) || (tax.TaxType == CSTaxType.VAT && tax.DeductibleVAT == true));
		}

		protected virtual void GetTaxReasonCodeAccount(APTran apTran, InventoryItem item, out int? accountID, out int? subID)
		{
			if (Base.posetup?.TaxReasonCodeID == null)
				throw new Common.Exceptions.FieldIsEmptyException(Base.Caches<POSetup>(), Base.posetup, typeof(POSetup.taxReasonCodeID));

			ReasonCode reasonCode = ReasonCode.PK.Find(Base, Base.posetup?.TaxReasonCodeID);
			if (reasonCode == null)
				throw new PXException(AP.Messages.ReasonCodeCannotNotFound, Base.posetup?.TaxReasonCodeID);

			INPostClass postclass = INPostClass.PK.Find(Base, item.PostClassID);
			if (postclass == null)
				throw new Common.Exceptions.RowNotFoundException(Base.Caches<INPostClass>(), item.PostClassID);

			INSite site = INSite.PK.Find(Base, apTran.SiteID);
			if (site == null)
				throw new Common.Exceptions.RowNotFoundException(Base.Caches<INSite>(), apTran.SiteID);

			accountID = reasonCode.AccountID;
			if (accountID == null)
				throw new Common.Exceptions.FieldIsEmptyException(Base.Caches<ReasonCode>(), reasonCode, typeof(ReasonCode.accountID), reasonCode.ReasonCodeID);

			if (reasonCode.SubID == null)
				throw new Common.Exceptions.FieldIsEmptyException(Base.Caches<ReasonCode>(), reasonCode, typeof(ReasonCode.subID), reasonCode.ReasonCodeID);

			if (reasonCode.SubMaskInventory == null)
				throw new Common.Exceptions.FieldIsEmptyException(Base.Caches<ReasonCode>(), reasonCode, typeof(ReasonCode.subMaskInventory), reasonCode.ReasonCodeID);

			subID = INReleaseProcess.GetReasonCodeSubID(Base, reasonCode, item, site, postclass);

			if (subID == null)
				throw new Common.Exceptions.FieldIsEmptyException(Base.Caches<ReasonCode>(), reasonCode, typeof(ReasonCode.subID), reasonCode.ReasonCodeID);
		}

		protected virtual void GetCOGSAccount(APTran apTran, InventoryItem item, out int? accountID, out int? subID)
		{
			if (apTran.POAccrualType == POAccrualType.Order)
			{
				POLineUOpen poline = Base2.poOrderLineUPD.Select(apTran.POOrderType, apTran.PONbr, apTran.POLineNbr);

				if (poline == null)
					throw new Common.Exceptions.RowNotFoundException(Base2.poAccrualUpdate.Cache,
						apTran.POAccrualRefNoteID, apTran.POAccrualLineNbr, apTran.POAccrualType);

				accountID = poline.ExpenseAcctID;
				subID = poline.ExpenseSubID;
			}
			else
			{
				POReceiptLineR1 poreceiptline = Base2.poReceiptLineUPD.Select(apTran.ReceiptType, apTran.ReceiptNbr, apTran.ReceiptLineNbr);

				if (poreceiptline == null)
					throw new Common.Exceptions.RowNotFoundException(Base2.poAccrualUpdate.Cache,
						apTran.POAccrualRefNoteID, apTran.POAccrualLineNbr, apTran.POAccrualType);

				accountID = poreceiptline.ExpenseAcctID;
				subID = poreceiptline.ExpenseSubID;
			}
		}

		protected virtual void CalculateTaxExpenseAllocation(APRegister apdoc, List<INRegister> inDocs)
		{
			if (!IsExtensionEnabled(apdoc))
				return;

			var taxByLines = CollectTaxes(apdoc);
			ApplyTaxAmtToPOAccrualStatuses(taxByLines, apdoc);
			CollectPOReceiptLines(taxByLines);
			CreateINAdjustment(taxByLines, apdoc, inDocs);
		}

		protected virtual IDictionary<int, TaxByLine> CollectTaxes(APRegister apdoc)
		{
			PXResultset<APTran> apTranTaxes = Base.APTran_TranType_RefNbr.Select(apdoc.DocType, apdoc.RefNbr);

			PXCache apTaxCache = Base.Caches<APTax>();
			
			Dictionary<int, TaxByLine> aptaxes =
				apTranTaxes
				.AsEnumerable()
				.Select(t => new {
					APTran = (APTran)t,
					InventoryItem = t.GetItem<InventoryItem>(),
					Tax = t.GetItem<Tax>(),
					APTax = (APTax)apTaxCache.Locate(t.GetItem<APTax>()) ?? t.GetItem<APTax>()
				})
				.Where(
					t => IsItemCostTax(t.Tax) &&
					POLineType.IsStock(t.APTran.LineType) &&
					t.InventoryItem.ValMethod != INValMethod.Standard &&
					t.APTran.ReceiptType != POReceiptType.POReturn)
				.GroupBy(t => t.APTran.LineNbr)
				.ToDictionary(
					r => (int)r.Key, // LineNbr
					r => new TaxByLine(
						r.First().APTran,
						r.First().InventoryItem,
						new Amount(
							r.Sum(a => GetTaxAmount(apdoc, a.Tax, a.APTax, true)),
							r.Sum(a => GetTaxAmount(apdoc, a.Tax, a.APTax, false)))));

			return aptaxes;
		}

		protected virtual decimal? GetTaxAmount(APRegister apdoc, Tax tax, APTax aptax, bool cury)
		{
			decimal? expenseAmt = cury ? aptax.CuryExpenseAmt : aptax.ExpenseAmt;
			decimal? taxAmt = cury ? aptax.CuryTaxAmt : aptax.TaxAmt;
			decimal? retainedTax = cury ? aptax.CuryRetainedTaxAmt : aptax.RetainedTaxAmt;

			return (tax.TaxType == CSTaxType.VAT ? expenseAmt : taxAmt)
				+ (apdoc.RetainageApply == true ? (retainedTax ?? 0m) : 0m);
		} 

		protected virtual void ApplyTaxAmtToPOAccrualStatuses(IDictionary<int, TaxByLine> taxByLines, APRegister apdoc)
		{
			var calcParameter = new TaxByPOReceiptLine.CalculationParameter();

			var poaccrualSplitsGroupedByKeyExceptPOReceipt = Base2.poAccrualSplitUpdate.Cache.Inserted.RowCast<POAccrualSplit>()
				.Where(s => s.APRefNbr == apdoc.RefNbr && s.APDocType == apdoc.DocType && taxByLines.ContainsKey((int)s.APLineNbr))
				.GroupBy(s => new Tuple<Guid?, int?, string, int?>(s.RefNoteID, s.LineNbr, s.Type, s.APLineNbr))
				.ToDictionary(s => s.Key, s => new
				{
					SumOfAccruedQty = s.Sum(split => split.AccruedQty),
					SumOfBaseAccruedQty = s.Sum(split => split.BaseAccruedQty),
					CalcParameter = calcParameter = new TaxByPOReceiptLine.CalculationParameter() { UseBaseUom = s.Any(split => split.AccruedQty == null) ? true : false },
					Splits = s.Select(split => new TaxByPOReceiptLine() { POAccrualSplit = split, CalcParameter = calcParameter }).ToList(),
				});

			foreach (var taxByLine in taxByLines.Values)
			{
				if (taxByLine.APTran.Qty == 0m ||
					taxByLine.APTran.POAccrualRefNoteID == null)
					continue;

				var poAccrualStatus = Base2.poAccrualUpdate.Locate(new POAccrualStatus()
				{
					RefNoteID = taxByLine.APTran.POAccrualRefNoteID,
					LineNbr = taxByLine.APTran.POAccrualLineNbr,
					Type = taxByLine.APTran.POAccrualType
				});

				if (poAccrualStatus == null)
					throw new Common.Exceptions.RowNotFoundException(Base2.poAccrualUpdate.Cache,
						taxByLine.APTran.POAccrualRefNoteID,
						taxByLine.APTran.POAccrualLineNbr,
						taxByLine.APTran.POAccrualType);

				poAccrualStatus.BilledTaxAdjCost += taxByLine.TaxAmt.Base * taxByLine.APTran.Sign ?? 0m;

				if (poAccrualStatus.BillCuryID == apdoc.CuryID)
					poAccrualStatus.CuryBilledTaxAdjCost += taxByLine.TaxAmt.Cury * taxByLine.APTran.Sign ?? 0m;
				else
					poAccrualStatus.CuryBilledTaxAdjCost = null;

				var detail = Base2.PrepareAPTranAccrualDetail(taxByLine.APTran, apdoc);
				if (detail != null)
				{
					detail.TaxAccruedCost += taxByLine.TaxAmt.Base * taxByLine.APTran.Sign ?? 0m;
					Base2.poAccrualDetailUpdate.Update(detail);
				}

				var poaccrrualSplitKey = new Tuple<Guid?, int?, string, int?>(
					taxByLine.APTran.POAccrualRefNoteID, taxByLine.APTran.POAccrualLineNbr, taxByLine.APTran.POAccrualType, taxByLine.APTran.LineNbr);

				if (!poaccrualSplitsGroupedByKeyExceptPOReceipt.ContainsKey(poaccrrualSplitKey))
					continue;

				var splitContainer = poaccrualSplitsGroupedByKeyExceptPOReceipt[poaccrrualSplitKey];
				taxByLine.Splits = splitContainer.Splits;

				Amount taxAmount = taxByLine.TaxAmt;

				if (!splitContainer.CalcParameter.UseBaseUom)
				{
					taxAmount *= splitContainer.SumOfAccruedQty;
					taxAmount /= (decimal)taxByLine.APTran.Qty;
				}
				else
				{
					taxAmount *= splitContainer.SumOfBaseAccruedQty;
					taxAmount /= (decimal)taxByLine.APTran.BaseQty;
				}

				AmountDistributionFactory.CreateService(DistributionMethod.RemainderToBiggestLine, new DistributionParameter<TaxByPOReceiptLine>()
				{
					Items = splitContainer.Splits,
					Amount = taxAmount.Base,
					CuryAmount = taxAmount.Cury,
					CuryRow = apdoc,
					CacheOfCuryRow = Base.APDocument.Cache,
					OnValueCalculated = (item, amount, curyAmount) => AddAmountToPOAccrual(poAccrualStatus, item, amount),
					OnRoundingDifferenceApplied = (item, newAmount, curyNewAmount, oldAmount, curyOldAmount) => AddAmountToPOAccrual(poAccrualStatus, item, newAmount - oldAmount),
				}).Distribute();

				Base2.poAccrualUpdate.Cache.MarkUpdated(poAccrualStatus);
			}
		}

		protected virtual TaxByPOReceiptLine AddAmountToPOAccrual(POAccrualStatus poAccrualStatus, TaxByPOReceiptLine split, decimal? taxAmtBase)
		{
			poAccrualStatus.ReceivedTaxAdjCost += taxAmtBase;

			split.POAccrualSplit = Base2.poAccrualSplitUpdate.Update(split.POAccrualSplit);

			var receiptLineDetail = poAccrualStatus.LineType.IsNotIn(POLineType.Service, POLineType.Freight)
				? Base2.FindPOReceiptLineAccrualDetail(split.POAccrualSplit)
				: null;
			if (receiptLineDetail != null)
			{
				receiptLineDetail.TaxAccruedCost += taxAmtBase;
				Base2.poAccrualDetailUpdate.Update(receiptLineDetail);
			}

			var apTranDetail = Base2.poAccrualDetailUpdate.Cache
				.Inserted
				.OfType<POAccrualDetail>()
				.FirstOrDefault(x => x.LineNbr == split.POAccrualSplit.APLineNbr);
			if (apTranDetail != null)
			{
				apTranDetail.TaxAdjAmt += taxAmtBase;
				Base2.poAccrualDetailUpdate.Update(apTranDetail);
			}

			return split;
		}

		protected virtual void CollectPOReceiptLines(IDictionary<int, TaxByLine> taxByLines)
		{
			var adjustmentLines = new List<AllocationServiceBase.POReceiptLineAdjustment>();

			var taxBylinesRelatedToReceiptLine = taxByLines.Values.Where(line => line.Splits != null);

			foreach (TaxByLine taxByLine in taxBylinesRelatedToReceiptLine)
			{
				foreach (TaxByPOReceiptLine split in taxByLine.Splits)
				{
					var receiptLine = (POReceiptLineR1)Base2.poReceiptLineUPD.Select(
						split.POAccrualSplit.POReceiptType, split.POAccrualSplit.POReceiptNbr, split.POAccrualSplit.POReceiptLineNbr);

					if (receiptLine == null)
						throw new Common.Exceptions.RowNotFoundException(Base2.poReceiptLineUPD.Cache,
							split.POAccrualSplit.POReceiptType, split.POAccrualSplit.POReceiptNbr, split.POAccrualSplit.POReceiptLineNbr);

					split.POReceiptLine = PropertyTransfer.Transfer(receiptLine, new POReceiptLine());
				}
			}
		}

		protected virtual void CreateINAdjustment(IDictionary<int, TaxByLine> taxByLines, APRegister apdoc, List<INRegister> inDocs)
		{
			var adjustmentLines = CollectINAdjustmentLines(taxByLines, apdoc);
			

			if (VerifyAdjustments(apdoc, adjustmentLines))
			{
				INRegister newINAdjustment = CreateINAdjustment(apdoc, adjustmentLines);

				OnTaxAdjustmentCreated(newINAdjustment, apdoc, adjustmentLines);
				
				inDocs.Add(newINAdjustment);
			}
		}

		protected virtual List<AllocationServiceBase.POReceiptLineAdjustment> CollectINAdjustmentLines(IDictionary<int, TaxByLine> taxByLines, APRegister apdoc)
		{
			var adjustmentLines = new List<AllocationServiceBase.POReceiptLineAdjustment>();

			foreach (var line in taxByLines.Values.Where(s => s.Splits != null))
			{
				foreach (var split in line.Splits.Where(s => s.POReceiptLine != null))
				{
					decimal totalToDistribute = split.POAccrualSplit.TaxAccruedCost ?? 0m;

					totalToDistribute -= PurchasePriceVarianceAllocationService.Instance.AllocateOverRCTLine(Base, adjustmentLines,
						split.POReceiptLine, totalToDistribute, line.APTran.BranchID);
					PurchasePriceVarianceAllocationService.Instance.AllocateRestOverRCTLines(adjustmentLines, totalToDistribute);
				}
			}

			return adjustmentLines;
		}

		protected virtual bool VerifyAdjustments(APRegister apdoc, IEnumerable<AllocationServiceBase.POReceiptLineAdjustment> adjustments)
		{
			if (apdoc.DocType == APDocType.DebitAdj)
			{
				var origdoc = APInvoice.PK.Find(Base, apdoc.OrigDocType, apdoc.OrigRefNbr);
				if (origdoc?.TaxCostINAdjRefNbr != null)
				{
					var origINAdjustment = INRegister.PK.Find(Base, INDocType.Adjustment, origdoc.TaxCostINAdjRefNbr);
					if (origINAdjustment != null && origINAdjustment.Released != true)
						throw new PXException(Messages.BillCannotReverseItHasNotReleasedINAdjustment, origdoc.TaxCostINAdjRefNbr, apdoc.OrigRefNbr);
				}
			}

			return adjustments.Any();
		}

		protected virtual INRegister CreateINAdjustment(APRegister apdoc, List<AllocationServiceBase.POReceiptLineAdjustment> adjustmentLines)
		{
			if (Base.posetup?.TaxReasonCodeID == null)
				throw new Common.Exceptions.FieldIsEmptyException(Base.Caches<POSetup>(), Base.posetup, typeof(POSetup.taxReasonCodeID));

			ReasonCode reasonCode = ReasonCode.PK.Find(Base, Base.posetup?.TaxReasonCodeID);
			if (reasonCode == null)
				throw new PXException(AP.Messages.ReasonCodeCannotNotFound, Base.posetup?.TaxReasonCodeID);

			INAdjustmentEntry inGraph = PXGraph.CreateInstance<INAdjustmentEntry>();
			inGraph.Clear();

			inGraph.FieldVerifying.AddHandler<INTran.inventoryID>((PXCache sender, PXFieldVerifyingEventArgs e) => { e.Cancel = true; });
			inGraph.FieldVerifying.AddHandler<INTran.origRefNbr>((PXCache sender, PXFieldVerifyingEventArgs e) => { e.Cancel = true; });

			inGraph.insetup.Current.RequireControlTotal = false;
			inGraph.insetup.Current.HoldEntry = false;

			INRegister newdoc = new INRegister();
			newdoc.DocType = INDocType.Adjustment;
			newdoc.OrigModule = BatchModule.AP;
			newdoc.OrigRefNbr = apdoc.RefNbr;
			newdoc.SiteID = null;
			newdoc.TranDate = apdoc.DocDate;
			newdoc.FinPeriodID = apdoc.FinPeriodID;
			newdoc.BranchID = apdoc.BranchID;
			newdoc.IsTaxAdjustmentTran = true;
			inGraph.adjustment.Insert(newdoc);

			var INAdjustmentFactory = GetINAdjustmentFactory(inGraph);
			INAdjustmentFactory.CreateAdjustmentTran(adjustmentLines, Base.posetup.TaxReasonCodeID);
			inGraph.Save.Press();

			return inGraph.adjustment.Current;
		}

		protected virtual void OnTaxAdjustmentCreated(INRegister adjustment, APRegister apdoc, List<AllocationServiceBase.POReceiptLineAdjustment> adjustmentLines)
        {
			apdoc.TaxCostINAdjRefNbr = adjustment.RefNbr;

			var details = Base2.poAccrualDetailUpdate.Cache
					.Inserted
					.OfType<POAccrualDetail>()
					.Where(x =>
						x.APDocType == apdoc.DocType
						&& x.APRefNbr == apdoc.RefNbr
						&& x.TaxAdjAmt != 0)
					.ToList();

			foreach (var detail in details)
            {
				detail.TaxAdjRefNbr = adjustment.RefNbr;
				detail.TaxAdjPosted = false;
				Base2.poAccrualDetailUpdate.Update(detail);

				var status = new POAccrualStatus
				{
					RefNoteID = detail.POAccrualRefNoteID,
					LineNbr = detail.POAccrualLineNbr,
					Type = detail.POAccrualType
				};
				status = Base2.poAccrualUpdate.Locate(status);
				if (status != null)
				{
					status.UnreleasedTaxAdjCntr++;
					Base2.poAccrualUpdate.Update(status);
				}
			}
		}

		protected virtual PurchasePriceVarianceINAdjustmentFactory GetINAdjustmentFactory(INAdjustmentEntry inGraph)
			=> new PurchasePriceVarianceINAdjustmentFactory(inGraph);
		#endregion
	}
}
