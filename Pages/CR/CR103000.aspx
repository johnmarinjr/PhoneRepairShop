<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormView.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="CR103000.aspx.cs"
	Inherits="Page_CR103000" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormView.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" runat="server" AutoCallBack="True" Visible="True" Width="100%" PrimaryView="Setup" TypeName="PX.Objects.CR.CRDuplicateValidationSetupMaint" PageLoadBehavior="InsertRecord">
		<DataTrees>
			<px:PXTreeDataMember TreeView="Nodes" TreeKeys="ID" />
		</DataTrees>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView runat="server"
		ID="frmSetup" DataSourceID="ds" Width="100%" DataMember="Setup"
		NoteIndicator="True" FilesIndicator="True" ActivityIndicator="True" ActivityField="NoteActivity" DefaultControlID="edName">

		<Template>
		</Template>
	</px:PXFormView>
	<px:PXSplitContainer runat="server" ID="sp0" SplitterPosition="270" Style="background-color: #eeeeee;" >
		<AutoSize Enabled="true" Container="Window" />
		<Template1>
			<px:PXTreeView ID="tree" runat="server"
							DataSourceID="ds" Height="500px" PopulateOnDemand="True" ShowRootNode="False" ExpandDepth="1" AutoRepaint="true" Caption="Comparison"
							AllowCollapse="true" CommitChanges="true" MatrixMode="true" SyncPosition="True" SyncPositionWithGraph="True" SelectFirstNode="True" Images="">

				<AutoCallBack Target="frmCurrentNode" Command="Refresh" Enabled="True" />
				<AutoSize Enabled="True" MinHeight="300" />
				<DataBindings>
					<px:PXTreeItemBinding DataMember="Nodes" TextField="Description" ValueField="ID" />
				</DataBindings>
			</px:PXTreeView>
		</Template1>
		<Template2>
			<px:PXFormView ID="frmCurrentNode" runat="server" Width="100%" DataMember="CurrentNode" DataSourceID="ds" >
				<AutoSize Container="Window" />
				<Template>
					<px:PXLayoutRule runat="server" StartColumn="True" GroupCaption="Rules of Comparison" StartGroup="True" LabelsWidth="M" ControlSize="L" />
					<px:PXNumberEdit ID="edValidationThreshold" Size="SM" runat="server" DataField="ValidationThreshold" Decimals="2" ValueType="Decimal" CommitChanges="true"/>
					<px:PXCheckBox ID="edValidateOnEntry" runat="server" DataField="ValidateOnEntry" CommitChanges="true"/>
				</Template>
			</px:PXFormView>
			<px:PXGrid ID="bottomGrid" runat="server" SkinID="Details" DataSourceID="ds" ActionsPosition="Top" Width="100%"
				MatrixMode="True" SyncPosition="True" SyncPositionWithGraph="True" CaptionVisible="False" Style="z-index: 101;" AllowPaging="False">
				<Levels>
					<px:PXGridLevel DataMember="ValidationRules">
						<Columns>
							<px:PXGridColumn DataField="ValidationType" />
							<px:PXGridColumn DataField="MatchingField" CommitChanges="True" />
							<px:PXGridColumn DataField="ScoreWeight" Width="110px" CommitChanges="True" />
							<px:PXGridColumn DataField="TransformationRule" Width="150px" CommitChanges="True" />
							<px:PXGridColumn DataField="CreateOnEntry" Width="110px" CommitChanges="True"/>
						</Columns>
					</px:PXGridLevel>
				</Levels>
				<AutoSize Enabled="true" Container="Parent"/>
				<Mode AllowUpload="True" />
				<ActionBar>
					<Actions>
						<Search Enabled="False" />
						<EditRecord Enabled="False" />
						<NoteShow Enabled="False" />
						<FilterShow Enabled="False" />
						<FilterSet Enabled="False" />
						<Upload Enabled="True" />
					</Actions>
					<CustomItems>
						<px:PXToolBarButton Tooltip="Copy Validation Rules" DisplayStyle="Image">
							<AutoCallBack Command="Copy" Enabled="True" Target="ds" />
							<Images Normal="main@Copy" />
						</px:PXToolBarButton>
						<px:PXToolBarButton Tooltip="Paste validation rules from clipboard" DisplayStyle="Image">
							<AutoCallBack Command="Paste" Enabled="True" Target="ds" />
							<Images Normal="main@Paste" />
						</px:PXToolBarButton>
					</CustomItems>
				</ActionBar>
				<CallbackCommands>
					<Refresh PostData="Page" RepaintControlsIDs="formRuleType" />
				</CallbackCommands>
				<Mode AllowUpload="True" InitNewRow="True" />
			</px:PXGrid>
		</Template2>
	</px:PXSplitContainer>
</asp:Content>
