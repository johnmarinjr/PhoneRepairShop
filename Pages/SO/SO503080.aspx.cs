using System;
using System.Drawing;
using System.Linq;
using System.Web.UI.WebControls;
using PX.Web.UI;
using PX.Common;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO;

public partial class Page_SO503080 : PX.Web.UI.PXPage
{
	private static class PriorityCss
	{
		public const string Urgent = "Css_SOPickingJob_Priority_Urgent";
		public const string High = "Css_SOPickingJob_Priority_High";
		public const string Medium = "Css_SOPickingJob_Priority_Medium";
		public const string Low = "Css_SOPickingJob_Priority_Low";
	}

	protected void Page_Init(object sender, EventArgs e) { }

	protected void Page_Load(object sender, EventArgs e)
	{
		longRunExists = null;
		RegisterStyle(PriorityCss.Urgent, null, Color.Red, true);
		RegisterStyle(PriorityCss.High, null, Color.Orange, false);
		RegisterStyle(PriorityCss.Medium, null, Color.Black, false);
		RegisterStyle(PriorityCss.Low, null, Color.Gray, false);
	}

	private void RegisterStyle(string name, Color? backColor, Color? foreColor, bool bold)
	{
		Style style = new Style();
		if (backColor.HasValue) style.BackColor = backColor.Value;
		if (foreColor.HasValue) style.ForeColor = foreColor.Value;
		if (bold) style.Font.Bold = true;
		this.Page.Header.StyleSheet.CreateStyleRule(style, this, "." + name);
	}

	private bool? longRunExists;
	protected void JobGrid_RowDataBound(object sender, PX.Web.UI.PXGridRowEventArgs e)
	{
		var job = PXResult.UnwrapMain(e.Row.DataItem);
		if (job is SOPickingJob pickJob)
		{
			var priorityCell = e.Row.Cells.Cast<PXGridCell>().First(c => string.Equals(c.DataField, nameof(SOPickingJob.Priority), StringComparison.OrdinalIgnoreCase));
			string newStyle = priorityCell.Style.CssClass;

			switch (pickJob.Priority)
			{
				case WMSJob.priority.Urgent: newStyle = PriorityCss.Urgent; break;
				case WMSJob.priority.High: newStyle = PriorityCss.High; break;
				case WMSJob.priority.Medium: newStyle = PriorityCss.Medium; break;
				case WMSJob.priority.Low: newStyle = PriorityCss.Low; break;
			}

			if (newStyle != priorityCell.Style.CssClass)
				priorityCell.Style.CssClass = newStyle;

			var timeCell = e.Row.Cells.Cast<PXGridCell>().First(c => string.Equals(c.DataField, nameof(SOPickingJob) + "__" + nameof(SOPickingJobEnq.timeInQueue), StringComparison.OrdinalIgnoreCase));
			timeCell.Style.CssClass = (longRunExists ?? (longRunExists = PXLongOperation.Exists(((PX.Web.UI.PXGrid)sender).DataGraph)).Value)
				? PriorityCss.Medium
				: PriorityCss.Low;
		}
	}
}