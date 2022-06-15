using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CR;
using PX.Objects.CR.Workflows;

namespace PX.Objects.AR.Workflows
{

	public class CustomerLocationMaint_Workflow : PXGraphExtension<CustomerLocationMaint>
	{
		public static bool IsActive() => false;


		public override void Configure(PXScreenConfiguration configuration)
		{
			LocationWorkflow.Configure(configuration);

			WorkflowContext<LocationMaint, Location> context = configuration.GetScreenConfigurationContext<LocationMaint, Location>();
			var otherCategory = context.Categories.Get(LocationWorkflow.ActionCategoryNames.CustomOther);
			var viewAccountLocationAction = context.ActionDefinitions.CreateExisting(g => ((CustomerLocationMaint)g).ViewAccountLocation, a => a.InFolder(otherCategory));
			context.UpdateScreenConfigurationFor(screen => screen.WithActions(a => a.Add(viewAccountLocationAction)));
		}
	}
}
