using System;
using System.Drawing;
using System.Web.UI.WebControls;
using PX.Data;
using PX.Common.Collection;
using PX.Data.EP;
using PX.Objects.CR;
using PX.Objects.EP;
using PX.TM;
using PX.Web.Controls;
using PX.Web.UI;

public partial class Page_EP405000 : PX.Web.UI.PXPage
{	
	protected void Page_Init(object sender, EventArgs e)
	{
		Master.PopupHeight = 460;
		Master.PopupWidth = 800;

		PXGridWithPreview grd = this.gridActivities;
		string internalGridID = null;
		foreach (PXDataSource.CommandState command in ds.GetCommandStates())
			if (command.Name != null && command.Name.StartsWith("NewActivity$", StringComparison.OrdinalIgnoreCase))
			{
				this.ds.CallbackCommands.Add(new PXDSCallbackCommand { Name = command.Name, CommitChanges = true, Visible = false });
				if (grd != null)
				{
					if (internalGridID == null) internalGridID = grd.DataMember + "_grid";
					PXDataSource.CommandState.Images images = command.Image;
					PXToolBarButton button = new PXToolBarButton { Text = command.DisplayName };
					button.Images.Normal = string.IsNullOrEmpty(images.Normal) ? Sprite.Main.GetFullUrl(Sprite.Main.AddNew) : images.Normal;
					button.Images.Disabled = string.IsNullOrEmpty(images.Disabled) ? string.Empty : images.Disabled;
					button.AutoCallBack.Enabled = true;
					button.AutoCallBack.Command = command.Name;
					button.AutoCallBack.Target = "ds";
					button.PopupCommand.Enabled = true;
					button.PopupCommand.Command = "Refresh";
					button.PopupCommand.Target = internalGridID;
					grd.ActionBar.CustomItems.Add(button);
				}
			}
	}
}
