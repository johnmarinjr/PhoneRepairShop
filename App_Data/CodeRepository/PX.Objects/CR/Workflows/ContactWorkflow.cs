using PX.Data;
using PX.Data.WorkflowAPI;
using static PX.Objects.CR.ContactMaint;

namespace PX.Objects.CR.Workflows
{
	public class ContactWorkflow : PXGraphExtension<ContactMaint>
	{
		public static bool IsActive() => false;

		#region Consts

		public static class CategoryNames
		{
			public const string RecordCreation = "RecordCreation";
			public const string Activities = "Activities";
			public const string Validation = "Validation";
		}

		#endregion

		public override void Configure(PXScreenConfiguration configuration)
		{
			var config = configuration.GetScreenConfigurationContext<ContactMaint, Contact>();

			#region Conditions
			var conditions = new
			{
				IsContactActive
					= config.Conditions.FromBql<Contact.status.IsNotEqual<ContactStatus.active>>(),

			}.AutoNameConditions();
			#endregion

			#region categories

			var categoryRecordCreation = config.Categories.CreateNew(CategoryNames.RecordCreation,
				category => category.DisplayName("Record Creation"));
			var categoryActivities = config.Categories.CreateNew(CategoryNames.Activities,
				category => category.DisplayName("Activities"));
			var categoryValidation = config.Categories.CreateNew(CategoryNames.Validation,
				category => category.DisplayName("Validation"));

			#endregion

			var actionCreatePhoneCall = config.ActionDefinitions.CreateExisting(
				CRActivityListBase<CRPMTimeActivity>._NEWACTIVITY_PHONECALL_WORKFLOW_COMMAND,
				a => a.WithCategory(categoryActivities));
			var actionCreateNote = config.ActionDefinitions.CreateExisting(
				CRActivityListBase<CRPMTimeActivity>._NEWACTIVITY_NOTE_WORKFLOW_COMMAND,
				a => a.WithCategory(categoryActivities));
			var actionCreateMail = config.ActionDefinitions.CreateExisting(
				CRActivityListBase<CRPMTimeActivity>._NEWMAILACTIVITY_WORKFLOW_COMMAND,
				a => a.WithCategory(categoryActivities));
			var actionCreateTask = config.ActionDefinitions.CreateExisting(
				CRActivityListBase<CRPMTimeActivity>._NEWTASK_WORKFLOW_COMMAND,
				a => a.WithCategory(categoryActivities));

			config.AddScreenConfigurationFor(screen =>
			{
				return screen
					.WithActions(actions =>
					{
						actions.Add(g => g.addOpportunity, c => c.WithCategory(categoryRecordCreation));
						actions.Add<CreateAccountFromContactGraphExt>(g =>
										g.CreateBAccount, c => c
											.WithCategory(categoryRecordCreation));
						actions.Add(g => g.addCase, c => c.WithCategory(categoryRecordCreation));
						actions.Add<CreateLeadFromContactGraphExt>(g =>
										g.CreateLead, c => c
											.WithCategory(categoryRecordCreation)
											.IsDisabledWhen(conditions.IsContactActive));

						actions.Add(actionCreatePhoneCall);
						actions.Add(actionCreateNote);
						actions.Add(actionCreateMail);
						actions.Add(actionCreateTask);

					})
					.WithCategories(categories =>
					{
						categories.Add(categoryRecordCreation);
						categories.Add(categoryValidation);
						categories.Add(categoryActivities);
					});
			});

		}
	}
}