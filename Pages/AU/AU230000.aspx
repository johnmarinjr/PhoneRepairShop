<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true"
	ValidateRequest="false" CodeFile="AU230000.aspx.cs" Inherits="Page_AU230000"%>

<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXLabel runat="server" Text="User-Defined Fields" CssClass="projectLink transparent border-box" />
	<pxa:AUDataSource ID="ds" runat="server" Width="100%" TypeName="PX.SM.ProjectUserFieldsMaintenance" PrimaryView="Items" Visible="true">
	</pxa:AUDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">
	<px:PXGrid ID="grid"
        runat="server"
        Width="100%"
		SkinID="Primary"
        AutoAdjustColumns="True"
        SyncPosition="True"
        FilesIndicator="False"
        NoteIndicator="False">
		<AutoSize Enabled="true" Container="Window" />
		<Mode AllowAddNew="False" />
		<ActionBar Position="Top" ActionsVisible="false">
			<Actions>
				<NoteShow MenuVisible="False" ToolBarVisible="False" />
				<AddNew MenuVisible="False" ToolBarVisible="False" />
				<ExportExcel MenuVisible="False" ToolBarVisible="False" />
				<AdjustColumns ToolBarVisible="False"/>
			</Actions>
		</ActionBar>
		<Levels>
			<px:PXGridLevel DataMember="Items">
				<RowTemplate>
					<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
					<px:PXTextEdit SuppressLabel="True" Height="100%" runat="server" ID="edSource" TextMode="MultiLine"
						DataField="Content" Font-Size="10pt" Font-Names="Courier New" Wrap="False" SelectOnFocus="False">
						<AutoSize Enabled="True" />
					</px:PXTextEdit>
				</RowTemplate>
				<Columns>
					<px:PXGridColumn DataField="AttributeID" Width="98px" LinkCommand="actionEdit"/>
					<px:PXGridColumn DataField="Description" Width="148px" />
                    <px:PXGridColumn DataField="ScreenID" Width="200px"/>
					<px:PXGridColumn DataField="LastModifiedByID_Modifier_Username" Width="108px" />
					<px:PXGridColumn DataField="LastModifiedDateTime" Width="90px" />
				</Columns>
			</px:PXGridLevel>
		</Levels>
	</px:PXGrid>
    
    <px:PXSmartPanel runat="server" ID="AddSelectUserFields" Width="470px" Height="400px" CaptionVisible="True"
		Caption="Add User-Defined Fields" ShowMaximizeButton="True" Key="ViewSelectUserFields" ShowAfterLoad="true" AutoRepaint="True">      
        <px:PXGrid runat="server" 
			ID="gridAddUserFields" DataSourceID="ds" 
			Width="100%" BatchUpdate="True" 
			CaptionVisible="False" 
			Caption="User-Defined Fields"
			AutoAdjustColumns="True"
            AllowPaging="False"
			SkinID="Details"
            FilesIndicator="False"
            NoteIndicator="False">
			<Levels>
				<px:PXGridLevel DataMember="ViewSelectUserFields">
					<Columns>
					    <px:PXGridColumn DataField="Selected" Type="CheckBox" AllowCheckAll="True" Width="70px"/>	
					    <px:PXGridColumn DataField="AttributeID" Width="200px"/>						
                        <px:PXGridColumn DataField="Description" Width="200px"/>										
					</Columns>
				</px:PXGridLevel>
			</Levels>
		    <AutoSize Enabled="True"/>
			<ActionBar Position="Top">
				<Actions>
					<AddNew MenuVisible="False" ToolBarVisible="False"/>
					<Delete MenuVisible="False" ToolBarVisible="False"/>
					<AdjustColumns  ToolBarVisible="False"/>
					<ExportExcel  ToolBarVisible="False"/>
				</Actions>
			</ActionBar>
		</px:PXGrid>			
	
         <px:PXPanel ID="PXPanel1" runat="server" SkinID="Buttons">
                    <px:PXButton ID="PXButton1" runat="server" DialogResult="OK" Text="Save" />
                    <px:PXButton ID="PXButton2" runat="server" DialogResult="No" Text="Cancel" CausesValidation="False" />
        </px:PXPanel>
	</px:PXSmartPanel>

        <px:PXSmartPanel runat="server" ID="EditSelectUserFields" Width="470px" Height="400px" CaptionVisible="True"
		Caption="Edit Attribute" ShowMaximizeButton="True" Key="EditUserFieldScreens" ShowAfterLoad="true" AutoRepaint="True">      
        <px:PXGrid runat="server" 
			ID="PXGrid1" DataSourceID="ds" 
			Width="100%" BatchUpdate="True" 
			AutoAdjustColumns="True"
            AllowPaging="False"
			SkinID="Details"
            FilesIndicator="False"
            NoteIndicator="False">
			<Levels>
				<px:PXGridLevel DataMember="EditUserFieldScreens">
					<Columns>
					    <px:PXGridColumn DataField="Selected" Type="CheckBox" AllowCheckAll="True" Width="70px"/>					
                        <px:PXGridColumn DataField="ScreenId" Width="200px"/>										
					</Columns>
				</px:PXGridLevel>
			</Levels>
		    <AutoSize Enabled="True"/>
            <Mode AllowAddNew="False"/>
			<ActionBar Position="Top">
				<Actions>
					<AddNew MenuVisible="False" ToolBarVisible="false"/>
					<Delete MenuVisible="False" ToolBarVisible="false"/>
					<AdjustColumns  ToolBarVisible="false"/>
					<ExportExcel  ToolBarVisible="false"/>
				</Actions>
			</ActionBar>
		</px:PXGrid>			
	
         <px:PXPanel ID="PXPanel2" runat="server" SkinID="Buttons">
                    <px:PXButton ID="PXButton3" runat="server" DialogResult="OK" Text="Save" />
                    <px:PXButton ID="PXButton4" runat="server" DialogResult="No" Text="Cancel" CausesValidation="False" />
        </px:PXPanel>
	</px:PXSmartPanel>
</asp:Content>
