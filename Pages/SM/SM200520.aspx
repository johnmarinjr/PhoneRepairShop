<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true"
	ValidateRequest="false" CodeFile="SM200520.aspx.cs" Inherits="Page_SM200520"
	Title="Site Map Maintenance" %>

<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <script type="text/javascript">
        function commandResult(ds, context)
        {
            if (context.command == "Save" || context.command == "Delete") {
                var ds = px_all[context.id];
                var isSitemapAltered = (ds.callbackResultArg == "RefreshSitemap");
                if (isSitemapAltered) __refreshMainMenu();
            }
        }
    </script>
	<px:PXDataSource ID="ds" Width="100%" runat="server" PrimaryView="SiteMap" TypeName="PX.SiteMap.Graph.SiteMapMaint"  Visible="True">
        <ClientEvents CommandPerformed="commandResult" />
		<CallbackCommands>
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>

<asp:Content ID="cont" ContentPlaceHolderID="phL" runat="Server">
	<px:PXGrid ID="grid" runat="server" Height="350px" Width="100%" AdjustPageSize="Auto"
		SkinID="Primary" AllowPaging="True" AllowSearch="True" FastFilterFields="ScreenID,Title" SyncPosition="True" OnDataBound="form_DataBound">
	    
		<Levels>
			<px:PXGridLevel DataMember="SiteMap">
				<RowTemplate>
				    <px:PXLayoutRule runat="server" StartColumn="True"  LabelsWidth="M" 
				                     ControlSize="L" StartGroup="True" />
				    <px:PXTextEdit ID="edTitle" runat="server" DataField="Title" />
				    <px:PXTextEdit ID="edUrl" runat="server" DataField="Url" />
				    <px:PXTextEdit ID="edGraphtype" runat="server" DataField="Graphtype" Enabled="False"/>
				    <px:PXMaskEdit ID="edScreenID" runat="server" DataField="ScreenID" InputMask="CC.CC.CC.CC" />
				    <px:PXDropDown ID="edWorkspaces" runat="server" DataField="Workspaces" AllowMultiSelect="True"/>
				    <px:PXDropDown ID="edCategory" runat="server" DataField="Category"/>
				    <px:PXCheckBox ID="chkLEP" runat="server" DataField="ListIsEntryPoint"/>
				</RowTemplate>
				<Columns>
				    <px:PXGridColumn DataField="ScreenID" DisplayFormat="CC.CC.CC.CC" CommitChanges="True" Width="120px" />  
				    <px:PXGridColumn DataField="Title" Width="200px"> </px:PXGridColumn>
				    <px:PXGridColumn DataField="Url" Width="200px" CommitChanges="True"> </px:PXGridColumn>
				    <px:PXGridColumn DataField="Graphtype" Width="200px"> </px:PXGridColumn>
				    <px:PXGridColumn DataField="Workspaces" Width="200px" />
				    <px:PXGridColumn DataField="Category" Width="200px" />
				    <px:PXGridColumn DataField="ListIsEntryPoint" TextAlign="Center" Type="CheckBox" Width="60px"/>
				</Columns>
				<Styles>
					<RowForm Height="250px">
					</RowForm>
				</Styles>
			</px:PXGridLevel>
		</Levels>
		<Layout FormViewHeight="250px" />
		<AutoSize Container="Window" Enabled="True" MinHeight="200" />
		<CallbackCommands>
			<Save PostData="Content" />
		</CallbackCommands>
	</px:PXGrid>
</asp:Content>
