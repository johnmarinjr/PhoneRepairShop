using System;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;
using PX.Objects.Common;

namespace PX.Objects.RQ
{
	using State = RQRequestStatus;
	using static RQRequest;
	using static BoundedTo<RQRequestEntry, RQRequest>;

	public class RQRequestEntry_Workflow : PXGraphExtension<RQRequestEntry>
	{
		public class Conditions : Condition.Pack
		{
			public Condition IsCancelled => GetOrCreate(b => b.FromBql<
				cancelled.IsEqual<True>
			>());

			public Condition IsOnHold => GetOrCreate(b => b.FromBql<
				hold.IsEqual<True>
			>());

			public Condition HasOpenOrderQty => GetOrCreate(b => b.FromBql<
				openOrderQty.IsGreater<CS.decimal0>
			>());

			public Condition HasZeroOpenOrderQty => GetOrCreate(b => b.FromBql<
				openOrderQty.IsEqual<CS.decimal0>
			>());
		}

		public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<RQRequestEntry, RQRequest>());

		protected virtual void Configure(WorkflowContext<RQRequestEntry, RQRequest> context)
		{
			var conditions = context.Conditions.GetPack<Conditions>();


			#region Categories
			var commonCategories = CommonActionCategories.Get(context);
			var processingCategory = commonCategories.Processing;
			var printingEmailingCategory = commonCategories.PrintingAndEmailing;
			var otherCategory = commonCategories.Other;
			#endregion

			const string initialState = "_";
			context.AddScreenConfigurationFor(screen =>
				screen
				.StateIdentifierIs<status>()
				.AddDefaultFlow(flow =>
					flow
					.WithFlowStates(flowStates =>
					{
						flowStates.Add(initialState, flowState => flowState.IsInitial(g => g.initializeState));
						flowStates.Add<State.hold>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.releaseFromHold, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add(g => g.requestForm);
								});
						});
						flowStates.Add<State.open>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.putOnHold);
									actions.Add(g => g.cancelRequest);
									actions.Add(g => g.requestForm);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnOpenOrderQtyExhausted);
								})
								.WithFieldStates(fieldStates =>
								{
									DisableWholeScreen(fieldStates);

									// but enable these
									fieldStates.AddField<RQRequestLine.requestedDate>();
									fieldStates.AddField<RQRequestLine.promisedDate>();
									fieldStates.AddField<RQRequestLine.cancelled>();
									fieldStates.AddField<RQRequestLine.inventoryID>();
									fieldStates.AddField<RQRequestLine.subItemID>();
									fieldStates.AddField<RQRequestLine.description>();
									fieldStates.AddField<RQRequestLine.orderQty>();
									fieldStates.AddField<RQRequestLine.uOM>();
									fieldStates.AddField<RQRequestLine.estUnitCost>();
								});
						});
						flowStates.Add<State.closed>(flowState =>
						{
							return flowState // OpenOrderQty == 0
								.WithActions(actions =>
								{
									actions.Add(g => g.requestForm);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnOpenOrderQtyIncreased);
								})
								.WithFieldStates(DisableWholeScreen);
						});
						flowStates.Add<State.canceled>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.putOnHold);
									actions.Add(g => g.requestForm);
								})
								.WithFieldStates(DisableWholeScreen);
						});
					})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(initialState, ts =>
						{
							ts.Add(t => t.To<State.canceled>().IsTriggeredOn(g => g.initializeState).When(conditions.IsCancelled));
							ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.initializeState).When(conditions.IsOnHold));
							ts.Add(t => t.To<State.closed>().IsTriggeredOn(g => g.initializeState).When(conditions.HasZeroOpenOrderQty));
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.initializeState));
						});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t.To<State.closed>().IsTriggeredOn(g => g.releaseFromHold).When(conditions.HasZeroOpenOrderQty));
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.releaseFromHold));
						});
						transitions.AddGroupFrom<State.open>(ts =>
						{
							ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.putOnHold).When(conditions.IsOnHold));
							ts.Add(t => t.To<State.canceled>().IsTriggeredOn(g => g.cancelRequest).When(conditions.IsCancelled));
							ts.Add(t => t.To<State.closed>().IsTriggeredOn(g => g.OnOpenOrderQtyExhausted));
						});
						transitions.AddGroupFrom<State.closed>(ts =>
						{
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.OnOpenOrderQtyIncreased));
						});
						transitions.AddGroupFrom<State.canceled>(ts =>
						{
							ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.putOnHold).When(conditions.IsOnHold));
						});
					}))
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
					actions.Add(g => g.cancelRequest, a => a
						.WithCategory(processingCategory)
						.WithFieldAssignments(fass => fass.Add<cancelled>(true)));
					#endregion

					#region Printing and Emailing			
					actions.Add(g => g.requestForm, a => a
						.WithCategory(printingEmailingCategory));
					#endregion
					#region Other
					actions.Add(g => g.validateAddresses, a => a
						.WithCategory(otherCategory));
					#endregion
				})
				.WithCategories(categories =>
				{
					categories.Add(processingCategory);
					categories.Add(printingEmailingCategory);
					categories.Add(otherCategory);
				})
				.WithHandlers(handlers =>
				{
					handlers.Add(handler => handler
						.WithTargetOf<RQRequest>()
						.OfEntityEvent<Events>(e => e.OpenOrderQtyChanged)
						.Is(g => g.OnOpenOrderQtyExhausted)
						.UsesTargetAsPrimaryEntity()
						.AppliesWhen(conditions.HasZeroOpenOrderQty));
					handlers.Add(handler => handler
						.WithTargetOf<RQRequest>()
						.OfEntityEvent<Events>(e => e.OpenOrderQtyChanged)
						.Is(g => g.OnOpenOrderQtyIncreased)
						.UsesTargetAsPrimaryEntity()
						.AppliesWhen(conditions.HasOpenOrderQty));
				}));
		}
		public static void DisableWholeScreen(FieldState.IContainerFillerFields fieldStates)
		{
			fieldStates.AddAllFields<RQRequest>(fs => fs.IsDisabled());
			fieldStates.AddField<RQRequest.orderNbr>();
			fieldStates.AddTable<RQRequestLine>(fs => fs.IsDisabled());
			fieldStates.AddTable<PO.POShipAddress>(fs => fs.IsDisabled());
			fieldStates.AddTable<PO.POShipContact>(fs => fs.IsDisabled());
			fieldStates.AddTable<CM.CurrencyInfo>(fs => fs.IsDisabled());
		}
	}
}
