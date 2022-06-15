using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.Common;
using PX.Objects.AM.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.AM
{
	public abstract class AMMoveLineSplittingExtension<TMoveGraph> : AMBatchLineSplittingExtension<TMoveGraph>
		where TMoveGraph : AMBatchEntryBase
	{
		#region Event Handlers
		#region AMMTran
		protected override void SubscribeForLineEvents()
		{
			base.SubscribeForLineEvents();
			ManualEvent.FieldOf<AMMTran, AMMTran.tranType>.Defaulting.Subscribe<string>(Base, EventHandler);
			ManualEvent.FieldOf<AMMTran, AMMTran.lastOper>.Updated.Subscribe<bool?>(Base, EventHandler);
			ManualEvent.FieldOf<AMMTran, AMMTran.operationID>.Updated.Subscribe<int?>(Base, EventHandler);
			ManualEvent.FieldOf<AMMTran, AMMTran.isScrap>.Updated.Subscribe<bool?>(Base, EventHandler);
			ManualEvent.FieldOf<AMMTran, AMMTran.qty>.Updated.Subscribe<decimal?>(Base, EventHandler);
		}

		protected override void EventHandler(ManualEvent.Row<AMMTran>.Selected.Args e)
		{
			if (!string.IsNullOrWhiteSpace(e.Row?.ProdOrdID))
			{
				AllowSplits(!IsNegativeMove(e.Row) && (IsScrap(e.Row) || e.Row.LastOper == true));
			}
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMMTran, AMMTran.tranType>.Defaulting.Args<string> e) => SetTranTypeInvtMult(e.Row);

		protected virtual void EventHandler(ManualEvent.FieldOf<AMMTran, AMMTran.lastOper>.Updated.Args<bool?> e) => SetTranTypeInvtMult(e.Row);

		protected virtual void EventHandler(ManualEvent.FieldOf<AMMTran, AMMTran.qty>.Updated.Args<decimal?> e) => SetTranTypeInvtMult(e.Row);

		protected virtual void EventHandler(ManualEvent.FieldOf<AMMTran, AMMTran.isScrap>.Updated.Args<bool?> e) => SetTranTypeInvtMult(e.Row);

		protected virtual void EventHandler(ManualEvent.FieldOf<AMMTran, AMMTran.operationID>.Updated.Args<int?> e)
		{
			if (!string.IsNullOrWhiteSpace(e.Row?.ProdOrdID) && e.Row.OperationID != null)
				SetTranTypeInvtMult(e.Row);
		}

		#endregion
		#region AMMTranSplit
		protected override void SubscribeForSplitEvents()
		{
			base.SubscribeForSplitEvents();
			ManualEvent.FieldOf<AMMTranSplit, AMMTranSplit.invtMult>.Updated.Subscribe<short?>(Base, EventHandler);
		}

		protected virtual void EventHandler(ManualEvent.FieldOf<AMMTranSplit, AMMTranSplit.invtMult>.Updated.Args<short?> e)
		{
			if (e.Row != null && LineCurrent != null && e.Row.LineNbr == LineCurrent.LineNbr)
				e.Row.TranType = e.Row.InvtMult < 1 ? AMTranType.Adjustment : AMTranType.Receipt;
		}

		protected override void EventHandler(ManualEvent.Row<AMMTranSplit>.Inserting.Args e)
		{
			base.EventHandler(e);

			if (e.Row == null)
				return;

			var rowParent = PXParentAttribute.SelectParent<AMMTran>(e.Cache, e.Row);
			if (rowParent == null)
				return;

			e.Row.TranType = rowParent.TranType ?? e.Row.TranType;
			e.Row.InvtMult = AMTranType.InvtMult(e.Row.TranType, rowParent.Qty);
		}
		#endregion
		#endregion

		protected virtual bool IsNegativeMove(AMMTranSplit split) => IsNegativeMove(GetAMMTran(split));
		protected virtual bool IsNegativeMove(AMMTran line) => line?.InventoryID != null && line.Qty.GetValueOrDefault() < 0;

		protected virtual bool IsScrap(AMMTranSplit split) => IsScrap(GetAMMTran(split));
		protected virtual bool IsScrap(AMMTran line) => line?.InventoryID != null && line.IsScrap == true;

		protected virtual AMMTran GetAMMTran(AMMTranSplit split) => GetAMMTran(split.DocType, split.BatNbr, split.LineNbr.GetValueOrDefault());
		protected virtual AMMTran GetAMMTran(string docType, string batNbr, int lineNbr)
		{
			var line =
				(AMMTran)LineCache.Locate(new AMMTran()
				{
					DocType = docType,
					BatNbr = batNbr,
					LineNbr = lineNbr
				});

			if (line == null)
				line =
					SelectFrom<AMMTran>.
					Where<
						AMMTran.docType.IsEqual<@P.AsString.ASCII>.
						And<AMMTran.batNbr.IsEqual<@P.AsString>>.
						And<AMMTran.lineNbr.IsEqual<@P.AsInt>>>.
					View.Select(Base, docType, batNbr, lineNbr);

			return line;
		}

		protected virtual void AllowSplits(bool allow)
		{
			SplitCache.AllowInsert = allow && LineCache.AllowInsert;
			SplitCache.AllowUpdate = allow && LineCache.AllowUpdate;
		}

		protected virtual void SetTranTypeInvtMult(AMMTran line)
		{
			if (line == null)
				return;
#if DEBUG
			var tranTypeOld = line.TranType;
			var invtMultOld = line.InvtMult;
#endif
			var tranTypeNew = line.Qty.GetValueOrDefault() < 0 ?
				AMTranType.Adjustment : AMTranType.Receipt;
			var invtMultNew = line.LastOper.GetValueOrDefault() || line.IsScrap == true
				? AMTranType.InvtMult(tranTypeNew, line.Qty)
				: 0;

#if DEBUG
			AMDebug.TraceWriteMethodName($"TranType = {tranTypeNew} (old value = {tranTypeOld}); InvtMult = {invtMultNew} (old value = {invtMultOld})");
#endif
			var syncSplits = false;
			if (invtMultNew != line.InvtMult)
			{
				syncSplits |= line.InvtMult != null;
				LineCache.SetValueExt<AMMTran.invtMult>(line, invtMultNew);
			}

			if (tranTypeNew != line.TranType)
			{
				syncSplits |= line.TranType != null;
				LineCache.SetValueExt<AMMTran.tranType>(line, tranTypeNew);
			}

			if (syncSplits)
			{
				SyncSplitTranType(line);
			}
		}

		public override void CreateNumbers(AMMTran Row, decimal BaseQty)
		{
			base.CreateNumbers(Row, BaseQty);
			if (Row.IsLotSerialPreassigned == true)
			{
				BuildPreassignNumbers(Row);
			}
		}

		public override void TruncateNumbers(AMMTran Row, decimal BaseQty)
		{
			base.TruncateNumbers(Row, BaseQty);
			if (Row.IsLotSerialPreassigned == true)
			{
				BuildPreassignNumbers(Row);
			}
		}

		public void BuildPreassignNumbers(AMMTran Row)
		{
			Dictionary<string, decimal?> AssignedSplits = new Dictionary<string, decimal?>();
			foreach (AMProdItemSplit split in SelectFrom<AMProdItemSplit>.Where<AMProdItemSplit.orderType.IsEqual<@P.AsString>
			.And<AMProdItemSplit.prodOrdID.IsEqual<@P.AsString>>>.View.Select(Base, Row.OrderType, Row.ProdOrdID))
			{
				if (split.QtyRemaining.GetValueOrDefault() > 0)
				{
					AssignedSplits.Add(split.LotSerialNbr, split.QtyRemaining);
				}
			}
			if (AssignedSplits.Count > 0)
			{
				foreach (AMMTranSplit detail in PXParentAttribute.SelectSiblings(SplitCache, (AMMTranSplit)Row, typeof(AMMTran)))
				{
					if (AssignedSplits.Count == 0)
                    {
						SplitCache.Delete(detail);
						continue;
					}
						
					if (AssignedSplits.ContainsKey(detail.LotSerialNbr) && AssignedSplits[detail.LotSerialNbr] > detail.Qty)
					{
						continue;
					}

					detail.LotSerialNbr = AssignedSplits.ElementAt(0).Key;
					if (detail.Qty >= AssignedSplits.ElementAt(0).Value)
					{
						detail.Qty = AssignedSplits.ElementAt(0).Value;
					}
					AssignedSplits.Remove(detail.LotSerialNbr);
					SplitCache.MarkUpdated(detail);
				}
			}

		}
	}	
}
