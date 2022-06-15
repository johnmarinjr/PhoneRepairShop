<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true"
	ValidateRequest="false" CodeFile="PM103000.aspx.cs" Inherits="Page_PM103000"
	Title="Project Transaction Visibility by Account Group" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.PM.PMAccountGroupAccess" PrimaryView="Group">
		<CallbackCommands>
			<px:PXDSCallbackCommand CommitChanges="True" Name="Save" />
			<px:PXDSCallbackCommand Name="Delete" Visible="false" />
            <px:PXDSCallbackCommand Name="CopyPaste" Visible="false" />
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView ID="formGroup" runat="server" Width="100%" DataMember="Group" Caption="Restriction Group" DefaultControlID="edGroupName" >
		<Template>
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
		    <px:PXSelector ID="edGroupName" runat="server" DataField="GroupName" />
		    <px:PXTextEdit ID="edDescription" runat="server" DataField="Description" />
		    <px:PXDropDown ID="edGroupType" runat="server" DataField="GroupType" />
		    <px:PXCheckBox ID="chkActive" runat="server" DataField="Active" />
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXTab ID="tab" runat="server" Height="168%" Width="100%" SelectedIndex="1">
		<Items>
			<px:PXTabItem Text="Users">
				<Template>
					<px:PXGrid ID="gridUsers" BorderWidth="0px" runat="server" Height="150px" Width="100%"
						AdjustPageSize="Auto" AllowSearch="True" SkinID="DetailsInTab" DataSourceID="ds" 
						FastFilterFields="FullName,Username">
						<Levels>
							<px:PXGridLevel DataMember="Users">
								<Mode AllowAddNew="True" AllowDelete="False" />
								<RowTemplate>
									<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M"  />
								    <px:PXCheckBox ID="chkIncluded" runat="server" DataField="Included" />
								    <px:PXSelector ID="edUsername" runat="server" DataField="Username" TextField="Username" />
								    <px:PXTextEdit ID="FullName" runat="server" DataField="FullName" />
								    <px:PXTextEdit ID="edComment" runat="server" DataField="Comment" />
								</RowTemplate>
								<Columns>
								    <px:PXGridColumn DataField="Included" TextAlign="Center" Type="CheckBox" AllowCheckAll="True" />
								    <px:PXGridColumn DataField="Username" />
								    <px:PXGridColumn DataField="FullName" />
								    <px:PXGridColumn DataField="Comment" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<AutoSize Enabled="True" />
		                <Mode AllowAddNew="False" AllowDelete="False" />
		                <ActionBar>
			                <Actions>
				                <AddNew Enabled="False" />
				                <Delete Enabled="False" />
			                </Actions>
		                </ActionBar>
						<EditPageParams>
							<px:PXControlParam ControlID="gridUsers" Name="Username" PropertyName="DataValues[&quot;Username&quot;]" Type="String" />
						</EditPageParams>
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Account Groups">
				<Template>
					<px:PXGrid ID="gridAccountGroups" BorderWidth="0px" runat="server" Height="150px" Width="100%"
						AdjustPageSize="Auto" AllowSearch="True" SkinID="DetailsInTab" TabIndex="400" 
						DataSourceID="ds" FastFilterFields="GroupCD,Description">
						<Levels>
							<px:PXGridLevel DataMember="AccountGroup">
								<Mode AllowAddNew="True" AllowDelete="False" />
								<Columns>
								    <px:PXGridColumn DataField="Included" TextAlign="Center" Type="CheckBox" AllowCheckAll="True" />
								    <px:PXGridColumn DataField="GroupCD" />
								    <px:PXGridColumn DataField="Description" />
								</Columns>
								<RowTemplate>
									<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
								    <px:PXCheckBox ID="chkAccountGroupIncluded" runat="server" DataField="Included"/>
								    <px:PXSegmentMask ID="edGroupCD" runat="server" DataField="GroupCD" AllowEdit="True"/>
								</RowTemplate>
								<Layout />
							</px:PXGridLevel>
						</Levels>
						<AutoSize Enabled="True" />
		                <Mode AllowAddNew="False" AllowDelete="False" />
		                <ActionBar>
			                <Actions>
				                <AddNew Enabled="False" />
				                <Delete Enabled="False" />
			                </Actions>
		                </ActionBar>
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
		</Items>
		<AutoSize Container="Window" Enabled="True" MinHeight="250" MinWidth="300" />
	</px:PXTab>
</asp:Content>
