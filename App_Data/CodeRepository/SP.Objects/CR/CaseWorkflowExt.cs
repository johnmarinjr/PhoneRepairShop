using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CR;
using PX.Objects.CR.Workflows;

namespace SP.Objects.CR
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class CaseWorkflowExt : PXGraphExtension<CaseWorkflow, CRCaseMaint>
	{
		public override void Configure(PXScreenConfiguration configuration)
		{
			var context = configuration.GetScreenConfigurationContext<CRCaseMaint, CRCase>();

			context.UpdateScreenConfigurationFor(screen =>
			{
				return screen
					.WithActions(actions =>
					{
						HideAction(actions, CaseWorkflow.ActionNames.Open);
						HideAction(actions, nameof(CRCaseMaint.TakeCase));
						HideAction(actions, CaseWorkflow.ActionNames.Close);
						HideAction(actions, CaseWorkflow.ActionNames.PendingCustomer);
						HideAction(actions, nameof(CRCaseMaint.Release));
						HideAction(actions, nameof(CRCaseMaint.ViewInvoice));
						HideAction(actions, nameof(CRCaseMaint.Assign));

						HideAction(actions, CRActivityListBase<CRPMTimeActivity>._NEWMAILACTIVITY_WORKFLOW_COMMAND);
						HideAction(actions, CRActivityListBase<CRPMTimeActivity>._NEWACTIVITY_WORKITEM_WORKFLOW_COMMAND);
						HideAction(actions, CRActivityListBase<CRPMTimeActivity>._NEWACTIVITY_NOTE_WORKFLOW_COMMAND);
						HideAction(actions, CRActivityListBase<CRPMTimeActivity>._NEWTASK_WORKFLOW_COMMAND);
						HideAction(actions, CRActivityListBase<CRPMTimeActivity>._NEWACTIVITY_PHONECALL_WORKFLOW_COMMAND);
					});
			});
		}

		private static void HideAction(BoundedTo<CRCaseMaint, CRCase>.ActionDefinition.ContainerAdjusterActions actions, string actionName)
		{
			actions.Update(actionName, action => action.IsHiddenAlways());
		}
	}
}