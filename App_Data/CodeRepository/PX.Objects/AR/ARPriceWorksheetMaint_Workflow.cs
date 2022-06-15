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

namespace PX.Objects.AR
{
	using State = AR.SPWorksheetStatus;
	using static ARPriceWorksheet;
	using static BoundedTo<ARPriceWorksheetMaint, ARPriceWorksheet>;

	public partial class ARPriceWorksheetMaint_Workflow : PXGraphExtension<ARPriceWorksheetMaint>
	{
		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<ARPriceWorksheetMaint, ARPriceWorksheet>());

		public class Conditions : Condition.Pack
		{
			public Condition IsOnHold => GetOrCreate(c => c.FromBql<
				hold.IsEqual<True>
			>());

			public Condition IsNotOnHold => GetOrCreate(c => c.FromBql<
				hold.IsEqual<False>
			>());
		}
		protected virtual void Configure(WorkflowContext<ARPriceWorksheetMaint, ARPriceWorksheet> context)
		{
			var conditions = context.Conditions.GetPack<Conditions>();
			#region Categories
			var processingCategory = context.Categories.CreateNew(CategoryID.Processing,
				category => category.DisplayName(CategoryNames.Processing));
			var otherCategory = context.Categories.CreateNew(CategoryID.Other,
				category => category.DisplayName(CategoryNames.Other));
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
							fss.Add<State.open>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.ReleasePriceWorksheet, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
										actions.Add(g => g.putOnHold);

									});
							});
							fss.Add<State.released>();

						}
						)
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
							ts.Add(t => t.To<State.open>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false))));
						});
						transitions.AddGroupFrom<State.open>(ts =>
						{
							ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.putOnHold).WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));
							ts.Add(t => t.To<State.released>().IsTriggeredOn(g => g.ReleasePriceWorksheet));

						});
					}
					))
					.WithActions(actions =>
					{
						actions.Add(g => g.initializeState, a => a.IsHiddenAlways());
						actions.Add(g => g.releaseFromHold, c => c
							.InFolder(processingCategory)
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false))));
						actions.Add(g => g.putOnHold, c => c
							.InFolder(processingCategory)
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));
						actions.Add(g => g.ReleasePriceWorksheet, c => c
							.InFolder(processingCategory));					
					})
					.WithCategories(categories =>
					{
						categories.Add(processingCategory);
						categories.Add(otherCategory);
					})
			);
		}

		public static class CategoryNames
		{
			public const string Processing = "Processing";
			public const string Other = "Other";
		}

		public static class CategoryID
		{
			public const string Processing = "ProcessingID";
			public const string Other = "OtherID";
		}
	}
}