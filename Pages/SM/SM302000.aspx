<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="SM302000.aspx.cs" Inherits="Page_SM302000" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" Runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" TypeName="PX.PushNotifications.UI.PushNotificationMaint" SuspendUnloading="False" PrimaryView="Hooks" />
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" Runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100" 
		Width="100%" DataMember="Hooks" TabIndex="5500">
		<Template>
			<px:PXLayoutRule runat="server" StartRow="True" ControlSize="M" LabelsWidth="SM" Merge="true" />
		    <px:PXSelector ID="edName" runat="server" DataField="Name"/>
            <px:PXTextEdit ID="edHeaderName" runat="server" AlreadyLocalized="False" DataField="HeaderName" DefaultLocale=""/>
            <px:PXCheckBox Size="S" runat="server" DataField="Active" ID="PXCheckBox1" />
            <px:PXLayoutRule runat="server"/>
            <px:PXLayoutRule runat="server" StartRow="True" ControlSize="M" LabelsWidth="SM" Merge="true" />
            <px:PXDropDown ID="edType" runat="server" DataField="Type" CommitChanges="true"/>
            <px:PXTextEdit ID="edHeaderValue" runat="server" AlreadyLocalized="False" DataField="HeaderValue" DefaultLocale=""/>
            <px:PXLayoutRule runat="server"/>
            <px:PXTextEdit ID="edAddress" runat="server" AlreadyLocalized="False" DataField="Address" DefaultLocale=""/>
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" Runat="Server">
    <px:PXTab ID="tab" runat="server" Width="100%" Height="150px" DataSourceID="ds" DataMember="SourcesGI">
		<Items>
			<px:PXTabItem Text="Generic Inquiries">
				<Template>
					<px:PXSplitContainer runat="server" PositionInPercent="true" SplitterPosition="50" Width="100%" Height="100%">
						<Template1>
							<px:PXGrid ID="grid1" runat="server" DataSourceID="ds" Style="z-index: 100" TabIndex="5700" Width="100%" Height="150px"
								SkinID="Details" Caption="Inquiries" CaptionVisible="true" AutoAdjustColumns="true" SyncPosition="true" KeepPosition="true">
								<ActionBar>
									<CustomItems>
										<px:PXToolBarButton CommandSourceID="ds" CommandName="ViewInquiry" />
									</CustomItems>
								</ActionBar>
								<Mode InitNewRow="True" AllowRowSelect="True"/>
								<Levels>
									<px:PXGridLevel DataMember="SourcesGI">
										<Columns>
											<px:PXGridColumn DataField="Active" Width="60" Type="CheckBox" TextAlign="Center" AllowResize="false"/>
											<px:PXGridColumn DataField="DesignID" />
											<px:PXGridColumn DataField="TrackAllFields" Width="150" Type="CheckBox" TextAlign="Center" AllowResize="false" CommitChanges="true" />
										</Columns>
									</px:PXGridLevel>
								</Levels>
								<AutoSize Container="Window" Enabled="True" MinHeight="150" />
								<AutoCallBack Command="Refresh" Target="grid3"/>
                                <CallbackCommands>
                                    <InitRow CommitChanges="true" />
                                </CallbackCommands>
							</px:PXGrid>
						</Template1>
						<Template2>
							<px:PXGrid ID="grid3" runat="server" DataSourceID="ds" Style="z-index: 100" TabIndex="5700" Width="100%" Height="150px"
								SkinID="Details" Caption="Fields" CaptionVisible="true" AutoAdjustColumns="true" SyncPosition="true" AutoRefresh="true" MatrixMode="true">
								<Mode InitNewRow="True" AllowRowSelect="True"/>
								<Levels>
									<px:PXGridLevel DataMember="TrackingFieldsGI">
										<Columns>
											<px:PXGridColumn DataField="TableName" Width="200" Type="DropDownList" CommitChanges="True" />
											<px:PXGridColumn DataField="FieldName" Width="300" Type="DropDownList" CommitChanges="True" />
										</Columns>
									</px:PXGridLevel>
								</Levels>
								<AutoSize Container="Window" Enabled="True" MinHeight="150" />
                                <CallbackCommands>
                                    <InitRow CommitChanges="true" />
                                </CallbackCommands>
							</px:PXGrid>
						</Template2>
						<AutoSize Enabled="True" Container="Window" />
					</px:PXSplitContainer>
				</Template>
		    </px:PXTabItem>
            <px:PXTabItem Text="Built-In Definitions">
				<Template>
					<px:PXSplitContainer runat="server" PositionInPercent="true" SplitterPosition="50" Width="100%" Height="100%">
						<Template1>
							<px:PXGrid ID="grid2" runat="server" DataSourceID="ds" Style="z-index: 100" TabIndex="5700" Width="100%" Height="150px"
								SkinID="Details" Caption="Definitions" CaptionVisible="true" AutoAdjustColumns="true" SyncPosition="true" KeepPosition="true">
                                <Mode InitNewRow="True" AllowRowSelect="True"/>
                                <Levels>
									<px:PXGridLevel DataMember="SourcesInCode">
										<Columns>
											<px:PXGridColumn DataField="Active" Width="60" Type="CheckBox" TextAlign="Center" AllowResize="false"/>
											<px:PXGridColumn DataField="InCodeClass"/>
											<px:PXGridColumn DataField="TrackAllFields" Width="150" Type="CheckBox" TextAlign="Center" AllowResize="false" CommitChanges="true" />
										</Columns>
									</px:PXGridLevel>
								</Levels>
								<AutoSize Container="Window" Enabled="True" MinHeight="150" />
								<AutoCallBack Command="Refresh" Target="grid4"/>
							</px:PXGrid>
						</Template1>
						<Template2>
							<px:PXGrid ID="grid4" runat="server" DataSourceID="ds" Style="z-index: 100" TabIndex="5700" Width="100%" Height="150px"
								SkinID="Details" Caption="Fields" CaptionVisible="true" AutoAdjustColumns="true" SyncPosition="true" AutoRefresh="true" MatrixMode="true">
								<Mode InitNewRow="True" AllowRowSelect="True"/>
								<Levels>
									<px:PXGridLevel DataMember="TrackingFieldsIC">
										<Columns>
											<px:PXGridColumn DataField="TableName" Width="200" Type="DropDownList" CommitChanges="True" />
											<px:PXGridColumn DataField="FieldName" Width="300" Type="DropDownList" CommitChanges="True" />
										</Columns>
									</px:PXGridLevel>
								</Levels>
								<AutoSize Container="Window" Enabled="True" MinHeight="150" />
                                <CallbackCommands>
                                    <InitRow CommitChanges="true" />
                                </CallbackCommands>
							</px:PXGrid>
						</Template2>
						<AutoSize Enabled="True" Container="Window" />
					</px:PXSplitContainer>
				</Template>
		    </px:PXTabItem>
        </Items>
	    <AutoSize Container="Window" Enabled="True" MinHeight="400" />
    </px:PXTab>
</asp:Content>
