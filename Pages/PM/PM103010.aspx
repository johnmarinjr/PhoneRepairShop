<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true"
	ValidateRequest="false" CodeFile="PM103010.aspx.cs" Inherits="Page_PM103010"
	Title="Restriction Groups by Account Group" %>
	

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.PM.PMAccountGroupAccessDetail"
		PrimaryView="AccountGroup">
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView ID="form" runat="server" Width="100%" Caption="Account Group" DataMember="AccountGroup" DefaultControlID="edGroupCD" TemplateContainer="">
		<Template>
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
			<px:PXSegmentMask ID="edGroupCD" runat="server" DataField="GroupCD" FilterByAllFields="True">
			    <AutoCallBack Command="Cancel" Target="ds"/>
			</px:PXSegmentMask>
		    <px:PXTextEdit ID="edDescription" runat="server" DataField="Description" Enabled="False" />
			<px:PXDropDown ID="edType" runat="server" DataField="Type" Enabled="False"  />
			<px:PXCheckBox ID="chkIsActive" runat="server" Checked="True" DataField="IsActive" />
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXGrid ID="grid" runat="server" Height="150px" Width="100%" AdjustPageSize="Auto" Caption="Restriction Groups" AllowSearch="True" SkinID="Details">
		<Levels>
			<px:PXGridLevel DataMember="Groups">
				<Mode AllowAddNew="False" AllowDelete="False" />
				<Columns>
				    <px:PXGridColumn DataField="Included" TextAlign="Center" Type="CheckBox" RenderEditorText="True" AllowCheckAll="True"/>
				    <px:PXGridColumn DataField="GroupName"/>
				    <px:PXGridColumn DataField="Description"/>
				    <px:PXGridColumn DataField="Active" TextAlign="Center" Type="CheckBox"/>
				    <px:PXGridColumn DataField="GroupType" Label="Visible To Entities" RenderEditorText="True"/>
				</Columns>
				<RowTemplate>
					<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
				    <px:PXCheckBox SuppressLabel="True" ID="chkSelected" runat="server" DataField="Included" />
				    <px:PXSelector ID="edGroupName" runat="server" DataField="GroupName" />
				    <px:PXTextEdit  ID="edDescription" runat="server" DataField="Description" />
				    <px:PXCheckBox SuppressLabel="True" ID="chkActive" runat="server" Checked="True" DataField="Active" />
				    <px:PXDropDown  ID="edGroupType" runat="server" DataField="GroupType" Enabled="False" />
				</RowTemplate>
			</px:PXGridLevel>
		</Levels>
		<AutoSize Container="Window" Enabled="True" />
		<Mode AllowAddNew="False" AllowDelete="False" />
		<ActionBar>
			<Actions>
				<AddNew Enabled="False" />
				<Delete Enabled="False" />
			</Actions>
		</ActionBar>
	</px:PXGrid>
</asp:Content>
