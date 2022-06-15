using System;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.WorkflowAPI;
using PX.Objects.Common;

namespace PX.Objects.IN
{
	using State = INDocStatus;
	using static INRegister;

	public abstract class INRegisterEntryBase_Workflow<TGraph, TDocType> : PXGraphExtension<TGraph>
		where TGraph : INRegisterEntryBase, new()
		where TDocType : IConstant, IBqlOperand, IImplement<IBqlString>
	{
		public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<TGraph, INRegister>());

		protected virtual void Configure(WorkflowContext<TGraph, INRegister> context)
		{
			BoundedTo<TGraph, INRegister>.Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsReleased
					= Bql<released.IsEqual<True>>(),

				IsOnHold
					= Bql<hold.IsEqual<True>>(),

				MatchDocumentType
					= Bql<docType.IsEqual<TDocType>>(),

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
											actions.Add(g => g.iNEdit);
										})
										.WithEventHandlers(handlers =>
										{
											handlers.Add(g => g.OnDocumentReleased);
										});
								});
								flowStates.Add<State.released>(flowState =>
								{
									return flowState
										.WithActions(actions =>
										{
											actions.Add(g => g.iNRegisterDetails);
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
								transitions.Add(t => t.From<State.balanced>().To<State.released>().IsTriggeredOn(g => g.OnDocumentReleased).When(conditions.IsReleased));
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

						#region Reports
						actions.Add(g => g.iNEdit, a => a
							.WithCategory(PredefinedCategory.Reports));
						actions.Add(g => g.iNRegisterDetails, a => a
							.WithCategory(PredefinedCategory.Reports));
						#endregion
					})
					.WithCategories(categories =>
					{
						categories.Add(processingCategory);
						categories.Update(FolderType.ReportsFolder, category => category.PlaceAfter(processingCategory));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler =>
							handler
							.WithTargetOf<INRegister>()
							.OfEntityEvent<Events>(e => e.DocumentReleased)
							.Is(g => g.OnDocumentReleased)
							.UsesTargetAsPrimaryEntity()
							.AppliesWhen(conditions.MatchDocumentType)
							.WithFieldAssignments(fass =>
							{
								fass.Add<released>(true);
								fass.Add<releasedToVerify>(false);
							}));
					});
			});
		}
	}
}