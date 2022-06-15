using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static PX.Objects.FS.FSPOReceiptProcess;
using static PX.Objects.PO.POOrderEntry;

namespace PX.Objects.FS
{
    public class SM_POOrderEntry : PXGraphExtension<POOrderEntry>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public override void Initialize()
        {
            base.Initialize();
            Base.onCopyPOLineFields = CopyPOLineFields;
        }

        [PXHidden]
        public PXSelect<FSServiceOrder> serviceOrderView;

        [PXHidden]
        public PXSetup<POSetup> POSetupRecord;

        [PXCopyPasteHiddenView]
        public PXSelectJoin<FSSODetSplit,
               InnerJoin<INItemPlan,
               On<
                   INItemPlan.planID, Equal<FSSODetSplit.planID>,
                   And<INItemPlan.planID, Equal<Required<POLine.planID>>>>>>
               FSSODetSplitFixedDemand;

        [PXCopyPasteHiddenView]
        public PXSelectJoin<FSSODet,
               InnerJoin<FSSODetSplit,
               On<
                   FSSODetSplit.srvOrdType, Equal<FSSODet.srvOrdType>,
                   And<FSSODetSplit.refNbr, Equal<FSSODet.refNbr>,
                   And<FSSODetSplit.lineNbr, Equal<FSSODet.lineNbr>>>>,
                InnerJoin<INItemPlan,
                On<
                    INItemPlan.planID, Equal<FSSODetSplit.planID>,
                    And<INItemPlan.planID, Equal<Required<POLine.planID>>>>>>>
                FSSODetFixedDemand;

        [PXHidden]
        public PXSelect<FSAppointment> AppointmentView;

        [PXHidden]
        public PXSelect<FSAppointmentDet> AppointmentLineView;

        [PXHidden]
        public PXSelect<FSApptLineSplit> apptSplitView;

        #region Event Handlers

        #region POOrder
        protected virtual void _(Events.RowPersisted<POOrder> e)
        {
            POOrder poOrderRow = (POOrder)e.Row;
            PXCache cache = e.Cache;

            if (poOrderRow.OrderType != POOrderType.RegularOrder)
            {
                return;
            }

            if (e.TranStatus == PXTranStatus.Open && e.Operation == PXDBOperation.Update)
            {
                string poOrderOldStatus = (string)cache.GetValueOriginal<POOrder.status>(poOrderRow);

                if (poOrderOldStatus != poOrderRow.Status)
                {
                    if (poOrderRow.Status != POOrderStatus.Completed && 
                        poOrderRow.Status != POOrderStatus.Cancelled 
                        && poOrderRow.Status != POOrderStatus.Rejected)
                    {
                        FSPOReceiptProcess.UpdateSrvOrdLinePOStatus(cache.Graph, poOrderRow);
                    }
                }
            }
        }
        #endregion

        #region INItemPlan
        protected virtual void _(Events.RowPersisted<INItemPlan> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (e.TranStatus == PXTranStatus.Open)
            {
                INItemPlan inItemPlanRow = (INItemPlan)e.Row;

                if (e.Operation == PXDBOperation.Update)
                {
                    if (inItemPlanRow.SupplyPlanID != null && inItemPlanRow.PlanType == INPlanConstants.PlanF6)
                    {
                        POLine poLine = null;
                        if (inItemPlanRow.SupplyPlanID != null)
                        {
                            poLine = Base.Transactions.Select().Where(x => ((POLine)x).PlanID == inItemPlanRow.SupplyPlanID).First();
                        }

                        FSSODet fsSODetRow = FSSODetFixedDemand.Select(inItemPlanRow.PlanID);
                        FSPOReceiptProcess.UpdatePOReferenceInSrvOrdLine(Base, FSSODetFixedDemand,
                            FSSODetSplitFixedDemand, AppointmentView, AppointmentLineView, fsSODetRow, Base.Document.Current,
                            poLine.LineNbr, poLine.Completed, e.Cache, inItemPlanRow, false);
                    }
                }
                else if (e.Operation == PXDBOperation.Delete
                    && (inItemPlanRow.PlanType == INPlanConstants.PlanF7 || inItemPlanRow.PlanType == INPlanConstants.PlanF8)
                )
                {
                    inItemPlanRow = PXSelect<INItemPlan, Where<INItemPlan.supplyPlanID, Equal<Required<INItemPlan.supplyPlanID>>>>.Select(Base, inItemPlanRow.PlanID);

                    if (inItemPlanRow != null && inItemPlanRow.SupplyPlanID != null)
                    {
                        FSSODet fsSODetRow = FSSODetFixedDemand.Select(inItemPlanRow.PlanID);
                        FSPOReceiptProcess.UpdatePOReferenceInSrvOrdLine(Base, FSSODetFixedDemand,
                            FSSODetSplitFixedDemand, AppointmentView, AppointmentLineView, fsSODetRow, Base.Document.Current, null, false,
                            e.Cache, inItemPlanRow, true);
                    }
                }
            }
        }
        #endregion

        #endregion

        public delegate void FillPOLineFromDemandDelegate(POLine dest, POFixedDemand demand, string OrderType, SOLineSplit3 solinesplit);

        [PXOverride]
        public virtual void FillPOLineFromDemand(POLine dest, POFixedDemand demand, string OrderType, SOLineSplit3 solinesplit, FillPOLineFromDemandDelegate del)
        {
            if (demand.PlanType == INPlanConstants.PlanF6)
            {
                PXResult<FSSODetSplit, FSSODet> fsSODetSplitDetRow =
                            (PXResult<FSSODetSplit, FSSODet>)
                            PXSelectJoin<FSSODetSplit,
                            InnerJoin<FSSODet,
                            On<
                                FSSODet.lineNbr, Equal<FSSODetSplit.lineNbr>,
                                And<FSSODet.srvOrdType, Equal<FSSODetSplit.srvOrdType>,
                                And<FSSODet.refNbr, Equal<FSSODetSplit.refNbr>>>>>,
                            Where<
                                FSSODetSplit.planID, Equal<Required<FSSODetSplit.planID>>>>
                            .Select(Base, demand.PlanID);

                if (fsSODetSplitDetRow != null)
                {
                    FSSODetSplit fsSODetSplitRow = (FSSODetSplit)fsSODetSplitDetRow;
                    FSSODet fsSODetRow = (FSSODet)fsSODetSplitDetRow;

                    dest.LineType = (fsSODetSplitRow.LineType == SO.SOLineType.Inventory
                                            ? POLineType.GoodsForServiceOrder
                                            : POLineType.NonStockForServiceOrder);

                    if (fsSODetRow.ManualCost == true)
                    {
                        dest.CuryUnitCost = fsSODetRow.CuryUnitCost;
                    }

                    dest.ProjectID = fsSODetRow.ProjectID;
                    dest.TaskID = fsSODetRow.TaskID;
                    dest.CostCodeID = fsSODetRow.CostCodeID;
                }
            }

            del(dest, demand, OrderType, solinesplit);
        }

        #region Overrides
        public delegate void ClearPOLinePlanIDIfPlanIsDeletedOrig();

        [PXOverride]
        public virtual void ClearPOLinePlanIDIfPlanIsDeleted(ClearPOLinePlanIDIfPlanIsDeletedOrig del)
        {
            ProcessSrvOrder(Base, Base.Document?.Current);

            if (del != null)
            {
                del();
            }
        }
        #endregion

        public virtual void CopyPOLineFields(POFixedDemand demand, POLine line)
        {
            PXResult<FSSODetSplit, FSSODet> fsSODetSplitDetRow =
                            (PXResult<FSSODetSplit, FSSODet>)
                            PXSelectJoin<FSSODetSplit,
                            InnerJoin<FSSODet,
                            On<
                                FSSODet.lineNbr, Equal<FSSODetSplit.lineNbr>,
                                And<FSSODet.srvOrdType, Equal<FSSODetSplit.srvOrdType>,
                                And<FSSODet.refNbr, Equal<FSSODetSplit.refNbr>>>>>,
                            Where<
                                FSSODetSplit.planID, Equal<Required<FSSODetSplit.planID>>>>
                            .Select(Base, demand.PlanID);

            if (fsSODetSplitDetRow != null)
            {
                FSSODet fsSODetRow = (FSSODet)fsSODetSplitDetRow;

                if (POSetupRecord.Current != null)
                {
                    if (POSetupRecord.Current.CopyLineNotesFromServiceOrder == true
                            || POSetupRecord.Current.CopyLineAttachmentsFromServiceOrder == true)
                    {
                        var fsSODetCache = new PXCache<FSSODet>(Base);
                        fsSODetCache.Update(fsSODetRow);

                        PXNoteAttribute.CopyNoteAndFiles(fsSODetCache,
                                                         fsSODetRow,
                                                         Base.Transactions.Cache,
                                                         line,
                                                         POSetupRecord.Current.CopyLineNotesFromServiceOrder == true,
                                                         POSetupRecord.Current.CopyLineAttachmentsFromServiceOrder == true);
                    }
                }

                line.TranDesc = fsSODetRow.TranDesc;
            }
        }

        public delegate string GetPOFixDemandSorterDelegate(POFixedDemand line);

        [PXOverride]
        public virtual string GetPOFixDemandSorter(POFixedDemand line, GetPOFixDemandSorterDelegate del)
        {
            if (line.PlanType == INPlanConstants.PlanF6)
            {
                FSSODet row = PXSelectJoin<FSSODet,
                                InnerJoin<FSSODetSplit,
                                    On<FSSODet.lineNbr, Equal<FSSODetSplit.lineNbr>,
                                        And<FSSODet.srvOrdType, Equal<FSSODetSplit.srvOrdType>,
                                        And<FSSODet.refNbr, Equal<FSSODetSplit.refNbr>>>>>,
                                Where<FSSODetSplit.planID, Equal<Required<FSSODetSplit.planID>>>>.Select(Base, line.PlanID);

                return row == null ? String.Empty : string.Format("{0}.{1}.{2:D7}", row.SrvOrdType, row.RefNbr, row.SortOrder.GetValueOrDefault());
            }
            else
            {
                return del(line);
            }
        }

        public virtual void ProcessSrvOrder(PXGraph graph, POOrder poOrder)
        {
            if (poOrder == null) return;

            if (!graph.Views.Caches.Contains(typeof(FSAppointment)))
                graph.Views.Caches.Add(typeof(FSAppointment));

            if (!graph.Views.Caches.Contains(typeof(FSAppointmentDet)))
                graph.Views.Caches.Add(typeof(FSAppointmentDet));

            if (!graph.Views.Caches.Contains(typeof(FSApptLineSplit)))
                graph.Views.Caches.Add(typeof(FSApptLineSplit));

            if (!graph.Views.Caches.Contains(typeof(FSServiceOrder)))
                graph.Views.Caches.Add(typeof(FSServiceOrder));

            if (!graph.Views.Caches.Contains(typeof(FSSODet)))
                graph.Views.Caches.Add(typeof(FSSODet));

            var fsSODetSplit = new PXSelect<FSSODetSplit>(graph);
            if (!graph.Views.Caches.Contains(typeof(FSSODetSplit)))
                graph.Views.Caches.Add(typeof(FSSODetSplit));

            var initemplan = new PXSelect<INItemPlan>(graph);

            var srvOrdLinesWithModifiedSplits = new List<SrvOrdLineWithSplits>();

            //Search for completed/cancelled POLines with uncompleted linked schedules
            var splitLinesAssociated =
                PXSelectJoin<POLine,
                InnerJoin<POOrder, On<POLine.FK.Order>,
                InnerJoin<INItemPlan, On<INItemPlan.supplyPlanID, Equal<POLine.planID>>,
                InnerJoin<FSSODetSplit, On<FSSODetSplit.planID, Equal<INItemPlan.planID>, And<FSSODetSplit.pOType, Equal<POLine.orderType>, And<FSSODetSplit.pONbr, Equal<POLine.orderNbr>, And<FSSODetSplit.pOLineNbr, Equal<POLine.lineNbr>>>>>>>>,
            Where<POLine.orderType, Equal<Required<POLine.orderType>>, And<POLine.orderNbr, Equal<Required<POLine.orderNbr>>,
                And2<Where<POLine.cancelled, Equal<boolTrue>, Or<POLine.completed, Equal<boolTrue>>>,
                And2<Where<POOrder.orderType, NotEqual<POOrderType.dropShip>, Or<POOrder.isLegacyDropShip, Equal<True>>>,
                And<FSSODetSplit.receivedQty, Less<FSSODetSplit.qty>, And<FSSODetSplit.pOCancelled, NotEqual<boolTrue>, And<FSSODetSplit.completed, NotEqual<boolTrue>>>>>>>>>.Select(graph, poOrder.OrderType, poOrder.OrderNbr);

            if (splitLinesAssociated?.Count > 0)
            {
                foreach (PXResult<POLine, POOrder, INItemPlan, FSSODetSplit> res in splitLinesAssociated)
                {
                    POLine poline = res;
                    INItemPlan plan = PXCache<INItemPlan>.CreateCopy(res);
                    FSSODetSplit parentschedule = PXCache<FSSODetSplit>.CreateCopy(res);

                    serviceOrderView.Current = (FSServiceOrder)PXParentAttribute.SelectParent(fsSODetSplit.Cache, parentschedule, typeof(FSServiceOrder));
                    FSSODetFixedDemand.Current = (FSSODet)PXParentAttribute.SelectParent(fsSODetSplit.Cache, parentschedule, typeof(FSSODet));

                    if (parentschedule.Completed != true && parentschedule.POCancelled != true && parentschedule.BaseQty > parentschedule.BaseReceivedQty)
                    {
                        FSPOReceiptProcess.UpdateSchedulesFromCompletedPOStatic(graph, fsSODetSplit, initemplan, parentschedule, serviceOrderView, plan);

                        if (initemplan.Cache.GetStatus(plan) != PXEntryStatus.Inserted)
                        {
                            initemplan.Delete(plan);
                        }

                        fsSODetSplit.Cache.SetStatus(parentschedule, PXEntryStatus.Notchanged);
                        parentschedule = PXCache<FSSODetSplit>.CreateCopy(parentschedule);

                        parentschedule.PlanID = null;
                        parentschedule.Completed = true;
                        parentschedule.POCompleted = true;
                        parentschedule.POCancelled = true;
                        fsSODetSplit.Cache.Update(parentschedule);

                        srvOrdLinesWithModifiedSplits.Add(new SrvOrdLineWithSplits(FSSODetFixedDemand.Current, parentschedule, plan.PlanQty));

                        INItemPlan inItemPlanRow = PXSelect<INItemPlan,
                                                    Where<INItemPlan.supplyPlanID, Equal<Required<INItemPlan.supplyPlanID>>>>
                                                    .Select(graph, poline.PlanID);


                        FSPOReceiptProcess.UpdatePOReferenceInSrvOrdLine(graph, FSSODetFixedDemand, fsSODetSplit, AppointmentView, AppointmentLineView,
                            FSSODetFixedDemand.Current, poOrder, poline.LineNbr, poline.Completed, null, inItemPlanRow, false);


                        foreach (SrvOrdLineWithSplits lineExt in srvOrdLinesWithModifiedSplits)
                        {
                            FSPOReceiptProcess.UpdatePOReceiptInfoInAppointmentsStatic(graph, lineExt, fsSODetSplit, AppointmentView, AppointmentLineView, apptSplitView, false);
                        }
                    }
                }
            }
            else
            {
                FSPOReceiptProcess.UpdateSrvOrdLinePOStatus(graph, poOrder);
            }
        }
    }
}
