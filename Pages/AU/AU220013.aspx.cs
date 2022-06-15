using System;
using PX.Web.UI;

public partial class Page_AU220013 : PX.Web.UI.PXPage
{
	protected void Page_Init(object sender, EventArgs e)
	{
	}
	protected void Page_Load(object sender, EventArgs e)
	{
		if (!this.Page.IsCallback)
		{
			PXGrid grid = this.tab.FindControl("grdWidgets") as PX.Web.UI.PXGrid;
			if (grid != null)
				this.Page.ClientScript.RegisterClientScriptBlock(GetType(), "gridWidgetsID", "var grdWidgetsID=\"" + grid.ClientID + "\";", true);

			grid = this.tab.FindControl("grdItems") as PX.Web.UI.PXGrid;
			if (grid != null)
				this.Page.ClientScript.RegisterClientScriptBlock(GetType(), "gridItemsID", "var grdItemsID=\"" + grid.ClientID + "\";", true);
		}
	}
}