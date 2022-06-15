<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="IN402010.aspx.cs"
    Inherits="Page_IN402010" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" Width="100%" TypeName="PX.Objects.IN.IntercompanyGoodsInTransitInq" PageLoadBehavior="PopulateSavedValues"
        PrimaryView="Filter" Visible="True" TabIndex="1">
        <CallbackCommands>
            <px:PXDSCallbackCommand Name="Cancel" PopupVisible="true" />
        </CallbackCommands>
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" Caption="Selection" DataMember="Filter"
        CaptionAlign="Justify" DefaultControlID="edInventoryID" MarkRequired="Dynamic">
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
            <px:PXBranchSelector CommitChanges="True" ID="bsOrgBAccountID" runat="server" DataField="OrgBAccountID" InitialExpandLevel="0" />
            <px:PXSegmentMask ID="edInventoryID" runat="server" DataField="InventoryID" DataSourceID="ds" AutoRefresh="true" CommitChanges="true" />
            <px:PXDateTimeEdit ID="edShippedBefore" runat="server" DataField="ShippedBefore" DisplayFormat="d" CommitChanges="True" />
            <px:PXCheckBox ID="chkShowOverdueItems" runat="server" DataField="ShowOverdueItems" CommitChanges="True" />
            <px:PXCheckBox ID="chkShowItemsWithoutReceipt" runat="server" DataField="ShowItemsWithoutReceipt" CommitChanges="True" />
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
            <px:PXSegmentMask ID="edSellingCompany" runat="server" DataField="SellingCompany" CommitChanges="True" />
            <px:PXSegmentMask ID="edSellingSiteID" runat="server" DataField="SellingSiteID" CommitChanges="True" />
            <px:PXSegmentMask ID="edPurchasingCompany" runat="server" DataField="PurchasingCompany" CommitChanges="True" />
            <px:PXSegmentMask ID="edPurchasingSiteID" runat="server" DataField="PurchasingSiteID" CommitChanges="True" />
        </Template>
    </px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
    <px:PXGrid ID="grid" runat="server" DataSourceID="ds" Height="144px" Style="z-index: 100; left: 0px; top: 0px;" Width="100%"
        AdjustPageSize="Auto" AllowPaging="True" AllowSearch="True" Caption="Inventory Summary" BatchUpdate="True" SkinID="PrimaryInquire"
        TabIndex="8" RestrictFields="True" SyncPosition="true" FastFilterFields="InventoryID">
        <Levels>
            <px:PXGridLevel DataMember="Results">
                <Columns>
                    <px:PXGridColumn DataField="InventoryID" />
                    <px:PXGridColumn DataField="TranDesc" />
                    <px:PXGridColumn DataField="SellingBranchID" />
                    <px:PXGridColumn DataField="SellingSiteID" />
                    <px:PXGridColumn DataField="ShipmentNbr" />
                    <px:PXGridColumn DataField="ShipDate" />
                    <px:PXGridColumn DataField="ShippedQty" />
                    <px:PXGridColumn DataField="UOM" />
                    <px:PXGridColumn DataField="ExtCost" />
                    <px:PXGridColumn DataField="DaysInTransit" />
                    <px:PXGridColumn DataField="DaysOverdue" />
                    <px:PXGridColumn DataField="PurchasingBranchID" />
                    <px:PXGridColumn DataField="PurchasingSiteID" />
                    <px:PXGridColumn DataField="POReceiptNbr" />
                    <px:PXGridColumn DataField="ShipmentStatus" />
                    <px:PXGridColumn DataField="RequestDate" />
                    <px:PXGridColumn DataField="ReceiptDate" />
                </Columns>
                <RowTemplate>
                    <px:PXSegmentMask ID="edInventoryID" runat="server" DataField="InventoryID" Enabled="false" AllowEdit="true" />
                    <px:PXSelector ID="edShipmentNbr" runat="server" DataField="ShipmentNbr" Enabled="False" AllowEdit="true" />
                    <px:PXSelector ID="edPOReceiptNbr" runat="server" DataField="POReceiptNbr" Enabled="False" AllowEdit="true" />
                </RowTemplate>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Container="Window" Enabled="True" MinHeight="150" />
        <Mode AllowAddNew="False" AllowDelete="False" AllowUpdate="False" />
    </px:PXGrid>
</asp:Content>
