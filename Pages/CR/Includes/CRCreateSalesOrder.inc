<px:PXSmartPanel ID="panelCreateSalesOrder" Key="CreateOrderParams" runat="server" Style="z-index: 108; left: 27px; position: absolute; top: 99px;" Caption="Create Sales Order" CaptionVisible="true"
	DesignView="Hidden" LoadOnDemand="true" AutoCallBack-Enabled="true" AutoCallBack-Target="formCopyTo" AutoCallBack-Command="Refresh"	CallBackMode-CommitChanges="True" CallBackMode-PostData="Page"
	AcceptButtonID="btnSave" CancelButtonID="btnCancel">
	<px:PXFormView ID="formCopyTo" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" CaptionVisible="False" DataMember="CreateOrderParams">
		<ContentStyle BackColor="Transparent" BorderStyle="None" />
		<Template>
			<px:PXLayoutRule ID="PXLayoutRule7" runat="server" StartColumn="True" LabelsWidth="S" ControlSize="M" />
				<px:PXSelector ID="edOrderType" runat="server" AllowNull="False" DataField="OrderType" DisplayMode="Text" Width="216px" CommitChanges="True" />
				<px:PXCheckBox ID="edMakeQuotePrimary" runat="server" DataField="MakeQuotePrimary" CommitChanges="true"/>
				<px:PXCheckBox ID="edRecalculatePrices" runat="server" DataField="RecalculatePrices" CommitChanges="true" />
				<px:PXCheckBox ID="edOverrideManualPrices" runat="server" DataField="OverrideManualPrices" CommitChanges="true" Style="margin-left: 25px" />
				<px:PXCheckBox ID="edRecalculateDiscounts" runat="server" DataField="RecalculateDiscounts" CommitChanges="true"/>
				<px:PXCheckBox ID="edOverrideManualDiscounts" runat="server" DataField="OverrideManualDiscounts" CommitChanges="true" Style="margin-left: 25px"/>
				<px:PXCheckBox ID="edOverrideManualDocGroupDiscounts" runat="server" DataField="OverrideManualDocGroupDiscounts" CommitChanges="true" Style="margin-left: 25px" /> 
				<px:PXCheckBox ID="edConfirmManualAmount" runat="server" DataField="ConfirmManualAmount" Width="480px" CommitChanges="true" UncheckImages="" />
				<px:PXCheckBox ID="cstChkAMIncludeEstimate" runat="server" DataField="AMIncludeEstimate" />
				<px:PXCheckBox ID="cstChkAMCopyConfigurations" runat="server" DataField="AMCopyConfigurations" />
		</Template>
	</px:PXFormView>

	<div style="padding: 5px; text-align: right;">
		<px:PXButton ID="btnSave" runat="server" CommitChanges="True" DialogResult="OK" Text="OK" Height="20"/>
		<px:PXButton ID="btnCancel" runat="server" DialogResult="Cancel" Text="Cancel" Height="20" />
	</div>
</px:PXSmartPanel>