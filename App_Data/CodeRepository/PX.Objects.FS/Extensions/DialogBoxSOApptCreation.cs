using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Common;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.CR.Extensions;
using PX.Objects.IN;
using PX.Objects.PM;

namespace PX.Objects.FS
{
    public abstract class DialogBoxSOApptCreation<TExtension, TGraph, TMain>
        : PXGraphExtension<TExtension, TGraph>
        where TExtension : PXGraphExtension<TGraph>, new()
        where TGraph : PXGraph, new()
        where TMain : class, IBqlTable, new()
    {
        public CRValidationFilter<DBoxDocSettings> DocumentSettings;

        #region Actions
        #region CreateSrvOrdDocument
        public PXAction<TMain> CreateSrvOrdDocument;
        [PXButton]
        [PXUIField(DisplayName = "Create Service Order", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual IEnumerable createSrvOrdDocument(PXAdapter adapter)
        {
            bool requiredFieldsFilled = DocumentSettings.AskExtFullyValid((graph, view) => { SetDBoxDefaults(ID.PostDoc_EntityType.SERVICE_ORDER); }, DialogAnswerType.Positive, false);
            ShowDialogBoxAndProcess(requiredFieldsFilled);

            return adapter.Get();
        }
        #endregion

        #region CreateApptDocument
        public PXAction<TMain> CreateApptDocument;
        [PXButton]
        [PXUIField(DisplayName = "Create Appointment", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual IEnumerable createApptDocument(PXAdapter adapter)
        {
            bool requiredFieldsFilled = DocumentSettings.AskExtFullyValid((graph, view) => { SetDBoxDefaults(ID.PostDoc_EntityType.APPOINTMENT); }, DialogAnswerType.Positive, true);
            ShowDialogBoxAndProcess(requiredFieldsFilled);

            return adapter.Get();
        }
        #endregion

        #region CreateInCalendar
        PXAction<TMain> CreateInCalendar;
        [PXButton]
        [PXUIField(DisplayName = "Create in Calendar", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual void createInCalendar()
        {
            throw new NotImplementedException();
        }
        #endregion
        #endregion

        #region Events
        #region FieldDefaulting
        protected virtual void _(Events.FieldDefaulting<DBoxDocSettings, DBoxDocSettings.scheduledDateTimeBegin> e)
        {
            e.NewValue = PXDBDateAndTimeAttribute.CombineDateTime(Base.Accessinfo.BusinessDate, PXTimeZoneInfo.Now);
        }
        #endregion

        protected virtual void _(Events.RowSelected<DBoxDocSettings> e)
        {
            if (e.Row == null)
            {
                return;
            }

            DBoxDocSettings settings = e.Row;

            PXDefaultAttribute.SetPersistingCheck<DBoxDocSettings.scheduledDateTimeBegin>
                    (e.Cache, e.Row, (settings.DestinationDocument == ID.PostDoc_EntityType.APPOINTMENT) ? 
                                                PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
            PXDefaultAttribute.SetPersistingCheck<DBoxDocSettings.scheduledDateTimeEnd>
                    (e.Cache, e.Row, (settings.DestinationDocument == ID.PostDoc_EntityType.APPOINTMENT
                                        && e.Row.HandleManuallyScheduleTime == true) ?
                                                PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
            PXUIFieldAttribute.SetVisible<DBoxDocSettings.projectTaskID>
                    (e.Cache, e.Row, !ProjectDefaultAttribute.IsNonProject(e.Row.ProjectID));
        }
        #endregion

        #region VirtualFunctions
        public virtual FSServiceOrder CreateDocument(
            ServiceOrderEntry srvOrdGraph,
            AppointmentEntry apptGraph,
            string sourceDocumentEntity,
            string sourceDocType,
            string sourceDocRefNbr,
            int? sourceDocID,
            PXCache headerCache,
            PXCache linesCache,
            DBoxHeader header,
            List<DBoxDetails> details,
            bool createAppointment)
        {
            srvOrdGraph.Clear();

            FSServiceOrder serviceOrderRow = new FSServiceOrder();

            serviceOrderRow.SrvOrdType = header.SrvOrdType;
            serviceOrderRow.SourceType = sourceDocumentEntity;
            serviceOrderRow.SourceDocType = sourceDocType;
            serviceOrderRow.SourceRefNbr = sourceDocRefNbr;
            serviceOrderRow.SourceID = sourceDocID;

            serviceOrderRow = srvOrdGraph.ServiceOrderRecords.Current = srvOrdGraph.ServiceOrderRecords.Insert(serviceOrderRow);
            serviceOrderRow = (FSServiceOrder)srvOrdGraph.ServiceOrderRecords.Cache.CreateCopy(serviceOrderRow);

            FSSrvOrdType fsSrvOrdTypeRow = FSSrvOrdType.PK.Find(Base, header.SrvOrdType);

            #region ServiceOrderHeader
            if (fsSrvOrdTypeRow.Behavior != FSSrvOrdType.behavior.Values.InternalAppointment)
            {
                serviceOrderRow.CustomerID = header.CustomerID;
                serviceOrderRow.LocationID = header.LocationID;
                serviceOrderRow = srvOrdGraph.ServiceOrderRecords.Update(serviceOrderRow);
                serviceOrderRow = (FSServiceOrder)srvOrdGraph.ServiceOrderRecords.Cache.CreateCopy(serviceOrderRow);
            }

            serviceOrderRow.CuryID = header.CuryID;
            serviceOrderRow.BranchID = header.BranchID;
            serviceOrderRow.BranchLocationID = header.BranchLocationID;
            serviceOrderRow.ContactID = header.ContactID;
            serviceOrderRow.DocDesc = header.Description;
			serviceOrderRow.LongDescr = header.LongDescr;
			serviceOrderRow.ProjectID = header.ProjectID;
            serviceOrderRow.DfltProjectTaskID = header.ProjectTaskID;
            serviceOrderRow.SalesPersonID = header.SalesPersonID;
            serviceOrderRow.OrderDate = header.OrderDate;
            serviceOrderRow.SLAETA = header.SLAETA;
            serviceOrderRow.ProblemID = header.ProblemID;
            serviceOrderRow.AssignedEmpID = header.AssignedEmpID;
            serviceOrderRow.AllowOverrideContactAddress = header.Contact != null || header.Address != null;

            serviceOrderRow = srvOrdGraph.ServiceOrderRecords.Update(serviceOrderRow);
            serviceOrderRow = (FSServiceOrder)srvOrdGraph.ServiceOrderRecords.Cache.CreateCopy(serviceOrderRow);
            #endregion

            #region Contact
            if (header.Contact != null)
            {
                FSContact fsContactRow = srvOrdGraph.ServiceOrder_Contact.Select();

                fsContactRow = (FSContact)srvOrdGraph.ServiceOrder_Contact.Cache.CreateCopy(fsContactRow);

                fsContactRow.FullName = header.Contact.FullName;
                fsContactRow.Title = header.Contact.Title;
                fsContactRow.Attention = header.Contact.Attention;
                fsContactRow.Email = header.Contact.Email;
                fsContactRow.Phone1 = header.Contact.Phone1;
                fsContactRow.Phone2 = header.Contact.Phone2;
                fsContactRow.Phone3 = header.Contact.Phone3;
                fsContactRow.Fax = header.Contact.Fax;

                srvOrdGraph.ServiceOrder_Contact.Update(fsContactRow);
            }
            #endregion
            #region Address
            if (header.Address != null) 
            {
                FSAddress fsAddressRow = srvOrdGraph.ServiceOrder_Address.Select();

                fsAddressRow = (FSAddress)srvOrdGraph.ServiceOrder_Address.Cache.CreateCopy(fsAddressRow);

                fsAddressRow.AddressLine1 = header.Address.AddressLine1;
                fsAddressRow.AddressLine2 = header.Address.AddressLine2;
                fsAddressRow.AddressLine3 = header.Address.AddressLine3;
                fsAddressRow.City = header.Address.City;
                fsAddressRow.CountryID = header.Address.CountryID;
                fsAddressRow.State = header.Address.State;
                fsAddressRow.PostalCode = header.Address.PostalCode;

                srvOrdGraph.ServiceOrder_Address.Update(fsAddressRow);
            }
            #endregion

            if (serviceOrderRow.TaxZoneID != header.TaxZoneID)
            {
                serviceOrderRow.TaxZoneID = header.TaxZoneID;
            }

            serviceOrderRow = srvOrdGraph.ServiceOrderRecords.Update(serviceOrderRow);

            if (header.CopyNotes == true || header.CopyFiles == true) { 
                SharedFunctions.CopyNotesAndFiles(headerCache,
                                                  srvOrdGraph.ServiceOrderRecords.Cache,
                                                  header.sourceDocument,
                                                  serviceOrderRow,
                                                  header.CopyNotes,
                                                  header.CopyFiles);
            }

            UDFHelper.CopyAttributes(headerCache, header.sourceDocument, srvOrdGraph.ServiceOrderRecords.Cache, serviceOrderRow, null);

            foreach (DBoxDetails line in details)
            {
                if (line.InventoryID != null)
                {
                    InventoryItem inventoryItemRow = SharedFunctions.GetInventoryItemRow(srvOrdGraph, line.InventoryID);

                    if (inventoryItemRow.StkItem == true
                            && srvOrdGraph.ServiceOrderTypeSelected.Current.PostTo == ID.SrvOrdType_PostTo.ACCOUNTS_RECEIVABLE_MODULE)
                    {
                        throw new PXException(TX.Error.STOCKITEM_NOT_HANDLED_BY_SRVORDTYPE, inventoryItemRow.InventoryCD);
                    }

                    FSSODet srvOrdLine = new FSSODet();
                    var soDetCache = srvOrdGraph.ServiceOrderDetails.Cache;

                    srvOrdLine.SourceNoteID = line.SourceNoteID;
                    soDetCache.Current = srvOrdLine = (FSSODet)soDetCache.Insert(srvOrdLine);
                    srvOrdLine = (FSSODet)soDetCache.CreateCopy(srvOrdLine);

                    srvOrdLine.LineType = line.LineType;
                    srvOrdLine.InventoryID = line.InventoryID;
                    srvOrdLine.IsFree = line.IsFree;

                    soDetCache.Current = srvOrdLine = (FSSODet)soDetCache.Update(srvOrdLine);
                    srvOrdLine = (FSSODet)soDetCache.CreateCopy(srvOrdLine);

                    srvOrdLine.BillingRule = line.BillingRule;
                    srvOrdLine.TranDesc = line.TranDesc;

                    if (line.SiteID != null)
                    {
                        srvOrdLine.SiteID = line.SiteID;
                    }

                    srvOrdLine.EstimatedDuration = line.EstimatedDuration;
                    srvOrdLine.EstimatedQty = line.EstimatedQty;
                    srvOrdLine = (FSSODet)soDetCache.Update(srvOrdLine);
                    srvOrdLine = (FSSODet)soDetCache.CreateCopy(srvOrdLine);

                    srvOrdLine.CuryUnitPrice = line.CuryUnitPrice;
                    srvOrdLine.ManualPrice = line.ManualPrice;

                    srvOrdLine.ProjectID = line.ProjectID;
                    srvOrdLine.ProjectTaskID = line.ProjectTaskID;
                    srvOrdLine.CostCodeID = line.CostCodeID;

                    srvOrdLine.ManualCost = line.EnablePO;

                    if (srvOrdLine.ManualCost == true)
                    {
                        srvOrdLine.CuryUnitCost = line.CuryUnitCost;
                    }

                    srvOrdLine.EnablePO = line.EnablePO;
                    srvOrdLine.POVendorID = line.POVendorID;
                    srvOrdLine.POVendorLocationID = line.POVendorLocationID;

                    srvOrdLine.TaxCategoryID = line.TaxCategoryID;

                    srvOrdLine.DiscPct = line.DiscPct;
                    srvOrdLine.CuryDiscAmt = line.CuryDiscAmt;
                    srvOrdLine.CuryBillableExtPrice = line.CuryBillableExtPrice;

                    soDetCache.Current = srvOrdLine = (FSSODet)soDetCache.Update(srvOrdLine);

                    SharedFunctions.CopyNotesAndFiles(linesCache,
                                                srvOrdGraph.ServiceOrderDetails.Cache,
                                                line.sourceLine,
                                                srvOrdLine,
                                                header.CopyNotes,
                                                header.CopyFiles);
                    
                    srvOrdLine = (FSSODet)soDetCache.Current;
                    OnAfterServiceOrderLineInsert(
                        srvOrdGraph.ServiceOrderDetails.Cache,
                        srvOrdLine,
                        linesCache,
                        line.sourceLine);
                }
            }

            serviceOrderRow = srvOrdGraph.ServiceOrderRecords.Current;
            OnBeforeServiceOrderPersist(
                srvOrdGraph.ServiceOrderRecords.Cache,
                serviceOrderRow,
                headerCache,
                header.sourceDocument);

            srvOrdGraph.Actions.PressSave();
            serviceOrderRow = srvOrdGraph.ServiceOrderRecords.Current;

            if (createAppointment == true)
            {
                FSAppointment appt = new FSAppointment();
                appt.SrvOrdType = serviceOrderRow.SrvOrdType;

                appt = apptGraph.AppointmentRecords.Current = apptGraph.AppointmentRecords.Insert(appt);
                appt = (FSAppointment)apptGraph.AppointmentRecords.Cache.CreateCopy(appt);

                appt.SORefNbr = serviceOrderRow.RefNbr;
                appt = apptGraph.AppointmentRecords.Current = apptGraph.AppointmentRecords.Update(appt);
                appt = (FSAppointment)apptGraph.AppointmentRecords.Cache.CreateCopy(appt);

                appt.ScheduledDateTimeBegin = header.ScheduledDateTimeBegin;

				if (fsSrvOrdTypeRow.CopyNotesToAppoinment == true)
					appt.LongDescr = header.LongDescr;

                if (header.HandleManuallyScheduleTime == true)
                {
                    appt.HandleManuallyScheduleTime = header.HandleManuallyScheduleTime;
                    appt.ScheduledDateTimeEnd = header.ScheduledDateTimeEnd;
                }

                appt = apptGraph.AppointmentRecords.Current = apptGraph.AppointmentRecords.Update(appt);

                UDFHelper.CopyAttributes(headerCache, header.sourceDocument, apptGraph.AppointmentRecords.Cache, appt, null);

                OnBeforeAppointmentPersist(
                    apptGraph.AppointmentRecords.Cache,
                    appt,
                    headerCache,
                    header.sourceDocument);

                apptGraph.Actions.PressSave();
                appt = apptGraph.AppointmentRecords.Current;
            }

            return serviceOrderRow;
        }

        public virtual void SetDBoxDefaults(string destinationDocument)
        {
            DocumentSettings.Current.DestinationDocument = destinationDocument;
            PrepareDBoxDefaults();
        }

        public virtual void ShowDialogBoxAndProcess(bool requiredFieldsFilled)
        {
            WebDialogResult dBoxAnswer = DocumentSettings.View.Answer;

            if (requiredFieldsFilled
                &&
                (dBoxAnswer == WebDialogResult.OK
                || dBoxAnswer == WebDialogResult.Yes))
            {
                var processingGraph = Base.Clone();

                PXLongOperation.StartOperation(Base, () =>
                {
                    DBoxHeader header = DocumentSettings.Current;
                    List<DBoxDetails> details = new List<DBoxDetails>();
                    ServiceOrderEntry srvOrdGraph = PXGraph.CreateInstance<ServiceOrderEntry>();
                    AppointmentEntry apptGraph = PXGraph.CreateInstance<AppointmentEntry>();

                    var extension = processingGraph.GetProcessingExtension<DialogBoxSOApptCreation<TExtension, TGraph, TMain>>();

                    extension.PrepareFilterFields(header, details);

                    using (var ts = new PXTransactionScope())
                    {
                        extension.CreateDocument(srvOrdGraph, apptGraph, header, details);
                        ts.Complete();
                    }

                    if (dBoxAnswer == WebDialogResult.Yes)
                    {
                        if (!Base.IsContractBasedAPI)
                        {
                            if (header.CreateAppointment == true
                                && apptGraph.AppointmentRecords.Current != null)
                            {
                                throw new PXRedirectRequiredException(apptGraph, null);
                            }
                            else if (srvOrdGraph.ServiceOrderRecords.Current != null)
                            {
                                throw new PXRedirectRequiredException(srvOrdGraph, null);
                            }
                        }
                    }
                });
            }
        }

        public virtual void PrepareFilterFields(
            DBoxHeader header,
            List<DBoxDetails> details)
        {
            PrepareHeaderAndDetails(header, details);
        }

        public virtual void OnBeforeServiceOrderPersist(
            PXCache cacheSrvOrd,
            FSServiceOrder srvOrd,
            PXCache sourceDocCache,
            object sourceDoc)
        {
        }

        public virtual void OnAfterServiceOrderLineInsert(
            PXCache cacheSrvOrdLine,
            FSSODet srvOrdLine,
            PXCache sourceLineCache,
            object sourceLine)
        {
        }

        public virtual void OnBeforeAppointmentPersist(
            PXCache cacheAppt,
            FSAppointment appt,
            PXCache sourceDocCache,
            object sourceDoc)
        {
        }
        #endregion

        #region AbstractFunctions
        public abstract void PrepareDBoxDefaults();

        public abstract void PrepareHeaderAndDetails(
            DBoxHeader header,
            List<DBoxDetails> details);

        public abstract void CreateDocument(
            ServiceOrderEntry srvOrdGraph,
            AppointmentEntry apptGraph,
            DBoxHeader header,
            List<DBoxDetails> details);
        #endregion
    }
}
