using PX.Data;
using PX.Objects.SO.Workflow.SalesOrder;

namespace PX.Objects.SO
{
	// just for backward compitibility
	public class SOOrderEntry_Workflow : PXGraphExtension<
		WorkflowBL,
		WorkflowCM,
		WorkflowIN,
		WorkflowRM,
		WorkflowQT,
		WorkflowSO,
		WorkflowTR,
		ScreenConfiguration,
		SOOrderEntry
	> { }
}
