using PX.Common;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PO.GraphExtensions.POReceiptEntryExt
{
	public class UpdatePOOnRelease: BaseUpdatePOAccrual<POReceiptEntry.MultiCurrency, POReceiptEntry, POReceipt>
	{
		public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.inventory>() || PXAccess.FeatureInstalled<FeaturesSet.pOReceiptsWithoutInventory>();

		/// <summary>
		/// Overrides <see cref="Extensions.MultiCurrency.MultiCurrencyGraph{POReceiptEntry, POReceipt}.GetTrackedExceptChildren"/>
		/// </summary>
		[PXOverride]
		public virtual PXSelectBase[] GetTrackedExceptChildren(Func<PXSelectBase[]> baseImpl)
		{
			return baseImpl().
				Union(new PXSelectBase[]
					{
						poLinesOpenUPD
					})
				.ToArray();
		}

		#region Views

		public PXSelect<POAccrualStatus> poAccrualUpdate;
		public PXSelect<POAccrualDetail> poAccrualDetailUpdate;
		public PXSelect<POAccrualSplit> poAccrualSplitUpdate;

		public PXSelect<POLineUOpen> poLinesOpenUPD;

		public PXSelect<APTranReceiptUpdate,
				Where<APTranReceiptUpdate.pOAccrualType, Equal<Current<POAccrualStatus.type>>,
					And<APTranReceiptUpdate.pOOrderType, Equal<Current<POAccrualStatus.orderType>>,
					And<APTranReceiptUpdate.pONbr, Equal<Current<POAccrualStatus.orderNbr>>,
					And<APTranReceiptUpdate.pOLineNbr, Equal<Current<POAccrualStatus.orderLineNbr>>,
					And<APTranReceiptUpdate.released, Equal<True>,
					And<APTranReceiptUpdate.unreceivedQty, Greater<decimal0>,
					And<APTranReceiptUpdate.baseUnreceivedQty, Greater<decimal0>>>>>>>>> apTranUpdate;

		#endregion

		#region Cache Attached

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[POLineRPlanID(typeof(POOrder.noteID), typeof(POOrder.hold))]
		protected virtual void POLineUOpen_PlanID_CacheAttached(PXCache sender)
		{
		}

		#endregion

		#region Receipt release

		/// <summary>
		/// Overrides <see cref="POReceiptEntry.ReleaseReceiptLine(POReceiptLine, POLineUOpen, POOrder)"/>
		/// </summary>
		[PXOverride]
		public virtual void ReleaseReceiptLine(POReceiptLine line, POLineUOpen poLine, POOrder poOrder, Action<POReceiptLine, POLineUOpen, POOrder> baseImpl)
		{
			if (line.ReceiptType == POReceiptType.TransferReceipt)
			{
				SetReceiptCostFinal(line, null);
			}
			else
			{
				POAccrualStatus poAccrual = POAccrualStatus.PK.FindDirty(Base, line.POAccrualRefNoteID, line.POAccrualLineNbr, line.POAccrualType);
				SetReceiptCostFinal(line, poAccrual);
				UpdatePOAccrualStatus(poAccrual, poLine, line, poOrder, Base.Document.Current);
				UpdatePOReceiptLineAccrualDetail(line);
			}

			baseImpl(line, poLine, poOrder);
		}

		/// <summary>
		/// Extends <see cref="POReceiptEntry.UpdateReceiptLineOnRelease(PXResult{POReceiptLine}, POLineUOpen)"/>
		/// </summary>
		[PXOverride]
		public virtual void UpdateReceiptLineOnRelease(PXResult<POReceiptLine> row, POLineUOpen pOLine)
		{
			UpdatePOLineOnReceipt(row, pOLine);
		}

		/// <summary>
		/// Overrides <see cref="POReceiptEntry.UpdateReceiptReleased"/>
		/// </summary>
		[PXOverride]
		public virtual POReceipt UpdateReceiptReleased(INRegister inRegister, Func<INRegister, POReceipt> baseImpl)
		{
			var receipt = baseImpl(inRegister);

			if (receipt.ReceiptType != POReceiptType.TransferReceipt
				&& (inRegister != null || receipt.POType == POOrderType.DropShip))
				IncUnreleasedReceiptCntr(receipt);

			return receipt;
		}

		public virtual POLineUOpen UpdatePOLineOnReceipt(PXResult<POReceiptLine> res, POLineUOpen poLine)
		{
			if (poLine == null || string.IsNullOrEmpty(poLine.OrderType) || string.IsNullOrEmpty(poLine.OrderNbr) || poLine.LineNbr == null)
			{
				return poLine;
			}

			POReceiptLine line = res;
			POOrder poOrder = PXResult.Unwrap<POOrder>(res);
			poLine = PXCache<POLineUOpen>.CreateCopy(poLine);

			decimal delta = line.ReceiptQty ?? Decimal.Zero;
			if (line.InventoryID != null && !String.IsNullOrEmpty(line.UOM) && !String.IsNullOrEmpty(poLine.UOM))
				delta = INUnitAttribute.ConvertFromBase(Base.Caches[typeof(POReceiptLine)], line.InventoryID, poLine.UOM, line.BaseReceiptQty.Value, INPrecision.QUANTITY);

			poLine.CompletedQty += delta * line.InvtMult;

			if (poLine.Completed != true || poLine.Closed != true)
			{
				bool completePOLine = false,
					closePOLine = false;

				bool unreleasedRcptExists = PXSelectReadonly<POReceiptLine,
					Where<POReceiptLine.pOType, Equal<Required<POReceiptLine.pOType>>,
						And<POReceiptLine.pONbr, Equal<Required<POReceiptLine.pONbr>>,
						And<POReceiptLine.pOLineNbr, Equal<Required<POReceiptLine.pOLineNbr>>,
						And<POReceiptLine.released, Equal<False>,
						And<
							Where<POReceiptLine.receiptType, NotEqual<Required<POReceiptLine.receiptType>>,
							Or<POReceiptLine.receiptNbr, NotEqual<Required<POReceiptLine.receiptNbr>>>>>>>>>>
					.Select(Base, line.POType, line.PONbr, line.POLineNbr, line.ReceiptType, line.ReceiptNbr).Count > 0;

				if (!unreleasedRcptExists && poLine.AllowComplete == true)
				{
					completePOLine = true;
				}

				if (!unreleasedRcptExists && poLine.POAccrualType == POAccrualType.Order)
				{
					var poLineAccrualStatus = GetAccrualStatusSummary(poLine);
					bool qtyCoincide = (poLineAccrualStatus.BilledUOM != null && poLineAccrualStatus.BilledUOM == poLineAccrualStatus.ReceivedUOM)
						? (poLineAccrualStatus.BilledQty == poLineAccrualStatus.ReceivedQty)
						: (poLineAccrualStatus.BaseBilledQty == poLineAccrualStatus.BaseReceivedQty);
					bool closePOLineByQty = (poLine.CompletePOLine == CompletePOLineTypes.Quantity);
					bool billedEnough;
					if (closePOLineByQty)
					{
						if (completePOLine)
						{
							billedEnough = true;
						}
						else
						{
							billedEnough = (poLineAccrualStatus.BilledUOM != null && poLineAccrualStatus.BilledUOM == poLine.UOM)
								? (poLine.OrderQty * poLine.RcptQtyThreshold / 100m <= poLineAccrualStatus.BilledQty)
								: (poLine.BaseOrderQty * poLine.RcptQtyThreshold / 100m <= poLineAccrualStatus.BaseBilledQty);
						}
					}
					else
					{
						if (poLineAccrualStatus.BillCuryID != null && poLineAccrualStatus.BillCuryID == poOrder.CuryID)
						{
							decimal? amountThreshold = (poLine.CuryExtCost + poLine.CuryRetainageAmt) * poLine.RcptQtyThreshold / 100m;
							billedEnough = amountThreshold != null && poLineAccrualStatus.CuryBilledAmt != null
								&& Math.Sign(amountThreshold.Value) == Math.Sign(poLineAccrualStatus.CuryBilledAmt.Value)
								&& Math.Abs(amountThreshold.Value) <= Math.Abs(poLineAccrualStatus.CuryBilledAmt.Value);
						}
						else
						{
							decimal? amountThreshold = (poLine.ExtCost + poLine.RetainageAmt) * poLine.RcptQtyThreshold / 100m;
							billedEnough = amountThreshold != null && poLineAccrualStatus.BilledAmt != null
								&& Math.Sign(amountThreshold.Value) == Math.Sign(poLineAccrualStatus.BilledAmt.Value)
								&& Math.Abs(amountThreshold.Value) <= Math.Abs(poLineAccrualStatus.BilledAmt.Value);
						}
					}
					if (qtyCoincide && billedEnough && (completePOLine || !closePOLineByQty))
					{
						completePOLine = closePOLine = true;
					}
				}

				if (completePOLine)
				{
					poLine.Completed = true;
					if (closePOLine)
					{
						poLine.Closed = true;
					}
				}
			}

			if (POLineType.UsePOAccrual(poLine.LineType) && poLine.POAccrualAcctID == null && poLine.POAccrualSubID == null)
			{
				poLine.POAccrualAcctID = line.POAccrualAcctID;
				poLine.POAccrualSubID = line.POAccrualSubID;
			}

			return poLinesOpenUPD.Update(poLine);
		}

		#endregion

		#region Return release

		/// <summary>
		/// Overrides <see cref="POReceiptEntry.ReleaseReturnLine(POReceiptLine, POLineUOpen, POReceiptLine2)"/>
		/// </summary>
		[PXOverride]
		public virtual void ReleaseReturnLine(POReceiptLine line, POLineUOpen poLine, POReceiptLine2 origLine, Action<POReceiptLine, POLineUOpen, POReceiptLine2> baseImpl)
		{
			POAccrualStatus poAccrual = POAccrualStatus.PK.FindDirty(Base, line.POAccrualRefNoteID, line.POAccrualLineNbr, line.POAccrualType);
			UpdatePOAccrualStatus(poAccrual, poLine, line, null, Base.Document.Current);
			UpdatePOReceiptLineAccrualDetail(line);

			baseImpl(line, poLine, origLine);
		}

		/// <summary>
		/// Extends <see cref="POReceiptEntry.UpdateReturnLineOnRelease(PXResult{POReceiptLine}, POLineUOpen)"/>
		/// </summary>
		[PXOverride]
		public virtual void UpdateReturnLineOnRelease(PXResult<POReceiptLine> row, POLineUOpen pOLine)
		{
			UpdatePOLineOnReturn(row, pOLine);
		}

		/// <summary>
		/// Overrides <see cref="POReceiptEntry.UpdateReturnReleased(INRegister)"/>
		/// </summary>
		[PXOverride]
		public virtual POReceipt UpdateReturnReleased(INRegister inRegister, Func<INRegister, POReceipt> baseImpl)
		{
			var receipt = baseImpl(inRegister);

			IncUnreleasedReceiptCntr(receipt);

			return receipt;
		}

		public virtual POLineUOpen UpdatePOLineOnReturn(POReceiptLine line, POLineUOpen poLine)
		{
			if (poLine == null || string.IsNullOrEmpty(poLine.OrderType) || string.IsNullOrEmpty(poLine.OrderNbr) || poLine.LineNbr == null)
			{
				return poLine;
			}

			decimal delta = line.ReceiptQty ?? Decimal.Zero;
			poLine = PXCache<POLineUOpen>.CreateCopy(poLine);
			if (line.InventoryID != null && !String.IsNullOrEmpty(line.UOM) && !String.IsNullOrEmpty(poLine.UOM))
				delta = INUnitAttribute.ConvertFromBase(Base.Caches[typeof(POReceiptLine)], line.InventoryID, poLine.UOM, line.BaseReceiptQty.Value, INPrecision.QUANTITY);

			poLine.CompletedQty += delta * line.InvtMult;

			bool dropshipReturn = POLineType.IsDropShip(line.LineType) && line.ReceiptType == POReceiptType.POReturn;
			if (poLine.AllowComplete == true && poLine.Completed == true && !dropshipReturn)
			{
				poLine.Completed = false;
				poLine.Closed = false;
			}

			return poLinesOpenUPD.Update(poLine);
		}

		#endregion

		#region POAccrualStatus

		public virtual POAccrualStatus UpdatePOAccrualStatus(POAccrualStatus origRow, POLineUOpen poLine, POReceiptLine rctLine, POOrder order, POReceipt rct)
		{
			PXCache cache = poAccrualUpdate.Cache;
			POAccrualStatus row;
			if (origRow == null)
			{
				row = new POAccrualStatus()
				{
					Type = rctLine.POAccrualType,
					RefNoteID = rctLine.POAccrualRefNoteID,
					LineNbr = rctLine.POAccrualLineNbr,
				};
				row = (POAccrualStatus)cache.Insert(row);
			}
			else
			{
				row = (POAccrualStatus)cache.CreateCopy(origRow);
			}

			SetIfNotNull<POAccrualStatus.lineType>(cache, row, rctLine.LineType);
			SetIfNotNull<POAccrualStatus.orderType>(cache, row, rctLine.POType);
			SetIfNotNull<POAccrualStatus.orderNbr>(cache, row, rctLine.PONbr);
			SetIfNotNull<POAccrualStatus.orderLineNbr>(cache, row, rctLine.POLineNbr);
			if (rctLine.POAccrualType == POAccrualType.Receipt)
			{
				SetIfNotNull<POAccrualStatus.receiptType>(cache, row, rctLine.ReceiptType);
				SetIfNotNull<POAccrualStatus.receiptNbr>(cache, row, rctLine.ReceiptNbr);
			}

			if (row.MaxFinPeriodID == null || rct.FinPeriodID.CompareTo(row.MaxFinPeriodID) > 0)
				SetIfNotNull<POAccrualStatus.maxFinPeriodID>(cache, row, rct.FinPeriodID);

			SetIfNotNull<POAccrualStatus.origUOM>(cache, row, poLine?.UOM);
			SetIfNotNull<POAccrualStatus.origCuryID>(cache, row, order?.CuryID);

			SetIfNotNull<POAccrualStatus.vendorID>(cache, row, rctLine.VendorID);
			SetIfNotNull<POAccrualStatus.payToVendorID>(cache, row, order?.PayToVendorID);
			SetIfNotNull<POAccrualStatus.inventoryID>(cache, row, rctLine.InventoryID);
			SetIfNotNull<POAccrualStatus.subItemID>(cache, row, rctLine.SubItemID);
			SetIfNotNull<POAccrualStatus.siteID>(cache, row, rctLine.SiteID);
			SetIfNotNull<POAccrualStatus.acctID>(cache, row, rctLine.POAccrualAcctID);
			SetIfNotNull<POAccrualStatus.subID>(cache, row, rctLine.POAccrualSubID);

			ARReleaseProcess.Amount origAccrualAmt = null;
			if (poLine?.OrderNbr != null)
			{
				origAccrualAmt = APReleaseProcess.GetExpensePostingAmount(Base, poLine.ToPOLine());
			}
			SetIfNotEmpty<POAccrualStatus.origQty>(cache, row, poLine?.OrderQty);
			SetIfNotEmpty<POAccrualStatus.baseOrigQty>(cache, row, poLine?.BaseOrderQty);
			SetIfNotEmpty<POAccrualStatus.curyOrigAmt>(cache, row, poLine?.CuryExtCost + poLine?.CuryRetainageAmt);
			SetIfNotEmpty<POAccrualStatus.origAmt>(cache, row, poLine?.ExtCost + poLine?.RetainageAmt);
			SetIfNotEmpty<POAccrualStatus.curyOrigCost>(cache, row, origAccrualAmt?.Cury);
			SetIfNotEmpty<POAccrualStatus.origCost>(cache, row, origAccrualAmt?.Base);
			SetIfNotEmpty<POAccrualStatus.curyOrigDiscAmt>(cache, row, poLine?.CuryDiscAmt);
			SetIfNotEmpty<POAccrualStatus.origDiscAmt>(cache, row, poLine?.DiscAmt);

			bool nulloutReceivedQty = (origRow != null && (origRow.ReceivedQty == null || !origRow.ReceivedUOM.IsIn(null, rctLine.UOM)));
			row.ReceivedUOM = nulloutReceivedQty ? null : rctLine.UOM;
			row.ReceivedQty += nulloutReceivedQty ? null : rctLine.InvtMult * rctLine.ReceiptQty;
			row.BaseReceivedQty += rctLine.InvtMult * rctLine.BaseReceiptQty;
			row.ReceivedCost += rctLine.InvtMult * (rctLine.TranCostFinal ?? rctLine.TranCost);

			return poAccrualUpdate.Update(row);
		}

		public virtual void IncUnreleasedReceiptCntr(POReceipt poReceipt)
		{
			var details = poAccrualDetailUpdate.Cache
				.Inserted
				.OfType<POAccrualDetail>()
				.Where(x =>
					x.POReceiptType == poReceipt.ReceiptType
					&& x.POReceiptNbr == poReceipt.ReceiptNbr)
				.ToList();

			if (!details.Any())
				return;

			var statuses = new HashSet<POAccrualStatus>(
				details.Select(x => new POAccrualStatus
				{
					RefNoteID = x.POAccrualRefNoteID,
					LineNbr = x.POAccrualLineNbr,
					Type = x.POAccrualType
				}),
				poAccrualUpdate.Cache.GetComparer());

			foreach (var s in statuses)
			{
				var status = poAccrualUpdate.Locate(s);
				if (status != null)
				{
					status.UnreleasedReceiptCntr++;
					poAccrualUpdate.Update(status);
				}
			}
		}

		#endregion

		#region POAccrualDetail

		public virtual bool StorePOAccrualDetail(POReceiptLine receiptLine)
			=> receiptLine.ReceiptType != POReceiptType.TransferReceipt
			&& receiptLine.LineType.IsNotIn(POLineType.Service, POLineType.Freight)
			&& (receiptLine.POType != POOrderType.ProjectDropShip || receiptLine.DropshipExpenseRecording != DropshipExpenseRecordingOption.OnBillRelease);

		public virtual POAccrualDetail PreparePOReceiptLineAccrualDetail(POReceiptLine receiptLine)
		{
			if (!StorePOAccrualDetail(receiptLine))
				return null;
			var detail = new POAccrualDetail
			{
				DocumentNoteID = Base.Document.Current.NoteID,
				LineNbr = receiptLine.LineNbr
			};
			detail = poAccrualDetailUpdate.Locate(detail);
			if (detail == null)
			{
				detail = new POAccrualDetail
				{
					DocumentNoteID = Base.Document.Current.NoteID,
					POReceiptType = receiptLine.ReceiptType,
					POReceiptNbr = receiptLine.ReceiptNbr,
					LineNbr = receiptLine.LineNbr,

					POAccrualRefNoteID = receiptLine.POAccrualRefNoteID,
					POAccrualLineNbr = receiptLine.POAccrualLineNbr,
					POAccrualType = receiptLine.POAccrualType,

					VendorID = receiptLine.VendorID,
					IsDropShip = POLineType.IsDropShip(receiptLine.LineType),
					BranchID = receiptLine.BranchID,
					DocDate = receiptLine.ReceiptDate,
					FinPeriodID = Base.Document.Current.FinPeriodID,
					TranDesc = receiptLine.TranDesc,
					UOM = receiptLine.UOM
				};
				detail = poAccrualDetailUpdate.Insert(detail);
			}
			return detail;
		}

		public virtual POAccrualDetail UpdatePOReceiptLineAccrualDetail(POReceiptLine receiptLine)
		{
			POAccrualDetail detail = PreparePOReceiptLineAccrualDetail(receiptLine);

			if (detail == null)
				return null;

			detail.AccruedQty += receiptLine.InvtMult * receiptLine.ReceiptQty;
			detail.BaseAccruedQty += receiptLine.InvtMult * receiptLine.BaseReceiptQty;

			detail.AccruedCost += receiptLine.InvtMult * (receiptLine.TranCostFinal ?? receiptLine.TranCost);

			if (receiptLine.ReceiptType == POReceiptType.POReturn && Base.Document.Current.POType != POOrderType.DropShip)
				detail.Posted = true;

			return poAccrualDetailUpdate.Update(detail);
		}

		#endregion

		#region POAccrualSplit

		public virtual void SetReceiptCostFinal(POReceiptLine rctLine, POAccrualStatus poAccrual)
		{
			if (rctLine.IsKit == true && rctLine.IsStkItem != true)
			{
				rctLine.TranCostFinal = 0m;
			}
			else if (poAccrual == null || poAccrual.BaseReceivedQty >= poAccrual.BaseBilledQty)
			{
				// can set actual billed cost only if something is already billed
				rctLine.TranCostFinal = rctLine.TranCost;
			}
			else
			{
				CurrencyInfo currencyInfo = Base.MultiCurrencyExt.GetDefaultCurrencyInfo();

				bool uomCoincide = string.Equals(rctLine.UOM, poAccrual.BilledUOM, StringComparison.OrdinalIgnoreCase)
					&& poAccrual.ReceivedQty != null && poAccrual.ReceivedUOM.IsIn(null, rctLine.UOM);
				decimal? rctLineQty = uomCoincide ? rctLine.ReceiptQty : rctLine.BaseReceiptQty;
				decimal? unreceivedBilledQty = uomCoincide
					? poAccrual.BilledQty - poAccrual.ReceivedQty
					: poAccrual.BaseBilledQty - poAccrual.BaseReceivedQty;
				decimal? unreceivedBilledCost = poAccrual.BilledCost + poAccrual.PPVAmt - poAccrual.ReceivedCost;
				decimal? accruedQty, baseAccruedQty, accruedCost, tranCost;
				if (rctLineQty <= unreceivedBilledQty)
				{
					accruedQty = rctLineQty;
					baseAccruedQty = rctLine.BaseUnbilledQty;
					tranCost = accruedCost = rctLineQty * unreceivedBilledCost / unreceivedBilledQty;
					rctLine.BaseUnbilledQty = rctLine.UnbilledQty = 0m;
				}
				else
				{
					accruedQty = unreceivedBilledQty;
					tranCost = accruedCost = unreceivedBilledCost;
					if (uomCoincide)
					{
						baseAccruedQty = rctLine.BaseUnbilledQty;
						rctLine.UnbilledQty -= unreceivedBilledQty;
						PXDBQuantityAttribute.CalcBaseQty<POReceiptLine.unbilledQty>(Base.transactions.Cache, rctLine);
						baseAccruedQty -= rctLine.BaseUnbilledQty;
					}
					else
					{
						rctLine.BaseUnbilledQty -= unreceivedBilledQty;
						PXDBQuantityAttribute.CalcTranQty<POReceiptLine.unbilledQty>(Base.transactions.Cache, rctLine);
						baseAccruedQty = unreceivedBilledQty;
					}
					if (rctLine.UnbilledQty > 0m && rctLine.ReceiptQty > 0m)
					{
						tranCost += rctLine.UnbilledQty * (rctLine.TranCost / rctLine.ReceiptQty);
					}
				}
				rctLine.TranCostFinal = currencyInfo.RoundBase(tranCost ?? 0m);

				InsertPOAccrualSplits(
					rctLine, poAccrual,
					uomCoincide ? rctLine.UOM : null,
					uomCoincide ? accruedQty : null,
					baseAccruedQty,
					accruedCost);
			}
			Base.transactions.Cache.SetStatus(rctLine, PXEntryStatus.Updated);
		}

		public virtual void InsertPOAccrualSplits(POReceiptLine rctLine, POAccrualStatus poAccrual,
			string accruedUom, decimal? accruedQty, decimal? baseAccruedQty, decimal? accruedCost)
		{
			CurrencyInfo currencyInfo = Base.MultiCurrencyExt.GetDefaultCurrencyInfo();

			bool uomCoincide = (accruedQty != null);
			decimal? qtyToDistribute = uomCoincide ? accruedQty : baseAccruedQty;
			decimal? costToDistribute = accruedCost;
			// can be only order-based billing - AP Bill released before PO Receipt
			foreach (APTranReceiptUpdate tran in apTranUpdate.View.SelectMultiBound(new object[] { poAccrual }))
			{
				if (qtyToDistribute <= 0) break;

				decimal? splitQty, baseSplitQty, splitCost;
				decimal? unreceivedQty = uomCoincide ? tran.UnreceivedQty : tran.BaseUnreceivedQty;
				if (unreceivedQty <= qtyToDistribute)
				{
					splitCost = currencyInfo.RoundBase((costToDistribute * unreceivedQty / qtyToDistribute) ?? 0m);
					costToDistribute -= splitCost;

					splitQty = unreceivedQty;
					baseSplitQty = tran.BaseUnreceivedQty;
					qtyToDistribute -= splitQty;
					tran.BaseUnreceivedQty = tran.UnreceivedQty = 0m;
				}
				else
				{
					splitCost = costToDistribute;
					costToDistribute = 0m;

					splitQty = qtyToDistribute;
					qtyToDistribute = 0m;
					if (uomCoincide)
					{
						baseSplitQty = tran.BaseUnreceivedQty;
						tran.UnreceivedQty -= splitQty;
						PXDBQuantityAttribute.CalcBaseQty<APTranReceiptUpdate.unreceivedQty>(apTranUpdate.Cache, tran);
						baseSplitQty -= tran.BaseUnreceivedQty;
					}
					else
					{
						tran.BaseUnreceivedQty -= splitQty;
						PXDBQuantityAttribute.CalcTranQty<APTranReceiptUpdate.unreceivedQty>(apTranUpdate.Cache, tran);
						baseSplitQty = splitQty;
					}
				}
				apTranUpdate.Update(tran);

				var poAccrualSplit = new POAccrualSplit()
				{
					RefNoteID = poAccrual.RefNoteID,
					LineNbr = poAccrual.LineNbr,
					Type = poAccrual.Type,
					APDocType = tran.TranType,
					APRefNbr = tran.RefNbr,
					APLineNbr = tran.LineNbr,
					POReceiptType = rctLine.ReceiptType,
					POReceiptNbr = rctLine.ReceiptNbr,
					POReceiptLineNbr = rctLine.LineNbr,
				};
				poAccrualSplit = this.poAccrualSplitUpdate.Insert(poAccrualSplit);
				poAccrualSplit.UOM = accruedUom;
				poAccrualSplit.AccruedQty = uomCoincide ? splitQty : null;
				poAccrualSplit.BaseAccruedQty = baseSplitQty;
				poAccrualSplit.AccruedCost = splitCost;
				poAccrualSplit.PPVAmt = 0m;
				poAccrualSplit.FinPeriodID = new string[] { tran.FinPeriodID, Base.Document.Current.FinPeriodID }.Max();
				poAccrualSplit = this.poAccrualSplitUpdate.Update(poAccrualSplit);
			}
		}

		#endregion
	}
}
