using System;
using System.Collections.Generic;
using System.Linq;

using PX.Api;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.Common;
using PX.Objects.AR;
using PX.Objects.IN;
using PX.Objects.CS;

namespace PX.Objects.SO.GraphExtensions
{
	public abstract class SOBaseItemAvailabilityExtension<TGraph, TLine, TSplit> : IN.GraphExtensions.ItemAvailabilityExtension<TGraph, TLine, TSplit>
		where TGraph : PXGraph
		where TLine : class, IBqlTable, ILSPrimary, new()
		where TSplit : class, IBqlTable, ILSDetail, new()
	{
		protected virtual ReturnedQtyResult MemoCheckQty(
			int? inventoryID,
			string arDocType, string arRefNbr, int? arTranLineNbr,
			string orderType, string orderNbr, int? orderLineNbr)
		{
			var qtyResult = new ReturnedQtyResult(true);

			bool hasRefToOrigSOLine = orderType != null && orderNbr != null && orderLineNbr != null;
			bool hasRefToOrigARTran = arDocType != null && arRefNbr != null && arTranLineNbr != null;
			if (!hasRefToOrigSOLine && !hasRefToOrigARTran)
				return qtyResult;

			SOInvoicedRecords invoiced = SelectInvoicedRecords(arDocType, arRefNbr);

			//return SO lines (including current document, excluding cancelled orders):
			//Note: SOOrder is LeftJoined instead of InnerJoin for current unsaved document lines to be included in the result.
			var returnSOLines = !hasRefToOrigARTran ? Array.Empty<SOLine>() :
				SelectFrom<SOLine>.
				LeftJoin<SOOrder>.On<SOLine.FK.Order>.
				Where<
					SOLine.invoiceType.IsEqual<@P.AsString.ASCII>.
					And<SOLine.invoiceNbr.IsEqual<@P.AsString>>.
					And<
						SOLine.behavior.IsEqual<SOBehavior.rM>.
						Or<
							SOLine.behavior.IsEqual<SOBehavior.cM>.
							And<SOOrder.cancelled.IsEqual<False>>>>>.
				View.Select(Base, arDocType, arRefNbr).RowCast<SOLine>();

			//return direct AR Transactions (including current document):
			var returnARTrans = !hasRefToOrigARTran ? Array.Empty<ARTran>() :
				SelectFrom<ARTran>.
				Where<
					ARTran.sOOrderNbr.IsNull.
					And<ARTran.origInvoiceType.IsEqual<@P.AsString.ASCII>>.
					And<ARTran.origInvoiceNbr.IsEqual<@P.AsString>>.
					And<ARTran.qty.Multiply<ARTran.invtMult>.IsGreater<decimal0>>>.
				View.Select(Base, arDocType, arRefNbr).RowCast<ARTran>();

			if (hasRefToOrigSOLine)
			{
				var invoicedFromSOLine = invoiced.Records
					.Where(r =>
						r.SOLine.OrderType == orderType &&
						r.SOLine.OrderNbr == orderNbr &&
						r.SOLine.LineNbr == orderLineNbr);
				var returnedFromSOLine = returnSOLines
					.Where(l =>
						l.OrigOrderType == orderType &&
						l.OrigOrderNbr == orderNbr
						&& l.OrigLineNbr == orderLineNbr)
					.Select(ReturnRecord.FromSOLine);
				qtyResult = CheckInvoicedAndReturnedQty(inventoryID, invoicedFromSOLine, returnedFromSOLine);
			}

			if (qtyResult.Success == true && hasRefToOrigARTran)
			{
				var invoicedFromOrigARTran = invoiced.Records.Where(r => r.ARTran.LineNbr == arTranLineNbr);
				var returnedFromOrigARTran =
						returnARTrans
						.Where(t => t.OrigInvoiceLineNbr == arTranLineNbr)
						.Select(ReturnRecord.FromARTran)
					.Concat(
						returnSOLines
						.Where(l => l.InvoiceLineNbr == arTranLineNbr)
						.Select(ReturnRecord.FromSOLine));
				qtyResult = CheckInvoicedAndReturnedQty(inventoryID, invoicedFromOrigARTran, returnedFromOrigARTran);
			}

			return qtyResult;
		}

		public virtual SOInvoicedRecords SelectInvoicedRecords(string arDocType, string arRefNbr)
		{
			return SelectInvoicedRecords(arDocType, arRefNbr, includeDirectLines: false);
		}

		protected virtual SOInvoicedRecords SelectInvoicedRecords(string arDocType, string arRefNbr, bool includeDirectLines)
		{
			PXSelectBase<ARTran> cmd = new
				SelectFrom<ARTran>.
				InnerJoin<InventoryItem>.On<ARTran.FK.InventoryItem>.
				LeftJoin<SOLine>.On<ARTran.FK.SOOrderLine>.
				LeftJoin<INTran>.On<INTran.FK.ARTran>.
				LeftJoin<INTranSplit>.On<INTranSplit.FK.Tran>.
				LeftJoin<INLotSerialStatus>.On<
					INLotSerialStatus.lotSerTrack.IsEqual<INLotSerTrack.serialNumbered>.
					And<INLotSerialStatus.inventoryID.IsEqual<INTranSplit.inventoryID>>.
					And<INLotSerialStatus.lotSerialNbr.IsEqual<INTranSplit.lotSerialNbr>>.
					And<
						INLotSerialStatus.qtyOnHand.IsGreater<decimal0>.
						Or<INLotSerialStatus.qtyINReceipts.IsGreater<decimal0>>.
						Or<INLotSerialStatus.qtySOShipping.IsLess<decimal0>>.
						Or<INLotSerialStatus.qtySOShipped.IsLess<decimal0>>>>.
				LeftJoin<SOSalesPerTran>.On<
					SOSalesPerTran.orderType.IsEqual<SOLine.orderType>.
					And<SOSalesPerTran.orderNbr.IsEqual<SOLine.orderNbr>>.
					And<SOSalesPerTran.salespersonID.IsEqual<SOLine.salesPersonID>>>.
				Where<
					ARTran.tranType.IsEqual<@P.AsString.ASCII>.
					And<ARTran.refNbr.IsEqual<@P.AsString>>.
					And<
						Brackets<
							INTran.released.IsEqual<True>.
							And<INTran.qty.IsGreater<decimal0>>.
							And<
								INTran.tranType.IsEqual<INTranType.issue>.
								Or<INTran.tranType.IsEqual<INTranType.debitMemo>>.
								Or<INTran.tranType.IsEqual<INTranType.invoice>>>>.
						Or<
							INTran.released.IsNull.
							And<ARTran.lineType.IsIn<SOLineType.miscCharge, SOLineType.nonInventory>>>>>.
				OrderBy<
					ARTran.inventoryID.Asc,
					INTranSplit.subItemID.Asc>.
				View(Base);

			if (!includeDirectLines)
				cmd.WhereAnd<Where<ARTran.lineType, Equal<SOLine.lineType>, And<SOLine.orderNbr, IsNotNull>>>();

			var splits = new SOInvoicedRecords(Base.Caches<ARTran>().GetComparer());
			foreach (PXResult<ARTran, InventoryItem, SOLine, INTran, INTranSplit, INLotSerialStatus, SOSalesPerTran> res in
				cmd.Select(arDocType, arRefNbr))
			{
				splits.Add(res);
			}

			return splits;
		}

		protected virtual ReturnedQtyResult CheckInvoicedAndReturnedQty(
			int? returnInventoryID,
			IEnumerable<SOInvoicedRecords.Record> invoiced,
			IEnumerable<ReturnRecord> returned)
		{
			if (returnInventoryID == null)
				return new ReturnedQtyResult(true);

			int origInventoryID = 0;
			decimal totalInvoicedQty = 0;
			var totalInvoicedQtyByComponent = new Dictionary<int, decimal>();
			var componentsInAKit = new Dictionary<int, decimal>();

			//invoiced are always either KIT or a regular item
			foreach (SOInvoicedRecords.Record record in invoiced)
			{
				origInventoryID = record.SOLine.InventoryID ?? record.ARTran.InventoryID.Value;
				totalInvoicedQty += INUnitAttribute.ConvertToBase(Base.Caches<ARTran>(), record.ARTran.InventoryID, record.ARTran.UOM, record.ARTran.Qty.Value, INPrecision.QUANTITY);

				foreach (SOInvoicedRecords.INTransaction intran in record.Transactions.Values)
				{
					if (!totalInvoicedQtyByComponent.ContainsKey(intran.Transaction.InventoryID.Value))
						totalInvoicedQtyByComponent[intran.Transaction.InventoryID.Value] = 0;

					totalInvoicedQtyByComponent[intran.Transaction.InventoryID.Value] +=
						INUnitAttribute.ConvertToBase(Base.Caches<INTran>(), intran.Transaction.InventoryID, intran.Transaction.UOM, intran.Transaction.Qty.Value, INPrecision.QUANTITY);
				}
			}

			foreach (KeyValuePair<int, decimal> kv in totalInvoicedQtyByComponent)
				componentsInAKit[kv.Key] = kv.Value / totalInvoicedQty;

			//returned can be a regular item or a kit or a component of a kit. 
			foreach (var ret in returned)
			{
				if (ret.InventoryID == origInventoryID || totalInvoicedQtyByComponent.Count == 0)//regular item or a kit
				{
					decimal returnedQty = INUnitAttribute.ConvertToBase(LineCache, ret.InventoryID, ret.UOM, ret.Qty, INPrecision.QUANTITY);
					totalInvoicedQty -= returnedQty;

					InventoryItem item = InventoryItem.PK.Find(Base, ret.InventoryID);
					if (item.KitItem == true)
					{
						foreach (KeyValuePair<int, decimal> kv in componentsInAKit)
						{
							totalInvoicedQtyByComponent[kv.Key] -= componentsInAKit[kv.Key] * returnedQty;
						}
					}
				}
				else //component of a kit. 
				{
					totalInvoicedQtyByComponent[ret.InventoryID.Value] -= INUnitAttribute.ConvertToBase(LineCache, ret.InventoryID, ret.UOM, ret.Qty, INPrecision.QUANTITY);
				}
			}

			bool success = true;
			if (returnInventoryID == origInventoryID)
			{
				if (totalInvoicedQty < 0m || totalInvoicedQtyByComponent.Values.Any(v => v < 0m))
					success = false;
			}
			else
			{
				if (totalInvoicedQty < 0m)
					success = false;

				if (totalInvoicedQtyByComponent.TryGetValue(returnInventoryID.Value, out decimal qtyByComponent) && qtyByComponent < 0)
					success = false;
			}

			return new ReturnedQtyResult(success, success ? null : returned.ToArray());
		}

		[PXInternalUseOnly]
		public abstract class ReturnRecord
		{
			public abstract int? InventoryID { get; }
			public abstract string UOM { get; }
			public abstract decimal Qty { get; }
			public abstract string DocumentNbr { get; }

			public static ReturnRecord FromSOLine(SOLine l) => new ReturnSOLine(l);

			public static ReturnRecord FromARTran(ARTran t) => new ReturnARTran(t);

			private class ReturnSOLine : ReturnRecord
			{
				public ReturnSOLine(SOLine line) => Line = line;

				public SOLine Line { get; }
				public override string DocumentNbr => Line.OrderNbr;
				public override int? InventoryID => Line.InventoryID;
				public override string UOM => Line.UOM;
				public override decimal Qty => (Line.RequireShipping == true && Line.Completed == true) ? (Line.ShippedQty ?? 0) : (Line.OrderQty ?? 0);
			}

			private class ReturnARTran : ReturnRecord
			{
				public ReturnARTran(ARTran tran) => Tran = tran;

				public ARTran Tran { get; }
				public override string DocumentNbr => Tran.RefNbr;
				public override int? InventoryID => Tran.InventoryID;
				public override string UOM => Tran.UOM;
				public override decimal Qty => Math.Abs(Tran.Qty ?? 0m);
			}
		}

		public class ReturnedQtyResult
		{
			public ReturnedQtyResult(bool success, ReturnRecord[] returnRecords = null)
			{
				Success = success;
				ReturnRecords = returnRecords;
			}

			public bool Success { get; private set; }
			public ReturnRecord[] ReturnRecords { get; private set; }
		}
	}
}
