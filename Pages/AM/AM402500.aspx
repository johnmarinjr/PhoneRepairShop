<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true"
    ValidateRequest="false" CodeFile="AM402500.aspx.cs" Inherits="Page_AM402500" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/FormTab.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" Runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.AM.ProductionWhereUsedInq" PrimaryView="Filter" 
        BorderStyle="NotSet" >
		<CallbackCommands>
             
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" Runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" Width="100%" DataMember="Filter" DefaultControlID="edInventoryID" >
        <Template>
            <px:PXLayoutRule ID="PXLayoutRule1" runat="server" StartColumn="True"  LabelsWidth="S" ControlSize="M" />
            <px:PXSegmentMask ID="edInventoryID" runat="server" DataField="InventoryID" AllowEdit="True" CommitChanges="true" />
            <px:PXSelector ID="edLotSerialNbr" runat="server" DataField="LotSerialNbr" AutoRefresh="True" CommitChanges="true" />
            <px:PXLayoutRule ID="PXLayoutRule2" runat="server" StartColumn="True"  LabelsWidth="S" ControlSize="M" />
             <px:PXSegmentMask ID="edSiteID" runat="server" DataField="SiteID" />
             <px:PXSegmentMask ID="edLocationID" runat="server" DataField="LocationID" AutoRefresh="True" />
             <px:PXLayoutRule ID="PXLayoutRule3" runat="server" StartColumn="True" LabelsWidth="M" ControlSize="M" />
			<px:PXDropDown ID="edProductionStatusID" runat="server" DataField="ProductionStatusID" AutoRefresh="True" CommitChanges="true" />
            <px:PXCheckBox ID="edMultiLevel" runat="server"  DataField="MultiLevel" CommitChanges="True" />
		</Template>
    </px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" Runat="Server" >
	<px:PXGrid ID="grid" runat="server" DataSourceID="ds" Width="100%" SkinID="Inquire" SyncPosition="True" TabIndex="300" >
		<Levels>
			<px:PXGridLevel DataMember="ProductionWhereUsed" >
                <RowTemplate>
                    <px:PXSegmentMask ID="gridParentInventoryID" runat="server" DataField="ParentInventoryID" AllowEdit="True" />
					<px:PXTextEdit ID="gridParentDescr" runat="server" DataField="ParentDescr" />
                    <px:PXTextEdit ID="gridParentLotSerialNbr" runat="server" DataField="ParentLotSerialNbr" />
                    <px:PXSegmentMask ID="gridComponentInventoryID" runat="server" DataField="ComponentInventoryID" AllowEdit="True" />
                    <px:PXTextEdit ID="gridComponentDescr" runat="server" DataField="ComponentDescr" />
					<px:PXTextEdit ID="gridComponentLotSerialNbr" runat="server" DataField="ComponentLotSerialNbr" />
					<px:PXNumberEdit ID="gridLevel" runat="server" DataField="Level" />
					<px:PXSelector ID="gridOrderType" runat="server" DataField="OrderType" />
					<px:PXSelector ID="gridProdOrdID" runat="server" DataField="ProdOrdID" AllowEdit="True" />
					<px:PXTextEdit ID="gridOperationID" runat="server" DataField="OperationID" />
					<px:PXNumberEdit ID="gridQtyIssued" runat="server" DataField="QtyIssued" />
					<px:PXTextEdit ID="gridUOM" runat="server" DataField="UOM" />
					<px:PXSegmentMask ID="gridSiteID" runat="server" DataField="SiteID" DisplayMode="Hint" AllowEdit="True" />
					<px:PXSegmentMask ID="gridLocationID" runat="server" DataField="LocationID" />
					<px:PXTextEdit ID="gridSalesOrderType" runat="server" DataField="SalesOrderType" />
					<px:PXSelector ID="gridSalesOrderNbr" runat="server" DataField="SalesOrderNbr" AllowEdit="True" />
					<px:PXSegmentMask ID="gridInventoryID" runat="server" DataField="InventoryID" AllowEdit="True" />
					<px:PXTextEdit ID="gridDescr" runat="server" DataField="Descr" />
					<px:PXTextEdit ID="gridLotSerialNbr" runat="server" DataField="LotSerialNbr" />
                    <px:PXTextEdit ID="gridProductionStatusID" runat="server" DataField="ProductionStatusID" />
                    <px:PXSelector ID="gridComponentOrderType" runat="server" DataField="ComponentOrderType" />
					<px:PXSelector ID="gridComponentProdOrdID" runat="server" DataField="ComponentProdOrdID" AllowEdit="True" />
					<px:PXSegmentMask ID="gridCustomerID" runat="server" DataField="CustomerID" />
					<px:PXTextEdit ID="gridCustomerName" runat="server" DataField="CustomerName" />
					<px:PXTextEdit ID="gridScheduleStatus" runat="server" DataField="ScheduleStatus" />
					<px:PXDateTimeEdit ID="gridProdDate" runat="server" DataField="ProdDate" />
					<px:PXDateTimeEdit ID="gridConstDate" runat="server" DataField="ConstDate" />
					<px:PXDateTimeEdit ID="gridStartDate" runat="server" DataField="StartDate" />
					<px:PXDateTimeEdit ID="gridEndDate" runat="server" DataField="EndDate" />
                    <px:PXNumberEdit ID="gridParentQty" runat="server" DataField="ParentQty" />
					<px:PXNumberEdit ID="gridParentQtyComplete" runat="server" DataField="ParentQtyComplete" />
					<px:PXNumberEdit ID="gridParentQtyScrapped" runat="server" DataField="ParentQtyScrapped" />
					<px:PXNumberEdit ID="gridParentQtyRemaining" runat="server" DataField="ParentQtyRemaining" />
					<px:PXTextEdit ID="gridParentUOM" runat="server" DataField="ParentUOM" />
				</RowTemplate>
                <Columns>
                    <px:PXGridColumn DataField="ParentInventoryID" Width="130px" />
                    <px:PXGridColumn DataField="ParentDescr" Width="130px" />
                    <px:PXGridColumn DataField="ParentLotSerialNbr" Width="100px" />
                    <px:PXGridColumn DataField="ComponentInventoryID" Width="130px" />
                    <px:PXGridColumn DataField="ComponentDescr" Width="130px" />
                    <px:PXGridColumn DataField="ComponentLotSerialNbr" Width="130px" />
                    <px:PXGridColumn DataField="Level" Width="50px" />
                    <px:PXGridColumn DataField="OrderType" Width="70px" />
                    <px:PXGridColumn DataField="ProdOrdID" Width="90px"  />
					<px:PXGridColumn DataField="OperationID" Width="70px" />
                    <px:PXGridColumn DataField="QtyIssued" Width="120px" />
                    <px:PXGridColumn DataField="UOM" Width="90px" /> 
                    <px:PXGridColumn DataField="SiteID" Width="130px" />
                    <px:PXGridColumn DataField="LocationID" Width="130px" />
                    <px:PXGridColumn DataField="SalesOrderType" Width="70px" />
                    <px:PXGridColumn DataField="SalesOrderNbr" Width="130px" />
					<px:PXGridColumn DataField="InventoryID" Width="130px"/>
                    <px:PXGridColumn DataField="Descr" Width="130px" />
                    <px:PXGridColumn DataField="LotSerialNbr" Width="100px" />
                    <px:PXGridColumn DataField="ProductionStatusID" Width="100px" />
					<px:PXGridColumn DataField="ComponentOrderType" Width="70px" />
                    <px:PXGridColumn DataField="ComponentProdOrdID" Width="100px"  /> 
                    <px:PXGridColumn DataField="CustomerID" Width="70px" />
                    <px:PXGridColumn DataField="CustomerName" TextAlign="Left" Width="110px" />
					<px:PXGridColumn DataField="ScheduleStatus" TextAlign="Center" Type="CheckBox" Width="85px" />
                    <px:PXGridColumn DataField="ProdDate" Width="90px" />
                    <px:PXGridColumn DataField="ConstDate" Width="90px" />
                    <px:PXGridColumn DataField="StartDate" Width="90px" />
                    <px:PXGridColumn DataField="EndDate" Width="90px" />
					<px:PXGridColumn DataField="ParentQty" Width="130px" />
                    <px:PXGridColumn DataField="ParentQtyComplete" Width="130px" />
                    <px:PXGridColumn DataField="ParentQtyScrapped" Width="130px" />
                    <px:PXGridColumn DataField="ParentQtyRemaining" Width="130px" />
					<px:PXGridColumn DataField="UOM" Width="90px" />
                </Columns>
			</px:PXGridLevel>
		</Levels>
		<AutoSize Container="Window" Enabled="True" MinHeight="150" />
		<ActionBar ActionsText="True">
		</ActionBar>
	</px:PXGrid>
</asp:Content>
