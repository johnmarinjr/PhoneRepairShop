<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true"
    ValidateRequest="false" CodeFile="AU220013.aspx.cs" Inherits="Page_AU220013" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <script type="text/javascript">
        function commandResult(ds, context) {

            var grid = null;
            var row = null;
            if (context.command == "moveUpWidget" || context.command == "moveDownWidget")
                grid = px_all[grdWidgetsID]; //check aspx.cs for grdWidgetsID
            else if (context.command == "moveUpItem" || context.command == "moveDownItem")
                grid = px_all[grdItemsID]; //check aspx.cs for grdItemsID

            if (context.command == "moveDownItem" || context.command == "moveDownWidget")
                row = grid.activeRow.nextRow();
            else if (context.command == "moveUpItem" || context.command == "moveUpWidget")
                row = grid.activeRow.prevRow();

            if (row)
                row.activate();
        }

        function toolsButtonClickHandler(grid, e) {
            if (isCommandMoving(e.button.commandName))
                grid.activeRow.dataChanged = true;
        }

        function isCommandMoving(commandName) {
            return commandName == "moveUpWidget" || commandName == "moveDownWidget"
                || commandName == "moveUpItem" || commandName == "moveDownItem";
        }

    </script>
    <pxa:AUDataSource ID="ds" runat="server" Width="100%" TypeName="PX.Api.Mobile.Workspaces.MobileSitemapWorkspaceMaint" PrimaryView="Workspaces" Visible="true">
        <CallbackCommands>
            <px:PXDSCallbackCommand CommitChanges="True" Name="Save" />
            <px:PXDSCallbackCommand DependOnGrid="grdWidgets" Name="moveUpWidget" Visible="false" CommitChanges="True" RepaintControls="All" />
            <px:PXDSCallbackCommand DependOnGrid="grdWidgets" Name="moveDownWidget" Visible="false" CommitChanges="True" RepaintControls="All" />
            <px:PXDSCallbackCommand DependOnGrid="grdItems" Name="moveUpItem" Visible="false" CommitChanges="True" RepaintControls="All" />
            <px:PXDSCallbackCommand DependOnGrid="grdItems" Name="moveDownItem" Visible="false" CommitChanges="True" RepaintControls="All" />
            <px:PXDSCallbackCommand DependOnGrid="grdItems" Name="moveUpWorkspace" Visible="false" CommitChanges="True" RepaintControls="All" />
            <px:PXDSCallbackCommand DependOnGrid="grdItems" Name="moveDownWorkspace" Visible="false" CommitChanges="True" RepaintControls="All" />
        </CallbackCommands>
    </pxa:AUDataSource>
</asp:Content>
<asp:Content ID="cont" ContentPlaceHolderID="phF" runat="Server">
    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Width="100%" Visible="true" DataMember="Workspaces" AllowCollapse="false">
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
            <px:PXLayoutRule runat="server" Merge="True" />
            <px:PXSelector runat="server" DataField="Name" ID="edName" CommitChanges="True" />
            <px:PXCheckBox runat="server" DataField="IsActive" ID="chkIsActive" Checked="True" />
            <px:PXLayoutRule runat="server" />
            <px:PXTextEdit runat="server" DataField="DisplayName" ID="edDisplayName" AllowNull="False" />
            <px:PXDropDown runat="server" DataField="Icon" ID="edIcon" AllowNull="False" />
        </Template>
    </px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
    <px:PXTab ID="tab" runat="server" Height="300px" Style="z-index: 100" Width="100%">
        <Items>
            <px:PXTabItem Text="Screens">
                <Template>
                    <px:PXGrid ID="grdItems" runat="server" DataSourceID="ds" Height="150px" Style="z-index: 100"
                        Width="100%" AllowPaging="False" AdjustPageSize="Auto" AllowSearch="True" SkinID="DetailsInTab"
                        AutoAdjustColumns="True" MatrixMode="true" SyncPosition="true" KeepPosition="true">
                        <ClientEvents ToolsButtonClick="toolsButtonClickHandler" />
                        <ActionBar ActionsVisible="true">
                            <CustomItems>
                                <px:PXToolBarButton CommandSourceID="ds" CommandName="moveUpItem" Visible="true" Text="Row Up" Tooltip="Move Row Up" DependOnGrid="grdItems">
                                    <Images Normal="main@ArrowUp" />
                                </px:PXToolBarButton>
                                <px:PXToolBarButton CommandSourceID="ds" CommandName="moveDownItem" Visible="true" Text="Row Down" Tooltip="Move Row Down" DependOnGrid="grdItems">
                                    <Images Normal="main@ArrowDown" />
                                </px:PXToolBarButton>
                            </CustomItems>
                        </ActionBar>
                        <Levels>
                            <px:PXGridLevel DataMember="WorkspaceItems">
                                <Mode InitNewRow="True" />
                                <RowTemplate>
                                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
                                    <px:PXSelector runat="server" ID="edItemID" DataField="ItemID" CommitChanges="True" AutoRefresh="True" DisplayMode="Hint" />
                                </RowTemplate>
                                <Columns>
                                    <px:PXGridColumn AllowNull="False" DataField="IsActive" TextAlign="Center" Type="CheckBox" Width="60px" />
                                    <px:PXGridColumn DataField="ItemID" Width="150px" AutoCallBack="true" />
                                    <px:PXGridColumn DataField="DisplayName" Width="150px" AutoCallBack="true" />
                                    <px:PXGridColumn DataField="ItemType" Width="150px" AutoCallBack="true" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <CallbackCommands>
                            <Save PostData="Page" />
                        </CallbackCommands>
                        <AutoSize Enabled="True" MinHeight="150" />
                        <Mode AllowUpload="True" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Widgets">
                <Template>
                    <px:PXGrid ID="grdWidgets" runat="server" DataSourceID="ds" Height="150px" Style="z-index: 100"
                        Width="100%" AllowPaging="False" AdjustPageSize="Auto" AllowSearch="True" SkinID="DetailsInTab"
                        AutoAdjustColumns="True" MatrixMode="true" SyncPosition="true" KeepPosition="true">
                        <ClientEvents ToolsButtonClick="toolsButtonClickHandler" />
                        <ActionBar ActionsVisible="true">
                            <CustomItems>
                                <px:PXToolBarButton CommandSourceID="ds" CommandName="moveUpWidget" Visible="true" Text="Row Up" Tooltip="Move Row Up" DependOnGrid="grdWidgets">
                                    <Images Normal="main@ArrowUp" />
                                </px:PXToolBarButton>
                                <px:PXToolBarButton CommandSourceID="ds" CommandName="moveDownWidget" Visible="true" Text="Row Down" Tooltip="Move Row Down" DependOnGrid="grdWidgets">
                                    <Images Normal="main@ArrowDown" />
                                </px:PXToolBarButton>
                            </CustomItems>
                        </ActionBar>
                        <Levels>
                            <px:PXGridLevel DataMember="WorkspaceWidgets">
                                <Mode InitNewRow="True" />
                                <RowTemplate>
                                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
                                    <px:PXSelector runat="server" ID="edDashboardID" DataField="DashboardID" CommitChanges="True" AutoRefresh="True" />
                                    <px:PXDropDown runat="server" ID="edWidgetID" DataField="WidgetID" CommitChanges="True" AutoRefresh="True" />
                                </RowTemplate>
                                <Columns>
                                    <px:PXGridColumn AllowNull="False" DataField="IsActive" TextAlign="Center" Type="CheckBox" Width="60px" />
                                    <px:PXGridColumn DataField="DashboardID" Width="150px" AutoCallBack="true" />
                                    <px:PXGridColumn DataField="WidgetID" Width="150px" AutoCallBack="true" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <CallbackCommands>
                            <Save PostData="Page" />
                        </CallbackCommands>
                        <AutoSize Enabled="True" MinHeight="150" />
                        <Mode AllowUpload="True" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
        </Items>
        <AutoSize Container="Window" Enabled="True" MinHeight="250" MinWidth="300" />
    </px:PXTab>
</asp:Content>
