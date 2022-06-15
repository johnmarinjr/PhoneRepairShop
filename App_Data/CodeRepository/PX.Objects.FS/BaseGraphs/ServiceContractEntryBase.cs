using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.FS.Scheduler;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.SM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PX.Objects.FS
{
	public class ServiceContractEntryBase<TGraph, TPrimary, TWhere> : PXGraph<TGraph, TPrimary>
        where TGraph : PX.Data.PXGraph
        where TPrimary : class, PX.Data.IBqlTable, new()
        where TWhere : class, IBqlWhere, new()
    {
        public bool isStatusChanged = false;
        public bool insertContractActionForSchedules = false;
        public bool skipStatusSmartPanels = false;

        public ServiceContractEntryBase()
        {
        }

        public bool IsCopyContract
        {
            get;
            protected set;
        }

		public bool IsRenewContract
		{
			get;
			protected set;
		}

		public bool IsForcastProcess
		{
			get;
			protected set;
		}

		public static class FSMailing
		{
			public const string EMAIL_SERVICE_CONTRACT_QUOTE = "FS CONTRACT QUOTE";
		}

		#region Selects
		[PXHidden]
        public PXSelect<BAccount> BAccount;
        [PXHidden]
        public PXSelect<Contact> Contact;

        // Baccount workaround
        [PXHidden]
        public PXSetup<FSSetup> SetupRecord;

        public CRAttributeList<FSServiceContract> Answers;

        public PXSelectJoin<FSServiceContract,
            LeftJoinSingleTable<Customer, On<Customer.bAccountID, Equal<FSServiceContract.customerID>>>,
            Where2<
                Where<Customer.bAccountID, IsNull, Or<Match<Customer, Current<AccessInfo.userName>>>>,
                And<TWhere>>> ServiceContractRecords;

        public PXSelect<FSServiceContract, 
               Where<
                   FSServiceContract.serviceContractID,Equal<Current<FSServiceContract.serviceContractID>>>> ServiceContractSelected;

        [PXCopyPasteHiddenView]
        public PXFilter<FSContractPeriodFilter> ContractPeriodFilter;

        public PXSelect<FSContractPeriod, 
               Where<
                   FSContractPeriod.serviceContractID, Equal<Current<FSServiceContract.serviceContractID>>,
               And<
                   Where2<
                       Where<
                           Current<FSContractPeriodFilter.contractPeriodID>, IsNull,
                       And<
                           FSContractPeriod.status, NotEqual<FSContractPeriod.status.Active>>>,
                   Or<
                       FSContractPeriod.contractPeriodID, Equal<Current<FSContractPeriodFilter.contractPeriodID>>>>>>> ContractPeriodRecords;

        public PXSelect<FSContractPeriodDet,
               Where<
                   FSContractPeriodDet.contractPeriodID, Equal<Current<FSContractPeriodFilter.contractPeriodID>>,
               And<
                   FSContractPeriodDet.serviceContractID, Equal<Current<FSServiceContract.serviceContractID>>>>> ContractPeriodDetRecords;

        public PXSelectReadonly3<FSScheduleDet,
               InnerJoin<FSSchedule,
               On<
                   FSSchedule.scheduleID, Equal<FSScheduleDet.scheduleID>>,
               InnerJoin<FSServiceContract,
               On<
                   FSServiceContract.serviceContractID, Equal<FSSchedule.entityID>,
                   And<FSServiceContract.serviceContractID, Equal<Current<FSServiceContract.serviceContractID>>,
                   And<FSScheduleDet.lineType, Equal<FSLineType.Service>>>>>>,
               OrderBy<
                   Asc<FSSchedule.refNbr>>> ScheduleServicesByContract;

        #region ScheduleDetServices

		public PXSelectReadonly2<FSScheduleDet,
               InnerJoin<FSSchedule,
               On<
                   FSSchedule.scheduleID, Equal<FSScheduleDet.scheduleID>>,
               InnerJoin<FSServiceContract,
               On<
                   FSServiceContract.serviceContractID, Equal<FSSchedule.entityID>,
                   And<FSServiceContract.serviceContractID, Equal<Current<FSServiceContract.serviceContractID>>>>>>,
               Where<
                   FSScheduleDet.lineType, Equal<FSLineType.Service>>,
               OrderBy<
                   Asc<FSSchedule.refNbr>>> ScheduleDetServicesByContract;
        #endregion

        #region ScheduleDetParts

        public PXSelectReadonly2<FSScheduleDet,
                                InnerJoin<FSSchedule,
                                    On<
                                        FSSchedule.scheduleID, Equal<FSScheduleDet.scheduleID>>,
                                InnerJoin<FSServiceContract,
                                    On<
                                        FSServiceContract.serviceContractID, Equal<FSSchedule.entityID>,
                                    And<
                                        FSServiceContract.serviceContractID, Equal<Current<FSServiceContract.serviceContractID>>,
                                    And<FSScheduleDet.lineType, Equal<FSLineType.Inventory_Item>>>>>>,
                                Where<True, Equal<True>>,
                                OrderBy<
                                        Asc<FSSchedule.refNbr>>> ScheduleDetPartsByContract;

        #endregion

        [PXCopyPasteHiddenView]
        public PXSelectJoin<FSSalesPrice,
                        InnerJoin<InventoryItem,
                            On<InventoryItem.inventoryID, Equal<FSSalesPrice.inventoryID>>,
                        InnerJoin<FSServiceContract,
                            On<
                                FSServiceContract.serviceContractID, Equal<FSSalesPrice.serviceContractID>,
                            And<
                                FSServiceContract.serviceContractID, Equal<Current<FSServiceContract.serviceContractID>>>>>>> SalesPriceLines;

        [PXCopyPasteHiddenView]
        public PXSelect<FSContractAction, 
               Where<
                   FSContractAction.serviceContractID, Equal<Current<FSServiceContract.serviceContractID>>>> ContractHistoryItems;

        [PXCopyPasteHiddenView]
        public PXFilter<FSActivationContractFilter> ActivationContractFilter;

        [PXCopyPasteHiddenView]
        public PXFilter<FSCopyContractFilter> CopyContractFilter;

		[PXCopyPasteHiddenView]
        public PXSelect<FSContractSchedule,
                                Where<
                                    FSContractSchedule.entityID, Equal<Current<FSServiceContract.serviceContractID>>>> ContractSchedules;

		[PXCopyPasteHiddenView]
        public PXFilter<FSTerminateContractFilter> TerminateContractFilter;

        [PXCopyPasteHiddenView]
        public PXFilter<FSSuspendContractFilter> SuspendContractFilter;

        [PXCopyPasteHiddenView]
        public PXSelect<ActiveSchedule, 
               Where<
                   ActiveSchedule.entityID, Equal<Current<FSServiceContract.serviceContractID>>, 
                   And<ActiveSchedule.active, Equal<True>>>> ActiveScheduleRecords;

        [PXCopyPasteHiddenView]
        public PXSelect<FSContractPostDoc,
                    Where<FSContractPostDoc.contractPeriodID, Equal<Current<FSContractPeriodFilter.contractPeriodID>>,
                        And<FSContractPostDoc.serviceContractID, Equal<Current<FSServiceContract.serviceContractID>>>>> ContractPostDocRecords;

        [PXCopyPasteHiddenView]
        public PXSelect<FSBillHistory,
               Where<
                   FSBillHistory.serviceContractRefNbr, Equal<Current<FSServiceContract.refNbr>>,
                   And<FSBillHistory.srvOrdType, IsNull>>,
               OrderBy<
                   Desc<FSBillHistory.createdDateTime>>> InvoiceRecords;

		[PXCopyPasteHiddenView]
		public CRValidationFilter<FSContractForecastFilter> ForecastFilter;

		[PXCopyPasteHiddenView]
		public PXSelect<FSContractForecast,
				Where<FSContractForecast.serviceContractID, Equal<Current<FSServiceContract.serviceContractID>>,
					And<FSContractForecast.active, Equal<True>>>>
			   forecastRecords;

		[PXCopyPasteHiddenView]
		public PXSelect<FSContractForecastDet,
				Where<FSContractForecastDet.serviceContractID, Equal<Current<FSServiceContract.serviceContractID>>,
					And<FSContractForecastDet.forecastID, Equal<Current<FSContractForecast.forecastID>>>>>
			   forecastDetRecords;

		[PXCopyPasteHiddenView]
		public FSContractActivities Activity;
		#endregion

		#region Report
		public PXAction<FSServiceContract> report;
        [PXButton(SpecialType = PXSpecialButtonType.ReportsFolder, MenuAutoOpen = true)]
        [PXUIField(DisplayName = "Reports")]
        public virtual IEnumerable Report(PXAdapter adapter,
            [PXString(8, InputMask = "CC.CC.CC.CC")]
            string reportID
            )
        {
            List<FSServiceContract> list = adapter.Get<FSServiceContract>().ToList();
            if (!string.IsNullOrEmpty(reportID))
            {
                Save.Press();
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                string actualReportID = null;

                PXReportRequiredException ex = null;
                Dictionary<PX.SM.PrintSettings, PXReportRequiredException> reportsToPrint = new Dictionary<PX.SM.PrintSettings, PXReportRequiredException>();

                foreach (FSServiceContract contract in list)
                {
                    parameters = new Dictionary<string, string>();
					parameters["FSServiceContract.RefNbr"] = contract.RefNbr;

					object customer = PXSelectorAttribute.Select<FSServiceContract.customerID>(ServiceContractSelected.Cache, contract);
                    actualReportID = new NotificationUtility(this).SearchReport(SO.SONotificationSource.Customer, customer, reportID, contract.BranchID);
                    ex = PXReportRequiredException.CombineReport(ex, actualReportID, parameters);

                    reportsToPrint = PX.SM.SMPrintJobMaint.AssignPrintJobToPrinter(reportsToPrint, parameters, adapter, new NotificationUtility(this).SearchPrinter, SO.SONotificationSource.Customer, reportID, actualReportID, contract.BranchID);
                }

                if (ex != null)
                {
                    PX.SM.SMPrintJobMaint.CreatePrintJobGroups(reportsToPrint);

                    throw ex;
                }
            }
            return adapter.Get();
        }
        #endregion

        #region Actions
        #region Cancel
        [PXCancelButton]
        [PXUIField(DisplayName = ActionsMessages.Cancel, MapEnableRights = PXCacheRights.Select)]
        protected new virtual IEnumerable Cancel(PXAdapter a)
        {
            InvoiceRecords.Cache.ClearQueryCache();
            return (new PXCancel<FSServiceContract>(this, "Cancel")).Press(a);
        }
        #endregion

        #region ActivateContract
        public PXAction<FSServiceContract> activateContract;
        [PXButton(Category = CommonActionCategories.DisplayNames.Processing)]
        [PXUIField(DisplayName = "Activate")]
        public virtual IEnumerable ActivateContract(PXAdapter adapter)
        {
            List<FSServiceContract> fsServiceContracts = adapter.Get<FSServiceContract>().ToList();
            string errorMessage = "";

            foreach (FSServiceContract fsServiceContractRow in fsServiceContracts)
            {
                if (fsServiceContractRow.isEditable())
                {
                    Save.Press();
                }

                if (CheckNewContractStatus(this,
                                           fsServiceContractRow,
                                           ID.Status_ServiceContract.ACTIVE,
                                           ref errorMessage) == false)
                {
                    throw new PXException(errorMessage);
                }

                if (fsServiceContractRow.Status != ID.Status_ServiceContract.DRAFT)
                {
                    if (skipStatusSmartPanels == true || IsImport || ActivationContractFilter.AskExt() == WebDialogResult.OK)
                    {
                        if (CheckDatesApplyOrScheduleStatusChange(this.ServiceContractRecords.Cache, fsServiceContractRow, this.Accessinfo.BusinessDate, ActivationContractFilter.Current.ActivationDate))
                        {
                            ApplyOrScheduleStatusChange(this, this.ServiceContractRecords.Cache, fsServiceContractRow, this.Accessinfo.BusinessDate, ActivationContractFilter.Current.ActivationDate, ID.Status_ServiceContract.ACTIVE);
                            UpdateSchedulesByActivateContract();
                            ApplyContractPeriodStatusChange(fsServiceContractRow);
                        }
                    }

                }
                else
                {
                    ApplyOrScheduleStatusChange(this, this.ServiceContractRecords.Cache, fsServiceContractRow, this.Accessinfo.BusinessDate, this.Accessinfo.BusinessDate, ID.Status_ServiceContract.ACTIVE);
                    ApplyContractPeriodStatusChange(fsServiceContractRow);

                    if (fsServiceContractRow.BillingType == FSServiceContract.billingType.Values.StandardizedBillings
                        || fsServiceContractRow.IsFixedRateContract == true)
                    {
                        ActivateCurrentPeriod();
                    }
                }
            }

            return fsServiceContracts;
        }
        #endregion

        #region SuspendContract
        public PXAction<FSServiceContract> suspendContract;
        [PXButton(Category = CommonActionCategories.DisplayNames.Processing)]
        [PXUIField(DisplayName = "Suspend")]
        public virtual IEnumerable SuspendContract(PXAdapter adapter)
        {
            List<FSServiceContract> fsServiceContracts = adapter.Get<FSServiceContract>().ToList();
            string errorMessage = "";

            foreach (FSServiceContract fsServiceContractRow in fsServiceContracts)
            {
                using (PXTransactionScope ts = new PXTransactionScope())
                {
                    if (CheckNewContractStatus(this,
                                               fsServiceContractRow,
                                               ID.Status_ServiceContract.SUSPENDED,
                                               ref errorMessage) == false)
                    {
                        throw new PXException(errorMessage);
                    }

                    if (skipStatusSmartPanels == true || SuspendContractFilter.AskExt() == WebDialogResult.OK)
                    {
                        if (CheckDatesApplyOrScheduleStatusChange(this.ServiceContractRecords.Cache, fsServiceContractRow, this.Accessinfo.BusinessDate, SuspendContractFilter.Current.SuspensionDate))
                        {
                            ApplyOrScheduleStatusChange(this, this.ServiceContractRecords.Cache, fsServiceContractRow, this.Accessinfo.BusinessDate, SuspendContractFilter.Current.SuspensionDate, ID.Status_ServiceContract.SUSPENDED);
                            UpdateSchedulesBySuspendContract(SuspendContractFilter.Current.SuspensionDate);
                            ForceUpdateCacheAndSave(ServiceContractRecords.Cache, fsServiceContractRow);
                        }
                    }

                    ts.Complete();
                }
            }

            return fsServiceContracts;
        }
		#endregion

		#region RenewContract
		public PXAction<FSServiceContract> renewContract;
		[PXButton(Category = CommonActionCategories.DisplayNames.Processing)]
		[PXUIField(DisplayName = "Renew")]
		public virtual IEnumerable RenewContract(PXAdapter adapter)
		{
			Save.Press();

			try
			{
				this.IsRenewContract = true;

				bool activatePeriod = false;

				List<FSServiceContract> fsServiceContracts = adapter.Get<FSServiceContract>().ToList();

				foreach (FSServiceContract fsServiceContractRow in fsServiceContracts)
				{
					fsServiceContractRow.RenewalDate = fsServiceContractRow.EndDate.Value.AddDays(1);

					if (fsServiceContractRow.DurationType != SC_Duration_Type.CUSTOM)
					{
						fsServiceContractRow.EndDate = GetEndDate(fsServiceContractRow, fsServiceContractRow.RenewalDate.Value, fsServiceContractRow.RenewalDate.Value);
					}
					else
					{
						fsServiceContractRow.EndDate = fsServiceContractRow.RenewalDate?.AddDays(fsServiceContractRow.Duration ?? 1).AddDays(-1);
					}

					fsServiceContractRow.StatusEffectiveUntilDate = fsServiceContractRow.EndDate.Value;

					ServiceContractRecords.Update(fsServiceContractRow);

					if (fsServiceContractRow.BillingType == FSServiceContract.billingType.Values.StandardizedBillings
						|| fsServiceContractRow.IsFixedRateContract == true)
					{
						FSContractPeriod fsCurrentContractPeriodRow = PXSelect<FSContractPeriod,
																		Where<FSContractPeriod.serviceContractID, Equal<Required<FSContractPeriod.serviceContractID>>>,
																		OrderBy<Desc<FSContractPeriod.endPeriodDate>>>
																	.SelectWindowed(this, 0, 1, fsServiceContractRow.ServiceContractID);

						if (fsCurrentContractPeriodRow != null
							&& fsCurrentContractPeriodRow.Status != ID.Status_ContractPeriod.INACTIVE)
						{
							activatePeriod = fsCurrentContractPeriodRow.Status != ID.Status_ContractPeriod.ACTIVE;

							List<FSContractPeriodDet> details = PXSelect<FSContractPeriodDet,
																			Where<FSContractPeriodDet.contractPeriodID, Equal<Required<FSContractPeriod.contractPeriodID>>>>
																		.Select(this, fsCurrentContractPeriodRow.ContractPeriodID).RowCast<FSContractPeriodDet>().ToList();

							GenerateNewContractPeriod(fsCurrentContractPeriodRow, details);
						}
					}
				}

				if (IsDirty == true)
				{
					Save.Press();
					this.IsRenewContract = false;
				}
					

				if (SetupRecord.Current != null
					&& SetupRecord.Current.EnableContractPeriodWhenInvoice == true
					&& activatePeriod == true)
					ActivateCurrentPeriod();
			}
			finally
			{
				this.IsRenewContract = false;
			}

			return adapter.Get();
		}
		#endregion

		#region CancelContract
		public PXAction<FSServiceContract> cancelContract;
        [PXButton(Category = CommonActionCategories.DisplayNames.Processing)]
        [PXUIField(DisplayName = "Cancel")]
        public virtual IEnumerable CancelContract(PXAdapter adapter)
        {
            List<FSServiceContract> fsServiceContracts = adapter.Get<FSServiceContract>().ToList();
            string errorMessage = "";

            foreach (FSServiceContract fsServiceContractRow in fsServiceContracts)
            {
                using (PXTransactionScope ts = new PXTransactionScope())
                {
                    if (CheckNewContractStatus(this,
                                               fsServiceContractRow,
                                               ID.Status_ServiceContract.CANCELED,
                                               ref errorMessage) == false)
                    {
                        throw new PXException(errorMessage);
                    }

                    if (skipStatusSmartPanels == true || TerminateContractFilter.AskExt() == WebDialogResult.OK)
                    {
                        if (CheckDatesApplyOrScheduleStatusChange(this.ServiceContractRecords.Cache, fsServiceContractRow, this.Accessinfo.BusinessDate, TerminateContractFilter.Current.CancelationDate))
                        {
                            ApplyOrScheduleStatusChange(this, this.ServiceContractRecords.Cache, fsServiceContractRow, this.Accessinfo.BusinessDate, TerminateContractFilter.Current.CancelationDate, ID.Status_ServiceContract.CANCELED);
                            UpdateSchedulesByCancelContract(TerminateContractFilter.Current.CancelationDate);

                            if (fsServiceContractRow.NextBillingInvoiceDate > TerminateContractFilter.Current.CancelationDate)
                            {
                                fsServiceContractRow.NextBillingInvoiceDate = TerminateContractFilter.Current.CancelationDate;
                            }

                            this.ContractPeriodFilter.SetValueExt<FSContractPeriodFilter.actions>(this.ContractPeriodFilter.Current, ID.ContractPeriod_Actions.SEARCH_BILLING_PERIOD);
                            //  On the FSContractPeriod of latest entry with status Invoiced or Pending for Invoice, if EndPeriodDate is mayor than Cancellation date, EndPeriodDate as Cancellation Date
                            //  On the FSContractPeriod table of latest entry with status Inactive, change the column Status to P = Pending for Invoice
                            ForceUpdateCacheAndSave(ServiceContractRecords.Cache, fsServiceContractRow);
                        }
                    }

                    ts.Complete();
                }
            }

            return fsServiceContracts;
        }
        #endregion

        #region AddSchedule
        public PXDBAction<FSServiceContract> addSchedule;
        [PXButton]
        [PXUIField(DisplayName = "Add Schedule")]
        public virtual void AddSchedule()
        {
        }
        #endregion

        #region ViewServiceOrderHistory
        public PXAction<FSServiceContract> viewServiceOrderHistory;
        [PXButton(Category =  FSToolbarCategory.CategoryNames.Inquiries)]
        [PXUIField(DisplayName = "Service Order History", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        protected virtual void ViewServiceOrderHistory()
        {
            FSServiceContract fsServiceContractRow = ServiceContractRecords.Current;

            if (fsServiceContractRow != null)
            {
                Dictionary<string, string> parameters = GetBaseParameters(fsServiceContractRow, true, true);
                parameters["ContractID"] = fsServiceContractRow.RefNbr;
                throw new PXRedirectToGIWithParametersRequiredException(new Guid(TX.GenericInquiries_GUID.SERVICE_ORDER_HISTORY), parameters);
            }
        }
        #endregion

        #region ViewAppointmentHistory
        public PXAction<FSServiceContract> viewAppointmentHistory;
        [PXButton(Category =  FSToolbarCategory.CategoryNames.Inquiries)]
        [PXUIField(DisplayName = "Appointment History", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        protected virtual void ViewAppointmentHistory()
        {
            FSServiceContract fsServiceContractRow = ServiceContractRecords.Current;

            if (fsServiceContractRow != null)
            {
                var graphAppointmentInq = PXGraph.CreateInstance<AppointmentInq>();

                AppointmentInq.AppointmentInqFilter appointmentInqFilterRow = new AppointmentInq.AppointmentInqFilter();

                appointmentInqFilterRow.BranchID = fsServiceContractRow.BranchID;
                appointmentInqFilterRow.BranchLocationID = fsServiceContractRow.BranchLocationID;
                appointmentInqFilterRow.CustomerID = fsServiceContractRow.CustomerID;
                appointmentInqFilterRow.CustomerLocationID = fsServiceContractRow.CustomerLocationID;
                appointmentInqFilterRow.ServiceContractID = fsServiceContractRow.ServiceContractID;

                graphAppointmentInq.Filter.Current = graphAppointmentInq.Filter.Insert(appointmentInqFilterRow);

                throw new PXRedirectRequiredException(graphAppointmentInq, null) { Mode = PXBaseRedirectException.WindowMode.Same };
            }
        }
        #endregion

        #region ViewContractScheduleDetails
        public PXAction<FSServiceContract> viewContractScheduleDetails;
        [PXButton(Category =  FSToolbarCategory.CategoryNames.Inquiries)]
        [PXUIField(DisplayName = "Contract Schedule Details", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        protected virtual void ViewContractScheduleDetails()
        {
            FSServiceContract fsServiceContractRow = ServiceContractRecords.Current;

            if (fsServiceContractRow != null)
            {
                Dictionary<string, string> parameters = GetBaseParameters(fsServiceContractRow, false, false);
                throw new PXRedirectToGIWithParametersRequiredException(new Guid(TX.GenericInquiries_GUID.CONTRACT_SCHEDULE_DETAILS_SUMMARY), parameters);
            }
        }
        #endregion

        #region ViewCustomerContracts
        public PXAction<FSServiceContract> viewCustomerContracts;
        [PXButton(Category =  FSToolbarCategory.CategoryNames.Inquiries)]
        [PXUIField(DisplayName = "Customer Contracts", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        protected virtual void ViewCustomerContracts()
        {
            FSServiceContract fsServiceContractRow = ServiceContractRecords.Current;

            if (fsServiceContractRow != null)
            {
                Dictionary<string, string> parameters = GetBaseParameters(fsServiceContractRow, false, false);
                throw new PXRedirectToGIWithParametersRequiredException(new Guid(TX.GenericInquiries_GUID.CONTRACT_SUMMARY), parameters);
            }
        }
        #endregion

        #region ViewCustomerContractSchedules
        public PXAction<FSServiceContract> viewCustomerContractSchedules;
        [PXButton(Category =  FSToolbarCategory.CategoryNames.Inquiries)]
        [PXUIField(DisplayName = "Customer Contract Schedules", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        protected virtual void ViewCustomerContractSchedules()
        {
            FSServiceContract fsServiceContractRow = ServiceContractRecords.Current;

            if (fsServiceContractRow != null)
            {
                Dictionary<string, string> parameters = GetBaseParameters(fsServiceContractRow, false, false);
                throw new PXRedirectToGIWithParametersRequiredException(new Guid(TX.GenericInquiries_GUID.CONTRACT_SCHEDULE_SUMMARY), parameters);
            }
        }
        #endregion

        #region ActivatePeriod
        public PXAction<FSServiceContract> activatePeriod;
        [PXButton]
        [PXUIField(DisplayName = "Activate Period", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual void ActivatePeriod()
        {
            ActivateCurrentPeriod();
        }
		#endregion

		#region ForecastPrintQuote
		public PXAction<FSServiceContract> forecastPrintQuote;
		[PXButton(Category = CommonActionCategories.DisplayNames.PrintingAndEmailing)]
		[PXUIField(DisplayName = "Forecast & Print Quote")]
		public virtual void ForecastPrintQuote()
		{
			if(ForecastFilter.View.Answer == WebDialogResult.None)
				this.Save.Press();

			bool requiredFieldsFilled = ForecastFilter.AskExtFullyValid((graph, view) => { SetForecastFilterDefaults(); }, DialogAnswerType.Positive, false);
			if (ForecastFilter.View.Answer == WebDialogResult.OK && requiredFieldsFilled == true)
			{
				this.IsForcastProcess = true;

				if (this.ServiceContractRecords.Current == null)
					return;

				FSServiceContract contract = this.ServiceContractRecords.Current;

				Dictionary<string, string> parameters = new Dictionary<string, string>();
				PXReportRequiredException ex = null;

				PXLongOperation.StartOperation(
				this,
				delegate ()
				{
					parameters = new Dictionary<string, string>();
					parameters[nameof(FSServiceContract) + "." + nameof(FSServiceContract.RefNbr)] = contract.RefNbr;

					try
					{
						this.ContractForecastProc(contract, ForecastFilter.Current?.StartDate, ForecastFilter.Current?.EndDate);
					} 
					finally
					{
						this.IsForcastProcess = false;
					}

					ex = PXReportRequiredException.CombineReport(ex, ID.ReportID.CONTRACT_FORECAST, parameters);
					ex.Mode = PXBaseRedirectException.WindowMode.New;

					if (ex != null)
					{
						throw ex;
					}
				});
			}
		}
		#endregion

		#region EmailQuoteContract
		public PXAction<FSServiceContract> emailQuoteContract;
		[PXButton(Category = CommonActionCategories.DisplayNames.PrintingAndEmailing)]
		[PXUIField(DisplayName = "Email Quote")]
		public virtual IEnumerable EmailQuoteContract(PXAdapter adapter)
		{
			return Notification(adapter, FSMailing.EMAIL_SERVICE_CONTRACT_QUOTE);
		}
		#endregion

		#region CopyContract
		public PXAction<FSServiceContract> copyContract;
        [PXButton(Category = CommonActionCategories.DisplayNames.Other)]
        [PXUIField(DisplayName = "Copy")]
        public virtual IEnumerable CopyContract(PXAdapter adapter)
        {
            List<FSServiceContract> list = adapter.Get<FSServiceContract>().ToList();

            WebDialogResult dialogResult = CopyContractFilter.AskExt();
            if ((dialogResult == WebDialogResult.OK || (this.IsContractBasedAPI && dialogResult == WebDialogResult.Yes)) && CopyContractFilter.Current.StartDate != null)
            {
                this.Save.Press();

				PXLongOperation.StartOperation(
				this,
				delegate ()
				{
					FSServiceContract contract = PXCache<FSServiceContract>.CreateCopy(ServiceContractRecords.Current);

					IsCopyContract = true;

					try
					{
						this.CopyContractProc(contract, CopyContractFilter.Current?.StartDate);
					}
					finally
					{
						IsCopyContract = false;
					}
				});
            }

            return list;
        }
        #endregion

        #region OpenPostBatch
        public ViewPostBatch<FSServiceContract> openPostBatch;
		#endregion

		#region Notification
		public PXAction<FSServiceContract> notification;
		[PXUIField(DisplayName = "Notifications", Visible = false)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.DataEntryF)]
		protected virtual IEnumerable Notification(PXAdapter adapter, [PXString] string notificationCD)
		{
			bool massProcess = adapter.MassProcess;
			var orders = adapter.Get<FSServiceContract>().ToArray();

			if (orders.Length > 0)
			{
				PXLongOperation.StartOperation(this, () =>
				{
					Lazy<ServiceContractEntry> scEntry = null;
					Lazy<RouteServiceContractEntry> rcEntry = null;

					if (orders[0].RecordType == ID.RecordType_ServiceContract.SERVICE_CONTRACT)
					{
						scEntry = Lazy.By(() => PXGraph.CreateInstance<ServiceContractEntry>());
					}
					else if (orders[0].RecordType == ID.RecordType_ServiceContract.ROUTE_SERVICE_CONTRACT)
					{
						rcEntry = Lazy.By(() => PXGraph.CreateInstance<RouteServiceContractEntry>());
					}

					bool anyfailed = false;

					foreach (FSServiceContract order in orders)
					{
						if (massProcess) PXProcessing<FSServiceContract>.SetCurrentItem(order);

						try
						{
							var parameters = new Dictionary<string, string>
							{
								[nameof(FSServiceContract) + "." + nameof(FSServiceContract.RefNbr)] = order.RefNbr,
							};

							order.EmailNotificationCD = notificationCD;

							if (scEntry != null && orders[0].RecordType == ID.RecordType_ServiceContract.SERVICE_CONTRACT)
							{
								scEntry.Value.ServiceContractRecords.Current = order;
								scEntry.Value.Activity.SendNotification("Contract", notificationCD, order.BranchID, parameters);
								scEntry.Value.ServiceContractRecords.Update(order);
							}
							else if (rcEntry != null && orders[0].RecordType == ID.RecordType_ServiceContract.ROUTE_SERVICE_CONTRACT)
							{
								rcEntry.Value.ServiceContractRecords.Current = order;
								rcEntry.Value.Activity.SendNotification("Contract", notificationCD, order.BranchID, parameters);
								rcEntry.Value.ServiceContractRecords.Update(order);
							}

							if (massProcess) PXProcessing<FSServiceContract>.SetProcessed();
						}
						catch (Exception exception) when (massProcess)
						{
							PXProcessing<FSServiceContract>.SetError(exception);
							anyfailed = true;
						}
					}

					if (scEntry != null && scEntry.Value.ServiceContractRecords.Cache.IsDirty)
						scEntry.Value.Save.Press();

					if (rcEntry != null && rcEntry.Value.ServiceContractRecords.Cache.IsDirty)
						rcEntry.Value.Save.Press();

					if (anyfailed)
						throw new PXOperationCompletedWithErrorException(ErrorMessages.SeveralItemsFailed);
				});

			}

			return orders;
		}
		#endregion
		#endregion

		#region Virtual Functions

		/// <summary>
		/// Enable or Disable the ServiceContract fields.
		/// </summary>
		public virtual void EnableDisable_Document(PXCache cache, FSServiceContract fsServiceContractRow)
        {
            bool enableInsertUpdate = fsServiceContractRow.isEditable() || IsForcastProcess == true;
            bool enableDelete = CanDeleteServiceContract(fsServiceContractRow);

            this.ServiceContractRecords.Cache.AllowInsert = true;
            this.ServiceContractRecords.Cache.AllowUpdate = enableInsertUpdate;
            this.ServiceContractRecords.Cache.AllowDelete = enableDelete;

            this.ServiceContractSelected.Cache.AllowInsert = true;
            this.ServiceContractSelected.Cache.AllowUpdate = enableInsertUpdate;
            this.ServiceContractSelected.Cache.AllowDelete = enableDelete;

            this.ContractPeriodRecords.Cache.AllowInsert = this.ContractPeriodRecords.Cache.AllowUpdate = enableInsertUpdate;
            this.ContractPeriodRecords.Cache.AllowDelete = enableDelete;

            this.ContractPeriodDetRecords.Cache.AllowInsert = this.ContractPeriodDetRecords.Cache.AllowUpdate = enableInsertUpdate;
            this.ContractPeriodDetRecords.Cache.AllowDelete = enableDelete;

            this.SalesPriceLines.Cache.AllowInsert = this.SalesPriceLines.Cache.AllowUpdate = enableInsertUpdate;
            this.SalesPriceLines.Cache.AllowDelete = enableDelete;

            this.ContractHistoryItems.Cache.AllowInsert = this.ContractHistoryItems.Cache.AllowUpdate = enableInsertUpdate;
            this.ContractHistoryItems.Cache.AllowDelete = enableDelete;

            PXUIFieldAttribute.SetEnabled<FSContractPeriodFilter.actions>(this.ContractPeriodFilter.Cache, this.ContractPeriodFilter.Current, enableInsertUpdate);
            PXUIFieldAttribute.SetEnabled<FSContractPeriodFilter.postDocRefNbr>(this.ContractPeriodFilter.Cache, this.ContractPeriodFilter.Current, enableInsertUpdate);
            PXUIFieldAttribute.SetEnabled<FSContractPeriodFilter.standardizedBillingTotal>(this.ContractPeriodFilter.Cache, this.ContractPeriodFilter.Current, enableInsertUpdate);
            PXUIFieldAttribute.SetEnabled<FSContractPeriodFilter.contractPeriodID>(this.ContractPeriodFilter.Cache, this.ContractPeriodFilter.Current, enableInsertUpdate);

            this.addSchedule.SetEnabled(enableInsertUpdate);
            this.activatePeriod.SetEnabled(EnableDisableActivatePeriodButton(fsServiceContractRow, ContractPeriodRecords.Current));

			if (enableInsertUpdate)
            {
                bool enableStartDate = fsServiceContractRow.Status == ID.Status_ServiceContract.DRAFT;
                bool enableExpirationType = fsServiceContractRow.Status == ID.Status_ServiceContract.DRAFT;
                bool enableExpirationDate = fsServiceContractRow.Status == ID.Status_ServiceContract.DRAFT;
                bool enableBillingType = fsServiceContractRow.Status == ID.Status_ServiceContract.DRAFT;
                bool enableBillingPeriod = fsServiceContractRow.Status == ID.Status_ServiceContract.DRAFT;

                PXUIFieldAttribute.SetEnabled<FSServiceContract.billingType>(cache, fsServiceContractRow, enableBillingType);
                PXUIFieldAttribute.SetEnabled<FSServiceContract.startDate>(cache, fsServiceContractRow, enableStartDate);
                PXUIFieldAttribute.SetEnabled<FSServiceContract.expirationType>(cache, fsServiceContractRow, enableExpirationType);
                PXUIFieldAttribute.SetEnabled<FSServiceContract.billingPeriod>(cache, fsServiceContractRow, enableBillingPeriod);
                PXDefaultAttribute.SetPersistingCheck<FSServiceContract.startDate>(cache,
                                                                                   fsServiceContractRow,
                                                                                   enableStartDate ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
                PXUIFieldAttribute.SetEnabled<FSServiceContract.endDate>(cache, fsServiceContractRow, enableExpirationDate || IsCopyPasteContext);
                PXDefaultAttribute.SetPersistingCheck<FSServiceContract.endDate>(cache,
                                                                                 fsServiceContractRow,
                                                                                 fsServiceContractRow.ExpirationType == ID.Contract_ExpirationType.EXPIRING ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);

                bool visibleUsageBillingCycle = SetupRecord.Current != null
                                                    && SetupRecord.Current.CustomerMultipleBillingOptions == false
                                                        && fsServiceContractRow.BillingType == FSServiceContract.billingType.Values.StandardizedBillings;

                PXUIFieldAttribute.SetVisible<FSServiceContract.usageBillingCycleID>(cache, fsServiceContractRow, visibleUsageBillingCycle);
            }
        }

        /// <summary>
        /// Enables/Disables the actions defined for ServiceContract
        /// It's called by RowSelected event of FSServiceContract.
        /// </summary>
        public virtual void EnableDisable_ActionButtons(PXGraph graph, PXCache cache, FSServiceContract fsServiceContractRow)
        {
            if (cache.GetStatus(fsServiceContractRow) == PXEntryStatus.Inserted)
            {
                activateContract.SetEnabled(false);
                suspendContract.SetEnabled(false);
                cancelContract.SetEnabled(false);
                addSchedule.SetEnabled(false);
                viewContractScheduleDetails.SetEnabled(false);
                viewServiceOrderHistory.SetEnabled(false);
                viewAppointmentHistory.SetEnabled(false);
                viewCustomerContracts.SetEnabled(false);
                viewCustomerContractSchedules.SetEnabled(false);
                activateContract.SetEnabled(false);
				renewContract.SetEnabled(false);
				emailQuoteContract.SetEnabled(false);
				forecastPrintQuote.SetEnabled(false);
			}
            else
            {
                string dummyErrorMessage = string.Empty;

                bool canActivate = CheckNewContractStatus(this, fsServiceContractRow, ID.Status_ServiceContract.ACTIVE, ref dummyErrorMessage);
                canActivate = (canActivate &&
                                ((fsServiceContractRow.BillingType == FSServiceContract.billingType.Values.PerformedBillings)
                                    || ((fsServiceContractRow.BillingType == FSServiceContract.billingType.Values.StandardizedBillings
                                            || fsServiceContractRow.IsFixedRateContract == true)
                                        && this.ContractPeriodDetRecords.Select().Count > 0)))
							|| IsImport;

                bool canSuspend = CheckNewContractStatus(this, fsServiceContractRow, ID.Status_ServiceContract.SUSPENDED, ref dummyErrorMessage);
                bool canCancel = CheckNewContractStatus(this, fsServiceContractRow, ID.Status_ServiceContract.CANCELED, ref dummyErrorMessage);
                bool entryStatusIsNotInserted = cache.GetStatus(fsServiceContractRow) != PXEntryStatus.Inserted;

				forecastPrintQuote.SetEnabled(fsServiceContractRow.Status != ID.Status_ServiceContract.EXPIRED && fsServiceContractRow.Status != ID.Status_ServiceContract.CANCELED);
				emailQuoteContract.SetEnabled(fsServiceContractRow.HasForecast == true && fsServiceContractRow.Status != ID.Status_ServiceContract.EXPIRED && fsServiceContractRow.Status != ID.Status_ServiceContract.CANCELED);

				copyContract.SetEnabled(true);
				activateContract.SetEnabled(canActivate);
                suspendContract.SetEnabled(canSuspend);
                cancelContract.SetEnabled(canCancel);
                addSchedule.SetEnabled(entryStatusIsNotInserted);
                viewContractScheduleDetails.SetEnabled(entryStatusIsNotInserted);
                viewServiceOrderHistory.SetEnabled(entryStatusIsNotInserted);
                viewAppointmentHistory.SetEnabled(entryStatusIsNotInserted);
                viewCustomerContracts.SetEnabled(entryStatusIsNotInserted);
                viewCustomerContractSchedules.SetEnabled(entryStatusIsNotInserted);
                activatePeriod.SetEnabled(EnableDisableActivatePeriodButton(fsServiceContractRow, ContractPeriodRecords.Current));
				renewContract.SetEnabled(fsServiceContractRow.Status == ID.Status_ServiceContract.ACTIVE
										&& fsServiceContractRow.ExpirationType == ID.Contract_ExpirationType.RENEWABLE);
			}
        }

        /// <summary>
        /// Validates startDate and endDate have correct values.
        /// </summary>
        public virtual void ValidateDates(PXCache cache, FSServiceContract fsServiceContractRow, PXResultset<FSContractSchedule> contractRows)
        {
            if (!fsServiceContractRow.StartDate.HasValue)
            {
                return;
            }

            if (contractRows.AsEnumerable().Where(y => ((FSContractSchedule)y).StartDate < fsServiceContractRow.StartDate
																	&& ((FSContractSchedule)y).StartDate != (DateTime?)cache.GetValueOriginal<FSServiceContract.startDate>(fsServiceContractRow)).Count() > 0)
            {
                cache.RaiseExceptionHandling
                   <FSServiceContract.startDate>(fsServiceContractRow,
                                                 fsServiceContractRow.StartDate,
                                                 new PXSetPropertyException(TX.Error.CONTRACT_START_DATE_GREATER_THAN_SCHEDULE_START_DATE,
                                                                            PXErrorLevel.Error));
            }

            if (contractRows.AsEnumerable().Where(y => ((FSContractSchedule)y).EndDate > fsServiceContractRow.EndDate
																&& ((FSContractSchedule)y).EndDate != (DateTime?)cache.GetValueOriginal<FSServiceContract.endDate>(fsServiceContractRow)).Count() > 0)
            {
                cache.RaiseExceptionHandling
                   <FSServiceContract.endDate>(fsServiceContractRow,
                                               fsServiceContractRow.EndDate,
                                               new PXSetPropertyException(TX.Error.CONTRACT_END_DATE_LESSER_THAN_SCHEDULE_END_DATE,
                                                                          PXErrorLevel.Error));
            }

            if (fsServiceContractRow.EndDate.HasValue 
                    && fsServiceContractRow.StartDate.Value.CompareTo(fsServiceContractRow.EndDate.Value) > 0)
            {
                cache.RaiseExceptionHandling<FSServiceContract.startDate>(fsServiceContractRow,
                                                                          fsServiceContractRow.StartDate,
                                                                          new PXSetPropertyException(TX.Error.END_DATE_LESSER_THAN_START_DATE, PXErrorLevel.RowError));

                cache.RaiseExceptionHandling<FSServiceContract.endDate>(fsServiceContractRow,
                                                                        fsServiceContractRow.EndDate,
                                                                        new PXSetPropertyException(TX.Error.END_DATE_LESSER_THAN_START_DATE, PXErrorLevel.RowError));
            }

            if (fsServiceContractRow.ExpirationType == ID.Contract_ExpirationType.EXPIRING
                && fsServiceContractRow.UpcomingStatus != null
                && fsServiceContractRow.UpcomingStatus != ID.Status_ServiceContract.EXPIRED
                && fsServiceContractRow.EndDate.Value.Date <= fsServiceContractRow.StatusEffectiveUntilDate.Value.Date)
            {
                cache.RaiseExceptionHandling<FSServiceContract.endDate>(fsServiceContractRow,
                                                                        fsServiceContractRow.EndDate,
                                                                        new PXSetPropertyException(TX.Error.EXPIRATION_DATE_LOWER_UPCOMING_STATUS, PXErrorLevel.RowError));
            }


			if (fsServiceContractRow.UpcomingStatus == ID.Status_ServiceContract.EXPIRED
				&& fsServiceContractRow.EndDate.HasValue == true
				&& fsServiceContractRow.EndDate.Value.Date <= this.Accessinfo.BusinessDate.Value.Date
				&& this.IsRenewContract == false
				&& this.IsForcastProcess == false)
            {
                cache.RaiseExceptionHandling<FSServiceContract.endDate>(fsServiceContractRow,
                                                                        fsServiceContractRow.EndDate,
                                                                        new PXSetPropertyException(TX.Error.EXPIRATION_DATE_LOWER_BUSINESS_DATE, PXErrorLevel.RowError));
            }
        }

        /// <summary>
        /// Sets the price configured in Price List for a Service when the <c>SourcePrice</c> is modified.
        /// </summary>
        public virtual decimal? GetSalesPrice(PXCache cache, FSSalesPrice fsSalesPriceRow)
        {
            decimal? serviceSalesPrice = null;
            FSServiceContract fsServiceContractRow = ServiceContractRecords.Current;

            SalesPriceSet salesPriceSet = FSPriceManagement.CalculateSalesPriceWithCustomerContract(cache,
                                                                                                    null,
                                                                                                    null,
                                                                                                    null,
                                                                                                    fsServiceContractRow.CustomerID,
                                                                                                    fsServiceContractRow.CustomerLocationID,
                                                                                                    null,
                                                                                                    fsSalesPriceRow.InventoryID,
                                                                                                    null,
                                                                                                    0m,
                                                                                                    fsSalesPriceRow.UOM,
                                                                                                    (DateTime)(fsServiceContractRow.StartDate ?? cache.Graph.Accessinfo.BusinessDate),
                                                                                                    fsSalesPriceRow.Mem_UnitPrice,
                                                                                                    alwaysFromBaseCurrency: true,
                                                                                                    currencyInfo: null,
                                                                                                    catchSalesPriceException: true);

            switch (salesPriceSet.ErrorCode)
            {
                case ID.PriceErrorCode.OK:
                    serviceSalesPrice = salesPriceSet.Price;
                    break;

                case ID.PriceErrorCode.UOM_INCONSISTENCY:
                    InventoryItem inventoryItemRow = SharedFunctions.GetInventoryItemRow(cache.Graph, fsSalesPriceRow.InventoryID);
                    cache.RaiseExceptionHandling<FSSalesPrice.uOM>(fsSalesPriceRow, 
                                                                   fsSalesPriceRow.UOM,
                                                                   new PXSetPropertyException(PXMessages.LocalizeFormatNoPrefix(TX.Error.INVENTORY_ITEM_UOM_INCONSISTENCY, inventoryItemRow.InventoryCD), PXErrorLevel.Error));
                    break;

                default:
                    throw new PXException(salesPriceSet.ErrorCode);
            }

            return serviceSalesPrice;
        }

        /// <summary>
        /// Updates all prices of <c>FSSalesPrice</c> lines.
        /// </summary>
        /// <param name="cache">PXCache instance.</param>
        /// <param name="fsServiceContractRow">FSServiceContract current row.</param>
        public virtual void UpdateSalesPrices(PXCache cache, FSServiceContract fsServiceContractRow)
        {
            foreach (FSSalesPrice fsSalesPriceRow in SalesPriceLines.Select())
            {
                fsSalesPriceRow.Mem_UnitPrice = GetSalesPrice(SalesPriceLines.Cache, fsSalesPriceRow) ?? 0.0m;
                PXUIFieldAttribute.SetEnabled<FSSalesPrice.mem_UnitPrice>(SalesPriceLines.Cache, fsSalesPriceRow, ServiceContractRecords.Current.SourcePrice == ID.SourcePrice.CONTRACT);
            }
        }

        /// <summary>
        /// Verifies the cache of the views for FSSalesPrice.
        /// </summary>
        public virtual void SetUnitPriceForSalesPricesRows(FSServiceContract fsServiceContractRow)
        {
            if (SalesPriceLines.Cache.IsDirty == true)
            {
                foreach (FSSalesPrice fsSalesPriceRow in SalesPriceLines.Select())
                {
                    if (fsServiceContractRow.SourcePrice == ID.SourcePrice.PRICE_LIST)
                    {
                        fsSalesPriceRow.UnitPrice = null;
                    }
                    else
                    {
                        fsSalesPriceRow.UnitPrice = fsSalesPriceRow.Mem_UnitPrice ?? 0.0m;
                    }

                    SalesPriceLines.Cache.SetStatus(fsSalesPriceRow, PXEntryStatus.Updated);
                }
            }
        }

        public virtual void SetVisibleActivatePeriodButton(PXCache cache, FSServiceContract fsServiceContractRow)
        {
            if (fsServiceContractRow == null)
            {
                return;
            }

            bool showAPB = fsServiceContractRow.BillingType != FSServiceContract.billingType.Values.PerformedBillings
                            && ContractPeriodFilter.Current.Actions == ID.ContractPeriod_Actions.MODIFY_UPCOMING_BILLING_PERIOD;

            activatePeriod.SetVisible(showAPB);
        }

        public virtual void SetVisibleContractBillingSettings(PXCache cache, FSServiceContract fsServiceContractRow)
        {
            if(fsServiceContractRow == null)
            {
                return;
            }

            bool showAPFB = fsServiceContractRow.BillingType == FSServiceContract.billingType.Values.PerformedBillings;

            PXUIFieldAttribute.SetVisible<FSServiceContract.billingPeriod>(cache, fsServiceContractRow, showAPFB == false);
            PXUIFieldAttribute.SetVisible<FSServiceContract.lastBillingInvoiceDate>(cache, fsServiceContractRow, showAPFB == false);
            PXUIFieldAttribute.SetVisible<FSServiceContract.nextBillingInvoiceDate>(cache, fsServiceContractRow, showAPFB == false);
            PXUIFieldAttribute.SetVisible<FSServiceContract.sourcePrice>(cache, fsServiceContractRow, showAPFB == true);
        }

        public virtual void SetUpcommingStatus(FSServiceContract fsServiceContractRow)
        {
            if (fsServiceContractRow == null)
            {
                return;
            }

            if (fsServiceContractRow.ExpirationType == ID.Contract_ExpirationType.UNLIMITED)
            {
                if (fsServiceContractRow.UpcomingStatus == ID.Status_ServiceContract.EXPIRED)
                {
                    fsServiceContractRow.UpcomingStatus = null;
                }

            }else
            {
                if(fsServiceContractRow.UpcomingStatus == null)
                {
                    fsServiceContractRow.UpcomingStatus = ID.Status_ServiceContract.EXPIRED;
                }
            }
        }

        public virtual void SetUsageBillingCycle(FSServiceContract fsServiceContractRow)
        {
            if (SetupRecord.Current != null
                    && SetupRecord.Current.CustomerMultipleBillingOptions == false
                          && fsServiceContractRow.BillingType == FSServiceContract.billingType.Values.StandardizedBillings)
            {
                if (fsServiceContractRow.BillTo == ID.Contract_BillTo.CUSTOMERACCT)
                {
                    Customer customerRow = PXSelect<Customer,
                                           Where<
                                               Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>
                                           .Select(this, fsServiceContractRow.CustomerID);

                    if (customerRow != null)
                    {
                        FSxCustomer fsxCustomerRow = PXCache<Customer>.GetExtension<FSxCustomer>(customerRow);
                        fsServiceContractRow.UsageBillingCycleID = fsxCustomerRow.BillingCycleID;
                    }
                }
            }
        }

        public virtual void SetBillInfo(PXCache cache, FSServiceContract fsServiceContractRow)
        {
            bool isCustomerAcct = fsServiceContractRow.BillTo == ID.Contract_BillTo.CUSTOMERACCT;

            PXUIFieldAttribute.SetEnabled<FSServiceContract.billCustomerID>(cache, fsServiceContractRow, !isCustomerAcct);
            PXUIFieldAttribute.SetEnabled<FSServiceContract.billLocationID>(cache, fsServiceContractRow, !isCustomerAcct);
            PXDefaultAttribute.SetPersistingCheck<FSServiceContract.billCustomerID>(cache, fsServiceContractRow, isCustomerAcct ? PXPersistingCheck.Nothing : PXPersistingCheck.NullOrBlank);
            PXDefaultAttribute.SetPersistingCheck<FSServiceContract.billLocationID>(cache, fsServiceContractRow, isCustomerAcct ? PXPersistingCheck.Nothing : PXPersistingCheck.NullOrBlank);
        }

        public virtual void SetBillTo(FSServiceContract fSServiceContractRow)
        {
            if (fSServiceContractRow.BillCustomerID == fSServiceContractRow.CustomerID)
            {
                fSServiceContractRow.BillTo = ID.Contract_BillTo.CUSTOMERACCT;
            }
            else
            {
                fSServiceContractRow.BillTo = ID.Contract_BillTo.SPECIFICACCT;
            }
        }

        public virtual Dictionary<string, string> GetBaseParameters(FSServiceContract fsServiceContractRow, bool loadBranch, bool loadBranchLocation)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            if (loadBranch == true)
            {
                Branch branchRow = PXSelect<Branch,
                                   Where<
                                       Branch.branchID, Equal<Required<Branch.branchID>>>>
                                   .Select(this, Accessinfo.BranchID);

                if (branchRow != null)
                {
                    parameters["BranchID"] = branchRow.BranchCD;
                }
            }

            if (loadBranchLocation == true)
            {
                FSBranchLocation fsBranchLocationRow = PXSelect<FSBranchLocation,
                                                       Where<
                                                           FSBranchLocation.branchID, Equal<Required<Branch.branchID>>,
                                                       And<
                                                           FSBranchLocation.branchLocationID, Equal<Required<FSBranchLocation.branchLocationID>>>>>
                                                       .Select(this, Accessinfo.BranchID, fsServiceContractRow.BranchLocationID);

                if (fsBranchLocationRow != null)
                {
                    parameters["BranchLocationID"] = fsBranchLocationRow.BranchLocationCD;
                }
            }

            Customer customerRow = PXSelect<Customer,
                                   Where<
                                       Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>
                                   .Select(this, fsServiceContractRow.CustomerID);

            Location locationRow = PXSelect<Location,
                                   Where<
                                       Location.locationID, Equal<Required<Location.locationID>>>>
                                   .Select(this, fsServiceContractRow.CustomerLocationID);

            if (customerRow != null && locationRow != null)
            {
                parameters["CustomerID"] = customerRow.AcctCD;
                parameters["CustomerLocationID"] = locationRow.LocationCD;
            }

            parameters["ServiceContractRefNbr"] = fsServiceContractRow.RefNbr;

            return parameters;
        }

        public virtual void SetDefaultBillingRule(PXCache cache, FSContractPeriodDet fsContractPeriodDetRow)
        {
            string billingRule = ID.ContractPeriod_BillingRule.TIME;

            if (fsContractPeriodDetRow.LineType == ID.LineType_ContractPeriod.NONSTOCKITEM)
            {
                billingRule = ID.BillingRule.FLAT_RATE;
            }
            else if (fsContractPeriodDetRow.LineType == ID.LineType_ContractPeriod.SERVICE)
            {
                InventoryItem inventoryItemRow = SharedFunctions.GetInventoryItemRow(this, fsContractPeriodDetRow.InventoryID);

                if (inventoryItemRow != null)
                {
                    FSxService fsxServiceRow = PXCache<InventoryItem>.GetExtension<FSxService>(inventoryItemRow);

                    if (fsxServiceRow != null)
                    {
                        billingRule = fsxServiceRow.BillingRule;

                        if (fsxServiceRow.BillingRule == ID.BillingRule.NONE)
                        {
                            billingRule = ID.ContractPeriod_BillingRule.TIME;
                        }
                    }
                }
            }

            cache.SetValueExt<FSContractPeriodDet.billingRule>(fsContractPeriodDetRow, billingRule);
        }

        public virtual void SetDefaultQtyTime(PXCache cache, FSContractPeriodDet fsContractPeriodDetRow)
        {
            if (fsContractPeriodDetRow.BillingRule == ID.ContractPeriod_BillingRule.TIME)
            {
                cache.SetValueExt<FSContractPeriodDet.time>(fsContractPeriodDetRow, 60);
                cache.SetValueExt<FSContractPeriodDet.qty>(fsContractPeriodDetRow, 0m);
            }
            else if (fsContractPeriodDetRow.BillingRule == ID.ContractPeriod_BillingRule.FLAT_RATE)
            {
                cache.SetValueExt<FSContractPeriodDet.time>(fsContractPeriodDetRow, 0);
                cache.SetValueExt<FSContractPeriodDet.qty>(fsContractPeriodDetRow, 1.0m);
            }
        }

        public static decimal? GetSalesPriceItemInfo(PXCache cacheDetail,
                                                     FSServiceContract fsServiceContractRow,
                                                     FSContractPeriodDet fsContractPeriodDet)
        {
            InventoryItem inventoryItemRow = SharedFunctions.GetInventoryItemRow(cacheDetail.Graph, fsContractPeriodDet?.InventoryID);

            if (inventoryItemRow == null)
            {
                return null;
            }

            SalesPriceSet salesPriceSet = FSPriceManagement.CalculateSalesPriceWithCustomerContract(cacheDetail,
                                                                                                    fsServiceContractRow.ServiceContractID,
                                                                                                    null,
                                                                                                    null,
                                                                                                    fsServiceContractRow.CustomerID,
                                                                                                    fsServiceContractRow.CustomerLocationID,
                                                                                                    null,
                                                                                                    inventoryItemRow.InventoryID,
                                                                                                    null,
                                                                                                    fsContractPeriodDet?.Qty,
                                                                                                    fsContractPeriodDet?.UOM,
                                                                                                    (DateTime)cacheDetail.Graph.Accessinfo.BusinessDate,
                                                                                                    fsContractPeriodDet.RecurringUnitPrice,
                                                                                                    alwaysFromBaseCurrency: true,
                                                                                                    currencyInfo: null,
                                                                                                    catchSalesPriceException: true);

            if (salesPriceSet.ErrorCode == ID.PriceErrorCode.UOM_INCONSISTENCY)
            {
                throw new PXException(PXMessages.LocalizeFormatNoPrefix(TX.Error.INVENTORY_ITEM_UOM_INCONSISTENCY, inventoryItemRow.InventoryCD), PXErrorLevel.Error);
            }

            return salesPriceSet.Price;
        }

        private static void EnableDisableContractPeriodDet(PXCache cache, FSContractPeriodDet fsContractPeriodDetRow)
        {
            PXUIFieldAttribute.SetEnabled<FSContractPeriodDet.billingRule>(cache, fsContractPeriodDetRow, fsContractPeriodDetRow.LineType == ID.LineType_ContractPeriod.SERVICE);
            PXUIFieldAttribute.SetEnabled<FSContractPeriodDet.time>(cache, fsContractPeriodDetRow, fsContractPeriodDetRow.BillingRule == ID.BillingRule.TIME);
            PXUIFieldAttribute.SetEnabled<FSContractPeriodDet.qty>(cache, fsContractPeriodDetRow, fsContractPeriodDetRow.BillingRule == ID.BillingRule.FLAT_RATE);
        }

        private static string GetContractPeriodFilterDefaultAction(PXGraph graph, int? serviceContractID)
        {
            bool anyInvoicedPeriod = PXSelect<FSContractPeriod,
                                     Where<
                                         FSContractPeriod.serviceContractID, Equal<Required<FSContractPeriod.serviceContractID>>,
                                     And<
                                         FSContractPeriod.status, Equal<FSContractPeriod.status.Active>>>>
                                     .Select(graph, serviceContractID).Count() > 0;

            return anyInvoicedPeriod == true ? ID.ContractPeriod_Actions.SEARCH_BILLING_PERIOD : ID.ContractPeriod_Actions.MODIFY_UPCOMING_BILLING_PERIOD;
        }

        private void AmountFieldSelectingHandler(PXCache cache, PXFieldSelectingEventArgs e, string name, string billingRule, int? time, decimal? qty)
        {
            if (IsCopyPasteContext)
            {
                return;
            }

            if (billingRule == ID.BillingRule.TIME)
            {
                e.ReturnState = PXStringState.CreateInstance(e.ReturnState, 6, null, name, false, null, ActionsMessages.TimeSpanLongHM, null, null, null, null);

                TimeSpan span = new TimeSpan(0, 0, time ?? 0, 0);
                int hours = span.Days * 24 + span.Hours;
                e.ReturnValue = string.Format("{1,4}{2:00}", span.Days, hours, span.Minutes);
            }
            else if (billingRule == ID.BillingRule.FLAT_RATE)
            {
                e.ReturnState = PXDecimalState.CreateInstance(e.ReturnState, 2, name, false, -1, null, null);
                e.ReturnValue = (qty ?? 0).ToString(CultureInfo.InvariantCulture);
            }
        }

        private static void SetRegularPrice(PXCache cache, FSContractPeriodDet fsContractPeriodDetRow, FSServiceContract fsServiceContractRow)
        {
            decimal? regularPrice = GetSalesPriceItemInfo(cache, fsServiceContractRow, fsContractPeriodDetRow);
            cache.SetValueExt<FSContractPeriodDet.regularPrice>(fsContractPeriodDetRow, regularPrice ?? 0);
        }
        
        private void InsertContractAction(object row, PXDBOperation operation, bool? changeRecurrence = false)
        {
            FSServiceContract fsServiceContractRow = null;
            FSSchedule fsScheduleRow = null;
            this.insertContractActionForSchedules = false;

            if (FSServiceContract.TryParse(row, out fsServiceContractRow) == false
                && FSSchedule.TryParse(row, out fsScheduleRow) == false)
            {
                return;
            }

			if (operation != PXDBOperation.Insert
				&& operation != PXDBOperation.Update)
			{
				return;
			}

            FSContractAction fsContractActionRow = new FSContractAction();
			fsContractActionRow.Type = fsServiceContractRow != null ? ID.RecordType_ContractAction.CONTRACT : ID.RecordType_ContractAction.SCHEDULE;
            fsContractActionRow.ServiceContractID = fsServiceContractRow != null ? fsServiceContractRow.ServiceContractID : fsScheduleRow.EntityID;
            fsContractActionRow.ActionBusinessDate = Accessinfo.BusinessDate;

			if (operation == PXDBOperation.Insert)
            {
				fsContractActionRow = ContractHistoryItems.Insert(fsContractActionRow);

				if (IsCopyContract != true && fsServiceContractRow.OrigServiceContractRefNbr == null)
				{
					fsContractActionRow.Action = ID.Action_ContractAction.CREATE;
					fsContractActionRow = ContractHistoryItems.Update(fsContractActionRow);
				}
				else
				{
					fsContractActionRow.Action = ID.Action_ContractAction.Copied;
					fsContractActionRow.OrigServiceContractRefNbr = fsServiceContractRow.OrigServiceContractRefNbr;
					fsContractActionRow = ContractHistoryItems.Update(fsContractActionRow);
					fsServiceContractRow.OrigServiceContractRefNbr = null;
				}
			}
			else if (operation == PXDBOperation.Update
						&& fsServiceContractRow != null
						&& this.IsRenewContract == true)
			{
				fsContractActionRow = ContractHistoryItems.Insert(fsContractActionRow);

				fsContractActionRow.Action = ID.Action_ContractAction.Renew;
				fsContractActionRow.EffectiveDate = fsServiceContractRow.RenewalDate;
			}
            else if (operation == PXDBOperation.Update
                        && (fsServiceContractRow == null
                                || (this.isStatusChanged == true)))
            {
				fsContractActionRow = ContractHistoryItems.Insert(fsContractActionRow);

				if (fsServiceContractRow != null)
				{
					fsContractActionRow.Action = GetActionFromServiceContractStatus(fsServiceContractRow);
					fsContractActionRow.EffectiveDate = fsServiceContractRow.StatusEffectiveFromDate;

					if (fsContractActionRow.Action == ID.Action_ContractAction.ACTIVATE)
					{
						this.insertContractActionForSchedules = true;
					}
					this.isStatusChanged = false;
				}
				else
				{
					fsContractActionRow.Action = GetActionFromSchedule(fsScheduleRow); // Darwing @TODO we have also need to add deleted Schedule Action.
					fsContractActionRow.ScheduleNextExecutionDate = fsScheduleRow.StartDate;
					fsContractActionRow.ScheduleRecurrenceDescr = fsScheduleRow.RecurrenceDescription;
					fsContractActionRow.ScheduleRefNbr = fsScheduleRow.RefNbr;
					fsContractActionRow.ScheduleChangeRecurrence = changeRecurrence ?? false;
				}

				fsContractActionRow = ContractHistoryItems.Update(fsContractActionRow);
			}
			else return;
			
			ContractHistoryItems.Cache.Persist(PXDBOperation.Insert);
        }

        public virtual void InsertContractActionBySchedules(PXDBOperation operation)
        {
            if (this.insertContractActionForSchedules == true)
            {
                foreach (ActiveSchedule scheduleRow in ActiveScheduleRecords.Select())
                {
                    InsertContractAction(scheduleRow, operation, scheduleRow.ChangeRecurrence);
                }
            }
        }

        public virtual ScheduleProjection GetNextExecutionProjection(PXCache cache, FSSchedule fsScheduleRow, DateTime startDate)
        {
            ScheduleProjection scheduleProjectionRow = new ScheduleProjection();
            DateTime endDate = startDate.AddYears(1);

            if (fsScheduleRow.LastGeneratedElementDate.HasValue == true 
                    && endDate <= fsScheduleRow.LastGeneratedElementDate)
            {
                endDate = fsScheduleRow.LastGeneratedElementDate.Value.AddYears(2);
            }

            var period = new Period(startDate, endDate);

            List<Scheduler.Schedule> mapScheduleResults = new List<Scheduler.Schedule>();
            var generator = new TimeSlotGenerator();

            mapScheduleResults = MapFSScheduleToSchedule.convertFSScheduleToSchedule(cache, fsScheduleRow, endDate, ID.RecordType_ServiceContract.SERVICE_CONTRACT, period);

            List<TimeSlot> timeSlots = generator.GenerateCalendar(period, mapScheduleResults);

            scheduleProjectionRow.Date = timeSlots[0].DateTimeBegin;
            scheduleProjectionRow.BeginDateOfWeek = SharedFunctions.StartOfWeek((DateTime)scheduleProjectionRow.Date, DayOfWeek.Monday);

            return scheduleProjectionRow;
        }

        public virtual bool CheckNewContractStatus(PXGraph graph, FSServiceContract fsServiceContractRow, string newContractStatus, ref string errorMessage)
        {
            errorMessage = string.Empty;

			if (IsImport) return true;

            // Draft => Active
            if (fsServiceContractRow.Status == ID.Status_ServiceContract.DRAFT
                    && newContractStatus == ID.Status_ServiceContract.ACTIVE)
            {
                return true;
            }

            // Active => Suspended
            if (fsServiceContractRow.Status == ID.Status_ServiceContract.ACTIVE
                    && newContractStatus == ID.Status_ServiceContract.SUSPENDED)
            {
                return true;
            }

            // Active => Canceled
            if (fsServiceContractRow.Status == ID.Status_ServiceContract.ACTIVE
                    && newContractStatus == ID.Status_ServiceContract.CANCELED)
            {
                return true;
            }

            // Active => Expired
            if (fsServiceContractRow.Status == ID.Status_ServiceContract.ACTIVE
                    && newContractStatus == ID.Status_ServiceContract.EXPIRED)
            {
                return true;
            }

            // Suspended => Active
            if (fsServiceContractRow.Status == ID.Status_ServiceContract.SUSPENDED
                    && newContractStatus == ID.Status_ServiceContract.ACTIVE)
            {
                return true;
            }

            // Suspended => Canceled
            if (fsServiceContractRow.Status == ID.Status_ServiceContract.SUSPENDED
                    && newContractStatus == ID.Status_ServiceContract.CANCELED)
            {
                return true;
            }

            errorMessage = TX.Error.INVALID_APPOINTMENT_STATUS_TRANSITION;
            return false;
        }

        public virtual void ApplyOrScheduleStatusChange(PXGraph graph, PXCache cache, FSServiceContract fsServiceContractRow, DateTime? businessDate, DateTime? effectiveDate, string newStatus)
        {
            if (fsServiceContractRow == null || businessDate.HasValue == false || effectiveDate.HasValue == false)
            {
                return;
            }

			if (newStatus == ID.Status_ServiceContract.EXPIRED)
			{ 
				FSServiceOrder soRow = PXSelect<FSServiceOrder,
								Where<FSServiceOrder.billServiceContractID, Equal<Required<FSServiceOrder.billServiceContractID>>,
									And<FSServiceOrder.completed, Equal<False>,
									And<FSServiceOrder.closed, Equal<False>>>>>
							.SelectWindowed(graph, 0, 1, fsServiceContractRow.ServiceContractID);

				FSAppointment appRow = soRow == null ? PXSelect<FSAppointment,
								Where<FSAppointment.billServiceContractID, Equal<Required<FSAppointment.billServiceContractID>>,
									And<FSAppointment.completed, Equal<False>,
									And<FSAppointment.closed, Equal<False>>>>>
							.SelectWindowed(graph, 0, 1, fsServiceContractRow.ServiceContractID) : null;

				if (appRow != null || soRow != null)
				{
					throw new PXException(PXMessages.LocalizeFormatNoPrefix(TX.Error.ServiceContractCannotBeExpired), PXErrorLevel.Error);
				}
			}

            if (effectiveDate.Value.Date == businessDate.Value.Date)
            {
                cache.SetValueExt<FSServiceContract.status>(fsServiceContractRow, newStatus);

                if (newStatus != ID.Status_ServiceContract.EXPIRED && newStatus != ID.Status_ServiceContract.CANCELED)
                {
                    fsServiceContractRow.UpcomingStatus = fsServiceContractRow.ExpirationType != ID.Contract_ExpirationType.UNLIMITED ? ID.Status_ServiceContract.EXPIRED : null;
                    fsServiceContractRow.StatusEffectiveFromDate = effectiveDate;
                    fsServiceContractRow.StatusEffectiveUntilDate = fsServiceContractRow.ExpirationType != ID.Contract_ExpirationType.UNLIMITED ? fsServiceContractRow.EndDate : null;
                }
                else
                {
                    fsServiceContractRow.UpcomingStatus = null;
                    fsServiceContractRow.StatusEffectiveFromDate = effectiveDate;
                    fsServiceContractRow.StatusEffectiveUntilDate = null;
                }

                if (newStatus == ID.Status_ServiceContract.CANCELED || newStatus == ID.Status_ServiceContract.SUSPENDED)
                {
                    DeleteScheduledAppSO(fsServiceContractRow, effectiveDate);
                }
            }
            else if(effectiveDate.Value.Date > businessDate.Value.Date)
            {
                fsServiceContractRow.UpcomingStatus = newStatus;
                fsServiceContractRow.StatusEffectiveUntilDate = effectiveDate;
            }
        }

        public void ExpireContract()
        {
            ApplyOrScheduleStatusChange(this,
                                        ServiceContractRecords.Cache,
                                        ServiceContractRecords.Current,
                                        Accessinfo.BusinessDate,
                                        Accessinfo.BusinessDate,
                                        ID.Status_ServiceContract.EXPIRED);

            ForceUpdateCacheAndSave(ServiceContractRecords.Cache, this.ServiceContractRecords.Current);
        }

        /// <summary>
        /// Return true if the Service Contract [fsServiceContractRow] can be deleted based on its status
        /// </summary>
        public static bool CanDeleteServiceContract(FSServiceContract fsServiceContractRow)
        {
            if (fsServiceContractRow == null
                    || fsServiceContractRow.Status != ID.Status_ServiceContract.DRAFT)
            {
                return false;
            }

            return true;
        }

        public virtual string GetActionFromServiceContractStatus(FSServiceContract fsServiceContractRow)
        {
            if (fsServiceContractRow == null)
            {
                return null;
            }

            switch (fsServiceContractRow.Status)
            {
                case ID.Status_ServiceContract.ACTIVE:
                    return ID.Action_ContractAction.ACTIVATE;
                case ID.Status_ServiceContract.SUSPENDED:
                    return ID.Action_ContractAction.SUSPEND;
                case ID.Status_ServiceContract.EXPIRED:
                    return ID.Action_ContractAction.EXPIRE;
                case ID.Status_ServiceContract.CANCELED:
                    return ID.Action_ContractAction.CANCEL;
                default:
                    return null;
            }
        }

        public virtual string GetActionFromSchedule(FSSchedule fsScheduleRow)
        {
            if (fsScheduleRow == null)
            {
                return null;
            }

            if (fsScheduleRow.Active == true)
            {
                return ID.Action_ContractAction.ACTIVATE;
            }
            else if (fsScheduleRow.Active == false)
            {
                return ID.Action_ContractAction.INACTIVATE_SCHEDULE;
            }
            else
            {
                return null;
            }
        }

        public virtual void ApplyContractPeriodStatusChange(FSServiceContract fsServiceContractRow)
        {
            if (fsServiceContractRow.BillingType == FSServiceContract.billingType.Values.StandardizedBillings)
            {
                FSContractPeriod fsContractPeriod = PXSelect<FSContractPeriod,
                                                    Where<
                                                        FSContractPeriod.serviceContractID, Equal<Required<FSContractPeriod.serviceContractID>>>,
                                                    OrderBy<
                                                        Desc<FSContractPeriod.createdDateTime>>>
                                                    .SelectWindowed(this, 0, 1, fsServiceContractRow.ServiceContractID);

                if (fsContractPeriod != null)
                { 
                    fsContractPeriod.Status = ID.Status_ContractPeriod.INACTIVE;
                }
            }

            ForceUpdateCacheAndSave(ServiceContractRecords.Cache, fsServiceContractRow);
        }

        public virtual void UpdateSchedulesByActivateContract()
        {
            foreach (ActiveSchedule activeScheduleRow in ActiveScheduleRecords.Select())
            {
                activeScheduleRow.EndDate = null;
                activeScheduleRow.StartDate = activeScheduleRow.EffectiveRecurrenceStartDate ?? activeScheduleRow.StartDate;
                activeScheduleRow.NextExecutionDate = activeScheduleRow.NextExecution ?? SharedFunctions.GetNextExecution(ActiveScheduleRecords.Cache, activeScheduleRow);
                activeScheduleRow.EnableExpirationDate = false;
                ActiveScheduleRecords.Cache.Update(activeScheduleRow);
            }
        }

        public virtual void UpdateSchedulesByCancelContract(DateTime? cancelDate)
        {
            foreach (ActiveSchedule activeScheduleRow in ActiveScheduleRecords.Select())
            {
                activeScheduleRow.EndDate = cancelDate;
                activeScheduleRow.EnableExpirationDate = true;

                if (activeScheduleRow.NextExecutionDate >= cancelDate)
                {
                    activeScheduleRow.NextExecutionDate = null;
                }

                ActiveScheduleRecords.Cache.Update(activeScheduleRow);
            }
        }

        public virtual void UpdateSchedulesBySuspendContract(DateTime? suspendDate)
        {
            foreach (ActiveSchedule activeScheduleRow in ActiveScheduleRecords.Select())
            {
                activeScheduleRow.EndDate = suspendDate;
                activeScheduleRow.EnableExpirationDate = true;

                if (activeScheduleRow.NextExecutionDate >= suspendDate)
                {
                    activeScheduleRow.NextExecutionDate = null;
                }

                ActiveScheduleRecords.Cache.Update(activeScheduleRow);
            }
        }

        public virtual void SetEffectiveUntilDate(PXCache cache, FSServiceContract fsServiceContractRow)
        {
            if (fsServiceContractRow.ExpirationType == ID.Contract_ExpirationType.UNLIMITED
                && string.IsNullOrEmpty(fsServiceContractRow.UpcomingStatus))
            {
                fsServiceContractRow.StatusEffectiveUntilDate = null;
            }
            else
            {
                cache.RaiseFieldUpdated<FSServiceContract.endDate>(fsServiceContractRow, null);
            }
        }

        public virtual bool CheckDatesApplyOrScheduleStatusChange(PXCache cache, FSServiceContract fsServiceContractRow, DateTime? businessDate, DateTime? effectiveDate)
        {
            if (effectiveDate.HasValue
                && ((effectiveDate.Value.Date < businessDate.Value.Date)
                    || (fsServiceContractRow.ExpirationType == ID.Contract_ExpirationType.EXPIRING
                        && effectiveDate.Value.Date >= fsServiceContractRow.EndDate)))
            {
                return false;
            }

            return true;
        }

        public virtual void ActivateCurrentPeriod()
        {
            ContractPeriodFilter.Cache.SetValueExt<FSContractPeriodFilter.actions>(ContractPeriodFilter.Current, ID.ContractPeriod_Actions.MODIFY_UPCOMING_BILLING_PERIOD);
            ContractPeriodFilter.Cache.RaiseRowSelected(ContractPeriodFilter.Current);
            ServiceContractSelected.Current = ServiceContractSelected.Select();

            if (ContractPeriodRecords.Current != null
                    && ContractPeriodRecords.Current.Status != ID.Status_ContractPeriod.INVOICED
                    && ServiceContractSelected.Current != null
                    && ServiceContractSelected.Current.Status == ID.Status_ServiceContract.ACTIVE)
            {
                //updating Current Period to active
                FSContractPeriod fsCurrentContractPeriodRow = ContractPeriodRecords.Current;
                ContractPeriodRecords.Cache.SetValueExt<FSContractPeriod.status>(ContractPeriodRecords.Current, ID.Status_ContractPeriod.ACTIVE);
                ContractPeriodRecords.Cache.SetStatus(ContractPeriodRecords.Current, PXEntryStatus.Updated);

                //updating Current Contract Billing Dates
                FSServiceContract fsServiceContractRow = ServiceContractSelected.Current;
                if (fsServiceContractRow.BillingType == FSServiceContract.billingType.Values.StandardizedBillings)
                {
                    fsServiceContractRow.LastBillingInvoiceDate = fsServiceContractRow.NextBillingInvoiceDate ?? fsServiceContractRow.LastBillingInvoiceDate;
                    fsServiceContractRow.NextBillingInvoiceDate = fsCurrentContractPeriodRow.EndPeriodDate != null ? fsCurrentContractPeriodRow.EndPeriodDate : null;
                }
                else if(fsServiceContractRow.IsFixedRateContract == true)
                {
                    fsServiceContractRow.NextBillingInvoiceDate = fsCurrentContractPeriodRow.StartPeriodDate;
                }
                ServiceContractSelected.Cache.SetStatus(fsServiceContractRow, PXEntryStatus.Updated);

				GenerateNewContractPeriod(fsCurrentContractPeriodRow, ContractPeriodDetRecords.Select().RowCast<FSContractPeriodDet>().ToList());

				Save.Press();
                ContractPeriodFilter.Cache.SetDefaultExt<FSContractPeriodFilter.actions>(ContractPeriodFilter.Current);
                UnholdAPPSORelatedToContractPeriod(ServiceContractSelected.Current, fsCurrentContractPeriodRow);
            }
        }

		public virtual void GenerateNewContractPeriod(FSContractPeriod fsCurrentContractPeriodRow, List<FSContractPeriodDet> fsContractPeriodDetRows)
		{
			if (fsCurrentContractPeriodRow == null)
				return;

			//Inserting new billing period
			FSContractPeriod fsContractPeriodRow = new FSContractPeriod();
			fsContractPeriodRow.StartPeriodDate = fsCurrentContractPeriodRow.EndPeriodDate.Value.AddDays(1);
			fsContractPeriodRow.EndPeriodDate = GetContractPeriodEndDate(ServiceContractSelected.Current, fsContractPeriodRow.StartPeriodDate.Value);

			if (fsContractPeriodRow.EndPeriodDate != null && fsContractPeriodRow.StartPeriodDate < fsContractPeriodRow.EndPeriodDate)
			{
				fsContractPeriodRow = ContractPeriodRecords.Current = ContractPeriodRecords.Insert(fsContractPeriodRow);

				if (fsContractPeriodDetRows != null)
				{
					foreach (FSContractPeriodDet fsCurrentContractPeriodDetRow in fsContractPeriodDetRows)
					{
						FSContractPeriodDet fsContractPeriodDetRow = new FSContractPeriodDet();
						fsContractPeriodDetRow.ServiceContractID = fsCurrentContractPeriodDetRow.ServiceContractID;
						fsContractPeriodDetRow.InventoryID = fsCurrentContractPeriodDetRow.InventoryID;
						fsContractPeriodDetRow.LineType = fsCurrentContractPeriodDetRow.LineType;

						fsContractPeriodDetRow = PXCache<FSContractPeriodDet>.CreateCopy(ContractPeriodDetRecords.Insert(fsContractPeriodDetRow));

						fsContractPeriodDetRow.BillingRule = fsCurrentContractPeriodDetRow.BillingRule;
						fsContractPeriodDetRow.SMEquipmentID = fsCurrentContractPeriodDetRow.SMEquipmentID;

						if (fsCurrentContractPeriodDetRow.BillingRule == ID.BillingRule.TIME)
						{
							fsContractPeriodDetRow.Time = fsCurrentContractPeriodDetRow.Time;
						}
						else
						{
							fsContractPeriodDetRow.Qty = fsCurrentContractPeriodDetRow.Qty;
						}

						fsContractPeriodDetRow.RecurringUnitPrice = fsCurrentContractPeriodDetRow.RecurringUnitPrice;
						fsContractPeriodDetRow.OverageItemPrice = fsCurrentContractPeriodDetRow.OverageItemPrice;
						fsContractPeriodDetRow.Rollover = fsCurrentContractPeriodDetRow.Rollover;
						fsContractPeriodDetRow.ProjectID = fsCurrentContractPeriodDetRow.ProjectID;
						fsContractPeriodDetRow.ProjectTaskID = fsCurrentContractPeriodDetRow.ProjectTaskID;
						fsContractPeriodDetRow.CostCodeID = fsCurrentContractPeriodDetRow.CostCodeID;
						fsContractPeriodDetRow.DeferredCode = fsCurrentContractPeriodDetRow.DeferredCode;

						ContractPeriodDetRecords.Update(fsContractPeriodDetRow);
					}
				}
			}
		}

		public virtual void DeleteScheduledAppSO(FSServiceContract fsServiceContractRow, DateTime? cancelationDate)
        {
            if (fsServiceContractRow != null && cancelationDate != null)
            {
                ServiceOrderEntry graphServiceOrderEntry = PXGraph.CreateInstance<ServiceOrderEntry>();
                AppointmentEntry graphAppointmentEntry = PXGraph.CreateInstance<AppointmentEntry>();
                PXResultset<FSServiceOrder> fsServiceOrderRows;
                PXResultset<FSAppointment> fsAppointmentRows;

                if (fsServiceContractRow.ScheduleGenType == ID.ScheduleGenType_ServiceContract.SERVICE_ORDER)
                {
                    fsServiceOrderRows = PXSelect<FSServiceOrder,
                                         Where<
                                             FSServiceOrder.serviceContractID, Equal<Required<FSServiceOrder.serviceContractID>>,
                                         And<
                                             FSServiceOrder.orderDate, Greater<Required<FSServiceOrder.orderDate>>,
                                         And<
                                             FSServiceOrder.openDoc, Equal<True>,
                                         And<
                                             FSServiceOrder.allowInvoice, Equal<False>>>>>>
                                         .Select(this, fsServiceContractRow.ServiceContractID, cancelationDate);

                    foreach (FSServiceOrder fsServiceOrderRow in fsServiceOrderRows)
                    {
                        graphServiceOrderEntry.ServiceOrderRecords.Current = graphServiceOrderEntry.ServiceOrderRecords.Search<FSServiceOrder.sOID>(fsServiceOrderRow.SOID, fsServiceOrderRow.SrvOrdType);
                        graphServiceOrderEntry.Delete.Press();
                    }
                }
                else
                {
                    fsAppointmentRows = PXSelect<FSAppointment,
                                        Where<
                                            FSAppointment.serviceContractID, Equal<Required<FSAppointment.serviceContractID>>,
                                        And<
                                            FSAppointment.executionDate, Greater<Required<FSAppointment.executionDate>>,
                                        And<
                                            FSAppointment.notStarted, Equal<True>>>>>
                                        .Select(this, fsServiceContractRow.ServiceContractID, cancelationDate);

                    foreach (FSAppointment fsAppointmentRow in fsAppointmentRows)
                    {
                        graphAppointmentEntry.AppointmentRecords.Current = graphAppointmentEntry.AppointmentRecords.Search<FSAppointment.appointmentID>(fsAppointmentRow.AppointmentID, fsAppointmentRow.SrvOrdType);
                        graphAppointmentEntry.Delete.Press();
                    }
                }

                fsServiceOrderRows = PXSelect<FSServiceOrder,
                                     Where<
                                         FSServiceOrder.billServiceContractID, Equal<Required<FSServiceOrder.billServiceContractID>>,
                                     And<
                                         FSServiceOrder.orderDate, Greater<Required<FSServiceOrder.orderDate>>,
                                     And<
                                         FSServiceOrder.closed, Equal<False>,
                                     And<
                                         FSServiceOrder.allowInvoice, Equal<False>>>>>>
                                     .Select(this, fsServiceContractRow.ServiceContractID, cancelationDate);

                foreach (FSServiceOrder fsServiceOrderRow in fsServiceOrderRows)
                {
                    var serviceOrder = graphServiceOrderEntry.ServiceOrderRecords.Current = graphServiceOrderEntry.ServiceOrderRecords.Search<FSServiceOrder.sOID>
                                                                                            (fsServiceOrderRow.SOID, fsServiceOrderRow.SrvOrdType);

                    if (serviceOrder?.BillingBy == ID.Billing_By.SERVICE_ORDER)
                    {
                        graphServiceOrderEntry.ServiceOrderRecords.Cache.SetValueExt<FSServiceOrder.billServiceContractID>(serviceOrder, null);

                        graphServiceOrderEntry.Save.Press();
                    }
                }

                fsAppointmentRows = PXSelect<FSAppointment,
                                    Where<
                                        FSAppointment.billServiceContractID, Equal<Required<FSAppointment.billServiceContractID>>,
                                    And<
                                        FSAppointment.executionDate, Greater<Required<FSAppointment.executionDate>>,
                                    And<
                                        FSAppointment.closed, Equal<False>>>>>
                                    .Select(this, fsServiceContractRow.ServiceContractID, cancelationDate);

                foreach (FSAppointment fsAppointmentRow in fsAppointmentRows)
                {
                    graphAppointmentEntry.AppointmentRecords.Current = graphAppointmentEntry.AppointmentRecords.Search<FSAppointment.appointmentID>
                                                                                            (fsAppointmentRow.AppointmentID, fsAppointmentRow.SrvOrdType);

                    var relatedServiceOrder = graphAppointmentEntry.ServiceOrderRelated.Current;

                    if (relatedServiceOrder?.BillingBy == ID.Billing_By.APPOINTMENT)
                    {
                        graphAppointmentEntry.AppointmentRecords.Cache.SetValueExt<FSAppointment.billServiceContractID>(graphAppointmentEntry.AppointmentRecords.Current, null);

                        graphAppointmentEntry.Save.Press();
                    }
                }
            }
        }

        public virtual void SetBillingPeriod(FSServiceContract fsServiceContractRow)
        {
			if (this.IsRenewContract)
				return;

            if (ContractPeriodRecords.Current != null)
            {
                ContractPeriodRecords.Current.StartPeriodDate = fsServiceContractRow.StartDate;
                ContractPeriodRecords.Current.EndPeriodDate = GetContractPeriodEndDate(fsServiceContractRow, fsServiceContractRow.StartDate);

                if (ContractPeriodRecords.Current.EndPeriodDate != null)
                {
                    if (ContractPeriodRecords.Cache.GetStatus(ContractPeriodRecords.Current) == PXEntryStatus.Notchanged)
                    {
                        ContractPeriodRecords.Cache.SetStatus(ContractPeriodRecords.Current, PXEntryStatus.Updated);
                    }
                }
                else
                {
                    if (ContractPeriodRecords.Current.Status != ID.Status_ContractPeriod.ACTIVE)
                    {
                        ContractPeriodRecords.Delete(ContractPeriodRecords.Current);
                    }
                }
            }
            else
            {
                if (fsServiceContractRow.BillingType == FSServiceContract.billingType.Values.StandardizedBillings)
                {
                    FSContractPeriod fsContractPeriodRow = new FSContractPeriod();
                    fsContractPeriodRow.StartPeriodDate = fsServiceContractRow.StartDate ?? Accessinfo.BusinessDate;
                    DateTime? endPeriodDate = GetContractPeriodEndDate(fsServiceContractRow, fsContractPeriodRow.StartPeriodDate.Value);

                    if (endPeriodDate != null)
                    {
                        fsContractPeriodRow.EndPeriodDate = endPeriodDate.Value;
                        ContractPeriodRecords.Current = ContractPeriodRecords.Insert(fsContractPeriodRow);
                    }

                    ContractPeriodFilter.SetValueExt<FSContractPeriodFilter.actions>(ContractPeriodFilter.Current, ID.ContractPeriod_Actions.MODIFY_UPCOMING_BILLING_PERIOD);
                }
            }
        }

        public virtual bool EnableDisableActivatePeriodButton(FSServiceContract fsServiceContractRow, FSContractPeriod fsContractPeriodRow)
        {
            return fsServiceContractRow != null
					&& fsServiceContractRow.Status == ID.Status_ServiceContract.ACTIVE 
                        && fsContractPeriodRow != null
                            && fsContractPeriodRow.Status == ID.Status_ContractPeriod.INACTIVE 
                                && AllowActivatePeriod(fsContractPeriodRow);
        }

        public virtual bool AllowActivatePeriod(FSContractPeriod fsContractPeriodRow)
        {
            int activePeriods = PXSelect<FSContractPeriod,
                                Where<
                                    FSContractPeriod.serviceContractID, Equal<Required<FSContractPeriod.serviceContractID>>,
                                And<
                                    FSContractPeriod.status, Equal<FSContractPeriod.status.Active>>>>
                                .Select(this, ServiceContractSelected.Current.ServiceContractID).Count();

            return activePeriods == 0;
        }

        public virtual void MarkBillingPeriodAsInvoiced(FSSetup fsSetupRow, FSContractPostDoc fsContractPostDocRow)
        {
            ContractPeriodFilter.Cache.SetValueExt<FSContractPeriodFilter.actions>(ContractPeriodFilter.Current, ID.ContractPeriod_Actions.SEARCH_BILLING_PERIOD);
            ContractPeriodFilter.Current.ContractPeriodID = fsContractPostDocRow.ContractPeriodID;
            ContractPeriodFilter.Cache.RaiseRowSelected(ContractPeriodFilter.Current);
            bool allowUpdate = ServiceContractRecords.Cache.AllowUpdate;

            if (ContractPeriodRecords.Current != null
                    && ServiceContractSelected.Current != null)
            {

                ServiceContractRecords.Cache.AllowUpdate = true;

                ContractPeriodRecords.Cache.SetValueExt<FSContractPeriod.invoiced>(ContractPeriodRecords.Current, true);
                ContractPeriodRecords.Cache.SetValueExt<FSContractPeriod.contractPostDocID>(ContractPeriodRecords.Current, fsContractPostDocRow.ContractPostDocID);

                string originalStatus = this.ContractPeriodRecords.Current.Status;
                ContractPeriodRecords.Cache.SetValueExt<FSContractPeriod.status>(ContractPeriodRecords.Current, ID.Status_ContractPeriod.INVOICED);

                ForceUpdateCacheAndSave(ContractPeriodRecords.Cache, ContractPeriodRecords.Current);

                if (originalStatus != ID.Status_ContractPeriod.PENDING)
                {
                    FSServiceContract fsServiceContractRow = ServiceContractSelected.Current;
                    if (fsServiceContractRow.BillingType == FSServiceContract.billingType.Values.StandardizedBillings)
                    {
                        fsServiceContractRow.LastBillingInvoiceDate = fsServiceContractRow.NextBillingInvoiceDate ?? fsServiceContractRow.LastBillingInvoiceDate;
                    }
                    else if (fsServiceContractRow.IsFixedRateContract == true)
                    {
                        fsServiceContractRow.LastBillingInvoiceDate = ContractPeriodRecords.Current.StartPeriodDate;
                    }

                    fsServiceContractRow.NextBillingInvoiceDate = null;
                    ForceUpdateCacheAndSave(ServiceContractSelected.Cache, ServiceContractSelected.Current);

                    if (fsSetupRow.EnableContractPeriodWhenInvoice == true)
                    {
                        ActivateCurrentPeriod();
                    }
                }

                ServiceContractRecords.Cache.AllowUpdate = allowUpdate;
            }
        }

        public virtual void UnholdAPPSORelatedToContractPeriod(FSServiceContract fsServiceContractRow, FSContractPeriod fsContractPeriodRow)
        {
            if (fsServiceContractRow == null && fsContractPeriodRow == null)
            {
                return;
            }

            ServiceOrderEntry graphServiceOrderEntry = PXGraph.CreateInstance<ServiceOrderEntry>();
            AppointmentEntry graphAppointmentEntry = PXGraph.CreateInstance<AppointmentEntry>();
            PXResultset<FSServiceOrder> fsServiceOrderRows;
            PXResultset<FSAppointment> fsAppointmentRows;

            fsServiceOrderRows = PXSelect<FSServiceOrder,
                                    Where<FSServiceOrder.billServiceContractID, Equal<Required<FSServiceOrder.billServiceContractID>>,
                                    And<FSServiceOrder.orderDate, GreaterEqual<Required<FSServiceOrder.orderDate>>,
                                    And<FSServiceOrder.orderDate, LessEqual<Required<FSServiceOrder.orderDate>>,
                                    And<FSServiceOrder.closed, Equal<False>,
                                    And<FSServiceOrder.canceled, Equal<False>,
                                    And<FSServiceOrder.allowInvoice, Equal<False>>>>>>>>
                                 .Select(this, fsServiceContractRow.ServiceContractID, fsContractPeriodRow.StartPeriodDate, fsContractPeriodRow.EndPeriodDate);

            foreach (FSServiceOrder fsServiceOrderRow in fsServiceOrderRows)
            {
                graphServiceOrderEntry.ServiceOrderRecords.Current = graphServiceOrderEntry.ServiceOrderRecords.Search<FSServiceOrder.sOID>(fsServiceOrderRow.SOID, fsServiceOrderRow.SrvOrdType);

                graphServiceOrderEntry.ServiceOrderRecords.Cache.SetDefaultExt<FSServiceOrder.billContractPeriodID>(graphServiceOrderEntry.ServiceOrderRecords.Current);
                graphServiceOrderEntry.ServiceOrderRecords.Cache.Update(graphServiceOrderEntry.ServiceOrderRecords.Current);
                graphServiceOrderEntry.Save.Press();
            }

            fsAppointmentRows = PXSelect<FSAppointment,
                                    Where<FSAppointment.billServiceContractID, Equal<Required<FSAppointment.billServiceContractID>>,
                                    And<FSAppointment.executionDate, GreaterEqual<Required<FSAppointment.executionDate>>,
                                    And<FSAppointment.executionDate, LessEqual<Required<FSAppointment.executionDate>>,
                                    And<FSAppointment.closed, Equal<False>,
                                    And<FSAppointment.canceled, Equal<False>>>>>>>
                                .Select(this, fsServiceContractRow.ServiceContractID, fsContractPeriodRow.StartPeriodDate, fsContractPeriodRow.EndPeriodDate);

            foreach (FSAppointment fsAppointmentRow in fsAppointmentRows)
            {
                graphAppointmentEntry.AppointmentRecords.Current = graphAppointmentEntry.AppointmentRecords.Search<FSAppointment.appointmentID>(fsAppointmentRow.AppointmentID, fsAppointmentRow.SrvOrdType);

                graphAppointmentEntry.AppointmentRecords.Cache.SetDefaultExt<FSAppointment.billContractPeriodID>(graphAppointmentEntry.AppointmentRecords.Current);
                graphAppointmentEntry.AppointmentRecords.Cache.Update(graphAppointmentEntry.AppointmentRecords.Current);
                graphAppointmentEntry.Save.Press();
            }
    }

        public virtual void SetBillCustomerAndLocationID(PXCache cache, FSServiceContract fsServiceContractRow)
        {
            BAccount bAccountRow = PXSelect<BAccount,
                                   Where<
                                       BAccount.bAccountID, Equal<Required<FSServiceOrder.customerID>>>>
                                   .Select(cache.Graph, fsServiceContractRow.CustomerID);

            int? billCustomerID = null;
            int? billLocationID = null;
            string billTo = ID.Contract_BillTo.CUSTOMERACCT;

            if (bAccountRow == null || bAccountRow.Type != BAccountType.ProspectType)
            {
                Customer customerRow = SharedFunctions.GetCustomerRow(cache.Graph, fsServiceContractRow.CustomerID);
                FSxCustomer fsxCustomerRow = PXCache<Customer>.GetExtension<FSxCustomer>(customerRow);

                switch (fsxCustomerRow.DefaultBillingCustomerSource)
                {
                    case ID.Default_Billing_Customer_Source.SERVICE_ORDER_CUSTOMER:
                        billCustomerID = fsServiceContractRow.CustomerID;
                        billLocationID = fsServiceContractRow.CustomerLocationID;
                        break;

                    case ID.Default_Billing_Customer_Source.DEFAULT_CUSTOMER:
                        billTo = ID.Contract_BillTo.SPECIFICACCT;
                        billCustomerID = fsServiceContractRow.CustomerID;
                        billLocationID = GetDefaultLocationID(cache.Graph, fsServiceContractRow.CustomerID);
                        break;

                    case ID.Default_Billing_Customer_Source.SPECIFIC_CUSTOMER:
                        billTo = ID.Contract_BillTo.SPECIFICACCT;
                        billCustomerID = fsxCustomerRow.BillCustomerID;
                        billLocationID = fsxCustomerRow.BillLocationID;
                        break;
                }
            }

            cache.SetValueExt<FSServiceContract.billTo>(fsServiceContractRow, billTo);
            cache.SetValueExt<FSServiceContract.billCustomerID>(fsServiceContractRow, billCustomerID);
            cache.SetValueExt<FSServiceContract.billLocationID>(fsServiceContractRow, billLocationID);
        }

        public virtual void ForceUpdateCacheAndSave(PXCache cache, object row)
        {
            cache.AllowUpdate = true;
            cache.SetStatus(row, PXEntryStatus.Updated);
            this.GetSaveAction().Press();
        }

        public virtual int? GetDefaultLocationID(PXGraph graph, int? bAccountID)
        {
            return ServiceOrderEntry.GetDefaultLocationIDInt(graph, bAccountID);
        }

        public virtual DateTime? GetContractPeriodEndDate(FSServiceContract fsServiceContractRow, DateTime? lastGeneratedElementDate)
        {
            bool expired = false;
            var generator = new TimeSlotGenerator();
            List<Scheduler.Schedule> mapScheduleResults = new List<Scheduler.Schedule>();

            mapScheduleResults = MapFSServiceContractToSchedule.convertFSServiceContractToSchedule(fsServiceContractRow, lastGeneratedElementDate);

            DateTime? endate = generator.GenerateNextOccurrence(mapScheduleResults, lastGeneratedElementDate ?? fsServiceContractRow.StartDate.Value, fsServiceContractRow.EndDate, out expired);

            if (fsServiceContractRow.BillingPeriod != ID.Contract_BillingPeriod.WEEK)
            {
                endate = endate?.AddDays(-1);
            }

            if (expired == true)
            {
                endate = fsServiceContractRow.EndDate;
            }

            if (fsServiceContractRow.ExpirationType == ID.Contract_ExpirationType.EXPIRING
                && fsServiceContractRow.EndDate != null
                    && fsServiceContractRow.EndDate < endate)
            {
                return null;
            }

            return endate;
        }
        public virtual void CalculateBillHistoryUnboundFields(PXCache cache, FSBillHistory fsBillHistoryRow)
        {
            using (new PXConnectionScope())
            {
                ServiceOrderEntry.CalculateBillHistoryUnboundFieldsInt(cache, fsBillHistoryRow);

				bool hasParent = fsBillHistoryRow.ParentEntityType != null;
				string docType = hasParent ? fsBillHistoryRow.ParentDocType : fsBillHistoryRow.ChildDocType;
				string refNbr = hasParent ? fsBillHistoryRow.ParentRefNbr : fsBillHistoryRow.ChildRefNbr;

                FSContractPeriod fsContractPeriodRow = PXSelectJoin<FSContractPeriod,
                                                       InnerJoin<
                                                           FSContractPostDoc, On<FSContractPostDoc.contractPeriodID, Equal<FSContractPeriod.contractPeriodID>>>,
                                                       Where<
                                                           FSContractPeriod.serviceContractID, Equal<Required<FSContractPeriod.serviceContractID>>,
                                                       And<
                                                           FSContractPostDoc.postDocType, Equal<Required<FSContractPostDoc.postDocType>>,
                                                       And<
                                                           FSContractPostDoc.postRefNbr, Equal<Required<FSContractPostDoc.postRefNbr>>>>>>
                                                       .Select(cache.Graph, ServiceContractSelected.Current.ServiceContractID, docType, refNbr);

                if (fsContractPeriodRow != null)
                {
                    fsBillHistoryRow.ServiceContractPeriodID = fsContractPeriodRow.ContractPeriodID;
                    fsBillHistoryRow.ContractPeriodStatus = fsContractPeriodRow.Status;
                }
            }
        }

		public virtual void ContractForecastProc(FSServiceContract contract, DateTime? startDate, DateTime? endDate)
		{
			if (startDate == null || endDate == null || DateTime.Compare(startDate.Value, endDate.Value) > 0)
			{
				throw new ArgumentException();
			}

			var period = new Period((DateTime)startDate, endDate);

			DeactivatePreviousForecast(contract.ServiceContractID);

			FSContractForecast forecast = (FSContractForecast)this.forecastRecords.Cache.CreateInstance();
			forecast.ServiceContractID = contract.ServiceContractID;
			forecast.DateTimeBegin = startDate;
			forecast.DateTimeEnd = endDate;
			forecast.Active = true;

			this.forecastRecords.Insert(forecast);

			//Looping through all active schedules
			foreach (FSSchedule activeSchedule in PXSelect<FSSchedule,
												Where<FSSchedule.active, Equal<True>,
													And<FSSchedule.entityID, Equal<Required<FSSchedule.entityID>>>>>
												.Select(this, contract.ServiceContractID))
			{

				if (contract.EndDate == activeSchedule.EndDate)
				{
					activeSchedule.EndDate = null;
				}

				List<Scheduler.Schedule> mapScheduleResults = new List<Scheduler.Schedule>();
				var generator = new TimeSlotGenerator();

				mapScheduleResults = MapFSScheduleToSchedule.convertFSScheduleToSchedule(this.forecastRecords.Cache, activeSchedule, endDate, ID.RecordType_ServiceContract.SERVICE_CONTRACT);
				List<TimeSlot> timeSlots = generator.GenerateCalendar(period, mapScheduleResults);

				int totalOcurrencesForCurrentSchedule = timeSlots.Count();
				foreach (FSScheduleDet detRows in PXSelect<FSScheduleDet,
												Where<FSScheduleDet.scheduleID, Equal<Required<FSScheduleDet.scheduleID>>>>
												.Select(this, activeSchedule.ScheduleID)
												.RowCast<FSScheduleDet>()
												.Where(x => x.LineType != ID.LineType_ALL.INSTRUCTION
														 && x.LineType != ID.LineType_ALL.COMMENT))
				{
					if (detRows.LineType == ID.LineType_ALL.SERVICE_TEMPLATE)
					{
						foreach(FSServiceTemplateDet templateDet in PXSelect<FSServiceTemplateDet,
																		Where<FSServiceTemplateDet.serviceTemplateID, Equal<Required<FSServiceTemplateDet.serviceTemplateID>>>>
																	.Select(this, detRows.ServiceTemplateID)
																	.RowCast<FSServiceTemplateDet>()
																	.Where(x => x.LineType != ID.LineType_ALL.INSTRUCTION
																			 && x.LineType != ID.LineType_ALL.COMMENT))
						{
							InventoryItem item = InventoryItem.PK.Find(this, templateDet.InventoryID);
							FSxService itemExt = PXCache<InventoryItem>.GetExtension<FSxService>(item);

							FSContractForecastDet forecastDet = CreateContractForecastDet(contract,
																					templateDet.LineType,
																					itemExt.BillingRule ?? detRows.BillingRule,
																					ListField_ForecastDet_Type.Schedule,
																					templateDet.InventoryID,
																					detRows.Qty * templateDet.Qty,
																					totalOcurrencesForCurrentSchedule,
																					scheduleID: detRows.ScheduleID,
																					scheduleDetID: detRows.ScheduleDetID,
																					tranDesc: detRows.TranDesc,
																					recurrenceDesc: activeSchedule.RecurrenceDescription,
																					sMEquipmentID: detRows.SMEquipmentID,
																					componentID: detRows.ComponentID,
																					equipmentAction: detRows.EquipmentAction,
																					equipmentLineRef: detRows.EquipmentLineRef,
																					priceStartDate: startDate);

							this.forecastDetRecords.Insert(forecastDet);
						}
					}
					else
					{
						FSContractForecastDet forecastDet = CreateContractForecastDet(contract,
																					detRows.LineType,
																					detRows.BillingRule,
																					ListField_ForecastDet_Type.Schedule,
																					detRows.InventoryID,
																					detRows.Qty,
																					totalOcurrencesForCurrentSchedule,
																					scheduleID: detRows.ScheduleID,
																					scheduleDetID: detRows.ScheduleDetID,
																					tranDesc: detRows.TranDesc,
																					recurrenceDesc: activeSchedule.RecurrenceDescription,
																					sMEquipmentID: detRows.SMEquipmentID,
																					componentID: detRows.ComponentID,
																					equipmentAction: detRows.EquipmentAction,
																					equipmentLineRef: detRows.EquipmentLineRef,
																					priceStartDate: startDate);

						this.forecastDetRecords.Insert(forecastDet);
					}
				}
			}

			if (contract.BillingType != FSServiceContract.billingType.Values.PerformedBillings)
			{
				FSServiceContract copy = (FSServiceContract)this.ServiceContractRecords.Cache.CreateCopy(contract);
				copy.ExpirationType = ID.Contract_ExpirationType.EXPIRING;
				copy.StartDate = startDate;
				copy.EndDate = endDate;

				FSContractPeriod lastContractPeriodRow = PXSelect<FSContractPeriod,
																		Where<FSContractPeriod.serviceContractID, Equal<Required<FSContractPeriod.serviceContractID>>>,
																		OrderBy<Desc<FSContractPeriod.endPeriodDate>>>
																	.SelectWindowed(this, 0, 1, copy.ServiceContractID);

				if (lastContractPeriodRow != null)
				{
					int periodOccurrences = 0;

					var periodDetList = PXSelect<FSContractPeriodDet,
											Where<FSContractPeriodDet.contractPeriodID, Equal<Required<FSContractPeriod.contractPeriodID>>>>
										.Select(this, lastContractPeriodRow.ContractPeriodID);

					if (periodDetList != null && periodDetList.Count() > 0)
					{
						FSContractPeriod fsContractPeriodRow = new FSContractPeriod();
						fsContractPeriodRow.StartPeriodDate = startDate;
						fsContractPeriodRow.EndPeriodDate = GetContractPeriodEndDate(copy, fsContractPeriodRow.StartPeriodDate.Value);

						while (fsContractPeriodRow.EndPeriodDate != null && fsContractPeriodRow.StartPeriodDate < endDate)
						{
							periodOccurrences++;

							fsContractPeriodRow.StartPeriodDate = fsContractPeriodRow.EndPeriodDate.Value.AddDays(1);
							fsContractPeriodRow.EndPeriodDate = GetContractPeriodEndDate(copy, fsContractPeriodRow.StartPeriodDate.Value);
						};

						foreach (FSContractPeriodDet detRows in periodDetList)
						{
							FSContractForecastDet forecastDet = CreateContractForecastDet(contract,
																							detRows.LineType,
																							detRows.BillingRule,
																							ListField_ForecastDet_Type.ContractPeriod,
																							detRows.InventoryID,
																							detRows.Qty,
																							periodOccurrences,
																							sMEquipmentID: detRows.SMEquipmentID,
																							contractPeriodID: detRows.ContractPeriodID,
																							contractPeriodDetID: detRows.ContractPeriodDetID,
																							unitPrice: detRows.RecurringUnitPrice,
																							overagePrice: detRows.OverageItemPrice,
																							uOM: detRows.UOM);

							this.forecastDetRecords.Insert(forecastDet);

							//Clearing out the price for the same inventory ID included in schedules for this billing type.
							if (contract.BillingType == FSServiceContract.billingType.Values.StandardizedBillings)
							{
								forecastDetRecords.Cache.Inserted
														.RowCast<FSContractForecastDet>()
														.Where(x => x.InventoryID == detRows.InventoryID
																	&& x.ServiceContractID == contract.ServiceContractID
																	&& x.ContractPeriodID == null
																	&& x.ForecastDetType == ListField_ForecastDet_Type.Schedule)
														.ForEach(x => x.TotalPrice = 0m);
							}
						}
					}
				}
			}

			if (this.IsDirty == true)
			{
				this.Actions.PressSave();
			}
		}

		public virtual void DeactivatePreviousForecast(int? serviceContractID)
		{
			foreach (FSContractForecast row in PXSelect<FSContractForecast,
														Where<FSContractForecast.active, Equal<True>,
															And<FSContractForecast.serviceContractID, Equal<Required<FSContractForecast.serviceContractID>>>>>
													   .Select(this, serviceContractID))
			{
				row.Active = false;
				this.forecastRecords.Update(row);
			}
		}

		public virtual FSContractForecastDet CreateContractForecastDet(FSServiceContract serviceContract,
																	   string lineType,
																	   string billingRule,
																	   string detType,
																	   int? inventoryID,
																	   decimal? qty,
																	   int? occurrences,
																	   int? componentID = null,
																	   string equipmentAction = null,
																	   int? equipmentLineRef = null,
																	   int? sMEquipmentID = null,
																	   string tranDesc = null,
																	   string recurrenceDesc = null,
																	   int? scheduleID = null,
																	   int? scheduleDetID = null,
																	   int? contractPeriodID = null,
																	   int? contractPeriodDetID = null,
																	   decimal? unitPrice = null,
																	   decimal? overagePrice = null,
																	   string uOM = null,
																	   DateTime? priceStartDate = null)
		{
			//UnitPrice & PriceStartDate cannot be null at the same time.
			//In case unitPrice is not provided, PriceStartDate is used to calculate its value.
			if (unitPrice == null && priceStartDate == null)
			{
				throw new ArgumentException();
			}

			FSContractForecastDet ret = new FSContractForecastDet();
			ret.ServiceContractID = serviceContract.ServiceContractID;
			ret.LineType = lineType;
			ret.BillingRule = billingRule;
			ret.ForecastDetType = detType;
			ret.Occurrences = occurrences;
			ret.ScheduleID = scheduleID;
			ret.ScheduleDetID = scheduleDetID;
			ret.InventoryID = inventoryID;
			ret.Qty = qty;
			ret.ComponentID = componentID;
			ret.EquipmentAction = equipmentAction;
			ret.EquipmentLineRef = equipmentLineRef;
			ret.SMEquipmentID = sMEquipmentID;
			ret.TranDesc = tranDesc;
			ret.RecurrenceDesc = recurrenceDesc;
			ret.ContractPeriodID = contractPeriodID;
			ret.ContractPeriodDetID = contractPeriodDetID;
			ret.UnitPrice = unitPrice;
			ret.UOM = uOM;
			ret.OveragePrice = overagePrice;

			if (detType == ListField_ForecastDet_Type.Schedule
				&& serviceContract.BillingType == FSServiceContract.billingType.Values.FixedRateBillings)
			{
				ret.TotalPrice = 0m;
			}

			if (unitPrice == null)
			{
				if (serviceContract.BillingType == FSServiceContract.billingType.Values.PerformedBillings
						&& serviceContract.SourcePrice == ID.SourcePrice.CONTRACT)
				{
					FSSalesPrice salesPrice = this.SalesPriceLines.Select().RowCast<FSSalesPrice>().Where(x => x.InventoryID == ret.InventoryID).FirstOrDefault();
					if (salesPrice != null)
					{
						ret.UnitPrice = salesPrice.UnitPrice;
						ret.UOM = salesPrice.UOM;
					}
				}
				else if (serviceContract.BillingType != FSServiceContract.billingType.Values.PerformedBillings
							|| serviceContract.SourcePrice == ID.SourcePrice.PRICE_LIST)
				{
					InventoryItem item = InventoryItem.PK.Find(this, ret.InventoryID);
					SalesPriceSet salesPriceSet = FSPriceManagement.CalculateSalesPriceWithCustomerContract(
																				this.forecastDetRecords.Cache,
																				null,
																				null,
																				null,
																				serviceContract.CustomerID,
																				serviceContract.CustomerLocationID,
																				null,
																				ret.InventoryID,
																				null,
																				ret.Qty,
																				item?.SalesUnit,
																				priceStartDate.Value,
																				null,
																				alwaysFromBaseCurrency: true,
																				currencyInfo: null,
																				catchSalesPriceException: true);

					if (salesPriceSet != null)
					{
						ret.UnitPrice = salesPriceSet.Price;
						ret.UOM = item.SalesUnit;
					}
				}
			}

			if (ret.BillingRule == ID.BillingRule.NONE)
			{
				ret.UnitPrice = 0.0m;
			}

			return ret;
		}

		public virtual void SetForecastFilterDefaults()
		{
			ForecastFilter.Current.StartDate = GetDfltForecastFilterStartDate();
			ForecastFilter.Current.EndDate = GetDfltForecastFilterEndDate();
		}

		public virtual DateTime? GetDfltForecastFilterStartDate()
		{
			switch (ServiceContractRecords.Current.ExpirationType)
			{
				case ID.Contract_ExpirationType.UNLIMITED:
				case ID.Contract_ExpirationType.EXPIRING:
					return ServiceContractRecords.Current.StartDate;
				case ID.Contract_ExpirationType.RENEWABLE:
					return ServiceContractRecords.Current.Status != ID.Status_ServiceContract.ACTIVE ?
								ServiceContractRecords.Current.StartDate :
								ServiceContractRecords.Current.RenewalDate ?? ServiceContractRecords.Current.StartDate;
				default:
					throw new NotImplementedException();
			}
		}

		public virtual DateTime? GetDfltForecastFilterEndDate()
		{
			switch (ServiceContractRecords.Current.ExpirationType)
			{
				case ID.Contract_ExpirationType.UNLIMITED:
					return GetEndDateFromDuration(ServiceContractRecords.Current.StartDate.Value, SC_Duration_Type.YEAR, 1);
				case ID.Contract_ExpirationType.EXPIRING:
					return ServiceContractRecords.Current.EndDate;
				case ID.Contract_ExpirationType.RENEWABLE:
					if (ServiceContractRecords.Current.Status == ID.Status_ServiceContract.ACTIVE)
					{
						DateTime? startDate = ServiceContractRecords.Current.RenewalDate ?? ServiceContractRecords.Current.StartDate;
						return GetEndDateFromDuration(startDate.Value, ServiceContractRecords.Current.DurationType, ServiceContractRecords.Current.Duration ?? 1);
					}
					else
					{
						return ServiceContractRecords.Current.EndDate;
					}
				default:
					throw new NotImplementedException();
			}
		}

		public virtual void CopyContractProc(FSServiceContract contract, DateTime? date)
		{
			FSContractPeriod currentPeriod = ContractPeriodRecords.Current;

			this.Clear(PXClearOption.PreserveTimeStamp);
			using (PXTransactionScope ts = new PXTransactionScope())
			{
				var origContracts =
				PXSelect<FSServiceContract,
				Where<FSServiceContract.refNbr, Equal<Required<FSServiceContract.refNbr>>>>
				.Select(this, contract.RefNbr);

				foreach (FSServiceContract sourceContract in origContracts)
				{
					var newContract = new FSServiceContract
					{
						CustomerID = sourceContract.CustomerID,
						CustomerLocationID = sourceContract.CustomerLocationID
					};

					newContract = ServiceContractRecords.Insert(newContract);

					FSServiceContract targetContract = PXCache<FSServiceContract>.CreateCopy(sourceContract);

					targetContract.RefNbr = newContract.RefNbr;
					targetContract.ServiceContractID = newContract.ServiceContractID;
					targetContract.StartDate = date;

					if (sourceContract.ExpirationType != ID.Contract_ExpirationType.UNLIMITED)
					{
						if (sourceContract.StartDate.HasValue && sourceContract.EndDate.HasValue)
						{
							targetContract.EndDate = GetEndDate(targetContract, targetContract.StartDate, targetContract.EndDate);
						}
					}

					targetContract.UpcomingStatus = null;
					targetContract.Status = newContract.Status;
					targetContract.StatusEffectiveFromDate = null;
					targetContract.StatusEffectiveUntilDate = null;
					targetContract.NoteID = newContract.NoteID;
					targetContract.OrigServiceContractRefNbr = sourceContract.RefNbr;

					targetContract = ServiceContractRecords.Update(targetContract);

					SharedFunctions.CopyNotesAndFiles(ServiceContractRecords.Cache,
													  ServiceContractRecords.Cache,
													  contract,
													  targetContract,
													  true,
													  true);

					Answers.CopyAllAttributes(targetContract, sourceContract);

					this.Actions.PressSave();

					targetContract = ServiceContractRecords.Current;

					if (sourceContract.BillingType != FSServiceContract.billingType.Values.PerformedBillings)
					{
						PXResultset<FSContractPeriodDet> sourcePeriodDetails =
							PXSelectReadonly<FSContractPeriodDet,
							Where<FSContractPeriodDet.serviceContractID, Equal<Required<FSContractPeriodDet.serviceContractID>>,
								And<FSContractPeriodDet.contractPeriodID, Equal<Required<FSContractPeriodDet.contractPeriodID>>>>>
							.Select(this, sourceContract.ServiceContractID, currentPeriod.ContractPeriodID);

						foreach (FSContractPeriodDet sourcePeriodDet in sourcePeriodDetails)
						{
							FSContractPeriodDet periodDet = PXCache<FSContractPeriodDet>.CreateCopy(sourcePeriodDet);
							periodDet.ServiceContractID = targetContract.ServiceContractID;
							periodDet.ContractPeriodID = ContractPeriodRecords.Current.ContractPeriodID;
							periodDet.ContractPeriodDetID = null;

							periodDet = (FSContractPeriodDet)this.ContractPeriodDetRecords.Cache.Insert(periodDet);

							this.ContractPeriodDetRecords.Cache.SetDefaultExt<FSContractPeriodDet.remainingQty>(periodDet);
							this.ContractPeriodDetRecords.Cache.SetDefaultExt<FSContractPeriodDet.remainingTime>(periodDet);
						}
					}

					this.Actions.PressSave();

					targetContract = ServiceContractRecords.Current;

					this.CopySchedules(sourceContract.ServiceContractID, date);

					SalesPriceLines.Select();

					targetContract = ServiceContractRecords.Current = (FSServiceContract)ServiceContractRecords.Search<FSServiceContract.refNbr>(targetContract.RefNbr);

					if (sourceContract.SourcePrice == ID.SourcePrice.CONTRACT)
					{
						PXResultset<FSSalesPrice> sourcePeriodPrices =
							PXSelectReadonly<FSSalesPrice,
							Where<FSSalesPrice.serviceContractID, Equal<Required<FSSalesPrice.serviceContractID>>>>
							.Select(this, sourceContract.ServiceContractID);

						foreach (FSSalesPrice sourcePeriodPrice in sourcePeriodPrices)
						{
							foreach (FSSalesPrice salesPrice in SalesPriceLines.Select().RowCast<FSSalesPrice>().Where(x => x.InventoryID == sourcePeriodPrice.InventoryID))
							{
								FSSalesPrice contractPrice = (FSSalesPrice)SalesPriceLines.Cache.CreateCopy(salesPrice);
								contractPrice.UnitPrice = sourcePeriodPrice.UnitPrice;
								this.SalesPriceLines.Cache.Update(contractPrice);
							}
						}
					}

					this.Actions.PressSave();
					ts.Complete();

					this.OpenCopiedContract(ServiceContractRecords.Current);
				}
			}
		}

		public virtual void CopySchedules(int? serviceContractID, DateTime? date)
		{
		}

		public virtual void OpenCopiedContract(FSServiceContract contract)
		{
		}

		public void UpdateSchedules(PXCache cache, FSServiceContract fsServiceContractRow, bool isRenewalAction = false)
		{
			if (fsServiceContractRow == null
				|| (fsServiceContractRow.Status != ID.Status_ServiceContract.DRAFT
					&& isRenewalAction == false))
				return;

			if ((int?)cache.GetValueOriginal<FSServiceContract.customerID>(fsServiceContractRow) == fsServiceContractRow.CustomerID
				&& (int?)cache.GetValueOriginal<FSServiceContract.customerLocationID>(fsServiceContractRow) == fsServiceContractRow.CustomerLocationID
				&& (int?)cache.GetValueOriginal<FSServiceContract.projectID>(fsServiceContractRow) == fsServiceContractRow.ProjectID
				&& (int?)cache.GetValueOriginal<FSServiceContract.dfltProjectTaskID>(fsServiceContractRow) == fsServiceContractRow.DfltProjectTaskID
				&& (DateTime?)cache.GetValueOriginal<FSServiceContract.startDate>(fsServiceContractRow) == fsServiceContractRow.StartDate
				&& (DateTime?)cache.GetValueOriginal<FSServiceContract.endDate>(fsServiceContractRow) == fsServiceContractRow.EndDate)
			{
				return;
			}

			if (fsServiceContractRow.RecordType == ID.RecordType_ServiceContract.SERVICE_CONTRACT)
			{
				var graphServiceContractScheduleEntry = PXGraph.CreateInstance<ServiceContractScheduleEntry>();

				foreach (FSContractSchedule row in ContractSchedules.Select().RowCast<FSContractSchedule>().Where(x => x.Active == true))
				{
					graphServiceContractScheduleEntry.ContractScheduleRecords.Current = graphServiceContractScheduleEntry
																							 .ContractScheduleRecords.Search<FSContractSchedule.scheduleID>
																							 (row.ScheduleID,
																							  row.EntityID,
																							  row.CustomerID);

					FSContractSchedule fsContractScheduleRow = (FSContractSchedule)graphServiceContractScheduleEntry
																							 .ContractScheduleRecords.Cache.CreateCopy(graphServiceContractScheduleEntry.ContractScheduleRecords.Current);

					fsContractScheduleRow.CustomerID = fsServiceContractRow.CustomerID;
					fsContractScheduleRow.CustomerLocationID = fsServiceContractRow.CustomerLocationID;
					fsContractScheduleRow.ProjectID = fsServiceContractRow.ProjectID;
					fsContractScheduleRow.DfltProjectTaskID = fsServiceContractRow.DfltProjectTaskID;

					bool needUpdateExcDate = false;

					if (fsContractScheduleRow.EndDate == (DateTime?)cache.GetValueOriginal<FSServiceContract.endDate>(fsServiceContractRow)
						&& fsContractScheduleRow.EndDate != fsServiceContractRow.EndDate)
					{
						fsContractScheduleRow.EndDate = fsServiceContractRow.EndDate;
						needUpdateExcDate = true;
					}

					if (fsContractScheduleRow.StartDate == (DateTime?)cache.GetValueOriginal<FSServiceContract.startDate>(fsServiceContractRow)
						&& fsContractScheduleRow.StartDate != fsServiceContractRow.StartDate)
					{
						fsContractScheduleRow.StartDate = fsServiceContractRow.StartDate;
						needUpdateExcDate = true;
					}

					if (needUpdateExcDate)
					{
						fsContractScheduleRow.NextExecutionDate = SharedFunctions.GetNextExecution(ContractSchedules.Cache, fsContractScheduleRow);
					}

					graphServiceContractScheduleEntry.ContractScheduleRecords.Update(fsContractScheduleRow);
				}

				graphServiceContractScheduleEntry.Save.Press();
			}
			else if (fsServiceContractRow.RecordType == ID.RecordType_ServiceContract.ROUTE_SERVICE_CONTRACT)
			{
				var graphServiceContractScheduleEntry = PXGraph.CreateInstance<RouteServiceContractScheduleEntry>();

				foreach (FSContractSchedule row in ContractSchedules.Select())
				{
					graphServiceContractScheduleEntry.ContractScheduleRecords.Current = graphServiceContractScheduleEntry
																							 .ContractScheduleRecords.Search<FSRouteContractSchedule.scheduleID>
																							 (row.ScheduleID,
																							  row.EntityID,
																							  row.CustomerID);

					FSRouteContractSchedule fsContractScheduleRow = (FSRouteContractSchedule)graphServiceContractScheduleEntry
																							 .ContractScheduleRecords.Cache.CreateCopy(graphServiceContractScheduleEntry.ContractScheduleRecords.Current);

					fsContractScheduleRow.CustomerID = fsServiceContractRow.CustomerID;
					fsContractScheduleRow.CustomerLocationID = fsServiceContractRow.CustomerLocationID;
					fsContractScheduleRow.ProjectID = fsServiceContractRow.ProjectID;
					fsContractScheduleRow.DfltProjectTaskID = fsServiceContractRow.DfltProjectTaskID;

					bool needUpdateExcDate = false;

					if (fsContractScheduleRow.EndDate == (DateTime?)cache.GetValueOriginal<FSServiceContract.endDate>(fsServiceContractRow)
						&& fsContractScheduleRow.EndDate != fsServiceContractRow.EndDate)
					{
						fsContractScheduleRow.EndDate = fsServiceContractRow.EndDate;
						needUpdateExcDate = true;
					}

					if (fsContractScheduleRow.StartDate == (DateTime?)cache.GetValueOriginal<FSServiceContract.startDate>(fsServiceContractRow)
						&& fsContractScheduleRow.StartDate != fsServiceContractRow.StartDate)
					{
						fsContractScheduleRow.StartDate = fsServiceContractRow.StartDate;
						needUpdateExcDate = true;
					}

					if (needUpdateExcDate)
					{
						fsContractScheduleRow.NextExecutionDate = SharedFunctions.GetNextExecution(ContractSchedules.Cache, fsContractScheduleRow);
					}

					graphServiceContractScheduleEntry.ContractScheduleRecords.Update(fsContractScheduleRow);
				}

				graphServiceContractScheduleEntry.Save.Press();
			}
		}
		#endregion

		#region Event Handlers

		#region FSServiceContract

		#region FieldSelecting
		#endregion
		#region FieldDefaulting

		protected virtual void _(Events.FieldDefaulting<FSServiceContract, FSServiceContract.scheduleGenType> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceContract fsServiceContractRow = (FSServiceContract)e.Row;
            PXCache cache = e.Cache;

            if (fsServiceContractRow.ScheduleGenType != null)
            {
                switch (fsServiceContractRow.ScheduleGenType)
                {
                    case ID.ScheduleGenType_ServiceContract.NONE:

                        SharedFunctions.DefaultGenerationType(cache, fsServiceContractRow, e.Args);
                        break;

                    case ID.ScheduleGenType_ServiceContract.APPOINTMENT:

                    case ID.ScheduleGenType_ServiceContract.SERVICE_ORDER:

                        e.NewValue = fsServiceContractRow.ScheduleGenType;
                        e.Cancel = true;
                        break;

                    default:
                        break;
                }
            }
            else
            {
                SharedFunctions.DefaultGenerationType(cache, fsServiceContractRow, e.Args);
            }
        }

        protected virtual void _(Events.FieldDefaulting<FSServiceContract, FSServiceContract.endDate> e)
        {
			FSServiceContract row = e.Row;

			e.NewValue = GetEndDate(row, row.StartDate, (DateTime?)e.NewValue);
		}

		protected virtual void _(Events.FieldDefaulting<FSServiceContract, FSServiceContract.duration> e)
		{
			FSServiceContract row = e.Row;

			e.NewValue = GetDuration(row, (int?)e.NewValue);
		}
		#endregion
		#region FieldUpdating
		#endregion
		#region FieldVerifying
		#endregion
		#region FieldUpdated

		protected virtual void _(Events.FieldUpdated<FSServiceContract, FSServiceContract.branchID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceContract fSServiceContractRow = (FSServiceContract)e.Row;

            fSServiceContractRow.BranchLocationID = null;
        }

        protected virtual void _(Events.FieldUpdated<FSServiceContract, FSServiceContract.billingPeriod> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceContract fsServiceContractRow = (FSServiceContract)e.Row;

            SetBillingPeriod(fsServiceContractRow);
        }

        protected virtual void _(Events.FieldUpdated<FSServiceContract, FSServiceContract.projectID> e)
        {
			if (e.Row == null)
            {
                return;
            }

			if (ContractPeriodDetRecords != null)
			{
				foreach (FSContractPeriodDet fsContractPeriodDetRow in ContractPeriodDetRecords.Select())
				{
					ContractPeriodDetRecords.Cache.SetDefaultExt<FSContractPeriodDet.projectID>(fsContractPeriodDetRow);
				}
			}

			e.Cache.SetDefaultExt<FSServiceContract.dfltProjectTaskID>(e.Row);

			if (ProjectDefaultAttribute.IsNonProject(e.Row.ProjectID))
			{
				e.Cache.SetDefaultExt<FSServiceContract.dfltCostCodeID>(e.Row);
			}
		}

		protected virtual void _(Events.FieldUpdated<FSServiceContract, FSServiceContract.dfltProjectTaskID> e)
		{
			if (e.Row == null)
			{
				return;
			}

			if (ContractPeriodDetRecords != null)
			{
				foreach (FSContractPeriodDet fsContractPeriodDetRow in ContractPeriodDetRecords.Select())
				{
					int? originalProjectID = (int?)ContractPeriodDetRecords.Cache.GetValueOriginal<FSContractPeriodDet.projectID>(fsContractPeriodDetRow);
					if (fsContractPeriodDetRow.ProjectID != originalProjectID || fsContractPeriodDetRow.ProjectTaskID == null)
					{
						PMTask task = null;
						if (fsContractPeriodDetRow.ProjectID != null && e.Row.DfltProjectTaskID != null)
						{
							task = PXSelect<PMTask,
											Where<PMTask.projectID, Equal<Required<PMTask.projectID>>,
											And<PMTask.taskID, Equal<Required<PMTask.taskID>>>>>
											.Select(this, fsContractPeriodDetRow.ProjectID, e.Row.DfltProjectTaskID);
						}

						fsContractPeriodDetRow.ProjectTaskID = task?.TaskID;
						ContractPeriodDetRecords.Update(fsContractPeriodDetRow);
					}
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<FSServiceContract, FSServiceContract.dfltCostCodeID> e)
		{
			if (e.Row == null)
			{
				return;
			}

			if (ContractPeriodDetRecords != null)
			{
				foreach (FSContractPeriodDet fsContractPeriodDetRow in ContractPeriodDetRecords.Select())
				{
					if (ProjectDefaultAttribute.IsNonProject(e.Row.ProjectID)
							|| fsContractPeriodDetRow.CostCodeID == null)
					{
						fsContractPeriodDetRow.CostCodeID = e.Row.DfltCostCodeID;
						ContractPeriodDetRecords.Update(fsContractPeriodDetRow);
					}
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<FSServiceContract, FSServiceContract.startDate> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceContract fsServiceContractRow = (FSServiceContract)e.Row;

            UpdateSalesPrices(e.Cache, fsServiceContractRow);
            e.Cache.SetDefaultExt<FSServiceContract.endDate>(e.Row);
            SetBillingPeriod(fsServiceContractRow);
        }

        protected virtual void _(Events.FieldUpdated<FSServiceContract, FSServiceContract.endDate> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceContract fsServiceContractRow = (FSServiceContract)e.Row;

            if (fsServiceContractRow.UpcomingStatus == ID.Status_ServiceContract.EXPIRED)
            {
                fsServiceContractRow.StatusEffectiveUntilDate = fsServiceContractRow.EndDate;
            }

            SetBillingPeriod(fsServiceContractRow);

			if (fsServiceContractRow.DurationType == SC_Duration_Type.CUSTOM)
				e.Cache.SetDefaultExt<FSServiceContract.duration>(e.Row);
		}

        protected virtual void _(Events.FieldUpdated<FSServiceContract, FSServiceContract.status> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceContract fsServiceContractRow = (FSServiceContract)e.Row;
            string oldStatus = (string)e.OldValue;

            this.isStatusChanged = oldStatus != fsServiceContractRow.Status;
        }

        protected virtual void _(Events.FieldUpdated<FSServiceContract, FSServiceContract.customerID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceContract fsServiceContractRow = (FSServiceContract)e.Row;

            Location locationRowTemp;

            if (fsServiceContractRow.CustomerID == null)
            {
                locationRowTemp = null;
            }
            else
            {
                locationRowTemp = PXSelectJoin<Location,
                                  InnerJoin<BAccount,
                                  On<
                                      BAccount.bAccountID, Equal<Location.bAccountID>,
                                      And<BAccount.defLocationID, Equal<Location.locationID>>>>,
                                  Where<
                                      Location.bAccountID, Equal<Required<Location.bAccountID>>>>
                                  .Select(this, fsServiceContractRow.CustomerID);
            }

            if (locationRowTemp == null)
            {
                fsServiceContractRow.CustomerLocationID = null;
            }
            else
            {
                fsServiceContractRow.CustomerLocationID = locationRowTemp.LocationID;
            }

            SetBillCustomerAndLocationID(e.Cache, fsServiceContractRow);

			e.Cache.SetDefaultExt<FSServiceContract.projectID>(e.Row);
		}

        protected virtual void _(Events.FieldUpdated<FSServiceContract, FSServiceContract.sourcePrice> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceContract fsServiceContractRow = (FSServiceContract)e.Row;
            UpdateSalesPrices(e.Cache, fsServiceContractRow);
        }

        protected virtual void _(Events.FieldUpdated<FSServiceContract, FSServiceContract.expirationType> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceContract fsServiceContractRow = (FSServiceContract)e.Row;
            e.Cache.SetDefaultExt<FSServiceContract.endDate>(e.Row);
            SetUpcommingStatus(fsServiceContractRow);
            SetEffectiveUntilDate(e.Cache, fsServiceContractRow);
        }

        protected virtual void _(Events.FieldUpdated<FSServiceContract, FSServiceContract.billingType> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceContract fsServiceContractRow = (FSServiceContract)e.Row;

            if (fsServiceContractRow.BillingType == FSServiceContract.billingType.Values.PerformedBillings && ContractPeriodFilter.Current != null)
            {
                ContractPeriodFilter.Current.ContractPeriodID = null;
                ContractPeriodFilter.Cache.SetDefaultExt<FSContractPeriodFilter.actions>(ContractPeriodFilter.Current);
            }

            if (fsServiceContractRow.BillingType == FSServiceContract.billingType.Values.StandardizedBillings
                    || fsServiceContractRow.IsFixedRateContract == true)
            {
                //No need to insert a new FSContractPeriod when coming from either of these options.
                if ((string)e.OldValue != FSServiceContract.billingType.Values.StandardizedBillings
                    && (string)e.OldValue != FSServiceContract.billingType.Values.FixedRateBillings
                    && (string)e.OldValue != FSServiceContract.billingType.Values.FixedRateAsPerformedBillings)
                {
                    FSContractPeriod fsContractPeriodRow = new FSContractPeriod();
                    fsContractPeriodRow.StartPeriodDate = fsServiceContractRow.StartDate ?? Accessinfo.BusinessDate;
                    DateTime? endPeriodDate = GetContractPeriodEndDate(fsServiceContractRow, fsContractPeriodRow.StartPeriodDate);

                    if (endPeriodDate != null)
                    {
                        fsContractPeriodRow.EndPeriodDate = endPeriodDate.Value;
                        ContractPeriodRecords.Current = ContractPeriodRecords.Insert(fsContractPeriodRow);
                    }
                }

                ContractPeriodFilter.SetValueExt<FSContractPeriodFilter.actions>(ContractPeriodFilter.Current, ID.ContractPeriod_Actions.MODIFY_UPCOMING_BILLING_PERIOD);  
            }
            else
            {
                if (ContractPeriodRecords.Current != null)
                {
                    ContractPeriodRecords.Delete(ContractPeriodRecords.Current);
                }
            }
        }

        protected virtual void _(Events.FieldUpdated<FSServiceContract, FSServiceContract.billTo> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceContract fsServiceContractRow = (FSServiceContract)e.Row;
            bool isCustomerAcct = (string)e.NewValue == ID.Contract_BillTo.CUSTOMERACCT;

            if (isCustomerAcct == true)
            {
                e.Cache.SetValueExt<FSServiceContract.billCustomerID>(fsServiceContractRow, fsServiceContractRow.CustomerID);
                fsServiceContractRow.BillLocationID = fsServiceContractRow.CustomerLocationID;
            }
        }

        protected virtual void _(Events.FieldUpdated<FSServiceContract, FSServiceContract.billCustomerID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceContract fsServiceContractRow = (FSServiceContract)e.Row;
            fsServiceContractRow.BillLocationID = null;
        }
		protected virtual void _(Events.FieldUpdated<FSServiceContract, FSServiceContract.duration> e)
		{
			if (e.Row == null)
				return;

			FSServiceContract fsServiceContractRow = (FSServiceContract)e.Row;

			if (fsServiceContractRow.DurationType != SC_Duration_Type.CUSTOM)
				e.Cache.SetDefaultExt<FSServiceContract.endDate>(e.Row);
		}
		#endregion

		protected virtual void _(Events.RowSelecting<FSServiceContract> e)
        {
			if (e.Row == null)
			{
				return;
			}

			e.Row.HasForecast = false;

			using (new PXConnectionScope())
			{
				var result = PXSelectReadonly<FSContractForecast,
									Where<
										FSContractForecast.active, Equal<True>,
										And<FSContractForecast.serviceContractID, Equal<Required<FSContractForecast.serviceContractID>>>>>
									.Select(this, e.Row.ServiceContractID);

				e.Row.HasForecast = result.Count() > 0;
			}
		}

        protected virtual void _(Events.RowSelected<FSServiceContract> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceContract fsServiceContractRow = (FSServiceContract)e.Row;
            PXCache cache = e.Cache;

            SetVisibleActivatePeriodButton(cache, fsServiceContractRow);
            EnableDisable_ActionButtons(this, cache, fsServiceContractRow);
            SetVisibleContractBillingSettings(cache, fsServiceContractRow);
            EnableDisable_Document(cache, fsServiceContractRow);
            SetUsageBillingCycle(fsServiceContractRow);
            SetBillInfo(cache, fsServiceContractRow);

            this.SalesPriceLines.AllowSelect = fsServiceContractRow.Mem_ShowPriceTab == true;
            this.SalesPriceLines.AllowSelect = fsServiceContractRow.Mem_ShowPriceTab == true;
            this.ServiceContractSelected.AllowSelect = fsServiceContractRow.Mem_ShowScheduleTab == false;
			
            PXUIFieldAttribute.SetEnabled<FSServiceContract.customerID>(cache, e.Row, fsServiceContractRow.Status == ID.Status_ServiceContract.DRAFT);

            PXUIFieldAttribute.SetEnabled<FSServiceContract.projectID>(cache,
                                                                       fsServiceContractRow,
																	   fsServiceContractRow.Status == ID.Status_ServiceContract.DRAFT);

            bool isFixedRateBilling = fsServiceContractRow.IsFixedRateContract == true;
            PXUIFieldAttribute.SetVisible<FSContractPeriodDet.deferredCode>(ContractPeriodDetRecords.Cache, null, isFixedRateBilling);
            PXUIFieldAttribute.SetEnabled<FSContractPeriodDet.deferredCode>(ContractPeriodDetRecords.Cache, null, isFixedRateBilling);

            PXUIFieldAttribute.SetVisible<FSContractPeriodDet.overageItemPrice>(ContractPeriodDetRecords.Cache, null, !isFixedRateBilling);
            PXUIFieldAttribute.SetEnabled<FSContractPeriodDet.overageItemPrice>(ContractPeriodDetRecords.Cache, null, !isFixedRateBilling);

            PXUIFieldAttribute.SetVisible<FSContractPeriodDet.remainingAmount>(ContractPeriodDetRecords.Cache, null, !isFixedRateBilling);
            PXUIFieldAttribute.SetEnabled<FSContractPeriodDet.remainingAmount>(ContractPeriodDetRecords.Cache, null, !isFixedRateBilling);

            PXUIFieldAttribute.SetVisible<FSContractPeriodDet.usedAmount>(ContractPeriodDetRecords.Cache, null, !isFixedRateBilling);
            PXUIFieldAttribute.SetEnabled<FSContractPeriodDet.usedAmount>(ContractPeriodDetRecords.Cache, null, !isFixedRateBilling);

            PXUIFieldAttribute.SetVisible<FSContractPeriodDet.scheduledAmount>(ContractPeriodDetRecords.Cache, null, !isFixedRateBilling);
            PXUIFieldAttribute.SetEnabled<FSContractPeriodDet.scheduledAmount>(ContractPeriodDetRecords.Cache, null, !isFixedRateBilling);

            SharedFunctions.SetVisibleEnableProjectField<FSServiceContract.dfltProjectTaskID>(cache, fsServiceContractRow, fsServiceContractRow.ProjectID);

			if (PXAccess.FeatureInstalled<FeaturesSet.costCodes>() == true)
				SharedFunctions.SetVisibleEnableProjectField<FSServiceContract.dfltCostCodeID>(cache, fsServiceContractRow, fsServiceContractRow.ProjectID);
		}

        protected virtual void _(Events.RowInserting<FSServiceContract> e)
        {
        }

        protected virtual void _(Events.RowInserted<FSServiceContract> e)
        {
        }

        protected virtual void _(Events.RowUpdating<FSServiceContract> e)
        {
        }

        protected virtual void _(Events.RowUpdated<FSServiceContract> e)
        {
        }

        protected virtual void _(Events.RowDeleting<FSServiceContract> e)
        {
        }

        protected virtual void _(Events.RowDeleted<FSServiceContract> e)
        {
        }

        protected virtual void _(Events.RowPersisting<FSServiceContract> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceContract fsServiceContractRow = (FSServiceContract)e.Row;
            PXCache cache = e.Cache;

            if (Accessinfo.ScreenID != ID.ScreenID.SERVICE_CONTRACT)
            {
                cache.AllowUpdate = true;
            }

            if (fsServiceContractRow.ExpirationType == ID.Contract_ExpirationType.UNLIMITED)
            {
                fsServiceContractRow.EndDate = null;
            }

            PXResultset<FSContractSchedule> contractRows = ContractSchedules.Select();

            ValidateDates(cache, fsServiceContractRow, contractRows);

            SetUnitPriceForSalesPricesRows(fsServiceContractRow);

			if (e.Operation == PXDBOperation.Delete)
            {
                PXResultset<FSSchedule> bqlResultSet = PXSelect<FSSchedule,
                                                       Where<
                                                           FSSchedule.entityID, Equal<Required<FSSchedule.entityID>>>>
                                                       .Select(this, fsServiceContractRow.ServiceContractID);

                ServiceContractScheduleEntryBase<RouteServiceContractScheduleEntry,
                                                 FSSchedule,
                                                 FSSchedule.scheduleID,
                                                 FSSchedule.entityID,
                                                 FSSchedule.customerID> routeServiceContractScheduleEntry = PXGraph.CreateInstance<ServiceContractScheduleEntryBase<RouteServiceContractScheduleEntry, FSSchedule, FSSchedule.scheduleID, FSSchedule.entityID, FSSchedule.customerID>>();

                foreach (FSSchedule fsScheduleRow in bqlResultSet)
                {
                    routeServiceContractScheduleEntry.ContractScheduleRecords.Current = fsScheduleRow;
                    routeServiceContractScheduleEntry.ContractScheduleRecords.Delete(fsScheduleRow);
                    routeServiceContractScheduleEntry.Save.Press();
                }

                //Detaching ServiceOrders and Appointments created by schedule generation linked to this ServiceContract.
                PXUpdate<
                    Set<FSServiceOrder.serviceContractID, Required<FSServiceOrder.serviceContractID>>,
                FSServiceOrder,
                Where<
                    FSServiceOrder.serviceContractID, Equal<Required<FSServiceOrder.serviceContractID>>>>
                .Update(this, null, fsServiceContractRow.ServiceContractID);

                PXUpdate<
                    Set<FSAppointment.serviceContractID, Required<FSAppointment.serviceContractID>>,
                FSAppointment,
                Where<
                    FSAppointment.serviceContractID, Equal<Required<FSAppointment.serviceContractID>>>>
                .Update(this, null, fsServiceContractRow.ServiceContractID);
            }
        }

        protected virtual void _(Events.RowPersisted<FSServiceContract> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSServiceContract fsServiceContractRow = (FSServiceContract)e.Row;

            string scheduleGenTypeOriginal = (string)e.Cache.GetValueOriginal<FSServiceContract.scheduleGenType>(fsServiceContractRow);
            int? origBranchID = (int?)e.Cache.GetValueOriginal<FSServiceContract.branchID>(fsServiceContractRow);
            int? origBranchLocationID = (int?)e.Cache.GetValueOriginal<FSServiceContract.branchLocationID>(fsServiceContractRow);

            if (e.TranStatus == PXTranStatus.Open
                    && (e.Operation == PXDBOperation.Insert
                            || e.Operation == PXDBOperation.Update))
            {
                InsertContractAction(fsServiceContractRow, e.Operation);
                InsertContractActionBySchedules(e.Operation);
            }

            if ((fsServiceContractRow.BillingType == FSServiceContract.billingType.Values.StandardizedBillings
                || fsServiceContractRow.IsFixedRateContract == true)
                && e.TranStatus == PXTranStatus.Completed)
            {
                ContractPeriodFilter.Cache.SetDefaultExt<FSContractPeriodFilter.contractPeriodID>(ContractPeriodFilter.Current);
            }

            if (e.TranStatus == PXTranStatus.Open
                && e.Operation == PXDBOperation.Update)
            {
                if (fsServiceContractRow.BranchID != origBranchID
                        || fsServiceContractRow.BranchLocationID != origBranchLocationID)
                {
                    PXUpdate<
                        Set<FSSchedule.branchID, Required<FSSchedule.branchID>,
                        Set<FSSchedule.branchLocationID, Required<FSSchedule.branchLocationID>>>,
                    FSSchedule,
                    Where<
                        FSSchedule.customerID, Equal<Required<FSSchedule.customerID>>,
                    And<
                        FSSchedule.entityID, Equal<Required<FSSchedule.entityID>>>>>
                    .Update(this,
                            fsServiceContractRow.BranchID,
                            fsServiceContractRow.BranchLocationID,
                            fsServiceContractRow.CustomerID,
                            fsServiceContractRow.ServiceContractID);
                }

                if (fsServiceContractRow.ScheduleGenType != scheduleGenTypeOriginal)
                {
                    PXUpdate<
                        Set<FSSchedule.scheduleGenType, Required<FSSchedule.scheduleGenType>>,
                    FSSchedule,
                    Where<
                        FSSchedule.customerID, Equal<Required<FSSchedule.customerID>>,
                    And<
                        FSSchedule.entityID, Equal<Required<FSSchedule.entityID>>>>>
                    .Update(this,
                            fsServiceContractRow.ScheduleGenType,
                            fsServiceContractRow.CustomerID,
                            fsServiceContractRow.ServiceContractID);

                    if (fsServiceContractRow.ScheduleGenType == ID.ScheduleGenType_ServiceContract.NONE)
                    {
                        PXUpdate<
                            Set<FSSchedule.active, Required<FSSchedule.active>>,
                        FSSchedule,
                        Where<
                            FSSchedule.customerID, Equal<Required<FSSchedule.customerID>>,
                        And<
                            FSSchedule.entityID, Equal<Required<FSSchedule.entityID>>>>>
                        .Update(this,
                                false,
                                fsServiceContractRow.CustomerID,
                                fsServiceContractRow.ServiceContractID);
                    }

                    ContractSchedules.Cache.Clear();
                    ContractSchedules.View.Clear();
                    ContractSchedules.View.RequestRefresh();
                }

				UpdateSchedules(e.Cache, fsServiceContractRow, this.IsRenewContract);
			}
        }

        #endregion
        #region FSSalesPrice

        #region FieldSelecting
        #endregion
        #region FieldDefaulting
        #endregion
        #region FieldUpdating
        #endregion
        #region FieldVerifying
        #endregion
        #region FieldUpdated
        #endregion

        protected virtual void _(Events.RowSelecting<FSSalesPrice> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSSalesPrice fsSalesPriceRow = (FSSalesPrice)e.Row;

            if (ServiceContractRecords.Current != null)
            {
                bool isContract = ServiceContractRecords.Current.SourcePrice == ID.SourcePrice.CONTRACT;

                if (isContract)
                {
                    fsSalesPriceRow.Mem_UnitPrice = fsSalesPriceRow.UnitPrice;
                }
                else
                {
                    fsSalesPriceRow.Mem_UnitPrice = GetSalesPrice(e.Cache, fsSalesPriceRow) ?? 0.0m;
                }
            }
        }

        protected virtual void _(Events.RowSelected<FSSalesPrice> e)
        {
            if (e.Row == null || ServiceContractRecords.Current == null)
            {
                return;
            }

            FSSalesPrice fsSalesPriceRow = (FSSalesPrice)e.Row;

            bool isContract = ServiceContractRecords.Current.SourcePrice == ID.SourcePrice.CONTRACT;

            PXUIFieldAttribute.SetEnabled<FSSalesPrice.mem_UnitPrice>(e.Cache, fsSalesPriceRow, isContract);
        }

        protected virtual void _(Events.RowInserting<FSSalesPrice> e)
        {
        }

        protected virtual void _(Events.RowInserted<FSSalesPrice> e)
        {
        }

        protected virtual void _(Events.RowUpdating<FSSalesPrice> e)
        {
        }

        protected virtual void _(Events.RowUpdated<FSSalesPrice> e)
        {
        }

        protected virtual void _(Events.RowDeleting<FSSalesPrice> e)
        {
        }

        protected virtual void _(Events.RowDeleted<FSSalesPrice> e)
        {
        }

        protected virtual void _(Events.RowPersisting<FSSalesPrice> e)
        {
        }

        protected virtual void _(Events.RowPersisted<FSSalesPrice> e)
        {
        }

        #endregion
        #region FSContractPeriodFilter

        #region FieldSelecting
        #endregion
        #region FieldDefaulting

        protected virtual void _(Events.FieldDefaulting<FSContractPeriodFilter, FSContractPeriodFilter.actions> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSContractPeriodFilter fsContractPeriodFilterRow = (FSContractPeriodFilter)e.Row;

            if (ServiceContractSelected.Current != null)
            {
                e.NewValue = GetContractPeriodFilterDefaultAction(this, ServiceContractSelected.Current.ServiceContractID);
            }
        }

        #endregion
        #region FieldUpdating
        #endregion
        #region FieldVerifying
        #endregion
        #region FieldUpdated

        protected virtual void _(Events.FieldUpdated<FSContractPeriodFilter, FSContractPeriodFilter.actions> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSContractPeriodFilter fsContractPeriodFilterRow = (FSContractPeriodFilter)e.Row;

            e.Cache.SetDefaultExt<FSContractPeriodFilter.contractPeriodID>(e.Row);
        }

        #endregion

        protected virtual void _(Events.RowSelecting<FSContractPeriodFilter> e)
        {
        }

        protected virtual void _(Events.RowSelected<FSContractPeriodFilter> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSContractPeriodFilter fsContractPeriodFilterRow = (FSContractPeriodFilter)e.Row;
            PXCache cache = e.Cache;

            PXUIFieldAttribute.SetEnabled<FSContractPeriodFilter.contractPeriodID>(cache, fsContractPeriodFilterRow, fsContractPeriodFilterRow.Actions == ID.ContractPeriod_Actions.SEARCH_BILLING_PERIOD);
            PXUIFieldAttribute.SetVisible<FSContractPeriodFilter.postDocRefNbr>(cache, fsContractPeriodFilterRow, fsContractPeriodFilterRow.Actions == ID.ContractPeriod_Actions.SEARCH_BILLING_PERIOD);

            FSContractPeriod fsContractPeriodSelectedRow = ContractPeriodRecords.Current;
            FSContractPostDoc fsContractPostDocRow = ContractPostDocRecords.Current;

            if (fsContractPeriodSelectedRow == null)
            {
                fsContractPeriodSelectedRow = ContractPeriodRecords.Current = ContractPeriodRecords.Select();
                ContractPeriodFilter.Cache.SetDefaultExt<FSContractPeriodFilter.contractPeriodID>(ContractPeriodFilter.Current);
            }

            if (fsContractPeriodSelectedRow != null && fsContractPeriodSelectedRow.ContractPeriodID != fsContractPeriodFilterRow.ContractPeriodID)
            {
                fsContractPeriodSelectedRow = ContractPeriodRecords.Current = ContractPeriodRecords.Select();
            }

            if (fsContractPostDocRow == null && fsContractPeriodSelectedRow != null && fsContractPeriodSelectedRow.Invoiced == true)
            {
                fsContractPostDocRow = ContractPostDocRecords.Current = ContractPostDocRecords.Select();
            }

            if (fsContractPostDocRow != null && fsContractPostDocRow.ContractPeriodID != fsContractPeriodFilterRow.ContractPeriodID)
            {
                fsContractPostDocRow = ContractPostDocRecords.Current = ContractPostDocRecords.Select();
            }

            bool allowInsertUpdateDelete = fsContractPeriodSelectedRow != null
                                                && ((ServiceContractRecords.Current.BillingType == FSServiceContract.billingType.Values.StandardizedBillings
                                                        && fsContractPeriodFilterRow.Actions != ID.ContractPeriod_Actions.SEARCH_BILLING_PERIOD
                                                        && fsContractPeriodSelectedRow.Status == ID.Status_ContractPeriod.INACTIVE
                                                        && ServiceContractRecords.Current.isEditable())
                                                 || (ServiceContractRecords.Current.IsFixedRateContract == true
                                                        && ((fsContractPeriodFilterRow.Actions == ID.ContractPeriod_Actions.SEARCH_BILLING_PERIOD
                                                            && (fsContractPeriodSelectedRow.Status == ID.Status_ContractPeriod.ACTIVE
                                                                || fsContractPeriodSelectedRow.Status == ID.Status_ContractPeriod.PENDING))
                                                           || fsContractPeriodFilterRow.Actions == ID.ContractPeriod_Actions.MODIFY_UPCOMING_BILLING_PERIOD)));

            this.ContractPeriodDetRecords.Cache.AllowUpdate = allowInsertUpdateDelete;
            this.ContractPeriodDetRecords.Cache.AllowInsert = allowInsertUpdateDelete;
            this.ContractPeriodDetRecords.Cache.AllowDelete = allowInsertUpdateDelete;

            if (fsContractPeriodSelectedRow != null && fsContractPeriodFilterRow.ContractPeriodID != null)
            {
                if (fsContractPostDocRow != null)
                {
                    fsContractPeriodFilterRow.PostDocRefNbr = fsContractPostDocRow.PostRefNbr;
                }
                else
                {
                    fsContractPeriodFilterRow.PostDocRefNbr = string.Empty;
                }

                fsContractPeriodFilterRow.StandardizedBillingTotal = fsContractPeriodSelectedRow.PeriodTotal;
            }
            else
            {
                fsContractPeriodFilterRow.PostDocRefNbr = string.Empty;
                fsContractPeriodFilterRow.StandardizedBillingTotal = 0;
            }

            activatePeriod.SetEnabled(EnableDisableActivatePeriodButton(ServiceContractRecords.Current, ContractPeriodRecords.Current));
        }

        protected virtual void _(Events.RowInserting<FSContractPeriodFilter> e)
        {
        }

        protected virtual void _(Events.RowInserted<FSContractPeriodFilter> e)
        {
        }

        protected virtual void _(Events.RowUpdating<FSContractPeriodFilter> e)
        {
        }

        protected virtual void _(Events.RowUpdated<FSContractPeriodFilter> e)
        {
        }

        protected virtual void _(Events.RowDeleting<FSContractPeriodFilter> e)
        {
        }

        protected virtual void _(Events.RowDeleted<FSContractPeriodFilter> e)
        {
        }

        protected virtual void _(Events.RowPersisting<FSContractPeriodFilter> e)
        {
        }

        protected virtual void _(Events.RowPersisted<FSContractPeriodFilter> e)
        {
        }

        #endregion
        #region FSContractPeriodDet

        #region FieldSelecting

        protected virtual void _(Events.FieldSelecting<FSContractPeriodDet, FSContractPeriodDet.amount> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSContractPeriodDet fsContractPeriodDetRow = (FSContractPeriodDet)e.Row;
            AmountFieldSelectingHandler(e.Cache, e.Args, typeof(FSContractPeriodDet.amount).Name, fsContractPeriodDetRow.BillingRule, fsContractPeriodDetRow.Time, fsContractPeriodDetRow.Qty);
        }

        protected virtual void _(Events.FieldSelecting<FSContractPeriodDet, FSContractPeriodDet.usedAmount> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSContractPeriodDet fsContractPeriodDetRow = (FSContractPeriodDet)e.Row;
            AmountFieldSelectingHandler(e.Cache, e.Args, typeof(FSContractPeriodDet.usedAmount).Name, fsContractPeriodDetRow.BillingRule, fsContractPeriodDetRow.UsedTime, fsContractPeriodDetRow.UsedQty);
        }

        protected virtual void _(Events.FieldSelecting<FSContractPeriodDet, FSContractPeriodDet.remainingAmount> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSContractPeriodDet fsContractPeriodDetRow = (FSContractPeriodDet)e.Row;
            AmountFieldSelectingHandler(e.Cache, e.Args, typeof(FSContractPeriodDet.remainingAmount).Name, fsContractPeriodDetRow.BillingRule, fsContractPeriodDetRow.RemainingTime, fsContractPeriodDetRow.RemainingQty);
        }

        protected virtual void _(Events.FieldSelecting<FSContractPeriodDet, FSContractPeriodDet.scheduledAmount> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSContractPeriodDet fsContractPeriodDetRow = (FSContractPeriodDet)e.Row;
            AmountFieldSelectingHandler(e.Cache, e.Args, typeof(FSContractPeriodDet.scheduledAmount).Name, fsContractPeriodDetRow.BillingRule, fsContractPeriodDetRow.ScheduledTime, fsContractPeriodDetRow.ScheduledQty);
        }

        #endregion
        #region FieldDefaulting

        protected virtual void _(Events.FieldDefaulting<FSContractPeriodDet, FSContractPeriodDet.deferredCode> e)
        {
            if (e.Row == null || ServiceContractRecords.Current == null)
            {
                return;
            }

            FSServiceContract serviceContract = ServiceContractRecords.Current;
            if(serviceContract.IsFixedRateContract == true)
            {
                InventoryItem item = (InventoryItem)PXSelectorAttribute.Select<FSContractPeriodDet.inventoryID>(e.Cache, e.Row);
                e.NewValue = item?.DeferredCode;
            }
        }

        protected virtual void _(Events.FieldDefaulting<FSContractPeriodDet, FSContractPeriodDet.qty> e)
        {
            if (e.Row == null)
            {
                return;
            }

            InventoryItem inventoryItemRow = (InventoryItem)PXSelectorAttribute.Select<FSContractPeriodDet.inventoryID>(e.Cache, e.Row);
            if (inventoryItemRow == null)
            {
                return;
            }

            FSxService fsxServiceRow = PXCache<InventoryItem>.GetExtension<FSxService>(inventoryItemRow);
            string billingRule = e.Row.BillingRule ?? fsxServiceRow.BillingRule;

            if (billingRule == ID.BillingRule.TIME)
            {
                e.NewValue = decimal.Divide((decimal)(e.Row.Time ?? 0), 60);
                e.Cancel = true;
            }
        }

		protected virtual void _(Events.FieldDefaulting<FSContractPeriodDet, FSContractPeriodDet.costCodeID> e)
		{
			if (e.Row == null || ServiceContractRecords.Current == null)
			{
				return;
			}

			if (!ProjectDefaultAttribute.IsNonProject(ServiceContractRecords.Current.ProjectID)
					&& PXAccess.FeatureInstalled<FeaturesSet.costCodes>())
			{
				e.NewValue = ServiceContractRecords.Current.DfltCostCodeID;
				e.Cancel = true;
			}
		}

		#endregion
		#region FieldUpdating
		#endregion
		#region FieldVerifying
		#endregion
		#region FieldUpdated

		protected virtual void _(Events.FieldUpdated<FSContractPeriodDet, FSContractPeriodDet.billingRule> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSContractPeriodDet fsContractPeriodDetRow = (FSContractPeriodDet)e.Row;
            SetDefaultQtyTime(e.Cache, fsContractPeriodDetRow);
        }

        protected virtual void _(Events.FieldUpdated<FSContractPeriodDet, FSContractPeriodDet.amount> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSContractPeriodDet fsContractPeriodDetRow = (FSContractPeriodDet)e.Row;
            PXCache cache = e.Cache;

            if (fsContractPeriodDetRow.BillingRule == ID.BillingRule.TIME)
            {
                int time = 0;

                if (int.TryParse(fsContractPeriodDetRow.Amount.Replace(" ", "0"), out time))
                {
                    int minutes = time % 100;
                    int hours = (time - minutes) / 100;
                    TimeSpan span = new TimeSpan(0, hours, minutes, 0);
                    cache.SetValueExt<FSContractPeriodDet.time>(fsContractPeriodDetRow, (int)span.TotalMinutes);
                }
            }
            else if (fsContractPeriodDetRow.BillingRule == ID.BillingRule.FLAT_RATE)
            {
                decimal qty = 0.0m;
                decimal.TryParse(fsContractPeriodDetRow.Amount, out qty);
                cache.SetValueExt<FSContractPeriodDet.qty>(fsContractPeriodDetRow, qty);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSContractPeriodDet, FSContractPeriodDet.inventoryID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSContractPeriodDet fsContractPeriodDetRow = (FSContractPeriodDet)e.Row;
            PXCache cache = e.Cache;

            SetDefaultBillingRule(cache, fsContractPeriodDetRow);
            cache.SetDefaultExt<FSContractPeriodDet.uOM>(e.Row);
            SetRegularPrice(cache, fsContractPeriodDetRow, ServiceContractRecords.Current);

            cache.SetValueExt<FSContractPeriodDet.recurringUnitPrice>(fsContractPeriodDetRow, fsContractPeriodDetRow.RegularPrice);
            cache.SetValueExt<FSContractPeriodDet.overageItemPrice>(fsContractPeriodDetRow, fsContractPeriodDetRow.RegularPrice);
        }

        #endregion

        protected virtual void _(Events.RowSelecting<FSContractPeriodDet> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSContractPeriodDet fsContractPeriodDetRow = (FSContractPeriodDet)e.Row;
            using (new PXConnectionScope())
            {
                SetRegularPrice(e.Cache, fsContractPeriodDetRow, ServiceContractRecords.Current);

                var fsSODetRows = PXSelectJoin<FSSODet,
                                  InnerJoin<FSServiceOrder,
                                  On<
                                      FSServiceOrder.sOID, Equal<FSSODet.sOID>>>,
                                  Where2<
                                      Where<
                                          FSServiceOrder.billServiceContractID, Equal<Required<FSServiceOrder.billServiceContractID>>,
                                          And<FSServiceOrder.billContractPeriodID, Equal<Required<FSServiceOrder.billContractPeriodID>>,
                                          And<FSServiceOrder.canceled, Equal<False>,
                                          And<FSServiceOrder.allowInvoice, Equal<False>,
                                          And<FSSODet.status, NotEqual<FSSODet.status.Canceled>>>>>>,
                                      And<
                                          Where2<
                                              Where<
                                                  FSSODet.inventoryID, Equal<Required<FSSODet.inventoryID>>,
                                                  And<FSSODet.contractRelated, Equal<True>>>,
                                              And<
                                                  Where2<
                                                      Where<
                                                          FSSODet.billingRule, Equal<Required<FSSODet.billingRule>>,
                                                          Or<Required<FSSODet.billingRule>, IsNull>>,
                                                      And<
                                                          Where<
                                                              FSSODet.SMequipmentID, Equal<Required<FSSODet.SMequipmentID>>,
                                                              Or<Required<FSSODet.SMequipmentID>, IsNull>>>>>>>>>
                                  .Select(this, fsContractPeriodDetRow.ServiceContractID,
                                          fsContractPeriodDetRow.ContractPeriodID, fsContractPeriodDetRow.InventoryID,
                                          fsContractPeriodDetRow.BillingRule, fsContractPeriodDetRow.BillingRule,
                                          fsContractPeriodDetRow.SMEquipmentID, fsContractPeriodDetRow.SMEquipmentID)
                                  .AsEnumerable();

                var fsAppointmentDetRows = PXSelectJoin<FSAppointmentDet,
                                           InnerJoin<FSAppointment,
                                           On<
                                               FSAppointment.appointmentID, Equal<FSAppointmentDet.appointmentID>>,
                                           InnerJoin<FSSODet,
                                           On<
                                               FSSODet.sODetID, Equal<FSAppointmentDet.sODetID>>>>,
                                           Where2<
                                               Where<
                                                   FSAppointment.billServiceContractID, Equal<Required<FSAppointment.billServiceContractID>>,
                                                   And<FSAppointment.billContractPeriodID, Equal<Required<FSAppointment.billContractPeriodID>>,
                                                   And<FSAppointment.closed, Equal<False>,
                                                   And<FSAppointment.canceled, Equal<False>,
                                                   And<FSAppointmentDet.isCanceledNotPerformed, NotEqual<True>>>>>>,
                                               And<
                                                   Where2<
                                                       Where<
                                                           FSAppointmentDet.inventoryID, Equal<Required<FSAppointmentDet.inventoryID>>,
                                                           And<FSAppointmentDet.contractRelated, Equal<True>>>,
                                                       And<
                                                           Where2<
                                                               Where<
                                                                   FSSODet.billingRule, Equal<Required<FSSODet.billingRule>>,
                                                                   Or<Required<FSSODet.billingRule>, IsNull>>,
                                                               And<
                                                                   Where<
                                                                       FSAppointmentDet.SMequipmentID, Equal<Required<FSAppointmentDet.SMequipmentID>>,
                                                                       Or<Required<FSAppointmentDet.SMequipmentID>, IsNull>>>>>>>>>
                                           .Select(this, fsContractPeriodDetRow.ServiceContractID,
                                                   fsContractPeriodDetRow.ContractPeriodID, fsContractPeriodDetRow.InventoryID,
                                                   fsContractPeriodDetRow.BillingRule, fsContractPeriodDetRow.BillingRule,
                                                   fsContractPeriodDetRow.SMEquipmentID, fsContractPeriodDetRow.SMEquipmentID)
                                           .AsEnumerable();

                decimal? qtySum = 0;

                if (fsSODetRows.Count() > 0 || fsAppointmentDetRows.Count() > 0)
                {
                    qtySum = fsSODetRows.Sum(x => ((FSSODet)x).EstimatedQty);

                    foreach (PXResult<FSAppointmentDet, FSAppointment, FSSODet> row in fsAppointmentDetRows)
                    {
                        FSAppointmentDet fsAppointmentDetRow = (FSAppointmentDet)row;
                        FSAppointment fsAppointmentRow = (FSAppointment)row;

                        if (fsAppointmentRow.NotStarted == true)
                        {
                            qtySum += fsAppointmentDetRow.EstimatedQty;
                        }
                        else
                        {
                            qtySum += fsAppointmentDetRow.ActualQty;
                        }
                    }
                }

                if (fsContractPeriodDetRow.BillingRule == ID.BillingRule.FLAT_RATE)
                {
                    fsContractPeriodDetRow.ScheduledQty = qtySum;
                }
                else if (fsContractPeriodDetRow.BillingRule == ID.BillingRule.TIME)
                {
                    fsContractPeriodDetRow.ScheduledTime = (int?)(qtySum * 60);
                }
            }
        }

        protected virtual void _(Events.RowSelected<FSContractPeriodDet> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSContractPeriodDet fsContractPeriodDetRow = (FSContractPeriodDet)e.Row;
            EnableDisableContractPeriodDet(e.Cache, fsContractPeriodDetRow);

            SharedFunctions.SetEnableCostCodeProjectTask<FSContractPeriodDet.projectTaskID, FSContractPeriodDet.costCodeID>(e.Cache, fsContractPeriodDetRow, fsContractPeriodDetRow.LineType, ServiceContractRecords.Current?.ProjectID);
        }

        protected virtual void _(Events.RowInserting<FSContractPeriodDet> e)
        {
        }

        protected virtual void _(Events.RowInserted<FSContractPeriodDet> e)
        {
        }

        protected virtual void _(Events.RowUpdating<FSContractPeriodDet> e)
        {
        }

        protected virtual void _(Events.RowUpdated<FSContractPeriodDet> e)
        {
        }

        protected virtual void _(Events.RowDeleting<FSContractPeriodDet> e)
        {
        }

        protected virtual void _(Events.RowDeleted<FSContractPeriodDet> e)
        {
        }

        protected virtual void _(Events.RowPersisting<FSContractPeriodDet> e)
        {
        }

        protected virtual void _(Events.RowPersisted<FSContractPeriodDet> e)
        {
        }

        #endregion
        #region PrepaidContractFilters
        protected virtual void _(Events.FieldUpdated<FSActivationContractFilter, FSActivationContractFilter.activationDate> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSActivationContractFilter filter = (FSActivationContractFilter)e.Row;
            PXCache cache = e.Cache;

            if (filter.ActivationDate.HasValue == true)
            {
                if (filter.ActivationDate.Value.Date < this.Accessinfo.BusinessDate.Value.Date)
                {
                    cache.RaiseExceptionHandling<FSActivationContractFilter.activationDate>(filter,
                                                                                            filter.ActivationDate,
                                                                                            new PXSetPropertyException(TX.Error.EFFECTIVE_DATE_LOWER_ACTUAL_DATE, PXErrorLevel.Error));
                }

                if (ServiceContractRecords.Current.ExpirationType == ID.Contract_ExpirationType.EXPIRING
                        && filter.ActivationDate.Value.Date >= ServiceContractRecords.Current.EndDate)
                {
                    cache.RaiseExceptionHandling<FSActivationContractFilter.activationDate>(filter,
                                                                                            filter.ActivationDate,
                                                                                            new PXSetPropertyException(TX.Error.EFFECTIVE_DATE_GREATER_END_DATE, PXErrorLevel.Error));
                }

                foreach (ActiveSchedule activeScheduleRow in ActiveScheduleRecords.Select())
                {
                    if (activeScheduleRow.ChangeRecurrence == true)
                    {
                        ActiveScheduleRecords.Cache.SetValueExt<ActiveSchedule.effectiveRecurrenceStartDate>(activeScheduleRow, ActivationContractFilter.Current.ActivationDate);
                    }
                }

            }
            else
            {
                cache.RaiseExceptionHandling<FSActivationContractFilter.activationDate>(filter,
                                                                                        filter.ActivationDate,
                                                                                        new PXSetPropertyException(TX.Error.FIELD_EMPTY, PXErrorLevel.Error));
            }
        }

        protected virtual void _(Events.FieldUpdated<FSTerminateContractFilter, FSTerminateContractFilter.cancelationDate> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSTerminateContractFilter filter = (FSTerminateContractFilter)e.Row;
            PXCache cache = e.Cache;

            if (filter.CancelationDate.HasValue == true)
            {
                if (filter.CancelationDate.Value.Date < this.Accessinfo.BusinessDate.Value.Date)
                {
                    cache.RaiseExceptionHandling<FSTerminateContractFilter.cancelationDate>(filter,
                                                                                            filter.CancelationDate,
                                                                                            new PXSetPropertyException(TX.Error.EFFECTIVE_DATE_LOWER_ACTUAL_DATE, PXErrorLevel.Error));
                }

                if (ServiceContractRecords.Current.ExpirationType == ID.Contract_ExpirationType.EXPIRING
                        && filter.CancelationDate.Value.Date >= ServiceContractRecords.Current.EndDate)
                {
                    cache.RaiseExceptionHandling<FSTerminateContractFilter.cancelationDate>(filter,
                                                                                            filter.CancelationDate,
                                                                                            new PXSetPropertyException(TX.Error.EFFECTIVE_DATE_GREATER_END_DATE, PXErrorLevel.Error));
                }
            }
            else
            {
                cache.RaiseExceptionHandling<FSTerminateContractFilter.cancelationDate>(filter,
                                                                                        filter.CancelationDate,
                                                                                        new PXSetPropertyException(TX.Error.FIELD_EMPTY, PXErrorLevel.Error));
            }
        }

        protected virtual void _(Events.FieldUpdated<FSSuspendContractFilter, FSSuspendContractFilter.suspensionDate> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSSuspendContractFilter filter = (FSSuspendContractFilter)e.Row;
            PXCache cache = e.Cache;

            if (filter.SuspensionDate.HasValue == true)
            {
                if (filter.SuspensionDate.Value.Date < this.Accessinfo.BusinessDate.Value.Date)
                {
                    cache.RaiseExceptionHandling<FSSuspendContractFilter.suspensionDate>(filter,
                                                                                         filter.SuspensionDate,
                                                                                         new PXSetPropertyException(TX.Error.EFFECTIVE_DATE_LOWER_ACTUAL_DATE, PXErrorLevel.Error));
                }

                if (ServiceContractRecords.Current.ExpirationType == ID.Contract_ExpirationType.EXPIRING
                        && filter.SuspensionDate.Value.Date >= ServiceContractRecords.Current.EndDate)
                {
                    cache.RaiseExceptionHandling<FSSuspendContractFilter.suspensionDate>(filter,
                                                                                         filter.SuspensionDate,
                                                                                         new PXSetPropertyException(TX.Error.EFFECTIVE_DATE_GREATER_END_DATE, PXErrorLevel.Error));
                }
            }
            else
            {
                cache.RaiseExceptionHandling<FSSuspendContractFilter.suspensionDate>(filter,
                                                                                     filter.SuspensionDate,
                                                                                     new PXSetPropertyException(TX.Error.FIELD_EMPTY, PXErrorLevel.Error));
            }
        }

        protected virtual void _(Events.FieldSelecting<ActiveSchedule, ActiveSchedule.effectiveRecurrenceStartDate> e)
        {
            if (e.Row == null)
            {
                return;
            }

            ActiveSchedule activeScheduleRow = (ActiveSchedule)e.Row;
            PXCache cache = e.Cache;

            if (ActivationContractFilter.Current.ActivationDate.HasValue == true
                    && activeScheduleRow.EffectiveRecurrenceStartDate == null)
            {
                if (activeScheduleRow.ChangeRecurrence == true)
                {
                    cache.SetValueExt<ActiveSchedule.effectiveRecurrenceStartDate>(activeScheduleRow, ActivationContractFilter.Current.ActivationDate);
                    e.ReturnValue = ActivationContractFilter.Current.ActivationDate;
                }
                else
                {
                    cache.SetValueExt<ActiveSchedule.effectiveRecurrenceStartDate>(activeScheduleRow, activeScheduleRow.StartDate);
                    e.ReturnValue = activeScheduleRow.StartDate;
                }
            }
        }

        protected virtual void _(Events.FieldUpdated<ActiveSchedule, ActiveSchedule.changeRecurrence> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (ActivationContractFilter.Current.ActivationDate.HasValue == true)
            {
                ActiveSchedule activeScheduleRow = (ActiveSchedule)e.Row;
                PXCache cache = e.Cache;

                if (activeScheduleRow.ChangeRecurrence == true)
                {
                    cache.SetValueExt<ActiveSchedule.effectiveRecurrenceStartDate>(activeScheduleRow, ActivationContractFilter.Current.ActivationDate);
                }
                else
                {
                    cache.SetValueExt<ActiveSchedule.effectiveRecurrenceStartDate>(activeScheduleRow, activeScheduleRow.StartDate);
                }
            }
        }

        protected virtual void _(Events.FieldUpdated<ActiveSchedule, ActiveSchedule.effectiveRecurrenceStartDate> e)
        {
            if (e.Row == null)
            {
                return;
            }

            ActiveSchedule activeScheduleRow = (ActiveSchedule)e.Row;
            PXCache cache = e.Cache;

            if (activeScheduleRow.EffectiveRecurrenceStartDate.HasValue == true)
            {
                ActiveSchedule activeScheduleRowCopy = (ActiveSchedule)cache.CreateCopy(activeScheduleRow);
                activeScheduleRowCopy.EndDate = null;
                activeScheduleRowCopy.StartDate = activeScheduleRow.EffectiveRecurrenceStartDate;
                activeScheduleRow.NextExecution = SharedFunctions.GetNextExecution(cache, activeScheduleRowCopy);
            }
        }
		#endregion
		#region FSBillHistory
		#region FieldSelecting
		#endregion
		#region FieldDefaulting
		#endregion
		#region FieldUpdating
		#endregion
		#region FieldVerifying
		#endregion
		#region FieldUpdated
		#endregion

		protected virtual void _(Events.RowSelecting<FSBillHistory> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSBillHistory fsBillHistoryRow = (FSBillHistory)e.Row;
            CalculateBillHistoryUnboundFields(e.Cache, fsBillHistoryRow);
        }

        protected virtual void _(Events.RowSelected<FSBillHistory> e)
        {
        }

        protected virtual void _(Events.RowInserting<FSBillHistory> e)
        {
        }

        protected virtual void _(Events.RowInserted<FSBillHistory> e)
        {
        }

        protected virtual void _(Events.RowUpdating<FSBillHistory> e)
        {
        }

        protected virtual void _(Events.RowUpdated<FSBillHistory> e)
        {
        }

        protected virtual void _(Events.RowDeleting<FSBillHistory> e)
        {
        }

        protected virtual void _(Events.RowDeleted<FSBillHistory> e)
        {
        }

        protected virtual void _(Events.RowPersisting<FSBillHistory> e)
        {
        }

        protected virtual void _(Events.RowPersisted<FSBillHistory> e)
        {
        }
		#endregion

		#endregion

		#region Shared methods

		/// <summary>Update visibility and other UI things for duration options</summary>
		protected virtual void EnableDisableRenewalFields(PXCache cache, FSServiceContract serviceContract)
		{
			bool durationVisible = serviceContract.ExpirationType == ID.Contract_ExpirationType.EXPIRING
								|| serviceContract.ExpirationType == ID.Contract_ExpirationType.RENEWABLE;

			bool isDraft = serviceContract.Status == ID.Status_ServiceContract.DRAFT;

			PXUIFieldAttribute.SetVisible<FSServiceContract.duration>(cache, null, durationVisible);
			PXUIFieldAttribute.SetVisible<FSServiceContract.durationType>(cache, null, durationVisible);
			PXUIFieldAttribute.SetVisible<FSServiceContract.renewalDate>(cache, null, serviceContract.ExpirationType == ID.Contract_ExpirationType.RENEWABLE);

			PXUIFieldAttribute.SetEnabled<FSServiceContract.durationType>(cache, serviceContract, isDraft);
			PXUIFieldAttribute.SetEnabled<FSServiceContract.duration>(cache, serviceContract, isDraft && serviceContract.DurationType != SC_Duration_Type.CUSTOM);
			PXUIFieldAttribute.SetEnabled<FSServiceContract.endDate>(cache, serviceContract, isDraft && serviceContract.DurationType == SC_Duration_Type.CUSTOM);
		}

		protected virtual DateTime? GetEndDate(FSServiceContract scRow, DateTime? startDate, DateTime? actualValue)
		{
			if (scRow.ExpirationType != ID.Contract_ExpirationType.EXPIRING
				&& scRow.ExpirationType != ID.Contract_ExpirationType.RENEWABLE)
			{
				return actualValue;
			}

			if (scRow.Duration == 0 || startDate == null)
			{
				return startDate;
			}

			switch (scRow.DurationType)
			{
				case SC_Duration_Type.YEAR:
				case SC_Duration_Type.QUARTER:
				case SC_Duration_Type.MONTH:
					return GetEndDateFromDuration(startDate.Value, scRow.DurationType, scRow.Duration ?? 1);
				default:
					return actualValue;
			}
		}

		public virtual DateTime GetEndDateFromDuration(DateTime date, string durationType, int duration)
		{
			switch (durationType)
			{
				case SC_Duration_Type.YEAR:
					return AddMonth(date, duration * GetBaseDurationOnMonths(durationType)).AddDays(-1);
				case SC_Duration_Type.QUARTER:
					return AddMonth(date, duration * GetBaseDurationOnMonths(durationType)).AddDays(-1);
				case SC_Duration_Type.MONTH:
					return AddMonth(date, duration * GetBaseDurationOnMonths(durationType)).AddDays(-1);
				default:
					throw new NotImplementedException();
			}
		}

		public virtual int GetBaseDurationOnMonths(string durationType)
		{
			switch (durationType)
			{
				case SC_Duration_Type.YEAR:
					return 12;
				case SC_Duration_Type.QUARTER:
					return 3;
				case SC_Duration_Type.MONTH:
					return 1;
				default:
					throw new NotImplementedException();
			}
		}

		public virtual DateTime AddMonth(DateTime date, int count)
		{
			if (count == 0)
				return date;

			if (date.Day != DateTime.DaysInMonth(date.Year, date.Month))
				return date.AddMonths(count);
			else
				return date.AddDays(1).AddMonths(count).AddDays(-1);
		}

		protected virtual int? GetDuration(FSServiceContract scRow, int? actualValue)
		{
			if (scRow.ExpirationType != ID.Contract_ExpirationType.EXPIRING
				&& scRow.ExpirationType != ID.Contract_ExpirationType.RENEWABLE)
			{
				return actualValue;
			}

			if (scRow.DurationType == SC_Duration_Type.CUSTOM)
			{
				if (scRow.StartDate == null || scRow.EndDate == null)
					return null;

				var result = (int?)(scRow.EndDate.Value - (scRow.RenewalDate.HasValue ? scRow.RenewalDate.Value : scRow.StartDate.Value)).Days + 1;

				if (result < 0)
					result = 0;

				return result;
			}

			return actualValue;
		}

		#endregion
	}
}
