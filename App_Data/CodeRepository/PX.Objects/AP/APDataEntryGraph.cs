using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CR;
using PX.Objects.GL;
using System;
using System.Collections;
using System.Collections.Generic;
using PX.Objects.Common;

namespace PX.Objects.AP
{
	public class APDataEntryGraph<TGraph, TPrimary> : PXGraph<TGraph, TPrimary>, IVoucherEntry
		where TGraph : PXGraph
		where TPrimary : APRegister, new()
	{
		public PXAction<TPrimary> putOnHold;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Hold", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[APMigrationModeDependentActionRestriction(
			restrictInMigrationMode: false,
			restrictForRegularDocumentInMigrationMode: true,
			restrictForUnreleasedMigratedDocumentInNormalMode: true)]
		protected virtual IEnumerable PutOnHold(PXAdapter adapter) => adapter.Get();

		[APMigrationModeDependentActionRestriction(
			restrictInMigrationMode: false,
			restrictForRegularDocumentInMigrationMode: true,
			restrictForUnreleasedMigratedDocumentInNormalMode: true)]
		public PXAction<TPrimary> releaseFromHold;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Remove Hold", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable ReleaseFromHold(PXAdapter adapter) => adapter.Get();

		public PXWorkflowEventHandler<TPrimary> OnUpdateStatus;

		public PXSetup<APSetup> apsetup;

		public PXAction DeleteButton => this.Delete;

		public PXInitializeState<TPrimary> initializeState;

		private readonly FinDocCopyPasteHelper CopyPasteHelper;
		public APDataEntryGraph() : base()
		{
			CopyPasteHelper = new FinDocCopyPasteHelper(this);
			FieldDefaulting.AddHandler<BAccountR.type>((sender, e) => { if (e.Row != null) e.NewValue = BAccountType.VendorType; });
		}

		public PXAction<TPrimary> release;
		[PXUIField(DisplayName = "Release", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXProcessButton]
		[APMigrationModeDependentActionRestriction(
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
		[APMigrationModeDependentActionRestriction(
			restrictInMigrationMode: false,
			restrictForRegularDocumentInMigrationMode: true,
			restrictForUnreleasedMigratedDocumentInNormalMode: true)]
		public virtual IEnumerable VoidCheck(PXAdapter adapter)
		{
			return adapter.Get();
		}

		public PXAction<TPrimary> viewBatch;
		[PXUIField(DisplayName = "Review Batch", Visible = false, MapEnableRights = PXCacheRights.Select)]
		[PXLookupButton]
		public virtual IEnumerable ViewBatch(PXAdapter adapter)
		{
			foreach (TPrimary apdoc in adapter.Get<TPrimary>())
			{
				if (!String.IsNullOrEmpty(apdoc.BatchNbr))
				{
					JournalEntry graph = PXGraph.CreateInstance<JournalEntry>();
					graph.BatchModule.Current = PXSelect<Batch,
						Where<Batch.module, Equal<BatchModule.moduleAP>,
						And<Batch.batchNbr, Equal<Required<Batch.batchNbr>>>>>
						.Select(this, apdoc.BatchNbr);
					throw new PXRedirectRequiredException(graph, "Current batch record");
				}
			}
			return adapter.Get();
		}

		protected virtual IEnumerable Report(PXAdapter adapter, string reportID)
		{
			foreach (TPrimary doc in adapter.Get<TPrimary>())
			{
				object masterPeriodID;

				this.Caches[typeof(TPrimary)].MarkUpdated(doc);

				this.Save.Press();

				var docMasterPeriod = (masterPeriodID = this.Caches[typeof(TPrimary)].GetValueExt<APRegister.tranPeriodID>(doc)) is PXFieldState
					? (string)((PXFieldState)masterPeriodID).Value
					: (string)masterPeriodID;

				Dictionary<string, string> parameters = new Dictionary<string, string>();
				parameters["PeriodFrom"] = docMasterPeriod;
				parameters["PeriodTo"] = docMasterPeriod;
				parameters["OrgBAccountID"] = PXAccess.GetBranchCD(doc.BranchID);
				parameters["DocType"] = doc.DocType;
				parameters["RefNbr"] = doc.RefNbr;
				throw new PXReportRequiredException(parameters, reportID, "Report");
			}
			return adapter.Get();
		}

		public override void CopyPasteGetScript(bool isImportSimple, List<Api.Models.Command> script, List<Api.Models.Container> containers)
		{
			CopyPasteHelper.SetBranchFieldCommandToTheTop(script);
		}
	}
}
