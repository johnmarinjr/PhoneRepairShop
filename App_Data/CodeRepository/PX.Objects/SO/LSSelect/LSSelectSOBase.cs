using System;
using System.Collections.Generic;
using System.Linq;

using PX.Common;
using PX.Data;

using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.IN;

namespace PX.Objects.SO
{
	[Obsolete] // the class is moved from ../Descriptor/Attribute.cs as is
	public abstract class LSSelectSOBase<TLSMaster, TLSDetail, Where> : LSSelect<TLSMaster, TLSDetail, Where>
		where TLSMaster : class, IBqlTable, ILSPrimary, new()
		where TLSDetail : class, IBqlTable, ILSDetail, new()
		where Where : IBqlWhere, new()
	{
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


		#region Ctor

		public LSSelectSOBase(PXGraph graph)
			: base(graph)
		{
		}

		#endregion

		#region Implementation

		protected virtual ReturnedQtyResult MemoAvailabilityCheckQty(
			PXCache sender, int? inventoryID,
			string arDocType, string arRefNbr, int? arTranLineNbr,
			string orderType, string orderNbr, int? orderLineNbr)
		{
			var qtyResult = new ReturnedQtyResult(true);

			bool hasRefToOrigSOLine = (orderType != null && orderNbr != null && orderLineNbr != null);
			bool hasRefToOrigARTran = (arDocType != null && arRefNbr != null && arTranLineNbr != null);
			if (!hasRefToOrigSOLine && !hasRefToOrigARTran)
				return qtyResult;

			SOInvoicedRecords invoiced = SelectInvoicedRecords(arDocType, arRefNbr);

			//return SO lines (including current document, excluding cancelled orders):
			//Note: SOOrder is LeftJoined instead of InnerJoin for current unsaved document lines to be included in the result.
			PXSelectBase<SOLine> selectReturnSOLines = new PXSelectJoin<SOLine,
				LeftJoin<SOOrder, On<SOOrder.orderType, Equal<SOLine.orderType>, And<SOOrder.orderNbr, Equal<SOLine.orderNbr>>>>,
				Where<SOLine.invoiceType, Equal<Required<SOLine.invoiceType>>,
				And<SOLine.invoiceNbr, Equal<Required<SOLine.invoiceNbr>>,
				And<Where<SOLine.behavior, Equal<SOBehavior.rM>,
				Or<SOLine.behavior, Equal<SOBehavior.cM>, And<SOOrder.cancelled, Equal<False>>>>>>>>(_Graph);
			var returnSOLines = selectReturnSOLines.Select(arDocType, arRefNbr).RowCast<SOLine>();

			//return direct AR Transactions (including current document):
			PXSelectBase<ARTran> selectReturnARTrans = new PXSelect<ARTran,
				Where<ARTran.sOOrderNbr, IsNull,
				And<ARTran.origInvoiceType, Equal<Required<ARTran.origInvoiceType>>,
				And<ARTran.origInvoiceNbr, Equal<Required<ARTran.origInvoiceNbr>>,
				And<Where<ARTran.invtMult, Equal<short1>, And<ARTran.qty, Greater<decimal0>,
				Or<ARTran.invtMult, Equal<shortMinus1>, And<ARTran.qty, Less<decimal0>>>>>>>>>>(_Graph);
			var returnARTrans = selectReturnARTrans.Select(arDocType, arRefNbr).RowCast<ARTran>();

			if (hasRefToOrigSOLine)
			{
				var invoicedFromSOLine = invoiced.Records.Where(r => r.SOLine.OrderType == orderType && r.SOLine.OrderNbr == orderNbr && r.SOLine.LineNbr == orderLineNbr);
				var returnedFromSOLine = returnSOLines.Where(l => l.OrigOrderType == orderType && l.OrigOrderNbr == orderNbr && l.OrigLineNbr == orderLineNbr)
					.Select(ReturnRecord.FromSOLine);
				qtyResult = CheckInvoicedAndReturnedQty(sender, inventoryID, invoicedFromSOLine, returnedFromSOLine);
			}
			if (qtyResult.Success == true && hasRefToOrigARTran)
			{
				var invoicedFromOrigARTran = invoiced.Records.Where(r => r.ARTran.LineNbr == arTranLineNbr);
				var returnedFromOrigARTran = returnARTrans.Where(t => t.OrigInvoiceLineNbr == arTranLineNbr).Select(ReturnRecord.FromARTran)
					.Concat(returnSOLines.Where(l => l.InvoiceLineNbr == arTranLineNbr).Select(ReturnRecord.FromSOLine));
				qtyResult = CheckInvoicedAndReturnedQty(sender, inventoryID, invoicedFromOrigARTran, returnedFromOrigARTran);
			}
			return qtyResult;
		}

		public virtual SOInvoicedRecords SelectInvoicedRecords(string arDocType, string arRefNbr)
		{
			return SelectInvoicedRecords(arDocType, arRefNbr, includeDirectLines: false);
		}

		protected virtual SOInvoicedRecords SelectInvoicedRecords(string arDocType, string arRefNbr, bool includeDirectLines)
		{
			SOInvoicedRecords splits = new SOInvoicedRecords(new Comparer<ARTran>(_Graph));

			PXSelectBase<ARTran> cmd = new PXSelectJoin<ARTran
				, InnerJoin<InventoryItem
					, On<InventoryItem.inventoryID, Equal<ARTran.inventoryID>
						>
				, LeftJoin<SOLine
					, On<SOLine.orderType, Equal<ARTran.sOOrderType>
						, And<SOLine.orderNbr, Equal<ARTran.sOOrderNbr>
							, And<SOLine.lineNbr, Equal<ARTran.sOOrderLineNbr>>
							>
						>
				, LeftJoin<INTran
					, On<INTran.aRDocType, Equal<ARTran.tranType>, And<INTran.aRRefNbr, Equal<ARTran.refNbr>, And<INTran.aRLineNbr, Equal<ARTran.lineNbr>>>>
				, LeftJoin<INTranSplit
					, On<INTranSplit.FK.Tran>
				, LeftJoin<INLotSerialStatus
					, On<INLotSerialStatus.lotSerTrack, Equal<INLotSerTrack.serialNumbered>
						, And<INLotSerialStatus.inventoryID, Equal<INTranSplit.inventoryID>
							, And<INLotSerialStatus.lotSerialNbr, Equal<INTranSplit.lotSerialNbr>
								, And<
									Where<
										INLotSerialStatus.qtyOnHand, Greater<decimal0>
										, Or<INLotSerialStatus.qtyINReceipts, Greater<decimal0>
											, Or<INLotSerialStatus.qtySOShipping, Less<decimal0>
												, Or<INLotSerialStatus.qtySOShipped, Less<decimal0>>
												>
											>
										>
									>
								>
							>
						>
				, LeftJoin<SOSalesPerTran
					, On<SOSalesPerTran.orderType, Equal<SOLine.orderType>, And<SOSalesPerTran.orderNbr, Equal<SOLine.orderNbr>, And<SOSalesPerTran.salespersonID, Equal<SOLine.salesPersonID>>>>>>>>>>,
				Where<ARTran.tranType, Equal<Required<AddInvoiceFilter.docType>>, And<ARTran.refNbr, Equal<Required<AddInvoiceFilter.refNbr>>,
				And<Where2<Where<INTran.released, Equal<boolTrue>, And<INTran.qty, Greater<decimal0>,
				And<Where<INTran.tranType, Equal<INTranType.issue>, Or<INTran.tranType, Equal<INTranType.debitMemo>, Or<INTran.tranType, Equal<INTranType.invoice>>>>>>>,
				Or<Where<INTran.released, IsNull, And<Where<ARTran.lineType, Equal<SOLineType.miscCharge>, Or<ARTran.lineType, Equal<SOLineType.nonInventory>>>>>>>>>>,
				OrderBy<Asc<ARTran.inventoryID, Asc<INTranSplit.subItemID>>>>(_Graph);

			if (!includeDirectLines)
			{
				cmd.WhereAnd<Where<ARTran.lineType, Equal<SOLine.lineType>, And<SOLine.orderNbr, IsNotNull>>>();
			}

			foreach (PXResult<ARTran, InventoryItem, SOLine, INTran, INTranSplit, INLotSerialStatus, SOSalesPerTran> res in
				cmd.Select(arDocType, arRefNbr))
			{
				splits.Add(res);
			}

			return splits;
		}

		protected virtual ReturnedQtyResult CheckInvoicedAndReturnedQty(PXCache sender, int? returnInventoryID, IEnumerable<SOInvoicedRecords.Record> invoiced, IEnumerable<ReturnRecord> returned)
		{
			if (returnInventoryID == null)
				return new ReturnedQtyResult(true);

			int origInventoryID = 0;
			decimal totalInvoicedQty = 0;
			Dictionary<int, decimal> totalInvoicedQtyByComponent = new Dictionary<int, decimal>();
			Dictionary<int, decimal> componentsInAKit = new Dictionary<int, decimal>();

			//invoiced are always either KIT or a regular item
			foreach (SOInvoicedRecords.Record record in invoiced)
			{
				origInventoryID = record.SOLine.InventoryID ?? record.ARTran.InventoryID.Value;
				totalInvoicedQty += INUnitAttribute.ConvertToBase(sender.Graph.Caches[typeof(ARTran)], record.ARTran.InventoryID, record.ARTran.UOM, (decimal)record.ARTran.Qty, INPrecision.QUANTITY);

				foreach (SOInvoicedRecords.INTransaction intran in record.Transactions.Values)
				{
					if (!totalInvoicedQtyByComponent.ContainsKey(intran.Transaction.InventoryID.Value))
						totalInvoicedQtyByComponent[intran.Transaction.InventoryID.Value] = 0;

					totalInvoicedQtyByComponent[intran.Transaction.InventoryID.Value] += INUnitAttribute.ConvertToBase(sender.Graph.Caches[typeof(INTran)], intran.Transaction.InventoryID, intran.Transaction.UOM, (decimal)intran.Transaction.Qty, INPrecision.QUANTITY);
				}
			}

			foreach (KeyValuePair<int, decimal> kv in totalInvoicedQtyByComponent)
			{
				componentsInAKit[kv.Key] = kv.Value / totalInvoicedQty;
			}

			//returned can be a regular item or a kit or a component of a kit. 
			foreach (var ret in returned)
			{
				if (ret.InventoryID == origInventoryID || totalInvoicedQtyByComponent.Count == 0)//regular item or a kit
				{
					decimal returnedQty = INUnitAttribute.ConvertToBase(sender, ret.InventoryID, ret.UOM, (decimal)ret.Qty, INPrecision.QUANTITY);
					totalInvoicedQty -= returnedQty;

					InventoryItem item = ReadInventoryItem(sender, ret.InventoryID);
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
					totalInvoicedQtyByComponent[ret.InventoryID.Value] -= INUnitAttribute.ConvertToBase(sender, ret.InventoryID, ret.UOM, (decimal)ret.Qty, INPrecision.QUANTITY);
				}
			}

			bool success = true;
			if (returnInventoryID == origInventoryID)
			{
				if (totalInvoicedQty < 0m || totalInvoicedQtyByComponent.Values.Any(v => v < 0m))
				{
					success = false;
				}
			}
			else
			{
				if (totalInvoicedQty < 0m)
				{
					success = false;
				}

				decimal qtyByComponent;
				if (totalInvoicedQtyByComponent.TryGetValue(returnInventoryID.Value, out qtyByComponent) && qtyByComponent < 0)
				{
					success = false;
				}
			}
			return new ReturnedQtyResult(success, success ? null : returned.ToArray());
		}

		public class Comparer<T> : IEqualityComparer<T>
		{
			protected PXCache _cache;
			public Comparer(PXGraph graph)
			{
				_cache = graph.Caches[typeof(T)];
			}

			public bool Equals(T a, T b)
			{
				return _cache.ObjectsEqual(a, b);
			}

			public int GetHashCode(T a)
			{
				return _cache.GetObjectHashCode(a);
			}
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

			public class ReturnSOLine : ReturnRecord
			{
				public SOLine Line { get; }
				public override string DocumentNbr => Line.OrderNbr;
				public override int? InventoryID => Line.InventoryID;
				public override string UOM => Line.UOM;
				public override decimal Qty => (Line.RequireShipping == true && Line.Completed == true) ? (Line.ShippedQty ?? 0) : (Line.OrderQty ?? 0);

				public ReturnSOLine(SOLine line)
				{
					Line = line;
				}
			}

			public class ReturnARTran : ReturnRecord
			{
				public ARTran Tran { get; }
				public override string DocumentNbr => Tran.RefNbr;
				public override int? InventoryID => Tran.InventoryID;
				public override string UOM => Tran.UOM;
				public override decimal Qty => Math.Abs(Tran.Qty ?? 0m);

				public ReturnARTran(ARTran tran)
				{
					Tran = tran;
				}
			}
		}

		#endregion
	}
}
