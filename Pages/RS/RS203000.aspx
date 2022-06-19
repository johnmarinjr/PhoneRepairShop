<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="RS203000.aspx.cs" Inherits="Page_RS203000" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/FormTab.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" Runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%"
        TypeName="PhoneRepairShop.RSSVRepairPriceMaint"
        PrimaryView="RepairPrices"
        >
		<CallbackCommands>

		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" Runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" DataMember="RepairPrices" Width="100%" AllowAutoHide="false">
        <Template>
          <px:PXLayoutRule ID="PXLayoutRule1" runat="server" StartRow="True" ControlSize="M" LabelsWidth="S"></px:PXLayoutRule>
          <px:PXSelector runat="server" ID="CstPXSelector3" DataField="ServiceID" ></px:PXSelector>
          <px:PXSelector runat="server" ID="CstPXSelector1" DataField="DeviceID" ></px:PXSelector>
          <px:PXLayoutRule ID="PXLayoutRule2" runat="server" StartColumn="True" ControlSize="M" LabelsWidth="S"></px:PXLayoutRule>
          <px:PXNumberEdit runat="server" ID="CstPXNumberEdit2" DataField="Price" ></px:PXNumberEdit>
        </Template>
		<AutoSize Container="Window" Enabled="True" MinHeight="200" />
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" Runat="Server">
	<px:PXTab ID="tab" runat="server" Width="100%" Height="150px" DataSourceID="ds" AllowAutoHide="false">
		<Items>
			<px:PXTabItem Text="Repair Items">
				<Template>
					<px:PXGrid SyncPosition="True" Width="100%" SkinID="Details" runat="server" ID="CstPXGrid5">
						<Levels>
							<px:PXGridLevel DataMember="RepairItems" >
								<Columns>
									<px:PXGridColumn CommitChanges="True" DataField="RepairItemType" Width="70" ></px:PXGridColumn>
									<px:PXGridColumn CommitChanges="True" Type="CheckBox" DataField="Required" Width="80" ></px:PXGridColumn>
									<px:PXGridColumn CommitChanges="True" DataField="InventoryID" Width="70" ></px:PXGridColumn>
									<px:PXGridColumn DataField="InventoryID_description" Width="280" ></px:PXGridColumn>
									<px:PXGridColumn CommitChanges="True" DataField="BasePrice" Width="100" ></px:PXGridColumn>
									<px:PXGridColumn CommitChanges="True" Type="CheckBox" DataField="IsDefault" Width="80" ></px:PXGridColumn></Columns>
								<RowTemplate>
									<px:PXSegmentMask runat="server" ID="CstPXSegmentMask6" DataField="InventoryID" AutoRefresh="True" ></px:PXSegmentMask></RowTemplate></px:PXGridLevel></Levels>
						<AutoSize Enabled="True" ></AutoSize>
						<Mode InitNewRow="True" ></Mode></px:PXGrid></Template>
			</px:PXTabItem>

		</Items>
		<AutoSize Container="Window" Enabled="True" MinHeight="150" ></AutoSize>
	</px:PXTab>
</asp:Content>