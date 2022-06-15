using PX.Objects.GL;
using System;
using System.Drawing;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class Page_GL404000 : PX.Web.UI.PXPage
{
	protected void Page_Init(object sender, EventArgs e)
	{
		this.grid.FilterShortCuts = true;
	}

	private static class TranCss
	{
		public const string Light = "CssIsErrorLight";
		public const string Regular = "CssIsErrorRegular";
		public const string Dark = "CssIsErrorDark";
	}

	protected void Page_Load(object sender, EventArgs e)
	{
		RegisterStyle(TranCss.Light, Color.MistyRose);
		RegisterStyle(TranCss.Regular, Color.LightPink);
		RegisterStyle(TranCss.Dark, Color.LightCoral);
	}

	private void RegisterStyle(string name, Color backColor)
	{
		var style = new Style
		{
			BackColor = backColor
		};
		Page.Header.StyleSheet.CreateStyleRule(style, this, "." + name);
	}

	protected void Tran_RowDataBound(object sender, PX.Web.UI.PXGridRowEventArgs e)
	{
		GLTranR item = e.Row.DataItem as GLTranR;
		if (item == null) return;

		if (item.MLScoreColor == GLTranR.StringColor.Light)
		{
			e.Row.Style.CssClass = TranCss.Light;
		}
		else if (item.MLScoreColor == GLTranR.StringColor.Regular)
		{
			e.Row.Style.CssClass = TranCss.Regular;
		}
		else if (item.MLScoreColor == GLTranR.StringColor.Dark)
		{
			e.Row.Style.CssClass = TranCss.Dark;
		}
	}
}
