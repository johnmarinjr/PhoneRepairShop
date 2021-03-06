<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true"
	ValidateRequest="false" CodeFile="PR204000.aspx.cs" Inherits="Page_PR204000" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormTab.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.PR.PTOBankMaint" PrimaryView="Bank">
		<CallbackCommands>
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" DataMember="Bank">
		<Template>
			<px:PXLayoutRule runat="server" StartRow="True" LabelsWidth="SM" ControlSize="SM" />
			<px:PXSelector runat="server" ID="edBankID" DataField="BankID" CommitChanges="true" />
			<px:PXDropDown runat="server" ID="edAccrualMethod" DataField="AccrualMethod" CommitChanges="true" />
			<px:PXNumberEdit runat="server" ID="edAccrualRate" DataField="AccrualRate" />
			<px:PXNumberEdit runat="server" ID="edHoursPerYear" DataField="HoursPerYear" />
			<px:PXNumberEdit runat="server" ID="edAccrualLimit" DataField="AccrualLimit" />
			<px:PXSelector runat="server" ID="edEarningTypeCD" DataField="EarningTypeCD" />

			<px:PXLayoutRule runat="server" StartColumn="true" LabelsWidth="0px" ControlSize="SM" />
			<px:PXCheckBox runat="server" ID="edIsActive" DataField="IsActive" />
			<px:PXCheckBox runat="server" ID="edIsCertifiedJobAccrual" DataField="IsCertifiedJobAccrual" />
			<px:PXCheckBox runat="server" ID="edAllowNegativeBalance" DataField="AllowNegativeBalance" CommitChanges="true" />
			<px:PXCheckBox runat="server" ID="edDisburseFromCarryover" DataField="DisburseFromCarryover" CommitChanges="true" />
			<px:PXCheckBox runat="server" ID="edCreateFinancialTransaction" DataField="CreateFinancialTransaction" CommitChanges="true" />

			<px:PXLayoutRule runat="server" StartRow="true" LabelsWidth="SM" ColumnSpan="2" />
			<px:PXTextEdit runat="server" ID="edDescription" DataField="Description" />

			<px:PXLayoutRule runat="server" StartRow="true" LabelsWidth="SM" ControlSize="S" Merge="true" />
			<px:PXDateTimeEdit runat="server" ID="edStartDateError" DataField="StartDate" Enabled="false" LabelWidth="134px" Width="0" />
			<px:PXDropDown runat="server" ID="edStartDateMonth" DataField="Filter.StartDateMonth" CommitChanges="true" SuppressLabel="true" />
			<px:PXNumberEdit runat="server" ID="edStartDateDay" DataField="Filter.StartDateDay" CommitChanges="true" SuppressLabel="true" Width="35" />
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="server">
	<px:PXTab ID="tab" runat="server" Width="100%" Height="150px" DataSourceID="ds" DataMember="CurrentBank">
		<Items>
			<px:PXTabItem Text="General Settings">
				<Template>
					<px:PXLayoutRule runat="server" StartRow="True" GroupCaption="Carryover Rules" LabelsWidth="M" ControlSize="SM" />
					<px:PXDropDown runat="server" ID="edCarryoverType" DataField="CarryoverType" CommitChanges="true" />
					<px:PXNumberEdit runat="server" ID="edCarryoverAmount" DataField="CarryoverAmount" CommitChanges="true" />
					<px:PXNumberEdit runat="server" ID="edCarryoverPayMonthLimit" DataField="CarryoverPayMonthLimit" CommitChanges="true" />

					<px:PXLayoutRule runat="server" GroupCaption="Disbursing Rules" />
					<px:PXDropDown runat="server" ID="edDisbursingType" DataField="DisbursingType" />

					<px:PXLayoutRule runat="server" StartColumn="true" GroupCaption="Front Loading Rules" LabelsWidth="SM" ControlSize="SM" />
					<px:PXNumberEdit runat="server" ID="edFrontLoadingAmount" DataField="FrontLoadingAmount" CommitChanges="true" />

					<px:PXLayoutRule runat="server" GroupCaption="Settlement Rules" LabelsWidth="SM" ControlSize="SM" />
					<px:PXDropDown ID="edSettlementBalanceType" DataField="SettlementBalanceType" CommitChanges="true" runat="server" />
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="GL Accounts" RepaintOnDemand="false">
				<Template>
					<px:PXLayoutRule ID="PXLayoutRule1" runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
					<px:PXSelector ID="edPTOExpenseActID" runat="server" DataField="PTOExpenseAcctID" />
					<px:PXSegmentMask ID="edPTOExpenseID" runat="server" DataField="PTOExpenseSubID" />
					<px:PXSelector ID="edPTOLiabilityAcctID" runat="server" DataField="PTOLiabilityAcctID" />
					<px:PXSegmentMask ID="edPTOLiabilitySubID" runat="server" DataField="PTOLiabilitySubID" />
					<px:PXSelector ID="edPTOAssetAcctID" runat="server" DataField="PTOAssetAcctID" />
					<px:PXSegmentMask ID="edPTOAssetSubID" runat="server" DataField="PTOAssetSubID" />
				</Template>
			</px:PXTabItem>
		</Items>
		<AutoSize Container="Window" Enabled="True" MinHeight="150" />
	</px:PXTab>
</asp:Content>
