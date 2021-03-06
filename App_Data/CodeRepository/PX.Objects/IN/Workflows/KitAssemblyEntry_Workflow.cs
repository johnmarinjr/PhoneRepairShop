using System;
using PX.Common;
using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.Common;

namespace PX.Objects.IN
{
	using State = INDocStatus;
	using static INKitRegister;
	using static BoundedTo<KitAssemblyEntry, INKitRegister>;

	public class KitAssemblyEntry_Workflow : PXGraphExtension<KitAssemblyEntry>
	{
		public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<KitAssemblyEntry, INKitRegister>());

		protected virtual void Configure(WorkflowContext<KitAssemblyEntry, INKitRegister> context)
		{
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsReleased
					= Bql<released.IsEqual<True>>(),

				IsOnHold
					= Bql<hold.IsEqual<True>>(),

				IsDisassembly
					= Bql<docType.IsEqual<INDocType.disassembly>>(),

				HasBatchNbr
					= Bql<batchNbr.IsNotNull.And<batchNbr.IsNotEqual<Empty>>>(),
			}.AutoNameConditions();

			#region Categories
			var processingCategory = CommonActionCategories.Get(context).Processing;
			#endregion

			const string initialState = "_";
			context.AddScreenConfigurationFor(screen =>
			{
				return screen
					.StateIdentifierIs<status>()
					.AddDefaultFlow(flow =>
					{
						return flow
							.WithFlowStates(flowStates =>
							{
								flowStates.Add(initialState, flowState => flowState.IsInitial(g => g.initializeState));
								flowStates.Add<State.hold>(flowState =>
								{
									return flowState
										.WithActions(actions =>
										{
											actions.Add(g => g.releaseFromHold, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
										});
								});
								flowStates.Add<State.balanced>(flowState =>
								{
									return flowState
										.WithActions(actions =>
										{
											actions.Add(g => g.release, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
											actions.Add(g => g.putOnHold);
										});
								});
								flowStates.Add<State.released>(flowState =>
								{
									return flowState
										.WithActions(actions =>
										{
											actions.Add(g => g.viewBatch, a => a.IsDuplicatedInToolbar());
										});
								});
							})
							.WithTransitions(transitions =>
							{
								transitions.AddGroupFrom(initialState, ts =>
								{
									ts.Add(t => t.To<State.released>().IsTriggeredOn(g => g.initializeState).When(conditions.IsReleased));
									ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.initializeState).When(conditions.IsOnHold));
									ts.Add(t => t.To<State.balanced>().IsTriggeredOn(g => g.initializeState));
								});

								transitions.Add(t => t.From<State.hold>().To<State.balanced>().IsTriggeredOn(g => g.releaseFromHold).When(!conditions.IsOnHold));
								transitions.Add(t => t.From<State.balanced>().To<State.hold>().IsTriggeredOn(g => g.putOnHold).When(conditions.IsOnHold));
							});
					})
					.WithActions(actions =>
					{
						actions.Add(g => g.initializeState, a => a.IsHiddenAlways());

						#region Processing
						actions.Add(g => g.releaseFromHold, a => a
							.WithCategory(processingCategory)
							.WithFieldAssignments(fass => fass.Add<hold>(false)));
						actions.Add(g => g.putOnHold, a => a
							.WithCategory(processingCategory)
							.WithFieldAssignments(fass => fass.Add<hold>(true)));
						actions.Add(g => g.release, a => a
							.WithCategory(processingCategory));
						#endregion

						#region Inquiries
						actions.Add(g => g.viewBatch, a => a
							.WithCategory(PredefinedCategory.Inquiries)
							.IsDisabledWhen(!conditions.HasBatchNbr));
						#endregion
					})
					.WithCategories(categories =>
					{
						categories.Add(processingCategory);
						categories.Update(FolderType.InquiriesFolder, category => category.PlaceAfter(processingCategory));
					})
					.WithFieldStates(fieldStates =>
					{
						fieldStates.Add<INKitSpecStkDet.allowQtyVariation>(s => s.IsDisabledAlways().IsHiddenWhen(conditions.IsDisassembly));
						fieldStates.Add<INKitSpecStkDet.maxCompQty>(s => s.IsDisabledAlways().IsHiddenWhen(conditions.IsDisassembly));
						fieldStates.Add<INKitSpecStkDet.minCompQty>(s => s.IsDisabledAlways().IsHiddenWhen(conditions.IsDisassembly));
						fieldStates.Add<INKitSpecStkDet.disassemblyCoeff>(s => s.IsDisabledAlways().IsHiddenWhen(!conditions.IsDisassembly));

						fieldStates.Add<INKitSpecNonStkDet.allowQtyVariation>(s => s.IsDisabledAlways().IsHiddenWhen(conditions.IsDisassembly));
						fieldStates.Add<INKitSpecNonStkDet.maxCompQty>(s => s.IsDisabledAlways().IsHiddenWhen(conditions.IsDisassembly));
						fieldStates.Add<INKitSpecNonStkDet.minCompQty>(s => s.IsDisabledAlways().IsHiddenWhen(conditions.IsDisassembly));
					});
			});
		}
	}
}