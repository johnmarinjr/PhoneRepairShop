using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using PX.Web.UI;
using PX.Data;
using PX.SM;
using System.Web;
using PX.Common;
using System.Linq;

[Customization.CstDesignMode(Disabled = true)]
public partial class Page_SM204505 : PX.Web.UI.PXPage
{
	protected void Page_Init(object sender, EventArgs e)
	{
		Uri uri = HttpContext.Current.Request.UrlReferrer;
		if ((uri == null) ||
			ContainsNoCase(uri.AbsolutePath, "projectbrowser.aspx"))
		{
			this.Master.FindControl("usrCaption").Visible = false;
		}

        ds.LongRunStatus += Ds_LongRunStatus;
	}

	private void Ds_LongRunStatus(object sender, PXDataSourceLongRunStatusEventArgs e)
	{
		if (ds.DataGraph.Actions["AutoValidateCertStatus"]?.GetEnabled() == true)
		{
			if (!PX.Translation.ResourceCollectingManager.IsStringCollecting
				&& ControlHelper.IsReloadPage(this))
			{
				if (PXLongOperation.GetCustomInfo(ds.DataGraph.UID) is string)
				{
					PXLongOperation.ClearStatus(ds.DataGraph.UID);
					e.LongRunStatus = PXLongRunStatus.NotExists;
				}
				if (PXSelect<CustProject,
					Where<CustProject.projID, IsNotNull,
					And<Where<CustProject.certificationStatusDateTime, IsNull,
					Or<CustProject.certificationStatusDateTime, Less<Required<CustProject.certificationStatusDateTime>>>>>>>
					.Select(ds.DataGraph, PXTimeZoneInfo.UtcNow.AddDays(-1)).Any())
				{
					e.LongRunMessage = "RUN-PACKAGES-VALIDATION";
				}
			}
			else
			{
				System.Threading.Thread.Sleep(200);
				string longRunMessage = PXLongOperation.GetCustomInfo(ds.DataGraph.UID) as string;
				if (longRunMessage != null && PXLongOperation.GetStatus(ds.DataGraph.UID) == PXLongRunStatus.InProcess)
				{
					e.LongRunMessage = longRunMessage;
				}
			}
		}
	}

    private bool ContainsNoCase(string s1, string s2)
	{
		if (String.IsNullOrEmpty(s1) ||
			String.IsNullOrEmpty(s2))
		{
			return false;
		}
		else
		{
			return s1.IndexOf(s2, StringComparison.OrdinalIgnoreCase) > -1;
		}
	}
	//protected void Page_Init(object sender, EventArgs e)
	//{
	//    this.pnlImport.FileUploadFinished += new PXFileUploadEventHandler(pnlImport_FileUploadFinished);
	//}

	//protected void Page_Load(object sender, EventArgs e)
	//{
	//    this.Page.ClientScript.RegisterClientScriptBlock(GetType(), "gridID", "var gridID=\"" + grid.ClientID + "\";", true);
	//    this.Page.ClientScript.RegisterClientScriptBlock(GetType(), "pnlImportID", "var pnlImportID=\"" + this.pnlImport.ClientID + "\";", true);
	//    this.Page.ClientScript.RegisterClientScriptBlock(GetType(), "dsID", "var dsID=\"" + this.ds.ClientID + "\";", true);
	//}

	//void pnlImport_FileUploadFinished(object sender, PXFileUploadEventArgs e)
	//{
	//    ProjectList graph = (ProjectList)this.ds.DataGraph;
	//    try
	//    {
	//        bool isPackage = e.UploadedFile.FullName.EndsWith(".zip");
	//        graph.ImportXML(e.UploadedFile.BinData, isPackage);
	//    }
	//    catch (PXException ex)
	//    {
	//        this.ClientScript.RegisterClientScriptBlock(this.GetType(), "uploadErr", "window.uploadErr = \"Error during file upload: " + ex.MessageNoPrefix.Replace('"', '\'') + "\";", true);
	//    }
	//}


	public void uploadPanel_Upload(PX.Web.UI.UserControls.PXUploadFilePanel.PXFileUploadedEventArgs args)
	{
		ProjectMaintenance.OnUploadPackage(args.FileName, args.BinData);

	}
}
