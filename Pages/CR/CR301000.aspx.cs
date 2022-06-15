using System;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.CR.LeadMaint_Extensions;
using PX.Web.UI;
using System.Drawing;
using System.Linq;
using System.Web.UI.WebControls;
using PX.Objects.CR.Wizard;
using PX.Objects.CR.Extensions.CRDuplicateEntities;

public partial class Page_CR301000 : PX.Web.UI.PXPage
{
	private static class RelationCss
	{
		public const string NonDirect = "CssRelationNonDirect";
	}

	private LeadMaint Graph => this.ds.DataGraph as LeadMaint;
	private LeadMaint.CreateAccountFromLeadGraphExt GetCreateAccountExt() => Graph.GetExtension<LeadMaint.CreateAccountFromLeadGraphExt>();
	private LeadMaint_LinkContactExt GetLinkContactExt() => Graph.GetExtension<LeadMaint_LinkContactExt>();
	private LeadMaint_LinkAccountExt GetLinkAccountExt() => Graph.GetExtension<LeadMaint_LinkAccountExt>();

	protected void Page_Init(object sender, EventArgs e)
	{
		Master.PopupHeight = 700;
		Master.PopupWidth = 900;
	}

	protected void Page_Load(object sender, EventArgs e)
	{
		RegisterStyle(RelationCss.NonDirect, null, Color.DimGray);

		ConfigureWizardBackButton(CreateContactBtnBack);
		ConfigureWizardBackButton(btnSelectContactForLinkBack);
		ConfigureWizardBackButton(btnBackLinkContact);
		ConfigureWizardBackButton(btnLinkAccountWithoutContactBack, alwaysVisible: true);

		pnlBtnSelectContactForLink.DataBinding += pnlBtnSelectContactForLink_DataBinding;
	}

	private void ConfigureWizardBackButton(PXButton button, bool alwaysVisible = false)
	{
		button.DialogResult = WizardResult.Back;

		if (alwaysVisible is false)
		{
			// need to set Visible, because it is (should be) false in aspx (to hide it on screens without wizard)
			// otherwise it wouldn't be rendered
			// to show/hide - need to change Hidden, Visible only for "permanent" hide (during screen load)
			button.Visible = true;
			button.Hidden = true;
			button.Parent.DataBinding += (sender, e) =>
			{
				button.Hidden = GetLinkAccountExt().IsWizardOpen() is false;
			};
		}
	}


	protected void edRefContactID_EditRecord(object sender, PX.Web.UI.PXNavigateEventArgs e)
	{
		var selector = sender as PX.Web.UI.PXSelector;
		object value = null;

		if (selector != null)
			value = selector.Value;

		try
		{
			var graph = PXGraph.CreateInstance<ContactMaint>();

			if (value != null)
			{
				Contact contact = PXSelect<Contact, Where<Contact.contactID, Equal<Required<CRLead.refContactID>>>>.SelectSingleBound(graph, null, value);

				graph.Contact.Current = contact;
			}

			PXRedirectHelper.TryRedirect(graph);
		}
		catch (PX.Data.PXRedirectRequiredException e1)
		{
			PX.Web.UI.PXBaseDataSource ds = this.ds as PX.Web.UI.PXBaseDataSource;
			PX.Web.UI.PXBaseDataSource.RedirectHelper helper = new PX.Web.UI.PXBaseDataSource.RedirectHelper(ds);
			helper.TryRedirect(e1);
		}
	}

	protected void AccountInfoAttributes_DataBound(object sender, EventArgs e)
	{
		(sender as PXFormView).Visible = GetCreateAccountExt()?.NeedToUse ?? true;
	}

	protected void ContactInfoAttributes_DataBound(object sender, EventArgs e)
	{
		(sender as PXFormView).Visible = GetCreateAccountExt()?.NeedToUse ?? true;
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

		var dedupExt = this.ds.DataGraph.FindImplementation<LeadMaint.CRDuplicateEntitiesForLeadGraphExt>();

		dedupExt.Highlight(e.Row.Cells, e.Row.DataItem as CRDuplicateResult);
	}

	private void RegisterStyle(string name, Color? backColor, Color? foreColor)
	{
		Style style = new Style();
		if (backColor.HasValue) style.BackColor = backColor.Value;
		if (foreColor.HasValue) style.ForeColor = foreColor.Value;
		this.Page.Header.StyleSheet.CreateStyleRule(style, this, "." + name);
	}

	protected void pnlBtnSelectContactForLink_DataBinding(object sender, EventArgs e)
	{
		var contacts = GetLinkContactExt().Link_SelectEntityForLink.SelectMain();
		btnSelectContactForLinkNext.Enabled = contacts.Any();
	}
}
