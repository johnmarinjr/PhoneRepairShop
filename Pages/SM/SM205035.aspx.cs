using System;
using PX.Api;
using PX.Web.UI;

public partial class Page_SM205035 : PXPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
    }

	protected void Histories_ColumnsGenerated(object sender, EventArgs e)
	{
		foreach (PXGridColumn column in ((PXGrid)sender).Columns)
        {
			column.AllowFilter = column.AllowSort =
				column.DataField.OrdinalEquals("ExecutionStatus") || column.DataField.OrdinalEquals("ExecutionResult");
        }
	}
}
