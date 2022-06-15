using System;
using PX.Web.UI;

public partial class Page_AU220012 : PX.Web.UI.PXPage
{
	protected void Page_Init(object sender, EventArgs e)
	{
	}
	protected void Page_Load(object sender, EventArgs e)
	{
		if (!this.Page.IsCallback)
		{
			PXGrid grid = this.grdWorkspaces as PX.Web.UI.PXGrid;
			if (grid != null)
				this.Page.ClientScript.RegisterClientScriptBlock(GetType(), "gridWorkspacesID", "var grdWorkspacesID=\"" + grid.ClientID + "\";", true);
		}
	}
}