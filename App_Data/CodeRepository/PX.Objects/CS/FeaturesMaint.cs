using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Caching;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Metadata;
using PX.Objects.CR;
using PX.Objects.GL.DAC;
using PX.Objects.GL.FinPeriods;
using PX.Objects.WZ;
using PX.Objects.AR;
using PX.Objects.SO;
using PX.Objects.AP.InvoiceRecognition;
using PX.Web.UI.Frameset.Model.DAC;

namespace PX.Objects.CS
{
	public class FeaturesMaint : PXGraph<FeaturesMaint>
	{
		[InjectDependency]
		protected ICacheControl<PageCache> PageCacheControl { get; set; }
		[InjectDependency]
		protected IScreenInfoCacheControl ScreenInfoCacheControl { get; set; }
		[InjectDependency]
		internal IInvoiceRecognitionService InvoiceRecognitionService { get; set; }

		public PXFilter<AfterActivation> ActivationBehaviour;

		public PXSelect<FeaturesSet> Features;

		protected IEnumerable features()
		{
			FeaturesSet current = (FeaturesSet)PXSelect<FeaturesSet,
								  Where<True, Equal<True>>,
								  OrderBy<Desc<FeaturesSet.status>>>
								  .SelectWindowed(this, 0, 1) ?? Features.Insert();
			current.LicenseID = PXVersionInfo.InstallationID;
			yield return current;
		}

		public FeaturesMaint()
		{
			SaveClose.SetVisible(false);
		}

		public PXSave<FeaturesSet> Save;
		public PXSaveClose<FeaturesSet> SaveClose;
		public PXCancel<FeaturesSet> Cancel;
		public PXAction<FeaturesSet> Insert;

		public PXAction<FeaturesSet> RequestValidation;
		public PXAction<FeaturesSet> CancelRequest;

		public PXSelectJoin<
						MasterFinPeriod,
						InnerJoin<OrganizationFinPeriod,
							On<MasterFinPeriod.finPeriodID, Equal<OrganizationFinPeriod.masterFinPeriodID>,
							And<OrganizationFinPeriod.organizationID, Equal<Required<OrganizationFinPeriod.organizationID>>>>>> MasterFinPeriods;

		public PXSelect<MUIWorkspace, Where<MUIWorkspace.workspaceID, Equal<Required<MUIWorkspace.workspaceID>>>> Workspace;

		public SelectFrom<CRValidation>
			.Where<CRValidation.gramValidationDateTime.IsEqual<CRValidation.defaultGramValidationDateTime>>
			.View
			CRDefaultValidations;

		public const int MAX_FINPERIOD_DISCREPANCY_MESSAGE_COUNT = 20;

		public PXSetup<GL.Company> Company;

		public override IEnumerable ExecuteSelect(string viewName, object[] parameters, object[] searches, string[] sortcolumns, bool[] descendings, PXFilterRow[] filters, ref int startRow, int maximumRows, ref int totalRows)
		{
			if (viewName == "Features")
				searches = null;

			return base.ExecuteSelect(viewName, parameters, searches, sortcolumns, descendings, filters, ref startRow, maximumRows, ref totalRows);
		}

		[PXButton(IsLockedOnToolbar = true)]
		[PXUIField(DisplayName = "Modify", MapEnableRights = PXCacheRights.Insert, MapViewRights = PXCacheRights.Select)]
		public IEnumerable insert(PXAdapter adapter)
		{
			var activationMode = this.ActivationBehaviour.Current;
			foreach (var item in new PXInsert<FeaturesSet>(this, "Insert").Press(adapter))
			{
				this.ActivationBehaviour.Cache.SetValueExt<AfterActivation.refresh>(this.ActivationBehaviour.Current, activationMode.Refresh);
				yield return item;
			}

		}

		[PXButton(IsLockedOnToolbar = true)]
		[PXUIField(DisplayName = "Enable")]
		public IEnumerable requestValidation(PXAdapter adapter)
		{
			foreach (FeaturesSet feature in adapter.Get())
			{
				if (feature.Status == 3)
				{
					bool? customerDiscountsOld = PXAccess.FeatureInstalled<FeaturesSet.customerDiscounts>();
					bool? multiCompanyOld = PXAccess.FeatureInstalled<FeaturesSet.multiCompany>();
					bool? multiBranchOld = PXAccess.FeatureInstalled<FeaturesSet.branch>();
					bool? materialManagementOld = PXAccess.FeatureInstalled<FeaturesSet.materialManagement>();
					PXCache cache = new PXCache<FeaturesSet>(this);
					FeaturesSet update = PXCache<FeaturesSet>.CreateCopy(feature);
					update.Status = 0;
					update = this.Features.Update(update);
					this.Features.Delete(feature);

					if (update.Status != 1)
						this.Features.Delete(new FeaturesSet() { Status = 1 });

					this.Persist();
					PXAccess.Version++;

					var tasks = PXSelect<WZTask>.Select(this);
					WZTaskEntry taskGraph = CreateInstance<WZTaskEntry>();
					foreach (WZTask task in tasks)
					{
						bool disableTask = false;
						bool enableTask = false;
						foreach (
							WZTaskFeature taskFeature in
								PXSelectReadonly<WZTaskFeature, Where<WZTaskFeature.taskID, Equal<Required<WZTask.taskID>>>>.Select(
									this, task.TaskID))
						{
							bool featureInstalled = (bool?)cache.GetValue(update, taskFeature.Feature) == true;

							if (!featureInstalled)
							{
								disableTask = true;
								enableTask = false;
								break;
							}

							enableTask = true;
						}

						if (disableTask)
						{
							task.Status = WizardTaskStatusesAttribute._DISABLED;
							taskGraph.TaskInfo.Update(task);
							taskGraph.Save.Press();
						}

						if (enableTask && task.Status == WizardTaskStatusesAttribute._DISABLED)
						{

							bool needToBeOpen = false;
							WZScenario scenario = PXSelect<WZScenario, Where<WZScenario.scenarioID, Equal<Required<WZTask.scenarioID>>>>.Select(this, task.ScenarioID);
							if (scenario != null && scenario.Status == WizardScenarioStatusesAttribute._ACTIVE)
							{
								WZTask parentTask =
									PXSelect<WZTask, Where<WZTask.taskID, Equal<Required<WZTask.parentTaskID>>>>.Select(
										this, task.ParentTaskID);

								if (parentTask != null && (parentTask.Status == WizardTaskStatusesAttribute._OPEN ||
														   parentTask.Status == WizardTaskStatusesAttribute._ACTIVE))
								{
									needToBeOpen = true;
								}

								foreach (
									PXResult<WZTaskPredecessorRelation, WZTask> predecessorResult in
										PXSelectJoin<WZTaskPredecessorRelation,
											InnerJoin
												<WZTask,
													On<WZTask.taskID, Equal<WZTaskPredecessorRelation.predecessorID>>>,
											Where<WZTaskPredecessorRelation.taskID, Equal<Required<WZTask.taskID>>>>.
											Select(this, task.TaskID))
								{
									WZTask predecessorTask = (WZTask)predecessorResult;
									if (predecessorTask != null)
									{
										if (predecessorTask.Status == WizardTaskStatusesAttribute._COMPLETED)
										{
											needToBeOpen = true;

										}
										else
										{
											needToBeOpen = false;
											break;
										}
									}
								}
							}
							task.Status = needToBeOpen ? WizardTaskStatusesAttribute._OPEN : WizardTaskStatusesAttribute._PENDING;
							taskGraph.TaskInfo.Update(task);
							taskGraph.Save.Press();
						}
					}

					if (customerDiscountsOld == true && update.CustomerDiscounts != true)
					{
						PXUpdate<Set<ARSetup.applyLineDiscountsIfCustomerPriceDefined, True>, ARSetup>.Update(this);
						PXUpdate<Set<ARSetup.applyLineDiscountsIfCustomerClassPriceDefined, True>, ARSetup>.Update(this);
						PXUpdate<Set<SOOrderType.recalculateDiscOnPartialShipment, False, Set<SOOrderType.postLineDiscSeparately, False>>, SOOrderType>.Update(this);
					}

					if (multiCompanyOld != update.MultiCompany)
					{
						PXUpdate<Set<ListEntryPoint.isActive, Required<ListEntryPoint.isActive>>, ListEntryPoint, 
							Where<ListEntryPoint.entryScreenID, Equal<Required<ListEntryPoint.entryScreenID>>>>
							.Update(this, update.MultiCompany == true, "CS101500");
					}

					UpdateARPrapreStatement(multiCompanyOld, multiBranchOld, update);

					if (materialManagementOld != true && update.MaterialManagement == true)
					{
						PXUpdate<Set<PM.PMSetup.stockInitRequired, True>, PM.PMSetup>.Update(this);
					}
					else if (materialManagementOld == true && update.MaterialManagement != true)
					{
						PXUpdate<Set<PM.PMSetup.stockInitRequired, False>, PM.PMSetup>.Update(this);
					}

					yield return update;
				}
				else
					yield return feature;
			}

			bool needRefresh = !(ActivationBehaviour.Current != null && ActivationBehaviour.Current.Refresh == false);

			PXDatabase.ResetSlots();
			PageCacheControl.InvalidateCache();
			ScreenInfoCacheControl.InvalidateCache();
			this.Clear();
			if (needRefresh)
				throw new PXRefreshException();
		}

		private void UpdateARPrapreStatement(bool? multiCompanyOld, bool? multiBranchOld, FeaturesSet updated)
		{
			if (multiCompanyOld != updated.MultiCompany || multiBranchOld != updated.Branch)
			{
				if (updated.MultiCompany == false && updated.Branch == false)
				{
					PXUpdate<Set<ARSetup.prepareStatements, Required<ARSetup.prepareStatements>>, ARSetup>.Update(this, ARSetup.prepareStatements.ForEachBranch);
				}
				else if (updated.MultiCompany == false && updated.Branch == true)
				{
					PXUpdate<Set<ARSetup.prepareStatements, Required<ARSetup.prepareStatements>>, ARSetup,
							Where<ARSetup.prepareStatements, Equal<Required<ARSetup.prepareStatements>>>>
							.Update(this, ARSetup.prepareStatements.ForEachBranch, ARSetup.prepareStatements.ConsolidatedForAllCompanies);
				}
				else if (updated.MultiCompany == true && updated.Branch == false)
				{
					PXUpdate<Set<ARSetup.prepareStatements, Required<ARSetup.prepareStatements>>, ARSetup,
							Where<ARSetup.prepareStatements, Equal<Required<ARSetup.prepareStatements>>>>
							.Update(this, ARSetup.prepareStatements.ConsolidatedForCompany, ARSetup.prepareStatements.ForEachBranch);
				}
			}
		}

		[PXButton]
		[PXUIField(DisplayName = "Cancel Validation Request", Visible = false)]
		public IEnumerable cancelRequest(PXAdapter adapter)
		{
			foreach (FeaturesSet feature in adapter.Get())
			{
				if (feature.Status == 2)
				{
					FeaturesSet update = PXCache<FeaturesSet>.CreateCopy(feature);
					update.Status = 3;
					this.Features.Delete(feature);
					update = this.Features.Update(update);
					this.Persist();
					yield return update;
				}
				else
					yield return feature;
			}
		}

		protected virtual void FeaturesSet_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			this.Save.SetVisible(false);
			this.Features.Cache.AllowInsert = true;
			FeaturesSet row = (FeaturesSet)e.Row;
			if (row == null) return;

			this.RequestValidation.SetEnabled(row.Status == 3);
			this.CancelRequest.SetEnabled(row.Status == 2);
			this.Features.Cache.AllowInsert = row.Status < 2;
			this.Features.Cache.AllowUpdate = row.Status == 3;
			this.Features.Cache.AllowDelete = false;

			bool screenIsOpenedFromScenario = !(ActivationBehaviour.Current != null && ActivationBehaviour.Current.Refresh == true);
			if (screenIsOpenedFromScenario && this.Actions.Contains("CancelClose"))
				this.Actions["CancelClose"].SetTooltip(WZ.Messages.BackToScenario);

			PXFieldState state = sender.GetStateExt<FeaturesSet.multipleBaseCurrencies>(row) as PXFieldState;
			if (state != null && state.ErrorLevel != PXErrorLevel.Error)
			{
				sender.RaiseExceptionHandling<FeaturesSet.multipleBaseCurrencies>(row, row.MultipleBaseCurrencies,
					new PXSetPropertyException(Messages.MBCFeatureIsInATestMode, PXErrorLevel.Warning));
			}

			PXFieldState accountingAnomalyDetectionState = sender.GetStateExt<FeaturesSet.glAnomalyDetection>(row) as PXFieldState;
			if (accountingAnomalyDetectionState != null && accountingAnomalyDetectionState.ErrorLevel != PXErrorLevel.Error)
			{
				sender.RaiseExceptionHandling<FeaturesSet.glAnomalyDetection>(row, row.GLAnomalyDetection,
					new PXSetPropertyException(Messages.GLAnomalyDetectionFeatureIsInTestMode, PXErrorLevel.Warning));
			}
		}

		protected virtual void FeaturesSet_RowInserting(PXCache sender, PXRowInsertingEventArgs e)
		{
			int? status = (int?)sender.GetValue<FeaturesSet.status>(e.Row);
			if (status != 3) return;

			FeaturesSet current = PXSelect<FeaturesSet,
				Where<True, Equal<True>>,
				OrderBy<Desc<FeaturesSet.status>>>
				.SelectWindowed(this, 0, 1);
			if (current != null)
			{
				sender.RestoreCopy(e.Row, current);
				sender.SetValue<FeaturesSet.status>(e.Row, 3);
			}
		}

		protected virtual void CheckMasterOrganizationCalendarDiscrepancy()
		{
			int messageCount = 0;
			bool isError = false;

			foreach (Organization organization in PXSelect<Organization>.Select(this))
			{
				foreach (MasterFinPeriod problemPeriod in PXSelectJoin<
					MasterFinPeriod,
					LeftJoin<OrganizationFinPeriod,
						On<MasterFinPeriod.finPeriodID, Equal<OrganizationFinPeriod.masterFinPeriodID>,
						And<OrganizationFinPeriod.organizationID, Equal<Required<OrganizationFinPeriod.organizationID>>>>>,
					Where<OrganizationFinPeriod.finPeriodID, IsNull>>
					.Select(this, organization.OrganizationID))
				{
					isError = true;
					if (messageCount <= MAX_FINPERIOD_DISCREPANCY_MESSAGE_COUNT)
					{
						PXTrace.WriteError(GL.Messages.DiscrepancyPeriod, organization.OrganizationCD, problemPeriod.FinPeriodID);

						messageCount++;
					}
					else
					{
						break;
					}
				}
			}

			if (isError)
			{
				throw new PXSetPropertyException(GL.Messages.DiscrepancyPeriodError);
			}
		}

		private Organization etalonOrganization = null;
		protected Organization EtalonOrganization => etalonOrganization ?? (etalonOrganization = PXSelect<Organization>.SelectSingleBound(this, new object[] { }));

		protected virtual void CheckOrganizationCalendarFieldsDiscrepancy()
		{
			int messageCount = 0;
			bool isError = false;

			if (EtalonOrganization != null)
			{
				foreach (Organization organization in PXSelect<
					Organization, 
					Where<Organization.organizationID, NotEqual<Required<Organization.organizationID>>>>
					.Select(this, EtalonOrganization.OrganizationID))
				{
					foreach (OrganizationFinPeriod problemPeriod in PXSelectJoin<
						OrganizationFinPeriod,
						LeftJoin<OrganizationFinPeriodStatus,
							On<OrganizationFinPeriodStatus.organizationID, Equal<Required<OrganizationFinPeriodStatus.organizationID>>,
							And<OrganizationFinPeriod.finPeriodID, Equal<OrganizationFinPeriodStatus.finPeriodID>,
							And<OrganizationFinPeriod.dateLocked, Equal<OrganizationFinPeriodStatus.dateLocked>,
							And<OrganizationFinPeriod.status, Equal<OrganizationFinPeriodStatus.status>,
							And<OrganizationFinPeriod.aPClosed, Equal<OrganizationFinPeriodStatus.aPClosed>,
							And<OrganizationFinPeriod.aRClosed, Equal<OrganizationFinPeriodStatus.aRClosed>,
							And<OrganizationFinPeriod.iNClosed, Equal<OrganizationFinPeriodStatus.iNClosed>,
							And<OrganizationFinPeriod.cAClosed, Equal<OrganizationFinPeriodStatus.cAClosed>,
							And<OrganizationFinPeriod.fAClosed, Equal<OrganizationFinPeriodStatus.fAClosed>>>>>>>>>>>,
						Where<OrganizationFinPeriodStatus.finPeriodID, IsNull,
							And<OrganizationFinPeriod.organizationID, Equal<Required<OrganizationFinPeriod.organizationID>>>>>
					.Select(this, organization.OrganizationID, EtalonOrganization.OrganizationID))
					{
						isError = true;
						if (messageCount <= MAX_FINPERIOD_DISCREPANCY_MESSAGE_COUNT)
						{
							string problemFields = GetProblemFields(organization, problemPeriod);

							PXTrace.WriteError(GL.Messages.DiscrepancyField,
								EtalonOrganization.OrganizationCD,
								organization.OrganizationCD,
								problemFields,
								problemPeriod.FinPeriodID);

							messageCount++;
						}
						else
						{
							break;
						}
					}
				}

				if (isError)
				{
					throw new PXSetPropertyException(GL.Messages.DiscrepancyFieldError);
				}
			}
		}

		private void CheckFullLengthOrganizationCalendars()
		{
			List<string> organizations = PXSelectJoinGroupBy<
				MasterFinPeriod,
				CrossJoin<Organization,
				LeftJoin<OrganizationFinPeriod, 
					On<MasterFinPeriod.finPeriodID, Equal<OrganizationFinPeriod.masterFinPeriodID>,
					And<Organization.organizationID, Equal<OrganizationFinPeriod.organizationID>>>>>,
				Where<OrganizationFinPeriod.finPeriodID, IsNull>,
				Aggregate<
					GroupBy<Organization.organizationCD>>>
				.Select(this)
				.RowCast<Organization>()
				.Select(org => org.OrganizationCD.Trim())
				.ToList();

			if (organizations.Any())
			{
				throw new PXSetPropertyException(GL.Messages.ShortOrganizationCalendarsDetected, string.Join(", ", organizations));
			}
		}

		private void CheckUnshiftedOrganizationCalendars()
		{
			List<string> organizations = PXSelectJoinGroupBy<
				OrganizationFinPeriod, 
				InnerJoin<Organization, 
					On<OrganizationFinPeriod.organizationID, Equal<Organization.organizationID>>>, 
				Where<OrganizationFinPeriod.finPeriodID, NotEqual<OrganizationFinPeriod.masterFinPeriodID>>, 
				Aggregate<
					GroupBy<Organization.organizationCD>>>
				.Select(this)
				.RowCast<Organization>()
				.Select(org => org.OrganizationCD.Trim())
				.ToList();

			if (organizations.Any())
			{
				throw new PXSetPropertyException(GL.Messages.ShiftedOrganizationCalendarsDetected, string.Join(", ", organizations));
			}
		}

		protected virtual void _(Events.FieldUpdating<FeaturesSet, FeaturesSet.multipleCalendarsSupport> e)
		{
			if (e.Row == null) return;

			if (e.Row.MultipleCalendarsSupport == true &&  (bool?)e.NewValue != true) // try to unset
			{
				CheckUnshiftedOrganizationCalendars();
				CheckFullLengthOrganizationCalendars();
			}
		}

		protected virtual void _(Events.FieldUpdating<FeaturesSet, FeaturesSet.centralizedPeriodsManagement> e)
		{
			e.NewValue = PXBoolAttribute.ConvertValue(e.NewValue);
			if (e.Row == null) return;

			if (e.Row.CentralizedPeriodsManagement != null && e.Row.CentralizedPeriodsManagement != (bool)e.NewValue && (bool)e.NewValue == true) // try to set
			{
				CheckMasterOrganizationCalendarDiscrepancy();
				CheckOrganizationCalendarFieldsDiscrepancy();
			}
		}

		protected virtual void _(Events.FieldUpdated<FeaturesSet, FeaturesSet.centralizedPeriodsManagement> e)
		{
			if (e.Row == null) return;

			if ((bool?)e.OldValue != true && 
				e.Row.CentralizedPeriodsManagement == true &&
				EtalonOrganization != null)
			{
				foreach (PXResult<MasterFinPeriod, OrganizationFinPeriod> res in MasterFinPeriods.Select(EtalonOrganization.OrganizationID))
				{
					MasterFinPeriod masterFinPeriod = res;
					OrganizationFinPeriod organizationFinPeriod = res;

					masterFinPeriod.DateLocked = organizationFinPeriod.DateLocked;
					masterFinPeriod.Status = organizationFinPeriod.Status;
					masterFinPeriod.APClosed = organizationFinPeriod.APClosed;
					masterFinPeriod.ARClosed = organizationFinPeriod.ARClosed;
					masterFinPeriod.INClosed = organizationFinPeriod.INClosed;
					masterFinPeriod.CAClosed = organizationFinPeriod.CAClosed;
					masterFinPeriod.FAClosed = organizationFinPeriod.FAClosed;

					MasterFinPeriods.Cache.Update(masterFinPeriod);
				}
			}
		}

		private string GetProblemFields(Organization organization, OrganizationFinPeriod problemPeriod)
		{
			OrganizationFinPeriod currentFinPeriod = PXSelect<
				OrganizationFinPeriod,
				Where<OrganizationFinPeriod.organizationID, Equal<Required<OrganizationFinPeriod.organizationID>>,
					And<OrganizationFinPeriod.finPeriodID, Equal<Required<OrganizationFinPeriod.finPeriodID>>>>>
				.Select(this, organization.OrganizationID, problemPeriod.FinPeriodID);

			List<string> fieldList = new List<string>();
			if (problemPeriod.DateLocked != currentFinPeriod.DateLocked)
				fieldList.Add(nameof(problemPeriod.DateLocked));

			if (problemPeriod.Status != currentFinPeriod.Status)
				fieldList.Add(nameof(problemPeriod.Status));

			if (problemPeriod.APClosed != currentFinPeriod.APClosed)
				fieldList.Add(nameof(problemPeriod.APClosed));

			if (problemPeriod.ARClosed != currentFinPeriod.ARClosed)
				fieldList.Add(nameof(problemPeriod.ARClosed));

			if (problemPeriod.INClosed != currentFinPeriod.INClosed)
				fieldList.Add(nameof(problemPeriod.INClosed));

			if (problemPeriod.CAClosed != currentFinPeriod.CAClosed)
				fieldList.Add(nameof(problemPeriod.CAClosed));

			if (problemPeriod.FAClosed != currentFinPeriod.FAClosed)
				fieldList.Add(nameof(problemPeriod.FAClosed));

			return String.Join(", ", fieldList.ToArray());
		}

		protected virtual void _(Events.FieldUpdating<FeaturesSet.aSC606> e)
		{
			e.NewValue = PXBoolAttribute.ConvertValue(e.NewValue);

			FeaturesSet row = (FeaturesSet)e.Row;
			if (row == null) return;

			bool? oldValue = row.ASC606;

			if (row.ASC606 != null && oldValue != (bool)e.NewValue)
			{
				int? result = PXSelectGroupBy<
					ARTranAlias, 
					Aggregate<Count>>
					.SelectSingleBound(this, null)
					.RowCount;

				if (result > 0)
				{
					string question = PXMessages.LocalizeFormatNoPrefixNLA(AR.Messages.UnreleasedDocsWithDRCodes, result);
					WebDialogResult wdr = Features.Ask(question, MessageButtons.YesNo);
					if (wdr != WebDialogResult.Yes)
					{
						e.NewValue = oldValue;
						e.Cancel = true;
						return;
					}
				}

				//The system calculates the number of Stock and Non-Stock Inventories 
				//in Active status which have MDA deferral code and empty field Allocation Method in Revenue Components.
				if ((bool)e.NewValue == false)
				{
					//use AR.Messages.MDAInventoriesWithoutAllocationMethod
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<FeaturesSet, FeaturesSet.multipleCalendarsSupport> e)
		{
			if (((FeaturesSet)e.Cache.Current)?.CentralizedPeriodsManagement == true && (bool?)e.NewValue == true && (bool?)e.OldValue == false)
			{
				e.NewValue = e.OldValue;
				throw new PXSetPropertyException(Messages.FeaturesAvoidanceDisablingRequiredToEnable, PXUIFieldAttribute.GetDisplayName<FeaturesSet.centralizedPeriodsManagement>(e.Cache));
			}
		}

		protected virtual void _(Events.FieldVerifying<FeaturesSet, FeaturesSet.centralizedPeriodsManagement> e)
		{
			if (((FeaturesSet)e.Cache.Current)?.MultipleCalendarsSupport == true && (bool?)e.NewValue == true && (bool?)e.OldValue == false)
			{
				e.NewValue = e.OldValue;
				throw new PXSetPropertyException(Messages.FeaturesAvoidanceDisablingRequiredToEnable, PXUIFieldAttribute.GetDisplayName<FeaturesSet.multipleCalendarsSupport>(e.Cache));
			}

			if (((FeaturesSet)e.Cache.Current)?.MultiCompany == false && (bool?)e.NewValue == false && (bool?)e.OldValue == true)
			{
				e.NewValue = e.OldValue;
				throw new PXSetPropertyException(Messages.FeaturesAvoidanceEnablingRequiredToDisable, PXUIFieldAttribute.GetDisplayName<FeaturesSet.multiCompany>(e.Cache));
			}

		}
		protected virtual void _(Events.FieldVerifying<FeaturesSet, FeaturesSet.multiCompany> e)
		{
			if (((FeaturesSet)e.Cache.Current)?.CentralizedPeriodsManagement == false && (bool?)e.NewValue == false && (bool?)e.OldValue == true)
			{
				e.NewValue = e.OldValue;
				throw new PXSetPropertyException(Messages.FeaturesAvoidanceEnablingRequiredToDisable, PXUIFieldAttribute.GetDisplayName<FeaturesSet.centralizedPeriodsManagement>(e.Cache));
			}
		}

		protected virtual void _(Events.FieldVerifying<FeaturesSet.imageRecognition> e)
		{
			VerifyRecognitionFeature(e, InvoiceRecognitionService.IsConfigured());
		}
        protected virtual void _(Events.FieldVerifying<FeaturesSet.businessCardRecognition> e)
        {
            var tryToActivate = (bool?)e.NewValue == true && (bool?)e.OldValue == false;
            if (!tryToActivate)
            {
                return;
            }

            if (!InvoiceRecognitionService.IsConfigured())
            {
                e.NewValue = false;
                PopupNoteManager.Message = PXMessages.LocalizeNoPrefix(Messages.FeatureImageRecognitionIsNotConfigured);

                throw new PXSetPropertyException(Messages.FeatureImageRecognitionIsNotConfigured);
            }
        }
        protected virtual void _(Events.FieldVerifying<FeaturesSet.apDocumentRecognition> e)
		{
			VerifyRecognitionFeature(e, InvoiceRecognitionService.IsConfigured());
		}

		public static void VerifyRecognitionFeature<F>(Events.FieldVerifying<F> e, bool IsConfigured)
			where F : class, IBqlField
		{
			var tryToActivate = (bool?)e.NewValue == true && (bool?)e.OldValue == false;
			if (!tryToActivate)
			{
				return;
			}

			if (IsConfigured)
			{
				return;
			}

			e.NewValue = false;

			var licenseState = PXLicenseHelper.License.State;
			var isTrialMode = licenseState == PXLicenseState.Invalid || licenseState == PXLicenseState.Rejected;

			throw isTrialMode ?
				new PXSetPropertyException(Messages.FeatureImageRecognitionIsNotConfiguredTrialMode) :
				new PXSetPropertyException(Messages.FeatureImageRecognitionIsNotConfigured);
		}

		protected virtual void _(Events.FieldUpdated<FeaturesSet, FeaturesSet.construction> e)
		{
			if (e.Row == null) return;

			MUIWorkspace workspace = Workspace.Select(Guid.Parse(Common.Constants.ProjectsWorkspaceID));
			if (workspace == null) return; 

			if ((bool?)e.NewValue == true && (bool?)e.OldValue == false)
			{
				workspace.Title = Common.Messages.ConstructionWorkspaceTitle;
				workspace.Icon = Common.Constants.ConstructionWorkspaceIcon;
			}
			else if ((bool?)e.NewValue == false && (bool?)e.OldValue == true)
			{
				workspace.Title = Common.Messages.ProjectsWorkspaceTitle;
				workspace.Icon = Common.Constants.ProjectsWorkspaceIcon;
			}
			Workspace.Update(workspace);
		}

		protected virtual void _(Events.FieldUpdating<FeaturesSet.contactDuplicate> e)
		{
			if (e.NewValue is true && e.OldValue is false)
			{
				foreach (CRValidation validation in CRDefaultValidations.Select())
				{
					// Set real date of duplication validation turned on
					// instead of unclear default value which could be not updated at all otherwise
					validation.GramValidationDateTime = PXTimeZoneInfo.Now;
					CRDefaultValidations.Cache.Update(validation);
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<FeaturesSet.contactDuplicate> e)
		{
			if (e.NewValue is true && e.OldValue is false)
			{
				if (SelectFrom<Contact>
					.InnerJoin<CRGramValidationDateTime.ByLead>.On<True.IsEqual<True>>
					.InnerJoin<CRGramValidationDateTime.ByContact>.On<True.IsEqual<True>>
					.InnerJoin<CRGramValidationDateTime.ByBAccount>.On<True.IsEqual<True>>
					.Where<
						Brackets<
							Contact.contactType.IsEqual<ContactTypesAttribute.lead>
							.And<Contact.grammValidationDateTime.IsLess<CRGramValidationDateTime.ByLead.value>>
						>
						.Or<Brackets<
							Contact.contactType.IsEqual<ContactTypesAttribute.person>
							.And<Contact.grammValidationDateTime.IsLess<CRGramValidationDateTime.ByContact.value>>
						>>
						.Or<Brackets<
							Contact.contactType.IsEqual<ContactTypesAttribute.bAccountProperty>
							.And<Contact.grammValidationDateTime.IsLess<CRGramValidationDateTime.ByBAccount.value>>
						>>
					>
					.View
					.Select(this)
					.Any())
				{
					e.Cache.RaiseExceptionHandling<FeaturesSet.contactDuplicate>(e.Row, e.NewValue,
						new PXSetPropertyException<FeaturesSet.contactDuplicate>(CR.Messages.NoGramsDuringTurningOnDuplicateValidationFeature, PXErrorLevel.Warning));
				}
			}
		}
		
		private void CheckCompaniesOnDifferBaseCury()
		{
			List<string> organizations = PXSelectReadonly<Organization,
				Where<Organization.active, Equal<True>, 
				And<Organization.baseCuryID, NotEqual<Optional2<Organization.baseCuryID>>>>>
				.Select(this, Company.Current.BaseCuryID)
				.RowCast<Organization>()
				.Select(org => org.OrganizationCD.Trim())
				.ToList();

			if (organizations.Any())
			{
				throw new PXSetPropertyException(Messages.FeaturesDifferBaseCuryExist);
			}
		}

		protected virtual void _(Events.FieldUpdating<FeaturesSet, FeaturesSet.multipleBaseCurrencies> e)
		{
			if (e.Row == null) return;

			if ((bool?)e.NewValue != true) // try to unset
			{
				CheckCompaniesOnDifferBaseCury();
			}
			else if ((bool?)e.NewValue == true)
			{
				ARSetup arsetup = PXSetup<ARSetup>.Select(this);
				if (arsetup?.PrepareStatements == ARSetup.prepareStatements.ConsolidatedForAllCompanies)
				{
					throw new PXSetPropertyException(Messages.ConsolidatedForAllCompanies);
				}				
			}
		}

		protected virtual void _(Events.FieldVerifying<FeaturesSet, FeaturesSet.multipleBaseCurrencies> e)
		{
			if (e.Row.ProjectAccounting == true && e.Row.ProjectMultiCurrency != true && (bool?)e.NewValue == true)
			{
				string dependencyNames = string.Empty;
				string displayName = string.Empty;

				if (e.Row?.MultiCompany == false)
				{
					displayName = PXUIFieldAttribute.GetDisplayName<FeaturesSet.multiCompany>(e.Cache);
					dependencyNames += !string.IsNullOrEmpty(dependencyNames) ? string.Format(", {0}", displayName) : displayName;
				}

				if (e.Row?.Multicurrency == false)
				{
					displayName = PXUIFieldAttribute.GetDisplayName<FeaturesSet.multicurrency>(e.Cache);
					dependencyNames += !string.IsNullOrEmpty(dependencyNames) ? string.Format(", {0}", displayName) : displayName;
				}

				if (e.Row?.VisibilityRestriction == false)
				{
					displayName = PXUIFieldAttribute.GetDisplayName<FeaturesSet.visibilityRestriction>(e.Cache);
					dependencyNames += !string.IsNullOrEmpty(dependencyNames) ? string.Format(", {0}", displayName) : displayName;
				}

				displayName = PXUIFieldAttribute.GetDisplayName<FeaturesSet.projectMultiCurrency>(e.Cache);
				dependencyNames += !string.IsNullOrEmpty(dependencyNames) ? string.Format(", {0}", displayName) : displayName;

				throw new PXSetPropertyException(Messages.FeaturesDependenciesAllRequired, dependencyNames);
			}
		}

		protected virtual void _(Events.FieldUpdated<FeaturesSet.payrollUS> e)
		{
			FeaturesSet row = e.Row as FeaturesSet;
			if (row == null || row.PayrollUS != true)
			{
				return;
			}

			// PayrollUS and PayrollCAN are mutually exclusive
			row.PayrollCAN = false;
		}

		protected virtual void _(Events.FieldUpdated<FeaturesSet.payrollCAN> e)
		{
			FeaturesSet row = e.Row as FeaturesSet;
			if (row == null || row.PayrollCAN != true)
			{
				return;
			}

			// PayrollUS and PayrollCAN are mutually exclusive
			row.PayrollUS = false;
		}

		protected virtual void _(Events.RowUpdated<FeaturesSet> e)
		{
			if (e.Row == null)
			{
				return;
			}

			e.Row.PayrollConstruction = e.Row.Construction == true && e.Row.PayrollUS == true;
		}
		
		protected virtual void _(Events.FieldUpdated<FeaturesSet, FeaturesSet.projectMultiCurrency> e)
		{
			if (e.Row == null) return;

			if ((bool?)e.NewValue != true && e.Row.ProjectAccounting == true) // try to unset
			{
				e.Row.MultipleBaseCurrencies = false;
			}
		}

		protected virtual void _(Events.FieldDefaulting<FeaturesSet, FeaturesSet.projectMultiCurrency> e)
		{
			e.NewValue = e.Row.ProjectAccounting == true && e.Row.MultipleBaseCurrencies == true;
		}

		protected virtual void _(Events.FieldUpdated<FeaturesSet, FeaturesSet.projectAccounting> e)
		{
			e.Cache.SetDefaultExt<FeaturesSet.projectMultiCurrency>(e.Row);
		}
	}

	[Serializable]
	public partial class AfterActivation : IBqlTable
	{
		#region Refresh
		public abstract class refresh : PX.Data.BQL.BqlBool.Field<refresh> { }

		protected Boolean? _Refresh;
		[PXDBBool]
		public virtual Boolean? Refresh
		{
			get
			{
				return this._Refresh;
			}
			set
			{
				this._Refresh = value;
			}
		}
		#endregion
	}

	[Serializable]
	[PXProjection(typeof(Select4<
					ARTran,
					Where<ARTran.released, NotEqual<True>,
						And<ARTran.deferredCode, IsNotNull>>,
					Aggregate<
						GroupBy<ARTran.refNbr,
						GroupBy<ARTran.tranType>>>>))]
	public partial class ARTranAlias : IBqlTable
	{
		#region TranType
		public abstract class tranType : IBqlField { }
		[PXDBString(3, IsKey = true, IsFixed = true, BqlField = typeof(ARTran.tranType))]
		public virtual string TranType { get; set; }
		#endregion
		#region RefNbr
		public abstract class refNbr : IBqlField { }
		[PXDBString(15, IsUnicode = true, IsKey = true, BqlField = typeof(ARTran.refNbr))]
		public virtual string RefNbr { get; set; }
		#endregion
	}
}
