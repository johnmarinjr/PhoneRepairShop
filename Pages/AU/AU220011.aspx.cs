using System;

public partial class Page_AU220011 : PX.Web.UI.PXPage
{
	protected void Page_Init(object sender, EventArgs e)
	{
		this.Master.FindControl("usrCaption").Visible = false;
	}
}