<px:PXSmartPanel ID="PanelPickList" runat="server" Caption="Pick List" CaptionVisible="True" Key="PickListEntries" LoadOnDemand="True" AutoReload="True" AutoRepaint="True">

    <px:PXFormView ID="PickListHeader" runat="server" Width="100%" DataMember="PickListEntries" DataSourceID="ds" SkinID="Transparent">
        <Parameters>
            <px:PXSyncGridParam ControlID="gridPickList" />
        </Parameters>
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="SM" />
            <px:PXTextEdit ID="edListNbr" runat="server" DataField="SOPickingJob__PickListNbr" />
            <px:PXDropDown ID="edStatus" runat="server" DataField="SOPickingJob__Status" />
            <px:PXDropDown ID="edPriority" runat="server" DataField="SOPickingJob__Priority" />
            <px:PXCheckBox ID="edAutomaticShipmentConfirmation" runat="server" DataField="SOPickingJob__AutomaticShipmentConfirmation" AlignLeft="True" />

            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="SM" />
            <px:PXNumberEdit ID="edPathLength" runat="server" DataField="SOPicker__PathLength" />
            <px:PXSelector ID="PXPrefPicker" runat="server" DataField="SOPickingJob__PreferredAssigneeID" />
            <px:PXSelector ID="edActPicker" runat="server" DataField="SOPickingJob__ActualAssigneeID" />
            <px:PXSelector ID="edTote" runat="server" DataField="SOPickerToShipmentLink__ToteID" />
        </Template>
    </px:PXFormView>

    <px:PXGrid ID="gridPickList" runat="server" DataSourceID="ds" SkinID="Inquire" SyncPosition="True">
        <Levels>
            <px:PXGridLevel DataMember="PickListEntries">
                <Columns>
                    <px:PXGridColumn DataField="LocationID" />
                    <px:PXGridColumn DataField="InventoryID" />
                    <px:PXGridColumn DataField="SubItemID" />
                    <px:PXGridColumn DataField="LotSerialNbr" />
                    <px:PXGridColumn DataField="Qty" TextAlign="Right" />
                    <px:PXGridColumn DataField="UOM" />
                    <px:PXGridColumn DataField="ExpireDate" />
                    <px:PXGridColumn DataField="PickedQty" TextAlign="Right" />
                </Columns>
                <RowTemplate>
                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
                    <px:PXSegmentMask ID="edPick_LocationID" runat="server" DataField="LocationID" />
                    <px:PXSegmentMask ID="edPick_SubItemID" runat="server" DataField="SubItemID" />
                    <px:PXNumberEdit ID="edPick_Qty" runat="server" DataField="Qty" />
                    <px:PXSelector ID="edPick_UOM" runat="server" DataField="UOM" />
                    <px:PXTextEdit ID="edPick_LotSerialNbr" runat="server" DataField="LotSerialNbr" />
                    <px:PXDateTimeEdit ID="edPick_ExpireDate" runat="server" DataField="ExpireDate" />
                    <px:PXNumberEdit ID="edPick_PickedQty" runat="server" DataField="PickedQty" />
                </RowTemplate>
                <Layout ColumnsMenu="False" />
            </px:PXGridLevel>
        </Levels>
        <AutoSize Enabled="True" MinHeight="300" />
        <Mode AllowAddNew="False" AllowDelete="False" AllowUpdate="False" AllowUpload="False" AllowFormEdit="False" />
    </px:PXGrid>

    <px:PXPanel ID="PXPanelPickListButtons" runat="server" SkinID="Buttons">
        <px:PXButton ID="PXButtonPickListViewSource" runat="server" CommandName="ViewPickListSource" CommandSourceID="ds" />
        <px:PXButton ID="PXButtonPickListClose" runat="server" DialogResult="Cancel" Text="Close" />
    </px:PXPanel>

</px:PXSmartPanel>