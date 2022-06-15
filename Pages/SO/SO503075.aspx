<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="SO503075.aspx.cs" Inherits="Page_SO503075" Title="Manage Picking Queue" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.SO.SOPickingJobProcess" PrimaryView="Filter" PageLoadBehavior="PopulateSavedValues">
        <CallbackCommands>
            <px:PXDSCallbackCommand Visible="false" Name="SelectItems" CommitChanges="true" />
            <px:PXDSCallbackCommand Visible="false" Name="SelectLocations" CommitChanges="true" />
        </CallbackCommands>
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" DataMember="Filter" Caption="Selection" DefaultControlID="edAction">
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="SM" />
            <px:PXDropDown runat="server" ID="edAction" DataField="Action" CommitChanges="True" />
            <px:PXDropDown runat="server" ID="edType" DataField="WorksheetType" CommitChanges="True" />
            <px:PXDropDown runat="server" ID="edPriority" DataField="Priority" CommitChanges="True" />
            <px:PXDateTimeEdit runat="server" ID="edEndDate" DataField="EndDate" CommitChanges="True" />
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="SM" />
            <px:PXSegmentMask runat="server" ID="edSiteID" DataField="SiteID" CommitChanges="True" />
            <px:PXSegmentMask runat="server" ID="edCustomerID" DataField="CustomerID" CommitChanges="True" />
            <px:PXSelector runat="server" ID="edCarrierPlugin" DataField="CarrierPluginID" CommitChanges="True" />
            <px:PXSelector runat="server" ID="edShipVia" DataField="ShipVia" CommitChanges="True" />
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="S" />
            <px:PXLayoutRule runat="server" Merge="True" LabelsWidth="SM" ControlSize="S" />
            <px:PXSegmentMask runat="server" ID="edInventoryItem" DataField="InventoryID" CommitChanges="True" />
            <px:PXButton runat="server" ID="btnItemList" Text="List" CommandName="SelectItems" CommandSourceID="ds"/>
            <px:PXLayoutRule runat="server" EndGroup="True" />
            <px:PXLayoutRule runat="server" Merge="True" LabelsWidth="SM" ControlSize="S" />
            <px:PXSegmentMask runat="server" ID="edLocation" DataField="LocationID" CommitChanges="True" />
            <px:PXButton runat="server" ID="btnLocationList" Text="List" CommandName="SelectLocations" CommandSourceID="ds"/>
            <px:PXLayoutRule runat="server" EndGroup="True" />
            <px:PXNumberEdit runat="server" ID="edMaxNumberOfLinesInPickList" DataField="MaxNumberOfLinesInPickList" CommitChanges="True" />
            <px:PXNumberEdit runat="server" ID="edMaxQtyInLines" DataField="MaxQtyInLines" CommitChanges="True" />
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="S" GroupCaption="Process Parameters" />
            <px:PXDropDown runat="server" ID="edNewPriority" DataField="NewPriority" CommitChanges="True" />
            <px:PXSelector runat="server" ID="edAssignee" DataField="AssigneeID" CommitChanges="True" />
        </Template>
    </px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
    <px:PXGrid ID="grid" runat="server" DataSourceID="ds" Height="150px" Width="100%" AllowPaging="true" AdjustPageSize="Auto" AllowSearch="true" 
        SkinID="PrimaryInquire" BatchUpdate="true" SyncPosition="True" RepaintColumns="true" NoteIndicator="False" FilesIndicator="False">
        <Levels>
            <px:PXGridLevel DataMember="PickingJobs">
                <Columns>
                    <px:PXGridColumn DataField="Selected" AllowNull="False" TextAlign="Center" Type="CheckBox" AllowCheckAll="True" AllowSort="False" AllowMove="False" />
                    <px:PXGridColumn DataField="SOPickingWorksheet__WorksheetType" RenderEditorText="True" />
                    <px:PXGridColumn DataField="SOPickingJob__PickListNbr" LinkCommand="showPickList" />
                    <px:PXGridColumn DataField="SOPickingJob__Status" RenderEditorText="True" />
                    <px:PXGridColumn DataField="SOPickingWorksheet__PickDate" />
                    <px:PXGridColumn DataField="SOPickingJob__Priority" RenderEditorText="True" />
                    <px:PXGridColumn DataField="SOPickingJob__PreferredAssigneeID" />
                    <px:PXGridColumn DataField="SOPickingJob__ActualAssigneeID" />
                    <px:PXGridColumn DataField="SOPicker__PathLength" />
                    <px:PXGridColumn DataField="Customer__AcctCD" />
                    <px:PXGridColumn DataField="Customer__AcctName" />
                    <px:PXGridColumn DataField="Location__LocationCD" />
                    <px:PXGridColumn DataField="Location__Descr" />
                    <px:PXGridColumn DataField="Carrier__CarrierID" />
                    <px:PXGridColumn DataField="Carrier__Description" />
                    <px:PXGridColumn DataField="SOShipment__ShipmentQty" AllowNull="False" TextAlign="Right" />
                    <px:PXGridColumn DataField="SOShipment__ShipmentWeight" AllowNull="False" TextAlign="Right" />
                    <px:PXGridColumn DataField="SOShipment__ShipmentVolume" AllowNull="False" TextAlign="Right" />
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Container="Window" Enabled="True" MinHeight="200" />
        <ActionBar DefaultAction="showPickList"/>
    </px:PXGrid>
    <%-- Inventory Item List Dialog --%>
    <px:PXSmartPanel ID="InventoryItemListDialog" runat="server" Caption="Inventory Item List" CaptionVisible="True" Key="selectedItems" LoadOnDemand="True" AutoReload="True" AutoRepaint="True">
        <px:PXGrid ID="gridItemList" runat="server" DataSourceID="ds" SkinID="Details" SyncPosition="True">
            <Levels>
                <px:PXGridLevel DataMember="selectedItems">
                    <Columns>
                        <px:PXGridColumn DataField="InventoryID" />
                        <px:PXGridColumn DataField="Descr" />
                    </Columns>
                    <RowTemplate>
                        <px:PXSegmentMask ID="iidInventoryID" runat="server" DataField="InventoryID" CommitChanges="True" />
                    </RowTemplate>
                    <Layout ColumnsMenu="False" />
                </px:PXGridLevel>
            </Levels>
            <AutoSize Enabled="True" MinHeight="300" />
            <Mode AllowAddNew="True" AllowDelete="True" AllowUpdate="True" AllowUpload="True" AllowFormEdit="False" />
        </px:PXGrid>
    </px:PXSmartPanel>
    <%-- Location List Dialog --%>
    <px:PXSmartPanel ID="LocationListDialog" runat="server" Caption="Location List" CaptionVisible="True" Key="selectedLocations" LoadOnDemand="True" AutoReload="True" AutoRepaint="True">
        <px:PXGrid ID="gridLocationList" runat="server" DataSourceID="ds" SkinID="Details" SyncPosition="True">
            <Levels>
                <px:PXGridLevel DataMember="selectedLocations">
                    <Columns>
                        <px:PXGridColumn DataField="LocationID" />
                        <px:PXGridColumn DataField="Descr" />
                    </Columns>
                    <RowTemplate>
                        <px:PXSegmentMask ID="ldLocationID" runat="server" DataField="LocationID" CommitChanges="True" />
                    </RowTemplate>
                    <Layout ColumnsMenu="False" />
                </px:PXGridLevel>
            </Levels>
            <AutoSize Enabled="True" MinHeight="300" />
            <Mode AllowAddNew="True" AllowDelete="True" AllowUpdate="True" AllowUpload="True" AllowFormEdit="False" />
        </px:PXGrid>
    </px:PXSmartPanel>
    <!--#include file="~\Pages\SO\Includes\ShowPickListPanel.inc"-->
</asp:Content>