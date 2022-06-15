using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.Common;
using System.Collections;

namespace PX.Objects.SO.Workflow.SalesOrder
{
	using State = SOOrderStatus;
	using static SOOrder;
	using static BoundedTo<SOOrderEntry, SOOrder>;

	public class WorkflowQT : WorkflowBase
	{
		protected override void Configure(WorkflowContext<SOOrderEntry, SOOrder> context)
		{
			var conditions = context.Conditions.GetPack<Conditions>();

			var processingCategory = CommonActionCategories.Get(context).Processing;

			context.UpdateScreenConfigurationFor(screen => screen.WithActions(actions =>
			{
				actions.Add(g => g.copyOrderQT, c => c
					.WithCategory(processingCategory, g => g.putOnHold));
			})
			.WithFlows(flows =>
			{
				flows.Add<SOBehavior.qT>(flow => flow
					.WithFlowStates(flowStates =>
					{
						flowStates.Add(State.Initial, fs => fs.IsInitial(g => g.initializeState));
						flowStates.Add<State.hold>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.releaseFromHold, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add(g => g.printQuote);
									actions.Add(g => g.cancelOrder);
									actions.Add(g => g.validateAddresses);
									actions.Add(g => g.recalculateDiscountsAction);
									actions.Add(g => g.emailQuote);
								});
						});
						flowStates.Add<State.open>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.copyOrderQT, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add(g => g.printQuote);
									actions.Add(g => g.cancelOrder);
									actions.Add(g => g.validateAddresses);
									actions.Add(g => g.recalculateDiscountsAction);
									actions.Add(g => g.emailQuote);
								});
						});
						flowStates.Add<State.completed>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.reopenOrder);
									actions.Add(g => g.copyOrderQT);
									actions.Add(g => g.validateAddresses);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnOrderDeleted_ReopenQuote);
								})
								.WithFieldStates(DisableWholeScreen);
						});
						flowStates.Add<State.cancelled>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.copyOrderQT, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.reopenOrder, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.validateAddresses);
								})
								.WithFieldStates(DisableWholeScreen);
						});
					})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(State.Initial, ts =>
						{
							ts.Add(t => t // QT New Hold
								.To<State.hold>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsOnHold));
							ts.Add(t => t // QT New Open
								.To<State.open>()
								.IsTriggeredOn(g => g.initializeState));
						});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.releaseFromHold)
								.WithFieldAssignments(fas => fas.Add<hold>(false)));
							ts.Add(t => t.To<State.cancelled>().IsTriggeredOn(g => g.cancelOrder));
						});
						transitions.AddGroupFrom<State.open>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.WithFieldAssignments(fas => fas.Add<hold>(true)));
							ts.Add(t => t.To<State.cancelled>().IsTriggeredOn(g => g.cancelOrder));
						});
						transitions.AddGroupFrom<State.cancelled>(ts =>
						{
							ts.Add(t => t
							.To<State.hold>()
							.IsTriggeredOn(g => g.reopenOrder)
							.When(conditions.IsHoldEntryOrLSEntryEnabled)
							.WithFieldAssignments(fas =>
							{
								fas.Add<hold>(v => v.SetFromValue(true));
							}));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.reopenOrder)
								.WithFieldAssignments(fas =>
								{
									fas.Add<inclCustOpenOrders>(true);
									fas.Add<approved>(v => v.SetFromValue(true));
								}));
						});
						transitions.AddGroupFrom<State.completed>(ts =>
						{
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.reopenOrder)
								.WithFieldAssignments(fas => fas.Add<completed>(false)));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnOrderDeleted_ReopenQuote)
								.WithFieldAssignments(fas => fas.Add<completed>(false)));
						});
					}));
			}));
		}
	}
}