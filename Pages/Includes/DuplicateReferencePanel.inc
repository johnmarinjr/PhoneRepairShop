<px:PXSmartPanel ID="panelDuplicate" runat="server" Caption="Duplicate Reference Nbr." CaptionVisible="true" LoadOnDemand="true" Key="duplicatefilter"
	AutoCallBack-Enabled="true" AutoCallBack-Command="Refresh" CallBackMode-CommitChanges="True"
	CallBackMode-PostData="Page">
	<div style="padding: 5px">
		<px:PXFormView ID="PXFormViewPanelDuplicate" runat="server" DataSourceID="ds" CaptionVisible="False" DataMember="duplicatefilter">
			<ContentStyle BackColor="Transparent" BorderStyle="None" />
			<Template>
				<px:PXLayoutRule ID="PXLayoutRulePanelDuplicate" runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="SM" />
				<px:PXLabel Size="xl" ID="lblMessage" runat="server">Record already exists. Please enter new Reference Nbr.</px:PXLabel>
				<px:PXMaskEdit CommitChanges="True" ID="edRefNbr" runat="server" DataField="RefNbr" InputMask="&gt;CCCCCCCCCCCCCCC" />
			</Template>
		</px:PXFormView>
	</div>
	<px:PXPanel ID="PXPanelDuplicateBtn" runat="server" SkinID="Buttons">
		<px:PXButton ID="PXButtonOK" runat="server" DialogResult="OK" Text="OK" CommandSourceID="ds" />
		<px:PXButton ID="PXButtonCancel" runat="server" DialogResult="Cancel" Text="Cancel" CommandSourceID="ds" />
	</px:PXPanel>
</px:PXSmartPanel>