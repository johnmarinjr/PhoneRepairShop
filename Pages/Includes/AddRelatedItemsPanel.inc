<px:PXSmartPanel ID="spAddRelatedItems" runat="server" Key="RelatedItemsFilter" LoadOnDemand="true" Width="1150px" Height="470px"
    Caption="Add Related Items" CaptionVisible="true" AutoRepaint="true" DesignView="Content" ShowAfterLoad="True" AutoReload="true" CloseButtonDialogResult="No" AcceptButtonID="btnRIOK">
    <px:PXFormView ID="fvRelatedItemsHeader" runat="server" CaptionVisible="False" DataMember="RelatedItemsFilter" DataSourceID="ds"
        Width="100%" SkinID="Transparent">
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
            <px:PXSegmentMask ID="smOrigInventory" runat="server" DataField="InventoryID" AllowEdit="False" Width="216px" />

            <px:PXLayoutRule runat="server" ControlSize="S" Merge="True" />
                <px:PXNumberEdit ID="neOrigInventoryPrice" runat="server" DataField="CuryUnitPrice" />
                <px:PXTextEdit ID="txtOrigInventoryCurrency" runat="server" DataField="CuryID" SuppressLabel="true" />

            <px:PXLayoutRule runat="server" ControlSize="S" Merge="True" />
                <px:PXNumberEdit ID="neOrigInventoryQty" runat="server" DataField="Qty" CommitChanges="true" />
                <px:PXTextEdit ID="lblOrigInventoryUom" runat="server" DataField="UOM" SuppressLabel="true" />

            <px:PXLayoutRule runat="server" ControlSize="S" Merge="True" />
                <px:PXNumberEdit ID="neOrigInventoryExtPrice" runat="server" DataField="CuryExtPrice" />
                <px:PXTextEdit ID="txtOrigInventoryCurrency2" runat="server" DataField="CuryID" SuppressLabel="true" />
            <px:PXLayoutRule runat="server" />

            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="S" />
            <px:PXLayoutRule runat="server" ControlSize="S" Merge="True"/>
                <px:PXNumberEdit ID="neOrigInventoryAvailQty" runat="server" DataField="AvailableQty" />
                <px:PXTextEdit ID="lblOrigInventoryUom2" runat="server" DataField="UOM" SuppressLabel="true" />
            <px:PXLayoutRule runat="server" LabelsWidth="S" ControlSize="XM" />
            <px:PXSegmentMask ID="smOrigInventotySite" runat="server" DataField="SiteID" Width="216px" />
            <px:PXCheckBox ID="cbKeepOriginalPrice" runat="server" DataField="KeepOriginalPrice" CommitChanges="True" />
            <px:PXCheckBox ID="cbOnlyAvailableRelatedItems" runat="server" DataField="OnlyAvailableItems" CommitChanges="True" />

            <px:PXCheckBox ID="cbShowSubstituteItems" runat="server" DataField="ShowSubstituteItems" Enabled="False" Visible="False" />
            <px:PXCheckBox ID="cbShowUpSellItems" runat="server" DataField="ShowUpSellItems" Enabled="False" Visible="False" />
            <px:PXCheckBox ID="cbShowCrossSellItems" runat="server" DataField="ShowCrossSellItems" Enabled="False" Visible="False" />
            <px:PXCheckBox ID="cbShowOtherRelatedItems" runat="server" DataField="ShowOtherRelatedItems" Enabled="False" Visible="False" />
            <px:PXCheckBox ID="cbShowAllRelatedItems" runat="server" DataField="ShowAllRelatedItems" Enabled="False" Visible="False" />
        </Template>
        <CallbackCommands>
	        <Save RepaintControls="None" RepaintControlsIDs="tabRelatedItems,gridAllRelatedItems,gridSubstituteItems,gridUpSellItems,gridCrossSellItems,gridOtherRelatedItems"/>
        </CallbackCommands>
    </px:PXFormView>
    <px:PXTab ID="tabRelatedItems" runat="server" Style="z-index: 100;" Width="100%" SyncPosition="True" >
        <Items>
            <px:PXTabItem Text="All Related Items" RepaintOnDemand="false" BindingContext="fvRelatedItemsHeader" VisibleExp="DataControls[&quot;cbShowAllRelatedItems&quot;].Value == true" >
                <Template>
                    <px:PXGrid ID="gridAllRelatedItems" runat="server" DataSourceID="ds"
                        AdjustPageSize="Auto" AutoAdjustColumns="true" Width="100%" SkinID="DetailsInTab" SyncPosition="True" NoteIndicator="True" FilesIndicator="True">
                        <CallbackCommands>
	                        <Save RepaintControls="None" RepaintControlsIDs="fvRelatedItemsHeader,gridSubstituteItems,gridUpSellItems,gridCrossSellItems,gridOtherRelatedItems" />
                        </CallbackCommands>
                        <Levels>
                            <px:PXGridLevel DataMember="allRelatedItems">
                                <Mode AllowAddNew="false" AllowDelete="false" />
                                <Columns>
                                    <px:PXGridColumn DataField="Selected"
                                        Type="CheckBox" TextAlign="Center" 
                                        AllowNull="False" AllowCheckAll="true"
                                        AutoCallBack="true" />
                                    <px:PXGridColumn DataField="QtySelected" />
                                    <px:PXGridColumn DataField="Rank" />
                                    <px:PXGridColumn DataField="Relation" />
                                    <px:PXGridColumn DataField="Tag" Type="DropDownList" />
                                    <px:PXGridColumn DataField="RelatedInventoryID" LinkCommand="ViewRelatedItem" />
                                    <px:PXGridColumn DataField="SubItemID" DisplayFormat="&gt;AA-A-A" />
                                    <px:PXGridColumn DataField="SubItemCD"
	                                    AllowNull="False" SyncNullable ="false" 
	                                    Visible="False" SyncVisible="false" 
	                                    AllowShowHide ="False" SyncVisibility="false" />
                                    <px:PXGridColumn DataField="Desc" />
                                    <px:PXGridColumn DataField="UOM" DisplayFormat="&gt;aaaaaa" CommitChanges="true" />
                                    <px:PXGridColumn DataField="CuryUnitPrice" />
                                    <px:PXGridColumn DataField="CuryExtPrice" />
                                    <px:PXGridColumn DataField="PriceDiff" />
                                    <px:PXGridColumn DataField="AvailableQty" />
                                    <px:PXGridColumn DataField="SiteID" />
                                    <px:PXGridColumn DataField="SiteCD" 
	                                    AllowNull="False" SyncNullable ="false" 
	                                    Visible="False" SyncVisible="false" 
	                                    AllowShowHide ="False" SyncVisibility="false" />
                                    <px:PXGridColumn DataField="Interchangeable" Type="CheckBox" TextAlign="Center"/>
                                    <px:PXGridColumn DataField="Required" Type="CheckBox" TextAlign="Center" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Enabled="true" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Substitute Items" RepaintOnDemand="false" BindingContext="fvRelatedItemsHeader" VisibleExp="DataControls[&quot;cbShowSubstituteItems&quot;].Value == true" >
                <Template>
                    <px:PXGrid ID="gridSubstituteItems" runat="server" DataSourceID="ds"
                        AdjustPageSize="Auto" AutoAdjustColumns="true" Width="100%" SkinID="DetailsInTab" SyncPosition="True" NoteIndicator="True" FilesIndicator="True">
                        <CallbackCommands>
	                        <Save RepaintControls="None" RepaintControlsIDs="fvRelatedItemsHeader,gridAllRelatedItems,gridUpSellItems,gridCrossSellItems,gridOtherRelatedItems"/>
                        </CallbackCommands>
                        <Levels>
                            <px:PXGridLevel DataMember="substituteItems">
                                <Mode AllowAddNew="false" AllowDelete="false" />
                                <Columns>
                                    <px:PXGridColumn DataField="Selected"
                                        Type="CheckBox" TextAlign="Center" 
                                        AllowNull="False" AllowCheckAll="true"
                                        AutoCallBack="true" />
                                    <px:PXGridColumn DataField="QtySelected" />
                                    <px:PXGridColumn DataField="Rank" />
                                    <px:PXGridColumn DataField="Relation" />
                                    <px:PXGridColumn DataField="Tag" Type="DropDownList" />
                                    <px:PXGridColumn DataField="RelatedInventoryID" LinkCommand="ViewRelatedItem" />
                                    <px:PXGridColumn DataField="SubItemID" DisplayFormat="&gt;AA-A-A" />
                                    <px:PXGridColumn DataField="SubItemCD"
	                                    AllowNull="False" SyncNullable ="false" 
	                                    Visible="False" SyncVisible="false" 
	                                    AllowShowHide ="False" SyncVisibility="false" />
                                    <px:PXGridColumn DataField="Desc" />
                                    <px:PXGridColumn DataField="UOM" DisplayFormat="&gt;aaaaaa" CommitChanges="true" />
                                    <px:PXGridColumn DataField="CuryUnitPrice" />
                                    <px:PXGridColumn DataField="CuryExtPrice" />
                                    <px:PXGridColumn DataField="PriceDiff" />
                                    <px:PXGridColumn DataField="AvailableQty" />
                                    <px:PXGridColumn DataField="SiteID" />
                                    <px:PXGridColumn DataField="SiteCD" 
	                                    AllowNull="False" SyncNullable ="false" 
	                                    Visible="False" SyncVisible="false" 
	                                    AllowShowHide ="False" SyncVisibility="false" />
                                    <px:PXGridColumn DataField="Interchangeable" Type="CheckBox" TextAlign="Center"/>
                                    <px:PXGridColumn DataField="Required" Type="CheckBox" TextAlign="Center" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Enabled="true" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Up-Sell Items" RepaintOnDemand="false" BindingContext="fvRelatedItemsHeader" VisibleExp="DataControls[&quot;cbShowUpSellItems&quot;].Value == true" >
                <Template>
                    <px:PXGrid ID="gridUpSellItems" runat="server" DataSourceID="ds"
                        AdjustPageSize="Auto" AutoAdjustColumns="true" Width="100%" SkinID="DetailsInTab" SyncPosition="True" NoteIndicator="True" FilesIndicator="True">
                        <CallbackCommands>
	                        <Save RepaintControls="None" RepaintControlsIDs="fvRelatedItemsHeader,gridAllRelatedItems,gridSubstituteItems,gridCrossSellItems,gridOtherRelatedItems"/>
                        </CallbackCommands>
                        <Levels>
                            <px:PXGridLevel DataMember="upSellItems">
                                <Mode AllowAddNew="false" AllowDelete="false" />
                                <Columns>
                                    <px:PXGridColumn DataField="Selected"
                                        Type="CheckBox" TextAlign="Center" 
                                        AllowNull="False" AllowCheckAll="true"
                                        AutoCallBack="true" />
                                    <px:PXGridColumn DataField="QtySelected" />
                                    <px:PXGridColumn DataField="Rank" />
                                    <px:PXGridColumn DataField="Relation" />
                                    <px:PXGridColumn DataField="Tag" Type="DropDownList" />
                                    <px:PXGridColumn DataField="RelatedInventoryID" LinkCommand="ViewRelatedItem" />
                                    <px:PXGridColumn DataField="SubItemID" DisplayFormat="&gt;AA-A-A" />
                                    <px:PXGridColumn DataField="SubItemCD"
	                                    AllowNull="False" SyncNullable ="false" 
	                                    Visible="False" SyncVisible="false" 
	                                    AllowShowHide ="False" SyncVisibility="false" />
                                    <px:PXGridColumn DataField="Desc" />
                                    <px:PXGridColumn DataField="UOM" DisplayFormat="&gt;aaaaaa" CommitChanges="true" />
                                    <px:PXGridColumn DataField="CuryUnitPrice" />
                                    <px:PXGridColumn DataField="CuryExtPrice" />
                                    <px:PXGridColumn DataField="PriceDiff" />
                                    <px:PXGridColumn DataField="AvailableQty" />
                                    <px:PXGridColumn DataField="SiteID" />
                                    <px:PXGridColumn DataField="SiteCD" 
	                                    AllowNull="False" SyncNullable ="false" 
	                                    Visible="False" SyncVisible="false" 
	                                    AllowShowHide ="False" SyncVisibility="false" />
                                    <px:PXGridColumn DataField="Interchangeable" Type="CheckBox" TextAlign="Center"/>
                                    <px:PXGridColumn DataField="Required" Type="CheckBox" TextAlign="Center" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Enabled="true" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Cross-Sell Items" RepaintOnDemand="false" BindingContext="fvRelatedItemsHeader" VisibleExp="DataControls[&quot;cbShowCrossSellItems&quot;].Value == true" >
                <Template>
                    <px:PXGrid ID="gridCrossSellItems" runat="server" DataSourceID="ds"
                        AdjustPageSize="Auto" AutoAdjustColumns="true" Width="100%" SkinID="DetailsInTab" SyncPosition="True" NoteIndicator="True" FilesIndicator="True">
                        <CallbackCommands>
	                        <Save RepaintControls="None" RepaintControlsIDs="fvRelatedItemsHeader,gridAllRelatedItems,gridSubstituteItems,gridUpSellItems,gridOtherRelatedItems"/>
                        </CallbackCommands>
                        <Levels>
                            <px:PXGridLevel DataMember="crossSellItems">
                                <Mode AllowAddNew="false" AllowDelete="false" />
                                <Columns>
                                    <px:PXGridColumn DataField="Selected"
                                        Type="CheckBox" TextAlign="Center" 
                                        AllowNull="False" AllowCheckAll="true"
                                        AutoCallBack="true" />
                                    <px:PXGridColumn DataField="QtySelected" />
                                    <px:PXGridColumn DataField="Rank" />
                                    <px:PXGridColumn DataField="Relation" />
                                    <px:PXGridColumn DataField="Tag" Type="DropDownList" />
                                    <px:PXGridColumn DataField="RelatedInventoryID" LinkCommand="ViewRelatedItem" />
                                    <px:PXGridColumn DataField="SubItemID" DisplayFormat="&gt;AA-A-A" />
                                    <px:PXGridColumn DataField="SubItemCD"
	                                    AllowNull="False" SyncNullable ="false" 
	                                    Visible="False" SyncVisible="false" 
	                                    AllowShowHide ="False" SyncVisibility="false" />
                                    <px:PXGridColumn DataField="Desc" />
                                    <px:PXGridColumn DataField="UOM" DisplayFormat="&gt;aaaaaa" CommitChanges="true" />
                                    <px:PXGridColumn DataField="CuryUnitPrice" />
                                    <px:PXGridColumn DataField="CuryExtPrice" />
                                    <px:PXGridColumn DataField="AvailableQty" />
                                    <px:PXGridColumn DataField="SiteID" />
                                    <px:PXGridColumn DataField="SiteCD" 
	                                    AllowNull="False" SyncNullable ="false" 
	                                    Visible="False" SyncVisible="false" 
	                                    AllowShowHide ="False" SyncVisibility="false" />
                                    <px:PXGridColumn DataField="Interchangeable" Type="CheckBox" TextAlign="Center"/>
                                    <px:PXGridColumn DataField="Required" Type="CheckBox" TextAlign="Center" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Enabled="true" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Other Related Items" RepaintOnDemand="false" BindingContext="fvRelatedItemsHeader" VisibleExp="DataControls[&quot;cbShowOtherRelatedItems&quot;].Value == true" >
                <Template>
                    <px:PXGrid ID="gridOtherRelatedItems" runat="server" DataSourceID="ds"
                        AdjustPageSize="Auto" AutoAdjustColumns="true" Width="100%" SkinID="DetailsInTab" SyncPosition="True" NoteIndicator="True" FilesIndicator="True">
                        <CallbackCommands>
	                        <Save RepaintControls="None" RepaintControlsIDs="fvRelatedItemsHeader,gridAllRelatedItems,gridSubstituteItems, gridUpSellItems,gridCrossSellItems"/>
                        </CallbackCommands>
                        <Levels>
                            <px:PXGridLevel DataMember="otherRelatedItems">
                                <Mode AllowAddNew="false" AllowDelete="false" />
                                <Columns>
                                    <px:PXGridColumn DataField="Selected"
                                        Type="CheckBox" TextAlign="Center" 
                                        AllowNull="False" AllowCheckAll="true"
                                        AutoCallBack="true" />
                                    <px:PXGridColumn DataField="QtySelected" />
                                    <px:PXGridColumn DataField="Rank" />
                                    <px:PXGridColumn DataField="Relation" />
                                    <px:PXGridColumn DataField="Tag" Type="DropDownList" />
                                    <px:PXGridColumn DataField="RelatedInventoryID" LinkCommand="ViewRelatedItem" />
                                    <px:PXGridColumn DataField="SubItemID" DisplayFormat="&gt;AA-A-A" />
                                    <px:PXGridColumn DataField="SubItemCD"
	                                    AllowNull="False" SyncNullable ="false" 
	                                    Visible="False" SyncVisible="false" 
	                                    AllowShowHide ="False" SyncVisibility="false" />
                                    <px:PXGridColumn DataField="Desc" />
                                    <px:PXGridColumn DataField="UOM" DisplayFormat="&gt;aaaaaa" CommitChanges="true" />
                                    <px:PXGridColumn DataField="CuryUnitPrice" />
                                    <px:PXGridColumn DataField="CuryExtPrice" />
                                    <px:PXGridColumn DataField="PriceDiff" />
                                    <px:PXGridColumn DataField="AvailableQty" />
                                    <px:PXGridColumn DataField="SiteID" />
                                    <px:PXGridColumn DataField="SiteCD" 
	                                    AllowNull="False" SyncNullable ="false" 
	                                    Visible="False" SyncVisible="false" 
	                                    AllowShowHide ="False" SyncVisibility="false" />
                                    <px:PXGridColumn DataField="Interchangeable" Type="CheckBox" TextAlign="Center"/>
                                    <px:PXGridColumn DataField="Required" Type="CheckBox" TextAlign="Center" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Enabled="true" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
        </Items>
        <CallbackCommands>
            <Search CommitChanges="True" PostData="Page" />
            <Refresh CommitChanges="True" PostData="Page" />
        </CallbackCommands>
        <AutoSize Enabled="True" Container="Parent" />
    </px:PXTab>
    <px:PXPanel ID="panel" runat="server" SkinID="Buttons">
        <px:PXButton ID="btnRIOk" runat="server" Text="Add & Close" DialogResult="OK" />
        <px:PXButton ID="btnRICancel" runat="server" DialogResult="No" Text="Cancel" />
    </px:PXPanel>
</px:PXSmartPanel>