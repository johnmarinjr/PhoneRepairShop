using System;
using PX.Objects.CR;
using PX.Data;
using System.Drawing;
using System.Web.UI.WebControls;


public partial class Page_CR306000 : PX.Web.UI.PXPage
{
	private static class RelationCss
	{
		public const string NonDirect = "CssRelationNonDirect";
	}

    protected void Page_Init(object sender, EventArgs e)
	{
		this.Master.PopupHeight = 700;
		this.Master.PopupWidth = 900;
	}

	protected void Page_Load(object sender, EventArgs e)
	{
		RegisterStyle(RelationCss.NonDirect, null, Color.DimGray);
	}

    protected void edContactID_EditRecord(object sender, PX.Web.UI.PXNavigateEventArgs e)
    {
        CRCaseMaint casemaint = this.ds.DataGraph as CRCaseMaint;
        if (casemaint != null)
        {
            CRCase currentcase = this.ds.DataGraph.Views[this.ds.DataGraph.PrimaryView].Cache.Current as CRCase;
            if (currentcase.ContactID == null && currentcase.CustomerID != null)
            {
                {
                    try
                    {
                        casemaint.addNewContact.Press();
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

	private void RegisterStyle(string name, Color? backColor, Color? foreColor)
	{
		Style style = new Style();
		if (backColor.HasValue) style.BackColor = backColor.Value;
		if (foreColor.HasValue) style.ForeColor = foreColor.Value;
		this.Page.Header.StyleSheet.CreateStyleRule(style, this, "." + name);
	}
}
