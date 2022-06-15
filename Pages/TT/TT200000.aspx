<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="TT200000.aspx.cs"
    Inherits="Page_TT200000" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormTab.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" PrimaryView="Employee" TypeName="PX.Objects.EP.EmployeeMaint">
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Width="100%" DataMember="Employee" Caption="Employee Info"
        NoteIndicator="True" FilesIndicator="True" LinkIndicator="True" BPEventsIndicator="True" DefaultControlID="edAcctCD"
        TabIndex="100">
        <Template>
			<px:PXTree runat="server" ID="treeOne" DataMember="testtree" DefaultDrag="Cut" Graph="PX.Objects.EP.EmployeeMaint" Width="400" Height="700" ExtraColumns='[{"tagname": "qp-text-editor", "title": "Nums", "width": 50}, {"tagname": "qp-check-box", "title": "Chs", "width": 50}]' OnAdd="AddNode" OnDelete="DeleteNode" Modifiable OnRename="RenameNode" OnChange="ChangeTree" IconField="CEmail" Mode="multi" FilterShowChildren="true"></px:PXTree>
            <px:PXLayoutRule ID="treeThree" runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
			<px:PXTree runat="server" ID="treeTwo" DataMember="testtree" DefaultDrag="Copy" Graph="PX.Objects.EP.EmployeeMaint" Modifiable IconField="CEmail" Mode="single" ExtraColumns='[{"tagname": "qp-text-editor", "title": "Nums", "width": 50}, {"tagname": "qp-check-box", "title": "Chs", "width": 50}]' OnAdd="AddNode" OnDelete="DeleteNode" OnRename="RenameNode" OnChange="ChangeTree" OnSelect="DeleteNode"></px:PXTree>
            <px:PXLayoutRule ID="PXLayoutRule1" runat="server" StartColumn="True" LabelsWidth="XS" ControlSize="S" />
            <px:PXDropDown ID="edVStatus" runat="server" DataField="VStatus" />
            <px:PXCheckBox ID="chkServiceManagement" runat="server" DataField="ChkServiceManagement"/>
        </Template>
    </px:PXFormView>
</asp:Content>