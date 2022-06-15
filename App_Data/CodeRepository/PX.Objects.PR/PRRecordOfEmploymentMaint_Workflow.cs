using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.PM;

namespace PX.Objects.PR
{
	using static BoundedTo<PRRecordOfEmploymentMaint, PRRecordOfEmployment>;

	public partial class PRRecordOfEmploymentMaint_Workflow : PXGraphExtension<PRRecordOfEmploymentMaint>
	{

		public override void Configure(PXScreenConfiguration config)
		{
			Configure(config.GetScreenConfigurationContext<PRRecordOfEmploymentMaint, PRRecordOfEmployment>());
		}

		protected virtual void Configure(WorkflowContext<PRRecordOfEmploymentMaint, PRRecordOfEmployment> context)
		{
			var processingCategory = context.Categories.CreateNew(ToolbarCategory.ActionCategoryNames.Processing,
				category => category.DisplayName(ToolbarCategory.ActionCategory.Processing));
			var correctionsCategory = context.Categories.CreateNew(ToolbarCategory.ActionCategoryNames.Corrections,
				category => category.DisplayName(ToolbarCategory.ActionCategory.Corrections));
			var otherCategory = context.Categories.CreateNew(ToolbarCategory.ActionCategoryNames.Other,
				category => category.DisplayName(ToolbarCategory.ActionCategory.Other));

			void DisableHeaderFields(FieldState.IContainerFillerFields fields)
			{
				fields.AddField<PRRecordOfEmployment.employeeID>(field => field.IsDisabled());
				fields.AddField<PRRecordOfEmployment.amendment>(field => field.IsDisabled());
				fields.AddField<PRRecordOfEmployment.reasonForROE>(field => field.IsDisabled());
				fields.AddField<PRRecordOfEmployment.periodType>(field => field.IsDisabled());
				fields.AddField<PRRecordOfEmployment.comments>(field => field.IsDisabled());
				fields.AddField<PRRecordOfEmployment.docDesc>(field => field.IsDisabled());
			}

			void DisableTabFields(FieldState.IContainerFillerFields fields)
			{
				fields.AddField<PRRecordOfEmployment.craPayrollAccountNumber>(field => field.IsDisabled());
				fields.AddField<PRRecordOfEmployment.firstDayWorked>(field => field.IsDisabled());
				fields.AddField<PRRecordOfEmployment.lastDayForWhichPaid>(field => field.IsDisabled());
				fields.AddField<PRRecordOfEmployment.finalPayPeriodEndingDate>(field => field.IsDisabled());
				fields.AddField<PRRecordOfEmployment.vacationPay>(field => field.IsDisabled());
				fields.AddField<PRRecordOfEmployment.totalInsurableHours>(field => field.IsDisabled());
				fields.AddField<PRRecordOfEmployment.totalInsurableEarnings>(field => field.IsDisabled());

				fields.AddTable<PRROEStatutoryHolidayPay>(state => state.IsDisabled());
				fields.AddTable<PRROEOtherMonies>(state => state.IsDisabled());
				fields.AddTable<PRROEInsurableEarningsByPayPeriod>(state => state.IsDisabled());
			}

			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<PRRecordOfEmployment.status>()
					.AddDefaultFlow(flow => flow
						.WithFlowStates(fss =>
						{
							fss.Add<ROEStatus.open>(flowState =>
							{
								return flowState
									.IsInitial()
									.WithActions(actions =>
									{
										actions.Add(g => g.Export, c => c.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									});
							});
							fss.Add<ROEStatus.exported>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.MarkAsSubmitted, c => c.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
										actions.Add(g => g.Reopen);
									})
									.WithFieldStates(DisableHeaderFields);
							});
							fss.Add<ROEStatus.submitted>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.Amend);
									})
									.WithFieldStates(fields =>
									{
										DisableHeaderFields(fields);
										DisableTabFields(fields);
									});
							});
							fss.Add<ROEStatus.needsAmendment>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.Amend, c => c.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									})
									.WithFieldStates(fields =>
									{
										DisableHeaderFields(fields);
										DisableTabFields(fields);
									});
							});
							fss.Add<ROEStatus.amended>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
									})
									.WithFieldStates(fields =>
									{
										fields.AddTable<PRRecordOfEmployment>(state => state.IsDisabled());
										fields.AddTable<PRROEStatutoryHolidayPay>(state => state.IsDisabled());
										fields.AddTable<PRROEOtherMonies>(state => state.IsDisabled());
										fields.AddTable<PRROEInsurableEarningsByPayPeriod>(state => state.IsDisabled());
									});
							});
						})
						.WithTransitions(transitions =>
						{
							transitions.AddGroupFrom<ROEStatus.open>(ts =>
							{
								ts.Add(t => t
									.To<ROEStatus.exported>()
									.IsTriggeredOn(g => g.Export));
							});
							transitions.AddGroupFrom<ROEStatus.exported>(ts =>
							{
								ts.Add(t => t
									.To<ROEStatus.submitted>()
									.IsTriggeredOn(g => g.MarkAsSubmitted));
								ts.Add(t => t
									.To<ROEStatus.open>()
									.IsTriggeredOn(g => g.Reopen));
							});
							transitions.AddGroupFrom<ROEStatus.submitted>(ts =>
							{
								ts.Add(t => t
									.To<ROEStatus.amended>()
									.IsTriggeredOn(g => g.Amend));
							});
							transitions.AddGroupFrom<ROEStatus.needsAmendment>(ts =>
							{
								ts.Add(t => t
									.To<ROEStatus.amended>()
									.IsTriggeredOn(g => g.Amend));
							});
							transitions.AddGroupFrom<ROEStatus.amended>(ts =>
							{
							});
						}))
						.WithActions(actions =>
						{
							actions.Add(g => g.Export, c => c.WithCategory(processingCategory));
							actions.Add(g => g.MarkAsSubmitted, c => c.WithCategory(processingCategory));
							actions.Add(g => g.Amend, c => c.WithCategory(correctionsCategory));
							actions.Add(g => g.Reopen, c => c.WithCategory(correctionsCategory));
							actions.Add(g => g.ShowFinalPaycheck, c => c.WithCategory(otherCategory));
						})
					.WithCategories(categories =>
					{
						categories.Add(processingCategory);
						categories.Add(correctionsCategory);
						categories.Add(otherCategory);
					}));
		}
	}
}
