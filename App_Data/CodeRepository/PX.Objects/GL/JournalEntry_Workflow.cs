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

namespace PX.Objects.GL
{
	using State = BatchStatus;
	using static Batch;
	using static BoundedTo<JournalEntry, Batch>;

	public partial class JournalEntry_Workflow : PXGraphExtension<JournalEntry>
	{

		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<JournalEntry, Batch>());

		public class Conditions : Condition.Pack
		{
			public Condition IsOnHold => GetOrCreate(c => c.FromBql<
				hold.IsEqual<True>.And<released.IsEqual<False>>
			>());

			public Condition IsBalanced => GetOrCreate(c => c.FromBql<
				hold.IsEqual<False>.And<released.IsEqual<False>>
			>());

			public Condition IsScheduled => GetOrCreate(c => c.FromBql<
				scheduled.IsEqual<True>.And<released.IsEqual<False>>
			>());

			public Condition IsPosted => GetOrCreate(c => c.FromBql<
				posted.IsEqual<True>.And<released.IsEqual<True>>
			>());

			public Condition IsUnposted => GetOrCreate(c => c.FromBql<
				posted.IsEqual<False>.And<released.IsEqual<True>>
			>());

			public Condition IsNotGLModule => GetOrCreate(c => c.FromBql<
				module.IsNotEqual<GL.BatchModule.moduleGL>
			>());

			public Condition IsNotReversed => GetOrCreate(c => c.FromBql<
				reverseCount.IsEqual<Zero>
			>());

			public Condition IsDisabledSchedule => GetOrCreate(c => c.FromBql<
				module.IsNotEqual<GL.BatchModule.moduleGL>
				.Or<batchType.IsEqual<BatchTypeCode.reclassification>>
				.Or<batchType.IsEqual<BatchTypeCode.trialBalance>>
				.Or<batchType.IsEqual<BatchTypeCode.allocation>>
			>());

			public Condition IsNotReclassification => GetOrCreate(c => c.FromBql<
				batchType.IsNotEqual<BatchTypeCode.reclassification>
			>());
		}

		protected virtual void Configure(WorkflowContext<JournalEntry, Batch> context)
		{
			var conditions = context.Conditions.GetPack<Conditions>();

			#region Categories
			var processingCategory = context.Categories.CreateNew(ActionCategoryNames.Processing,
				category => category.DisplayName(ActionCategory.Processing));
			var correctionsCategory = context.Categories.CreateNew(ActionCategoryNames.Corrections,
				category => category.DisplayName(ActionCategory.Corrections));
			var customOtherCategory = context.Categories.CreateNew(ActionCategoryNames.CustomOther,
				category => category.DisplayName(ActionCategory.Other));
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
										actions.Add(g => g.editReclassBatch, a => a.IsDuplicatedInToolbar());
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
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
										actions.Add(g => g.putOnHold);
										actions.Add(g => g.createSchedule);
										actions.Add(g => g.editReclassBatch, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.glEditDetails);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnConfirmSchedule);
										handlers.Add(g => g.OnReleaseBatch);
										handlers.Add(g => g.OnUpdateStatus);
									});
							});
							fss.Add<State.scheduled>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.createSchedule, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.glEditDetails);
									})
									.WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnConfirmSchedule);
										handlers.Add(g => g.OnVoidSchedule);
									});
							});
							fss.Add<State.voided>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.glEditDetails);
									});
							});
							fss.Add<State.unposted>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.reverseBatch);
										actions.Add(g => g.reclassify);
										actions.Add(g => g.batchRegisterDetails);
										actions.Add(g => g.glReversingBatches);
										actions.Add(g => g.post, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									})
									.WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnPostBatch);
									});
							});
							fss.Add<State.posted>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.reverseBatch);
										actions.Add(g => g.reclassify);
										actions.Add(g => g.batchRegisterDetails);
										actions.Add(g => g.glReversingBatches);
									});
							});
						})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(initialState, ts =>
						{
								ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.initializeState).When(conditions.IsOnHold)); // New Hold
								ts.Add(t => t.To<State.balanced>().IsTriggeredOn(g => g.initializeState).When(conditions.IsBalanced)); // New Balance
								ts.Add(t => t.To<State.unposted>().IsTriggeredOn(g => g.initializeState).When(conditions.IsUnposted)); // New Balance
								ts.Add(t => t.To<State.scheduled>().IsTriggeredOn(g => g.initializeState).When(conditions.IsScheduled)); // New Reserved
						});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.DoesNotPersist()
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false))));
							ts.Add(t => t
								.To<State.balanced>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsBalanced));
						});
						transitions.AddGroupFrom<State.balanced>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.DoesNotPersist()
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));
							ts.Add(t => t
								.To<State.scheduled>()
								.IsTriggeredOn(g => g.OnConfirmSchedule)
								.WithFieldAssignments(fas =>
								{
									fas.Add<scheduled>(e => e.SetFromValue(true));
									fas.Add<scheduleID>(e => e.SetFromExpression("@ScheduleID"));
								}));
							ts.Add(t => t
								.To<State.unposted>()
								.IsTriggeredOn(g => g.OnReleaseBatch));
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsOnHold));
							ts.Add(t => t
								.To<State.unposted>()
								.IsTriggeredOn(g => g.OnUpdateStatus)
								.When(conditions.IsUnposted));
						});
						transitions.AddGroupFrom<State.unposted>(ts =>
						{
							ts.Add(t => t
								.To<State.posted>()
								.IsTriggeredOn(g => g.OnPostBatch));
						});
						transitions.AddGroupFrom<State.scheduled>(ts =>
						{
							ts.Add(t => t
								.To<State.scheduled>()
								.IsTriggeredOn(g => g.OnConfirmSchedule)
								.WithFieldAssignments(fas =>
								{
									fas.Add<scheduled>(e => e.SetFromValue(true));
									fas.Add<scheduleID>(e => e.SetFromExpression("@ScheduleID"));
								}));
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnVoidSchedule)
								.WithFieldAssignments(fas =>
								{
									fas.Add<voided>(e => e.SetFromValue(true));
									fas.Add<scheduled>(e => e.SetFromValue(false));
									fas.Add<scheduleID>(e => e.SetFromValue(null));
								}));
						});
						
					}))
					.WithActions(actions =>
					{
						actions.Add(g => g.initializeState, a => a.IsHiddenAlways());
						actions.Add(g => g.releaseFromHold, c => c
							.InFolder(processingCategory)
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.IsHiddenWhen(conditions.IsNotGLModule)
							.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false))));
						actions.Add(g => g.putOnHold, c => c
							.InFolder(processingCategory)
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.IsHiddenWhen(conditions.IsNotGLModule)
							.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));
						actions.Add(g => g.editReclassBatch, c => c
							.InFolder(correctionsCategory)
							.IsHiddenWhen(conditions.IsNotReclassification));
						actions.Add(g => g.release, c => c
							.InFolder(processingCategory)
							.PlaceAfter(nameof(JournalEntry.Last))
							.IsDisabledWhen(conditions.IsNotGLModule));
						actions.Add(g => g.post, c => c
							.InFolder(processingCategory)
							.PlaceAfter(nameof(JournalEntry.Release)));
						//Unable to define availability - complex condition depended on transaction
						actions.Add(g => g.reverseBatch, c => c
							.InFolder(correctionsCategory));
						actions.Add(g => g.createSchedule, c => c
							.InFolder(customOtherCategory)
							.IsDisabledWhen(conditions.IsDisabledSchedule));
						//Unable to define availability - complex condition depended on transaction
						actions.Add(g => g.reclassify, c => c
							.InFolder(correctionsCategory));
						actions.Add(g => g.batchRegisterDetails, c => c.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.glEditDetails, c => c.InFolder(FolderType.ReportsFolder));
						actions.Add(g => g.glReversingBatches, c => c
							.InFolder(FolderType.ReportsFolder)
							.IsDisabledWhen(conditions.IsNotReversed));

					})
					.WithHandlers(handlers =>
						{
							handlers.Add(handler => handler
								.WithTargetOf<Batch>()
								.WithParametersOf<GL.Schedule>()
								.OfEntityEvent<Batch.Events>(e => e.ConfirmSchedule)
								.Is(g => g.OnConfirmSchedule)
								.UsesTargetAsPrimaryEntity());
							handlers.Add(handler => handler
								.WithTargetOf<Batch>()
								.WithParametersOf<GL.Schedule>()
								.OfEntityEvent<Batch.Events>(e => e.VoidSchedule)
								.Is(g => g.OnVoidSchedule)
								.UsesTargetAsPrimaryEntity());
							handlers.Add(handler => handler
								.WithTargetOf<Batch>()
								.OfEntityEvent<Batch.Events>(e => e.ReleaseBatch)
								.Is(g => g.OnReleaseBatch)
								.UsesTargetAsPrimaryEntity());
							handlers.Add(handler => handler
								.WithTargetOf<Batch>()
								.OfEntityEvent<Batch.Events>(e => e.PostBatch)
								.Is(g => g.OnPostBatch)
								.UsesTargetAsPrimaryEntity());
							handlers.Add(handler => handler
								.WithTargetOf<Batch>()
								.OfFieldsUpdated<BqlFields.FilledWith<Batch.hold, Batch.released>>()
								.Is(g => g.OnUpdateStatus)
								.UsesTargetAsPrimaryEntity());
						})
					.WithCategories(categories =>
					{
						categories.Add(processingCategory);
						categories.Add(correctionsCategory);
						categories.Add(customOtherCategory);
						categories.Update(FolderType.ReportsFolder, category => category.PlaceAfter(customOtherCategory));
					})
			);
		}
	
		public static class ActionCategoryNames
		{
			public const string Processing = "Processing";
			public const string Corrections = "Corrections";
			public const string CustomOther = "CustomOther";
		}

		public static class ActionCategory
		{
			public const string Processing = "Processing";
			public const string Corrections = "Corrections";
			public const string Other = "Other";
		}
	}
}