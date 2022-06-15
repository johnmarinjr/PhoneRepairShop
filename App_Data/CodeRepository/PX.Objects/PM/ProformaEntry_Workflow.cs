using System;
using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CN.ProjectAccounting.PM.GraphExtensions;

namespace PX.Objects.PM
{
	using static BoundedTo<ProformaEntry, PMProforma>;

	public partial class ProformaEntry_Workflow : PXGraphExtension<ProformaEntry>
	{
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<ProformaEntry, PMProforma>());
		}

		protected virtual void Configure(WorkflowContext<ProformaEntry, PMProforma> context)
		{
			var processingCategory = context.Categories.CreateNew(ToolbarCategory.ActionCategoryNames.Processing,
				category => category.DisplayName(ToolbarCategory.ActionCategory.Processing));
			var correctionsCategory = context.Categories.CreateNew(ToolbarCategory.ActionCategoryNames.Corrections,
				category => category.DisplayName(ToolbarCategory.ActionCategory.Corrections));
			var printingAndEmailingCategory = context.Categories.CreateNew(ToolbarCategory.ActionCategoryNames.PrintingAndEmailing,
				category => category.DisplayName(ToolbarCategory.ActionCategory.PrintingAndEmailing));
			var otherCategory = context.Categories.CreateNew(ToolbarCategory.ActionCategoryNames.Other,
				category => category.DisplayName(ToolbarCategory.ActionCategory.Other));

			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<PMProforma.status>()
					.AddDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<ProformaStatus.onHold>(flowState =>
							{
								return flowState
									.IsInitial()
									.WithActions(actions =>
									{
										actions.Add(g => g.removeHold, c => c.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
										actions.Add(g => g.send);
										actions.Add<ProformaEntryExt>(g => g.aia, c => c.IsDuplicatedInToolbar());
									});
							});
							fss.Add<ProformaStatus.open>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, c => c.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
										actions.Add(g => g.hold);
										actions.Add(g => g.send);
										actions.Add<ProformaEntryExt>(g => g.aia, c => c.IsDuplicatedInToolbar());
									})
									.WithEventHandlers(handlers =>
										handlers.Add(g => g.OnRelease));
							});
							fss.Add<ProformaStatus.closed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.send);
										actions.Add<ProformaEntryExt>(g => g.aia, c => c.IsDuplicatedInToolbar());
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.AddGroupFrom<ProformaStatus.onHold>(ts =>
							{
								ts.Add(t => t
									.To<ProformaStatus.open>()
									.IsTriggeredOn(g => g.removeHold));
							});
							transitions.AddGroupFrom<ProformaStatus.open>(ts =>
							{
								ts.Add(t => t
									.To<ProformaStatus.onHold>()
									.IsTriggeredOn(g => g.hold));
								ts.Add(t => t
									.To<ProformaStatus.closed>()
									.IsTriggeredOn(g => g.release));
								ts.Add(t => t
									.To<ProformaStatus.closed>()
									.IsTriggeredOn(g => g.OnRelease));
							});
							transitions.AddGroupFrom<ProformaStatus.closed>(ts =>
							{
							});
						}))
					.WithActions(actions =>
					{
						actions.Add(g => g.removeHold, c => c
							.InFolder(processingCategory));
						//.WithFieldAssignments(fa => fa.Add<PMProforma.hold>(f => f.SetFromValue(false))));
						actions.Add(g => g.hold, c => c
							.InFolder(processingCategory)
							.WithFieldAssignments(fa => fa.Add<PMProforma.hold>(f => f.SetFromValue(true))));
						actions.Add(g => g.release, c => c
							.InFolder(processingCategory));
						actions.Add(g => g.send, c => c
							.InFolder(printingAndEmailingCategory));
						actions.Add(g => g.proformaReport, c => c
							.InFolder(printingAndEmailingCategory));
						actions.Add<ProformaEntryExt>(g => g.aia,
							c => c.InFolder(FolderType.ReportsFolder));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<PMProforma>()
							.OfEntityEvent<PMProforma.Events>(e => e.Release)
							.Is(g => g.OnRelease)
							.UsesTargetAsPrimaryEntity());
					})
					.WithCategories(categories =>
					{
						categories.Add(processingCategory);
						categories.Add(correctionsCategory);
						categories.Add(printingAndEmailingCategory);
						categories.Add(otherCategory);
						categories.Update(FolderType.ReportsFolder, category => category.PlaceAfter(otherCategory));
					}));
		}
	}

	public class ProformaEntry_Workflow_CbApi_Adapter : PXGraphExtension<ProformaEntry>
	{
		public static bool IsActive() => true;

		public override void Initialize()
		{
			base.Initialize();
			if (!Base.IsContractBasedAPI && !Base.IsImport)
				return;

			Base.RowUpdated.AddHandler<PMProforma>(RowUpdated);

			void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
			{
				if (e.Row is PMProforma row
					&& e.OldRow is PMProforma oldRow
					&& row.Hold is bool newHold
					&& oldRow.Hold is bool oldHold
					&& newHold != oldHold)
				{
					// change it only by transition
					row.Hold = oldHold;

					Base.RowUpdated.RemoveHandler<PMProforma>(RowUpdated);

					Base.OnAfterPersist += InvokeTransition;
					void InvokeTransition(PXGraph obj)
					{
						obj.OnAfterPersist -= InvokeTransition;
						(newHold ? Base.hold : Base.removeHold).PressImpl(internalCall: true);
					}
				}
			}
		}
	}
}
