using System;
using System.Collections.Generic;

using PX.Data;
using PX.Objects.Common.Exceptions;

using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.IN
{
	[Obsolete] // the class is moved from ../Descriptor/Attribute.cs as is
	public class LSINTran : LSSelect<INTran, INTranSplit,
		Where<INTranSplit.docType, Equal<Current<INRegister.docType>>,
		And<INTranSplit.refNbr, Equal<Current<INRegister.refNbr>>>>>
	{
		#region Ctor
		public LSINTran(PXGraph graph)
			: base(graph)
		{
			this.MasterQtyField = typeof(INTran.qty);
			graph.FieldDefaulting.AddHandler<INTranSplit.subItemID>(INTranSplit_SubItemID_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<INTranSplit.locationID>(INTranSplit_LocationID_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<INTranSplit.invtMult>(INTranSplit_InvtMult_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<INTranSplit.lotSerialNbr>(INTranSplit_LotSerialNbr_FieldDefaulting);
			graph.RowPersisting.AddHandler<INTranSplit>(INTranSplit_RowPersisting);
			graph.RowUpdated.AddHandler<INRegister>(INRegister_RowUpdated);
			graph.RowSelected.AddHandler<INTran>(INTran_RowSelected);
		}

		#endregion

		#region Implementation
		protected virtual void INRegister_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (!sender.ObjectsEqual<INRegister.hold>(e.Row, e.OldRow) && (bool?)sender.GetValue<INRegister.hold>(e.Row) == false)
			{
				PXCache cache = sender.Graph.Caches[typeof(INTran)];

				foreach (INTran item in PXParentAttribute.SelectSiblings(cache, null, typeof(INRegister)))
				{
					if (Math.Abs((decimal)item.BaseQty) >= 0.0000005m && (item.UnassignedQty >= 0.0000005m || item.UnassignedQty <= -0.0000005m))
					{
						cache.RaiseExceptionHandling<INTran.qty>(item, item.Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned));

						cache.MarkUpdated(item);
					}
				}
			}
		}

		private void INTran_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			var row = (INTran)e.Row;
			if (row == null)
				return;

			InventoryItem ii = InventoryItem.PK.Find(MasterCache.Graph, row.InventoryID);
			PXUIFieldAttribute.SetReadOnly<INTranSplit.inventoryID>(DetailCache, null, ii == null || !(ii.StkItem == false && (ii.KitItem ?? false)));
		}

		protected override void Master_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
			{
				PXCache cache = sender.Graph.Caches[typeof(INRegister)];
				object doc = PXParentAttribute.SelectParent(sender, e.Row, typeof(INRegister)) ?? cache.Current;

				bool? OnHold = (bool?)cache.GetValue<INRegister.hold>(doc);

				if (OnHold == false && Math.Abs((decimal)((INTran)e.Row).BaseQty) >= 0.0000005m && (((INTran)e.Row).UnassignedQty >= 0.0000005m || ((INTran)e.Row).UnassignedQty <= -0.0000005m))
				{
					if (sender.RaiseExceptionHandling<INTran.qty>(e.Row, ((INTran)e.Row).Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned)))
					{
						throw new PXRowPersistingException(typeof(INTran.qty).Name, ((INTran)e.Row).Qty, Messages.BinLotSerialNotAssigned);
					}
				}
			}
			base.Master_RowPersisting(sender, e);
		}

		public override void Availability_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			if (!PXLongOperation.Exists(sender.Graph.UID))
			{
				INTran tran = (INTran)e.Row;
				AvailabilityFetchMode fetchMode = tran?.Released == true ? AvailabilityFetchMode.None : AvailabilityFetchMode.ExcludeCurrent;
				IStatus availability = GetAvailability(sender, tran, fetchMode);
				if (availability != null)
				{
					if (!PXAccess.FeatureInstalled<FeaturesSet.materialManagement>())
					{
						e.ReturnValue = BuildAvailabilityStatusLine(sender, tran, availability);
						AvailabilityCheck(sender, tran, availability);
					}
					else
					{
						IStatus availabilityProject = GetAvailability(sender, tran, fetchMode | AvailabilityFetchMode.Project);
						if (availabilityProject != null)
						{
							e.ReturnValue = BuildAvailabilityStatusLine(sender, tran, availability, availabilityProject);
							AvailabilityCheck(sender, tran, availabilityProject);
						}
					}
				}
			}
			else
			{
				e.ReturnValue = string.Empty;
			}

			base.Availability_FieldSelecting(sender, e);
		}

		private IStatus GetAvailability(PXCache sender, INTran tran, AvailabilityFetchMode fetchMode)
		{
			IStatus availability = AvailabilityFetch(sender, tran, fetchMode);

			if (availability != null)
			{
				availability.QtyOnHand = INUnitAttribute.ConvertFromBase<INTran.inventoryID, INTran.uOM>(sender, tran, (decimal)availability.QtyOnHand, INPrecision.QUANTITY);
				availability.QtyAvail = INUnitAttribute.ConvertFromBase<INTran.inventoryID, INTran.uOM>(sender, tran, (decimal)availability.QtyAvail, INPrecision.QUANTITY);
				availability.QtyNotAvail = INUnitAttribute.ConvertFromBase<INTran.inventoryID, INTran.uOM>(sender, tran, (decimal)availability.QtyNotAvail, INPrecision.QUANTITY);
				availability.QtyHardAvail = INUnitAttribute.ConvertFromBase<INTran.inventoryID, INTran.uOM>(sender, tran, (decimal)availability.QtyHardAvail, INPrecision.QUANTITY);
				availability.QtyActual = INUnitAttribute.ConvertFromBase<INTran.inventoryID, INTran.uOM>(sender, tran, (decimal)availability.QtyActual, INPrecision.QUANTITY);
			}

			return availability;
		}

		private string BuildAvailabilityStatusLine(PXCache sender, INTran tran, IStatus availability)
		{
			return string.Format(
					PXMessages.LocalizeNoPrefix(Messages.Availability_ActualInfo),
					sender.GetValue<INTran.uOM>(tran),
					FormatQty(availability.QtyOnHand),
					FormatQty(availability.QtyAvail),
					FormatQty(availability.QtyHardAvail),
					FormatQty(availability.QtyActual));
		}

		private string BuildAvailabilityStatusLine(PXCache sender, INTran tran, IStatus availability, IStatus availabilityProject)
		{
			return string.Format(
					PXMessages.LocalizeNoPrefix(Messages.Availability_ActualInfo_Project),
					sender.GetValue<INTran.uOM>(tran),
					FormatQty(availabilityProject.QtyOnHand),
					FormatQty(availabilityProject.QtyAvail),
					FormatQty(availabilityProject.QtyHardAvail),
					FormatQty(availabilityProject.QtyActual),
					FormatQty(availability.QtyOnHand),
					FormatQty(availability.QtyAvail),
					FormatQty(availability.QtyHardAvail),
					FormatQty(availability.QtyActual));
		}

		public override INTranSplit Convert(INTran item)
		{
			using (InvtMultScope<INTran> ms = new InvtMultScope<INTran>(item))
			{
				INTranSplit ret = item;
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
				if (((INTranSplit)e.Row).BaseQty != 0m && ((INTranSplit)e.Row).LocationID == null)
				{
					ThrowFieldIsEmpty<INTranSplit.locationID>(sender, e.Row);
				}
			}
		}

		public virtual void INTranSplit_SubItemID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(INTran)];
			if (cache.Current != null && (e.Row == null || ((INTran)cache.Current).LineNbr == ((INTranSplit)e.Row).LineNbr))
			{
				e.NewValue = ((INTran)cache.Current).SubItemID;
				e.Cancel = true;
			}
		}

		public virtual void INTranSplit_LocationID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(INTran)];
			if (cache.Current != null && (e.Row == null || ((INTran)cache.Current).LineNbr == ((INTranSplit)e.Row).LineNbr))
			{
				e.NewValue = ((INTran)cache.Current).LocationID;
				e.Cancel = true;
			}
		}

		public virtual void INTranSplit_InvtMult_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(INTran)];
			if (cache.Current != null && (e.Row == null || ((INTran)cache.Current).LineNbr == ((INTranSplit)e.Row).LineNbr))
			{
				using (InvtMultScope<INTran> ms = new InvtMultScope<INTran>((INTran)cache.Current))
				{
					e.NewValue = ((INTran)cache.Current).InvtMult;
					e.Cancel = true;
				}
			}
		}

		public virtual void INTranSplit_LotSerialNbr_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((INTranSplit)e.Row).InventoryID);

			if (item != null)
			{
				object InvtMult = ((INTranSplit)e.Row).InvtMult;
				if (InvtMult == null)
				{
					sender.RaiseFieldDefaulting<INTranSplit.invtMult>(e.Row, out InvtMult);
				}

				object TranType = ((INTranSplit)e.Row).TranType;
				if (TranType == null)
				{
					sender.RaiseFieldDefaulting<INTranSplit.tranType>(e.Row, out TranType);
				}

				INLotSerTrack.Mode mode = GetTranTrackMode((ILSMaster)e.Row, item);
				if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
				{
					ILotSerNumVal lotSerNum = ReadLotSerNumVal(sender, item);
					foreach (INTranSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<INTranSplit>(sender, item, lotSerNum, mode, 1m))
					{
						e.NewValue = lssplit.LotSerialNbr;
						e.Cancel = true;
					}
				}
				//otherwise default via attribute
			}
		}

		public override void Master_Qty_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if ((decimal?)e.NewValue < 0m)
			{
				throw new PXSetPropertyException(CS.Messages.Entry_GE, PXErrorLevel.Error, (int)0);
			}
		}

		protected override void Master_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			var row = (INTran)e.Row;
			if (row.InvtMult != (short)0)
			{
				base.Master_RowInserted(sender, e);
			}
			else
			{
				//this piece of code supposed to support dropships and landed costs for dropships. ReceiptCostAdjustment is generated for landedcosts and ppv adjustments, so we need actual lotSerialNbr, thats why it has to stay
				if (row.TranType != INTranType.ReceiptCostAdjustment)
				{
					sender.SetValue<INTran.lotSerialNbr>(e.Row, null);
					sender.SetValue<INTran.expireDate>(e.Row, null);
				}
			}
		}

		protected override void Master_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
		{
			if (((INTran)e.Row).InvtMult != (short)0)
			{
				base.Master_RowDeleted(sender, e);
			}
		}

		protected override void Master_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (((INTran)e.Row).InvtMult != (short)0)
			{
				if (Equals(((INTran)e.Row).TranType, ((INTran)e.OldRow).TranType) == false)
				{
					sender.SetDefaultExt<INTran.invtMult>(e.Row);

					PXCache cache = sender.Graph.Caches[typeof(INTranSplit)];
					foreach (INTranSplit split in PXParentAttribute.SelectSiblings(cache, (INTranSplit)(INTran)e.Row, typeof(INTran)))
					{
						INTranSplit copy = PXCache<INTranSplit>.CreateCopy(split);

						split.TranType = ((INTran)e.Row).TranType;

						cache.MarkUpdated(split);
						cache.RaiseRowUpdated(split, copy);
					}
				}

				base.Master_RowUpdated(sender, e);
			}
			else
			{
				sender.SetValue<INTran.lotSerialNbr>(e.Row, null);
				sender.SetValue<INTran.expireDate>(e.Row, null);
			}
		}

		protected override IEnumerable<PXExceptionInfo> GetAvailabilityCheckErrors(PXCache sender, ILSMaster Row, IStatus availability)
		{
			foreach (var errorInfo in base.GetAvailabilityCheckErrors(sender, Row, availability))
				yield return errorInfo;
			if (Row.InvtMult == -1 && Row.BaseQty > 0m && availability != null)
			{
				INRegister doc = (INRegister)sender.Graph.Caches[typeof(INRegister)].Current;
				if (availability.QtyOnHand - Row.Qty < 0m && doc != null && doc.Released == false)
				{
					switch (GetWarningLevel(availability))
					{
						case AvailabilityWarningLevel.LotSerial:
							yield return new PXExceptionInfo(PXErrorLevel.RowWarning, Messages.StatusCheck_QtyLotSerialOnHandNegative);
							break;
						case AvailabilityWarningLevel.Location:
							yield return new PXExceptionInfo(PXErrorLevel.RowWarning, Messages.StatusCheck_QtyLocationOnHandNegative);
							break;
						case AvailabilityWarningLevel.Site:
							yield return new PXExceptionInfo(PXErrorLevel.RowWarning, Messages.StatusCheck_QtyOnHandNegative);
							break;
					}
				}
			}
		}

		protected override void RaiseQtyExceptionHandling(PXCache sender, object row, object newValue, PXExceptionInfo ei)
		{
			object[] arguments;
			if (row is INTran)
			{
				arguments = new object[]
		{
					sender.GetStateExt<INTran.inventoryID>(row),
					sender.GetStateExt<INTran.subItemID>(row),
					sender.GetStateExt<INTran.siteID>(row),
					sender.GetStateExt<INTran.locationID>(row),
					sender.GetValue<INTran.lotSerialNbr>(row)
				};
				sender.RaiseExceptionHandling<INTran.qty>(row, newValue, new PXSetPropertyException(ei.MessageFormat, errorLevel: ei.ErrorLevel ?? PXErrorLevel.Warning, args: arguments));
			}
			else
			{
				arguments = new object[]
				{
					sender.GetStateExt<INTranSplit.inventoryID>(row),
					sender.GetStateExt<INTranSplit.subItemID>(row),
					sender.GetStateExt<INTranSplit.siteID>(row),
					sender.GetStateExt<INTranSplit.locationID>(row),
					sender.GetValue<INTranSplit.lotSerialNbr>(row)
				};
				sender.RaiseExceptionHandling<INTranSplit.qty>(row, newValue, new PXSetPropertyException(ei.MessageFormat, errorLevel: ei.ErrorLevel ?? PXErrorLevel.Warning, args: arguments));
			}
		}
		public override void DefaultLotSerialNbr(PXCache sender, INTranSplit row)
		{
			if (row.DocType == INDocType.Receipt && row.TranType == INTranType.Transfer
				|| (!string.IsNullOrEmpty(row.OrigModule) && row.OrigModule != BatchModule.IN))
				row.AssignedNbr = null;
			else
				base.DefaultLotSerialNbr(sender, row);
		}
		#endregion
		protected override void SetEditMode()
		{
			if (!Initialized || PrevCorrectionMode != CorrectionMode || PrevFullMode != FullMode)
			{
				PXUIFieldAttribute.SetEnabled(MasterCache, null, false);
				PXUIFieldAttribute.SetEnabled(DetailCache, null, false);

				if (PrevCorrectionMode = CorrectionMode)
				{
					PXUIFieldAttribute.SetEnabled(MasterCache, null, nameof(INTran.LocationID), true);
					PXUIFieldAttribute.SetEnabled(MasterCache, null, nameof(INTran.LotSerialNbr), true);
					PXUIFieldAttribute.SetEnabled(MasterCache, null, nameof(INTran.ExpireDate), true);
					PXUIFieldAttribute.SetEnabled(MasterCache, null, nameof(INTran.ReasonCode), true);
					PXUIFieldAttribute.SetEnabled(MasterCache, null, nameof(INTran.ProjectID), true);
					PXUIFieldAttribute.SetEnabled(MasterCache, null, nameof(INTran.TaskID), true);
					PXUIFieldAttribute.SetEnabled(MasterCache, null, nameof(INTran.CostCodeID), true);

					PXUIFieldAttribute.SetEnabled(DetailCache, null, nameof(INTranSplit.SubItemID), true);
					PXUIFieldAttribute.SetEnabled(DetailCache, null, nameof(INTranSplit.Qty), true);
					PXUIFieldAttribute.SetEnabled(DetailCache, null, nameof(INTranSplit.LocationID), true);
					PXUIFieldAttribute.SetEnabled(DetailCache, null, nameof(INTranSplit.LotSerialNbr), true);
					PXUIFieldAttribute.SetEnabled(DetailCache, null, nameof(INTranSplit.ExpireDate), true);
				}

				if (PrevFullMode = FullMode)
				{
					PXUIFieldAttribute.SetEnabled(MasterCache, null, true);
					PXUIFieldAttribute.SetEnabled<INTran.docType>(MasterCache, null, false);
					PXUIFieldAttribute.SetEnabled<INTran.refNbr>(MasterCache, null, false);
					PXUIFieldAttribute.SetEnabled<INTran.lineNbr>(MasterCache, null, false);
					PXUIFieldAttribute.SetEnabled<INTran.invtMult>(MasterCache, null, false);
					PXUIFieldAttribute.SetEnabled<INTran.origRefNbr>(MasterCache, null, false);
					PXUIFieldAttribute.SetEnabled<INTran.acctID>(MasterCache, null, false);
					PXUIFieldAttribute.SetEnabled<INTran.subID>(MasterCache, null, false);
					PXUIFieldAttribute.SetEnabled<INTran.invtAcctID>(MasterCache, null, false);
					PXUIFieldAttribute.SetEnabled<INTran.invtSubID>(MasterCache, null, false);
					PXUIFieldAttribute.SetEnabled<INTran.cOGSAcctID>(MasterCache, null, false);
					PXUIFieldAttribute.SetEnabled<INTran.cOGSSubID>(MasterCache, null, false);
					PXUIFieldAttribute.SetEnabled<INTran.toSiteID>(MasterCache, null, false);
					PXUIFieldAttribute.SetEnabled<INTran.toLocationID>(MasterCache, null, false);
					PXUIFieldAttribute.SetEnabled<INTran.released>(MasterCache, null, false);
					PXUIFieldAttribute.SetEnabled<INTran.releasedDateTime>(MasterCache, null, false);
					PXUIFieldAttribute.SetEnabled(MasterCache, null, nameof(INTran.POReceiptNbr), false);
					PXUIFieldAttribute.SetEnabled(MasterCache, null, nameof(INTran.POReceiptType), false);
					PXUIFieldAttribute.SetEnabled(MasterCache, null, nameof(INTran.SOOrderNbr), false);
					PXUIFieldAttribute.SetEnabled(MasterCache, null, nameof(INTran.SOShipmentNbr), false);

					PXUIFieldAttribute.SetEnabled(DetailCache, null, true);
					PXUIFieldAttribute.SetEnabled(DetailCache, null, nameof(INTranSplit.UOM), false);
					PXUIFieldAttribute.SetEnabled(DetailCache, null, nameof(INTranSplit.UnitCost), false);
				}

				Initialized = true;
			}
		}

		protected override void AppendSerialStatusCmdWhere(PXSelectBase<INLotSerialStatus> cmd, INTran Row, INLotSerClass lotSerClass)
		{
			if (Row.SubItemID != null)
			{
				cmd.WhereAnd<Where<INLotSerialStatus.subItemID, Equal<Current<INLotSerialStatus.subItemID>>>>();
			}
			if (Row.LocationID != null)
			{
				cmd.WhereAnd<Where<INLotSerialStatus.locationID, Equal<Current<INLotSerialStatus.locationID>>>>();
			}
			else
			{
				switch (Row.TranType)
				{
					case INTranType.Issue:
						cmd.WhereAnd<Where<INLocation.receiptsValid, Equal<boolTrue>>>();
						break;
					case INTranType.Transfer:
						cmd.WhereAnd<Where<INLocation.transfersValid, Equal<boolTrue>>>();
						break;
					default:
						cmd.WhereAnd<Where<INLocation.salesValid, Equal<boolTrue>>>();
						break;
				}
			}

			if (lotSerClass.IsManualAssignRequired == true)
			{
				if (string.IsNullOrEmpty(Row.LotSerialNbr))
					cmd.WhereAnd<Where<boolTrue, Equal<boolFalse>>>();
				else
					cmd.WhereAnd<Where<INLotSerialStatus.lotSerialNbr, Equal<Current<INLotSerialStatus.lotSerialNbr>>>>();
			}
		}

		protected override void SetMasterQtyFromBase(PXCache sender, INTran master)
		{
			if (master.UOM == master.OrigUOM && master.BaseQty == master.BaseOrigFullQty)
			{
				master.Qty = master.OrigFullQty;
				return;
			}
			base.SetMasterQtyFromBase(sender, master);
		}
	}
}
