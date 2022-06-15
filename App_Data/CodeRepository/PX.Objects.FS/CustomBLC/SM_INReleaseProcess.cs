using PX.Data;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.FS
{
    public class SM_INReleaseProcess : PXGraphExtension<INReleaseProcess>
    {
        #region InternalClass
        public class SODet
        {
            public int? SODetID;
            public bool? IsSerialized;
            public List<SODetSplit> Splits;

            public SODet(int? id) 
            {
                SODetID = id;
                IsSerialized = false;
                Splits = new List<SODetSplit>();
            }

            public SODet(int? id, string serialNbr, decimal? curyUnitCost, bool? _isSerialized)
            {
                SODetID = id;
                IsSerialized = _isSerialized;
                Splits = new List<SODetSplit>();
                Splits.Add(new SODetSplit(serialNbr, curyUnitCost));
            }
        }

        public class SODetSplit
        {
            public string LotSerialNbr;
            public decimal? CuryUnitCost;

            public SODetSplit(string serialNbr, decimal? cost)
            {
                LotSerialNbr = serialNbr;
                CuryUnitCost = cost;
            }
        }

        public class SrvOrder 
        {
            public int? SOID;
            public string srvOrdType;
            public List<SODet> Details;

            public SrvOrder() 
            {
                Details = new List<SODet>();
            }

            public SrvOrder(int? sOIDs, string srvOrdType, int? sODetID)
            {
                this.SOID = sOIDs;
                this.srvOrdType = srvOrdType;
                Details = new List<SODet>();
                Details.Add(new SODet(sODetID));
            }

            public SrvOrder(int? sOIDs, string srvOrdType, int? sODetID, string serialNbr, decimal? curyUnitCost, bool? IsSerialized)
            {
                this.SOID = sOIDs;
                this.srvOrdType = srvOrdType;
                Details = new List<SODet>();
                Details.Add(new SODet(sODetID, serialNbr, curyUnitCost, IsSerialized));
            }
        }

        public class INTranProcess
        {
            public FSAppointmentDet appDet;
            public FSSODet soDet;
            public INTran inTran;
            public INTranSplit inTranSplit;
        }

        public virtual void AddNewSODet(List<SrvOrder> srvOrderList, int? sOID, string srvOrdType, int? sODetID, string serialNbr, decimal? curyUnitCost)
        {
            SrvOrder current = srvOrderList.Find(x => x.SOID == sOID);

            if (current != null)
            {
                SODet currentDet = current.Details.Find(x => x.SODetID == sODetID);

                if (currentDet == null || currentDet.SODetID == null)
                {
                    current.Details.Add(string.IsNullOrEmpty(serialNbr) == true ? new SODet(sODetID) : new SODet(sODetID, serialNbr, curyUnitCost, true));
                }
                else 
                {
                    if (string.IsNullOrEmpty(serialNbr) == false) 
                    {
                        SODetSplit soDetSplit = currentDet.Splits.Find(x => x.LotSerialNbr == serialNbr);

                        if (soDetSplit == null || string.IsNullOrEmpty(soDetSplit.LotSerialNbr) == true) 
                        {
                            currentDet.Splits.Add(new SODetSplit(serialNbr, curyUnitCost));
                        }
                    }
                }
            }
            else
            {
                current = string.IsNullOrEmpty(serialNbr) == true ? new SrvOrder(sOID, srvOrdType, sODetID) : new SrvOrder(sOID, srvOrdType, sODetID, serialNbr, curyUnitCost, true);

                srvOrderList.Add(current);
            }
        }
        #endregion

        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public override void Initialize()
        {
            base.Initialize();
            Base.onBeforeSalesOrderProcessPOReceipt = ProcessPOReceipt;
        }

        public bool updateCosts = false;

        #region Views
        public PXSelect<FSServiceOrder> serviceOrderView;
        public PXSelect<FSSODetSplit> soDetSplitView;
        public PXSelect<FSAppointmentDet> apptDetView;
        public PXSelect<FSApptLineSplit> apptSplitView;
        #endregion

        #region CacheAttached
        [PXDBString(IsKey = true, IsUnicode = true)]
        [PXParent(typeof(Select<FSServiceOrder, Where<FSServiceOrder.srvOrdType, Equal<Current<FSSODetSplit.srvOrdType>>, And<FSServiceOrder.refNbr, Equal<Current<FSSODetSplit.refNbr>>>>>))]
        [PXDefault()]
        protected virtual void FSSODetSplit_RefNbr_CacheAttached(PXCache sender)
        {
        }

        [PXDBDate()]
        [PXDefault()]
        protected virtual void FSSODetSplit_OrderDate_CacheAttached(PXCache sender)
        {
        }

        [PXDBLong()]
        [INItemPlanIDSimple()]
        protected virtual void FSSODetSplit_PlanID_CacheAttached(PXCache sender)
        {
        }

        [PXDBInt()]
        protected virtual void FSSODetSplit_SiteID_CacheAttached(PXCache sender)
        {
        }

        [PXDBInt()]
        protected virtual void FSSODetSplit_LocationID_CacheAttached(PXCache sender)
        {
        }

        // The selector is removed to avoid validation.
        #region Remove LotSerialNbr Selector
        [PXDBString]
        protected void _(Events.CacheAttached<FSSODetSplit.lotSerialNbr> e) { }

        [PXDBString]
        protected void _(Events.CacheAttached<FSAppointmentDet.lotSerialNbr> e) { }

        [PXDBString]
        protected void _(Events.CacheAttached<FSApptLineSplit.lotSerialNbr> e) { }
        #endregion

        // Attribute PXDBDefault is removed to prevent values ​​explicitly assigned by code from being changed in the Persist.
        #region Remove PXDBDefault
        [PXDBString(20, IsKey = true, IsUnicode = true, InputMask = "")]
        protected void _(Events.CacheAttached<FSApptLineSplit.apptNbr> e) { }

        [PXDBString(15, IsUnicode = true, InputMask = "")]
        protected void _(Events.CacheAttached<FSApptLineSplit.origSrvOrdNbr> e) { }

        [PXDBInt()]
        protected void _(Events.CacheAttached<FSApptLineSplit.origLineNbr> e) { }

        [PXDBDate()]
        protected void _(Events.CacheAttached<FSApptLineSplit.apptDate> e) { }
        #endregion

        #region Remove CheckUnique
        [PXDBInt(IsKey = true)]
        [PXLineNbr(typeof(FSAppointment.lineCntr))]
        [PXUIField(DisplayName = "Line Nbr.", Visible = false, Enabled = false)]
        [PXFormula(null, typeof(MaxCalc<FSAppointment.maxLineNbr>))]
        protected void _(Events.CacheAttached<FSAppointmentDet.lineNbr> e) { }
        #endregion
        #endregion

        public virtual List<PXResult<INItemPlan, INPlanType>> ProcessPOReceipt(PXGraph graph, IEnumerable<PXResult<INItemPlan, INPlanType>> list, string POReceiptType, string POReceiptNbr)
        {
            return FSPOReceiptProcess.ProcessPOReceipt(graph, list, POReceiptType, POReceiptNbr, stockItemProcessing: true);
        }

        #region Overrides
        public delegate void PersistDelegate();

        [PXOverride]
        public virtual void Persist(PersistDelegate baseMethod)
        {
            if (SharedFunctions.isFSSetupSet(Base) == false)
            {
                baseMethod();
                return;
            }

            using (PXTransactionScope ts = new PXTransactionScope())
            {
                baseMethod();
                UpdateCosts(Base.inregister.Current);
                ts.Complete();
            }
        }

        public delegate void ReleaseDocProcDelegate(JournalEntry je, INRegister doc);

        [PXOverride]
        public virtual void ReleaseDocProc(JournalEntry je, INRegister doc, ReleaseDocProcDelegate del)
        {
            ValidatePostBatchStatus(PXDBOperation.Update, ID.Batch_PostTo.IN, doc.DocType, doc.RefNbr);

            if (del != null)
            {
                del(je, doc);
            }
        }
        #endregion

        public virtual void UpdateCosts(INRegister inRegisterRow)
        {
            if (inRegisterRow == null) 
            {
                return;
            }

            FSPostRegister fsPostRegisterRow = GetPostRegister(inRegisterRow);

            if (fsPostRegisterRow == null
                || string.IsNullOrEmpty(fsPostRegisterRow.RefNbr) == true)
            {
                return;
            }

            List<INTranProcess> inTranProcessList = new List<INTranProcess>();

            if (fsPostRegisterRow.PostedTO == ID.Batch_PostTo.SO)
            { 
                var soINTranRows = PXSelectJoin<FSPostDet,
                            InnerJoin<INTran,
                                On<FSPostDet.sOPosted, Equal<True>,
                                    And<INTran.sOOrderType, Equal<FSPostDet.sOOrderType>,
                                    And<INTran.sOOrderNbr, Equal<FSPostDet.sOOrderNbr>,
                                    And<INTran.sOOrderLineNbr, Equal<FSPostDet.sOLineNbr>>>>>,
                            LeftJoin<INTranSplit,
                                On<INTranSplit.docType, Equal<INTran.docType>,
                                And<INTranSplit.refNbr, Equal<INTran.refNbr>,
                                And<INTranSplit.lineNbr, Equal<INTran.lineNbr>>>>,
                            LeftJoin<FSSODet,
                                    On<FSSODet.postID, Equal<FSPostDet.postID>>,
                            LeftJoin<FSAppointmentDet,
                                    On<FSAppointmentDet.postID, Equal<FSPostDet.postID>>>>>>,
                            Where<
                                INTran.docType, Equal<Required<INTran.docType>>,
                            And<
                                INTran.refNbr, Equal<Required<INTran.refNbr>>>>,
                            OrderBy<Asc<FSSODet.sOID,
                                    Asc<FSAppointmentDet.appointmentID,
                                    Asc<INTran.inventoryID>>>>>
                        .Select(Base, inRegisterRow.DocType, inRegisterRow.RefNbr);

                foreach (PXResult<FSPostDet, INTran, INTranSplit, FSSODet, FSAppointmentDet> bqlResult in soINTranRows)
                {
                    inTranProcessList.Add(new INTranProcess()
                    {
                        appDet = (FSAppointmentDet)bqlResult,
                        soDet = (FSSODet)bqlResult,
                        inTran = (INTran)bqlResult,
                        inTranSplit = (INTranSplit)bqlResult
                    });
                }
            }

            if (fsPostRegisterRow.PostedTO == ID.Batch_PostTo.SI)
            {
                if (fsPostRegisterRow.EntityType == ID.PostDoc_EntityType.SERVICE_ORDER)
                {
                    var soInvoiceINTranRows = PXSelectJoin<INTran,
                                        InnerJoin<FSARTran,
                                            On<INTran.aRDocType, Equal<FSARTran.tranType>,
                                            And<INTran.aRRefNbr, Equal<FSARTran.refNbr>,
                                            And<INTran.aRLineNbr, Equal<FSARTran.lineNbr>>>>,
                                        LeftJoin<FSSODet,
                                                On<FSSODet.srvOrdType, Equal<FSARTran.srvOrdType>,
                                                   And<FSSODet.refNbr, Equal<FSARTran.serviceOrderRefNbr>,
                                                   And<FSSODet.lineNbr, Equal<FSARTran.serviceOrderLineNbr>,
                                                   And<FSARTran.appointmentRefNbr, IsNull>>>>,
                                        LeftJoin<FSPostDet,
                                            On<FSPostDet.postID, Equal<FSAppointmentDet.postID>>>>>,
                                    Where<
                                        FSPostDet.sOInvPosted, Equal<True>,
                                        And<INTran.docType, Equal<Required<INTran.docType>>,
                                    And<
                                        INTran.refNbr, Equal<Required<INTran.refNbr>>>>>,
                                    OrderBy<Asc<FSSODet.sOID,
                                            Asc<FSAppointmentDet.appointmentID,
                                            Asc<INTran.inventoryID>>>>>
                                .Select(Base, inRegisterRow.DocType, inRegisterRow.RefNbr);

                    foreach (PXResult<INTran, FSARTran, FSSODet, FSPostDet> bqlResult in soInvoiceINTranRows)
                    {
                        inTranProcessList.Add(new INTranProcess()
                        {
                            appDet = null,
                            soDet = (FSSODet)bqlResult,
                            inTran = (INTran)bqlResult
                        });
                    }
                }
                else if(fsPostRegisterRow.EntityType == ID.PostDoc_EntityType.APPOINTMENT)
                {
                    var soInvoiceINTranRows = PXSelectJoin<INTran,
                                        InnerJoin<FSARTran,
                                            On<INTran.aRDocType, Equal<FSARTran.tranType>,
                                            And<INTran.aRRefNbr, Equal<FSARTran.refNbr>,
                                            And<INTran.aRLineNbr, Equal<FSARTran.lineNbr>>>>,
                                        LeftJoin<FSAppointmentDet,
                                                On<FSAppointmentDet.srvOrdType, Equal<FSARTran.srvOrdType>,
                                                   And<FSAppointmentDet.refNbr, Equal<FSARTran.appointmentRefNbr>,
                                                   And<FSAppointmentDet.lineNbr, Equal<FSARTran.appointmentLineNbr>>>>,
                                        LeftJoin<FSPostDet,
                                            On<FSPostDet.postID, Equal<FSAppointmentDet.postID>>>>>,
                                    Where<
                                        FSPostDet.sOInvPosted, Equal<True>,
                                        And<INTran.docType, Equal<Required<INTran.docType>>,
                                    And<
                                        INTran.refNbr, Equal<Required<INTran.refNbr>>>>>,
                                    OrderBy<Asc<FSSODet.sOID,
                                            Asc<FSAppointmentDet.appointmentID,
                                            Asc<INTran.inventoryID>>>>>
                                .Select(Base, inRegisterRow.DocType, inRegisterRow.RefNbr);

                    foreach (PXResult<INTran, FSARTran, FSAppointmentDet, FSPostDet> bqlResult in soInvoiceINTranRows)
                    {
                        inTranProcessList.Add(new INTranProcess()
                        {
                            appDet = (FSAppointmentDet)bqlResult,
                            soDet = null,
                            inTran = (INTran)bqlResult
                        });
                    }
                }
            }

            if (fsPostRegisterRow.PostedTO == ID.Batch_PostTo.IN)
            {
                var pmINTranRows = PXSelectJoin<FSPostDet,
                        InnerJoin<INTran,
                            On<FSPostDet.iNPosted, Equal<True>,
                                And<INTran.docType, Equal<FSPostDet.iNDocType>,
                                And<INTran.refNbr, Equal<FSPostDet.iNRefNbr>,
                                And<INTran.lineNbr, Equal<FSPostDet.iNLineNbr>>>>>,
                        LeftJoin<INTranSplit,
                            On<INTranSplit.docType, Equal<INTran.docType>,
                            And<INTranSplit.refNbr, Equal<INTran.refNbr>,
                            And<INTranSplit.lineNbr, Equal<INTran.lineNbr>>>>,
                        LeftJoin<FSSODet,
                                On<FSSODet.postID, Equal<FSPostDet.postID>>,
                        LeftJoin<FSAppointmentDet,
                                On<FSAppointmentDet.postID, Equal<FSPostDet.postID>>>>>>,
                        Where<
                            INTran.docType, Equal<Required<INTran.docType>>,
                        And<
                            INTran.refNbr, Equal<Required<INTran.refNbr>>>>,
                        OrderBy<Asc<FSSODet.sOID,
                                Asc<FSAppointmentDet.appointmentID,
                                Asc<INTran.inventoryID>>>>>
                    .Select(Base, inRegisterRow.DocType, inRegisterRow.RefNbr);

                foreach (PXResult<FSPostDet, INTran, INTranSplit, FSSODet, FSAppointmentDet> bqlResult in pmINTranRows)
                {
                    inTranProcessList.Add(new INTranProcess()
                    {
                        appDet = (FSAppointmentDet)bqlResult,
                        soDet = (FSSODet)bqlResult,
                        inTran = (INTran)bqlResult,
                        inTranSplit = (INTranSplit)bqlResult
                    });
                }
            }


            if (inTranProcessList.Count > 0)
            { 
                AppointmentEntry appGraph = PXGraph.CreateInstance<AppointmentEntry>();
                ServiceOrderEntry soGraph = PXGraph.CreateInstance<ServiceOrderEntry>();

                soGraph.Clear(PXClearOption.ClearAll);
                appGraph.Clear(PXClearOption.ClearAll);

                PXResultset<FSApptLineSplit> appSplitList = null;
                PXResultset<FSSODetSplit> soSplitList = null;
                int? currenApptLineNbr = null;
                int? currentSOLineNbr = null;
                List<SrvOrder> srvOrderList = new List<SrvOrder>();
                FSAppointmentDet apptLine = null;
                FSSODet soLine = null;
                FSSrvOrdType fsSrvOrdTypeRow = null;

                foreach (INTranProcess inTranProcessRow in inTranProcessList) 
                {
                    FSAppointmentDet fsAppointmentDetRow = inTranProcessRow.appDet;
                    FSSODet fsSODetRow = inTranProcessRow.soDet;
                    INTran inTranRow = inTranProcessRow.inTran;
                    INTranSplit inTranSplitRow = inTranProcessRow.inTranSplit;

                    if (fsSODetRow != null && fsSODetRow.SODetID != null)
                    {
                        if (fsSrvOrdTypeRow == null || fsSrvOrdTypeRow.SrvOrdType != fsSODetRow.SrvOrdType)
                        {
                            fsSrvOrdTypeRow = FSSrvOrdType.PK.Find(soGraph, fsSODetRow.SrvOrdType);
                        }

                        if (soGraph.ServiceOrderRecords.Current == null
                            || soGraph.ServiceOrderRecords.Current.SrvOrdType != fsSODetRow.SrvOrdType
                            || soGraph.ServiceOrderRecords.Current.RefNbr != fsSODetRow.RefNbr)
                        {
                            if (soGraph.ServiceOrderRecords.Current != null 
                                && soGraph.IsDirty == true)
                            {
                                soGraph.Save.Press();
								soGraph.Clear(PXClearOption.ClearAll);
							}

                            soGraph.ServiceOrderRecords.Current = soGraph.ServiceOrderRecords.Search<FSServiceOrder.refNbr>(fsSODetRow.RefNbr, fsSODetRow.SrvOrdType);
                            soGraph.ServiceOrderRecords.Current.IsINReleaseProcess = true;
                            currentSOLineNbr = null;
                        }

                        if (currentSOLineNbr == null || currentSOLineNbr != fsSODetRow.LineNbr)
                        {
                            currentSOLineNbr = fsSODetRow.LineNbr;
                            soSplitList = PXSelect<FSSODetSplit,
                                            Where<FSSODetSplit.srvOrdType, Equal<Required<FSApptLineSplit.srvOrdType>>,
                                                 And<FSSODetSplit.refNbr, Equal<Required<FSSODetSplit.refNbr>>,
                                                 And<FSSODetSplit.lineNbr, Equal<Required<FSApptLineSplit.lineNbr>>,
                                                 And<FSSODetSplit.pOCreate, Equal<False>>>>>>
                            .Select(soGraph, fsSODetRow.SrvOrdType, fsSODetRow.RefNbr, fsSODetRow.LineNbr);
                        }

                        decimal? soSplitCostAmtTotal = 0m;
                        decimal? soSplitQtyTotal = 0m;

                        foreach (FSSODetSplit split in soSplitList)
                        {
                            if (split.LotSerialNbr == inTranRow.LotSerialNbr 
                                || (inTranSplitRow != null && split.LotSerialNbr == inTranSplitRow.LotSerialNbr))
                            {
                                split.UnitCost = inTranRow.UnitCost;

                                decimal splitCuryUnitCost;
                                PXDBCurrencyAttribute.CuryConvCury(soGraph.Splits.Cache, split, (decimal)split.UnitCost, out splitCuryUnitCost, CommonSetupDecPl.PrcCst);

                                split.CuryUnitCost = splitCuryUnitCost;

                                soGraph.Splits.Update(split);
                            }

                            soSplitQtyTotal += split.Qty;
                            soSplitCostAmtTotal += split.Qty * split.UnitCost;
                        }

                        if (soLine == null
                            || soLine.SrvOrdType != fsSODetRow.SrvOrdType
                            || soLine.RefNbr != fsSODetRow.RefNbr
                            || soLine.LineNbr != fsSODetRow.LineNbr)
                        {
                            soLine = FSSODet.PK.Find(soGraph, fsSODetRow.SrvOrdType, fsSODetRow.RefNbr, fsSODetRow.LineNbr);
                        }

                        soLine.UnitCost = soSplitQtyTotal > 0m ? soSplitCostAmtTotal / soSplitQtyTotal :  inTranRow.UnitCost;

                        decimal CuryUnitCost;
                        PXDBCurrencyAttribute.CuryConvCury(soGraph.ServiceOrderDetails.Cache, soLine, (decimal)soLine.UnitCost, out CuryUnitCost, CommonSetupDecPl.PrcCst);

                        soLine.CuryUnitCost = CuryUnitCost;

                        if (fsSrvOrdTypeRow.PostTo == ID.SrvOrdType_PostTo.PROJECTS
                                && fsSrvOrdTypeRow.BillingType == ID.SrvOrdType_BillingType.COST_AS_COST)
                        {
                            soLine.CuryUnitPrice = CuryUnitCost;
                        }

                        soLine = (FSSODet)soGraph.ServiceOrderDetails.Cache.CreateCopy(soGraph.ServiceOrderDetails.Update(soLine));

                        //Updating cost value in all associated appointment lines.
                        foreach (FSAppointmentDet row in PXSelect<FSAppointmentDet, 
                                                            Where<FSAppointmentDet.sODetID, Equal<Required<FSAppointmentDet.sODetID>>>,
                                                            OrderBy<Asc<FSAppointmentDet.appointmentID>>>
                                                           .Select(appGraph, soLine.SODetID))
                        {
                            if (appGraph.AppointmentRecords.Current == null
                                || appGraph.AppointmentRecords.Current.SrvOrdType != row.SrvOrdType
                                || appGraph.AppointmentRecords.Current.RefNbr != row.RefNbr)
                            {
                                if (appGraph.AppointmentRecords.Current != null
                                    && appGraph.IsDirty == true)
                                {
                                    appGraph.Save.Press();
                                }

                                appGraph.AppointmentRecords.Current = appGraph.AppointmentRecords.Search<FSAppointment.refNbr>(row.RefNbr, row.SrvOrdType);
                                appGraph.AppointmentRecords.Current.MustUpdateServiceOrder = false;
                            }

                            FSAppointmentDet apptDetRow = FSAppointmentDet.PK.Find(appGraph, row.SrvOrdType, row.RefNbr, row.LineNbr); ;

                            if (apptDetRow != null)
                            {
                                apptDetRow.CuryUnitCost = soLine.CuryUnitCost;
                                apptDetRow = (FSAppointmentDet)appGraph.AppointmentDetails.Update(apptDetRow);
                            }
                        }
                    }

                    if (fsAppointmentDetRow != null && fsAppointmentDetRow.AppDetID != null) 
                    { 
                        if(fsSrvOrdTypeRow == null || fsSrvOrdTypeRow.SrvOrdType != fsAppointmentDetRow.SrvOrdType)
                        {
                            fsSrvOrdTypeRow = FSSrvOrdType.PK.Find(appGraph, fsAppointmentDetRow.SrvOrdType);
                        }

                        if(appGraph.AppointmentRecords.Current == null 
                            || appGraph.AppointmentRecords.Current.SrvOrdType != fsAppointmentDetRow.SrvOrdType
                            || appGraph.AppointmentRecords.Current.RefNbr != fsAppointmentDetRow.RefNbr) 
                        {
                            if (appGraph.AppointmentRecords.Current != null 
                                && appGraph.IsDirty == true)
                            {
                                appGraph.Save.Press();
								appGraph.Clear(PXClearOption.ClearAll);
							}

                            appGraph.AppointmentRecords.Current = appGraph.AppointmentRecords.Search<FSAppointment.refNbr>(fsAppointmentDetRow.RefNbr, fsAppointmentDetRow.SrvOrdType);
                            appGraph.AppointmentRecords.Current.IsINReleaseProcess = true;
                            currenApptLineNbr = null;
                        }

                        if (currenApptLineNbr == null || currenApptLineNbr != fsAppointmentDetRow.LineNbr) {
                            currenApptLineNbr = fsAppointmentDetRow.LineNbr;
                            appSplitList = PXSelect<FSApptLineSplit, 
                                            Where<FSApptLineSplit.srvOrdType, Equal<Required<FSApptLineSplit.srvOrdType>>,
                                                 And<FSApptLineSplit.apptNbr, Equal <Required<FSApptLineSplit.apptNbr>>,
                                                 And<FSApptLineSplit.lineNbr, Equal <Required<FSApptLineSplit.lineNbr>>>>>>
                            .Select(appGraph, fsAppointmentDetRow.SrvOrdType, fsAppointmentDetRow.RefNbr, fsAppointmentDetRow.LineNbr);
                        }

                        decimal? apptSplitCostAmtTotal = 0m;
                        decimal? apptSplitQtyTotal = 0m;

                        foreach (FSApptLineSplit split in appSplitList) 
                        {
                            if (split.LotSerialNbr == inTranRow.LotSerialNbr 
                                    || (inTranSplitRow != null && split.LotSerialNbr == inTranSplitRow.LotSerialNbr))
                            {
                                split.UnitCost = inTranRow.UnitCost;

                                decimal splitCuryUnitCost;
                                PXDBCurrencyAttribute.CuryConvCury(appGraph.Splits.Cache, split, (decimal)split.UnitCost, out splitCuryUnitCost, CommonSetupDecPl.PrcCst);

                                split.CuryUnitCost = splitCuryUnitCost;

                                appGraph.Splits.Update(split);

                                AddNewSODet(srvOrderList, appGraph.AppointmentRecords.Current.SOID, appGraph.AppointmentRecords.Current.SrvOrdType, fsAppointmentDetRow.SODetID, split.LotSerialNbr, split.CuryUnitCost);
                            }

                            apptSplitQtyTotal += split.Qty;
                            apptSplitCostAmtTotal += split.Qty * split.UnitCost;
                        }

                        if (apptLine == null
                            || apptLine.SrvOrdType != fsAppointmentDetRow.SrvOrdType
                            || apptLine.RefNbr != fsAppointmentDetRow.RefNbr
                            || apptLine.LineNbr != fsAppointmentDetRow.LineNbr)
                        {
                            apptLine = FSAppointmentDet.PK.Find(appGraph, fsAppointmentDetRow.SrvOrdType, fsAppointmentDetRow.RefNbr, fsAppointmentDetRow.LineNbr);
                        }

                        if (apptLine.UnitCost != inTranRow.UnitCost) 
                        { 
                            apptLine.UnitCost = apptSplitQtyTotal > 0m ? apptSplitCostAmtTotal / apptSplitQtyTotal : inTranRow.UnitCost;

                            decimal curyUnitCost;
                            PXDBCurrencyAttribute.CuryConvCury(appGraph.AppointmentDetails.Cache, apptLine, (decimal)apptLine.UnitCost, out curyUnitCost, CommonSetupDecPl.PrcCst);
                            
                            apptLine.CuryUnitCost = curyUnitCost;

                            if (fsSrvOrdTypeRow.PostTo == ID.SrvOrdType_PostTo.PROJECTS
                                && fsSrvOrdTypeRow.BillingType == ID.SrvOrdType_BillingType.COST_AS_COST)
                            {
                                apptLine.CuryUnitPrice = curyUnitCost;
                            }

                            apptLine = (FSAppointmentDet)appGraph.AppointmentDetails.Cache.CreateCopy(appGraph.AppointmentDetails.Update(apptLine));

                            AddNewSODet(srvOrderList, appGraph.AppointmentRecords.Current.SOID, appGraph.AppointmentRecords.Current.SrvOrdType, fsAppointmentDetRow.SODetID, string.Empty, null);
                        }
                    }
                }

                if (appGraph.AppointmentRecords.Current != null 
                    && appGraph.IsDirty == true)
                {
                    appGraph.Save.Press();
					appGraph.Clear(PXClearOption.ClearAll);
				}

                if (soGraph.ServiceOrderRecords.Current != null 
                    && soGraph.IsDirty == true)
                {
                    soGraph.Save.Press();
					soGraph.Clear(PXClearOption.ClearAll);
				}

                if(srvOrderList.Count > 0)
                    UpdateServiceOrderAffectedCost(soGraph, srvOrderList);
            }
        }

        private void UpdateServiceOrderAffectedCost(ServiceOrderEntry soGraph, List<SrvOrder> srvOrderList)
        {
            foreach (SrvOrder srvOrder in srvOrderList)
            {
                soGraph.ServiceOrderRecords.Current = soGraph.ServiceOrderRecords.Search<FSServiceOrder.sOID>(srvOrder.SOID, srvOrder.srvOrdType);
                soGraph.ServiceOrderRecords.Current.IsINReleaseProcess = true;

                foreach (SODet detail in srvOrder.Details)
                {
                    FSSODet fsSODetRow = FSSODet.UK.Find(soGraph, detail.SODetID);

                    var appDetList = PXSelect<FSAppointmentDet,
                                            Where<FSAppointmentDet.sODetID, Equal<Required<FSAppointmentDet.sODetID>>>>
                                            .Select(soGraph, fsSODetRow.SODetID);

                    decimal? totalSum = 0m;

                    foreach (FSSODetSplit fsSODetSplitRow in PXSelect<FSSODetSplit,
                                                    Where<FSSODetSplit.srvOrdType, Equal<Required<FSApptLineSplit.srvOrdType>>,
                                                        And<FSSODetSplit.refNbr, Equal<Required<FSSODetSplit.refNbr>>,
                                                        And<FSSODetSplit.lineNbr, Equal<Required<FSApptLineSplit.lineNbr>>,
                                                        And<FSSODetSplit.pOCreate, Equal<False>>>>>>
                                                        .Select(soGraph, fsSODetRow.SrvOrdType, fsSODetRow.RefNbr, fsSODetRow.LineNbr)) 
                    {
                        if (string.IsNullOrEmpty(fsSODetSplitRow.LotSerialNbr) == false)
                        {
                            if (detail.Splits != null && detail.Splits.Exists(x => x.LotSerialNbr == fsSODetSplitRow.LotSerialNbr))
                            {
                                fsSODetSplitRow.CuryUnitCost = detail.Splits.Find(x => x.LotSerialNbr == fsSODetSplitRow.LotSerialNbr).CuryUnitCost;
                                soGraph.Splits.Update(fsSODetSplitRow);
                            }

                            totalSum = totalSum + fsSODetSplitRow.CuryUnitCost * fsSODetSplitRow.Qty;
                        }
                        else
                        {
                            decimal? sum = 0m;
                            decimal? pendingQty = fsSODetSplitRow.Qty;

                            if (detail.IsSerialized == false)
                            {
                                foreach (FSAppointmentDet fsAppDetRow in appDetList)
                                {
                                    if (fsAppDetRow.INOpenQty == null)
                                    {
                                        fsAppDetRow.INOpenQty = fsAppDetRow.BillableQty;
                                    }

                                    if (fsAppDetRow.INOpenQty < pendingQty)
                                    {
                                        pendingQty = pendingQty - fsAppDetRow.INOpenQty;
                                        sum = sum + fsAppDetRow.INOpenQty * fsAppDetRow.CuryUnitCost;
                                        fsAppDetRow.INOpenQty = 0;
                                    }
                                    else
                                    {
                                        fsAppDetRow.INOpenQty = fsAppDetRow.INOpenQty - pendingQty;
                                        sum = sum + pendingQty * fsAppDetRow.CuryUnitCost;
                                        pendingQty = 0;
                                        break;
                                    }
                                }
                            }

                            sum = sum + pendingQty * fsSODetRow.CuryOrigUnitCost;
                            fsSODetSplitRow.CuryUnitCost = fsSODetSplitRow.Qty == 0 ? fsSODetSplitRow.CuryUnitCost : sum / fsSODetSplitRow.Qty;
                            totalSum = totalSum + sum;
                            soGraph.Splits.Update(fsSODetSplitRow);
                        }
                    }

                    fsSODetRow.CuryUnitCost = fsSODetRow.Qty == 0 ? fsSODetRow.CuryUnitCost : totalSum / fsSODetRow.Qty;

                    if (soGraph.ServiceOrderTypeSelected.Current != null
                        && soGraph.ServiceOrderTypeSelected.Current.PostTo == ID.SrvOrdType_PostTo.PROJECTS
                            && soGraph.ServiceOrderTypeSelected.Current.BillingType == ID.SrvOrdType_BillingType.COST_AS_COST)
                    {
                        fsSODetRow.CuryUnitPrice = fsSODetRow.CuryUnitCost;
                    }

                    soGraph.ServiceOrderDetails.Update(fsSODetRow);
                }

                if (soGraph.ServiceOrderRecords.Current != null 
                    && soGraph.IsDirty == true)
                {
                    soGraph.Save.Press();
                }
            }
        }

        public virtual FSPostRegister GetPostRegister(INRegister inRegisterRow)
        {
            if (Base.inregister.Current == null)
            {
                return null;
            }

            var row = PXSelect<FSPostRegister,
                        Where2<
                            Where<
                                FSPostRegister.postedTO, Equal<ListField_PostTo.SO>,
                                And<FSPostRegister.postDocType, Equal<Required<FSPostRegister.postDocType>>,
                                And<FSPostRegister.postRefNbr, Equal<Required<FSPostRegister.postRefNbr>>>>>,
                        Or2<
                            Where<
                                FSPostRegister.postedTO, Equal<ListField_PostTo.SI>,
                                And<FSPostRegister.postDocType, Equal<Required<FSPostRegister.postDocType>>,
                                And<FSPostRegister.postRefNbr, Equal<Required<FSPostRegister.postRefNbr>>>>>,
                            Or<
                                Where<
                                    FSPostRegister.postedTO, Equal<ListField_PostTo.IN>,
                                    And<FSPostRegister.postDocType, Equal<Required<FSPostRegister.postDocType>>,
                                    And<FSPostRegister.postRefNbr, Equal<Required<FSPostRegister.postRefNbr>>>>>>>>>
                       .SelectWindowed(Base, 0, 1, 
                                        inRegisterRow.SOOrderType, inRegisterRow.SOOrderNbr, 
                                        inRegisterRow.SrcDocType, inRegisterRow.SrcRefNbr, 
                                        inRegisterRow.DocType, inRegisterRow.RefNbr)
                       .FirstOrDefault();

            return row;
        }

        #region Validations
        public virtual void ValidatePostBatchStatus(PXDBOperation dbOperation, string postTo, string createdDocType, string createdRefNbr)
        {
            DocGenerationHelper.ValidatePostBatchStatus<INRegister>(Base, dbOperation, postTo, createdDocType, createdRefNbr);
        }
        #endregion
    }
}
