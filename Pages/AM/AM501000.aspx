<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="AM501000.aspx.cs" Inherits="Page_AM501000" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" Width="100%" runat="server" 
        TypeName="PX.Objects.AM.APSRoughCutProcess" PrimaryView="OrderList" Visible="True">
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="phF" Runat="server">
    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Width="100%" DataMember="Filter" LinkPage="" DefaultControlID="edReleaseOrders">
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True"  LabelsWidth="XS" ControlSize="SM" />
			<px:PXDropDown ID="edProcessAction" runat="server" DataField="ProcessAction" CommitChanges="true" />
			<px:PXLayoutRule runat="server" StartColumn="True"  LabelsWidth="M" ControlSize="M" />
            <px:PXCheckBox ID="edReleaseOrders" runat="server" DataField="ReleaseOrders" AlignLeft="True" />
			<px:PXLayoutRule runat="server" StartColumn="True"  LabelsWidth="M" ControlSize="M" />
			<px:PXCheckBox ID="edExcludePlanning" runat="server" DataField="ExcludePlanningOrders" AlignLeft="True" CommitChanges="true" />
			<px:PXLayoutRule runat="server" StartColumn="True"  LabelsWidth="M" ControlSize="M" />
			<px:PXCheckBox ID="edExcludeFirmOrders" runat="server" DataField="ExcludeFirmOrders" AlignLeft="True" CommitChanges="true" />
        </Template>
    </px:PXFormView>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phG" runat="Server">
    <px:PXGrid ID="grid" runat="server" Height="400px" Width="100%" Style="z-index: 100" AllowPaging="True" 
        AllowSearch="True" DataSourceID="ds" BatchUpdate="True" AdjustPageSize="Auto" SkinID="PrimaryInquire" Caption="Orders" SyncPosition="True" TabIndex="1100">
		<Levels>
			<px:PXGridLevel DataKeyNames="OrderType,ProdOrdID,SchdID" DataMember="OrderList">
			    <RowTemplate>
			        <px:PXCheckBox ID="edSelected" runat="server" DataField="Selected"/>
			        <px:PXNumberEdit ID="edQtytoProd" runat="server" DataField="QtytoProd"/>
			        <px:PXNumberEdit ID="edQtyRemaining" runat="server" DataField="QtyRemaining"/>
			        <px:PXTextEdit ID="edAMProdItemUOM" runat="server" DataField="AMProdItem__UOM"/>
			        <px:PXDropDown ID="edOrderType" runat="server" DataField="OrderType"/>
			        <px:PXSelector ID="edProdOrdID" runat="server" DataField="ProdOrdID" AllowEdit="True"/>
			        <px:PXSegmentMask ID="edInventoryID" runat="server" DataField="InventoryID" AllowEdit="True"/>
			        <px:PXTextEdit ID="edInventoryID_description" runat="server" DataField="InventoryID_description"/>
			        <px:PXNumberEdit ID="edSchPriority" runat="server" DataField="SchPriority"/>
			        <px:PXDateTimeEdit ID="edConstDate" runat="server" DataField="ConstDate"/>
			        <px:PXDateTimeEdit ID="edStartDate" runat="server" DataField="StartDate_Date"/>
					<px:PXDateTimeEdit ID="edStartTime" runat="server" DataField="StartTime_Time" DisplayFormat="t" EditFormat="t" TimeMode="True" />
					<px:PXDateTimeEdit ID="edEndDate" runat="server" DataField="EndDate_Date"/>
                    <px:PXDateTimeEdit ID="edEndTime" runat="server" DataField="EndTime_Time" DisplayFormat="t" EditFormat="t" TimeMode="True" />
			        <px:PXTextEdit ID="edSchdConst" runat="server" DataField="SchedulingMethod"/>
					<px:PXDropDown ID="edScheduleStatus" runat="server" DataField="ScheduleStatus" />
					<px:PXCheckBox ID="edFirmSchedule" runat="server" DataField="FirmSchedule"/>
			        <px:PXSegmentMask ID="edSiteID" runat="server" DataField="SiteID" AllowEdit="True"/>
			        <px:PXNumberEdit ID="edSchdID" runat="server" DataField="SchdID"/>
                    <px:PXDateTimeEdit ID="edAMProdItemProdDate" runat="server" DataField="AMProdItem__ProdDate"/>
                    <px:PXSegmentMask ID="edAMProdItemCustomerID" runat="server" DataField="AMProdItem__CustomerID" AllowEdit="True" />
                    <px:PXSelector ID="edAMProdItemOrdNbr" runat="server" DataField="AMProdItem__OrdNbr"/>
			        <px:PXTextEdit ID="edAMProdItemDescr" runat="server" DataField="AMProdItem__Descr"/>
					<px:PXSegmentMask ID="edBranchID" runat="server" DataField="AMProdItem__BranchID" />
                </RowTemplate>
			    <Columns>
                    <px:PXGridColumn DataField="Selected" AllowCheckAll="True" Width="30px" TextAlign="Center" Type="CheckBox" />
			        <px:PXGridColumn DataField="QtytoProd" TextAlign="Right" Width="100px"/>
			        <px:PXGridColumn DataField="QtyRemaining" TextAlign="Right" Width="100px"/>
			        <px:PXGridColumn DataField="AMProdItem__UOM" />
			        <px:PXGridColumn DataField="OrderType" Width="75px"/>
			        <px:PXGridColumn DataField="ProdOrdID" Width="110px"/>
			        <px:PXGridColumn DataField="InventoryID" Width="100px"/>
			        <px:PXGridColumn DataField="InventoryID_description" Width="200px"/>
			        <px:PXGridColumn DataField="SchPriority" TextAlign="Right"/>
			        <px:PXGridColumn DataField="ConstDate" Width="90px"/>
			        <px:PXGridColumn DataField="StartDate_Date" Width="90px"/>
			        <px:PXGridColumn DataField="EndDate_Date" Width="90px"/>
			        <px:PXGridColumn DataField="SchedulingMethod"/>
					<px:PXGridColumn DataField="ScheduleStatus"/>
					<px:PXGridColumn DataField="FirmSchedule" Width="60px" TextAlign="Center" Type="CheckBox"/>
			        <px:PXGridColumn DataField="SiteID" Width="120px"/>
			        <px:PXGridColumn DataField="SchdID" TextAlign="Right"/>
                    <px:PXGridColumn DataField="AMProdItem__ProdDate" />
                    <px:PXGridColumn DataField="AMProdItem__CustomerID" />
                    <px:PXGridColumn DataField="AMProdItem__OrdNbr" />
			        <px:PXGridColumn DataField="AMProdItem__Descr" />
			        <px:PXGridColumn DataField="AMProdItem__StatusID" />
					<px:PXGridColumn DataField="StartDate_Time" Width="90px"/>
					<px:PXGridColumn DataField="EndDate_Time" Width="90px"/>
					<px:PXGridColumn DataField="AMProdItem__BranchID" Width="125px" />
                </Columns>
			</px:PXGridLevel>
		</Levels>
		<ActionBar DefaultAction="ViewDocument"/>
        <AutoSize Container="Window" Enabled="True" MinHeight="200" />
	</px:PXGrid>
</asp:Content>
