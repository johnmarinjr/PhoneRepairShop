using PX.Common;
using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.GL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Objects.Common;

namespace PX.Objects.AR
{
	public class ARDataEntryGraph<TGraph, TPrimary> : PXGraph<TGraph, TPrimary>, PX.Objects.GL.IVoucherEntry, IActionsMenuGraph
		where TGraph : PXGraph
		where TPrimary : ARRegister, new()
	{
		[InjectDependency]
		protected PX.Reports.IReportLoaderService ReportLoader { get; private set; }

		[InjectDependency]
		protected Func<string, ReportNotificationGenerator> ReportNotificationGeneratorFactory { get; private set; }

		public PXAction ActionsMenuItem => action;
		public PXInitializeState<TPrimary> initializeState;

		public PXAction<TPrimary> putOnHold;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Hold", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable PutOnHold(PXAdapter adapter) => adapter.Get();

		public PXAction<TPrimary> releaseFromHold;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Remove Hold", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable ReleaseFromHold(PXAdapter adapter) => adapter.Get();

		public PXAction<TPrimary> printAREdit;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "AR Edit Detailed", MapEnableRights = PXCacheRights.Select)]
		public virtual IEnumerable PrintAREdit(PXAdapter adapter, string reportID = null) => Report(adapter, reportID ?? "AR610500");

		public PXAction<TPrimary> printARRegister;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "AR Register Detailed", MapEnableRights = PXCacheRights.Select)]
		public virtual IEnumerable PrintARRegister(PXAdapter adapter, string reportID = null) => Report(adapter, reportID ?? "AR622000");

		public PXAction DeleteButton => this.Delete;

		private readonly FinDocCopyPasteHelper CopyPasteHelper;

		public ARDataEntryGraph() : base()
		{
			CopyPasteHelper = new FinDocCopyPasteHelper(this);
			FieldDefaulting.AddHandler<BAccountR.type>((sender, e) => { if (e.Row != null) e.NewValue = BAccountType.CustomerType; });
		}

		public PXAction<TPrimary> release;
		[PXUIField(DisplayName = "Release", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXProcessButton]
		[ARMigrationModeDependentActionRestriction(
			restrictInMigrationMode: false,
			restrictForRegularDocumentInMigrationMode: true,
			restrictForUnreleasedMigratedDocumentInNormalMode: true)]
		public virtual IEnumerable Release(PXAdapter adapter)
		{
			return adapter.Get();
		}

		public PXAction<TPrimary> voidCheck;
		[PXUIField(DisplayName = "Void", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update, Visible = false)]
		[PXProcessButton]
		[ARMigrationModeDependentActionRestriction(
			restrictInMigrationMode: false,
			restrictForRegularDocumentInMigrationMode: true,
			restrictForUnreleasedMigratedDocumentInNormalMode: true)]
		public virtual IEnumerable VoidCheck(PXAdapter adapter)
		{
			return adapter.Get();
		}

		public PXAction<TPrimary> viewBatch;
		[PXUIField(DisplayName = "Review Batch", Visible = false, MapEnableRights = PXCacheRights.Select)]
		[PXLookupButton(DisplayOnMainToolbar = false)]
		public virtual IEnumerable ViewBatch(PXAdapter adapter)
		{
			foreach (TPrimary ardoc in adapter.Get<TPrimary>())
			{
				if (!String.IsNullOrEmpty(ardoc.BatchNbr))
				{
					JournalEntry graph = PXGraph.CreateInstance<JournalEntry>();
					graph.BatchModule.Current = PXSelect<Batch,
						Where<Batch.module, Equal<BatchModule.moduleAR>,
						And<Batch.batchNbr, Equal<Required<Batch.batchNbr>>>>>
						.Select(this, ardoc.BatchNbr);
					throw new PXRedirectRequiredException(graph, "Current batch record");
				}
			}
			return adapter.Get();
		}

		public PXAction<TPrimary> action;
		[PXUIField(DisplayName = "Actions", MapEnableRights = PXCacheRights.Select)]
		[PXButton(CommitChanges = true, MenuAutoOpen = true, SpecialType = PXSpecialButtonType.ActionsFolder)]
		[ARMigrationModeDependentActionRestriction(
			restrictInMigrationMode: false,
			restrictForRegularDocumentInMigrationMode: true,
			restrictForUnreleasedMigratedDocumentInNormalMode: true)]
		protected virtual IEnumerable Action(PXAdapter adapter,
			[PXString()]
			string ActionName)
		{
			if (!string.IsNullOrEmpty(ActionName))
			{
				PXAction action = this.Actions[ActionName];

				if (action != null)
				{
					List<object> items = new List<object>();
					foreach (object record in adapter.Get())
					{
						items.Add(record);
					}

					Save.Press();

					List<object> result = new List<object>();
					PXAdapter newAdapter = new PXAdapter(new PXView.Dummy(this, adapter.View.BqlSelect, items));
					newAdapter.MassProcess = adapter.MassProcess;
					foreach (object data in action.Press(newAdapter))
					{
						result.Add(data);
					}
					return result;
				}
			}
			return adapter.Get();
		}

		public PXAction<TPrimary> inquiry;
		[PXUIField(DisplayName = "Inquiries", MapEnableRights = PXCacheRights.Select)]
		[PXButton(CommitChanges = true, MenuAutoOpen = true, SpecialType = PXSpecialButtonType.InquiriesFolder)]
		protected virtual IEnumerable Inquiry(PXAdapter adapter, [PXString()] string ActionName)
		{
			if (!string.IsNullOrEmpty(ActionName))
			{
				PXAction action = this.Actions[ActionName];

				if (action != null)
				{
					Save.Press();
					foreach (object data in action.Press(adapter)) ;
				}
			}
			return adapter.Get();
		}

		public virtual Dictionary<string, string> PrepareReportParams(string reportID, TPrimary doc)
		{
			var parameters = new Dictionary<string, string>();

			string[] reportsWithParams = new string[] { ARReports.AREditDetailedReportID, ARReports.ARRegisterDetailedReportID };
			Reports.Controls.Report report = ReportLoader.LoadReport(reportID, incoming: null);

			if (report == null)
				throw new PXException(ErrorMessages.ElementDoesntExistOrNoRights, reportID);

			if (reportsWithParams.Contains(reportID))
			{
				Dictionary<string, string> generalParams = new Dictionary<string, string>
				{
					["DocType"] = doc.DocType,
					["RefNbr"] = doc.RefNbr,
					["OrgBAccountID"] = PXAccess.GetBranchCD(doc.BranchID)
				};

				foreach (Reports.ReportParameter param in report.Parameters)
				{
					string paramValue = null;
					bool isGeneralParam = generalParams.TryGetValue(param.Name, out paramValue);

					if (!isGeneralParam && param.Nullable)
					{
						parameters[param.Name] = null;
					}
					else if (isGeneralParam)
					{
						parameters[param.Name] = paramValue;
					}
				}
			}
			else
			{
				string tableName = doc.GetType().Name;
				var generalFilters = new Dictionary<string, string>();
				generalFilters[tableName + ".DocType"] = doc.DocType;
				generalFilters[tableName + ".RefNbr"] = doc.RefNbr;

				foreach (Reports.FilterExp filter in report.Filters)
				{
					bool isGeneralFilter = generalFilters.TryGetValue(filter.DataField, out string filterValue);

					if (isGeneralFilter)
					{
						parameters[filter.DataField] = filterValue;
					}
				}
			}
			return parameters;
		}

		public PXAction<TPrimary> report;
		[PXUIField(DisplayName = "Reports", MapEnableRights = PXCacheRights.Select)]
		[PXButton(CommitChanges = true, MenuAutoOpen = true, SpecialType = PXSpecialButtonType.ReportsFolder)]
		protected virtual IEnumerable Report(PXAdapter adapter, [PXString(8, InputMask = "CC.CC.CC.CC")] string reportID)
		{
			PXReportRequiredException ex = null;
			Dictionary<PX.SM.PrintSettings, PXReportRequiredException> reportsToPrint = new Dictionary<PX.SM.PrintSettings, PXReportRequiredException>();

			foreach (TPrimary doc in adapter.Get<TPrimary>())
			{
				this.Caches[typeof(TPrimary)].MarkUpdated(doc);
				Dictionary<string, string> parameters = PrepareReportParams(reportID, doc);
				string customerReportID = GetCustomerReportID(reportID, doc);

				ex = PXReportRequiredException.CombineReport(ex, customerReportID, parameters);

				reportsToPrint = PX.SM.SMPrintJobMaint.AssignPrintJobToPrinter(reportsToPrint, parameters, adapter, new NotificationUtility(this).SearchPrinter, ARNotificationSource.Customer, reportID, customerReportID, doc.BranchID);
			}

			this.Save.Press();
			if (ex != null)
			{
				PX.SM.SMPrintJobMaint.CreatePrintJobGroups(reportsToPrint);

				throw ex;
			}

			return adapter.Get();
		}

		public virtual string GetCustomerReportID(string reportID, TPrimary doc)
		{
			return reportID;
		}

		public override void CopyPasteGetScript(bool isImportSimple, List<Api.Models.Command> script, List<Api.Models.Container> containers)
		{
			CopyPasteHelper.SetBranchFieldCommandToTheTop(script);
		}
	}
}
