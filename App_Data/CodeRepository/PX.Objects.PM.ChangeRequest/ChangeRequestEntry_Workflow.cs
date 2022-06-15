using System;
using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.PM;

namespace PX.Objects.PM.ChangeRequest
{
	using static BoundedTo<ChangeRequestEntry, PMChangeRequest>;

	public partial class ChangeRequestEntry_Workflow : PXGraphExtension<ChangeRequestEntry>
	{
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<ChangeRequestEntry, PMChangeRequest>());
		}

		protected virtual void Configure(WorkflowContext<ChangeRequestEntry, PMChangeRequest> context)
		{
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsHoldDisabled
					= Bql<PMChangeRequest.changeOrderNbr.IsNotNull.Or<PMChangeRequest.costChangeOrderNbr.IsNotNull>>()
			}.AutoNameConditions();

			var processingCategory = context.Categories.CreateNew(ToolbarCategory.ActionCategoryNames.Processing,
				category => category.DisplayName(ToolbarCategory.ActionCategory.Processing));
			var printingAndEmailingCategory = context.Categories.CreateNew(ToolbarCategory.ActionCategoryNames.PrintingAndEmailing,
				category => category.DisplayName(ToolbarCategory.ActionCategory.PrintingAndEmailing));

			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<PMChangeRequest.status>()
					.AddDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<ChangeRequestStatus.onHold>(flowState =>
							{
								return flowState
									.IsInitial()
									.WithActions(actions =>
									{
										actions.Add(g => g.removeHold, c => c.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
										actions.Add(g => g.send);

									});
							});
							fss.Add<ChangeRequestStatus.open>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.hold);
										actions.Add(g => g.createChangeOrder, c => c.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
										actions.Add(g => g.send);
									})
									.WithEventHandlers(handlers =>
										handlers.Add(g => g.OnClose));
							});
							fss.Add<ChangeRequestStatus.closed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.send);
									})
									.WithEventHandlers(handlers =>
										handlers.Add(g => g.OnOpen));
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.AddGroupFrom<ChangeRequestStatus.onHold>(ts =>
							{
								ts.Add(t => t
									.To<ChangeRequestStatus.open>()
									.IsTriggeredOn(g => g.removeHold));
							});
							transitions.AddGroupFrom<ChangeRequestStatus.open>(ts =>
							{
								ts.Add(t => t
									.To<ChangeRequestStatus.onHold>()
									.IsTriggeredOn(g => g.hold));
								ts.Add(t => t
									.To<ChangeRequestStatus.closed>()
									.IsTriggeredOn(g => g.OnClose));
							});
							transitions.AddGroupFrom<ChangeRequestStatus.closed>(ts =>
							{
								ts.Add(t => t
									.To<ChangeRequestStatus.open>()
									.IsTriggeredOn(g => g.OnOpen));
							});
						}))
					.WithActions(actions =>
					{
						actions.Add(g => g.removeHold, c => c
							.InFolder(processingCategory));
						//.WithFieldAssignments(fa => fa.Add<PMChangeRequest.hold>(f => f.SetFromValue(false))));
						actions.Add(g => g.hold, c => c
							.InFolder(processingCategory)
							.IsDisabledWhen(conditions.IsHoldDisabled)
							.WithFieldAssignments(fa => fa.Add<PMChangeRequest.hold>(f => f.SetFromValue(true))));
						actions.Add(g => g.createChangeOrder, c => c
							.InFolder(processingCategory));
						actions.Add(g => g.send, c => c
							.InFolder(printingAndEmailingCategory));
						actions.Add(g => g.crReport, c => c
							.InFolder(printingAndEmailingCategory));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<PMChangeRequest>()
							.OfEntityEvent<PMChangeRequest.Events>(e => e.Open)
							.Is(g => g.OnOpen)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<PMChangeRequest>()
							.OfEntityEvent<PMChangeRequest.Events>(e => e.Close)
							.Is(g => g.OnClose)
							.UsesTargetAsPrimaryEntity());
					})
					.WithCategories(categories =>
					{
						categories.Add(processingCategory);
						categories.Add(printingAndEmailingCategory);
					}));
		}
	}

	public class ChangeRequestEntry_Workflow_CbApi_Adapter : PXGraphExtension<ChangeRequestEntry>
	{
		public static bool IsActive() => true;

		public override void Initialize()
		{
			base.Initialize();
			if (!Base.IsContractBasedAPI && !Base.IsImport)
				return;

			Base.RowUpdated.AddHandler<PMChangeRequest>(RowUpdated);

			void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
			{
				if (e.Row is PMChangeRequest row
					&& e.OldRow is PMChangeRequest oldRow
					&& row.Hold is bool newHold
					&& oldRow.Hold is bool oldHold
					&& newHold != oldHold)
				{
					// change it only by transition
					row.Hold = oldHold;

					Base.RowUpdated.RemoveHandler<PMChangeRequest>(RowUpdated);

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