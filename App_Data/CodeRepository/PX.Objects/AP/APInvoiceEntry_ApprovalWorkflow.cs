using PX.Data;
using PX.Data.WorkflowAPI;
using System.Collections;
using PX.Objects.EP;

namespace PX.Objects.AP
{
	using State = APDocStatus;
	using static APInvoice;
	using static BoundedTo<APInvoiceEntry, APInvoice>;

	public class APApprovalSettings : EPApprovalSettings<APSetupApproval>
	{
	}

	public class APInvoiceEntry_ApprovalWorkflow : PXGraphExtension<APInvoiceEntry_Workflow, APInvoiceEntry>
	{
		
		[PXWorkflowDependsOnType(typeof(APSetupApproval))]
		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<APInvoiceEntry, APInvoice>());
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
	

		protected virtual void Configure(WorkflowContext<APInvoiceEntry, APInvoice> context)
		{
			var approvalCategory = context.Categories.Get(APInvoiceEntry_Workflow.ActionCategory.Approval);
			var conditions = context.Conditions.GetPack<Conditions>();		
			
			var approveAction = context.ActionDefinitions
				.CreateExisting<APInvoiceEntry_ApprovalWorkflow>(g => g.approve, a => a
					.WithCategory(approvalCategory)
					.PlaceAfter(g => g.releaseFromHold)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<APRegister.approved>(e => e.SetFromValue(true))));

			var rejectAction = context.ActionDefinitions
				.CreateExisting<APInvoiceEntry_ApprovalWorkflow>(g => g.reject, a => a
					.WithCategory(approvalCategory, approveAction)
					.PlaceAfter(approveAction)
					.IsHiddenWhen(conditions.IsApprovalDisabled)
					.WithFieldAssignments(fa => fa.Add<APRegister.rejected>(e => e.SetFromValue(true))));

			Workflow.ConfiguratorFlow InjectApprovalWorkflow(Workflow.ConfiguratorFlow flow)
			{
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
								fas.Add<APRegister.approved>(f => f.SetFromValue(false));
								fas.Add<APRegister.rejected>(f => f.SetFromValue(false));
							}));
					});
			});
		}

		public PXAction<APInvoice> approve;

		[PXButton(CommitChanges = true),
		 PXUIField(DisplayName = "Approve", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Approve(PXAdapter adapter) => adapter.Get();

		public PXAction<APInvoice> reject;

		[PXButton(CommitChanges = true),
		 PXUIField(DisplayName = "Reject", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable Reject(PXAdapter adapter) => adapter.Get();
	}
}