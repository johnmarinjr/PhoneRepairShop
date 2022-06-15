using System;
using System.Drawing;
using System.Web.UI.WebControls;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.IN.Matrix.DAC.Unbound;

public partial class Page_CR304500 : PX.Web.UI.PXPage
{
	private static class RelationCss
	{
		public const string NonDirect = "CssRelationNonDirect";
	}

	protected void Page_Init(object sender, EventArgs e)
	{
		this.Master.PopupHeight = 700;
		this.Master.PopupWidth = 1100;
	}

	protected void MatrixAttributes_AfterSyncState(object sender, EventArgs e)
	{
		InventoryMatrixEntry.EnableCommitChangesAndMoveExtraColumnAtTheEnd(MatrixAttributes.Columns, 0);
	}

	protected void MatrixMatrix_AfterSyncState(object sender, EventArgs e)
	{
		InventoryMatrixEntry.EnableCommitChangesAndMoveExtraColumnAtTheEnd(MatrixMatrix.Columns);
	}

	protected void MatrixItems_AfterSyncState(object sender, EventArgs e)
	{
		InventoryMatrixEntry.InsertAttributeColumnsByTemplateColumn(
			MatrixItems.Columns, ds.DataGraph.Caches[typeof(MatrixInventoryItem)].Fields);
	}

	protected void MatrixItems_OnInit(object sender, EventArgs e)
	{
		InventoryMatrixEntry.InsertAttributeColumnsByTemplateColumn(
			MatrixItems.Columns, ds.DataGraph.Caches[typeof(MatrixInventoryItem)].Fields);
	}

	protected void RelationsGrid_RowDataBound(object sender, PX.Web.UI.PXGridRowEventArgs e)
	{
		var row = PXResult.Unwrap<CRRelation>(e.Row.DataItem);
		if (row == null) return;

		if (row.IsDirectRole == false)
		{
			e.Row.Style.CssClass = RelationCss.NonDirect;
		}
	}

	private void RegisterStyle(string name, Color? backColor, Color? foreColor)
	{
		Style style = new Style();
		if (backColor.HasValue) style.BackColor = backColor.Value;
		if (foreColor.HasValue) style.ForeColor = foreColor.Value;
		this.Page.Header.StyleSheet.CreateStyleRule(style, this, "." + name);
	}
}
