using System;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Collections.Generic;
using System.Linq;
using PX.SM;
using PX.Data;
using PX.MessageBus.Implementation.ReportIncident;
using CommonServiceLocator;
using PX.Licensing;

public partial class Frames_Trace : PX.Web.UI.PXPage
{
	private readonly ILicensing _licensing = ServiceLocator.Current.GetInstance<ILicensing>();

	protected void Page_Load(object sender, EventArgs e)
	{
		IEnumerable<TraceItem> errors = TraceMaint.GetTrace().Reverse();

		DrawTable(placeholder, errors);

		lblVersion.Text = PX.Data.PXVersionInfo.Version;
		String cust = Customization.CstWebsiteStorage.PublishedProjectList;
		lblCustomization.Text = String.IsNullOrEmpty(cust) ? PXLocalizer.Localize(PX.SM.Messages.None, typeof(PX.SM.Messages).FullName) : cust;
		btnReportIncident.Style.Add("float", "right");
		btnReportIncident.Style.Add("border-radius", "0px");
		btnReportIncident.Style.Add("height", "31px");

		var graph = new PreferencesGeneralMaint();

		var item = graph.PrefsGlobal?.Select()?.ToArray()?.FirstOrDefault()?.GetItem<PreferencesGlobal>();
		var enableTelemetry = item?.EnableTelemetry == true;
		var isLicensed = _licensing?.License?.Licensed == true;

		if (PX.Common.WebConfig.EnableReportIncident)
		{
			if (!enableTelemetry || !isLicensed)
			{
				btnReportIncident.Enabled = false;
				btnReportIncident.ToolTip = "The button is disabled because the option Send Diagnostic & Usage data to Acumatica is not switched on";
			}
		}
		else
		{
			btnReportIncident.Visible = false;
		}
	}

	protected void btnReportIncident_Click(object sender, EventArgs e)
	{
		var incidentId = Guid.NewGuid().ToString();
		PXTrace.Logger.ForTelemetry("ReportIncident", "Incident").Information("User reports incident with id {incidentId}", incidentId);
		var userName = PXAccess.GetUserName();

		if (PX.Common.WebConfig.IsClusterEnabled)
        {
			ClusterReportIncident.ReportIncident(userName, incidentId);
		}
		else
        {
			PX.Telemetry.TelemetryReporter.ReportIncident(userName, incidentId);
		}

		string script = $"ReportIncident('{incidentId}');";
		ClientScript.RegisterClientScriptBlock(this.GetType(), "repInc", script, true);
	}
	private void DrawTable(HtmlGenericControl place, IEnumerable<TraceItem> errros)
	{
		foreach (TraceItem item in errros)
		{
			Control control = Page.LoadControl("~/Controls/TraceItem.ascx");
			control.GetType().InvokeMember("Initialise",	System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
				null, control, new Object[] { item });

			place.Controls.Add(control);
		}
	}
}
