using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ProjectDefinition.Workflow;
using PX.Data.WorkflowAPI;
using PX.Objects.Common.Extensions;
using PX.Objects.CR.Workflows;
using PX.Objects.CS;

namespace PX.Objects.CA
{
	using State = CADocStatus;
	using static CARecon;
	using static BoundedTo<CAReconEntry, CARecon>;

	public partial class CAReconEntry_Workflow : PXGraphExtension<CAReconEntry>
	{
		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<CAReconEntry, CARecon>());

		public class Conditions : Condition.Pack
		{
			public Condition IsOnHold => GetOrCreate(c => c.FromBql<
				hold.IsEqual<True>
			>());

			public Condition IsNotOnHold => GetOrCreate(c => c.FromBql<
				hold.IsEqual<False>
			>());

			public Condition IsVoided => GetOrCreate(c => c.FromBql<
				voided.IsEqual<True>
			>());
		}

		protected virtual void Configure(WorkflowContext<CAReconEntry, CARecon> context)
			{
			var conditions = context.Conditions.GetPack<Conditions>();


			#region Categories
			var processingCategory = context.Categories.CreateNew(CategoryID.Processing,
				category => category.DisplayName(CategoryNames.Processing));
			var correctionsCategory = context.Categories.CreateNew(CategoryID.Corrections,
				category => category.DisplayName(CategoryNames.Corrections));
			#endregion

			const string initialState = "_";

			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<status>()
					.AddDefaultFlow(flow =>
						flow
						.WithFlowStates(fss =>
						{
							fss.Add(initialState, flowState => flowState.IsInitial(g => g.initializeState));
							fss.Add<State.hold>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.releaseFromHold, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
										actions.Add(g => g.printReconciliationReport);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnUpdateStatus);
									});
							});
							fss.Add<State.balanced>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.Release, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
										actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printReconciliationReport);


									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnUpdateStatus);
									});;;
							});
							fss.Add<State.closed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.Voided, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printReconciliationReport);
									});
							});
							fss.Add<State.voided>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.printReconciliationReport);
									});
							});

						})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(initialState, ts =>
						{
							ts.Add(t => t.To<State.hold>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsOnHold)); // New Hold
							ts.Add(t => t.To<State.balanced>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsNotOnHold)); // New Balanced
						});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t.To<State.balanced>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false))));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsNotOnHold));
						});
						transitions.AddGroupFrom<State.balanced>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));
							ts.Add(t => t.To<State.closed>()
								.IsTriggeredOn(g => g.Release));
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsOnHold));
						});
						transitions.AddGroupFrom<State.closed>(ts =>
						{
							ts.Add(t => t.To<State.voided>().IsTriggeredOn(g => g.Voided).When(conditions.IsVoided));
						});
					}
					))
					.WithActions(actions =>
					{
						actions.Add(g => g.initializeState, a => a.IsHiddenAlways());
						actions.Add(g => g.putOnHold, c => c
							.InFolder(processingCategory)
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));
						actions.Add(g => g.releaseFromHold, c => c
							.InFolder(processingCategory)
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false)))); 
						actions.Add(g => g.Release, c => c
							.InFolder(processingCategory)
							.PlaceAfter(nameof(CAReconEntry.Last)));
						actions.Add(g => g.Voided, c => c
							.InFolder(correctionsCategory));
						actions.Add(g => g.printReconciliationReport, c => c
							.InFolder(FolderType.ReportsFolder));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<CARecon>()
							.OfFieldUpdated<CARecon.hold>()
							.Is(g => g.OnUpdateStatus)
							.UsesTargetAsPrimaryEntity());
					})
					.WithCategories(categories =>
					{
						categories.Add(processingCategory);
						categories.Add(correctionsCategory);
					})
			);
		}

		public static class CategoryNames
		{
			public const string Processing = "Processing";
			public const string Corrections = "Corrections";
		}

		public static class CategoryID
		{
			public const string Processing = "ProcessingID";
			public const string Corrections = "CorrectionsID";
		}
	}
}