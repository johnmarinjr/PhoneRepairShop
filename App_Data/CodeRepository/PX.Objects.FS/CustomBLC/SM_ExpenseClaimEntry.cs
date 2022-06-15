using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.EP;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.PM;
using System;

namespace PX.Objects.FS
{
    public class SM_ExpenseClaimEntry : PXGraphExtension<ExpenseClaimEntry>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        #region Private Members
        private ServiceOrderEntry _ServiceOrderGraph;
        protected ServiceOrderEntry GetServiceOrderGraph(bool clearGraph)
        {
            if (_ServiceOrderGraph == null)
            {
                _ServiceOrderGraph = PXGraph.CreateInstance<ServiceOrderEntry>();
            }
            else if (clearGraph == true)
            {
                _ServiceOrderGraph.Clear();
            }

            return _ServiceOrderGraph;
        }

        private AppointmentEntry _AppointmentGraph;
        protected AppointmentEntry GetAppointmentGraph(bool clearGraph)
        {
            if (_AppointmentGraph == null)
            {
                _AppointmentGraph = PXGraph.CreateInstance<AppointmentEntry>();
            }
            else if (clearGraph == true)
            {
                _AppointmentGraph.Clear();
            }

            return _AppointmentGraph;
        }
        #endregion

        #region Views
        public PXSelect<FSServiceOrder> ServiceOrderRecords;
        public PXSelect<FSAppointment> AppointmentRecords;
        #endregion

        #region CacheAttached
        #region FSServiceOrder_SrvOrdType
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXSelector(typeof(Search<FSSrvOrdType.srvOrdType>), SubstituteKey = typeof(FSSrvOrdType.srvOrdType), DescriptionField = typeof(FSSrvOrdType.srvOrdType))]
        protected virtual void FSServiceOrder_SrvOrdType_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #region FSAppointment_SrvOrdType
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXUIField(DisplayName = "Service Order Type", Visibility = PXUIVisibility.SelectorVisible)]
        [PXSelector(typeof(Search<FSSrvOrdType.srvOrdType>), SubstituteKey = typeof(FSSrvOrdType.srvOrdType), DescriptionField = typeof(FSSrvOrdType.srvOrdType))]
        protected virtual void FSAppointment_SrvOrdType_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #region FSServiceOrder_CustomerID
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXSelector(typeof(Search<Customer.bAccountID>), SubstituteKey = typeof(Customer.acctCD), DescriptionField = typeof(Customer.acctName))]
        protected virtual void FSServiceOrder_CustomerID_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #region FSAppointment_CustomerID
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXSelector(typeof(Search<Customer.bAccountID>), SubstituteKey = typeof(Customer.acctCD), DescriptionField = typeof(Customer.acctName))]
        protected virtual void FSAppointment_CustomerID_CacheAttached(PXCache sender)
        {
        }
		#endregion
		#region FSEntityType
		public String _FSEntityType;
		public abstract class fsEntityType : PX.Data.BQL.BqlString.Field<fsEntityType> { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXString()]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void EPExpenseClaimDetails_FSEntityType_CacheAttached(PXCache sender)
        {
        }

		#endregion
		#endregion

		#region FieldSelecting
		#endregion
		#region FieldDefaulting
		protected virtual void _(Events.FieldDefaulting<FSxEPExpenseClaimDetails.fsEntityType> e)
        {
        }
        #endregion
        #region FieldUpdating
        #endregion
        #region FieldVerifying
        #endregion
        #region FieldUpdated
        protected virtual void _(Events.FieldUpdated<EPExpenseClaimDetails.billable> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSxEPExpenseClaimDetails extRow = e.Cache.GetExtension<FSxEPExpenseClaimDetails>((EPExpenseClaimDetails)e.Row);

            if (extRow.FSBillable == true && (bool?)e.NewValue == true)
            {
                e.Cache.SetValueExt<FSxEPExpenseClaimDetails.fsBillable>(e.Row, false);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSxEPExpenseClaimDetails.fsBillable> e)
        {
            if (e.Row == null)
            {
                return;
            }

            EPExpenseClaimDetails row = (EPExpenseClaimDetails)e.Row;

            if (row.Billable == true && (bool?)e.NewValue == true)
            {
                e.Cache.SetValueExt<EPExpenseClaimDetails.billable>(e.Row, false);
            }
        }
        #endregion

        protected virtual void _(Events.RowSelecting<EPExpenseClaimDetails> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSxEPExpenseClaimDetails extRow = e.Cache.GetExtension<FSxEPExpenseClaimDetails>((EPExpenseClaimDetails)e.Row);

            if (extRow.FSEntityNoteID != null)
            {
                using (new PXConnectionScope())
                {
                    var item = new EntityHelper(Base).GetEntityRow(extRow.FSEntityNoteID, true);

                    if (item != null) 
                    {
                        if (extRow.FSEntityType == null)
                        {
                            extRow.FSEntityType = item.GetType().GetLongName();
                        }

                        extRow.IsDocBilledOrClosed = IsFSDocumentBilledOrClosed(item, extRow.FSEntityType);
                        extRow.IsDocRelatedToProject = IsFSDocumentRelatedToProjects(item, extRow.FSEntityType);
                    }
                }
            }
        }

        protected virtual void _(Events.RowSelected<EPExpenseClaimDetails> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSxEPExpenseClaimDetails extRow = e.Cache.GetExtension<FSxEPExpenseClaimDetails>((EPExpenseClaimDetails)e.Row);

            PXDefaultAttribute.SetPersistingCheck<FSxEPExpenseClaimDetails.fsEntityNoteID>
                                (e.Cache, e.Row, PXPersistingCheck.Nothing);

            PXUIFieldAttribute.SetEnabled<FSxEPExpenseClaimDetails.fsEntityNoteID>(e.Cache, e.Row, false);
            PXUIFieldAttribute.SetEnabled<FSxEPExpenseClaimDetails.fsBillable>(e.Cache, e.Row, extRow.FSEntityNoteID != null && extRow.IsDocBilledOrClosed == false && extRow.IsDocRelatedToProject == false);

            if (extRow.FSBillable == true && extRow.IsDocBilledOrClosed == true)
            {
                PXUIFieldAttribute.SetEnabled<EPExpenseClaimDetails.inventoryID>(e.Cache, e.Row, false);
                PXUIFieldAttribute.SetEnabled<EPExpenseClaimDetails.customerID>(e.Cache, e.Row, false);
                PXUIFieldAttribute.SetEnabled<EPExpenseClaimDetails.customerLocationID>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<EPExpenseClaimDetails.billable>(e.Cache, e.Row, false);
                PXUIFieldAttribute.SetEnabled<EPExpenseClaimDetails.qty>(e.Cache, e.Row, false);
                PXUIFieldAttribute.SetEnabled<EPExpenseClaimDetails.uOM>(e.Cache, e.Row, false);
                PXUIFieldAttribute.SetEnabled<EPExpenseClaimDetails.curyUnitCost>(e.Cache, e.Row, false);
                PXUIFieldAttribute.SetEnabled<EPExpenseClaimDetails.curyExtCost>(e.Cache, e.Row, false);
                PXUIFieldAttribute.SetEnabled<EPExpenseClaimDetails.curyEmployeePart>(e.Cache, e.Row, false);
                PXUIFieldAttribute.SetEnabled<EPExpenseClaimDetails.contractID>(e.Cache, e.Row, false);
                PXUIFieldAttribute.SetEnabled<EPExpenseClaimDetails.taskID>(e.Cache, e.Row, false);
                PXUIFieldAttribute.SetEnabled<EPExpenseClaimDetails.costCodeID>(e.Cache, e.Row, false);
            }
        }

        protected virtual void _(Events.RowUpdated<EPExpenseClaimDetails> e)
        {
        }

        protected virtual void _(Events.RowPersisting<EPExpenseClaimDetails> e)
        {
            if (e.Operation == PXDBOperation.Insert || e.Operation == PXDBOperation.Update)
            {
                EPExpenseClaimDetails row = e.Row;
                FSxEPExpenseClaimDetails extRow = e.Cache.GetExtension<FSxEPExpenseClaimDetails>((EPExpenseClaimDetails)e.Row);
                PXSetPropertyException exception = null;

                if (extRow != null && extRow.FSEntityNoteID != null) 
                {
                    if (extRow.FSEntityType == ID.FSEntityType.ServiceOrder)
                    {
                        FSServiceOrder serviceOrder = (FSServiceOrder)new EntityHelper(Base).GetEntityRow(typeof(FSServiceOrder), extRow.FSEntityNoteID);

                        if (serviceOrder != null) 
                        {
                            if (serviceOrder.BranchID != row.BranchID)
                                exception = new PXSetPropertyException(TX.Error.FSRelatedDocumentNotMatchCurrentDocumentBranch, PXErrorLevel.RowError);

                            if (serviceOrder.ProjectID != row.ContractID)
                            {
                                PMProject pmProjectRow = PMProject.PK.Find(Base, serviceOrder.ProjectID);
                                exception = new PXSetPropertyException(TX.Error.FSRelatedDocumentNotMatchCurrentDocumentProject,
                                                                       TX.Linked_Entity_Type.APBill,
                                                                       pmProjectRow?.ContractCD,
                                                                       e.Cache.GetValueExt<EPExpenseClaimDetails.contractID>(row), PXErrorLevel.Error);
                            }
                        }
                    }

                    if (extRow.FSEntityType == ID.FSEntityType.Appointment)
                    {
                        FSAppointment appointment = (FSAppointment)new EntityHelper(Base).GetEntityRow(typeof(FSAppointment), extRow.FSEntityNoteID);

                        if (appointment != null)
                        {
                            FSServiceOrder fsServiceOrderRow = GetServiceOrderRelated(Base, appointment.SrvOrdType, appointment.SORefNbr);

                            if (fsServiceOrderRow != null) 
                            { 
                                if (appointment.BranchID != row.BranchID)
                                    exception = new PXSetPropertyException(TX.Error.FSRelatedDocumentNotMatchCurrentDocumentBranch, PXErrorLevel.RowError);

                                if (appointment.ProjectID != row.ContractID)
                                {
                                    PMProject pmProjectRow = PMProject.PK.Find(Base, appointment.ProjectID);
                                    exception = new PXSetPropertyException(TX.Error.FSRelatedDocumentNotMatchCurrentDocumentProject,
                                                                           TX.Linked_Entity_Type.APBill,
                                                                           pmProjectRow?.ContractCD,
                                                                           e.Cache.GetValueExt<EPExpenseClaimDetails.contractID>(row), PXErrorLevel.Error);
                                }
                            }
                        }
                    }

                    if (exception != null)
                    {
                        e.Cache.RaiseExceptionHandling<FSxEPExpenseClaimDetails.fsEntityNoteID>(e.Row, extRow.FSEntityNoteID, exception);
                    }
                }
            }
        }

        protected virtual void _(Events.RowPersisted<EPExpenseClaimDetails> e)
        {
            PXGraph graph = e.Cache.Graph;
            EPExpenseClaimDetails row = e.Row;

            if (row != null && e.TranStatus == PXTranStatus.Open) 
            { 
                FSxEPExpenseClaimDetails extRow = e.Cache.GetExtension<FSxEPExpenseClaimDetails>((EPExpenseClaimDetails)e.Row);

				var item = new EntityHelper(Base).GetEntityRow(extRow.FSEntityNoteID, true);

				if (item != null)
				{
					extRow.FSEntityType = item.GetType().GetLongName();
					extRow.IsDocBilledOrClosed = IsFSDocumentBilledOrClosed(item, extRow.FSEntityType);
					extRow.IsDocRelatedToProject = IsFSDocumentRelatedToProjects(item, extRow.FSEntityType);
				}

				if (extRow != null)
                {
                    int? oriInventoryID = (int?)e.Cache.GetValueOriginal<EPExpenseClaimDetails.inventoryID>(e.Row);
                    Guid? oriFSEntityNoteID = (Guid?)e.Cache.GetValueOriginal<FSxEPExpenseClaimDetails.fsEntityNoteID>(e.Row);

                    AppointmentEntry graphAppointment = null;
                    ServiceOrderEntry graphServiceOrder = null;
                   
                    //Delete related AppointmentDet or FSSODet 
                    if (e.Operation == PXDBOperation.Delete || oriInventoryID != row.InventoryID || oriFSEntityNoteID != extRow.FSEntityNoteID) 
                    {
                        PXResult<FSSODet, FSAppointmentDet> result = (PXResult<FSSODet, FSAppointmentDet>)
                                                                            SelectFrom<FSSODet>
                                                                            .LeftJoin<FSAppointmentDet>
                                                                                .On<FSAppointmentDet.sODetID.IsEqual<FSSODet.sODetID>>
                                                                            .Where<FSSODet.linkedEntityType.IsEqual<@P.AsString>
                                                                            .And<FSSODet.linkedDocRefNbr.IsEqual<@P.AsString>>>
                                                                            .View
                                                                            .SelectSingleBound(graph, null, FSSODet.linkedEntityType.Values.ExpenseReceipt, row.ClaimDetailCD);

                        if (result != null) 
                        { 
                            //Delete Appointment if needed
                            FSAppointmentDet oldFSAppointmentDet = (FSAppointmentDet)result;
                            if (oldFSAppointmentDet != null && string.IsNullOrEmpty(oldFSAppointmentDet.LineRef) == false) 
                            {
                                graphAppointment = GetAppointmentGraph(true);

                                graphAppointment.AppointmentRecords.Current = graphAppointment.AppointmentRecords
                                                .Search<FSAppointment.refNbr>(oldFSAppointmentDet.RefNbr, oldFSAppointmentDet.SrvOrdType);

                                graphAppointment.AppointmentDetails.Delete(oldFSAppointmentDet);

                                if (graphAppointment.IsDirty)
                                {
                                    graphAppointment.Save.Press();
                                }

                                if (graphAppointment.AppointmentRecords.Current != null && Base.Caches[typeof(FSAppointment)].GetStatus(graphAppointment.AppointmentRecords.Current) == PXEntryStatus.Updated)
                                {
                                    graph.Caches[typeof(FSAppointment)].Update(graphAppointment.AppointmentRecords.Current);
                                }
                            }

                            //Delete Service Order If needed
                            FSSODet oldFSSODet = (FSSODet)result;
                            if (oldFSSODet != null && string.IsNullOrEmpty(oldFSSODet.LineRef) == false)
                            {
                                graphServiceOrder = GetServiceOrderGraph(true);

                                //Load existing ServiceOrder
                                if (graphServiceOrder.ServiceOrderRecords.Current == null
                                    || graphServiceOrder.ServiceOrderRecords.Current.RefNbr != oldFSAppointmentDet.RefNbr
                                    || graphServiceOrder.ServiceOrderRecords.Current.SrvOrdType != oldFSAppointmentDet.SrvOrdType)
                                {
                                    graphServiceOrder.ServiceOrderRecords.Current = graphServiceOrder.ServiceOrderRecords
                                                    .Search<FSServiceOrder.refNbr>(oldFSSODet.RefNbr, oldFSSODet.SrvOrdType);
                                }

                                graphServiceOrder.ServiceOrderDetails.Delete(oldFSSODet);

                                if (graphServiceOrder.IsDirty)
                                {
                                    graphServiceOrder.Save.Press();
                                }

                                if (graphServiceOrder.ServiceOrderRecords.Current != null && Base.Caches[typeof(FSServiceOrder)].GetStatus(graphServiceOrder.ServiceOrderRecords.Current) == PXEntryStatus.Updated)
                                {
                                    graph.Caches[typeof(FSServiceOrder)].Update(graphServiceOrder.ServiceOrderRecords.Current);
                                }
                            }
                        }
                    }

                    if(e.Operation != PXDBOperation.Delete && extRow.FSEntityNoteID != null) 
                    { 
                        if (extRow.FSEntityType == ID.FSEntityType.ServiceOrder)
                        {
                            FSServiceOrder serviceOrder = (FSServiceOrder)new EntityHelper(Base).GetEntityRow(typeof(FSServiceOrder), extRow.FSEntityNoteID);
                            FSServiceOrder result = UpdateServiceOrderDetail(graphServiceOrder, serviceOrder, row, extRow);

                            if (serviceOrder != null && Base.Caches[typeof(FSServiceOrder)].GetStatus(serviceOrder) == PXEntryStatus.Updated)
                            {
                                graph.Caches[typeof(FSServiceOrder)].Update(result);
                                graph.SelectTimeStamp();
                            }
                        }

                        if (extRow.FSEntityType == ID.FSEntityType.Appointment)
                        {
                            FSAppointment appointment = (FSAppointment)new EntityHelper(Base).GetEntityRow(typeof(FSAppointment), extRow.FSEntityNoteID);
                            FSAppointment result = UpdateAppointmentDetail(graphAppointment, appointment, row, extRow);

                            if (appointment != null && Base.Caches[typeof(FSAppointment)].GetStatus(appointment) == PXEntryStatus.Updated) { 
                                graph.Caches[typeof(FSAppointment)].Update(result);
                                graph.SelectTimeStamp();
                            }
                        }
                    }
                }
            }

            if (row == null || e.TranStatus != PXTranStatus.Open) return;
            Note note = PXSelect<Note, Where<Note.noteID, Equal<Required<Note.noteID>>>>.SelectSingleBound(e.Cache.Graph, null, e.Cache.GetValue(row, typeof(FSxEPExpenseClaimDetails.fsEntityNoteID).Name));
            if (note?.EntityType != null)
            {
                var item = note.NoteID.With(id => new EntityHelper(Base).GetEntityRow(id.Value, true));
                Type itemType = item.GetType();
                if (itemType != null)
                {
                    if (graph.Views.Caches.Contains(itemType)) return;
                    PXCache itemCache = graph.Caches[itemType];
                    object entity = new EntityHelper(graph).GetEntityRow(itemType, note.NoteID);
                    if (itemCache.GetStatus(entity) == PXEntryStatus.Updated)
                        itemCache.PersistUpdated(entity);
                }
            }
        }

		public virtual bool IsFSDocumentBilledOrClosed(object row, string fsEntityType)
			=> SM_ExpenseClaimDetailEntry.IsFSDocumentBilledOrClosedInt(row, fsEntityType);

		public virtual bool IsFSDocumentRelatedToProjects(object row, string fsEntityType)
			=> SM_ExpenseClaimDetailEntry.IsFSDocumentRelatedToProjectsInt(row, fsEntityType);

		public virtual FSServiceOrder GetServiceOrderRelated(PXGraph graph, string srvOrdType, string refNbr)
			=> SM_ExpenseClaimDetailEntry.GetServiceOrderRelatedInt(graph, srvOrdType, refNbr);

		public virtual FSServiceOrder UpdateServiceOrderDetail(ServiceOrderEntry graph,
													 FSServiceOrder serviceOrder,
													 EPExpenseClaimDetails row,
													 FSxEPExpenseClaimDetails extRow)
			=> SM_ExpenseClaimDetailEntry.UpdateServiceOrderDetailInt(graph = GetServiceOrderGraph(false), serviceOrder, row, extRow);

		public virtual void InsertUpdateDocDetail<DAC>(PXSelectBase dacView, object dacRow, EPExpenseClaimDetails epExpenseClaimRow, FSxEPExpenseClaimDetails fsxEPExpenseClaimDetails)
			where DAC : class, IBqlTable, IFSSODetBase, new()
			=> SM_ExpenseClaimDetailEntry.InsertUpdateDocDetailInt<DAC>(dacView, dacRow, epExpenseClaimRow, fsxEPExpenseClaimDetails);

		public virtual FSAppointment UpdateAppointmentDetail(AppointmentEntry graph,
													FSAppointment appointment,
													EPExpenseClaimDetails row,
													FSxEPExpenseClaimDetails extRow)
			=> SM_ExpenseClaimDetailEntry.UpdateAppointmentDetailInt(graph = GetAppointmentGraph(false), appointment, row, extRow);
	}
}
