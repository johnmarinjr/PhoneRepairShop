<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true"	ValidateRequest="false"
	CodeFile="SM204005.aspx.cs" Inherits="Pages_SM_SM204005" Title="Task Templates" %>
<%@ MasterType VirtualPath="~/MasterPages/FormTab.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" Width="100%" runat="server" Visible="True" PrimaryView="TaskTemplates" TypeName="PX.SM.TaskTemplateMaint">
		<CallbackCommands>
			<px:PXDSCallbackCommand Name="createBusinessEvent" Visible="False" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="viewBusinessEvent" Visible="False" DependOnGrid="grdCreatedByEvents" />
		</CallbackCommands>	 
		<DataTrees> 
			<px:PXTreeDataMember TreeView="EntityItems" TreeKeys="Key"/>
            <px:PXTreeDataMember TreeView="PreviousEntityItems" TreeKeys="Key"/>
			<px:PXTreeDataMember TreeView="ScreenOwnerItems" TreeKeys="Key"/>
		</DataTrees>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView ID="frmTask" runat="server" DataSourceID="ds" DataMember="TaskTemplates" Width="100%" DefaultControlID="edTaskTemplateID">
		<Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="L" />
            <px:PXSelector runat="server" ID="edTemplateID" DataField="TaskTemplateID" FilterByAllFields="True" AutoRefresh="True" TextField="Name" NullText="<NEW>" DataSourceID="ds">
                <GridProperties>
                    <Columns>
                        <px:PXGridColumn DataField="TaskTemplateID" Width="60px" />
                        <px:PXGridColumn DataField="Name" Width="120px" />
                        <px:PXGridColumn DataField="Summary" Width="220px" />
                        <px:PXGridColumn DataField="ScreenID" Width="60px" />
                    </Columns>
                </GridProperties>
            </px:PXSelector>
            <px:PXTextEdit runat="server" ID="edName" DataField="Name" AlreadyLocalized="False" DefaultLocale="" />
			<px:PXTreeSelector runat="server" ID="edOwnerName" DataField="OwnerName" TreeDataSourceID="ds" PopulateOnDemand="True" InitialExpandLevel="0" ShowRootNode="false"
				MinDropWidth="468" MaxDropWidth="600" AllowEditValue="False" AppendSelectedValue="False" AutoRefresh="true" TreeDataMember="ScreenOwnerItems">
				<DataBindings>
					<px:PXTreeItemBinding DataMember="ScreenOwnerItems" TextField="Name" ValueField="Path" ImageUrlField="Icon" />
				</DataBindings>
			</px:PXTreeSelector>

            <px:PXLayoutRule runat="server" ColumnSpan="2" />
            <px:PXTreeSelector runat="server" ID="edSummary" DataField="Summary" TreeDataSourceID="ds" PopulateOnDemand="True" InitialExpandLevel="0" ShowRootNode="false"
                MinDropWidth="468" MaxDropWidth="600" AllowEditValue="true" AppendSelectedValue="true" AutoRefresh="true" TreeDataMember="EntityItems">
                <DataBindings>
                    <px:PXTreeItemBinding DataMember="EntityItems" TextField="Name" ValueField="Path" ImageUrlField="Icon" ToolTipField="Path" />
                </DataBindings>
            </px:PXTreeSelector>

            <px:PXLayoutRule runat="server" LabelsWidth="S" ControlSize="L" StartColumn="True" />
            <px:PXSelector runat="server" ID="edScreenID" DataField="ScreenID" DisplayMode="Hint" FilterByAllFields="true" CommitChanges="True" />
            <px:PXSelector runat="server" ID="edLocale" DataField="LocaleName" DisplayMode="Text" />
            
			<px:PXLayoutRule runat="server" LabelsWidth="S" ControlSize="L" Merge="True" />
            <px:PXCheckBox runat="server" ID="edAttachActivity" DataField="attachActivity" SuppressLabel="True" AlignLeft="False" />
            <px:PXTreeSelector runat="server" ID="edRefNoteId" DataField="RefNoteID" TreeDataSourceID="ds" PopulateOnDemand="True" InitialExpandLevel="0" ShowRootNode="False"
				MinDropWidth="468" MaxDropWidth="600" AllowEditValue="true" AppendSelectedValue="False" AutoRefresh="true" TreeDataMember="EntityItems">
                <DataBindings>
                    <px:PXTreeItemBinding DataMember="EntityItems" TextField="Name" ValueField="Path" ImageUrlField="Icon" ToolTipField="Path" />
                </DataBindings>
            </px:PXTreeSelector>
            
			<px:PXLayoutRule runat="server" LabelsWidth="S" ControlSize="M" />
            <px:PXCheckBox runat="server" ID="chkShowCreatedByEventsTabExpr" DataField="ShowCreatedByEventsTabExpr" SuppressLabel="True" />
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXTab ID="tab" runat="server" Height="150px" Style="z-index: 100" Width="100%" DataSourceID="ds" DataMember="CurrentTaskTemplate">
		<Activity HighlightColor="" SelectedColor="" Width="" Height="" />
		<Items>
			<px:PXTabItem Text="Body">
				<Template>
					<px:PXRichTextEdit ID="edBody" runat="server" EncodeInstructions="true" DataField="Body" Style="border-width: 0px; border-top-width: 1px; width: 100%;"
					    AllowInsertParameter="true" AllowInsertPrevParameter="True" AllowPlaceholders="true" AllowAttached="true" AllowSearch="true" AllowMacros="true" AllowLoadTemplate="true" AllowSourceMode="true"
				        OnBeforePreview="edBody_BeforePreview" OnBeforeFieldPreview="edBody_BeforeFieldPreview" FileAttribute="embedded">
				        <AutoSize Enabled="True" MinHeight="216" />
				        <InsertDatafield DataSourceID="ds" DataMember="EntityItems" TextField="Name" ValueField="Path" ImageField="Icon" />
				        <InsertDatafieldPrev DataSourceID="ds" DataMember="PreviousEntityItems" TextField="Name" ValueField="Path" ImageField="Icon" />
                        <LoadTemplate TypeName="PX.SM.TaskTemplateMaint" DataMember="TaskTemplatesRO" ViewName="TaskTemplate" ValueField="taskTemplateID" TextField="Name" DataSourceID="ds" Size="M"/>
			        </px:PXRichTextEdit>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Task Settings">
				<Template>
					<px:PXGrid ID="gridSettings" runat="server" DataSourceID="ds" Height="150px" Style="z-index: 100"
						Width="100%" AllowPaging="False" AdjustPageSize="Auto" AllowSearch="True" SkinID="DetailsInTab"
						AutoAdjustColumns="True" MatrixMode="false" SyncPosition="True" KeepPosition="True">
						<ActionBar>
							<Actions>
								<Upload MenuVisible="False" ToolBarVisible="False" />
								<NoteShow MenuVisible="False" ToolBarVisible="False" />
							</Actions>
						</ActionBar>
						<Levels>
							<px:PXGridLevel DataMember="TaskTemplateSettings">
								<RowTemplate>
									<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
                                    <pxa:PXFormulaCombo ID="edValue" runat="server" DataField="Value" EditButton="True" FieldsAutoRefresh="True"  FieldsRootAutoRefresh="true"
										OnExternalFieldsNeeded="edValue_ExternalFieldsNeeded" OnInternalFieldsNeeded="edValue_InternalFieldsNeeded" SkinID="GI"/>
								</RowTemplate>
								<Columns>
									<px:PXGridColumn DataField="IsActive" AllowNull="False" TextAlign="Center" Type="CheckBox" Width="50px" />
									<px:PXGridColumn DataField="FieldName" AutoCallBack="True" Width="175px" CommitChanges="True" />
									<px:PXGridColumn DataField="FromSchema" AllowNull="False" TextAlign="Center" Type="CheckBox" Width="75px" CommitChanges="True" />
									<%--<px:PXGridColumn DataField="Value" EditorID="edValue" RenderEditorText="True" Width="300px" CommitChanges="True" />--%>
                                    <px:PXGridColumn DataField="Value" Width="150px" Key="value1" AllowSort="False" MatrixMode="true" AllowStrings="True" DisplayMode="Value"/>
									<px:PXGridColumn DataField="LineNbr" Visible="False" AllowShowHide="False" ForceExport="True" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<Mode AllowAddNew="False" AllowUpdate="True" AllowDelete="False" />
						<AutoSize Enabled="True" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Created by Events" BindingContext="frmTask" VisibleExp="DataControls[&quot;chkShowCreatedByEventsTabExpr&quot;].Value=1">
				<Template>
                    <px:PXGrid ID="grdCreatedByEvents" runat="server" DataSourceID="ds" Width="100%" SkinID="Details"
						AutoAdjustColumns="True" AllowPaging="False" SyncPosition="True">
						<ActionBar>
                            <CustomItems>
                                <px:PXToolBarButton Text="Create Business Event">
                                   <AutoCallBack Command="createBusinessEvent" Target="ds" />
                                </px:PXToolBarButton>
                            </CustomItems>
                        </ActionBar>
						<Levels>
							<px:PXGridLevel DataMember="BusinessEvents">
								<RowTemplate>
									<px:PXSelector ID="edEventName" runat="server" DataField="Name" AutoRefresh="True" />
								</RowTemplate>
								<Columns>
									<px:PXGridColumn DataField="Name" LinkCommand="ViewBusinessEvent" />
									<px:PXGridColumn DataField="Description" />
									<px:PXGridColumn DataField="Active" Type="CheckBox" />
									<px:PXGridColumn DataField="Type" Width="200" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<Mode AllowUpdate="False" />
						<AutoSize Enabled="True" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
		</Items>
		<AutoSize Container="Window" Enabled="True" MinHeight="250" />
	</px:PXTab>
	<px:PXSmartPanel ID="pnlCreateBusinessEvent" runat="server" Key="NewEventData" Caption="Create Business Event" CaptionVisible="True"
        AutoReload="True" AutoRepaint="True" Width="500">
	    <px:PXFormView ID="frmCreateBusinessEvent" runat="server" DataMember="NewEventData" DataSourceID="ds" Width="100%" SkinID="Transparent">
		    <Template>
			    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="L" />
			    <px:PXTextEdit runat="server" ID="txtName" DataField="Name" CommitChanges="True" />
		    </Template>
	    </px:PXFormView>
		<px:PXPanel ID="pnlButtons" runat="server" SkinID="Buttons">
            <px:PXButton ID="btnOK" runat="server" DialogResult="OK" Text="OK" />
			<px:PXButton ID="btnCancel" runat="server" DialogResult="Cancel" Text="Cancel" />
		</px:PXPanel>
	</px:PXSmartPanel>
</asp:Content>
