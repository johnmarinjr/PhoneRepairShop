<px:PXSmartPanel ID="PanelCreateInvoice" runat="server" Style="z-index: 108; position: absolute; left: 27px; top: 99px;" Caption="Create Invoice"
	CaptionVisible="True" LoadOnDemand="true" ShowAfterLoad="true" Key="CreateInvoicesParams" AutoCallBack-Enabled="true" AutoCallBack-Target="formCreateInvoice" AutoCallBack-Command="Refresh"
	CallBackMode-CommitChanges="True" CallBackMode-PostData="Page" AcceptButtonID="PXButton5" CancelButtonID="PXButton6">
	<px:PXFormView ID="formCreateInvoice" runat="server" DataSourceID="ds" Style="z-index: 100; text-align: left;" Width="100%" Caption="Services Settings" CaptionVisible="False" SkinID="Transparent"
		DataMember="CreateInvoicesParams">
		<Template>
			<px:PXLayoutRule ID="PXLayoutRule6" runat="server" StartRow="True" LabelsWidth="SM" ControlSize="XM" SuppressLabel="True" />
			<px:PXCheckBox ID="edMakeQuotePrimary" runat="server" DataField="MakeQuotePrimary" CommitChanges="true"/>
			<px:PXCheckBox ID="edRecalculatePrices" runat="server" DataField="RecalculatePrices" CommitChanges="true" />
			<px:PXCheckBox ID="edOverrideManualPrices" runat="server" DataField="OverrideManualPrices" CommitChanges="true" Style="margin-left: 25px" />
			<px:PXCheckBox ID="edRecalculateDiscounts" runat="server" DataField="RecalculateDiscounts" CommitChanges="true" />
			<px:PXCheckBox ID="edOverrideManualDiscounts" runat="server" DataField="OverrideManualDiscounts" CommitChanges="true" Style="margin-left: 25px" />
			<px:PXCheckBox ID="edOverrideManualDocGroupDiscounts" runat="server" DataField="OverrideManualDocGroupDiscounts" CommitChanges="true" Style="margin-left: 25px" />
			<px:PXCheckBox ID="edConfirmManualAmount" runat="server" DataField="ConfirmManualAmount" Width="480px" CommitChanges="true" />
		</Template>
	</px:PXFormView>
	<div style="padding: 5px; text-align: right;">
		<px:PXButton ID="PXButton5" runat="server" Text="Create" CommitChanges="True" DialogResult="Yes" Width="63px" Height="20px" Style="margin-left: 5px" />
		<px:PXButton ID="PXButton6" runat="server" Text="Cancel" CommitChanges="True" DialogResult="Cancel" Width="63px" Height="20px" Style="margin-left: 5px" />
	</div>
</px:PXSmartPanel>