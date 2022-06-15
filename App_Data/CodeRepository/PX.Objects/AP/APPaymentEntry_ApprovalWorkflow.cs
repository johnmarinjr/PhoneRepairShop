using PX.Data;
using PX.Data.WorkflowAPI;
using System.Collections;
using PX.Objects.AP.Standalone;

namespace PX.Objects.AP
{
	using State = APDocStatus;
	using static APPayment;
	using static BoundedTo<APPaymentEntry, APPayment>;

	public class APPaymentEntry_ApprovalWorkflow : PXGraphExtension<APPaymentEntry_Workflow, APPaymentEntry>
	{
		[PXWorkflowDependsOnType(typeof(APSetupApproval))]
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<APPaymentEntry, APPayment>());
		}
		public class Conditions : Condition.Pack
		{
			public Condition IsApproved => GetOrCreate(b => b.FromBql<
				APRegister.approved.IsEqual<True>
			>());

			public Condition IsRejected => GetOrCreate(b => b.FromBql<
				APRegister.rejected.IsEqual<True>
			>());

			public Condition IsApprovalDisabled => GetOrCreate(b => b.FromBqlType(
				APApprovalSettings
				.IsApprovalDisabled<docType, APDocType,
					Where<status.IsNotIn<APDocStatus.pendingApproval, APDocStatus.rejected>>>()));
		}
		protected virtual void Configure(WorkflowContext<APPaymentEntry, APPayment> context)
		{
			var approvalCategory = context.Categories.Get(APPaymentEntry_Workflow.ActionCategory.Approval);
			var conditions = context.Conditions.GetPack<Conditions>();

			var approveAction = context.ActionDefinitions
				.CreateExisting<APPaymentEntry_ApprovalWorkflow>(g => g.approve, a => a
					.WithCategory(approvalCategory)
					.PlaceAfter(g => g.releaseFromHold)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<APRegister.approved>(e => e.SetFromValue(true))));

			var rejectAction = context.ActionDefinitions
				.CreateExisting<APPaymentEntry_ApprovalWorkflow>(g => g.reject, a => a
					.WithCategory(approvalCategory, approveAction)
					.PlaceAfter(approveAction)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<APRegister.rejected>(e => e.SetFromValue(true))));

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
									actions.Add(g => g.printAPEdit);
									actions.Add(g => g.vendorDocuments);
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
								fas.Add<APRegister.approved>(f => f.SetFromValue(false));
								fas.Add<APRegister.rejected>(f => f.SetFromValue(false));
							}));
					});
			});
		}

		public PXAction<APPayment> approve;

		[PXButton(CommitChanges = true),
		 PXUIField(DisplayName = "Approve", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<APPayment> reject;

		[PXButton(CommitChanges = true),
		 PXUIField(DisplayName = "Reject", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
	}
}