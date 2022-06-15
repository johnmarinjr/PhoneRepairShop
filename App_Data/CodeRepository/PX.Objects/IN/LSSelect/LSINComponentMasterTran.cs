using System;
using System.Collections.Generic;

using PX.Data;

using PX.Objects.Common.Exceptions;

namespace PX.Objects.IN
{
	[Obsolete] // the class is moved from ../KitAssemblyEntry.cs as is
	public class LSINComponentMasterTran : LSSelect<INKitRegister, INKitTranSplit,
		Where<INKitTranSplit.docType, Equal<Current<INKitRegister.docType>>,
		And<INKitTranSplit.refNbr, Equal<Current<INKitRegister.refNbr>>>>>
	{
		#region Ctor
		public LSINComponentMasterTran(PXGraph graph)
			: base(graph)
		{
			graph.FieldDefaulting.AddHandler<INKitTranSplit.subItemID>(INTranSplit_SubItemID_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<INKitTranSplit.locationID>(INTranSplit_LocationID_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<INKitTranSplit.invtMult>(INTranSplit_InvtMult_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<INKitTranSplit.lotSerialNbr>(INTranSplit_LotSerialNbr_FieldDefaulting);
			graph.FieldVerifying.AddHandler<INKitTranSplit.qty>(INTranSplit_Qty_FieldVerifying);
			graph.FieldVerifying.AddHandler<INKitRegister.qty>(INTran_Qty_FieldVerifying);
			graph.RowUpdated.AddHandler<INKitRegister>(INKitRegister_RowUpdated);
		}
		#endregion

		#region Implementation
		public override void Availability_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			if (!PXLongOperation.Exists(sender.Graph.UID))
			{
				IStatus availability = AvailabilityFetch(sender, (INKitRegister)e.Row, ((INKitRegister)e.Row)?.Released == true ? AvailabilityFetchMode.None : AvailabilityFetchMode.ExcludeCurrent);

				if (availability != null)
				{
					PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((INKitRegister)e.Row).KitInventoryID);

					availability.QtyOnHand = INUnitAttribute.ConvertFromBase<INKitRegister.kitInventoryID, INKitRegister.uOM>(sender, e.Row, (decimal)availability.QtyOnHand, INPrecision.QUANTITY);
					availability.QtyAvail = INUnitAttribute.ConvertFromBase<INKitRegister.kitInventoryID, INKitRegister.uOM>(sender, e.Row, (decimal)availability.QtyAvail, INPrecision.QUANTITY);
					availability.QtyNotAvail = INUnitAttribute.ConvertFromBase<INKitRegister.kitInventoryID, INKitRegister.uOM>(sender, e.Row, (decimal)availability.QtyNotAvail, INPrecision.QUANTITY);
					availability.QtyHardAvail = INUnitAttribute.ConvertFromBase<INKitRegister.kitInventoryID, INKitRegister.uOM>(sender, e.Row, (decimal)availability.QtyHardAvail, INPrecision.QUANTITY);
					availability.QtyActual = INUnitAttribute.ConvertFromBase<INKitRegister.inventoryID, INKitRegister.uOM>(sender, e.Row, (decimal)availability.QtyActual, INPrecision.QUANTITY);

					e.ReturnValue = PXMessages.LocalizeFormatNoPrefix(Messages.Availability_ActualInfo,
						sender.GetValue<INTran.uOM>(e.Row),
						FormatQty(availability.QtyOnHand),
						FormatQty(availability.QtyAvail),
						FormatQty(availability.QtyHardAvail),
						FormatQty(availability.QtyActual));

					AvailabilityCheck(sender, (INKitRegister)e.Row, availability);
				}
				else
				{
					e.ReturnValue = string.Empty;
				}
			}
			else
			{
				e.ReturnValue = string.Empty;
			}

			base.Availability_FieldSelecting(sender, e);
		}

		public override INKitTranSplit Convert(INKitRegister item)
		{
			using (InvtMultScope<INKitRegister> ms = new InvtMultScope<INKitRegister>(item))
			{
				INKitTranSplit ret = item;
				//baseqty will be overriden in all cases but AvailabilityFetch
				ret.BaseQty = item.BaseQty - item.UnassignedQty;
				return ret;
			}
		}

		public void ThrowFieldIsEmpty<Field>(PXCache sender, object data)
			where Field : IBqlField
		{
			if (sender.RaiseExceptionHandling<Field>(data, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, $"[{typeof(Field).Name}]")))
			{
				throw new PXRowPersistingException(typeof(Field).Name, null, ErrorMessages.FieldIsEmpty, typeof(Field).Name);
			}
		}

		public virtual void INTranSplit_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if (e.Row != null && ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update))
			{
				if (((INKitTranSplit)e.Row).BaseQty != 0m && ((INKitTranSplit)e.Row).LocationID == null)
				{
					ThrowFieldIsEmpty<INKitTranSplit.locationID>(sender, e.Row);
				}
			}
		}
		protected override void Master_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
			{
				INKitRegister row = e.Row as INKitRegister;
				if (row != null && row.Hold == false && Math.Abs((decimal)row.BaseQty) >= 0.0000005m && (row.UnassignedQty >= 0.0000005m || row.UnassignedQty <= -0.0000005m))
				{
					if (sender.RaiseExceptionHandling<INKitRegister.qty>(e.Row, row.Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned)))
					{
						throw new PXRowPersistingException(typeof(INKitRegister.qty).Name, row.Qty, Messages.BinLotSerialNotAssigned);
					}
				}
			}
			base.Master_RowPersisting(sender, e);
		}
		protected virtual void INKitRegister_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (!sender.ObjectsEqual<INKitRegister.hold>(e.Row, e.OldRow) && (bool?)sender.GetValue<INKitRegister.hold>(e.Row) == false)
			{
				if (((INKitRegister)e.Row).UnassignedQty != 0)
				{
					sender.RaiseExceptionHandling<INKitRegister.qty>(e.Row, ((INKitRegister)e.Row).Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned));
				}
			}
		}

		public virtual void INTranSplit_SubItemID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(INKitRegister)];
			if (cache.Current != null && (e.Row == null || ((INKitRegister)cache.Current).LineNbr == ((INKitTranSplit)e.Row).LineNbr))
			{
				e.NewValue = ((INKitRegister)cache.Current).SubItemID;
				e.Cancel = true;
			}
		}

		public virtual void INTranSplit_LocationID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(INKitRegister)];
			if (cache.Current != null && (e.Row == null || ((INKitRegister)cache.Current).LineNbr == ((INKitTranSplit)e.Row).LineNbr))
			{
				e.NewValue = ((INKitRegister)cache.Current).LocationID;
				e.Cancel = true;
			}
		}

		public virtual void INTranSplit_InvtMult_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(INKitRegister)];
			if (cache.Current != null && (e.Row == null || ((INKitRegister)cache.Current).LineNbr == ((INKitTranSplit)e.Row).LineNbr))
			{
				using (InvtMultScope<INKitRegister> ms = new InvtMultScope<INKitRegister>((INKitRegister)cache.Current))
				{
					e.NewValue = ((INKitRegister)cache.Current).InvtMult;
					e.Cancel = true;
				}
			}
		}

		public virtual void INTranSplit_LotSerialNbr_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((INKitTranSplit)e.Row).InventoryID);

			if (item != null)
			{
				object InvtMult = ((INKitTranSplit)e.Row).InvtMult;
				if (InvtMult == null)
				{
					sender.RaiseFieldDefaulting<INKitTranSplit.invtMult>(e.Row, out InvtMult);
				}

				object TranType = ((INKitTranSplit)e.Row).TranType;
				if (TranType == null)
				{
					sender.RaiseFieldDefaulting<INKitTranSplit.tranType>(e.Row, out TranType);
				}

				INLotSerTrack.Mode mode = INLotSerialNbrAttribute.TranTrackMode(item, (string)TranType, (short?)InvtMult);
				if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
				{
					ILotSerNumVal lotSerNum = ReadLotSerNumVal(sender, item);
					foreach (INKitTranSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<INKitTranSplit>(sender, item, lotSerNum, mode, 1m))
					{
						e.NewValue = lssplit.LotSerialNbr;
						e.Cancel = true;
					}
				}
				//otherwise default via attribute
			}
		}

		public virtual void INTranSplit_Qty_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((INKitTranSplit)e.Row).InventoryID);

			if (item != null && INLotSerialNbrAttribute.IsTrackSerial(item, ((INKitTranSplit)e.Row).TranType, ((INKitTranSplit)e.Row).InvtMult))
			{
				if (e.NewValue != null && e.NewValue is decimal && (decimal)e.NewValue != 0m && (decimal)e.NewValue != 1m)
				{
					e.NewValue = 1m;
				}
			}
		}

		public virtual void INTran_Qty_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if ((decimal?)e.NewValue < 0m)
			{
				throw new PXSetPropertyException(CS.Messages.Entry_GE, PXErrorLevel.Error, (int)0);
			}
		}

		protected override void RaiseQtyExceptionHandling(PXCache sender, object row, object newValue, PXExceptionInfo ei)
		{
			if (row is INKitRegister)
			{
				sender.RaiseExceptionHandling<INKitRegister.qty>(row, null, new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning, sender.GetStateExt<INKitRegister.kitInventoryID>(row), sender.GetStateExt<INKitRegister.subItemID>(row), sender.GetStateExt<INKitRegister.siteID>(row), sender.GetStateExt<INKitRegister.locationID>(row), sender.GetValue<INKitRegister.lotSerialNbr>(row)));
			}
			else
			{
				sender.RaiseExceptionHandling<INKitTranSplit.qty>(row, null, new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning, sender.GetStateExt<INKitTranSplit.inventoryID>(row), sender.GetStateExt<INKitTranSplit.subItemID>(row), sender.GetStateExt<INKitTranSplit.siteID>(row), sender.GetStateExt<INKitTranSplit.locationID>(row), sender.GetValue<INKitTranSplit.lotSerialNbr>(row)));
			}
		}

		protected override object[] SelectDetail(PXCache sender, INKitRegister row)
		{
			PXSelectBase<INKitTranSplit> select = new PXSelect<INKitTranSplit,
			Where<INKitTranSplit.docType, Equal<Required<INKitRegister.docType>>,
			And<INKitTranSplit.refNbr, Equal<Required<INKitRegister.refNbr>>,
			And<INKitTranSplit.lineNbr, Equal<Required<INKitRegister.kitLineNbr>>>>>>(_Graph);

			PXResultset<INKitTranSplit> res = select.Select(row.DocType, row.RefNbr, row.KitLineNbr);

			List<object> list = new List<object>(res.Count);

			foreach (INKitTranSplit detail in res)
			{
				list.Add(detail);
			}

			return list.ToArray();
		}

		protected override object[] SelectDetail(PXCache sender, INKitTranSplit row)
		{
			INKitRegister kitRow = (INKitRegister)PXParentAttribute.SelectParent(sender, row, typeof(INKitRegister));

			return SelectDetail(sender, kitRow);
		}

		#endregion
	}
}
