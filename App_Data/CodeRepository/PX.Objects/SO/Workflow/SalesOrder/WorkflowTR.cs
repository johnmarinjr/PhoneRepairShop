using PX.Data;
using PX.Data.WorkflowAPI;

namespace PX.Objects.SO.Workflow.SalesOrder
{
	using State = SOOrderStatus;
	using static SOOrder;
	using static BoundedTo<SOOrderEntry, SOOrder>;

	public class WorkflowTR : WorkflowBase
	{
		protected override void Configure(WorkflowContext<SOOrderEntry, SOOrder> context)
		{
			var conditions = context.Conditions.GetPack<WorkflowSO.Conditions>();
			context.UpdateScreenConfigurationFor(screen => screen.WithFlows(flows =>
			{
				flows.Add<SOBehavior.tR>(flow => flow
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
									actions.Add(g => g.printSalesOrder);
									actions.Add(g => g.copyOrder);
									actions.Add(g => g.validateAddresses);
								});
						});
						flowStates.Add<State.open>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.createShipmentIssue, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.printSalesOrder);
									actions.Add(g => g.cancelOrder);
									actions.Add(g => g.completeOrder);
									actions.Add(g => g.placeOnBackOrder);
									actions.Add(g => g.createPurchaseOrder);
									actions.Add(g => g.createTransferOrder);
									actions.Add(g => g.emailSalesOrder);
									actions.Add(g => g.copyOrder);
									actions.Add(g => g.validateAddresses);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnShipmentLinked);
									handlers.Add(g => g.OnShipmentCreationFailed);
								});
						});
						flowStates.Add<State.shipping>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.createShipmentIssue);
									actions.Add(g => g.emailSalesOrder);
									actions.Add(g => g.createPurchaseOrder);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnShipmentUnlinked);
									handlers.Add(g => g.OnShipmentConfirmed);
								})
								.WithFieldStates(DisableWholeScreen);
						});
						flowStates.Add<State.backOrder>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.openOrder, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add(g => g.createShipmentIssue, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.cancelOrder);
									actions.Add(g => g.completeOrder);
									actions.Add(g => g.putOnHold);
									actions.Add(g => g.emailSalesOrder);
									actions.Add(g => g.copyOrder);
									actions.Add(g => g.validateAddresses);
									actions.Add(g => g.createPurchaseOrder);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnShipmentLinked);
									handlers.Add(g => g.OnShipmentCorrected);
								});
						});
						flowStates.Add<State.completed>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.printSalesOrder);
									actions.Add(g => g.emailSalesOrder);
									actions.Add(g => g.copyOrder);
									actions.Add(g => g.validateAddresses);
									actions.Add(g => g.reopenOrder);
								})
								.WithEventHandlers(handlers =>
								{
									handlers.Add(g => g.OnShipmentCorrected);
								})
								.WithFieldStates(DisableWholeScreen);
						});
						flowStates.Add<State.cancelled>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.copyOrder, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.reopenOrder, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add(g => g.printSalesOrder);
									actions.Add(g => g.validateAddresses);
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
								.WithFieldAssignments(fas => fas.Add<completed>(true)));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.initializeState));
						});
						transitions.AddGroupFrom<State.hold>(ts =>
						{
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.releaseFromHold));
							ts.Add(t => t.To<State.cancelled>().IsTriggeredOn(g => g.cancelOrder));
						});
						transitions.AddGroupFrom<State.open>(ts =>
						{
							ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.putOnHold));
							ts.Add(t => t.To<State.cancelled>().IsTriggeredOn(g => g.cancelOrder));
							ts.Add(t => t.To<State.shipping>().IsTriggeredOn(g => g.OnShipmentLinked));
							ts.Add(t => t
								.To<State.backOrder>()
								.IsTriggeredOn(g => g.placeOnBackOrder)
								.WithFieldAssignments(fas => fas.Add<backOrdered>(true)));
							ts.Add(t => t
								.To<State.backOrder>()
								.IsTriggeredOn(g => g.OnShipmentCreationFailed)
								.WithFieldAssignments(fas => fas.Add<backOrdered>(true)));
						});
						transitions.AddGroupFrom<State.shipping>(ts =>
						{
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnShipmentUnlinked)
								.When(conditions.IsShippable));
							ts.Add(t => t
								.To<State.completed>()
								.IsTriggeredOn(g => g.OnShipmentConfirmed)
								.When(conditions.IsShippingCompleted)
								.WithFieldAssignments(fas => fas.Add<completed>(true)));
							ts.Add(t => t
								.To<State.backOrder>()
								.IsTriggeredOn(g => g.OnShipmentConfirmed)
								.When(conditions.IsShippable)
								.WithFieldAssignments(fas => fas.Add<backOrdered>(true)));
						});
						transitions.AddGroupFrom<State.backOrder>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.WithFieldAssignments(fas => fas.Add<backOrdered>(false)));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.openOrder)
								.WithFieldAssignments(fas => fas.Add<backOrdered>(false)));
							ts.Add(t => t.To<State.shipping>().IsTriggeredOn(g => g.OnShipmentLinked));
							ts.Add(t => t.To<State.shipping>().IsTriggeredOn(g => g.OnShipmentCorrected));
							ts.Add(t => t.To<State.cancelled>().IsTriggeredOn(g => g.cancelOrder));
						});
						transitions.AddGroupFrom<State.completed>(ts =>
						{
							ts.Add(t => t.To<State.shipping>().IsTriggeredOn(g => g.OnShipmentCorrected));
							ts.Add(t => t
								.To<State.backOrder>()
								.IsTriggeredOn(g => g.reopenOrder)
								.WithFieldAssignments(fas => fas.Add<backOrdered>(false)));
						});
						transitions.AddGroupFrom<State.cancelled>(ts =>
						{
							ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.reopenOrder));
						});
					})
				);
			}));
		}
	}
}