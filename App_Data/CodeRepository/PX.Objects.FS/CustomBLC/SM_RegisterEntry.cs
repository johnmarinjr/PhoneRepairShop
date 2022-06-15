using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.Extensions.MultiCurrency;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PM;
using System;
using System.Collections.Generic;
using System.Linq;
using static PX.Objects.FS.MessageHelper;

namespace PX.Objects.FS
{
    public class SM_RegisterEntry : FSPostingBase<RegisterEntry>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        #region Overrides
        public delegate void ReleaseDocumentDelegate(PMRegister doc);

        [PXOverride]
        public virtual void ReleaseDocument(PMRegister doc, ReleaseDocumentDelegate baseMethod)
        {
            if (SharedFunctions.isFSSetupSet(Base) == false)
            {
                baseMethod(doc);
                return;
            }

            using (PXTransactionScope ts = new PXTransactionScope())
            {
                UpdateCosts(doc);
                baseMethod(doc);
                ts.Complete();
            }
        }
        #endregion

        #region Event Handlers
        #region PMRegister
        protected virtual void _(Events.RowPersisting<PMRegister> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            ValidatePostBatchStatus(e.Operation, ID.Batch_PostTo.PM, e.Row.Module, e.Row.RefNbr);
        }

        protected virtual void _(Events.RowPersisted<PMRegister> e)
        {
            if (e.Operation == PXDBOperation.Delete && e.TranStatus == PXTranStatus.Open)
            {
                CleanPostingInfoLinkedToDoc(e.Row);
            }
        }
        #endregion

        #region EPActivityApprove
        protected virtual void _(Events.RowPersisted<EPActivityApprove> e)
        {
            if (e.Row == null || !TimeCardHelper.IsTheTimeCardIntegrationEnabled(Base))
            {
                return;
            }

            if (e.Row.Released == true
                    && (bool)e.Cache.GetValueOriginal<EPActivityApprove.released>(e.Row) == false
                        && e.TranStatus == PXTranStatus.Open)
            {
                UpdateAppointmentApprovedTime(e.Cache, e.Row);
            }
        }
        #endregion
        #endregion

        public virtual void UpdateCosts(PMRegister pmRegisterRow)
        {
            if (pmRegisterRow == null)
            {
                return;
            }

            var pmINTranRows = PXSelectJoin<FSPostDet,
                        InnerJoin<PMTran,
                            On<FSPostDet.pMPosted, Equal<True>,
                                And<PMTran.tranType, Equal<FSPostDet.pMDocType>,
                                And<PMTran.refNbr, Equal<FSPostDet.pMRefNbr>,
                                And<PMTran.tranID, Equal<FSPostDet.pMTranID>>>>>,
                        LeftJoin<FSSODet,
                                On<FSSODet.postID, Equal<FSPostDet.postID>,
                                    And<FSSODet.lineType, Equal<FSLineType.Inventory_Item>>>,
                        LeftJoin<FSAppointmentDet,
                                On<FSAppointmentDet.postID, Equal<FSPostDet.postID>,
                                    And<FSAppointmentDet.lineType, Equal<FSLineType.Inventory_Item>>>,
                        LeftJoin<FSSrvOrdType,
                            On<Where2<
                                    Where<FSSODet.sODetID, IsNotNull,
                                        And<FSSrvOrdType.srvOrdType, Equal<FSSODet.srvOrdType>>>,
                                    Or<Where<FSAppointmentDet.appDetID, IsNotNull,
                                        And<FSSrvOrdType.srvOrdType, Equal<FSAppointmentDet.srvOrdType>>>>>>>>>>,
                        Where<
                            PMTran.tranType, Equal<Required<PMTran.tranType>>,
                        And<
                            PMTran.refNbr, Equal<Required<PMTran.refNbr>>,
                            And<FSSrvOrdType.postTo, Equal<FSPostTo.Projects>,
                            And<FSSrvOrdType.billingType, Equal<ListField_SrvOrdType_BillingType.CostAsCost>>>>>,
                        OrderBy<Asc<PMTran.tranType,
                                Asc<PMTran.refNbr>>>>
                    .Select(Base, pmRegisterRow.Module, pmRegisterRow.RefNbr);

            if (pmINTranRows.Count() > 0)
            {
                RegisterEntry pmEntry = Base;

                foreach (PXResult<FSPostDet, PMTran, FSSODet, FSAppointmentDet, FSSrvOrdType> record in pmINTranRows)
                {
                    PMTran tranRow = (PMTran)record;
                    FSPostDet postDetRow = (FSPostDet)record;

                    if (pmEntry.Document.Current == null
                            || pmEntry.Document.Current.Module != tranRow.TranType
                            || pmEntry.Document.Current.RefNbr != tranRow.RefNbr)
                    {
                        throw new PXInvalidOperationException();
                    }

                    PXResult<FSPostDet, INTran> inPostDetTranRowUpd = 
                        (PXResult<FSPostDet, INTran>)
                        PXSelectJoin<FSPostDet,
                        InnerJoin<INTran,
                            On<FSPostDet.iNPosted, Equal<True>,
                                And<INTran.docType, Equal<FSPostDet.iNDocType>,
                                And<INTran.refNbr, Equal<FSPostDet.iNRefNbr>,
                                And<INTran.lineNbr, Equal<FSPostDet.iNLineNbr>,
                                And<INTran.released, Equal<True>>>>>>>,
                        Where<FSPostDet.postID, Equal<Required<FSPostDet.postID>>>>
                        .Select(Base, postDetRow.PostID);

                    INTran inTranRowUpd = (INTran)inPostDetTranRowUpd;
                    PMTran pmTranRow = PMTran.PK.Find(Base, tranRow);

                    if (inTranRowUpd != null && pmTranRow != null)
                    {
                        IPXCurrencyHelper currencyHelper = Base.FindImplementation<IPXCurrencyHelper>();
						decimal curyUnitCost = currencyHelper.GetCurrencyInfo(pmTranRow.BaseCuryInfoID).CuryConvCuryRaw(inTranRowUpd.UnitCost.Value);
						pmTranRow.TranCuryUnitRate = Math.Round(curyUnitCost, CommonSetupDecPl.PrcCst, MidpointRounding.AwayFromZero);
                        pmEntry.Transactions.Update(pmTranRow);
                    }
                }

                if (pmEntry.Document.Current != null && pmEntry.IsDirty == true)
                {
                    pmEntry.Save.Press();
                }
            }
        }

        //TODO AC-171744
        public virtual void UpdateAppointmentApprovedTime(PXCache cache, PMTimeActivity timeActivity)
        {
            if (PXAccess.FeatureInstalled<FeaturesSet.timeReportingModule>() == false)
                return;

            var graphAppointmentEntry = PXGraph.CreateInstance<AppointmentEntry>();

            FSxPMTimeActivity fsxPMTimeActivityRow = cache.GetExtension<FSxPMTimeActivity>(timeActivity);

            if (fsxPMTimeActivityRow == null || fsxPMTimeActivityRow.LogLineNbr.HasValue == false)
            {
                return;
            }

            FSAppointmentLog appointmentlog = PXSelect<FSAppointmentLog,
                                                Where<
                                                    FSAppointmentLog.docID, Equal<Required<FSAppointmentLog.docID>>,
                                                    And<FSAppointmentLog.lineNbr, Equal<Required<FSAppointmentLog.lineNbr>>>>>
                                                .Select(Base, fsxPMTimeActivityRow.AppointmentID, fsxPMTimeActivityRow.LogLineNbr);

            if (appointmentlog != null)
            {
                graphAppointmentEntry.SkipTimeCardUpdate = true;

                graphAppointmentEntry.AppointmentRecords.Current = graphAppointmentEntry.AppointmentRecords.Search<FSAppointment.appointmentID>
                                                    (appointmentlog.DocID, appointmentlog.DocType);

                appointmentlog.ApprovedTime = true;
                graphAppointmentEntry.LogRecords.Update(appointmentlog);
                graphAppointmentEntry.Save.Press();
            }
        }

        #region Invoicing Methods
        public override List<ErrorInfo> GetErrorInfo()
        {
            return MessageHelper.GetErrorInfo<PMTran>(Base.Document.Cache, Base.Document.Current, Base.Transactions, typeof(PMTran));
        }

        public virtual string GetLineDisplayHint(PXGraph graph, string lineRefNbr, string lineDescr, int? inventoryID)
        {
            return MessageHelper.GetLineDisplayHint(graph, lineRefNbr, lineDescr, inventoryID);
        }

        public override void CreateInvoice(PXGraph graphProcess, List<DocLineExt> docLines, short invtMult, DateTime? invoiceDate, string invoiceFinPeriodID, OnDocumentHeaderInsertedDelegate onDocumentHeaderInserted, OnTransactionInsertedDelegate onTransactionInserted, PXQuickProcess.ActionFlow quickProcessFlow)
        {
            if (docLines.Count == 0)
            {
                return;
            }

            FSServiceOrder fsServiceOrderRow = docLines[0].fsServiceOrder;
            FSSrvOrdType fsSrvOrdTypeRow = docLines[0].fsSrvOrdType;
            FSPostDoc fsPostDocRow = docLines[0].fsPostDoc;
            FSAppointment fsAppointmentRow = docLines[0].fsAppointment;

            PMRegister pmRegisterRow = new PMRegister();

            pmRegisterRow.Module = BatchModule.PM;
            pmRegisterRow.Date = invoiceDate;
            pmRegisterRow.Description = fsAppointmentRow != null ? fsAppointmentRow.DocDesc : fsServiceOrderRow.DocDesc;
            pmRegisterRow.Status = PMRegister.status.Balanced;
            pmRegisterRow.OrigDocType = fsAppointmentRow != null ? PMOrigDocType.Appointment : PMOrigDocType.ServiceOrder;
            pmRegisterRow.OrigNoteID = fsAppointmentRow != null ? fsAppointmentRow.NoteID : fsServiceOrderRow.NoteID;

            pmRegisterRow = PXCache<PMRegister>.CreateCopy(Base.Document.Insert(pmRegisterRow));

            IDocLine docLine = null;
            PMTask pmTaskRow = null;
            PMTran pmTranRow = null;

            foreach (DocLineExt docLineExt in docLines)
            {
                docLine = docLineExt.docLine;
                fsPostDocRow = docLineExt.fsPostDoc;
                fsServiceOrderRow = docLineExt.fsServiceOrder;
                fsSrvOrdTypeRow = docLineExt.fsSrvOrdType;
                fsAppointmentRow = docLineExt.fsAppointment;

                pmTranRow = new PMTran();

                pmTranRow.Date = invoiceDate;
                pmTranRow.BranchID = docLine.BranchID;

                pmTranRow = PXCache<PMTran>.CreateCopy(Base.Transactions.Insert(pmTranRow));

                pmTaskRow = docLineExt.pmTask;

                if (pmTaskRow != null && pmTaskRow.Status == ProjectTaskStatus.Completed)
                {
                    throw new PXException(TX.Error.POSTING_PMTASK_ALREADY_COMPLETED, fsServiceOrderRow.RefNbr, GetLineDisplayHint(Base, docLine.LineRef, docLine.TranDesc, docLine.InventoryID), pmTaskRow.TaskCD);
                }

                if (docLine.ProjectID != null)
                {
                    pmTranRow.ProjectID = docLine.ProjectID;
                }

                if (docLine.ProjectID != null && docLine.ProjectTaskID != null)
                {
                    pmTranRow.TaskID = docLine.ProjectTaskID;
                }

                pmTranRow.TranCuryID = fsAppointmentRow != null ? fsAppointmentRow.CuryID : fsServiceOrderRow.CuryID;
                pmTranRow.UOM = docLine.UOM;
                pmTranRow.BAccountID = fsServiceOrderRow.BillCustomerID;
                pmTranRow.Billable = true;
                pmTranRow.InventoryID = docLine.InventoryID;
                pmTranRow.CostCodeID = docLine.CostCodeID;
                pmTranRow.LocationID = fsServiceOrderRow.BillLocationID;
                pmTranRow.Qty = docLine.GetQty(FieldType.BillableField);
                pmTranRow.FinPeriodID = invoiceFinPeriodID;
                pmTranRow.TranCuryUnitRate = (docLine.IsFree == false ? docLine.CuryUnitPrice * invtMult : 0m);
                pmTranRow.TranCuryAmount = docLine.GetTranAmt(FieldType.BillableField) * invtMult;
                pmTranRow.AccountGroupID = fsSrvOrdTypeRow.AccountGroupID;
                pmTranRow.Description = docLine.TranDesc;

                fsPostDocRow.DocLineRef = pmTranRow = Base.Transactions.Update(pmTranRow);
            }
        }

        public override FSCreatedDoc PressSave(int batchID, List<DocLineExt> docLines, BeforeSaveDelegate beforeSave)
        {
            if (Base.Document.Current == null)
            {
                throw new SharedClasses.TransactionScopeException();
            }

            if (beforeSave != null)
            {
                beforeSave(Base);
            }
            
            Base.Save.Press();

            var fsCreatedDocRow = new FSCreatedDoc()
            {
                BatchID = batchID,
                PostTo = ID.Batch_PostTo.PM,
                CreatedDocType = Base.Document.Current.Module,
                CreatedRefNbr = Base.Document.Current.RefNbr
            };

            return fsCreatedDocRow;
        }

        public override void Clear()
        {
            Base.Clear(PXClearOption.ClearAll);
        }

        public override PXGraph GetGraph()
        {
            return Base;
        }

        public override void DeleteDocument(FSCreatedDoc fsCreatedDocRow)
        {
            Base.Document.Current = Base.Document.Search<PMRegister.refNbr>(fsCreatedDocRow.CreatedRefNbr, fsCreatedDocRow.CreatedDocType);

            if (Base.Document.Current != null)
            {
                if (Base.Document.Current.RefNbr == fsCreatedDocRow.CreatedRefNbr
                        && Base.Document.Current.Module == fsCreatedDocRow.CreatedDocType)
                {
                    Base.Delete.Press();
                }
            }
        }

        public override void CleanPostInfo(PXGraph cleanerGraph, FSPostDet fsPostDetRow)
        {
            PXUpdate<
                Set<FSPostInfo.pMDocType, Null,
                Set<FSPostInfo.pMRefNbr, Null,
                Set<FSPostInfo.pMTranID, Null,
                Set<FSPostInfo.pMPosted, False>>>>,
            FSPostInfo,
            Where<
                FSPostInfo.postID, Equal<Required<FSPostInfo.postID>>,
                And<FSPostInfo.pMPosted, Equal<True>>>>
            .Update(cleanerGraph, fsPostDetRow.PostID);
        }

        public virtual PMRegister RevertInvoice()
        {
            if (Base.Document.Current == null) 
            {
                return null;
            }

            RegisterEntry targetGraph = null;
            PMRegister targetRow = null;
            PMRegister currentRow = Base.Document.Current;

            using (PXTransactionScope ts = new PXTransactionScope())
            {
                targetGraph = PXGraph.CreateInstance<RegisterEntry>();

                targetRow = new PMRegister();

                targetRow.Module = currentRow.Module;
                targetRow.Date = currentRow.Date;
                targetRow.Description = currentRow.Description;
                targetRow.OrigDocType = currentRow.OrigDocType;
                targetRow.OrigNoteID = currentRow.OrigNoteID;

                PMRegister doc = (PMRegister)targetGraph.Document.Cache.CreateCopy(targetGraph.Document.Insert(targetRow));

                doc.Description = PXMessages.LocalizeFormatNoPrefix(TX.Messages.ReverseDescrPrefix, currentRow.Description);

                targetGraph.Document.Update(doc);

                foreach (PMTran tran in Base.Transactions.Select())
                {
                    var reversal = this.ReverseTran(tran);
                    targetGraph.Transactions.Insert(reversal);
                }

                targetGraph.Save.Press();

                targetRow = targetGraph.Document.Current;

                if (targetRow.Hold == true)
                {
                    targetGraph.Document.Cache.SetValueExtIfDifferent<PMRegister.hold>(targetRow, false);

                    targetRow = targetGraph.Document.Update(targetRow);
                }

                targetGraph.ReleaseDocument(targetGraph.Document.Current);

                if (targetGraph.Document.Current.Status != PMRegister.status.Released) 
                {
                    throw new PXInvalidOperationException();
                }

                targetRow = targetGraph.Document.Current;

                ts.Complete();
            }

            return targetRow;
        }

        public virtual PMTran ReverseTran (PMTran tran)
        {
            var reversal = PXCache<PMTran>.CreateCopy(tran);
            reversal.OrigTranID = tran.TranID;

            reversal.TranID = null;
            reversal.TranType = null;
            reversal.TranDate = null;
            reversal.TranPeriodID = null;
            reversal.RefNbr = null;
            reversal.ARRefNbr = null;
            reversal.ARTranType = null;
            reversal.RefLineNbr = null;
            reversal.ProformaRefNbr = null;
            reversal.ProformaLineNbr = null;
            reversal.BatchNbr = null;
            reversal.RemainderOfTranID = null;
            reversal.OrigProjectID = null;
            reversal.OrigTaskID = null;
            reversal.OrigAccountGroupID = null;
            reversal.NoteID = null;
            reversal.AllocationID = null;

            reversal.TranCuryAmount *= -1;
            reversal.ProjectCuryAmount *= -1;
            reversal.TranCuryAmountCopy = null;
            reversal.Amount *= -1;
            reversal.Qty *= -1;
            reversal.BillableQty *= -1;

            reversal.Billable = false;
            reversal.Released = false;
            reversal.Billed = false;
            reversal.Allocated = false;

            reversal.IsNonGL = false;
            reversal.ExcludedFromBilling = true;
            reversal.ExcludedFromAllocation = true;
            reversal.ExcludedFromBillingReason = PXMessages.LocalizeFormatNoPrefix(PM.Messages.ExcludedFromBillingAsReversal, tran.TranID);
            reversal.Reverse = PMReverse.Never;

            return reversal;
        }

        #endregion

        #region Public Methods
        public virtual void ValidatePostBatchStatus(PXDBOperation dbOperation, string postTo, string createdDocType, string createdRefNbr)
        {
            DocGenerationHelper.ValidatePostBatchStatus<PMRegister>(Base, dbOperation, postTo, createdDocType, createdRefNbr);
        }
        #endregion
    }
}
