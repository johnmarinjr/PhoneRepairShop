<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true"
	ValidateRequest="false" CodeFile="GL510000.aspx.cs" Inherits="Page_GL510000"
	Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="server">
	<px:PXDataSource ID="ds" Width="100%" runat="server" Visible="True" PrimaryView="Filter"
		TypeName="PX.GLAnomalyDetection.SendTransactionsProcess">
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="server">
	<px:PXFormView ID="form" runat="server" DataMember="Filter" Width="100%">
		<Template>
			<px:PXLayoutRule ID="PXLayoutRule1" runat="server" StartColumn="True" LabelsWidth="S" ControlSize="M" />
			<px:PXDropDown ID="edAction" runat="server" DataField="Action" CommitChanges="True"/>
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="server">
	<px:PXGrid ID="grid" runat="server" AllowSearch="true" Width="100%"
		SkinID="PrimaryInquire" AllowPaging="true" AdjustPageSize="Auto" FilesIndicator ="false" NoteIndicator ="false">
		<Levels>
			<px:PXGridLevel DataMember="Organizations">
				<RowTemplate>
					<px:PXSelector ID="edFromPeriodID" runat="server" DataField="FromPeriodID" CommitChanges="True"/>
					<px:PXSelector ID="edToPeriodID" runat="server" DataField="ToPeriodID" CommitChanges="True"/>
				</RowTemplate>
				<Columns>
					<px:PXGridColumn DataField="Selected" TextAlign="Center" AllowCheckAll="true" Type="CheckBox" />
					<px:PXGridColumn DataField="OrganizationCD"/>
					<px:PXGridColumn DataField="OrganizationName"/>
					<px:PXGridColumn DataField="FromPeriodID"/>
					<px:PXGridColumn DataField="ToPeriodID"/>
					<px:PXGridColumn DataField="FromAnalyzePeriodID" CommitChanges="True"/>
					<px:PXGridColumn DataField="ToAnalyzePeriodID" CommitChanges="True"/>
					<px:PXGridColumn DataField="ServiceStatus"/>
				</Columns>
			</px:PXGridLevel>
		</Levels>
		<AutoSize Container="Window" Enabled="True" MinHeight="400" />
	</px:PXGrid>
</asp:Content>
