<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPages/ListView.master"
	ValidateRequest="false" Title="Untitled Page"
	CodeFile="BC601000.aspx.cs" Inherits="Page_BC601000" %>

<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" PrimaryView="Filter" TypeName="PX.Commerce.Objects.ProcessPII">
		<CallbackCommands>
			<px:PXDSCallbackCommand Name="Process" StartNewGroup="True" />
			<px:PXDSCallbackCommand Name="ProcessAll" />
			<px:PXDSCallbackCommand Name="OpenEntity" CommitChanges="True" Visible="False"/>
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">

	<px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100" Caption="Operation" Width="100%" DataMember="Filter">
		<Template>
			<px:PXLayoutRule ID="PXLayoutRule1" runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
			<px:PXDropDown ID="edAction" runat="server" AllowNull="False" DataField="Action" CommitChanges="True" />
			<px:PXDropDown ID="edMasterEntity" runat="server" AllowNull="False" DataField="MasterEntity" CommitChanges="True" />
			<px:PXLayoutRule runat="server" Merge="True" />
			<px:PXTextEdit ID="txtDocumentDateWithinXDays"  Size="xxs" runat="server" AlignLeft="true" DataField="DocumentDateWithinXDays" CommitChanges="True"/>
			<px:PXTextEdit SuppressLabel="True" ID="edDays1"  DataField="Days" runat="server" SkinID="Label" Enabled="False" />
		</Template>
	</px:PXFormView>

	<px:PXGrid ID="grid" runat="server" Height="400px" Width="100%" Style="z-index: 100"
		AllowPaging="True" AllowSearch="true" AdjustPageSize="Auto" DataSourceID="ds"
		SkinID="PrimaryInquire" SyncPosition="True" AutoAdjustColumns="True" ActionsPosition="Top">
		<Levels>
			<px:PXGridLevel DataMember="SelectedItems">
				<Columns>
					<px:PXGridColumn AllowCheckAll="True" AllowShowHide="False" DataField="Selected"
						TextAlign="Center" Type="CheckBox" Width="40px" AutoCallBack="True" />
					<px:PXGridColumn DataField="Type" Width="100px"  />
					<px:PXGridColumn DataField="Reference" Width="150px" LinkCommand="OpenEntity" />
					<px:PXGridColumn DataField="MasterEntityType" Width="250px" />
					<px:PXGridColumn DataField="Pseudonymized" Width="150px" />
					<px:PXGridColumn DataField="ShipToCompanyName" Width="150px" />
					<px:PXGridColumn DataField="ShipToAttention" Width="150px" />
					<px:PXGridColumn DataField="ShipToEmail" Width="150px" />
					<px:PXGridColumn DataField="ShipToPhone1" Width="150px"   />
					<px:PXGridColumn DataField="ShipToAddressLine1" Width="150px" />
					<px:PXGridColumn DataField="ShipToAddressLine2" Width="150px" />
					<px:PXGridColumn DataField="ShipToCity" Width="150px"  />
					<px:PXGridColumn DataField="ShipToState" Width="150px"  />
					<px:PXGridColumn DataField="ShipToCountry" Width="150px" />
					<px:PXGridColumn DataField="ShipToPostalCode" Width="150px"  />

					<px:PXGridColumn DataField="BillToCompanyName" Width="150px" />
					<px:PXGridColumn DataField="BillToAttention" Width="150px" />
					<px:PXGridColumn DataField="BillToEmail" Width="150px" />
					<px:PXGridColumn DataField="BillToPhone1" Width="150px"   />
					<px:PXGridColumn DataField="BillToAddressLine1" Width="150px" />
					<px:PXGridColumn DataField="BillToAddressLine2" Width="150px" />
					<px:PXGridColumn DataField="BillToCity" Width="150px"  />
					<px:PXGridColumn DataField="BillToState" Width="150px"  />
					<px:PXGridColumn DataField="BillToCountry" Width="150px" />
					<px:PXGridColumn DataField="BillToPostalCode" Width="150px"  />
				</Columns>
			</px:PXGridLevel>
		</Levels>
		<AutoSize Container="Window" Enabled="True" MinHeight="200" />
		<Mode AllowAddNew="False" AllowDelete="False" AllowUpdate="False" />
	</px:PXGrid>



</asp:Content>
