<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true"
ValidateRequest="false" CodeFile="AM401700.aspx.cs" Inherits="Page_AM401700"
Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" TypeName="PX.Objects.AM.AsBuiltConfigInq" PrimaryView="Filter"
                     Visible="True" Width="100%" BorderStyle="NotSet">
                <DataTrees>
            <px:PXTreeDataMember TreeView="Tree" TreeKeys="ParentID, MatlLine, Level"/>
                    </DataTrees>
        		<CallbackCommands>
                    <px:PXDSCallbackCommand Name="Cancel" Visible="True" />
		</CallbackCommands>
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" Runat="Server">
    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Width="100%" DataMember="Filter" Caption="Selection"> 
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True"  LabelsWidth="SM" ControlSize="XM" />
            <px:PXSelector CommitChanges="True" ID="edLotSerialNbr" runat="server" DataField="LotSerialNbr" AllowEdit="True" />
            <px:PXSelector CommitChanges="True" ID="edInventoryID" runat="server" DataField="InventoryID" AllowEdit="True" />
            <px:PXLayoutRule runat="server" StartColumn="True"  LabelsWidth="SM" ControlSize="M" />
            <px:PXSelector CommitChanges="True" ID="edSOOrderNbr" runat="server" DataField="OrdNbr" AllowEdit="True" />
            <px:PXSelector CommitChanges="True" ID="edProdOrdID" runat="server" DataField="ProdOrdID" AllowEdit="True" />
            <px:PXLayoutRule runat="server" StartColumn="True"  LabelsWidth="SM" ControlSize="XS" />
            <px:PXTextEdit CommitChanges="True" ID="edLevelsToDisplay" runat="server" DataField="LevelsToDisplay" AllowEdit="True" />
        </Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
<px:PXSplitContainer runat="server" ID="sp1" SplitterPosition="400">
    <AutoSize Enabled="true" Container="Window" />
        <Template1>
            <px:PXTreeView ID="edTree1" runat="server" DataSourceID="ds" PopulateOnDemand="True" RootNodeText="BOM" ExpandDepth="10" SelectFirstNode="true"
                ShowRootNode="false" Caption="Features" CaptionVisible="false" AllowCollapse="False" Height="100%" DataMember="Tree" >
                <DataBindings>
                    <px:PXTreeItemBinding DataMember="Tree" TextField="Label" ValueField="SelectedValue" ImageUrlField="Icon" ToolTipField="ToolTip" />
                </DataBindings>
                <AutoCallBack Command="Refresh" Target="form" ActiveBehavior="true">
                    <Behavior RepaintControlsIDs="form, gridMatl" RepaintControls="All" />
                </AutoCallBack>
                <AutoSize Enabled="True" />
            </px:PXTreeView>
        </Template1>
    <Template2>
                            <px:PXGrid ID="gridMatl" runat="server" DataSourceID="ds" Width="100%" SkinID="DetailsInTab" SyncPosition="True">
                                <Levels>
                                    <px:PXGridLevel DataMember="ProdLotSerialRecs">
                                        <RowTemplate>
                                            <px:PXLayoutRule ID="PXLayoutRule5" runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
                                            <px:PXSegmentMask ID="edInventoryID" runat="server" DataField="InventoryID" AllowEdit="True" />
                                            <px:PXTextEdit ID="edDescrMat" runat="server" DataField="Descr" MaxLength="60" />
                                            <px:PXTextEdit ID="edLotSerial" runat="server" DataField="LotSerialNbr"/>
                                            <px:PXSegmentMask ID="edParentInvtID" runat="server" DataField="ParentInventoryID" AllowEdit="True" />
                                            <px:PXTextEdit ID="edParentDescr" runat="server" DataField="ParentDescr" MaxLength="60" />
                                            <px:PXTextEdit ID="edParentLotSerial" runat="server" DataField="ParentLotSerialNbr"/>
                                            <px:PXNumberEdit ID="edQtyReq" runat="server" DataField="Qty" />
                                            <px:PXSelector ID="edUOM" runat="server" DataField="UOM" AutoRefresh="True" />                                         
                                        </RowTemplate>
                                        <Columns>                                            
                                            <px:PXGridColumn DataField="InventoryID" Width="130px" AutoCallBack="True" />                                            
                                            <px:PXGridColumn DataField="Descr" MaxLength="255" Width="200px" />
                                            <px:PXGridColumn DataField="LotSerialNbr" Width="150px" />
                                            <px:PXGridColumn DataField="ParentInventoryID" Width="130px" AutoCallBack="True" />                                            
                                            <px:PXGridColumn DataField="ParentDescr" MaxLength="255" Width="200px" />
                                            <px:PXGridColumn DataField="ParentLotSerialNbr" Width="180px" />
                                            <px:PXGridColumn DataField="Qty" TextAlign="Right" Width="108px" AutoCallBack="True" />                                                                       
                                            <px:PXGridColumn DataField="UOM" Width="81px" AutoCallBack="True" />
                                        </Columns>
                                    </px:PXGridLevel>
                                </Levels>
                                     <Parameters>
                                        <px:PXControlParam ControlID="edTree1" Name="selectedValue" PropertyName="SelectedValue" /> 
                                    </Parameters>
                                <AutoSize Container="Window" Enabled="True" MinHeight="200" />
                            </px:PXGrid>
            </Template2>
    </px:PXSplitContainer>
</asp:Content>
