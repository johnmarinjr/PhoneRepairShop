using PX.Objects.PJ.ProjectManagement.Descriptor;
using PX.Objects.PJ.Submittals.PJ.Descriptor;
using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.PJ.Submittals.PJ.DAC;
using PX.Objects.PJ.Submittals.PJ.Graphs;
using PX.Objects.CS;
using System;
using static PX.Data.WorkflowAPI.BoundedTo<PX.Objects.PJ.Submittals.PJ.Graphs.SubmittalEntry, PX.Objects.PJ.Submittals.PJ.DAC.PJSubmittal>;
using ReasonDefinition = PX.Objects.PJ.Submittals.PJ.DAC.PJSubmittal.reason;

namespace PX.Objects.PJ.Submittals.PJ.GraphExtensions
{
	public class SubmittalEntryWorkflowExtension : PXGraphExtension<SubmittalEntry>
	{
		public const string FormOpenID = "FormOpen";
		public const string FormCloseID = "FormClose";

		private const string DisableConditionID = "RevisionCondition";

		private static readonly string[] NewReasons =
		{
			ReasonDefinition.New,
			ReasonDefinition.Revision
		};

		private static readonly string[] OpenReasons =
		{
			ReasonDefinition.Issued,
			ReasonDefinition.Submitted,
			ReasonDefinition.PendingApproval
		};

		private static readonly string[] CloseReasons =
		{
			ReasonDefinition.Approved,
			ReasonDefinition.ApprovedAsNoted,
			ReasonDefinition.Rejected,
			ReasonDefinition.Canceled,
			ReasonDefinition.ReviseAndResubmit
		};

		private const string ReasonFieldName = nameof(PJSubmittal.Reason);
		private const string ClosedDateFieldName = nameof(PJSubmittal.DateClosed);

		public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.constructionProjectManagement>();

		public override void Configure(PXScreenConfiguration configuration)
		{
			base.Configure(configuration);

			var context = configuration.GetScreenConfigurationContext<SubmittalEntry, PJSubmittal>();

			var processingCategory = context.Categories.CreateNew(PX.Objects.PM.ToolbarCategory.ActionCategoryNames.Processing,
				category => category.DisplayName(PX.Objects.PM.ToolbarCategory.ActionCategory.Processing));
			var printingAndEmailingCategory = context.Categories.CreateNew(PX.Objects.PM.ToolbarCategory.ActionCategoryNames.PrintingAndEmailing,
				category => category.DisplayName(PX.Objects.PM.ToolbarCategory.ActionCategory.PrintingAndEmailing));

			var revisionCondition = context
				.Conditions
				.FromLambda((PJSubmittal s) => s?.Status == PJSubmittal.status.Closed && s?.IsLastRevision != true)
				.WithSharedName(DisableConditionID);

			var openForm = CreateReasonsForm(FormOpenID, OpenReasons, ReasonDefinition.Issued);
			var closeForm = CreateReasonsForm(FormCloseID, CloseReasons, ReasonDefinition.Approved, fields =>
			{
				fields.Add(ClosedDateFieldName, field =>
				{
					return field
						.WithSchemaOf<PJSubmittal.dateClosed>()
						.IsRequired()
						.Prompt(SubmittalMessage.DateClosed)
						.DefaultValue(RelativeDatesManager.TODAY);
				});
			});

			var openAction = CreateAction(openForm, SubmittalMessage.OpenAction, processingCategory, revisionCondition);
			var closeAction = CreateAction(closeForm, SubmittalMessage.CloseAction, processingCategory);
			var deleteAction = context.ActionDefinitions.CreateExisting(sub => sub.Delete,
				act => act.PlaceAfter(sub => sub.Insert));
			var createRevisionAction = context.ActionDefinitions.CreateExisting(sub => sub.CreateRevision, 
				act => act.IsDisabledWhen(revisionCondition).PlaceAfter(openAction).InFolder(processingCategory));

			context.AddScreenConfigurationFor(screen =>
			{
				return screen
				.StateIdentifierIs<PJSubmittal.status>()
				.AddDefaultFlow(flow =>
				{
					return flow
					.WithFlowStates(states =>
					{
						states.Add<PJSubmittal.status.newStatus>(flowState =>
						{
							return flowState
								.IsInitial()
								.WithFieldStates(fields =>
								{
									fields.AddField<ReasonDefinition>(field =>
									{
										return field
											.DefaultValue(ReasonDefinition.New)
											.ComboBoxValues(NewReasons);
									});
								})
								.WithActions(actions =>
								{
									actions.Add(openAction, c => c.IsDuplicatedInToolbar());
									actions.Add(deleteAction);
									actions.Add(g => g.PrintSubmittal, c => c.IsDuplicatedInToolbar());
									actions.Add(g => g.SendEmail, c => c.IsDuplicatedInToolbar());
								});
						});
						states.Add<PJSubmittal.status.open>(flowState =>
						{
							return flowState
								.WithFieldStates(fields =>
								{
									fields.AddField<ReasonDefinition>(field =>
									{
										return field
											.DefaultValue(ReasonDefinition.Issued)
											.ComboBoxValues(OpenReasons);
									});
								})
								.WithActions(actions =>
								{
									actions.Add(closeAction, c => c.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
									actions.Add(g => g.PrintSubmittal, c => c.IsDuplicatedInToolbar());
									actions.Add(g => g.SendEmail, c => c.IsDuplicatedInToolbar());
								});
						});
						states.Add<PJSubmittal.status.closed>(flowState =>
						{
							return flowState
								.WithFieldStates(fields =>
								{
									fields.AddField<ReasonDefinition>(field =>
									{
										return field
											.DefaultValue(ReasonDefinition.Approved)
											.ComboBoxValues(CloseReasons);
									});
								})
								.WithActions(actions =>
								{
									actions.Add(openAction, c => c.IsDuplicatedInToolbar());
									actions.Add(createRevisionAction, c => c.IsDuplicatedInToolbar());
									actions.Add(g => g.PrintSubmittal, c => c.IsDuplicatedInToolbar());
									actions.Add(g => g.SendEmail, c => c.IsDuplicatedInToolbar());
								});
						});
					})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom<PJSubmittal.status.newStatus>(ts =>
						{
							ts.Add(t => t
								.To<PJSubmittal.status.open>()
								.IsTriggeredOn(openAction));
						});
						transitions.AddGroupFrom<PJSubmittal.status.open>(ts =>
						{
							ts.Add(t => t
								.To<PJSubmittal.status.closed>()
								.IsTriggeredOn(closeAction)
								.WithFieldAssignments(fields => fields.Add<PJSubmittal.dateClosed>(f => f.SetFromFormField(closeForm, ClosedDateFieldName))));
						});
						transitions.AddGroupFrom<PJSubmittal.status.closed>(ts =>
						{
							ts.Add(t => t
								.To<PJSubmittal.status.open>()
								.IsTriggeredOn(openAction)
								.WithFieldAssignments(fields => fields.Add<PJSubmittal.dateClosed>(f => f.SetFromValue(null))));
						});
					});
				})
				.WithActions(actions =>
				{
					actions.Add(openAction);
					actions.Add(closeAction);
					actions.Add(deleteAction);
					actions.Add(createRevisionAction);
					actions.Add(g => g.PrintSubmittal, c => c
							.InFolder(printingAndEmailingCategory));
					actions.Add(g => g.SendEmail, c => c
							.InFolder(printingAndEmailingCategory));
				})
				.WithForms(forms =>
				{
					forms.Add(openForm);
					forms.Add(closeForm);
				})
				.WithFieldStates(fields =>
				{
					fields.Add<ReasonDefinition>(field =>
					{
						return field
						.SetComboValues(
							(ReasonDefinition.Approved, SubmittalReason.Approved),
							(ReasonDefinition.ApprovedAsNoted, SubmittalReason.ApprovedAsNoted),
							(ReasonDefinition.Canceled, SubmittalReason.Canceled),
							(ReasonDefinition.Issued, SubmittalReason.Issued),
							(ReasonDefinition.New, SubmittalReason.New),
							(ReasonDefinition.PendingApproval, SubmittalReason.PendingApproval),
							(ReasonDefinition.Rejected, SubmittalReason.Rejected),
							(ReasonDefinition.ReviseAndResubmit, SubmittalReason.ReviseAndResubmit),
							(ReasonDefinition.Revision, SubmittalReason.Revision),
							(ReasonDefinition.Submitted, SubmittalReason.Submitted));
					});
				})
				.WithCategories(categories =>
				{
					categories.Add(processingCategory);
					categories.Add(printingAndEmailingCategory);
				});
		});

			#region Setup Helpers
			Form.IConfigured CreateReasonsForm(string formID, string[] valueCollection, string defaultValue, Action<FormField.IContainerFillerFields> fieldsExt = null)
			{
				return context.Forms.Create(formID, form =>
				{
					return form
					.Prompt(SubmittalMessage.WorkflowPromptFormTitle)
					.WithFields(fields =>
					{
						fields.Add(ReasonFieldName, field =>
						{
							return field
								.WithSchemaOf<ReasonDefinition>()
								.IsRequired()
								.Prompt(ReasonFieldName)
								.DefaultValue(defaultValue)
								.OnlyComboBoxValues(valueCollection);
						});

						fieldsExt?.Invoke(fields);
					});
				});
			}

			ActionDefinition.IConfigured CreateAction(Form.IConfigured form, string name, ActionCategory.IConfigured folder, Condition disableCondition = null)
			{
				return context.ActionDefinitions.CreateNew(name, action =>
				{
					var retAction = action
						.InFolder(FolderType.ActionsFolder)
						.WithFieldAssignments(fields =>
						{
							fields.Add<ReasonDefinition>(field => field.SetFromFormField(form, ReasonFieldName));
						})
						.DisplayName(name)
						.InFolder(folder)
						.WithForm(form)
						.IsExposedToMobile(true)
						.PlaceAfter(sub => sub.SendEmail);

					return disableCondition != null ? retAction.IsDisabledWhen(disableCondition) : retAction;
				});
			} 
			#endregion
		}
	}
}
