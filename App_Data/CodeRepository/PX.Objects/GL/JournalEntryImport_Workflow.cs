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
	using State = TrialBalanceImportMapStatusAttribute;
	using static GLTrialBalanceImportMap;
	using static BoundedTo<JournalEntryImport, GLTrialBalanceImportMap>;

	public partial class JournalEntryImport_Workflow : PXGraphExtension<JournalEntryImport>
	{
		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<JournalEntryImport, GLTrialBalanceImportMap>());
		
		public class Conditions : Condition.Pack
		{
			public Condition IsOnHold => GetOrCreate(c => c.FromBql<
				isHold.IsEqual<True>
			>());

			public Condition IsNotOnHold => GetOrCreate(c => c.FromBql<
				isHold.IsEqual<False>
			>());
		}
		protected virtual void Configure(WorkflowContext<JournalEntryImport, GLTrialBalanceImportMap> context)
		{
			var conditions = context.Conditions.GetPack<Conditions>();
			#region Categories
			var processingCategory = context.Categories.CreateNew(ActionCategoryNames.Processing,
				category => category.DisplayName(ActionCategory.Processing));
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

									});
							});
							fss.Add<State.balanced>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
										actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());

									});
							});
							fss.Add<State.released>();
						})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(initialState, ts =>
						{
							ts.Add(t => t.To<State.hold>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsOnHold)); // New Hold
						});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t.To<State.balanced>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.DoesNotPersist()
								.WithFieldAssignments(fas => fas.Add<isHold>(f => f.SetFromValue(false))));
						});
						transitions.AddGroupFrom<State.balanced>(ts =>
						{
							ts.Add(t => t.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.DoesNotPersist()
								.WithFieldAssignments(fas => fas.Add<isHold>(f => f.SetFromValue(true))));
							ts.Add(t => t.To<State.released>().IsTriggeredOn(g => g.release));
						});
					}
					))
					.WithActions(actions =>
					{
						actions.Add(g => g.initializeState, a => a.IsHiddenAlways());
						actions.Add(g => g.releaseFromHold, c => c
							.InFolder(processingCategory)
							.PlaceAfter(nameof(JournalEntryImport.Last))
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.WithFieldAssignments(fas => fas.Add<isHold>(f => f.SetFromValue(false))));
						actions.Add(g => g.putOnHold, c => c
							.InFolder(processingCategory)
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.WithFieldAssignments(fas => fas.Add<isHold>(f => f.SetFromValue(true))));
						actions.Add(g => g.release, c => c
							.InFolder(processingCategory));
					})
					.WithCategories(categories =>
					{
						categories.Add(processingCategory);
					})
			);
		}

		public static class ActionCategoryNames
		{
			public const string Processing = "Processing";
		}

		public static class ActionCategory
		{
			public const string Processing = "Processing";
		}
	}
}