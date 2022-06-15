using System;
using PX.Objects.AP;
using PX.Data;

public partial class Page_AP301000 : PX.Web.UI.PXPage
{
	private static class PickCss
	{
		public const string Enabled = "cssEnabled";
	}

	protected void Page_Init(object sender, EventArgs e)
	{
		this.Master.PopupHeight = 700;
		this.Master.PopupWidth = 1070;
		if (this.Master.DocumentsGrid != null)
			this.Master.SetDocumentTemplate(docsTemplate.Columns[0].CellTemplate);
	}

	protected void transactionsGrid_RowDataBound(object sender, PX.Web.UI.PXGridRowEventArgs e)
	{
		var graph = (APInvoiceEntry)((PX.Web.UI.PXGrid)sender).DataGraph;
		if (graph.Document.Current.Status == APDocStatus.UnderReclassification)
		{
			APTran row = e.Row.DataItem as APTran;
			PXFieldState state = null;

			state = graph.Transactions.Cache.GetValueExt<APTran.projectID>(row) as PXFieldState;
			if(state.Enabled)
				e.Row.Cells["ProjectID"].Style.CssClass = PickCss.Enabled;

			state = graph.Transactions.Cache.GetValueExt<APTran.taskID>(row) as PXFieldState;
			if (state.Enabled)
				e.Row.Cells["TaskID"].Style.CssClass = PickCss.Enabled;

			state = graph.Transactions.Cache.GetValueExt<APTran.costCodeID>(row) as PXFieldState;
			if (state.Enabled)
				e.Row.Cells["CostCodeID"].Style.CssClass = PickCss.Enabled;

			state = graph.Transactions.Cache.GetValueExt<APTran.accountID>(row) as PXFieldState;
			if (state.Enabled)
				e.Row.Cells["AccountID"].Style.CssClass = PickCss.Enabled;

			state = graph.Transactions.Cache.GetValueExt<APTran.subID>(row) as PXFieldState;
			if (state.Enabled)
				e.Row.Cells["SubID"].Style.CssClass = PickCss.Enabled;

			state = graph.Transactions.Cache.GetValueExt<APTran.pOLineNbr>(row) as PXFieldState;
			if (state.Enabled)
				e.Row.Cells["POLineNbr"].Style.CssClass = PickCss.Enabled;

			state = graph.Transactions.Cache.GetValueExt<PX.Objects.CN.Subcontracts.AP.CacheExtensions.ApTranExt.subcontractLineNbr>(row) as PXFieldState;
			if (state.Enabled)
				e.Row.Cells["SubcontractLineNbr"].Style.CssClass = PickCss.Enabled;
		}
	}
}
