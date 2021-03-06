<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="IN204500.aspx.cs"
    Inherits="Page_IN204500" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource EnableAttributes="true" ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.IN.INItemSiteMaint" PrimaryView="itemsiterecord">
        <CallbackCommands>
            <px:PXDSCallbackCommand Name="UpdateReplenishment" Visible="false" CommitChanges="true" />
            <px:PXDSCallbackCommand Name="ViewBOM" Visible="false" CommitChanges="true" />
            <px:PXDSCallbackCommand Name="ViewPlanningBOM" Visible="false" CommitChanges="true" />
        </CallbackCommands>
        <DataTrees>
            <px:PXTreeDataMember TreeView="_EPCompanyTree_Tree_" TreeKeys="WorkgroupID" />
        </DataTrees>
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" DataMember="itemsiterecord" Caption="Item/Warehouse Summary"
        NoteIndicator="True" FilesIndicator="True" ActivityIndicator="true" ActivityField="NoteActivity" DefaultControlID="edInventoryID">
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
            <px:PXSegmentMask ID="edInventoryID" runat="server" DataField="InventoryID" AllowEdit="True">
                <GridProperties>
                    <PagerSettings Mode="NextPrevFirstLast" />
                </GridProperties>
            </px:PXSegmentMask>
            <px:PXSegmentMask ID="edSiteID" runat="server" DataField="SiteID" AllowEdit="True" />
            <px:PXDropDown ID="edSiteStatus" runat="server" AllowNull="False" DataField="SiteStatus" />
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
			<px:PXCheckBox CommitChanges="True" ID="chkProductManagerOverride" runat="server" DataField="ProductManagerOverride" />
            <px:PXTreeSelector CommitChanges="True" ID="edProductWorkgroupID" runat="server" DataField="ProductWorkgroupID" TreeDataMember="_EPCompanyTree_Tree_"
                TreeDataSourceID="ds" PopulateOnDemand="true" InitialExpandLevel="0" ShowRootNode="false">
                <DataBindings>
                    <px:PXTreeItemBinding TextField="Description" ValueField="Description" />
                </DataBindings>
            </px:PXTreeSelector>
            <px:PXSelector ID="edProductManagerID" runat="server" DataField="ProductManagerID" AllowEdit="True" CommitChanges="True" />
        </Template>
    </px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXTab ID="tab" runat="server" Width="100%" Height="513px" DataSourceID="ds" DataMember="itemsitesettings" MarkRequired="Dynamic">
        <Activity HighlightColor="" SelectedColor="" Width="" Height=""></Activity>
        <Items>
            <px:PXTabItem Text="General" RepaintOnDemand="false">
                <Template>
                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
                    <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Storage Defaults" />
                    <px:PXSegmentMask CommitChanges="True" ID="edDfltShipLocationID" runat="server" DataField="DfltShipLocationID" AutoRefresh="True"
                        AllowEdit="True">
                        <Parameters>
                            <px:PXControlParam ControlID="form" Name="INItemSite.siteID" PropertyName="NewDataKey[&quot;SiteID&quot;]" Type="String" />
                        </Parameters>
                    </px:PXSegmentMask>
                    <px:PXSegmentMask CommitChanges="True" ID="edDfltReceiptLocationID" runat="server" DataField="DfltReceiptLocationID" AutoRefresh="True"
                        AllowEdit="True">
                        <Parameters>
                            <px:PXControlParam ControlID="form" Name="INItemSite.siteID" PropertyName="NewDataKey[&quot;SiteID&quot;]" Type="String" />
                        </Parameters>
                    </px:PXSegmentMask>
                    <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Physical Inventory" />
                    <px:PXCheckBox CommitChanges="True" ID="chkABCCodeOverride" runat="server" DataField="ABCCodeOverride" />
                    <px:PXSelector CommitChanges="True" ID="edABCCodeID" runat="server" DataField="ABCCodeID" />
                    <px:PXCheckBox ID="chkABCCodeIsFixed" runat="server" DataField="ABCCodeIsFixed" />
                    <px:PXCheckBox CommitChanges="True" ID="chkMovementClassOverride" runat="server" DataField="MovementClassOverride" />
                    <px:PXSelector CommitChanges="True" ID="edMovementClassID" runat="server" DataField="MovementClassID" AllowEdit="True" />
                    <px:PXCheckBox ID="chkMovementClassIsFixed" runat="server" DataField="MovementClassIsFixed" />
                    <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Item Defaults" />
	                <px:PXSelector runat="server" ID="edCountryOfOrigin" DataField="CountryOfOrigin" />
                    <px:PXLayoutRule runat="server" StartColumn="True" ControlSize="XM" LabelsWidth="SM" />
                    <px:PXLayoutRule runat="server" GroupCaption="GL Accounts" StartGroup="True" />
                    <px:PXCheckBox ID="chkOverrideInvtAcctSub" runat="server" DataField="OverrideInvtAcctSub" CommitChanges="true" />
                    <px:PXSegmentMask ID="edInvtAcctID" runat="server" DataField="InvtAcctID" AllowEdit="True" CommitChanges="true" />
                    <px:PXSegmentMask ID="edInvtSubID" runat="server" DataField="InvtSubID">
                        <Parameters>
                            <px:PXControlParam ControlID="tab" Name="INItemSite.invtAcctID" PropertyName="DataControls[&quot;edInvtAcctID&quot;].Value"
                                Type="String" />
                        </Parameters>
                    </px:PXSegmentMask>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Replenishment" RepaintOnDemand="False">
                <Template>
                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
                    <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Replenishment Settings" />
                    <px:PXSelector CommitChanges="True" ID="edReplenishmentClassID" runat="server" DataField="ReplenishmentClassID" />
                    <px:PXCheckBox CommitChanges="True" ID="chkReplenishmentPolicyOverride" runat="server" DataField="ReplenishmentPolicyOverride" />
                    <px:PXSelector CommitChanges="True" ID="edReplenishmentPolicyID" runat="server" DataField="ReplenishmentPolicyID" />
                    <px:PXDropDown CommitChanges="True" ID="edReplenishmentSource" runat="server" DataField="ReplenishmentSource" />
                    <px:PXDropDown CommitChanges="true" ID="edReplenishmentMethod" runat="server" DataField="ReplenishmentMethod"/>
                    <px:PXSegmentMask ID="edReplenishmentSourceSiteID" runat="server" DataField="ReplenishmentSourceSiteID" CommitChanges="true" />
                    <px:PXLayoutRule runat="server" Merge="True" />
                    <px:PXNumberEdit Size="xxs" ID="edMaxShelfLife" runat="server" DataField="MaxShelfLife" />
                    <px:PXCheckBox CommitChanges="True" ID="chkMaxShelfLifeOverride" runat="server" DataField="MaxShelfLifeOverride" />
                    <px:PXLayoutRule runat="server" />
                    <px:PXLayoutRule runat="server" Merge="True" />
                    <px:PXDateTimeEdit Size="s" ID="edLaunchDate" runat="server" DataField="LaunchDate" />
                    <px:PXCheckBox CommitChanges="True" ID="chkLaunchDateOverride" runat="server" DataField="LaunchDateOverride" />
                    <px:PXLayoutRule runat="server" />
                    <px:PXLayoutRule runat="server" Merge="True" />
                    <px:PXDateTimeEdit Size="s" ID="edTerminationDate" runat="server" DataField="TerminationDate" />
                    <px:PXCheckBox CommitChanges="True" ID="chkTerminationDateOverride" runat="server" DataField="TerminationDateOverride" />
					<px:PXLayoutRule runat="server" />
                    <px:PXLayoutRule runat="server" Merge="True" />
					<px:PXnumberEdit Size="s" ID="edServiceLevelPct" runat="server" DataField="ServiceLevelPct" />
                    <px:PXCheckBox CommitChanges="True" ID="chkServiceLevelOverride" runat="server" DataField="ServiceLevelOverride" />
                    <px:PXLayoutRule runat="server" />
                    <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Replenishment Parameters" />
                    <px:PXLayoutRule runat="server" Merge="True" />
                    <px:PXNumberEdit Size="xs" ID="edSafetyStock" runat="server" DataField="SafetyStock" />
                    <px:PXCheckBox CommitChanges="True" ID="chkSafetyStockOverride" runat="server" DataField="SafetyStockOverride" />

                    <px:PXLayoutRule runat="server" />
                    <px:PXLayoutRule runat="server" Merge="True" />
                    <px:PXNumberEdit Size="xs" ID="edMinQty" runat="server" DataField="MinQty" />
                    <px:PXCheckBox CommitChanges="True" ID="chkMinQtyOverride" runat="server" DataField="MinQtyOverride" />

                    <px:PXLayoutRule runat="server" />
                    <px:PXLayoutRule runat="server" Merge="True" />
                    <px:PXNumberEdit Size="xs" ID="edMaxQty" runat="server" DataField="MaxQty" />
                    <px:PXCheckBox CommitChanges="True" ID="chkMaxQtyOverride" runat="server" DataField="MaxQtyOverride" />

					<px:PXLayoutRule runat="server" />
                    <px:PXLayoutRule runat="server" Merge="True" />
                    <px:PXNumberEdit Size="xs" ID="edTransferERQ" runat="server" DataField="TransferERQ" />
                    <px:PXCheckBox CommitChanges="True" ID="chkTransferERQOverride" runat="server" DataField="TransferERQOverride" />
					
					<px:PXLayoutRule runat="server" />

					<px:PXLayoutRule runat="server" LabelsWidth="L" ControlSize="XM" />
                    <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Demand Forecast Result" />
					<px:PXNumberEdit ID="edDemandPerDayAverage" runat="server" AllowNull="True" DataField="DemandPerDayAverage" 
                        Enabled="False"/>
					<px:PXNumberEdit ID="edDemandPerDaySTDEV" runat="server" AllowNull="True" DataField="DemandPerDaySTDEV" 
                        Enabled="False"/>
					<px:PXNumberEdit ID="edLeadTimeAverage" runat="server" AllowNull="True"  DataField="LeadTimeAverage" Enabled="False"/>
					<px:PXNumberEdit ID="edLeadTimeSTDEV" runat="server" AllowNull="True" DataField="LeadTimeSTDEV" Enabled="False" />
					<px:PXNumberEdit ID="edSafetyStockSuggested" runat="server" AllowNull="True" DataField="SafetyStockSuggested" 
                        Enabled="False"/>
					<px:PXNumberEdit ID="edMinQtySuggested" runat="server" AllowNull="True" DataField="MinQtySuggested" Enabled="False"/>
					<px:PXDateTimeEdit ID="edLastForecastDate" runat="server" DataField="LastForecastDate" Enabled="False"/>
                    

                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
                    <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Preferred Parameters" />
                    <px:PXCheckBox CommitChanges="True" ID="chkPreferredVendorOverride" runat="server" DataField="PreferredVendorOverride" />
                    <px:PXSegmentMask CommitChanges="True" ID="edPreferredVendorID" runat="server" DataField="PreferredVendorID" AllowEdit="True" />
                    <px:PXSegmentMask ID="edPreferredVendorLocationID" runat="server" DataField="PreferredVendorLocationID" AutoRefresh="True" />
                    <px:PXSegmentMask ID="edInventoryItem__DefaultSubItemID" runat="server" DataField="InventoryItem__DefaultSubItemID" />

                    <px:PXFormView ID="preferredForm" runat="server" DataMember="PreferredVendorItem" CaptionVisible="False" 
                        DataSourceID="ds" RenderStyle="Simple">
                        <Template>
                            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
                            <px:PXNumberEdit ID="edVLeadTime" runat="server" DataField="VLeadTime" Enabled="False" />
                            <px:PXNumberEdit ID="edAddLeadTimeDays" runat="server" DataField="AddLeadTimeDays" Enabled="False" />
                            <px:PXNumberEdit ID="edMinOrdFreq" runat="server" DataField="MinOrdFreq" Enabled="False" />
                            <px:PXNumberEdit ID="edMinOrdQty" runat="server" DataField="MinOrdQty" Enabled="False" />
                            <px:PXNumberEdit ID="edMaxOrdQty" runat="server" DataField="MaxOrdQty" Enabled="False" />
                            <px:PXNumberEdit ID="edLotSize" runat="server" DataField="LotSize" Enabled="False" />
                            <px:PXNumberEdit ID="edERQ" runat="server" DataField="ERQ" Enabled="False" /></Template>
                    </px:PXFormView>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Subitem Replenishment Info" RepaintOnDemand="False" >
                <Template>
										<px:PXPanel runat="server" ID="pnlSubitem">
											<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
											<px:PXCheckBox CommitChanges="True" AlignLeft="true" ID="chkSubItemOverride" runat="server" DataField="SubItemOverride" />
										</px:PXPanel>
                    <px:PXGrid ID="subRepGrid" runat="server" DataSourceID="ds" SkinID="DetailsWithFilter" Width="100%" Height="200px">
                        <Mode InitNewRow="True" />
                        <AutoSize Enabled="True" />
                        <ActionBar>
                            <CustomItems>
                                <px:PXToolBarButton>
                                    <AutoCallBack Command="UpdateReplenishment" Target="ds" />
                                </px:PXToolBarButton>
                            </CustomItems>
                        </ActionBar>
                        <Levels>
                            <px:PXGridLevel DataMember="subitemrecords">
                                <Mode InitNewRow="True" />
                                <RowTemplate>
                                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
                                    <px:PXSegmentMask ID="edSubRInventoryID" runat="server" DataField="InventoryID" />
                                    <px:PXSegmentMask ID="edSubRSubItemID" runat="server" DataField="SubItemID" />
                                    <px:PXNumberEdit ID="edSubRSafetyStock" runat="server" DataField="SafetyStock" />
                                    <px:PXNumberEdit ID="edSubRMinQty" runat="server" DataField="MinQty" />
                                    <px:PXNumberEdit ID="edSubRMaxQty" runat="server" DataField="MaxQty" />
									<px:PXNumberEdit ID="edSubRTransferERQ" runat="server" DataField="TransferERQ" />
									
                                </RowTemplate>
                                <Columns>
                                    <px:PXGridColumn DataField="SubItemID" DisplayFormat="&gt;A" Label="Subitem" />
                                    <px:PXGridColumn AllowNull="False" DataField="SafetyStock" Label="Safety Stock" TextAlign="Right" Width="100px" />
                                    <px:PXGridColumn AllowNull="False" DataField="MinQty" Label="Reorder Point" TextAlign="Right" Width="100px" />
                                    <px:PXGridColumn AllowNull="False" DataField="MaxQty" Label="Max Qty." TextAlign="Right" Width="100px" />
									<px:PXGridColumn AllowNull="False" DataField="TransferERQ" Label="Transfer ERQ" TextAlign="Right" Width="100px" />
									<px:PXGridColumn AllowNull="False" DataField="SafetyStockSuggested" Label="Safety Stock Suggested" TextAlign="Right" Width="100px" />
                                    <px:PXGridColumn AllowNull="False" DataField="MinQtySuggested" Label="Reorder Point Suggested" TextAlign="Right" Width="100px" />
                                    <px:PXGridColumn AllowNull="False" DataField="MaxQtySuggested" Label="Max Qty. Suggested" TextAlign="Right" Width="100px" />
                                    <px:PXGridColumn AllowNull="False" DataField="DemandPerDayAverage" Label="Demand Per Day Average" TextAlign="Right" Width="100px" />
									<px:PXGridColumn AllowNull="False" DataField="DemandPerDaySTDEV" Label="Demand Per STDEV" TextAlign="Right" Width="100px" />
                                    <px:PXGridColumn AllowNull="False" DataField="ItemStatus" Label="Item Status" RenderEditorText="True" />
                                </Columns>
                                <Layout FormViewHeight="" />
                            </px:PXGridLevel>
                        </Levels>
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Price/Cost">
                <Template>
                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="M" ColumnWidth="XM" />
                    <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Standard Cost" />
                    <px:PXCheckBox CommitChanges="True" SuppressLabel="True" ID="chkStdCostOverride" runat="server" DataField="StdCostOverride" />
                    
                    <px:PXLayoutRule ID="edMergeLastStdCostLabel1" runat="server" Merge="true" />
                    <px:PXTextEdit ID="edLastStdCostLabel" runat="server" 
                                           Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                           DataField="LastStdCost_Label" 
                                           SuppressLabel="true" 
                                           Width="84px"  
                                           Enabled="false"
                                           IsClientControl="false" />
                    <px:PXNumberEdit ID="edLastStdCost" runat="server" DataField="LastStdCost" Enabled="False" CommitChanges="True" SuppressLabel="true" />
                    <px:PXLayoutRule ID="edMergeLastStdCostLabel2" runat="server" Merge="false" />

                    <px:PXLayoutRule ID="edMergeStdCost1" runat="server" Merge="true" />
                    <px:PXTextEdit ID="edStdCostLabel" runat="server" 
                                           Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                           DataField="StdCost_Label" 
                                           SuppressLabel="true" 
                                           Width="84px"  
                                           Enabled="false"
                                           IsClientControl="false" />
                    <px:PXNumberEdit ID="edStdCost" runat="server" DataField="StdCost" Enabled="False" SuppressLabel="true" />
                    <px:PXLayoutRule ID="edMergeStdCost2" runat="server" Merge="false" />

                    <px:PXDateTimeEdit ID="edStdCostDate" runat="server" DataField="StdCostDate" Enabled="False" />

                    <px:PXLayoutRule ID="edMergePendingStdCost1" runat="server" Merge="true" />
                    <px:PXTextEdit ID="edPendingStdCostLabel" runat="server" 
                                           Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                           DataField="PendingStdCost_Label" 
                                           SuppressLabel="true" 
                                           Width="84px"  
                                           Enabled="false"
                                           IsClientControl="false" />
                    <px:PXNumberEdit ID="edPendingStdCost" runat="server" DataField="PendingStdCost" SuppressLabel="true" />
                    <px:PXLayoutRule ID="edMergePendingStdCost2" runat="server" Merge="false" />
                    
                    <px:PXDateTimeEdit ID="edPendingStdCostDate" runat="server" DataField="PendingStdCostDate" />
                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="M" />
                    <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Price Management" />
                    <px:PXTreeSelector ID="edPriceWorkgroupID" runat="server" DataField="PriceWorkgroupID" TreeDataMember="_EPCompanyTree_Tree_"
                        TreeDataSourceID="ds" PopulateOnDemand="True" InitialExpandLevel="0" ShowRootNode="False">
                        <DataBindings>
                            <px:PXTreeItemBinding TextField="Description" ValueField="Description" />
                        </DataBindings>
                    </px:PXTreeSelector>
                    <px:PXSelector ID="edPriceManagerID" runat="server" DataField="PriceManagerID" AllowEdit="True" />
                    <px:PXCheckBox ID="chkCommisionable" runat="server" DataField="Commissionable" />
                    <px:PXCheckBox CommitChanges="True" SuppressLabel="True" ID="chkMarkupPctOverride" runat="server" DataField="MarkupPctOverride" />
                    <px:PXNumberEdit ID="edMarkupPct" runat="server" DataField="MarkupPct" />
                    <px:PXCheckBox CommitChanges="True" SuppressLabel="True" ID="chkRecPriceOverride" runat="server" DataField="RecPriceOverride" />
                    
                    <px:PXLayoutRule ID="edMergeRecPrice1" runat="server" Merge="true" />
                    <px:PXTextEdit ID="edRecPriceLabel" runat="server" 
                                           Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                           DataField="RecPrice_Label" 
                                           SuppressLabel="true" 
                                           Width="84px"  
                                           Enabled="false"
                                           IsClientControl="false" />
                    <px:PXNumberEdit ID="edRecPrice" runat="server" DataField="RecPrice" SuppressLabel="true" />
                    <px:PXLayoutRule ID="edMergeRecPrice2" runat="server" Merge="false" />
                    
                    <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Cost Statistics" />

                    <px:PXLayoutRule ID="edMergeLastCost1" runat="server" Merge="true" />
                    <px:PXTextEdit ID="edLastCostLabel" runat="server" 
                                           Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                           DataField="LastCost_Label" 
                                           SuppressLabel="true" 
                                           Width="84px"  
                                           Enabled="false"
                                           IsClientControl="false" />
                    <px:PXNumberEdit ID="edLastCost" runat="server" DataField="LastCost" SuppressLabel="true" />
                    <px:PXLayoutRule ID="edMergeLastCost2" runat="server" Merge="false" />

                    <px:PXLayoutRule ID="edMergeAvgCost1" runat="server" Merge="true" />
                    <px:PXTextEdit ID="edAvgCostLable" runat="server" 
                                           Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                           DataField="AvgCost_Label" 
                                           SuppressLabel="true" 
                                           Width="84px"  
                                           Enabled="false"
                                           IsClientControl="false" />
                    <px:PXNumberEdit ID="edAvgCost" runat="server" DataField="AvgCost" SuppressLabel="true" />
                    <px:PXLayoutRule ID="edMergeAvgCost2" runat="server" Merge="false" />

                    <px:PXLayoutRule ID="edMergeMinCost1" runat="server" Merge="true" />
                    <px:PXTextEdit ID="edMinCostLabel" runat="server" 
                                           Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                           DataField="MinCost_Label" 
                                           SuppressLabel="true" 
                                           Width="84px"  
                                           Enabled="false"
                                           IsClientControl="false" />
                    <px:PXNumberEdit ID="edMinCost" runat="server" DataField="MinCost" SuppressLabel="true" />
                    <px:PXLayoutRule ID="edMergeMinCost2" runat="server" Merge="false" />

                    <px:PXLayoutRule ID="edMergeMaxCost1" runat="server" Merge="true" />
                    <px:PXTextEdit ID="edMaxCostLabel" runat="server" 
                                           Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                           DataField="MaxCost_Label" 
                                           SuppressLabel="true" 
                                           Width="84px"  
                                           Enabled="false"
                                           IsClientControl="false" />
                    <px:PXNumberEdit ID="edMaxCost" runat="server" DataField="MaxCost" SuppressLabel="true" />
                    <px:PXLayoutRule ID="edMergeMaxCost2" runat="server" Merge="false" />
                </Template>
            </px:PXTabItem>
			<px:PXTabItem Text="Manufacturing">
				<Template>
					<px:PXLayoutRule runat="server" StartColumn="True" GroupCaption="General" StartGroup="True" />
					<px:PXLayoutRule runat="server" Merge="true" />
					<px:PXSelector runat="server" ID="edAMBOMID" DataField="AMBOMID"/>
                    <px:PXButton runat="server" ID="btnViewBOM" AlreadyLocalized="True" Style="min-width:20px; width:20px; 
                         border-style: none;padding-left:0px;padding-right:0px;height:20px;padding-top:0px; background-color:Transparent;" >
                        <Images Normal="main@RecordEdit" />
                        <AutoCallBack Command="ViewBOM" Target="ds" />
                    </px:PXButton>
                    <px:PXLayoutRule runat="server" />
                    <px:PXLayoutRule runat="server" Merge="true" />
					<px:PXSelector runat="server" ID="edAMPlanningBOMID" DataField="AMPlanningBOMID" />
                    <px:PXButton runat="server" ID="btnViewPlanningBOM" AlreadyLocalized="True" Style="min-width:20px; width:20px; 
                         border-style: none;padding-left:0px;padding-right:0px;height:20px;padding-top:0px; background-color:Transparent;" >
                        <Images Normal="main@RecordEdit" />
                        <AutoCallBack Command="ViewPlanningBOM" Target="ds" />
                    </px:PXButton>
                    <px:PXLayoutRule runat="server" />
					<px:PXSelector runat="server" ID="edAMConfigurationID" DataField="AMConfigurationID" AllowEdit="True" />
					<px:PXLayoutRule runat="server" Merge="True" />
					<px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Replenishments" />
					<px:PXLayoutRule runat="server" Merge="True" />
					<px:PXLayoutRule runat="server" />
					<px:PXLayoutRule runat="server" Merge="True" />
					<px:PXNumberEdit runat="server" CommitChanges="True" ID="edAMSafetyStock" DataField="AMSafetyStock" />
					<px:PXCheckBox runat="server" CommitChanges="True" DataField="AMSafetyStockOverride" ID="chkAMSafetyStockOverride" />
					<px:PXLayoutRule runat="server" />
					<px:PXLayoutRule runat="server" Merge="True" />
					<px:PXNumberEdit runat="server" CommitChanges="True" ID="edAMMinQty" DataField="AMMinQty" />
					<px:PXCheckBox runat="server" CommitChanges="True" DataField="AMMinQtyOverride" ID="chkAMMinQtyOverride" />
					<px:PXLayoutRule runat="server" />
					<px:PXLayoutRule runat="server" Merge="True" />
					<px:PXNumberEdit runat="server" ID="edAMMinOrdQty" DataField="AMMinOrdQty" />
					<px:PXCheckBox runat="server" CommitChanges="True" DataField="AMMinOrdQtyOverride" ID="chkAMMinOrdQtyOverride" />
					<px:PXLayoutRule runat="server" />
					<px:PXLayoutRule runat="server" Merge="True" />
					<px:PXNumberEdit runat="server" ID="edAMMaxOrdQty" DataField="AMMaxOrdQty" />
					<px:PXCheckBox runat="server" CommitChanges="True" DataField="AMMaxOrdQtyOverride" ID="chkAMMaxOrdQtyOverride" />
					<px:PXLayoutRule runat="server" />
					<px:PXLayoutRule runat="server" Merge="True" />
					<px:PXNumberEdit runat="server" ID="edAMLotSize" DataField="AMLotSize" />
					<px:PXCheckBox runat="server" CommitChanges="True" DataField="AMLotSizeOverride" ID="chkAMLotSizeOverride" />
					<px:PXLayoutRule runat="server" />
					<px:PXLayoutRule runat="server" Merge="True" />
					<px:PXNumberEdit runat="server" ID="edAMMFGLeadTime" DataField="AMMFGLeadTime" />
					<px:PXCheckBox runat="server" CommitChanges="True" DataField="AMMFGLeadTimeOverride" ID="chkAMMFGLeadTimeOverride" />
					<px:PXLayoutRule runat="server" StartColumn="True" StartGroup="True" GroupCaption="Scrap" />
					<px:PXLayoutRule runat="server" LabelsWidth="M" ColumnWidth="M" />
					<px:PXCheckBox runat="server" DataField="AMScrapOverride" ID="edAMScrapOverride" CommitChanges="True" />
					<px:PXSegmentMask runat="server" ID="edAMScrapSiteID" DataField="AMScrapSiteID" AllowEdit="True" CommitChanges="True" AutoRefresh="True" />
					<px:PXSegmentMask runat="server" ID="edAMScrapLocationID" DataField="AMScrapLocationID" AllowEdit="True" AutoRefresh="True" CommitChanges="True" />
					<px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="MRP Consolidation" LabelsWidth="M" ColumnWidth="M" />
                    <px:PXLayoutRule runat="server" Merge="True" />
					<px:PXNumberEdit runat="server" ID="edAMGroupWindow" DataField="AMGroupWindow" />
					<px:PXCheckBox runat="server" CommitChanges="True" DataField="AMGroupWindowOverride" ID="chkAMGroupWindowOverride" />
					<px:PXLayoutRule runat="server" LabelsWidth="S" ControlSize="XM" />
					<px:PXGrid runat="server" Height="150px" SkinID="Attributes" Width="450px" ID="AMGridAMSubItemDefault" Caption="Sub Item Defaults" MatrixMode="False" DataSourceID="ds" SyncPosition="True">
						<Levels>
							<px:PXGridLevel DataMember="AMSubItemDefaults" DataKeyNames="SiteID,InventoryID,SubItemID">
								<RowTemplate>
									<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
									<px:PXSegmentMask runat="server" DataField="SubItemID" ID="edSubItemAMSubItemID" />
									<px:PXSelector runat="server" DataField="BOMID" ID="edSubItemAMBOMID" AutoRefresh="True" />
									<px:PXSelector runat="server" DataField="PlanningBOMID" ID="edSubItemAMPlanningBOMID" AutoRefresh="True" /></RowTemplate>
								<Columns>
									<px:PXGridColumn DataField="SubItemID" />
									<px:PXGridColumn DataField="BOMID" />
									<px:PXGridColumn DataField="PlanningBOMID" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
		</Items>
        <AutoSize Container="Window" Enabled="True" MinHeight="150" />
    </px:PXTab>
</asp:Content>
