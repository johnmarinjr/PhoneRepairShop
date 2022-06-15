using System;
using PX.Web.UI;
using PX.CloudServices.DAC;

public partial class Page_AP301110 : PXPage
{
	protected void Page_Init(object sender, EventArgs e)
	{
	}

	protected void grid_RowDataBound(object sender, PXGridRowEventArgs e)
	{
		var item = e.Row.DataItem as RecognizedRecord;
		if (item == null)
		{
			return;
		}

		if (item.Status == RecognizedRecordStatusListAttribute.Processed)
		{
			e.Row.Style.CssClass = "green20";
		}
		else if (item.Status == RecognizedRecordStatusListAttribute.Error)
		{
			e.Row.Style.CssClass = "red20";
		}
	}
}
