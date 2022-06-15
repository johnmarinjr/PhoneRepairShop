<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="SO503080.aspx.cs" Inherits="Page_SO503080" Title="Picking Queue" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" TypeName="PX.Objects.SO.SOPickingJobEnq" PrimaryView="Filter" PageLoadBehavior="PopulateSavedValues" Visible="True" Width="100%" >
        <CallbackCommands>
            <px:PXDSCallbackCommand Name="Save" Visible="False" />
        </CallbackCommands>
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Width="100%" DataMember="Filter" >
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="SM" />
            <px:PXSegmentMask runat="server" ID="edSiteID" DataField="SiteID" CommitChanges="True" />
            <px:PXSelector runat="server" ID="edAssignee" DataField="AssigneeID" CommitChanges="True" />
            <px:PXDropDown runat="server" ID="edType" DataField="WorksheetType" CommitChanges="True" />
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="SM" />
            <px:PXSegmentMask runat="server" ID="edCustomerID" DataField="CustomerID" CommitChanges="True" />
            <px:PXSelector runat="server" ID="edCarrierPlugin" DataField="CarrierPluginID" CommitChanges="True" />
            <px:PXSelector runat="server" ID="edShipVia" DataField="ShipVia" CommitChanges="True" />
            <%-- Tab switchers --%>
            <px:PXCheckBox ID="chkShowPick" runat="server" DataField="ShowPick" Visible ="False" />
            <px:PXCheckBox ID="chkShowPack" runat="server" DataField="ShowPack" Visible ="False" />
        </Template>
    </px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
    <px:PXTab ID="tab" runat="server" Height="540px" Style="z-index: 100;" Width="100%">
        <Items>
            <px:PXTabItem Text="Pick" VisibleExp="DataControls[&quot;chkShowPick&quot;].Value == true" BindingContext="form">
                <Template>
                    <px:PXGrid ID="grid" runat="server" DataSourceID="ds" Height="150px" Width="100%" AllowPaging="true" AdjustPageSize="Auto" AllowSearch="true" 
                        SkinID="Details" SyncPosition="True" SyncPositionWithGraph="True" RepaintColumns="true" NoteIndicator="False" FilesIndicator="False" OnRowDataBound="JobGrid_RowDataBound">
                        <Levels>
                            <px:PXGridLevel DataMember="PickingJobs">
                                <Columns>
                                    <px:PXGridColumn DataField="SOPickingWorksheet__WorksheetType" RenderEditorText="True" />
                                    <px:PXGridColumn DataField="SOPickingJob__PickListNbr" LinkCommand="showPickList" />
                                    <px:PXGridColumn DataField="SOPickingJob__Status" RenderEditorText="True" />
                                    <px:PXGridColumn DataField="Priority" CommitChanges="True" />
                                    <px:PXGridColumn DataField="PreferredAssigneeID" CommitChanges="True" />
                                    <px:PXGridColumn DataField="SOPickingJob__ActualAssigneeID" />
                                    <px:PXGridColumn DataField="SOPickingWorksheet__PickDate" />
                                    <px:PXGridColumn DataField="SOPickingJob__TimeInQueue" />
                                    <px:PXGridColumn DataField="SOPicker__PathLength" />
                                    <px:PXGridColumn DataField="SOPickingJob__PickingStartedAt" DisplayFormat="g" />
                                    <px:PXGridColumn DataField="AutomaticShipmentConfirmation" CommitChanges="True" Type="CheckBox" />
                                    <px:PXGridColumn DataField="SOShipment__ShipmentQty" TextAlign="Right" />
                                    <px:PXGridColumn DataField="SOShipment__ShipmentWeight" TextAlign="Right" />
                                    <px:PXGridColumn DataField="SOShipment__ShipmentVolume" TextAlign="Right" />
                                    <px:PXGridColumn DataField="SOShipment__ShipDate" />
                                    <px:PXGridColumn DataField="Customer__AcctCD" />
                                    <px:PXGridColumn DataField="Customer__AcctName" />
                                    <px:PXGridColumn DataField="Location__LocationCD" />
                                    <px:PXGridColumn DataField="Location__Descr" />
                                    <px:PXGridColumn DataField="Carrier__CarrierID" />
                                    <px:PXGridColumn DataField="Carrier__Description" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <Mode AllowFormEdit="False" AllowSort="False" />
                        <AutoSize Enabled="True" MinHeight="200" />
                        <ActionBar DefaultAction="showPickList">
                            <Actions>
                                <AddNew ToolBarVisible="False" />
                                <Delete ToolBarVisible="False" />
                            </Actions>
                            <CustomItems>
                                <px:PXToolBarButton CommandName="HoldJob" CommandSourceID="ds" DependOnGrid="grid" />
                            </CustomItems>
                        </ActionBar>
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Pack" VisibleExp="DataControls[&quot;chkShowPack&quot;].Value == true" BindingContext="form">
                <Template>
                    <px:PXGrid ID="grid" runat="server" DataSourceID="ds" Height="150px" Width="100%" AllowPaging="false" AllowSearch="true" 
                        SkinID="Details" SyncPosition="True" SyncPositionWithGraph="True" RepaintColumns="true" NoteIndicator="False" OnRowDataBound="JobGrid_RowDataBound">
                        <Levels>
                            <px:PXGridLevel DataMember="PackingJobs">
                                <Columns>
                                    <px:PXGridColumn DataField="SOPickingWorksheet__WorksheetType" RenderEditorText="True" />
                                    <px:PXGridColumn DataField="SOPickingJob__PickListNbr" LinkCommand="showPickList" />
                                    <px:PXGridColumn DataField="SOPickingJob__Status" RenderEditorText="True" />
                                    <px:PXGridColumn DataField="Priority" CommitChanges="True" />
                                    <px:PXGridColumn DataField="PreferredAssigneeID" CommitChanges="True" />
                                    <px:PXGridColumn DataField="SOPickingJob__ActualAssigneeID" />
                                    <px:PXGridColumn DataField="SOPickingWorksheet__PickDate" />
                                    <px:PXGridColumn DataField="SOPickingJob__TimeInQueue" />
                                    <px:PXGridColumn DataField="SOPicker__PathLength" />
                                    <px:PXGridColumn DataField="SOPickingJob__PickingStartedAt" DisplayFormat="g" />
                                    <px:PXGridColumn DataField="SOPickingJob__PickedAt" DisplayFormat="g" />
                                    <px:PXGridColumn DataField="AutomaticShipmentConfirmation" CommitChanges="True" Type="CheckBox" />
                                    <px:PXGridColumn DataField="SOShipment__ShipmentQty" TextAlign="Right" />
                                    <px:PXGridColumn DataField="SOShipment__ShipmentWeight" TextAlign="Right" />
                                    <px:PXGridColumn DataField="SOShipment__ShipmentVolume" TextAlign="Right" />
                                    <px:PXGridColumn DataField="SOShipment__ShipDate" />
                                    <px:PXGridColumn DataField="Customer__AcctCD" />
                                    <px:PXGridColumn DataField="Customer__AcctName" />
                                    <px:PXGridColumn DataField="Location__LocationCD" />
                                    <px:PXGridColumn DataField="Location__Descr" />
                                    <px:PXGridColumn DataField="Carrier__CarrierID" />
                                    <px:PXGridColumn DataField="Carrier__Description" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <Mode AllowFormEdit="False" AllowSort="False" />
                        <AutoSize Enabled="True" MinHeight="200" />
                        <ActionBar DefaultAction="showPickList">
                            <Actions>
                                <AddNew ToolBarVisible="False" />
                                <Delete ToolBarVisible="False" />
                            </Actions>
                        </ActionBar>
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
        </Items>
        <AutoSize Enabled="True" Container="Window" />
    </px:PXTab>
    <!--#include file="~\Pages\SO\Includes\ShowPickListPanel.inc"-->
</asp:Content>
