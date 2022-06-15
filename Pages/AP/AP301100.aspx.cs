using PX.Web.UI;
using System;
using System.Web.UI;
using System.Linq;
using PX.Data;
using PX.Objects.AP;

public partial class Page_AP301100 : PXPage
{
	protected void Page_Init(object sender, EventArgs e)
	{
	}

	protected void Page_Load(object sender, EventArgs e)
	{
		if (IsCallback)
		{
			return;
		}

		var container = (PXSplitContainer)PXSplitContainer0.FindControl("PXSplitContainer1");
		var form = container.FindControl("edDocument");

		RegisterControlClientIdVariable(form, "AllowFiles", "allowFilesId");
		RegisterControlClientIdVariable(form, "AllowFilesMsg", "allowFilesMsgId");
		RegisterControlClientIdVariable(form, "AllowUploadFile", "allowUploadFileId");
		RegisterControlClientIdVariable(form, "FileID", "fileIDControlID");
		RegisterControlClientIdVariable(form, "RecognizedDataJson", "recognizedDataId");
		RegisterControlClientIdVariable(form, "VendorTermIndex", "vendorTermId");
		RegisterControlClientIdVariable(edFeedback, "edFieldBound", "fieldBoundFeedbackId");
		RegisterControlClientIdVariable(edFeedback, "edTableRelated", "tableRelatedFeedbackId");
		RegisterVariable("linesHintSingleLine", PXLocalizer.Localize(Messages.LinesHintSingleLine, typeof(Messages).FullName));
		RegisterVariable("linesHintMultipleLines", PXLocalizer.Localize(Messages.LinesHintMultipleLines, typeof(Messages).FullName));
		RegisterVariable("linesHintButonText", PXLocalizer.Localize(Messages.LinesHintButonText, typeof(Messages).FullName));
		RegisterVariable("linesHintSelectTextPrefix", PXLocalizer.Localize(Messages.LinesHintSelectPrefix, typeof(Messages).FullName));
		RegisterVariable("linesHintSelectTextSingleLine", PXLocalizer.Localize(Messages.LinesHintSelectSingleLine, typeof(Messages).FullName));
		RegisterVariable("linesHintSelectTextMultipleLines", PXLocalizer.Localize(Messages.LinesHintSelectMultipleLines, typeof(Messages).FullName));

		var grid = (PXGrid)PXSplitContainer0.FindControl("edItems");
		var exitTableDefiningItem = grid.ActionBar.CustomItems.Items.FirstOrDefault(i => i.Key == "exitTableDefining");
		if (exitTableDefiningItem != null)
		{
			grid.ActionBar.CustomItems.Remove(exitTableDefiningItem);
			grid.ActionBar.CustomItems.Add(exitTableDefiningItem);
		}
	}

	private void RegisterControlClientIdVariable(Control parent, string id, string variable)
	{
		var control = parent.FindControl(id);
		var script = string.Format("let {0} = '{1}';", variable, control.ClientID);

		Page.ClientScript.RegisterClientScriptBlock(GetType(), variable, script, true);
	}

	private void RegisterVariable(string name, string value)
	{
		var script = string.Format("let {0} = '{1}';", name, value);

		Page.ClientScript.RegisterClientScriptBlock(GetType(), name, script, true);
	}
}
