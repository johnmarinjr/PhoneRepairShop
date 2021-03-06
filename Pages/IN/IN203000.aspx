<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="IN203000.aspx.cs" Inherits="Page_IN203000" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.IN.Matrix.Graphs.TemplateInventoryItemMaint" PrimaryView="Item">
        <CallbackCommands>
            <px:PXDSCallbackCommand StartNewGroup="True" Name="Action" CommitChanges="true" />
            <px:PXDSCallbackCommand Name="Inquiry" />
            <px:PXDSCallbackCommand Name="AddWarehouseDetail" Visible="false" CommitChanges="true" />
            <px:PXDSCallbackCommand Name="UpdateReplenishment" Visible="false" CommitChanges="true" DependOnGrid="repGrid" />
            <px:PXDSCallbackCommand Name="GenerateSubitems" Visible="false" CommitChanges="true" DependOnGrid="repGrid" />
            <px:PXDSCallbackCommand Name="ViewGroupDetails" Visible="False" DependOnGrid="grid3" />
            <px:PXDSCallbackCommand Name="syncSalesforce" Visible="false" />
            <px:PXDSCallbackCommand DependOnGrid="PXGridIdGenerationRules" Name="IdRowUp" Visible="False" />
            <px:PXDSCallbackCommand DependOnGrid="PXGridIdGenerationRules" Name="IdRowDown" Visible="False" />	
            <px:PXDSCallbackCommand DependOnGrid="PXGridDescriptionGenerationRules" Name="DescriptionRowUp" Visible="False" />
            <px:PXDSCallbackCommand DependOnGrid="PXGridDescriptionGenerationRules" Name="DescriptionRowDown" Visible="False" />	
            <px:PXDSCallbackCommand CommitChanges="True" Visible="False" Name="DeleteItems" DependOnGrid="grdMatrixItems" />
            <px:PXDSCallbackCommand CommitChanges="True" Visible="false" Name="ViewMatrixItem" DependOnGrid="grdMatrixItems" />
            <px:PXDSCallbackCommand Name="CreateMatrixItems" CommitChanges="true" StartNewGroup="true" />
            <px:PXDSCallbackCommand Name="ApplyToItems" CommitChanges="true" />
            <px:PXDSCallbackCommand Name="CreateUpdate" CommitChanges="true" Visible="false" />
            
        </CallbackCommands>
        <DataTrees>
            <px:PXTreeDataMember TreeView="_EPCompanyTree_Tree_" TreeKeys="WorkgroupID" />
            <px:PXTreeDataMember TreeView="EntityItems" TreeKeys="Key" />
            <px:PXTreeDataMember TreeKeys="CategoryID" TreeView="Categories" />
        </DataTrees>
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
    <px:PXSmartPanel ID="pnlChangeID" runat="server" Caption="Specify New ID"
        CaptionVisible="true" DesignView="Hidden" LoadOnDemand="true" Key="ChangeIDDialog" CreateOnDemand="false" AutoCallBack-Enabled="true"
        AutoCallBack-Target="formChangeID" AutoCallBack-Command="Refresh" CallBackMode-CommitChanges="True" CallBackMode-PostData="Page"
        AcceptButtonID="btnOK">
        <px:PXFormView ID="formChangeID" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" CaptionVisible="False"
            DataMember="ChangeIDDialog">
            <ContentStyle BackColor="Transparent" BorderStyle="None" />
            <Template>
                <px:PXLayoutRule ID="rlAcctCD" runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
                <px:PXSegmentMask ID="edAcctCD" runat="server" DataField="CD" />
            </Template>
        </px:PXFormView>
        <px:PXPanel ID="pnlChangeIDButton" runat="server" SkinID="Buttons">
            <px:PXButton ID="btnOK" runat="server" DialogResult="OK" Text="OK">
                <AutoCallBack Target="formChangeID" Command="Save" />
            </px:PXButton>
			<px:PXButton ID="btnCancel" runat="server" DialogResult="Cancel" Text="Cancel" />
        </px:PXPanel>
    </px:PXSmartPanel>
    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" DataMember="Item" Caption="Stock Item Summary" NoteIndicator="True" FilesIndicator="True" ActivityIndicator="True"
        ActivityField="NoteActivity" DefaultControlID="edInventoryCD">
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
            <px:PXSegmentMask ID="edInventoryCD" runat="server" DataField="InventoryCD" DataSourceID="ds" AutoRefresh="true" >
                <GridProperties FastFilterFields="InventoryCD,Descr" />
			</px:PXSegmentMask>

			<px:PXLayoutRule runat="server" ColumnSpan="2" />
            <px:PXTextEdit ID="edDescr" runat="server" DataField="Descr" />
            
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
			<px:PXCheckBox ID="edStkItem" runat="server" DataField="StkItem" CommitChanges="true" Size="XXL" />
        </Template>
    </px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
    <px:PXTab ID="tab" runat="server" Width="100%" Height="606px" DataSourceID="ds" DataMember="ItemSettings" FilesIndicator="False" NoteIndicator="False">
        <AutoSize Enabled="True" Container="Window" MinHeight="150" />
        <Items>
            <px:PXTabItem Text="General">
                <Template>
                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
                    <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Item Defaults" />
					<px:PXDropDown ID="edItemStatus" runat="server" DataField="ItemStatus" />
                    <px:PXSegmentMask CommitChanges="True" ID="edItemClassID" runat="server" DataField="ItemClassID" AllowEdit="True" AutoRefresh="true" />
                    <px:PXDropDown ID="edItemType" runat="server" DataField="ItemType" />
                    <px:PXDropDown CommitChanges="True" ID="edValMethod" runat="server" DataField="ValMethod" />
                    <px:PXSelector ID="edTaxCategoryID" runat="server" DataField="TaxCategoryID" AllowEdit="True" CommitChanges="True" AutoRefresh="True" />
                    <px:PXSelector CommitChanges="True" ID="edPostClassID" runat="server" DataField="PostClassID" AllowEdit="True" />
                    <px:PXSelector CommitChanges="True" ID="edLotSerClassID" runat="server" DataField="LotSerClassID" AllowEdit="True" />
                    <px:PXSelector runat="server" ID="edCountryOfOrigin" DataField="CountryOfOrigin" />
                    <px:PXCheckBox ID="chkNonStockReceipt" runat="server" Checked="True" DataField="NonStockReceipt" CommitChanges="true" />
                    <px:PXCheckBox ID="chkNonStockReceiptAsService" runat="server" DataField="NonStockReceiptAsService" CommitChanges="true" />
                    <px:PXCheckBox ID="chkNonStockShip" runat="server" Checked="True" DataField="NonStockShip" />
                    <px:PXDropDown ID="edCompletePOLine" runat="server" DataField="CompletePOLine" />

                    <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Field Service Defaults" />
                    <px:PXMaskEdit runat="server" ID="edEstimatedDuration" DataField="EstimatedDuration" />
                    <px:PXCheckBox runat="server" ID="edRouteService" DataField="ItemClass.Mem_RouteService" Enabled="False" />

                    <px:PXLayoutRule runat="server" StartGroup="True" />
                    <px:PXFormView ID="CurySettingsFormDefaultSite" runat="server" SkinID="Inside" RenderStyle="Fieldset" DataSourceID="ds" DataMember="CurySettings_InventoryItem" Caption="Warehouse Defaults">
                        <Template>
                            <px:PXLayoutRule runat="server"  LabelsWidth="SM" ControlSize="XM" />
                            <px:PXSegmentMask CommitChanges="True" ID="edDfltSiteID" runat="server" DataField="DfltSiteID" AllowEdit="True" />
                            <px:PXSegmentMask CommitChanges="True" ID="edDfltShipLocationID" runat="server" DataField="DfltShipLocationID" AutoRefresh="True" AllowEdit="True" />
                            <px:PXSegmentMask CommitChanges="True" ID="edDfltReceiptLocationID" runat="server" DataField="DfltReceiptLocationID" AutoRefresh="True" AllowEdit="True" />
                        </Template>
                    </px:PXFormView>

                    <px:PXLayoutRule runat="server" Merge="True" />
                    <px:PXSegmentMask Size="s" ID="edDefaultSubItemID" runat="server" DataField="DefaultSubItemID" AutoRefresh="True" />
                    <px:PXCheckBox ID="chkDefaultSubItemOnEntry" runat="server" DataField="DefaultSubItemOnEntry" />
                    <px:PXLayoutRule runat="server" />
                    <px:PXLayoutRule ID="PXLayoutRule2" runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
                    <px:PXLayoutRule runat="server" GroupCaption="Unit of Measure" StartGroup="True" />
					<px:PXLayoutRule runat="server" Merge="true" />
                    <px:PXSelector ID="edBaseUnit" Size="s" runat="server" AllowEdit="True" CommitChanges="True" DataField="BaseUnit" Style="margin-right:30px"/>
					<px:PXCheckBox ID="chkDecimalBaseUnit" runat="server" DataField="DecimalBaseUnit" CommitChanges="True"/>
					<px:PXLayoutRule runat="server" Merge="true" />
                    <px:PXSelector ID="edSalesUnit" Size="s" runat="server" AllowEdit="True" AutoRefresh="True" CommitChanges="True" DataField="SalesUnit" Style="margin-right:30px"/>
					<px:PXCheckBox ID="chkDecimalSalesUnit" runat="server" DataField="DecimalSalesUnit" CommitChanges="True" />
					<px:PXLayoutRule runat="server" Merge="true" />
                    <px:PXSelector ID="edPurchaseUnit" Size="s" runat="server" AllowEdit="True" AutoRefresh="True" CommitChanges="True" DataField="PurchaseUnit" Style="margin-right:30px"/>
					<px:PXCheckBox ID="chkDecimalPurchaseUnit" runat="server" DataField="DecimalPurchaseUnit" CommitChanges="True" />
					<px:PXLayoutRule runat="server"/>
                    <px:PXLayoutRule runat="server" LabelsWidth="SM" ControlSize="S" SuppressLabel="True" />
                    <px:PXGrid ID="gridUnits" runat="server" DataSourceID="ds" SkinID="ShortList" Width="400px" Height="114px" Caption="Conversions" CaptionVisible="false">
                        <Mode InitNewRow="True" />
                        <Levels>
                            <px:PXGridLevel DataMember="itemunits">
                                <RowTemplate>
                                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
                                    <px:PXNumberEdit ID="edItemClassID2" runat="server" DataField="ItemClassID" />
                                    <px:PXNumberEdit ID="edInventoryID" runat="server" DataField="InventoryID" />
                                    <px:PXMaskEdit ID="edFromUnit" runat="server" DataField="FromUnit" />
                                    <px:PXMaskEdit ID="edSampleToUnit" runat="server" DataField="SampleToUnit" />
                                    <px:PXNumberEdit ID="edUnitRate" runat="server" DataField="UnitRate" />
                                </RowTemplate>
                                <Columns>
                                    <px:PXGridColumn DataField="UnitType" Type="DropDownList" Width="99px" Visible="False" />
                                    <px:PXGridColumn DataField="ItemClassID" Width="36px" Visible="False" />
                                    <px:PXGridColumn DataField="InventoryID" Visible="False" TextAlign="Right" Width="54px" />
                                    <px:PXGridColumn DataField="FromUnit" Width="72px" CommitChanges="True" />
                                    <px:PXGridColumn DataField="UnitMultDiv" Type="DropDownList" Width="90px" CommitChanges="True" />
                                    <px:PXGridColumn DataField="UnitRate" TextAlign="Right" Width="108px" CommitChanges="True" />
                                    <px:PXGridColumn DataField="SampleToUnit" Width="72px" />
                                    <px:PXGridColumn DataField="PriceAdjustmentMultiplier" TextAlign="Right" Width="108px" CommitChanges="True" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <Layout ColumnsMenu="False" />
                    </px:PXGrid>
                    <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Physical Inventory" />
                    <px:PXSelector CommitChanges="True" ID="edPICycleID" runat="server" DataField="CycleID" AllowEdit="True" />
                    <px:PXSelector CommitChanges="True" ID="edABCCodeID" runat="server" DataField="ABCCodeID" AllowEdit="True" />
                    <px:PXCheckBox SuppressLabel="True" ID="chkABCCodeIsFixed" runat="server" DataField="ABCCodeIsFixed" />
                    <px:PXSelector CommitChanges="True" ID="edMovementClassID" runat="server" DataField="MovementClassID" AllowEdit="True" />
                    <px:PXCheckBox SuppressLabel="True" ID="chkMovementClassIsFixed" runat="server" DataField="MovementClassIsFixed" />
                </Template>
            </px:PXTabItem>
			<px:PXTabItem Text="Fulfillment">
                <Template>
                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="SM" />
                    <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Dimensions" />
                    <px:PXNumberEdit ID="edBaseItemWeight" runat="server" DataField="BaseItemWeight" />
                    <px:PXSelector ID="edWeightUOM" runat="server" DataField="WeightUOM" Size="S" AutoRefresh="true" />
                    <px:PXNumberEdit ID="edBaseItemVolume" runat="server" DataField="BaseItemVolume" />
                    <px:PXSelector ID="edVolumeUOM" runat="server" DataField="VolumeUOM" Size="S" AutoRefresh="true" />
                    <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="International Shipping" />
                    <px:PXTextEdit runat="server" ID="edHSTariffCode" DataField="HSTariffCode" />
					<px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Shipping Thresholds" />
					<px:PXNumberEdit ID="edUndershipThreshold" runat="server" DataField="UndershipThreshold" />
					<px:PXNumberEdit ID="edOvershipThreshold" runat="server" DataField="OvershipThreshold" />
					<px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Sales Categories" />
					<px:PXGrid ID="PXGridCategory" runat="server" DataSourceID="ds" Height="220px" Width="250px"
                        SkinID="ShortList" MatrixMode="False">
                        <Levels>
                            <px:PXGridLevel DataMember="Category">
                                <RowTemplate>
                                    <px:PXLayoutRule ID="PXLayoutRule2" runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
                                    <px:PXTreeSelector ID="edParent" runat="server" DataField="CategoryID" PopulateOnDemand="True"
                                        ShowRootNode="False" TreeDataSourceID="ds" TreeDataMember="Categories" CommitChanges="true">
                                        <DataBindings>
                                            <px:PXTreeItemBinding TextField="Description" ValueField="CategoryID" />
                                        </DataBindings>
                                    </px:PXTreeSelector>
                                </RowTemplate>
                                <Columns>
                                    <px:PXGridColumn DataField="CategoryID" Width="220px" TextField="INCategory__Description" AllowResize="False"/>
                                </Columns>
                                <Layout FormViewHeight="" />
                            </px:PXGridLevel>
                        </Levels>
                    </px:PXGrid>
                    <px:PXLayoutRule runat="server" StartColumn="True" />
                    <px:PXLayoutRule ID="PXLayoutRule5" runat="server" StartGroup="True" GroupCaption="Automatic Packaging" />
                    <px:PXLayoutRule ID="PXLayoutRule6" runat="server" Merge="True" />
                    <px:PXDropDown ID="edPackageOption" runat="server" DataField="PackageOption" CommitChanges="true" AllowNull="False" />
                    <px:PXCheckBox ID="edPackSeparately" DataField="PackSeparately" runat="server" />
                    <px:PXLayoutRule ID="PXLayoutRule7" runat="server" Merge="False" />
                    <px:PXGrid ID="PXGridBoxes" runat="server" Caption="Boxes" DataSourceID="ds" Height="130px" Width="420px" SkinID="ShortList" FilesIndicator="False" NoteIndicator="false">
                        <Levels>
                            <px:PXGridLevel DataMember="Boxes">
                                <RowTemplate>
                                    <px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="SM" StartColumn="True" />
                                    <px:PXSelector ID="edBoxID" runat="server" DataField="BoxID" />
                                    <px:PXSelector ID="edUOM_box" runat="server" DataField="UOM" />
                                    <px:PXNumberEdit ID="edQty_box" runat="server" DataField="Qty" />
                                </RowTemplate>
                                <Columns>
                                    <px:PXGridColumn DataField="BoxID" Width="91px" CommitChanges="True" />
                                    <px:PXGridColumn DataField="Description" Width="91px" />
                                    <px:PXGridColumn DataField="UOM" Width="54px" CommitChanges="True" />
                                    <px:PXGridColumn DataField="Qty" TextAlign="Right" Width="54px" />
                                    <px:PXGridColumn DataField="MaxWeight" Width="54px" />
                                    <px:PXGridColumn DataField="MaxVolume" Width="54px" />
                                    <px:PXGridColumn DataField="MaxQty" TextAlign="Right" Width="54px" />
                                </Columns>
                                <Layout FormViewHeight="" />
                            </px:PXGridLevel>
                        </Levels>
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Price/Cost">
                <Template>
                    <px:PXLayoutRule runat="server" StartColumn="True" StartGroup="True" ControlSize="XM" GroupCaption="Price Management" />
                    <px:PXSelector ID="edPriceClassID" runat="server" DataField="PriceClassID" AllowEdit="True" />
                    <px:PXSelector CommitChanges="True" ID="edPriceWorkgroupID" runat="server" DataField="PriceWorkgroupID" ShowRootNode="False" />
                    <px:PXSelector ID="edPriceManagerID" runat="server" DataField="PriceManagerID" AutoRefresh="True" CommitChanges="True"/>
                    <px:PXCheckBox SuppressLabel="True" ID="chkCommisionable" runat="server" DataField="Commisionable" />
                    <px:PXNumberEdit ID="edMinGrossProfitPct" runat="server" DataField="MinGrossProfitPct" />
                    <px:PXNumberEdit ID="edMarkupPct" runat="server" DataField="MarkupPct" />
                    
                    <px:PXFormView ID="curySettingsForm" runat="server" SkinID="Inside" RenderStyle="simple" DataSourceID="ds" DataMember="CurySettings_InventoryItem" CaptionVisible="false">
                        <Template>
                            <px:PXLayoutRule ID="PXLayoutRule3" runat="server" StartColumn="true" ControlSize="XM" />
                            <px:PXLayoutRule ID="edMerge01" runat="server" Merge="true" />
                            <px:PXTextEdit ID="edRecPriceLabel" runat="server" 
                                           Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                           DataField="RecPrice_Label" 
                                           SuppressLabel="true" 
                                           Width="104px"  
                                           Enabled="false"
                                           IsClientControl="false" />
                            <px:PXNumberEdit ID="edRecPrice" runat="server" DataField="RecPrice" SuppressLabel="true" />
                            <px:PXLayoutRule ID="edMerge02" runat="server" Merge="false" />
                            <px:PXLayoutRule ID="edMerge11" runat="server" Merge="true" />
                            <px:PXTextEdit ID="edBasePriceLabel" runat="server" 
                                           Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                           DataField="BasePrice_Label" 
                                           SuppressLabel="true" 
                                           Width="104px"  
                                           Enabled="false"
                                           IsClientControl="false" />
                            <px:PXNumberEdit ID="edBasePrice" runat="server" DataField="BasePrice" Enabled="true" SuppressLabel="true" />
                            <px:PXLayoutRule ID="edMerge12" runat="server" Merge="false" />
                        </Template>
                    </px:PXFormView>

                    <px:PXLayoutRule runat="server" StartColumn="True" ControlSize="XM" StartGroup="True" GroupCaption="Standard Cost" />
                    <px:PXFormView ID="curySettingsForm2" runat="server" SkinID="Inside" RenderStyle="simple" DataSourceID="ds" DataMember="CurySettings_InventoryItem" CaptionVisible="false">
                        <Template>
                            <px:PXLayoutRule ID="edMerge21" runat="server" Merge="true" />
                            <px:PXTextEdit ID="edPendingStdCostLabel" runat="server" 
                                           Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                           DataField="PendingStdCost_Label" 
                                           SuppressLabel="true" 
                                           Width="104px"  
                                           Enabled="false"
                                           IsClientControl="false" />
                            <px:PXNumberEdit ID="edPendingStdCost" runat="server" DataField="PendingStdCost" SuppressLabel="true" />
                            <px:PXLayoutRule ID="edMerge22" runat="server" Merge="false" />
                            
                            <px:PXDateTimeEdit ID="edPendingStdCostDate" runat="server" DataField="PendingStdCostDate" />
                            <px:PXLayoutRule ID="edMerge31" runat="server" Merge="true" />
                            <px:PXTextEdit ID="edStdCostLabel" runat="server" 
                                           Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                           DataField="StdCost_Label" 
                                           SuppressLabel="true" 
                                           Width="104px"  
                                           Enabled="false"
                                           IsClientControl="false" />
                            <px:PXNumberEdit ID="edStdCost" runat="server" DataField="StdCost" SuppressLabel="true" />
                            <px:PXLayoutRule ID="edMerge32" runat="server" Merge="false" />
                            
                            <px:PXDateTimeEdit ID="edStdCostDate" runat="server" DataField="StdCostDate" Enabled="False" />
                            <px:PXLayoutRule ID="edMerge41" runat="server" Merge="true" />
                            <px:PXTextEdit ID="edLastStdCostLabel" runat="server" 
                                           Style="background-color: transparent; border-width:0px; padding-left:0px; color:#5c5c5c"
                                           DataField="CurySettings_InventoryItem.LastStdCost_Label" 
                                           SuppressLabel="true" 
                                           Width="104px"  
                                           Enabled="false"
                                           IsClientControl="false" />
                            <px:PXNumberEdit ID="edLastStdCost" runat="server" DataField="CurySettings_InventoryItem.LastStdCost" SuppressLabel="true" />
                            <px:PXLayoutRule ID="edMerge42" runat="server" Merge="false" />
                        </Template>
                    </px:PXFormView>

					<px:PXLayoutRule runat="server" ID="PXLayoutRuleA1" StartGroup="true" GroupCaption="Cost Accrual" ControlSize="XM" />
					<px:PXCheckBox ID="chkAccrueCost" runat="server" DataField="AccrueCost" CommitChanges="True" />
                    <px:PXDropDown ID="edCostBasis" runat="server" DataField="CostBasis" CommitChanges="true" />
					<px:PXNumberEdit ID="edPercentOfSalesPrice" runat="server" DataField="PercentOfSalesPrice" />
                    <px:PXLayoutRule runat="server" StartGroup="True" GroupCaption="Field Service Defaults" />
                    <px:PXSelector runat="server" ID="edDfltEarningType" DataField="DfltEarningType" />
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Vendors" LoadOnDemand="true">
                <Template>
                    <px:PXGrid ID="PXGridVendorItems" runat="server" DataSourceID="ds" Height="100%" Width="100%" SkinID="DetailsInTab" SyncPosition="true">
                        <Mode InitNewRow="True" />
                        <Levels>
                            <px:PXGridLevel DataMember="VendorItems" DataKeyNames="RecordID">
                                <RowTemplate>
                                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
                                    <px:PXSegmentMask ID="edVendorID" runat="server" DataField="VendorID" AllowEdit="True" />
                                    <px:PXSegmentMask Size="xxs" ID="vp_edSubItemID" runat="server" DataField="SubItemID" AutoRefresh="True" />
                                    <px:PXSegmentMask ID="edLocation__VSiteID" runat="server" DataField="Location__VSiteID" AllowEdit="true" />
                                    <px:PXSegmentMask ID="edVendorLocationID" runat="server" DataField="VendorLocationID" AutoRefresh="True" AllowEdit="True" />
                                    <px:PXNumberEdit ID="edAddLeadTimeDays" runat="server" DataField="AddLeadTimeDays" />
                                    <px:PXCheckBox ID="vp_chkActive" runat="server" Checked="True" DataField="Active" />
                                    <px:PXNumberEdit ID="edMinOrdFreq" runat="server" DataField="MinOrdFreq" />
                                    <px:PXNumberEdit ID="edMinOrdQty" runat="server" DataField="MinOrdQty" />
                                    <px:PXNumberEdit ID="edMaxOrdQty" runat="server" DataField="MaxOrdQty" />
                                    <px:PXNumberEdit ID="edLotSize" runat="server" DataField="LotSize" />
                                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
                                    <px:PXNumberEdit ID="edERQ" runat="server" DataField="ERQ" />
                                    <px:PXSelector ID="edCuryID" runat="server" DataField="CuryID" />
                                    <px:PXNumberEdit ID="edLastPrice" runat="server" DataField="LastPrice" Enabled="False" />
                                    <px:PXCheckBox ID="chkIsDefault" runat="server" DataField="IsDefault" />
                                    <px:PXTextEdit ID="edVendor__AcctName" runat="server" DataField="Vendor__AcctName" />
                                    <px:PXNumberEdit ID="edLocation__VLeadTime" runat="server" DataField="Location__VLeadTime" />
                                </RowTemplate>
                                <Columns>
                                    <px:PXGridColumn DataField="Active" TextAlign="Center" Type="CheckBox" Width="45px" />
                                    <px:PXGridColumn DataField="IsDefault" TextAlign="Center" Type="CheckBox" Width="45px" />
                                    <px:PXGridColumn DataField="VendorID" Width="81px" AutoCallBack="True" />
                                    <px:PXGridColumn DataField="Vendor__AcctName" Width="210px" />
                                    <px:PXGridColumn DataField="VendorLocationID" Width="54px" AutoCallBack="True" />
                                    <px:PXGridColumn DataField="Location__VSiteID" Width="81px" />
                                    <px:PXGridColumn DataField="SubItemID" AutoCallBack="True" />
                                    <px:PXGridColumn DataField="PurchaseUnit" Width="63px" />
                                    <px:PXGridColumn DataField="Location__VLeadTime" Width="90px" TextAlign="Right" />
                                    <px:PXGridColumn DataField="OverrideSettings" TextAlign="Center" Type="CheckBox" Width="60px" AutoCallBack="True" />
                                    <px:PXGridColumn DataField="AddLeadTimeDays" TextAlign="Right" Width="90px" />
                                    <px:PXGridColumn DataField="MinOrdFreq" TextAlign="Right" Width="84px" />
                                    <px:PXGridColumn DataField="MinOrdQty" TextAlign="Right" Width="81px" />
                                    <px:PXGridColumn DataField="MaxOrdQty" TextAlign="Right" Width="81px" />
                                    <px:PXGridColumn DataField="LotSize" TextAlign="Right" Width="81px" />
                                    <px:PXGridColumn DataField="ERQ" TextAlign="Right" Width="81px" />
                                    <px:PXGridColumn DataField="CuryID" Width="54px" />
                                    <px:PXGridColumn DataField="LastPrice" TextAlign="Right" Width="99px" />
                                    <px:PXGridColumn DataField="PrepaymentPct" TextAlign="Right" AllowNull="True" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Enabled="True" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="GL Accounts">
                <Template>
                    <px:PXLayoutRule ID="PXLayoutRule4" runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
                    <px:PXSegmentMask ID="edInvtAcctID" runat="server" DataField="InvtAcctID" CommitChanges="true" AutoRefresh="true" />
					<px:PXSegmentMask ID="edNonStockInvtAcctID" runat="server" DataField="ExpenseAccrualAcctID" CommitChanges="true" AutoRefresh="true" />
                    <px:PXSegmentMask ID="edInvtSubID" runat="server" DataField="InvtSubID" AutoRefresh="True" CommitChanges="True" />
					<px:PXSegmentMask ID="edNonStockInvtSubID" runat="server" DataField="ExpenseAccrualSubID" AutoRefresh="True" CommitChanges="True" />
                    <px:PXSegmentMask ID="edReasonCodeSubID" runat="server" DataField="ReasonCodeSubID" AutoRefresh="True" />
                    <px:PXSegmentMask ID="edSalesAcctID" runat="server" DataField="SalesAcctID" CommitChanges="true" />
                    <px:PXSegmentMask ID="edSalesSubID" runat="server" DataField="SalesSubID" AutoRefresh="True" />
                    <px:PXSegmentMask ID="edCOGSAcctID" runat="server" DataField="COGSAcctID" CommitChanges="true" />
					<px:PXSegmentMask ID="edNonStockCOGSAcctID" runat="server" DataField="ExpenseAcctID" CommitChanges="true" />
                    <px:PXSegmentMask ID="edCOGSSubID" runat="server" DataField="COGSSubID" AutoRefresh="True" />
					<px:PXSegmentMask ID="edNonStockCOGSSubID" runat="server" DataField="ExpenseSubID" AutoRefresh="True" />
                    <px:PXSegmentMask ID="edStdCstVarAcctID" runat="server" DataField="StdCstVarAcctID" CommitChanges="true" />
                    <px:PXSegmentMask ID="edStdCstVarSubID" runat="server" DataField="StdCstVarSubID" AutoRefresh="True" />
                    <px:PXSegmentMask ID="edStdCstRevAcctID" runat="server" DataField="StdCstRevAcctID" AutoRefresh="True" CommitChanges="true" />
                    <px:PXSegmentMask ID="edStdCstRevSubID" runat="server" DataField="StdCstRevSubID" AutoRefresh="True" />
                    <px:PXSegmentMask ID="edPOAccrualAcctID" runat="server" DataField="POAccrualAcctID" CommitChanges="true" />
                    <px:PXSegmentMask ID="edPOAccrualSubID" runat="server" DataField="POAccrualSubID" AutoRefresh="True" />
                    <px:PXSegmentMask ID="edPPVAcctID" runat="server" DataField="PPVAcctID" CommitChanges="true" />
                    <px:PXSegmentMask ID="edPPVSubID" runat="server" DataField="PPVSubID" AutoRefresh="True" />
                    <px:PXSegmentMask ID="edLCVarianceAcctID" runat="server" DataField="LCVarianceAcctID" CommitChanges="true" />
                    <px:PXSegmentMask ID="edLCVarianceSubID" runat="server" DataField="LCVarianceSubID" AutoRefresh="True" />
					<px:PXSegmentMask ID="edDeferralAcctID" runat="server" DataField="DeferralAcctID" CommitChanges="true" />
                    <px:PXSegmentMask ID="edDeferralSubID" runat="server" DataField="DeferralSubID" AutoRefresh="True" />
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Description" LoadOnDemand="true" >
                <Template>
                    <px:PXRichTextEdit ID="edBody" runat="server" DataField="Body" Style="border-width: 0px; border-top-width: 1px; width: 100%;"
                        AllowAttached="true" AllowSearch="true" AllowLoadTemplate="false" AllowSourceMode="true">
                        <AutoSize Enabled="True" MinHeight="216" />
                        <LoadTemplate TypeName="PX.SM.SMNotificationMaint" DataMember="Notifications" ViewName="NotificationTemplate" ValueField="notificationID" TextField="Name" DataSourceID="ds" Size="M"/>
                    </px:PXRichTextEdit>
                </Template>
            </px:PXTabItem>
			<px:PXTabItem Text="Configuration">
                <Template>
                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
					<px:PXLayoutRule runat="server" StartGroup="True" ControlSize="XM" GroupCaption="Attributes" />
                    <px:PXGrid ID="PXGridAnswers" runat="server" DataSourceID="ds" Height="150px" MatrixMode="True" Width="420px" SkinID="Attributes">
                        <Levels>
                            <px:PXGridLevel DataKeyNames="AttributeID,EntityType,EntityID" DataMember="Answers">
                                <RowTemplate>
                                    <px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="M" StartColumn="True" />
                                    <px:PXTextEdit ID="edParameterID" runat="server" DataField="AttributeID" Enabled="False" />
                                    <px:PXTextEdit ID="edAnswerValue" runat="server" DataField="Value" />
                                </RowTemplate>
                                <Columns>
                                    <px:PXGridColumn AllowShowHide="False" DataField="AttributeID" TextField="AttributeID_description" TextAlign="Left" Width="135px" />
                                    <px:PXGridColumn DataField="isRequired" TextAlign="Center" Type="CheckBox" Width="80px" />
									<px:PXGridColumn DataField="AttributeCategory" Type="DropDownList" />
                                    <px:PXGridColumn DataField="Value" Width="185px" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                    </px:PXGrid>

					<px:PXSelector ID="edDefaultColumnMatrixAttributeID" runat="server" DataField="DefaultColumnMatrixAttributeID" AllowEdit="True" CommitChanges="True" AutoRefresh="true" />
					<px:PXSelector ID="edDefaultRowMatrixAttributeID" runat="server" DataField="DefaultRowMatrixAttributeID" AllowEdit="True" CommitChanges="True" AutoRefresh="true" />

                    <px:PXLayoutRule runat="server" StartGroup="True" ControlSize="XM" />
                    <px:PXImageUploader Height="320px" Width="430px" ID="imgUploader" runat="server" DataField="ImageUrl" AllowUpload="true" ShowComment="true" DataMember="ItemSettings" />

                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
					<px:PXLayoutRule runat="server" StartGroup="True" ControlSize="XM" GroupCaption="Inventory ID Segment Settings" />
                    <px:PXGrid ID="PXGridIdGenerationRules" runat="server" DataSourceID="ds" Height="150px" Width="820px" SkinID="Details" StatusField="Sample">
                        <!--#include file="~\Pages\Includes\InventoryMatrixIDRules.inc"-->
                    </px:PXGrid>
                    <px:PXLayoutRule runat="server" StartGroup="True" ControlSize="XM" GroupCaption="Description Segment Settings" />
                    <px:PXGrid ID="PXGridDescriptionGenerationRules" runat="server" DataSourceID="ds" Height="150px" Width="820px" SkinID="Details" StatusField="Sample">
                        <!--#include file="~\Pages\Includes\InventoryMatrixDescriptionRules.inc"-->
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
			<px:PXTabItem Text="Item Creation" LoadOnDemand="true">
				<Template>
					<px:PXFormView ID="PXFormView1" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" DataMember="Header">
						<Template>
							<px:PXLayoutRule runat="server" StartColumn="True">
							</px:PXLayoutRule>
							<px:PXSelector runat="server" DataField="ColAttributeID" Size="M" CommitChanges="True" ID="edColAttributeID" AutoRefresh="True">
							</px:PXSelector>
							<px:PXSelector runat="server" DataField="RowAttributeID" Size="M" CommitChanges="True" ID="edRowAttributeID" AutoRefresh="True">
							</px:PXSelector>
						</Template>
					</px:PXFormView>
					<!--#include file="~\Pages\Includes\InventoryMatrixCreateItems.inc"-->
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Update Settings" LoadOnDemand="true">
				<Template>
					<px:PXLayoutRule runat="server" StartGroup="True" ControlSize="XM" GroupCaption="Fields Excluded from Update" />
                    <px:PXGrid ID="gridFieldsExcludedFromUpdate" runat="server" DataSourceID="ds" SkinID="Details" SyncPosition="true" CaptionVisible="false" Width="700px" AutoAdjustColumns="true">
                        <Levels>
                            <px:PXGridLevel DataMember="FieldsExcludedFromUpdate">
                                <RowTemplate>
							        <px:PXDropDown runat="server" DataField="TableName" Size="M" CommitChanges="True" ID="edTableName" />
							        <px:PXSelector runat="server" DataField="FieldName" Size="M" ID="edFieldName" AutoRefresh="True" />
                                    <px:PXCheckbox runat="server" DataField="IsActive" Size="M" ID="chkIsActive" />
                                </RowTemplate>
                                <Columns>
                                    <px:PXGridColumn DataField="TableName" Type="DropDownList" CommitChanges="true" />
                                    <px:PXGridColumn DataField="FieldName" />
									<px:PXGridColumn DataField="IsActive" Type="CheckBox" AllowCheckAll="true" />
                                </Columns>
							</px:PXGridLevel>
						</Levels>
						<AutoSize Enabled="True" />
					</px:PXGrid>

					<px:PXLayoutRule runat="server" StartGroup="True" ControlSize="XM" GroupCaption="Attributes Excluded from Update" />
                    <px:PXGrid ID="gridAttributesExcludedFromUpdate" runat="server" DataSourceID="ds" SkinID="Details" SyncPosition="true" MatrixMode="True" CaptionVisible="false" Width="700px" AutoAdjustColumns="true">
                        <Levels>
                            <px:PXGridLevel DataMember="AttributesExcludedFromUpdate">
                                <RowTemplate>
							        <px:PXSelector runat="server" DataField="FieldName" Size="M" ID="edAttributeFieldName" AutoRefresh="True" />
                                    <px:PXTextEdit ID="edAttributeAnswerValue" runat="server" DataField="CSAnswers__Value" />
                                    <px:PXCheckbox runat="server" DataField="IsActive" Size="M" ID="chkAttributeIsActive" />
                                </RowTemplate>
                                <Columns>
                                    <px:PXGridColumn DataField="FieldName" Width="180px" />
                                    <px:PXGridColumn DataField="CSAnswers__IsRequired" Type="CheckBox" />
                                    <px:PXGridColumn DataField="CSAnswers__AttributeCategory" />
                                    <px:PXGridColumn DataField="CSAnswers__Value" Width="185px" />
                                    <px:PXGridColumn DataField="IsActive" Type="CheckBox" AllowCheckAll="true" />
                                </Columns>
							</px:PXGridLevel>
						</Levels>
						<AutoSize Enabled="True" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Matrix Items" LoadOnDemand="true">
                <Template>
                    <px:PXPanel ID="UpdateOnlySelectedPanel" runat="server">
                        <px:PXCheckBox ID="UpdateOnlySelected" runat="server" DataField="UpdateOnlySelected" Size="XXL" />
                    </px:PXPanel>
					<px:PXGrid ID="grdMatrixItems" runat="server" DataSourceID="ds" SkinID="DetailsInTab" SyncPosition="True" RepaintColumns="True"
						Width="100%" Height="100%" OnAfterSyncState="MatrixItems_AfterSyncState" OnInit="MatrixItems_OnInit">
						<Levels>
							<px:PXGridLevel DataMember="MatrixItems">
								<RowTemplate>
									<px:PXSegmentMask ID="matrixItemsInventoryID" runat="server" DataField="InventoryID" />
									<px:PXSegmentMask ID="matrixItemsDfltSiteID" runat="server" DataField="DfltSiteID" AllowEdit="True" />
									<px:PXSegmentMask ID="matrixItemsItemClassID" runat="server" DataField="ItemClassID" AllowEdit="True" />
									<px:PXSegmentMask ID="matrixItemsTaxCategoryID" runat="server" DataField="TaxCategoryID" AllowEdit="True" />
                                </RowTemplate>
                                <Columns>
									<px:PXGridColumn DataField="Selected" Type="CheckBox" AllowCheckAll="true" />
									<px:PXGridColumn DataField="InventoryID" DisplayFormat="&gt;CCCCC-CCCCCCCCCCCCCCC" LinkCommand="ViewMatrixItem" />
									<px:PXGridColumn DataField="Descr" />
									<px:PXGridColumn DataField="DfltSiteID" />
									<px:PXGridColumn DataField="AttributeValue0" />
									<px:PXGridColumn DataField="ItemClassID" />
									<px:PXGridColumn DataField="TaxCategoryID" />
									<px:PXGridColumn DataField="RecPrice" />
									<px:PXGridColumn DataField="LastStdCost" />
									<px:PXGridColumn DataField="BasePrice" />
									<px:PXGridColumn DataField="StkItem" Type="CheckBox" />
                                </Columns>
							</px:PXGridLevel>
						</Levels>
						<Mode AllowAddNew="False" AllowDelete="False" />
						<AutoSize Enabled="True" />
                        <ActionBar>
                            <CustomItems>
                                <px:PXToolBarButton Text="Delete" Key="cmdDelete" CommandName="DeleteItems" CommandSourceID="ds" DependOnGrid="grdMatrixItems">
                                    <AutoCallBack>
                                        <Behavior CommitChanges="True" PostData="Page" />
                                    </AutoCallBack>
                                </px:PXToolBarButton>
							</CustomItems>
						</ActionBar>
					</px:PXGrid>
                </Template>
            </px:PXTabItem>
			<px:PXTabItem Text="eCommerce">
				<Template>
					<px:PXLayoutRule runat="server" ID="BCPXLayoutRule1" StartColumn="True" ControlSize="XL" LabelsWidth="SM" />
                    <px:PXCheckBox runat="server" ID="edExportToExternal" DataField="ExportToExternal" CommitChanges="True" />				
                    <px:PXDropDown runat="server" ID="edVisibility" DataField="Visibility" />
					<px:PXDropDown runat="server" ID="edAvailability" DataField="Availability" CommitChanges="True" />
					<px:PXDropDown runat="server" ID="edNotAvailMode" DataField="NotAvailMode" />
					<px:PXTextEdit runat="server" ID="edCustomURL" DataField="CustomURL" CommitChanges="True" />
					<px:PXTextEdit runat="server" ID="edPageTitle" DataField="PageTitle" />
					<px:PXTextEdit runat="server" ID="edShortDescription" DataField="ShortDescription" />
					<px:PXTextEdit runat="server" ID="edSearchKeywords" DataField="SearchKeywords" />
					<px:PXTextEdit runat="server" ID="edMetaKeywords" DataField="MetaKeywords" />
					<px:PXTextEdit runat="server" ID="edMetaDescription" DataField="MetaDescription" Height="150" TextMode="MultiLine" />
					<px:PXLayoutRule runat="server" ID="BCPXLayoutRule2" StartColumn="True" />
					<px:PXGrid runat="server" ID="gridInventoryFileUrls" Caption="Media URLs" CaptionVisible="True" AutoAdjustColumns="True" Height="265px" SkinID="ShortList" Width="500px">
						<Levels>
							<px:PXGridLevel DataMember="InventoryFileUrls">
								<Columns>
									<px:PXGridColumn DataField="FileURL" Width="250" CommitChanges="True" />
									<px:PXGridColumn DataField="FileType" Width="120" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
        </Items>
        <AutoSize Enabled="True" MinHeight="150" />
    </px:PXTab>
    <px:PXSmartPanel ID="pnlUpdatePrice" runat="server" Key="VendorItems" CaptionVisible="true" DesignView="Content" Caption="Update Effective Vendor Prices" AllowResize="false">
        <px:PXFormView ID="formEffectiveDate" runat="server" DataSourceID="ds" CaptionVisible="false" DataMember="VendorInventory$UpdatePrice" Width="280px" Height="50px" SkinID="Transparent">
            <Template>
                <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
                <px:PXDateTimeEdit ID="edPendingDate" runat="server" DataField="PendingDate" />
            </Template>
        </px:PXFormView>
        <px:PXPanel ID="PXPanelBtn" runat="server" SkinID="Buttons">
            <px:PXButton ID="PXButton3" runat="server" DialogResult="OK" Text="Update" />
            <px:PXButton ID="PXButton4" runat="server" DialogResult="No" Text="Cancel" />
        </px:PXPanel>
    </px:PXSmartPanel>
</asp:Content>
