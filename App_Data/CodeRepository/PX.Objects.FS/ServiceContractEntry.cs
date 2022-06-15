using PX.Data;
using PX.Objects.CR;
using System;
using System.Linq;

namespace PX.Objects.FS
{
    public class ServiceContractEntry : ServiceContractEntryBase<ServiceContractEntry, FSServiceContract, 
            Where<FSServiceContract.recordType, Equal<ListField_RecordType_ContractSchedule.ServiceContract>>>
    {        
        public ServiceContractEntry()
            : base()
        {
            ContractSchedules.AllowUpdate = false;
        }

        #region CacheAttached
        #region FSContractSchedule_SrvOrdType
        [PXDBString(4, IsFixed = true)]
        [PXUIField(DisplayName = "Service Order Type")]
        [PXDefault]
        [FSSelectorContractSrvOrdTypeAttribute]
        protected virtual void FSContractSchedule_SrvOrdType_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #region FSContractSchedule_CustomerLocationID
        [PXUIField(DisplayName = "Location ID")]
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        protected virtual void FSContractSchedule_CustomerLocationID_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #endregion

        #region Event Handlers

        #region FSServiceContract
        protected override void _(Events.RowSelecting<FSServiceContract> e)
        {
            if (e.Row == null)
            {
                return;
            }

			base._(e);

            var fsServiceContractRow = (FSServiceContract)e.Row;
            fsServiceContractRow.HasProcessedSchedule = false;
            fsServiceContractRow.HasSchedule = false;

            using (new PXConnectionScope())
            {
                 var result = PXSelectReadonly<FSSchedule,
                                     Where<
                                         FSSchedule.entityType, Equal<FSSchedule.entityType.Contract>,
                                         And<FSSchedule.entityID, Equal<Required<FSSchedule.entityID>>>>>
                                     .Select(this, fsServiceContractRow.ServiceContractID).RowCast<FSSchedule>()?.ToList();

                fsServiceContractRow.HasSchedule = result.Count() > 0;
                fsServiceContractRow.HasProcessedSchedule = result.Where(x => x.LastGeneratedElementDate != null).Count() > 0;
            }
        }

        protected override void _(Events.RowSelected<FSServiceContract> e)
        {
            base._(e);

            if (e.Row == null)
            {
                return;
            }

            var fsServiceContractRow = (FSServiceContract)e.Row;
            PXCache cache = e.Cache;

			PXUIFieldAttribute.SetEnabled<FSServiceContract.scheduleGenType>(cache, e.Row, fsServiceContractRow.HasProcessedSchedule == false);

			EnableDisableRenewalFields(cache, fsServiceContractRow);
			SharedFunctions.ServiceContractDynamicDropdown(cache, fsServiceContractRow);
        }

		protected virtual void _(Events.FieldUpdated<FSServiceContract, FSServiceContract.scheduleGenType> e)
        {
            if (e.Row == null)
            {
                return;
            }

            var fsServiceContractRow = (FSServiceContract)e.Row;

            foreach(FSContractSchedule fsContractScheduleRow in ContractSchedules.Select())
            {
                if (fsContractScheduleRow.LastGeneratedElementDate == null && fsContractScheduleRow.ScheduleGenType != fsServiceContractRow.ScheduleGenType)
                {
                    ContractSchedules.SetValueExt<FSSchedule.scheduleGenType>(fsContractScheduleRow, fsServiceContractRow.ScheduleGenType);
                }
            }
        }
		#endregion

		#region FSContractSchedule Events

		protected virtual void _(Events.RowSelected<FSContractSchedule> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSContractSchedule fsContractScheduleRow = (FSContractSchedule)e.Row;
            SharedFunctions.ShowWarningScheduleNotProcessed(e.Cache, fsContractScheduleRow);
        }

        #endregion

        #endregion

        #region Actions

        #region OpenScheduleScreen
        [PXButton(OnClosingPopup = PXSpecialButtonType.Cancel)]
        [PXUIField(DisplayName = "Add Schedule")]
        public override void AddSchedule()
		{
			if(ServiceContractSelected.Current != null) ValidateContact(ServiceContractSelected.Current);
			var graphServiceContractScheduleEntry = PXGraph.CreateInstance<ServiceContractScheduleEntry>();
            FSContractSchedule fsContractScheduleRow = new FSContractSchedule()
            {
                EntityID = ServiceContractRecords.Current.ServiceContractID,
                ScheduleGenType = ServiceContractRecords.Current.ScheduleGenType,
                ProjectID = ServiceContractRecords.Current.ProjectID
            };

            graphServiceContractScheduleEntry.ContractScheduleRecords.Insert(fsContractScheduleRow);

            throw new PXRedirectRequiredException(graphServiceContractScheduleEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
        }
        #endregion

        #region OpenScheduleScreen
        public PXAction<FSServiceContract> OpenScheduleScreen;
        [PXButton(OnClosingPopup = PXSpecialButtonType.Cancel)]
        [PXUIField(MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        protected virtual void openScheduleScreen()
        {
            if (ContractSchedules.Current != null)
            {
                var graphServiceContractScheduleEntry = PXGraph.CreateInstance<ServiceContractScheduleEntry>();

                graphServiceContractScheduleEntry.ContractScheduleRecords.Current = graphServiceContractScheduleEntry
                                                                                    .ContractScheduleRecords.Search<FSContractSchedule.scheduleID>
                                                                                    (ContractSchedules.Current.ScheduleID,
                                                                                     ServiceContractRecords.Current.ServiceContractID,
                                                                                     ServiceContractRecords.Current.CustomerID);

                throw new PXRedirectRequiredException(graphServiceContractScheduleEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }
        }
        #endregion

        #region OpenScheduleScreenByScheduleDetService
        public PXAction<FSServiceContract> OpenScheduleScreenByScheduleDetService;
        [PXButton(OnClosingPopup = PXSpecialButtonType.Cancel)]
        [PXUIField(MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        protected virtual void openScheduleScreenByScheduleDetService()
        {
            if (ScheduleDetServicesByContract.Current != null)
            {
                var graphServiceContractScheduleEntry = PXGraph.CreateInstance<ServiceContractScheduleEntry>();

                graphServiceContractScheduleEntry.ContractScheduleRecords.Current = graphServiceContractScheduleEntry
                                                                                    .ContractScheduleRecords.Search<FSContractSchedule.scheduleID>
                                                                                    (ScheduleDetServicesByContract.Current.ScheduleID,
                                                                                     ServiceContractRecords.Current.ServiceContractID);

                throw new PXRedirectRequiredException(graphServiceContractScheduleEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }
        }
        #endregion

        #region OpenScheduleScreenByScheduleDetPart
        public PXAction<FSServiceContract> OpenScheduleScreenByScheduleDetPart;
        [PXButton(OnClosingPopup = PXSpecialButtonType.Cancel)]
        [PXUIField(MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        protected virtual void openScheduleScreenByScheduleDetPart()
        {
            if (ScheduleDetPartsByContract.Current != null)
            {
                var graphServiceContractScheduleEntry = PXGraph.CreateInstance<ServiceContractScheduleEntry>();

                graphServiceContractScheduleEntry.ContractScheduleRecords.Current = graphServiceContractScheduleEntry
                                                                                    .ContractScheduleRecords.Search<FSContractSchedule.scheduleID>
                                                                                    (ScheduleDetPartsByContract.Current.ScheduleID, 
                                                                                     ServiceContractRecords.Current.ServiceContractID);

                throw new PXRedirectRequiredException(graphServiceContractScheduleEntry, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }
        }
		#endregion

		#endregion

		#region Overrides
		public override void CopySchedules(int? serviceContractID, DateTime? date)
		{
			if (serviceContractID == null || date == null)
				return;

			ServiceContractScheduleEntry graphServiceContractScheduleEntry = PXGraph.CreateInstance<ServiceContractScheduleEntry>();

			PXResultset<FSContractSchedule> sourceSchedules =
					PXSelectReadonly<FSContractSchedule,
					Where<FSContractSchedule.entityType, Equal<FSContractSchedule.entityType.Contract>,
						And<FSContractSchedule.entityID, Equal<Required<FSContractSchedule.entityID>>,
						And<FSContractSchedule.active, Equal<True>>>>>
					.Select(graphServiceContractScheduleEntry, serviceContractID);

			foreach (FSContractSchedule sourceSchedule in sourceSchedules)
			{
				graphServiceContractScheduleEntry.Clear(PXClearOption.PreserveTimeStamp);

				try
				{
					graphServiceContractScheduleEntry.IsCopyContract = true;

					FSServiceContract sourceContract = FSServiceContract.PK.Find(this, sourceSchedule.EntityID);
					FSContractSchedule targetScheduleContract = PXCache<FSContractSchedule>.CreateCopy(sourceSchedule);

					targetScheduleContract.EntityID = ServiceContractRecords.Current.ServiceContractID;
					targetScheduleContract.ScheduleID = null;
					targetScheduleContract.RefNbr = null;
					targetScheduleContract.NoteID = null;
					targetScheduleContract.LastGeneratedElementDate = null;
					targetScheduleContract.EstimatedDurationTotal = 0;
					targetScheduleContract.ScheduleDuration = 0;
					targetScheduleContract.OrigServiceContractRefNbr = sourceContract?.RefNbr;
					targetScheduleContract.OrigScheduleRefNbr = sourceSchedule.RefNbr;

					targetScheduleContract.StartDate = date;
					if (sourceSchedule.StartDate.HasValue && sourceSchedule.EndDate.HasValue)
					{
						targetScheduleContract.EndDate = date.Value.AddDays(sourceSchedule.EndDate.Value.Subtract(sourceSchedule.StartDate.Value).Days);
					}

					targetScheduleContract = graphServiceContractScheduleEntry.ContractScheduleRecords.Insert(targetScheduleContract);

					SharedFunctions.CopyNotesAndFiles(
						graphServiceContractScheduleEntry.ContractScheduleRecords.Cache,
						graphServiceContractScheduleEntry.ContractScheduleRecords.Cache,
						sourceSchedule,
						targetScheduleContract,
						true,
						true);

					graphServiceContractScheduleEntry.Answers.CopyAllAttributes(targetScheduleContract, sourceSchedule);

					PXResultset<FSScheduleDet> sourceScheduleDetails =
							PXSelectReadonly<FSScheduleDet,
							Where<FSScheduleDet.scheduleID, Equal<Required<FSScheduleDet.scheduleID>>>>
							.Select(this, sourceSchedule.ScheduleID);

					foreach (FSScheduleDet sourceScheduleDet in sourceScheduleDetails)
					{
						FSScheduleDet targetScheduleDet = PXCache<FSScheduleDet>.CreateCopy(sourceScheduleDet);

						targetScheduleDet.ScheduleID = targetScheduleContract.ScheduleID;
						targetScheduleDet.LineNbr = null;
						targetScheduleDet.NoteID = null;

						targetScheduleDet = (FSScheduleDet)graphServiceContractScheduleEntry.ScheduleDetails.Cache.Insert(targetScheduleDet);

						SharedFunctions.CopyNotesAndFiles(
							graphServiceContractScheduleEntry.ScheduleDetails.Cache,
							graphServiceContractScheduleEntry.ScheduleDetails.Cache,
							sourceScheduleDet,
							targetScheduleDet,
							true,
							true);
					}

					graphServiceContractScheduleEntry.Actions.PressSave();
				}finally
				{
					graphServiceContractScheduleEntry.IsCopyContract = false;
				}
			}

		}

		public override void OpenCopiedContract(FSServiceContract contract)
		{
			ServiceContractEntry newSCGraph = PXGraph.CreateInstance<ServiceContractEntry>();
			newSCGraph.ServiceContractRecords.Current = newSCGraph.ServiceContractRecords.Search<FSServiceContract.refNbr>(contract.RefNbr);

			throw new PXRedirectRequiredException(newSCGraph, null) { Mode = PXBaseRedirectException.WindowMode.Same };
		}
		#endregion

		#region Validations
		public virtual void ValidateContact(FSServiceContract row)
		{
			if (row.CustomerContactID == null) return;
			CR.Contact contact = CR.Contact.PK.Find(this, row.CustomerContactID);
			if (contact?.Status != ContactStatus.Active)
				throw new PXException(string.Format(TX.Error.OperationCannotPerformDueToContactDeactivated, contact.DisplayName), PXErrorLevel.Error);
		}
		#endregion
	}
}
