using PX.Data;
using System.Collections;
using PX.Data.BQL.Fluent;
using PX.Data.BQL;
using System;
using System.Linq;
using System.Collections.Generic;

namespace PX.Objects.AM
{
    public class LateAssignmentMaint : PXGraph<LateAssignmentMaint>
    {
        public PXAction<AMProdItemSplitPreassign> Cancel;
        public PXFirst<AMProdItemSplitPreassign> First;
        public PXPrevious<AMProdItemSplitPreassign> Previous;
        public PXNext<AMProdItemSplitPreassign> Next;
        public PXLast<AMProdItemSplitPreassign> Last;

        public PXSelect<AMProdItemSplitPreassign,
			Where<AMProdItemSplitPreassign.orderType, Equal<Optional<AMProdItemSplitPreassign.orderType>>>> ProdItemSplits;

		[PXHidden]
		public PXSelect<AMProdItemSplit> ProdItemSplitUpdate;

		[PXHidden]
        public PXSelect<AMProdMatlLotSerial> ProdMatlLotSerial;
        
        public PXSelect<AMProdMatlLotSerialAssigned,
            Where<AMProdMatlLotSerialAssigned.orderType, Equal<Current<AMProdItemSplitPreassign.orderType>>,
                And<AMProdMatlLotSerialAssigned.prodOrdID, Equal<Current<AMProdItemSplitPreassign.prodOrdID>>,
                And<AMProdMatlLotSerialAssigned.parentLotSerialNbr, Equal<Current<AMProdItemSplitPreassign.lotSerialNbr>>,
                And<AMProdMatlLotSerialAssigned.lotSerialNbr, NotEqual<StringEmpty>>>>>> MatlAssigned;

        public PXSelect<AMProdMatlLotSerialUnassigned,
            Where<AMProdMatlLotSerialUnassigned.orderType, Equal<Current<AMProdItemSplitPreassign.orderType>>,
                And<AMProdMatlLotSerialUnassigned.prodOrdID, Equal<Current<AMProdItemSplitPreassign.prodOrdID>>,
                And<AMProdMatlLotSerialUnassigned.lotSerialNbr, NotEqual<StringEmpty>,
                And<AMProdMatlLotSerialUnassigned.parentLotSerialNbr, Equal<StringEmpty>>>>>> MatlUnassigned;

        [PXHidden]
        public PXSetup<AMPSetup> ampsetup;

        public LateAssignmentMaint()
        {
            ProdItemSplits.AllowDelete =
                    ProdItemSplits.AllowUpdate = false;

            MatlAssigned.AllowDelete =
                MatlAssigned.AllowInsert =
                    MatlAssigned.AllowUpdate = false;

            MatlUnassigned.AllowDelete =
                MatlUnassigned.AllowInsert = false;

			PXUIFieldAttribute.SetEnabled(MatlUnassigned.Cache, null, false);
        }

		public override bool IsDirty => ProdMatlLotSerial?.Cache?.IsDirty == true;

		public static void Redirect(string orderType, string prodOrdID, string lotSerialNbr)
		{
			if(string.IsNullOrWhiteSpace(orderType) || string.IsNullOrWhiteSpace(prodOrdID))
			{
				return;
			}

			var lateAss = CreateInstance<LateAssignmentMaint>();

			lateAss.ProdItemSplits.Current = string.IsNullOrWhiteSpace(lotSerialNbr)
				? SelectFrom<AMProdItemSplitPreassign>
					.Where<AMProdItemSplitPreassign.orderType.IsEqual<@P.AsString>
						.And<AMProdItemSplitPreassign.prodOrdID.IsEqual<@P.AsString>>>
						.View.Select(lateAss, orderType, prodOrdID)
				: SelectFrom<AMProdItemSplitPreassign>
					.Where<AMProdItemSplitPreassign.orderType.IsEqual<@P.AsString>
						.And<AMProdItemSplitPreassign.prodOrdID.IsEqual<@P.AsString>
						.And<AMProdItemSplitPreassign.lotSerialNbr.IsEqual<@P.AsString>>>>
						.View.Select(lateAss, orderType, prodOrdID, lotSerialNbr);

			if(lateAss.ProdItemSplits.Current == null)
			{
				return;
			}

			PXRedirectHelper.TryRedirect(lateAss, PXRedirectHelper.WindowMode.New);
		}

		#region Buttons

		[PXUIField(DisplayName = ActionsMessages.Cancel, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXCancelButton]
		protected virtual IEnumerable cancel(PXAdapter a)
		{
			if (a.Searches.Length == 2 && a.Searches[0] != null)
			{
				var orderType = (string)a.Searches[0];
				if (orderType.Length > 2)
				{
					//Order type not entered - reset
					a.Searches[0] = null;
					a.Searches[1] = null;
				}
			}

			if (a.Searches.Length > 2)
			{
				var orderType = a.Searches[0];
				var prodOrdID = a.Searches[1];
				var lotSerialNbr = a.Searches[2];

				AMProdItemSplit prodItemSplit = null;
				if (orderType != null && prodOrdID != null && lotSerialNbr != null)
				{
					prodItemSplit = SelectFrom<AMProdItemSplit>
					.Where<AMProdItemSplit.orderType.IsEqual<@P.AsString>
						.And<AMProdItemSplit.prodOrdID.IsEqual<@P.AsString>
						.And<AMProdItemSplit.lotSerialNbr.IsEqual<@P.AsString>>>>
						.View.Select(this, orderType, prodOrdID, lotSerialNbr);
				}

				if (prodItemSplit == null)
				{
					var prodItem = (AMProdItem)SelectFrom<AMProdItem>
					.Where<AMProdItem.orderType.IsEqual<@P.AsString>
						.And<AMProdItem.prodOrdID.IsEqual<@P.AsString>>>
						.View.SelectWindowed(this, 0, 1, orderType, prodOrdID);

					if (prodItem == null)
					{
						a.Searches[1] = null;
					}

					a.Searches[2] = null;
				}
			}

			foreach (AMProdItemSplitPreassign row in (new PXCancel<AMProdItemSplitPreassign>(this, "Cancel")).Press(a))
			{
				return new object[] { row };
			}

			return new object[0];
		}

		public PXAction<AMProdItemSplit> Allocate;
        [PXUIField(DisplayName = "Allocate", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
        [PXButton]
        protected virtual IEnumerable allocate(PXAdapter adapter)
        {
            if(MatlUnassigned.Current == null)
            {
                return adapter.Get();
            }

            AllocateMaterial(MatlUnassigned.Current);

            Actions.PressSave();

            return Cancel.Press(adapter);
		}

        public PXAction<AMProdItemSplit> Unallocate;
        [PXUIField(DisplayName = "Unallocate", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
        [PXButton]
        protected virtual IEnumerable unallocate(PXAdapter adapter)
        {
            if (MatlAssigned.Current == null)
            {
                return adapter.Get();
            }

            UnallocateMaterial(MatlAssigned.Current);

            Actions.PressSave();

			return Cancel.Press(adapter);
        }
        #endregion

        protected virtual void UnallocateMaterial(AMProdMatlLotSerialAssigned matlAssigned)
        {
			var prodMatlLotSerial = GetRelatedAMProdMatlLotSerialUnassigned(matlAssigned);

            var prodMatlLotSerialDelete = (AMProdMatlLotSerial)matlAssigned;
            ProdMatlLotSerial.Delete(prodMatlLotSerialDelete);

            if (prodMatlLotSerial != null)
            {
                prodMatlLotSerial.QtyIssued += matlAssigned.QtyIssued;
                ProdMatlLotSerial.Update(prodMatlLotSerial);
            }
            else
            {
                ProdMatlLotSerial.Insert(new AMProdMatlLotSerial
                {
                    OrderType = matlAssigned.OrderType,
                    ProdOrdID = matlAssigned.ProdOrdID,
                    OperationID = matlAssigned.OperationID,
                    LineID = matlAssigned.LineID,
                    LotSerialNbr = matlAssigned.LotSerialNbr,
                    ParentLotSerialNbr = string.Empty,
                    QtyIssued = matlAssigned.QtyIssued
                });
            }

			var parentLotSerialNbr = ProdItemSplits.Current?.LotSerialNbr;
			if (matlAssigned.OrderType != null && matlAssigned.ProdOrdID != null && parentLotSerialNbr != null)
			{
				SetProdItemSplit(matlAssigned.OrderType, matlAssigned.ProdOrdID, parentLotSerialNbr);
			}
		}

		protected virtual AMProdMatlLotSerial GetRelatedAMProdMatlLotSerialUnassigned(AMProdMatlLotSerialAssigned matlAssigned)
		{
			AMProdMatlLotSerialUnassigned unassigned = PXSelect<AMProdMatlLotSerialUnassigned,
				Where<AMProdMatlLotSerialUnassigned.orderType, Equal<Required<AMProdMatlLotSerialUnassigned.orderType>>,
					And<AMProdMatlLotSerialUnassigned.prodOrdID, Equal<Required<AMProdMatlLotSerialUnassigned.prodOrdID>>,
					And<AMProdMatlLotSerialUnassigned.operationID, Equal<Required<AMProdMatlLotSerialUnassigned.operationID>>,
					And<AMProdMatlLotSerialUnassigned.lineID, Equal<Required<AMProdMatlLotSerialUnassigned.lineID>>,
					And<AMProdMatlLotSerialUnassigned.lotSerialNbr, Equal<Required<AMProdMatlLotSerialUnassigned.lotSerialNbr>>,
					And<AMProdMatlLotSerialUnassigned.parentLotSerialNbr, Equal<StringEmpty>
				>>>>>>>.Select(this, matlAssigned.OrderType, matlAssigned.ProdOrdID, matlAssigned.OperationID, matlAssigned.LineID, matlAssigned.LotSerialNbr);

			if (unassigned != null)
			{
				return (AMProdMatlLotSerial)unassigned;
			}

			var locateMe = (AMProdMatlLotSerial)matlAssigned;
			locateMe.ParentLotSerialNbr = string.Empty;
			var located = (AMProdMatlLotSerial)ProdMatlLotSerial.Cache.Locate(locateMe);
			if(located == null || ProdMatlLotSerial.Cache.GetStatus(located).IsDeleted())
			{
				return null;
			}
			return located;
		}

        protected virtual void AllocateMaterial(AMProdMatlLotSerialUnassigned matlUnassigned)
        {
			var qtyToAllocate = Math.Min(matlUnassigned?.QtyToAllocate ?? 0m, matlUnassigned?.QtyIssued ?? 0m);
			if (qtyToAllocate <= 0)
			{
				return;
			}

			AMProdMatlLotSerial unassignedUpdateDelete = ProdMatlLotSerial.Cache.LocateElse((AMProdMatlLotSerial)matlUnassigned);
			if (qtyToAllocate >= matlUnassigned.QtyIssued)
			{				
				ProdMatlLotSerial.Delete(unassignedUpdateDelete);
			}
			else
			{
				unassignedUpdateDelete.QtyIssued -= qtyToAllocate;
				ProdMatlLotSerial.Update(unassignedUpdateDelete);
			}

			var parentLotSerialNbr = ProdItemSplits.Current?.LotSerialNbr;
			var prodMatlLotSerial = GetRelatedAMProdMatlLotSerialAssigned(matlUnassigned, parentLotSerialNbr);
            if (prodMatlLotSerial != null)
            {
                prodMatlLotSerial.QtyIssued += qtyToAllocate;
                ProdMatlLotSerial.Update(prodMatlLotSerial);
            }
            else
            {
                ProdMatlLotSerial.Insert(new AMProdMatlLotSerial
                {
                    OrderType = matlUnassigned.OrderType,
                    ProdOrdID = matlUnassigned.ProdOrdID,
                    OperationID = matlUnassigned.OperationID,
                    LineID = matlUnassigned.LineID,
                    LotSerialNbr = matlUnassigned.LotSerialNbr,
                    ParentLotSerialNbr = parentLotSerialNbr,
                    QtyIssued = qtyToAllocate
                }); 
            }

			if (matlUnassigned.OrderType != null && matlUnassigned.ProdOrdID != null && parentLotSerialNbr != null)
			{
				var prodItemSplit = GetProdItemSplit(matlUnassigned.OrderType, matlUnassigned.ProdOrdID, parentLotSerialNbr);
				if (prodItemSplit != null && prodItemSplit.IsMaterialLinked != true)
				{
					prodItemSplit.IsMaterialLinked = true;
					ProdItemSplitUpdate.Update(prodItemSplit);
				}
			}
		}

		protected virtual AMProdMatlLotSerial GetRelatedAMProdMatlLotSerialAssigned(AMProdMatlLotSerialUnassigned matlUnassigned, string parentLotSerialNbr)
		{
			AMProdMatlLotSerialAssigned assigned = PXSelect<AMProdMatlLotSerialAssigned,
                Where<AMProdMatlLotSerialAssigned.orderType, Equal<Required<AMProdMatlLotSerialAssigned.orderType>>,
                    And<AMProdMatlLotSerialAssigned.prodOrdID, Equal<Required<AMProdMatlLotSerialAssigned.prodOrdID>>,
                    And<AMProdMatlLotSerialAssigned.operationID, Equal<Required<AMProdMatlLotSerialAssigned.operationID>>,
                    And<AMProdMatlLotSerialAssigned.lineID, Equal<Required<AMProdMatlLotSerialAssigned.lineID>>,
                    And<AMProdMatlLotSerialAssigned.lotSerialNbr, Equal<Required<AMProdMatlLotSerialAssigned.lotSerialNbr>>,
                    And<AMProdMatlLotSerialAssigned.parentLotSerialNbr, Equal<Required<AMProdMatlLotSerialUnassigned.parentLotSerialNbr>>
                >>>>>>>.Select(this, matlUnassigned.OrderType, matlUnassigned.ProdOrdID, matlUnassigned.OperationID,
					matlUnassigned.LineID, matlUnassigned.LotSerialNbr, parentLotSerialNbr);

			if(assigned != null)
			{
				return (AMProdMatlLotSerial)assigned;
			}

			var locateMe = (AMProdMatlLotSerial)matlUnassigned;
			locateMe.ParentLotSerialNbr = parentLotSerialNbr;
			var located = (AMProdMatlLotSerial)ProdMatlLotSerial.Cache.Locate(locateMe);
			if(located == null || ProdMatlLotSerial.Cache.GetStatus(located).IsDeleted())
			{
				return null;
			}
			return located;
		}

        protected virtual void _(Events.RowSelected<AMProdItemSplitPreassign> e)
        {
            if (e.Row?.ProdOrdID == null)
            {
                return;
            }

            SetScreenEneabled(ProductionStatus.IsReleasedTransactionStatus(e.Row?.StatusID, e.Row?.Hold, e.Row?.Function));
		}

        protected virtual void SetScreenEneabled(bool enabled)
        {
            Allocate.SetEnabled(enabled);
            Unallocate.SetEnabled(enabled);
			PXUIFieldAttribute.SetEnabled<AMProdMatlLotSerialUnassigned.qtyToAllocate>(MatlUnassigned.Cache, null, enabled);
        }

		protected virtual decimal GetAllocatedMaterialQty(AMProdMatlLotSerialUnassigned unassignedMatl)
		{
			if(unassignedMatl?.InventoryID == null)
			{
				return 0;
			}

			return GetMatchingAllocatedMaterial(unassignedMatl)?.Sum(x => x.QtyIssued.GetValueOrDefault()) ?? 0m;
		}

		protected virtual IEnumerable<AMProdMatlLotSerialAssigned> GetMatchingAllocatedMaterial(AMProdMatlLotSerialUnassigned unassignedMatl)
		{
			return MatlAssigned.Cache.Cached.RowCast<AMProdMatlLotSerialAssigned>()
				.Where(r => r.OrderType == unassignedMatl?.OrderType
				&& r.ProdOrdID == unassignedMatl?.ProdOrdID
				&& r.OperationID == unassignedMatl?.OperationID
				&& r.LineID == unassignedMatl?.LineID);
		}

		protected virtual void _(Events.FieldSelecting<AMProdMatlLotSerialUnassigned, AMProdMatlLotSerialUnassigned.qtyRequired> e)
		{
			var parent = ProdItemSplits.Current;
			if (e.Row != null && parent != null)
			{
				e.ReturnValue = ((AMProdMatl)e.Row).GetTotalReqQty(parent.BaseQty.GetValueOrDefault(), false);
				if (e.Cache.GetStatus(e.Row) != PXEntryStatus.Updated)
				{
					e.Cache.SetDefaultExt<AMProdMatlLotSerialUnassigned.qtyToAllocate>(e.Row);
				}
			}
		}

		protected virtual void _(Events.FieldDefaulting<AMProdMatlLotSerialUnassigned, AMProdMatlLotSerialUnassigned.qtyToAllocate> e)
		{
			var parent = ProdItemSplits.Current;
			if (e.Row != null && parent != null)
			{
				var assignedQty = GetAllocatedMaterialQty(e.Row);
				var qtyRequired = ((AMProdMatl)e.Row).GetTotalReqQty(parent.BaseQty.GetValueOrDefault(), false);
				e.NewValue = Math.Min(e.Row.QtyIssued.GetValueOrDefault(), (qtyRequired - assignedQty).NotLessZero());
			}
		}

		protected virtual void SetProdItemSplit(string orderType, string prodOrderId, string lotSerialNbr)
		{
			AMProdItemSplit prodItemSplit = GetProdItemSplit(orderType, prodOrderId, lotSerialNbr);
			if (prodItemSplit == null)
			{
				return;
			}

			var isLinked = false;
			foreach (AMProdMatlLotSerial result in PXSelect<AMProdMatlLotSerial
						, Where<AMProdMatlLotSerial.orderType, Equal<Required<AMProdMatlLotSerial.orderType>>
						, And<AMProdMatlLotSerial.prodOrdID, Equal<Required<AMProdMatlLotSerial.prodOrdID>>
						, And<AMProdMatlLotSerial.parentLotSerialNbr, Equal<Required<AMProdMatlLotSerial.parentLotSerialNbr>>>>>>
						.SelectWindowed(this, 0, 2, orderType, prodOrderId, lotSerialNbr))
			{
				isLinked = result.QtyIssued > 0;
			}

			if (isLinked != prodItemSplit.IsMaterialLinked.GetValueOrDefault())
			{
				prodItemSplit.IsMaterialLinked = isLinked;
				ProdItemSplitUpdate.Update(prodItemSplit);
			}
		}

		protected virtual AMProdItemSplit GetProdItemSplit(string orderType, string prodOrderId, string lotSerialNbr)
		{
			return SelectFrom<AMProdItemSplit>
				.Where<AMProdItemSplit.orderType.IsEqual<@P.AsString>
					.And<AMProdItemSplit.prodOrdID.IsEqual<@P.AsString>
					.And<AMProdItemSplit.lotSerialNbr.IsEqual<@P.AsString>>>>
					.View.Select(this, orderType, prodOrderId, lotSerialNbr);
		}
	}
}
