using System;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

public partial class Page_CA204550 : PX.Web.UI.PXPage
{
	protected void Page_Init(object sender, EventArgs e)
	{
		this.Master.FindControl("usrCaption").Visible = false;
		this.Master.PopupHeight = 495;
		this.Master.PopupWidth = 550;
	}
}
