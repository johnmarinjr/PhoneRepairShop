using System;
using System.Web.UI;
using PX.Export.Excel.Core;
using PX.SM;
using PX.Web.UI;
using PX.Data;

public partial class Page_SM210000 : PX.Web.UI.PXPage
{
	protected PXSelector selector;

	protected void Page_Init(object sender, EventArgs e)
	{




		pnlNewRev.FileUploadFinished += new PXFileUploadEventHandler(pnlNewRev_FileUploadFinished);
		WikiFileMaintenance graph = (WikiFileMaintenance)ds.DataGraph;
		string authority = Request.GetWebsiteAuthority().GetLeftPart(UriPartial.Authority);
		graph.GetFileAddress = authority + ResolveUrl("~/Frames/GetFile.ashx");
		PXToolBarItem oldButton = ds.ToolBar.Items["edit"];
		if (oldButton != null)
		{
			ds.ToolBar.Items.Remove(oldButton);
		}
		ds.PreRender += ds_PreRender;
	}

	protected void Page_Load(object sender, EventArgs e)
	{
		Control grid = this.tab.FindControl("gridRevisions");
		if (!this.Page.IsCallback)
		{
			this.Page.ClientScript.RegisterClientScriptBlock(GetType(), "gridID", "var gridRevisionsID=\"" + grid.ClientID + "\";", true);
			this.Page.ClientScript.RegisterClientScriptBlock(GetType(), "pnlNewRevID", "var pnlNewRevID=\"" + this.pnlNewRev.ClientID + "\";", true);
			this.Page.ClientScript.RegisterClientScriptBlock(GetType(), "dsID", "var dsID=\"" + this.ds.ClientID + "\";", true);
		}
		PXLabel lbl = this.tab.FindControl("lblAccessRights") as PXLabel;
		if (lbl != null)
			lbl.Text = ActionsMessages.AccessRights;
	}

	protected void pnlNewRev_FileUploadFinished(object sender, PXFileUploadEventArgs e)
	{
		WikiFileMaintenance graph = (WikiFileMaintenance) ds.DataGraph;		
		try
		{
			if(e.UploadedFile.BinData.Length > 0)
				graph.NewRevision(e.UploadedFile, this.pnlNewRev.CheckIn);
		}
		catch (PXException ex)
		{
			this.ClientScript.RegisterClientScriptBlock(this.GetType(), "uploadErr", "window.uploadErr = \"Error during file upload: " + ex.MessageNoPrefix.Replace('"', '\'') + "\";", true);
		}
	}

	private void ds_PreRender(object sender, EventArgs e)
	{
		form.DataBind();
	}

	public override void ProcessRequest(System.Web.HttpContext context)
	{
		string fileId = context.Request.QueryString["fileId"];
		PXBlobStorageUtils.OnBeforeEditFile(fileId);

		base.ProcessRequest(context);		
	}
}
