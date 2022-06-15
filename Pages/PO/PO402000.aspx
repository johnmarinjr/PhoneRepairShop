<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="PO402000.aspx.cs" Inherits="Page_PO402000" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="true" TypeName="PX.Objects.PO.POAccrualInquiry" PrimaryView="Filter" PageLoadBehavior="PopulateSavedValues">
		<CallbackCommands>
			<px:PXDSCallbackCommand Name="Cancel" CommitChanges="true" />
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" DataMember="Filter" 
		Caption="Selection" DefaultControlID="edCustomerID" MarkRequired="Dynamic" SyncPosition="False">
		<Template>
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
			<px:PXBranchSelector ID="edOrgBAccountID" runat="server" DataField="OrgBAccountID" CommitChanges="True" InitialExpandLevel="0" />
			<px:PXSegmentMask ID="edVendorID" runat="server" DataField="VendorID" CommitChanges="True" AutoRefresh="true" />
			<px:PXSelector ID="edPeriod" runat="server" DataField="FinPeriodID" AutoRefresh="true" CommitChanges="True" />
			<px:PXSegmentMask ID="edAcctID" runat="server" DataField="AcctID" CommitChanges="True"/>
			<px:PXSegmentMask ID="edSubCD" runat="server" DataField="SubCD" SelectMode="Segment" CommitChanges="True"/>
			<px:PXCheckBox ID="chkShowLines" runat="server" DataField="ShowByLines" CommitChanges="True" LabelWidth="m" />

			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="XM" ControlSize="S" />
			<px:PXNumberEdit ID="edUnbilledAmt" runat="server" DataField="UnbilledAmt" />
			<px:PXNumberEdit ID="edNotReceivedAmt" runat="server" DataField="NotReceivedAmt" />
			<px:PXNumberEdit ID="edNotInvoicedAmt" runat="server" DataField="NotInvoicedAmt" />
			<px:PXNumberEdit ID="edNotAdjustedAmt" runat="server" DataField="NotAdjustedAmt" />
			<px:PXNumberEdit ID="edBalance" runat="server" DataField="Balance" />
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXGrid ID="grid" runat="server" DataSourceID="ds" Height="153px" Style="z-index: 100" Width="100%" Caption="Documents" 
		AllowSearch="True" AdjustPageSize="Auto" SkinID="PrimaryInquire" AllowPaging="True" 
		TabIndex="1300" RestrictFields="True" SyncPosition="true" NoteIndicator="true" FilesIndicator="true" 
		FastFilterFields="OrderNbr,DocumentNbr,VendorID,InventoryID">
		<ActionBar DefaultAction="ViewDocument"/>
		<Levels>
			<px:PXGridLevel DataMember="ResultRecords">
				<RowTemplate>
					<px:PXSelector ID="edOrderNbr" runat="server" DataField="OrderNbr" AllowEdit="True" />
					<px:PXSelector ID="edINRefNbr" runat="server" DataField="INRefNbr" AllowEdit="True" />
					<px:PXSelector ID="edPPVAdjRefNbr" runat="server" DataField="PPVAdjRefNbr" AllowEdit="True" />
					<px:PXSelector ID="edTaxAdjRefNbr" runat="server" DataField="TaxAdjRefNbr" AllowEdit="True" />
					<px:PXSegmentMask ID="edInventoryID" runat="server" DataField="InventoryID" AllowEdit="True" />
				</RowTemplate>
				<Columns>
					<px:PXGridColumn DataField="OrderType" Type="DropDownList" />
					<px:PXGridColumn DataField="OrderNbr" />
					
					<px:PXGridColumn DataField="DocumentType" Type="DropDownList" />
					<px:PXGridColumn DataField="DocumentNbr" LinkCommand="ViewDocument" />
					<px:PXGridColumn DataField="LineNbr" />
					
					<px:PXGridColumn DataField="DocDate" />
					<px:PXGridColumn DataField="VendorID" />
					<px:PXGridColumn DataField="VendorName" />
					<px:PXGridColumn DataField="INDocType" Type="DropDownList" />
					<px:PXGridColumn DataField="INRefNbr" />
					<px:PXGridColumn DataField="FinPeriodID" />
					<px:PXGridColumn DataField="PPVAdjRefNbr" />
					<px:PXGridColumn DataField="TaxAdjRefNbr" />
					<px:PXGridColumn DataField="BranchID" />
					
					<px:PXGridColumn DataField="SiteID" AllowShowHide="Server" />
					<px:PXGridColumn DataField="InventoryID" AllowShowHide="Server" />
					<px:PXGridColumn DataField="TranDesc" AllowShowHide="Server" />
					
					<px:PXGridColumn DataField="AcctID" />
					<px:PXGridColumn DataField="SubID" />

					<px:PXGridColumn DataField="UnbilledAmt" />
					<px:PXGridColumn DataField="NotReceivedAmt" />
					<px:PXGridColumn DataField="NotInvoicedAmt" />
					<px:PXGridColumn DataField="NotAdjustedAmt" />
					<px:PXGridColumn DataField="AccrualAmt" />
				</Columns>
			</px:PXGridLevel>
		</Levels>
		<ActionBar DefaultAction="ViewDocument">
			<Actions>
				<Refresh ToolBarVisible="False"/>
			</Actions>
		</ActionBar>
		<AutoSize Container="Window" Enabled="True" MinHeight="150" />
	</px:PXGrid>
</asp:Content>
