<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true"
    ValidateRequest="false" CodeFile="AM215555.aspx.cs" Inherits="Page_AM215555"
    Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" TypeName="PX.Objects.AM.ManufacturingDiagram" PrimaryView="Filter" PageLoadBehavior="PopulateSavedValues"
        Visible="True" Width="100%" BorderStyle="NotSet">
        <CallbackCommands>
        </CallbackCommands>
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
    <px:PXFormView ID="form" runat="server" Width="100%" DataMember="Filter" DataSourceID="ds" OnRowUpdated="form_OnRowUpdated">
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" />
            <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Filters" LabelsWidth="SM" ControlSize="M" />
            <px:PXSegmentMask ID="fSiteID" runat="server" DataField="SiteID" FilterByAllFields="True" CommitChanges="True" />
            <px:PXSelector ID="fOrderType" runat="server" DataField="OrderType" FilterByAllFields="True" CommitChanges="True" />
            <px:PXSelector ID="fProdOrdId" runat="server" DataField="ProdOrdId" AutoRefresh="True" CommitChanges="True">
                <GridProperties FastFilterFields="InventoryID,InventoryItem__Descr,CustomerID,Customer__AcctName">
                    <Layout ColumnsMenu="False" />
                </GridProperties>
            </px:PXSelector>
            <px:PXDropDown ID="fOrderStatus" runat="server" DataField="OrderStatus" CommitChanges="True" />
            <px:PXDropDown ID="fScheduleStatus" runat="server" DataField="ScheduleStatus" AllowMultiSelect="True" CommitChanges="True" />
            <px:PXSelector ID="fProductWorkgroupId" runat="server" DataField="ProductWorkgroupId" CommitChanges="True" />
            <px:PXSelector ID="fProductManagerId" runat="server" DataField="ProductManagerId" AutoRefresh="True" CommitChanges="True" />
            <px:PXCheckBox ID="fIncludeOnHold" runat="server" DataField="IncludeOnHold" CommitChanges="True" />

            <px:PXLayoutRule runat="server" StartColumn="True" />
            <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Additional Filters" LabelsWidth="SM" ControlSize="M" />
            <px:PXSegmentMask ID="fInventoryID" runat="server" DataField="InventoryID" CommitChanges="True" />
            <px:PXSelector ID="fSoOrderType" runat="server" DataField="SoOrderType" CommitChanges="True" />
            <px:PXSelector ID="fSoNumber" runat="server" DataField="SoNumber" FilterByAllFields="True" CommitChanges="True" />
            <px:PXSegmentMask ID="fCustomerID" runat="server" DataField="CustomerID" CommitChanges="True" />

            <px:PXLayoutRule runat="server" StartColumn="True" />
            <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Date Range" LabelsWidth="XXS" ControlSize="M" Merge="True" />
            <px:PXDateTimeEdit ID="fDateFrom" runat="server" DataField="DateFrom" CommitChanges="True" />
            <px:PXDateTimeEdit ID="fDateTo" runat="server" DataField="DateTo" CommitChanges="True" />

            <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Display Settings" LabelsWidth="SM" ControlSize="M" />
            <px:PXDropDown ID="fColorCodingOrders" runat="server" DataField="ColorCodingOrders" CommitChanges="True" />
        </Template>
    </px:PXFormView>

    <px:PXFormView ID="WorkflowFictiveDiagram" runat="server" Width="100%" AutoSize="True" SyncPosition="True" RenderStyle="Simple">
        <Template>
            <px:PXManufacturingDiagram runat="server" Enabled="True" ID="mdiagram" DataField="Layout" AutoSize="True"></px:PXManufacturingDiagram>
        </Template>

        <AutoSize Container="Window" Enabled="True" MinHeight="400" />
    </px:PXFormView>
</asp:Content>

