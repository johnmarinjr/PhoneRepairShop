using PX.Data;
using PX.Data.WorkflowAPI;
using System.Collections;
using PX.Objects.AP;
using PX.Objects.EP;

namespace PX.Objects.AR
{
	using State = ARDocStatus;
	using static ARInvoice;
	using static BoundedTo<ARInvoiceEntry, ARInvoice>;

	public class ARApprovalSettings : EPApprovalSettings<ARSetupApproval>
	{
	}

	public class ARInvoiceEntry_ApprovalWorkflow : PXGraphExtension<ARInvoiceEntry_Workflow, ARInvoiceEntry>
	{
		[PXWorkflowDependsOnType(typeof(ARSetupApproval))]
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<ARInvoiceEntry, ARInvoice>());
		}
		public class Conditions : Condition.Pack
		{
			public Condition IsApproved => GetOrCreate(b => b.FromBql<
				ARRegister.approved.IsEqual<True>
			>());

			public Condition IsRejected => GetOrCreate(b => b.FromBql<
				ARRegister.rejected.IsEqual<True>
			>());

			public Condition IsApprovalDisabled => GetOrCreate(b => b.FromBqlType(
				ARApprovalSettings
					.IsApprovalDisabled<docType, ARDocType,
						Where<status.IsNotIn<ARDocStatus.pendingApproval, ARDocStatus.rejected>>>()));
			public Condition NonEditable => GetOrCreate(b => b.FromBqlType(
				BqlCommand.Compose(
					typeof(Where<,,>),
					typeof(status), typeof(NotEqual<ARDocStatus.hold>), typeof(And<>), typeof(Not<>),
					ARApprovalSettings.IsApprovalDisabled<docType, ARDocType>()
					)));
		}

		protected virtual void Configure(WorkflowContext<ARInvoiceEntry, ARInvoice> context)
		{
			var approvalCategory = context.Categories.Get(ARInvoiceEntry_Workflow.CategoryID.Approval);
			var conditions = context.Conditions.GetPack<Conditions>();		
			

			var approveAction = context.ActionDefinitions
				.CreateExisting<ARInvoiceEntry_ApprovalWorkflow>(g => g.approve, a => a
					.WithCategory(approvalCategory)
					.PlaceAfter(g => g.releaseFromHold)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<ARRegister.approved>(e => e.SetFromValue(true))));

			var rejectAction = context.ActionDefinitions
				.CreateExisting<ARInvoiceEntry_ApprovalWorkflow>(g => g.reject, a => a
					.WithCategory(approvalCategory, approveAction)
					.PlaceAfter(approveAction)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<ARRegister.rejected>(e => e.SetFromValue(true))));

			Workflow.ConfiguratorFlow InjectApprovalWorkflow(Workflow.ConfiguratorFlow flow)
			{
				const string initialState = "_";

				return flow
					.WithFlowStates(states =>
					{
						states.UpdateSequence<State.HoldToBalance>(seq =>
							seq.WithStates(sss =>
							{
								sss.Add<State.pendingApproval>(flowState =>
								{
									return flowState
										.IsSkippedWhen(conditions.IsApproved)
										.WithActions(actions =>
										{
											actions.Add(approveAction, a => a.IsDuplicatedInToolbar());
											actions.Add(rejectAction, a => a.IsDuplicatedInToolbar());
											actions.Add(g => g.putOnHold);
										})
										.PlaceAfter<State.hold>();
								});
							}));
						states.Add<State.rejected>(flowState =>
						{
							return flowState
								.WithActions(actions =>
								{
									actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());
									actions.Add(g => g.printAREdit);
									actions.Add(g => g.customerDocuments);
								});
						});
					})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom<State.pendingApproval>(ts =>
						{
							ts.Add(t => t
								.To<State.HoldToBalance>()
								.IsTriggeredOn(g => g.OnUpdateStatus));
							ts.Add(t => t
								.ToNext()
								.IsTriggeredOn(approveAction)
								.When(conditions.IsApproved));
							ts.Add(t => t
								.To<State.rejected>()
								.IsTriggeredOn(rejectAction)
								.When(conditions.IsRejected));
							ts.Add(t => t.To<State.scheduled>()
								.IsTriggeredOn(g => g.OnConfirmSchedule)
								.WithFieldAssignments(fas =>
								{
									fas.Add<scheduled>(e => e.SetFromValue(true));
									fas.Add<scheduleID>(e => e.SetFromExpression("@ScheduleID"));
								}));
						});
						transitions.AddGroupFrom<State.rejected>(ts =>
						{
							ts.Add(t => t
								.To<State.hold>()
								.IsTriggeredOn(g => g.putOnHold)
								.DoesNotPersist()
							);
						});
					});
			}

			context.UpdateScreenConfigurationFor(screen =>
			{
				return screen
					.UpdateDefaultFlow(InjectApprovalWorkflow)
					.WithActions(actions =>
					{
						actions.Add(approveAction);
						actions.Add(rejectAction);
						actions.Update(
							g => g.putOnHold,
							a => a.WithFieldAssignments(fas =>
							{
								fas.Add<ARRegister.approved>(f => f.SetFromValue(false));
								fas.Add<ARRegister.rejected>(f => f.SetFromValue(false));
							}));
						actions.Update(
							g => g.releaseFromCreditHold,
							a => (BoundedTo<ARInvoiceEntry, ARInvoice>.ActionDefinition.ConfiguratorAction)a.InFolder(
								approvalCategory, rejectAction));

						actions.Update(
							g => g.recalculateDiscountsAction,
							a => a.IsDisabledWhenElse(conditions.NonEditable));
					});
			});
		}

		public PXAction<ARInvoice> approve;

		[PXButton(CommitChanges = true),
		 PXUIField(DisplayName = "Approve", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<ARInvoice> reject;

		[PXButton(CommitChanges = true),
		 PXUIField(DisplayName = "Reject", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
	}
}