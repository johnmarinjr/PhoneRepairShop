using System;
using System.Drawing;
using System.Web.UI.WebControls;
using PX.Web.UI;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.CR.Extensions.CRDuplicateEntities;

public partial class Page_CR303000 : PX.Web.UI.PXPage
{
	private static class RelationCss
	{
		public const string NonDirect = "CssRelationNonDirect";
	}

	protected void Page_Init(object sender, EventArgs e)
	{
		Master.PopupHeight = 600;
		Master.PopupWidth = 800;
	}

	protected void Page_Load(object sender, EventArgs e)
	{
		RegisterStyle(RelationCss.NonDirect, null, Color.DimGray);
	}

	protected void edPrmCntID_EditRecord(object sender, PX.Web.UI.PXNavigateEventArgs e)
	{
		var baMaint = this.ds.DataGraph as BusinessAccountMaint;
		if (baMaint != null)
		{
			var ext = baMaint.GetExtension<BusinessAccountMaint.PrimaryContactGraphExt>();

			BAccount bAccount = baMaint.BAccount.Cache.Current as BAccount;
			if (ext != null && bAccount != null && (bAccount.PrimaryContactID == null || bAccount.PrimaryContactID < 0))
			{
				{
					try
					{
						ext.AddNewPrimaryContact.Press();
					}
					catch (PX.Data.PXRedirectRequiredException e1)
					{
						PX.Web.UI.PXBaseDataSource ds = this.ds as PX.Web.UI.PXBaseDataSource;
						PX.Web.UI.PXBaseDataSource.RedirectHelper helper = new PX.Web.UI.PXBaseDataSource.RedirectHelper(ds);
						helper.TryRedirect(e1);
					}
				}
			}
		}
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

	protected void Duplicates_RowDataBound(object sender, PXGridRowEventArgs e)
	{
		if (e.Row.DataItem == null)
			return;

		var dedupExt = this.ds.DataGraph.FindImplementation<BusinessAccountMaint.CRDuplicateEntitiesForBAccountGraphExt>();

		dedupExt.Highlight(e.Row.Cells, e.Row.DataItem as CRDuplicateResult);
	}

	private void RegisterStyle(string name, Color? backColor, Color? foreColor)
	{
		Style style = new Style();
		if (backColor.HasValue) style.BackColor = backColor.Value;
		if (foreColor.HasValue) style.ForeColor = foreColor.Value;
		this.Page.Header.StyleSheet.CreateStyleRule(style, this, "." + name);
	}
}
