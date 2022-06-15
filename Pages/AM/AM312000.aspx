<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true"
ValidateRequest="false" CodeFile="AM312000.aspx.cs" Inherits="Page_AM312000" Title="Late Assignment" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" TypeName="PX.Objects.AM.LateAssignmentMaint" PrimaryView="ProdItemSplits" 
        Visible="True" BorderStyle="NotSet" HeaderDescriptionField="Descr">
        <CallbackCommands>
            <px:PXDSCallbackCommand Visible="false" Name="Allocate" CommitChanges="True" DependOnGrid="gridUnassigned" />
            <px:PXDSCallbackCommand Visible="false" Name="Unallocate" CommitChanges="True" DependOnGrid="gridAssigned" />
        </CallbackCommands>
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Width="100%" DefaultControlID="edOrderType" Caption="Late Assignment" DataMember="ProdItemSplits" >
        <Template>
            <px:PXLayoutRule runat="server" LabelsWidth="MS" ControlSize="M" />
            <px:PXSelector CommitChanges="True" ID="edOrderType" runat="server" DataField="OrderType" AllowEdit="True" DataSourceID="ds" />
            <px:PXSelector ID="edProdOrdID" runat="server" DataField="ProdOrdID" AutoRefresh="True" CommitChanges="True" AllowEdit="true" DataSourceID="ds" />
            <px:PXSelector ID="edLotSerialNbr" runat="server" DataField="LotSerialNbr" AutoRefresh="True" DataSourceID="ds" CommitChanges="true"/>
			<px:PXLayoutRule runat="server" StartColumn="True"  LabelsWidth="S" ControlSize="M" />
            <px:PXDropdown ID="edStatusID" runat="server" DataField="StatusID" />
            <px:PXSegmentMask ID="edInventoryID" runat="server" DataField="InventoryID" />
            <px:PXSegmentMask ID="edSiteID" runat="server" DataField="SiteId" />
            <px:PXLayoutRule runat="server" StartColumn="True"  LabelsWidth="S" ControlSize="M" />
            <px:PXNumberEdit ID ="edQty" runat="server" DataField="Qty" />
            <px:PXNumberEdit ID ="edQtyComplete" runat="server" DataField="QtyComplete" />
            <px:PXNumberEdit ID ="edQtyScrapped" runat="server" DataField="QtyScrapped" />
            <px:PXNumberEdit ID ="edQtyRemaining" runat="server" DataField="QtyRemaining" />
        </Template>
    </px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
    <px:PXSplitContainer ID="SptCont1" runat="server" SkinID="Horizontal" SplitterPosition="300" Height="650px" Panel1MinSize="100" Panel2MinSize="100">
        <AutoSize Container="Window" Enabled="true" MinHeight="300"/>
        <Template1>
            <px:PXGrid ID="gridAssigned" runat="server" DataSourceID="ds" Width="100%" Height="100%" SkinID="Inquire" Caption="Allocated Components" SyncPosition="true" CaptionVisible="true"
                 AllowSearch="true" RepaintColumns="true" >
                <Levels>
                    <px:PXGridLevel DataMember="MatlAssigned" DataKeyNames="OrderType, ProdOrdID, OperationID, LineID, LotSerNbr, ParentLotSerialNbr">
                        <RowTemplate>
                            <px:PXSegmentMask ID="gaInventoryID" runat="server" DataField="InventoryID" AllowEdit="True" />
                            <px:PXTextEdit ID="gaDescr" runat="server" DataField="Descr" />
                            <px:PXTextEdit ID="gnLotSerialNbr" runat="server" DataField="LotSerialNbr" />
                            <px:PXNumberEdit ID ="gaQtyIssued" runat="server" DataField="QtyIssued" />
                            <px:PXTextEdit ID="gaBaseUnit" runat="server" DataField="BaseUnit" />
                        </RowTemplate>
                        <Columns>
                            <px:PXGridColumn DataField="InventoryID" Width="130px" />
                            <px:PXGridColumn DataField="Descr" MaxLength="60" Width="150px" />
                            <px:PXGridColumn DataField="LotSerialNbr" Width="130px" />
                            <px:PXGridColumn DataField="QtyIssued" MaxLength="60" Width="120px" />
                            <px:PXGridColumn DataField="BaseUnit" MaxLength="60" Width="90px" />
                        </Columns>
                    </px:PXGridLevel>
                </Levels>
                <AutoSize Enabled="True"/>
                <ActionBar ActionsText="False">
                    <CustomItems>
                        <px:PXToolBarButton Text="Unallocate" >
                            <AutoCallBack Command="Unallocate" Target="ds" />
                        </px:PXToolBarButton>
                    </CustomItems>
                </ActionBar>
            </px:PXGrid>
        </Template1>
        <Template2>
            <px:PXGrid ID="gridUnassigned" runat="server" DataSourceID="ds" Width="100%" Height="100%" SkinID="Inquire" SyncPosition="True"
                TabIndex="2600" Caption="Unallocated Components" RepaintColumns="true" CaptionVisible="true"
                AllowSearch="true" >
                <Levels>
                    <px:PXGridLevel DataMember="MatlUnassigned" DataKeyNames="OrderType, ProdOrdID, OperationID, LineID, LotSerNbr, ParentLotSerialNbr" >
                        <RowTemplate>
                            <px:PXSegmentMask ID="guInventoryID" runat="server" DataField="InventoryID" AllowEdit="True" AutoRefresh="true"/>
                            <px:PXTextEdit ID="guDescr" runat="server" DataField="Descr" AutoRefresh="true"/>
                            <px:PXTextEdit ID="guLotSerialNbr" runat="server" DataField="LotSerialNbr" AutoRefresh="true"/>
                            <px:PXNumberEdit ID ="guQtyIssued" runat="server" DataField="QtyIssued" AutoRefresh="true" />
                            <px:PXTextEdit ID="guBaseUnit" runat="server" DataField="BaseUnit" AutoRefresh="true"/>
                            <px:PXNumberEdit ID ="guQtyRequired" runat="server" DataField="QtyRequired" AutoRefresh="true"/>
                            <px:PXNumberEdit ID ="guQtytoAllocate" runat="server" DataField="QtyToAllocate" AutoRefresh="true" CommitChanges="true"/>
                        </RowTemplate>
                        <Columns>
                            <px:PXGridColumn DataField="InventoryID" Width="130px" />
                            <px:PXGridColumn DataField="Descr" MaxLength="60" Width="150px" />
                            <px:PXGridColumn DataField="LotSerialNbr" Width="130px" />
                            <px:PXGridColumn DataField="QtyIssued" MaxLength="60" Width="120px" />
                            <px:PXGridColumn DataField="BaseUnit" MaxLength="60" Width="90px" />
                            <px:PXGridColumn DataField="QtyRequired" MaxLength="60" Width="120px" />
                            <px:PXGridColumn DataField="QtyToAllocate" MaxLength="60" Width="120px" />
                        </Columns>
                    </px:PXGridLevel>
                </Levels>
                <ActionBar ActionsText="False">
                    <CustomItems>
                        <px:PXToolBarButton Text="Allocate" >
                            <AutoCallBack Command="Allocate" Target="ds" />
                        </px:PXToolBarButton>
                    </CustomItems>
                </ActionBar>
                <AutoSize Enabled="True"/> 
            </px:PXGrid>
        </Template2>
    </px:PXSplitContainer>
</asp:Content>
