<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true"
	ValidateRequest="false" CodeFile="SM209600.aspx.cs" Inherits="Page_SM209600"
	Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" Width="100%" runat="server" TypeName="PX.Objects.AP.InvoiceRecognition.ExcludedVendorDomainMaint" PrimaryView="Domains" Visible="True">
		<CallbackCommands>
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">
    <px:PXGrid ID="grid" runat="server" DataSourceID="ds" Width="100%" Height="100%" Caption="Excluded Vendor Domains" CaptionVisible="false"
        SkinID="Primary" AllowPaging="true" AdjustPageSize="Auto" AutoAdjustColumns="true" SyncPosition="true" FastFilterFields="Name">
        <Levels>
            <px:PXGridLevel DataMember="Domains">
                <Columns>
                    <px:PXGridColumn DataField="Name" />
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Container="Window" Enabled="true" MinHeight="200" />
    </px:PXGrid>
</asp:Content>
