<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="AR501000.aspx.cs" Inherits="Page_AR501000" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" Width="100%" runat="server" Visible="True" PrimaryView="ARDocumentList" TypeName="PX.Objects.AR.ARDocumentRelease">
		<CallbackCommands>
			<px:PXDSCallbackCommand Name="viewDocument" Visible="false" />
            <px:PXDSCallbackCommand Visible="False" Name="ARDocumentList_refNbr_ViewDetails" />
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">
	<px:PXGrid ID="grid" runat="server" Height="400px" Width="100%" Style="z-index: 100" AllowPaging="true" AdjustPageSize="Auto" AllowSearch="true" DataSourceID="ds" BatchUpdate="True" SkinID="PrimaryInquire" Caption="AR Documents"  
        FastFilterFields="RefNbr, CustomerID, CustomerID_BAccountR_acctName" SyncPosition="true">
		<Levels>
			<px:PXGridLevel DataMember="ARDocumentList">
				<Columns>
					<px:PXGridColumn DataField="Selected" TextAlign="Center" Type="CheckBox" AllowCheckAll="True" AllowSort="False" AllowMove="False" />
					<px:PXGridColumn DataField="DocType" Type="DropDownList" />
					<px:PXGridColumn DataField="RefNbr" />
					<px:PXGridColumn DataField="PaymentMethodID" />
					<px:PXGridColumn DataField="CustomerID" />
					<px:PXGridColumn DataField="CustomerID_BAccountR_acctName" />
					<px:PXGridColumn DataField="CustomerRefNbr" />
					<px:PXGridColumn DataField="Status" Type="DropDownList" />
					<px:PXGridColumn DataField="DocDate" />
					<px:PXGridColumn DataField="FinPeriodID" />
					<px:PXGridColumn DataField="CuryOrigDocAmt" TextAlign="Right" />
					<px:PXGridColumn DataField="CuryID" />
					<px:PXGridColumn DataField="OrigDocAmt" TextAlign="Right" />
					<px:PXGridColumn DataField="DocDesc" />
				</Columns>
			</px:PXGridLevel>
		</Levels>
		<AutoSize Container="Window" Enabled="True" MinHeight="400" />
		<Layout ShowRowStatus="False" />
	</px:PXGrid>
</asp:Content>
