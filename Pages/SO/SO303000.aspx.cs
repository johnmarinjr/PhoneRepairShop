using System;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using PX.Web.UI;
using PX.Objects.IN.RelatedItems;
using PX.Data;
using PX.Objects.SO.GraphExtensions.SOInvoiceEntryExt;

public partial class Page_SO303000 : PX.Web.UI.PXPage
{
	protected void Page_Init(object sender, EventArgs e)
	{
	}

	protected void grid_RowDataBound(object sender, PXGridRowEventArgs e)
	{
		e.Row.Cells["RelatedItems"].Style.CssClass = "RelatedItemsCell";
	}
}
