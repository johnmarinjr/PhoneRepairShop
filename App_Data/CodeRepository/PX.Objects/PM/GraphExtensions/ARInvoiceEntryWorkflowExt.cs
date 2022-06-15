using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.AR;
using PX.Objects.CS;

namespace PX.Objects.PM
{
	public class ARInvoiceEntryWorkflowExt : PXGraphExtension<ARInvoiceEntry_Workflow, ARInvoiceEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.projectAccounting>();
		}

		public override void Configure(PXScreenConfiguration config)
		{
			var context = config.GetScreenConfigurationContext<ARInvoiceEntry, ARInvoice>();
			BoundedTo<ARInvoiceEntry, ARInvoice>.Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();

			var conditions = new
			{
				CreateScheduleHidden
					= Bql<ARInvoice.proformaExists.IsEqual<True>>(),
			}.AutoNameConditions();

			config.GetScreenConfigurationContext<ARInvoiceEntry, ARInvoice>().UpdateScreenConfigurationFor(screen =>
			{
				return screen
					.WithActions(actions =>
					{
						actions.Update(g => g.createSchedule, c => c.IsHiddenWhenElse(conditions.CreateScheduleHidden));
					});
			});
		}
	}
}
