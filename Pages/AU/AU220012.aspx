<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true"
	ValidateRequest="false" CodeFile="AU220012.aspx.cs" Inherits="Page_AU220012"
	 %>

<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <script type="text/javascript">
		function commandResult(ds, context)
        {

			var grid = null;
			var row = null;
            if (context.command == "moveUpWorkspace" || context.command == "moveDownWorkspace")
                grid = px_all[grdWorkspacesID]; //check aspx.cs for grdWorkspacesID
            

            if (context.command == "moveDownWorkspace")
				row = grid.activeRow.nextRow();
            else if (context.command == "moveUpWorkspace")
				row = grid.activeRow.prevRow();

			if (row)
				row.activate();
		}

		function toolsButtonClickHandler(grid, e)
		{
		    if (isCommandMoving(e.button.commandName))
		        grid.activeRow.dataChanged = true;
		}

		function isCommandMoving(commandName)
		{
            return commandName == "moveUpWorkspace" || commandName == "moveDownWorkspace"  ;
		}

	</script>
    <pxa:AUDataSource ID="ds" runat="server" Width="100%" TypeName="PX.Api.Mobile.Workspaces.MobileSitemapManageWorkspaceMaint" PrimaryView="Workspaces" Visible="true">
        <ClientEvents CommandPerformed="commandResult" />
		<CallbackCommands>
			<px:PXDSCallbackCommand CommitChanges="True" Name="openWorkspace" DependOnGrid="grdWorkspaces" Visible="False" RepaintControls="All" />
            <px:PXDSCallbackCommand DependOnGrid="grdWorkspaces" Name="moveUpWorkspace" Visible="True" CommitChanges="True" RepaintControls="All"/>
            <px:PXDSCallbackCommand DependOnGrid="grdWorkspaces" Name="moveDownWorkspace" Visible="True" CommitChanges="True" RepaintControls="All"/>
		</CallbackCommands>
	</pxa:AUDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">
    <px:PXGrid ID="grdWorkspaces"
               runat="server"
               Width="100%"
		       SkinID="Primary"
               AutoAdjustColumns="True"
               SyncPosition="True"
               FilesIndicator="True"
               NoteIndicator="True" 
               AllowFilter ="True" 
               CaptionVisible ="True"> 
		<AutoSize Enabled="true" Container="Window" />
        <ClientEvents ToolsButtonClick="toolsButtonClickHandler" />
		<Levels>
			<px:PXGridLevel DataMember="Workspaces">
				<RowTemplate>
					<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
				</RowTemplate>
				<Columns>
					<px:PXGridColumn DataField="Name" Width="250px" LinkCommand="openWorkspace" />
					<px:PXGridColumn DataField="DisplayName" Width="108px" />
                    <px:PXGridColumn DataField="IsActive" Width="108px" Type="CheckBox" />
				</Columns>
			</px:PXGridLevel>
		</Levels>
	</px:PXGrid>
</asp:Content>
