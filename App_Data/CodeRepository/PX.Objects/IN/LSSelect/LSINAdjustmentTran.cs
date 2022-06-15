using System;
using PX.Data;

namespace PX.Objects.IN
{
	[Obsolete] // the class is moved from ../Descriptor/Attribute.cs as is
	public class LSINAdjustmentTran : LSINTran
	{
		public LSINAdjustmentTran(PXGraph graph)
			: base(graph)
		{
			graph.FieldVerifying.AddHandler<INTran.uOM>(INTran_UOM_FieldVerifying);
		}

		public virtual void INTran_UOM_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((INTran)e.Row).InventoryID);
			if (item != null && INLotSerialNbrAttribute.IsTrackSerial(item, ((INTran)e.Row).TranType, ((INTran)e.Row).InvtMult))
			{
				object newval;

				sender.RaiseFieldDefaulting<INTran.uOM>(e.Row, out newval);

				if (object.Equals(newval, e.NewValue) == false)
				{
					e.NewValue = newval;
					sender.RaiseExceptionHandling<INTran.uOM>(e.Row, null, new PXSetPropertyException(Messages.SerialItemAdjustment_UOMUpdated, PXErrorLevel.Warning, newval));
				}
			}
		}

		public override void Master_Qty_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((INTran)e.Row).InventoryID);

			if (item != null && INLotSerialNbrAttribute.IsTrackSerial(item, ((INTran)e.Row).TranType, ((INTran)e.Row).InvtMult))
			{
				if (e.NewValue != null && e.NewValue is decimal && (decimal)e.NewValue != 0m && (decimal)e.NewValue != 1m && (decimal)e.NewValue != -1m)
				{
					e.NewValue = (decimal)e.NewValue > 0 ? 1m : -1m;
					sender.RaiseExceptionHandling<INTran.qty>(e.Row, null, new PXSetPropertyException(Messages.SerialItemAdjustment_LineQtyUpdated, PXErrorLevel.Warning, ((InventoryItem)item).BaseUnit));
				}
			}
		}

		public override void CreateNumbers(PXCache sender, INTran Row, decimal BaseQty, bool AlwaysAutoNextNbr)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, Row.InventoryID);
			INLotSerClass itemclass = item;

			if (itemclass.LotSerTrack != INLotSerTrack.NotNumbered &&
				 itemclass.LotSerAssign == INLotSerAssign.WhenReceived &&
				 (Row.SubItemID == null || Row.LocationID == null))
				return;

			base.CreateNumbers(sender, Row, BaseQty, AlwaysAutoNextNbr);
		}
		public override void IssueNumbers(PXCache sender, INTran Row, decimal BaseQty)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, Row.InventoryID);
			INLotSerClass itemclass = item;

			if (itemclass.LotSerTrack != INLotSerTrack.NotNumbered &&
				 itemclass.LotSerAssign == INLotSerAssign.WhenReceived &&
				 (Row.LotSerialNbr == null || Row.SubItemID == null || Row.LocationID == null))
				return;

			base.IssueNumbers(sender, Row, BaseQty);
		}
	}
}
