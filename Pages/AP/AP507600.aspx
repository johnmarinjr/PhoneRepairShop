<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="AP507600.aspx.cs" Inherits="Page_AP507600" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" Runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%"
        TypeName="PX.Objects.Localizations.CA.T5018Fileprocessing"
        PrimaryView="MasterView">
		<CallbackCommands>

		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" Runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" DataMember="MasterView" Width="100%" Height="180px" SyncPosition="true">
		<Template>
			<px:PXLayoutRule ID="PXLayoutRule1" runat="server" StartRow="True"></px:PXLayoutRule>
			<px:PXSelector CommitChanges="True" runat="server" ID="CstPXSegmentMask9" DataField="OrganizationID" ></px:PXSelector>
			<px:PXDateTimeEdit CommitChanges="True" runat="server" ID="CstPXDateTimeEdit6a" DataField="FromDate" ></px:PXDateTimeEdit>
			<px:PXDateTimeEdit CommitChanges="True" runat="server" ID="CstPXDateTimeEdit19a" DataField="ToDate" ></px:PXDateTimeEdit>
            	<%--<px:PXSelector CommitChanges="True" runat="server" ID="PXFromPeriod1" DataField="FromPeriod" ></px:PXSelector>
			<px:PXSelector CommitChanges="True" runat="server" ID="PXToPeriod2" DataField="ToPeriod" ></px:PXSelector>--%>
			<px:PXTextEdit runat="server" ID="CstPXTextEdit1" DataField="AcctName" ></px:PXTextEdit>
			<px:PXDropDown CommitChanges="True" runat="server" ID="CstPXDropDown4" DataField="FilingType" ></px:PXDropDown>
			<px:PXTextEdit CommitChanges="True" runat="server" ID="CstPXTextEdit16" DataField="SubmissionNo" ></px:PXTextEdit>
			<px:PXLayoutRule runat="server" ID="CstPXLayoutRule20" StartColumn="True" ></px:PXLayoutRule>
			<px:PXTextEdit runat="server" ID="PXThersholdLimit1" DataField="ThersholdAmount" CommitChanges="true"></px:PXTextEdit>
			<px:PXTextEdit runat="server" ID="CstPXTextEdit17" DataField="TaxRegistrationID" ></px:PXTextEdit>
			<px:PXDropDown runat="server" ID="CstPXDropDown7" DataField="Language" ></px:PXDropDown>
			<px:PXDropDown runat="server" ID="CstPXDropDown18" DataField="Title" ></px:PXDropDown>
			<px:PXTextEdit runat="server" ID="CstPXTextEdit5" DataField="FirstName" ></px:PXTextEdit>
			<px:PXTextEdit runat="server" ID="CstPXTextEdit8" DataField="LastName" ></px:PXTextEdit>
			<px:PXLayoutRule runat="server" ID="CstPXLayoutRule21" StartColumn="True" ></px:PXLayoutRule>
			<px:PXTextEdit runat="server" ID="CstPXTextEdit3" DataField="EMail" ></px:PXTextEdit>
			<px:PXTextEdit runat="server" ID="CstPXTextEdit12" DataField="Phone1" ></px:PXTextEdit>
			<px:PXTextEdit runat="server" ID="CstPXTextEdit13" DataField="Phone2" ></px:PXTextEdit>
			<px:PXTextEdit runat="server" ID="CstPXTextEdit14" DataField="PostalCode" ></px:PXTextEdit>
			<px:PXTextEdit runat="server" ID="CstPXTextEdit15" DataField="SecteMail" ></px:PXTextEdit>
			<px:PXTextEdit runat="server" ID="CstPXTextEdit2" DataField="AreaCode" ></px:PXTextEdit>
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" Runat="Server">
	<px:PXGrid ID="grid" runat="server"  DataSourceID="ds" Width="100%" Height="150px" SkinID="Inquire" AllowPaging="true" AdjustPageSize="Auto">
		<Levels>
			<px:PXGridLevel DataMember="DetailsView">
			    <Columns>
				<px:PXGridColumn Type="CheckBox" AllowCheckAll="True" AllowMove="False" TextAlign="Center" DataField="Selected" Width="60" ></px:PXGridColumn>
				<px:PXGridColumn DataField="VAcctCD" Width="140"></px:PXGridColumn>
				<px:PXGridColumn DataField="VAcctName" Width="220"></px:PXGridColumn>
				<px:PXGridColumn DataField="PayerOrganizationID" Width="140"></px:PXGridColumn>
				<px:PXGridColumn DataField="CuryAdjdAmt" Width="100"></px:PXGridColumn>
				<px:PXGridColumn DataField="LTaxRegistrationID" Width="180"></px:PXGridColumn></Columns>
			</px:PXGridLevel>
		</Levels>
		<AutoSize Container="Window" Enabled="True" MinHeight="150" ></AutoSize>
		<ActionBar>
            <%--<Actions > <AddNew ToolBarVisible="False" /></Actions>--%>
		</ActionBar>
	</px:PXGrid>
</asp:Content>
