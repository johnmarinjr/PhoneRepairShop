using PX.Data;
using PX.Data.WorkflowAPI;

namespace PX.Objects.SO.Workflow.SalesOrder
{
	using State = SOOrderStatus;
	using CreatePaymentExt = GraphExtensions.SOOrderEntryExt.CreatePaymentExt;
	using Blanket = GraphExtensions.SOOrderEntryExt.Blanket;
	using static SOOrder;
	using static BoundedTo<SOOrderEntry, SOOrder>;

	public class WorkflowBL : WorkflowBase
	{
		public new class Conditions : WorkflowBase.Conditions
		{
			public Condition IsExpiredByDate => GetOrCreate(b => b.FromBql<
				expireDate.IsLess<AccessInfo.businessDate.FromCurrent>
			>());
		}

		protected override void Configure(WorkflowContext<SOOrderEntry, SOOrder> context)
		{
			var conditions = context.Conditions.GetPack<Conditions>();
			context.UpdateScreenConfigurationFor(screen => screen.WithFlows(flows =>
			{
				flows.Add<SOBehavior.bL>(flow => flow
					.WithFlowStates(flowStates =>
					{
						flowStates.Add(State.Initial, fs => fs.IsInitial(g => g.initializeState));
						flowStates.Add<State.hold>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.releaseFromHold, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add(g => g.cancelOrder);
									actions.Add<Blanket>(g => g.printBlanket);
									actions.Add(g => g.recalculateDiscountsAction);
									actions.Add<SOOrderEntryExternalTax>(e => e.recalcExternalTax);
								});
						});
						flowStates.Add<State.open>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());
									actions.Add<Blanket>(g => g.printBlanket);
									actions.Add<Blanket>(e => e.createChildOrders, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add(g => g.prepareInvoice);
									actions.Add(g => g.cancelOrder);
									actions.Add(g => g.completeOrder);
									actions.Add<Blanket>(e => e.processExpiredOrder);
									actions.Add(g => g.createPurchaseOrder);
									actions.Add<Blanket>(g => g.emailBlanket);
									actions.Add(g => g.recalculateDiscountsAction);
									actions.Add<SOOrderEntryExternalTax>(e => e.recalcExternalTax);
									actions.Add<CreatePaymentExt>(e => e.createAndCapturePayment);
									actions.Add<CreatePaymentExt>(e => e.createAndAuthorizePayment);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnBlanketCompleted);
								});
						});
						flowStates.Add<State.expired>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add(g => g.completeOrder, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add(g => g.cancelOrder);
								})
								.WithFieldStates(states =>
								{
									states.AddAllFields<SOOrder>(state => state.IsDisabled());
									states.AddField<orderType>();
									states.AddField<orderNbr>();

									states.AddAllFields<SOLine>(state => state.IsDisabled());

									states.AddTable<SOLineSplit>(state => state.IsDisabled());
									states.AddField<SOLineSplit.isAllocated>();

									states.AddTable<SOTaxTran>(state => state.IsDisabled());
									states.AddTable<SOBillingAddress>(state => state.IsDisabled());
									states.AddTable<SOBillingContact>(state => state.IsDisabled());
									states.AddTable<SOShippingAddress>(state => state.IsDisabled());
									states.AddTable<SOShippingContact>(state => state.IsDisabled());
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnBlanketCompleted);
								});
						});
						flowStates.Add<State.completed>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.prepareInvoice, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add<Blanket>(g => g.printBlanket);
									actions.Add<Blanket>(g => g.emailBlanket);
									actions.Add(g => g.reopenOrder);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnBlanketReopened);
								})
								.WithFieldStates(DisableWholeScreen);
						});
						flowStates.Add<State.cancelled>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.reopenOrder, a => a.IsDuplicatedInToolbar());
								})
								.WithFieldStates(DisableWholeScreen);
						});
					})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(State.Initial, ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsOnHold));
							ts.Add(t => t
								.To<State.completed>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsCompleted)
								.WithFieldAssignments(fas =>
								{
									fas.Add<completed>(true);
								}));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.initializeState));
						});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t
								.To<State.expired>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.When(conditions.IsExpiredByDate)
								.WithFieldAssignments(fas => fas.Add<isExpired>(true)));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.releaseFromHold));
							ts.Add(t => t
								.To<State.cancelled>()
								.IsTriggeredOn(g => g.cancelOrder));
						});
						transitions.AddGroupFrom<State.open>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold));
							ts.Add(t => t
								.To<State.expired>()
								.IsTriggeredOn<Blanket>(e => e.processExpiredOrder)
								.When(conditions.IsExpiredByDate)
								.WithFieldAssignments(fas => fas.Add<isExpired>(true)));
							ts.Add(t => t
								.To<State.completed>()
								.IsTriggeredOn(g => g.OnBlanketCompleted)
								.WithFieldAssignments(fas => fas.Add<completed>(true)));
							ts.Add(t => t
								.To<State.cancelled>()
								.IsTriggeredOn(g => g.cancelOrder));
						});
						transitions.AddGroupFrom<State.expired>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.WithFieldAssignments(fas => fas.Add<isExpired>(false)));
							ts.Add(t => t
								.To<State.completed>()
								.IsTriggeredOn(g => g.completeOrder)
								.WithFieldAssignments(fas => fas.Add<isExpired>(false)));
							ts.Add(t => t
								.To<State.completed>()
								.IsTriggeredOn(g => g.OnBlanketCompleted)
								.WithFieldAssignments(fas =>
								{
									fas.Add<isExpired>(false);
									fas.Add<completed>(true);
								}));
							ts.Add(t => t
								.To<State.cancelled>()
								.IsTriggeredOn(g => g.cancelOrder)
								.WithFieldAssignments(fas => fas.Add<isExpired>(false)));
						});
						transitions.AddGroupFrom<State.completed>(ts =>
						{
							ts.Add(t => t
								.To<State.expired>()
								.IsTriggeredOn(g => g.reopenOrder)
								.When(conditions.IsExpiredByDate)
								.WithFieldAssignments(fas => fas.Add<isExpired>(true)));
							ts.Add(t => t
								.To<State.expired>()
								.IsTriggeredOn(g => g.OnBlanketReopened)
								.When(conditions.IsExpiredByDate)
								.WithFieldAssignments(fas =>
								{
									fas.Add<isExpired>(true);
									fas.Add<completed>(false);
								}));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.reopenOrder));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnBlanketReopened)
								.WithFieldAssignments(fas => fas.Add<completed>(false)));
						});
						transitions.AddGroupFrom<State.cancelled>(ts =>
						{
							ts.Add(t => t
								.To<State.expired>()
								.IsTriggeredOn(g => g.reopenOrder)
								.When(conditions.IsExpiredByDate)
								.WithFieldAssignments(fas =>
								{
									fas.Add<approved>(true);
									fas.Add<isExpired>(true);
								}));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.reopenOrder)
								.WithFieldAssignments(fas =>
								{
									fas.Add<approved>(true);
								}));
						});
					})
				);
			}));
		}
	}
}
