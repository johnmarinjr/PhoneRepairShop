<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="FS100400.aspx.cs" Inherits="Page_FS100400" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/FormTab.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" Runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" 
        PrimaryView="RouteSetupRecord" SuspendUnloading="False" 
        TypeName="PX.Objects.FS.RouteSetupMaint">
		<CallbackCommands>
			<px:PXDSCallbackCommand CommitChanges="True" Name="Save" ></px:PXDSCallbackCommand>
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phF" runat="Server">
	<px:PXTab ID="tab" runat="server" Width="100%" DataSourceID="ds" DataMember="RouteSetupRecord">
	    <Items>
	      	<px:PXTabItem Text="General">
	      	<Template>
			<px:PXFormView ID="edRouteSetup" runat="server" DataMember="RouteSetupRecord" DataSourceID="ds" Width="100%">
				<Template>
					<px:PXLayoutRule runat="server" StartRow="True" LabelsWidth="XM" ControlSize="XM">
					</px:PXLayoutRule>
					<px:PXLayoutRule runat="server" GroupCaption="Numbering Settings" >
					</px:PXLayoutRule>
					<px:PXFormView ID="edEquipmentNumbering" runat="server" DataMember="SetupRecord" DataSourceID="ds" Width="100%" RenderStyle="Simple">
						<Template>
							<px:PXLayoutRule runat="server" StartRow="True" LabelsWidth="XM" ControlSize="XM">
							</px:PXLayoutRule>
							<px:PXSelector ID="edEquipmentNumberingID" runat="server" AllowEdit = "True"
								DataField="EquipmentNumberingID">
							</px:PXSelector>
						</Template>
					</px:PXFormView>
					<px:PXSelector ID="edRouteNumberingID" runat="server" 
						DataField="RouteNumberingID" AllowEdit = "True" >
					</px:PXSelector>
					<px:PXFormView ID="edSetupContractNumbering" runat="server" DataMember="SetupRecord" DataSourceID="ds" Width="100%" RenderStyle="Simple">
						<Template>
							<px:PXLayoutRule runat="server" StartRow="True" LabelsWidth="XM" ControlSize="XM">
							</px:PXLayoutRule>
							<px:PXSelector ID="edServiceContractNumberingID" runat="server" AllowEdit = "True"
								DataField="ServiceContractNumberingID">
							</px:PXSelector>
							<px:PXSelector ID="edScheduleNumberingID" runat="server" AllowEdit = "True"
								DataField="ScheduleNumberingID">
							</px:PXSelector>
						</Template>
					</px:PXFormView>
					<px:PXLayoutRule runat="server" GroupCaption="Contract Settings" >
					</px:PXLayoutRule>
					<px:PXCheckBox ID="edEnableSeasonScheduleContractRoute" runat="server" AlignLeft="True" DataField="EnableSeasonScheduleContract">
					</px:PXCheckBox>
					<px:PXLayoutRule runat="server" GroupCaption="Route Settings">
					</px:PXLayoutRule>   
					<px:PXSelector ID="edDfltSrvOrdType" runat="server" 
						DataField="DfltSrvOrdType" >                        
					</px:PXSelector>                      
					<px:PXCheckBox ID="edAutoCalculateRouteStats" runat="server" DataField="AutoCalculateRouteStats" AlignLeft="True">
					</px:PXCheckBox>
					<px:PXCheckBox ID="edGroupINDocumentsByPostingProcess" runat="server" DataField="GroupINDocumentsByPostingProcess" Text="Group IN documents by Posting process" AlignLeft="True" CommitChanges="True">
					</px:PXCheckBox> 
					<px:PXCheckBox ID="edSetFirstManualAppointment" runat="server" DataField="SetFirstManualAppointment" AlignLeft="True">
					</px:PXCheckBox> 
					<px:PXCheckBox ID="edTrackRouteLocation" runat="server" DataField="TrackRouteLocation" AlignLeft="True">
					</px:PXCheckBox>
					<px:PXLayoutRule 
						runat="server" 
						StartColumn="True" 
						GroupCaption="Billing Settings"
						LabelsWidth="XM" ControlSize="XM">
					</px:PXLayoutRule>
					<%-- Posting Settings Fields--%>
					<px:PXDropDown ID="edContractPostTo" runat="server" CommitChanges="True" DataField="SetupRecord.ContractPostTo">
			        </px:PXDropDown>
					<px:PXSelector ID="edContractPostOrderType" runat="server" AllowEdit="True" AutoRefresh="True" CommitChanges="True" DataField="SetupRecord.ContractPostOrderType" DataSourceID="ds">
					</px:PXSelector>
					<px:PXSelector ID="edDfltTermIDARSO" runat="server" AllowEdit="True" AutoRefresh="True" DataField="SetupRecord.DfltContractTermIDARSO" DataSourceID="ds">
					</px:PXSelector>
					<px:PXDropDown ID="edSalesAcctSource" runat="server" CommitChanges="True" DataField="SetupRecord.ContractSalesAcctSource">
					</px:PXDropDown>
					<px:PXSegmentMask ID="edCombineSubFrom" runat="server" CommitChanges="True" DataField="SetupRecord.ContractCombineSubFrom">
					</px:PXSegmentMask>
					<px:PXCheckBox ID="edEnableContractPeriodWhenInvoice" runat="server" DataField="SetupRecord.EnableContractPeriodWhenInvoice" AlignLeft="True">
					</px:PXCheckBox>
					 <%-- Posting Settingss Fields--%>
			</Template>
			<AutoSize Container="Window" Enabled="True" MinHeight="200" />
		</px:PXFormView>
		</Template>
      	</px:PXTabItem>
		<px:PXTabItem Text="Mailing && Printing">
            <Template>
                <px:PXSplitContainer runat="server" ID="sp1" SplitterPosition="350" 
                    SkinID="Horizontal" Height="500px" SavePosition="True">
                    <AutoSize Enabled="True" ></AutoSize>
                    <Template1>
                            <px:PXGrid ID="gridNS" runat="server" SkinID="DetailsInTab" Width="100%" Height="150px"
                                Caption="Default Sources" AdjustPageSize="Auto" AllowPaging="True" TabIndex="300"
                                DataSourceID="ds">
                                <AutoCallBack Target="gridNR" Command="Refresh" ></AutoCallBack>
                                <Levels>
                                    <px:PXGridLevel DataMember="Notifications">
                                        <RowTemplate>
                                            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" ></px:PXLayoutRule>
                                            <px:PXMaskEdit ID="edNotificationCD" runat="server" DataField="NotificationCD"></px:PXMaskEdit>
                                            <px:PXSelector ID="edNotificationID" runat="server" DataField="NotificationID" ValueField="Name"></px:PXSelector> 
                                            <px:PXDropDown ID="edFormat" runat="server" DataField="Format"></px:PXDropDown>  
                                            <px:PXCheckBox ID="chkActive" runat="server" DataField="Active"></px:PXCheckBox> 
                                            <px:PXSelector ID="edReportID" runat="server" DataField="ReportID" ValueField="ScreenID"></px:PXSelector>
                                            <px:PXSelector ID="edEMailAccount" runat="server" DataField="EMailAccountID" DisplayMode="Text"></px:PXSelector>
                                        </RowTemplate>
                                        <Columns>
                                            <px:PXGridColumn DataField="Active" TextAlign="Center" Type="CheckBox"></px:PXGridColumn>
                                            <px:PXGridColumn DataField="NotificationCD"></px:PXGridColumn>    
                                            <px:PXGridColumn DataField="EMailAccountID" DisplayMode="Text"></px:PXGridColumn>    
                                            <px:PXGridColumn DataField="ReportID" AutoCallBack="True"></px:PXGridColumn> 
                                            <px:PXGridColumn DataField="NotificationID" AutoCallBack="True"></px:PXGridColumn>
                                            <px:PXGridColumn DataField="Format" RenderEditorText="True" AutoCallBack="True"></px:PXGridColumn>
                                            <px:PXGridColumn DataField="RecipientsBehavior" CommitChanges="True" />
                                        </Columns>
                                    </px:PXGridLevel>
                                </Levels>
                                <AutoSize Enabled="True" ></AutoSize>
                            </px:PXGrid>
                    </Template1>
                    <Template2>
                            <px:PXGrid ID="gridNR" runat="server" SkinID="DetailsInTab" Width="100%" Caption="Default Recipients"
                                AdjustPageSize="Auto" AllowPaging="True" TabIndex="400" DataSourceID="ds">
                                <Parameters>
                                    <px:PXSyncGridParam ControlID="gridNS" ></px:PXSyncGridParam>
                                </Parameters>
                                <CallbackCommands>
                                    <Save CommitChangesIDs="gridNR" RepaintControls="None" RepaintControlsIDs="ds"></Save>
                                    <FetchRow RepaintControls="None" ></FetchRow>
                                </CallbackCommands>
                                <Levels>
                                    <px:PXGridLevel DataMember="Recipients" DataKeyNames="RecipientID">
                                        <Columns>
                                            <px:PXGridColumn DataField="Active" TextAlign="Center" Type="CheckBox">
                                            </px:PXGridColumn>
                                            <px:PXGridColumn DataField="ContactType" RenderEditorText="True" AutoCallBack="True">
                                            </px:PXGridColumn>
                                            <px:PXGridColumn DataField="OriginalContactID" Visible="False" AllowShowHide="False" ></px:PXGridColumn>
                                            <px:PXGridColumn DataField="ContactID">
                                                <NavigateParams>
                                                    <px:PXControlParam Name="ContactID" ControlID="gridNR" PropertyName="DataValues[&quot;OriginalContactID&quot;]" ></px:PXControlParam>
                                                </NavigateParams>
                                            </px:PXGridColumn>
                                            <px:PXGridColumn DataField="Format" RenderEditorText="True" AutoCallBack="True">
                                            </px:PXGridColumn>
                                            <px:PXGridColumn DataField="AddTo" />
                                        </Columns>
                                        <RowTemplate>
                                            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" ></px:PXLayoutRule>
                                            <px:PXSelector ID="edContactID" runat="server" DataField="ContactID" AutoRefresh="True"
                                                ValueField="DisplayName" AllowEdit="True">
                                            </px:PXSelector>
                                        </RowTemplate>
                                    </px:PXGridLevel>
                                </Levels>
                                <AutoSize Enabled="True" ></AutoSize>
                            </px:PXGrid>
                    </Template2>
                </px:PXSplitContainer>
            </Template>
        </px:PXTabItem>
    </Items>
    <AutoSize Container="Window" Enabled="True" MinHeight="150" />
  	</px:PXTab>
</asp:Content>
