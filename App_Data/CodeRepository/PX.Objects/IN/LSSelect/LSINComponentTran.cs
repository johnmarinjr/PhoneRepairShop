using System;
using System.Collections.Generic;

using PX.Data;

using PX.Objects.Common.Exceptions;

namespace PX.Objects.IN
{
	[Obsolete] // the class is moved from ../KitAssemblyEntry.cs as is
	public class LSINComponentTran : LSSelect<INComponentTran, INComponentTranSplit,
		Where<INComponentTranSplit.docType, Equal<Current<INKitRegister.docType>>,
		And<INComponentTranSplit.refNbr, Equal<Current<INKitRegister.refNbr>>>>>
	{
		#region Ctor
		public LSINComponentTran(PXGraph graph)
			: base(graph)
		{
			graph.FieldDefaulting.AddHandler<INComponentTranSplit.subItemID>(INTranSplit_SubItemID_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<INComponentTranSplit.locationID>(INTranSplit_LocationID_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<INComponentTranSplit.invtMult>(INTranSplit_InvtMult_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<INComponentTranSplit.lotSerialNbr>(INTranSplit_LotSerialNbr_FieldDefaulting);
			graph.FieldVerifying.AddHandler<INComponentTranSplit.qty>(INTranSplit_Qty_FieldVerifying);
			graph.FieldVerifying.AddHandler<INComponentTran.qty>(INTran_Qty_FieldVerifying);
			graph.RowUpdated.AddHandler<INKitRegister>(INKitRegister_RowUpdated);
			graph.RowUpdated.AddHandler<INComponentTran>(INComponentTran_RowUpdated);
		}
		#endregion

		#region Implementation

		protected override void SetEditMode()
		{
			if (!Initialized)
			{
				PXUIFieldAttribute.SetEnabled(MasterCache, null, true);
				PXUIFieldAttribute.SetEnabled(DetailCache, null, true);
				PXUIFieldAttribute.SetEnabled(DetailCache, null, "UOM", false);
				Initialized = true;
			}
		}

		protected virtual void INKitRegister_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (!sender.ObjectsEqual<INKitRegister.hold>(e.Row, e.OldRow) && (bool?)sender.GetValue<INKitRegister.hold>(e.Row) == false)
			{
				PXCache cache = sender.Graph.Caches[typeof(INComponentTran)];

				foreach (INComponentTran item in PXParentAttribute.SelectSiblings(cache, null, typeof(INKitRegister)))
				{
					if (Math.Abs((decimal)item.BaseQty) >= 0.0000005m && (item.UnassignedQty >= 0.0000005m || item.UnassignedQty <= -0.0000005m))
					{
						cache.RaiseExceptionHandling<INComponentTran.qty>(item, item.Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned));

						cache.MarkUpdated(item);
					}
				}
			}
		}

		protected override void Master_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
			{
				PXCache cache = sender.Graph.Caches[typeof(INKitRegister)];
				object doc = PXParentAttribute.SelectParent(sender, e.Row, typeof(INKitRegister)) ?? cache.Current;

				bool? OnHold = (bool?)cache.GetValue<INKitRegister.hold>(doc);

				if (OnHold == false && Math.Abs((decimal)((INComponentTran)e.Row).BaseQty) >= 0.0000005m && (((INComponentTran)e.Row).UnassignedQty >= 0.0000005m || ((INComponentTran)e.Row).UnassignedQty <= -0.0000005m))
				{
					if (sender.RaiseExceptionHandling<INComponentTran.qty>(e.Row, ((INComponentTran)e.Row).Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned)))
					{
						throw new PXRowPersistingException(typeof(INComponentTran.qty).Name, ((INComponentTran)e.Row).Qty, Messages.BinLotSerialNotAssigned);
					}
				}
			}
			base.Master_RowPersisting(sender, e);
		}

		protected virtual void INComponentTran_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			INComponentTran row = (INComponentTran)e.Row;
			if (row == null) return;
			if (!PXLongOperation.Exists(sender.Graph.UID))
			{
				IStatus availability = AvailabilityFetch(sender, (INComponentTran)e.Row, AvailabilityFetchMode.ExcludeCurrent);

				if (availability != null)
				{
					PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((INComponentTran)e.Row).InventoryID);

					availability.QtyOnHand = INUnitAttribute.ConvertFromBase<INComponentTran.inventoryID, INComponentTran.uOM>(sender, e.Row, (decimal)availability.QtyOnHand, INPrecision.QUANTITY);
					availability.QtyAvail = INUnitAttribute.ConvertFromBase<INComponentTran.inventoryID, INComponentTran.uOM>(sender, e.Row, (decimal)availability.QtyAvail, INPrecision.QUANTITY);
					availability.QtyNotAvail = INUnitAttribute.ConvertFromBase<INComponentTran.inventoryID, INComponentTran.uOM>(sender, e.Row, (decimal)availability.QtyNotAvail, INPrecision.QUANTITY);
					availability.QtyHardAvail = INUnitAttribute.ConvertFromBase<INComponentTran.inventoryID, INComponentTran.uOM>(sender, e.Row, (decimal)availability.QtyHardAvail, INPrecision.QUANTITY);

					AvailabilityCheck(sender, (INComponentTran)e.Row, availability);
				}
			}
		}
		protected override DateTime? ExpireDateByLot(PXCache sender, ILSMaster item, ILSMaster master)
		{
			if (master != null && master.InvtMult > 0)
			{
				item.ExpireDate = null;
				return base.ExpireDateByLot(sender, item, null);
			}
			else return base.ExpireDateByLot(sender, item, master);
		}

		public override void Availability_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			if (!PXLongOperation.Exists(sender.Graph.UID))
			{
				IStatus availability = AvailabilityFetch(sender, (INComponentTran)e.Row, ((INComponentTran)e.Row)?.Released == true ? AvailabilityFetchMode.None : AvailabilityFetchMode.ExcludeCurrent);

				if (availability != null)
				{
					PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((INComponentTran)e.Row).InventoryID);

					availability.QtyOnHand = INUnitAttribute.ConvertFromBase<INComponentTran.inventoryID, INComponentTran.uOM>(sender, e.Row, (decimal)availability.QtyOnHand, INPrecision.QUANTITY);
					availability.QtyAvail = INUnitAttribute.ConvertFromBase<INComponentTran.inventoryID, INComponentTran.uOM>(sender, e.Row, (decimal)availability.QtyAvail, INPrecision.QUANTITY);
					availability.QtyNotAvail = INUnitAttribute.ConvertFromBase<INComponentTran.inventoryID, INComponentTran.uOM>(sender, e.Row, (decimal)availability.QtyNotAvail, INPrecision.QUANTITY);
					availability.QtyHardAvail = INUnitAttribute.ConvertFromBase<INComponentTran.inventoryID, INComponentTran.uOM>(sender, e.Row, (decimal)availability.QtyHardAvail, INPrecision.QUANTITY);
					availability.QtyActual = INUnitAttribute.ConvertFromBase<INComponentTran.inventoryID, INComponentTran.uOM>(sender, e.Row, (decimal)availability.QtyActual, INPrecision.QUANTITY);

					e.ReturnValue = PXMessages.LocalizeFormatNoPrefix(Messages.Availability_ActualInfo,
						sender.GetValue<INTran.uOM>(e.Row),
						FormatQty(availability.QtyOnHand),
						FormatQty(availability.QtyAvail),
						FormatQty(availability.QtyHardAvail),
						FormatQty(availability.QtyActual));
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

		protected override IEnumerable<PXExceptionInfo> GetAvailabilityCheckErrors(PXCache sender, ILSMaster Row, IStatus availability)
		{
			foreach (var errorInfo in base.GetAvailabilityCheckErrors(sender, Row, availability))
				yield return errorInfo;
			if (Row.InvtMult == -1 && Row.BaseQty > 0m)
			{
				if (availability != null && availability.QtyAvail < Row.Qty)
				{
					switch (GetWarningLevel(availability))
					{
						case AvailabilityWarningLevel.LotSerial:
							yield return new PXExceptionInfo(Messages.StatusCheck_QtyLotSerialNegative);
							break;
						case AvailabilityWarningLevel.Location:
							yield return new PXExceptionInfo(Messages.StatusCheck_QtyLocationNegative);
							break;
						case AvailabilityWarningLevel.Site:
							yield return new PXExceptionInfo(Messages.StatusCheck_QtyNegative);
							break;
					}
				}
			}
		}

		public override INComponentTranSplit Convert(INComponentTran item)
		{
			using (InvtMultScope<INComponentTran> ms = new InvtMultScope<INComponentTran>(item))
			{
				INComponentTranSplit ret = item;
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
				if (((INComponentTranSplit)e.Row).BaseQty != 0m && ((INComponentTranSplit)e.Row).LocationID == null)
				{
					ThrowFieldIsEmpty<INComponentTranSplit.locationID>(sender, e.Row);
				}
			}
		}

		public virtual void INTranSplit_SubItemID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(INComponentTran)];
			if (cache.Current != null && (e.Row == null || ((INComponentTran)cache.Current).LineNbr == ((INComponentTranSplit)e.Row).LineNbr))
			{
				e.NewValue = ((INComponentTran)cache.Current).SubItemID;
				e.Cancel = true;
			}
		}

		public virtual void INTranSplit_LocationID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(INComponentTran)];
			if (cache.Current != null && (e.Row == null || ((INComponentTran)cache.Current).LineNbr == ((INComponentTranSplit)e.Row).LineNbr))
			{
				e.NewValue = ((INComponentTran)cache.Current).LocationID;
				e.Cancel = true;
			}
		}

		public virtual void INTranSplit_InvtMult_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(INComponentTran)];
			if (cache.Current != null && (e.Row == null || ((INComponentTran)cache.Current).LineNbr == ((INComponentTranSplit)e.Row).LineNbr))
			{
				using (InvtMultScope<INComponentTran> ms = new InvtMultScope<INComponentTran>((INComponentTran)cache.Current))
				{
					e.NewValue = ((INComponentTran)cache.Current).InvtMult;
					e.Cancel = true;
				}
			}
		}

		public virtual void INTranSplit_LotSerialNbr_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((INComponentTranSplit)e.Row).InventoryID);

			if (item != null)
			{
				object InvtMult = ((INComponentTranSplit)e.Row).InvtMult;
				if (InvtMult == null)
				{
					sender.RaiseFieldDefaulting<INComponentTranSplit.invtMult>(e.Row, out InvtMult);
				}

				object TranType = ((INComponentTranSplit)e.Row).TranType;
				if (TranType == null)
				{
					sender.RaiseFieldDefaulting<INComponentTranSplit.tranType>(e.Row, out TranType);
				}

				INLotSerTrack.Mode mode = INLotSerialNbrAttribute.TranTrackMode(item, (string)TranType, (short?)InvtMult);
				if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
				{
					ILotSerNumVal lotSerNum = ReadLotSerNumVal(sender, item);
					foreach (INComponentTranSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<INComponentTranSplit>(sender, item, lotSerNum, mode, 1m))
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
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((INComponentTranSplit)e.Row).InventoryID);

			if (item != null && INLotSerialNbrAttribute.IsTrackSerial(item, ((INComponentTranSplit)e.Row).TranType, ((INComponentTranSplit)e.Row).InvtMult))
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
			if (row is INComponentTran)
			{
				sender.RaiseExceptionHandling<INComponentTran.qty>(row, null, new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning, sender.GetStateExt<INComponentTran.inventoryID>(row), sender.GetStateExt<INComponentTran.subItemID>(row), sender.GetStateExt<INComponentTran.siteID>(row), sender.GetStateExt<INComponentTran.locationID>(row), sender.GetValue<INTran.lotSerialNbr>(row)));
			}
			else
			{
				sender.RaiseExceptionHandling<INComponentTranSplit.qty>(row, null, new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning, sender.GetStateExt<INComponentTranSplit.inventoryID>(row), sender.GetStateExt<INComponentTranSplit.subItemID>(row), sender.GetStateExt<INComponentTranSplit.siteID>(row), sender.GetStateExt<INComponentTranSplit.locationID>(row), sender.GetValue<INComponentTranSplit.lotSerialNbr>(row)));
			}
		}


		#endregion
	}
}
