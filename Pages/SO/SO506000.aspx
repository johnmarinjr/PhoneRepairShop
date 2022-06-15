<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="SO506000.aspx.cs" Inherits="Page_SO506000" Title="Manifests/ScanForms" %>

<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" Width="100%" runat="server" Visible="True" PrimaryView="Filter" TypeName="PX.ExternalCarriersHelper.SOCreateShipmentManifestProcess">
		<CallbackCommands/>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" Width="100%" DataMember="Filter"> 
		<Template>
		    	<px:PXLayoutRule  runat="server" StartColumn="True" LabelsWidth="S" />
			<px:PXSelector ID="edCarrierID" runat="server" DataField="CarrierID" CommitChanges="true" />
			<px:PXDateTimeEdit ID="edShipDate" runat="server" DataField="ShipDate" CommitChanges="true" />
                        <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S"  />
                        <px:PXCheckBox CommitChanges="True" ID="chkPrintWithDeviceHub" runat="server" DataField="PrintWithDeviceHub" AlignLeft="true" />
                        <px:PXSelector CommitChanges="True" ID="edPrinterID" runat="server" DataField="PrinterID" />
		</Template>	
	</px:PXFormView>
	<px:PXGrid ID="grid" runat="server" Height="400px" Width="100%" Style="z-index: 100" AllowPaging="true" AdjustPageSize="Auto" AllowSearch="true" DataSourceID="ds" BatchUpdate="True" SkinID="PrimaryInquire" Caption="Shipments"  
        FastFilterFields="ShipmentNbr, CustomerID, Ship Via" SyncPosition="true" TabIndex="300">
		<Levels>
			<px:PXGridLevel DataMember="ShipmentList">
				<Columns>
					<px:PXGridColumn DataField="Selected" TextAlign="Center" Type="CheckBox" AllowCheckAll="True" AllowSort="False" AllowMove="False" />
					<px:PXGridColumn DataField="ShipmentNbr" Width="150px" LinkCommand="viewDetails" />
					<px:PXGridColumn DataField="CustomerID" Width="150px" />
                    <px:PXGridColumn DataField="CustomerID_description" Width="220px" />
                    <px:PXGridColumn DataField="ShipVia" DisplayMode="Hint" Width="150px">
                    </px:PXGridColumn>
				</Columns>
			</px:PXGridLevel>
		</Levels>
		<AutoSize Container="Window" Enabled="True" MinHeight="400" />
		<Layout ShowRowStatus="False" />
	</px:PXGrid>
</asp:Content>