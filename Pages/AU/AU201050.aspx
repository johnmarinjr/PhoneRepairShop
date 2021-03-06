<%@ Page Title="Customization/Actions" Language="C#"
    MasterPageFile="~/MasterPages/ListView.master"
    AutoEventWireup="true"
    ValidateRequest="false"
    CodeFile="AU201050.aspx.cs"
    Inherits="Pages_AU_AU201050" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXFormView runat="server" SkinID="transparent" ID="formTitle"
        DataSourceID="ds" DataMember="ViewPageTitle" Width="100%">
        <Template>
            <px:PXTextEdit runat="server" ID="PageTitle" DataField="PageTitle" SelectOnFocus="False"
                SkinID="Label" SuppressLabel="true"
                Width="90%"
                Style="padding: 10px">
                <font size="14pt" names="Arial,sans-serif;" />
            </px:PXTextEdit>
        </Template>
    </px:PXFormView>
    <pxa:AUDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.SM.AUWorkflowActionsMaint"
        PrimaryView="ViewActionsWithoutCategories">
        <CallbackCommands>
            <px:PXDSCallbackCommand CommitChanges="True" Name="actionViewChanges" Visible="False" RepaintControls="All" DependOnGrid="gridActions" StateColumn="ViewChangesAvailable"/>
            <px:PXDSCallbackCommand CommitChanges="False" Name="refreshFieldsGrid" Visible="False" RepaintControls="All" HideText="True" />
        </CallbackCommands>
    </pxa:AUDataSource>
</asp:Content>

<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">

    <px:PXGrid ID="gridActions" runat="server" DataSourceID="ds"
        Width="100%" Height="400px"
        AdjustPageSize="Auto"
        BorderWidth="0px"
        AllowSearch="True"
        SkinID="Primary"
        SyncPositionWithGraph="True"
        SyncPosition="True"
        AllowPaging="false"
        AutoAdjustColumns="True">
        <Mode AllowFormEdit="False" InitNewRow="False" AllowAddNew="False" AllowRowSelect="True"  />
        <AutoSize Container="Window" Enabled="True" MinHeight="150" />
        <ActionBar>
            <Actions>
                <AdjustColumns ToolBarVisible="false" />
                <ExportExcel ToolBarVisible="false" />
                <AddNew ToolBarVisible="False" />
            </Actions>
        </ActionBar>
        <Levels>
            <px:PXGridLevel DataMember="ViewActionsWithoutCategories" SortOrder="Order">
                <Columns>
                    <px:PXGridColumn DataField="ActionName" Width="100" LinkCommand="actionEdit" />
                    <px:PXGridColumn DataField="DisplayName" Width="100" />
                    <px:PXGridColumn DataField="ActionType" Width="100" />
                    <px:PXGridColumn DataField="DisableCondition" Width="100" />
                    <px:PXGridColumn DataField="HideCondition" Width="100" />
                    <px:PXGridColumn DataField="Form" Width="100" />
                    <px:PXGridColumn DataField="MassProcessingScreen" Width="100" />
                    <px:PXGridColumn DataField="Category" Width="100"  />
                    <px:PXGridColumn DataField="Status" Width="100" />
                    <px:PXGridColumn DataField="ViewChangesAvailable" Type="CheckBox"  />
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Enabled="True" MinHeight="150" />
    </px:PXGrid>
</asp:Content>

<asp:Content runat="server" ID="phDialogs" ContentPlaceHolderID="phDialogs">
    <px:PXSmartPanel runat="server" ID="PanelEditAction" Key="FilterActionEdit"
        Caption="Action Properties"
        CaptionVisible="True"
		AutoRepaint="true"
        ClientEvents-AfterRepaint="fixActiveControl"
        AcceptButtonID="PXButtonOK"
        CancelButtonID="PXButtonCancel"
        ShowCloseButton="False"
        Width="900"
        Height="600px">
        <px:PXFormView runat="server" DataSourceID="ds" ID="FormActionEdit" DataMember="FilterActionEdit" Width="100%" SkinID="Transparent" AutoRepaint="True">
            <Template>
                <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
                <px:PXTextEdit ID="ActionName" runat="server" DataField="ActionName" CommitChanges="True" />
                <px:PXDropDown ID="GraphActionName" runat="server" DataField="GraphActionName" CommitChanges="True" />
                <px:PXTextEdit ID="DisplayName" runat="server" DataField="DisplayName" />
                <px:PXDropDown ID="DisableCondition" runat="server" DataField="DisableCondition" />
                <px:PXDropDown ID="HideCondition" runat="server" DataField="HideCondition" />
                <px:PXDropDown ID="Form" runat="server" DataField="Form" CommitChanges="True" />
                <px:PXDropDown ID="Icon" runat="server" DataField="Icon" />
                <px:PXSelector ID="MassProcessingScreen" runat="server" DataField="MassProcessingScreen" CommitChanges="True" />
                <px:PXCheckBox runat="server" ID="echbBatchMode" DataField="BatchMode"></px:PXCheckBox>
                <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
                <px:PXDropDown runat="server" ID="ActionType" DataField="ActionType" CommitChanges="True" />
                <px:PXDropDown runat="server" ID="MenuFolderType" DataField="MenuFolderType" CommitChanges="True" />
                <px:PXSelector ID="DestinationScreenID" runat="server" DataField="DestinationScreenID" CommitChanges="True" AutoRefresh="True" />
                <px:PXDropDown runat="server" ID="WindowMode" DataField="WindowMode" />
                <px:PXDropDown runat="server" ID="MapEnableRights" DataField="MapEnableRights"  />
                <px:PXDropDown runat="server" ID="MapViewRights" DataField="MapViewRights"  />
                <px:PXCheckBox runat="server" ID="echbExposedToMobile" DataField="ExposedToMobile" ></px:PXCheckBox>
                <px:PXDropDown runat="server" ID="rwaeDisplayOnToolbar" DataField="DisplayOnToolbar" AllowNull="False" CommitChanges="True" ></px:PXDropDown>
                <px:PXDropDown runat="server" ID="Connotation" DataField="Connotation"></px:PXDropDown>
            </Template>
        </px:PXFormView>
        <px:PXTab runat="server" ID="tabActionsDetails" Width="100%" OnPreRender="tabActionsDetails_OnPreRender" >
            <AutoSize Enabled="True" MinHeight="150" />
            <Items>
                <px:PXTabItem Text="Navigation Parameters" RepaintOnDemand="False" >
                    <Template>
                        <px:PXGrid ID="gridParams" runat="server" DataSourceID="ds" Style="z-index: 100"
                            Width="100%" AdjustPageSize="Auto" BorderWidth="0px" AllowSearch="True" SkinID="Details"
                            AllowPaging="false" SyncPosition="True"  MatrixMode="True" OnEditorsCreated="grd_EditorsCreated_RelativeDates"
                            AutoAdjustColumns="true">
                            <ActionBar>
                                <Actions>
                                    <AdjustColumns ToolBarVisible="false" />
                                    <ExportExcel ToolBarVisible="false" />
                                </Actions>
                            </ActionBar>
                            <Levels>
                                <px:PXGridLevel DataMember="ViewActionParamsFiltered">
                                    <Mode InitNewRow="True" />
                                    <RowTemplate>
                                        <pxa:PXFormulaCombo ID="edSANValue" runat="server" DataField="Value" EditButton="True"
                                                            FieldsAutoRefresh="True" FieldsRootAutoRefresh="true" LastNodeName="Fields and Parameters"
                                                            IsInternalVisible="false" IsExternalVisible="false" OnRootFieldsNeeded="edValue_RootFieldsNeeded"
                                                            SkinID="GI" />
                                    </RowTemplate>
                                    <Columns>
                                        <px:PXGridColumn DataField="IsActive" Width="100px" Type="CheckBox" />
                                        <px:PXGridColumn DataField="Name" Width="250px" MatrixMode="True" CommitChanges="True" />
                                        <px:PXGridColumn DataField="Value" Width="200px" CommitChanges="True" Key="value" AllowSort="False" MatrixMode="true" AllowStrings="True" DisplayMode="Value" />
                                        <px:PXGridColumn DataField="FromSchema" Width="250px" CommitChanges="True" Type="CheckBox" />
                                    </Columns>
                                </px:PXGridLevel>
                            </Levels>
                            <AutoSize Enabled="True" MinHeight="100" />
                        </px:PXGrid>
                    </Template>
                </px:PXTabItem>
                <px:PXTabItem Text="Field Update" RepaintOnDemand="False" >
                    <Template>
                        <px:PXGrid runat="server" ID="gridStateActionsFields" CaptionVisible="False" AutoAdjustColumns="True" Width="100%" AllowPaging="False"
                            MatrixMode="True" SyncPosition="True" SkinID="Details" OnEditorsCreated="grd_EditorsCreated_RelativeDates" SyncPositionWithGraph="True">
                            <AutoSize Enabled="True" MinHeight="150" />
                            <AutoCallBack Enabled="True" Command="refreshFieldsGrid" Target="ds" SuppressOnReload="True" FromUIOnly="True"></AutoCallBack>
                            <ActionBar ActionsVisible="True">
                                <CustomItems>
                                    <px:PXToolBarButton DisplayStyle="Image" ImageKey="ArrowUp" Visible="true" Tooltip="Move Up">
                                        <ActionBar />
                                        <AutoCallBack Command="fieldMoveUp" Target="ds" />
                                    </px:PXToolBarButton>
                                    <px:PXToolBarButton DisplayStyle="Image" ImageKey="ArrowDown" Visible="True" Tooltip="Move Down">
                                        <ActionBar />
                                        <AutoCallBack Command="fieldMoveDown" Target="ds" />
                                    </px:PXToolBarButton>
                                </CustomItems>
                            </ActionBar>
                            <Levels>
                                <px:PXGridLevel DataMember="StateActionFieldsPerAction" SortOrder="StateActionFieldLineNbr">
                                    <Mode InitNewRow="True" AllowRowSelect="true" />
                                    <RowTemplate>
                                        <pxa:PXFormulaCombo ID="edSAFValue" runat="server" DataField="Value" EditButton="True"
                                            FieldsAutoRefresh="True" FieldsRootAutoRefresh="true" LastNodeName="Fields and Parameters"
                                            IsInternalVisible="false" IsExternalVisible="false" OnRootFieldsNeeded="edValue_RootFieldsNeeded"
                                            SkinID="GI" />
                                    </RowTemplate>
                                    <Columns>
                                        <px:PXGridColumn DataField="IsActive" Type="CheckBox" Width="60px" TextAlign="Center" CommitChanges="True" />
                                        <px:PXGridColumn DataField="FieldName" Width="200px" CommitChanges="True" />
                                        <px:PXGridColumn DataField="IsFromScheme" Type="CheckBox" Width="60px" TextAlign="Center" CommitChanges="True" />
                                        <px:PXGridColumn DataField="Value" Width="200px" CommitChanges="True" Key="value" AllowSort="False" MatrixMode="true" AllowStrings="True" DisplayMode="Value" />
                                        <px:PXGridColumn DataField="Status" Width="50px" />
                                    </Columns>
                                </px:PXGridLevel>
                            </Levels>
                            <CallbackCommands>
                                <Refresh CommitChanges="True" PostData="Page" RepaintControls="OwnerContent" />
                                <InitRow CommitChanges="true" />
                            </CallbackCommands>
                        </px:PXGrid>
                    </Template>
                </px:PXTabItem>
                <px:PXTabItem Text="Action Parameters" RepaintOnDemand="False" >
                    <Template>
                        <px:PXGrid runat="server" ID="gridStateActionsParams" CaptionVisible="False" AutoAdjustColumns="True" Width="100%" AllowPaging="False" 
                            MatrixMode="True" SyncPosition="True" SkinID="Details" OnEditorsCreated="grd_EditorsCreated_RelativeDates">
                            <AutoSize Enabled="True" MinHeight="150" />
                            <ActionBar ActionsVisible="True">
                            </ActionBar>
                            <Levels>
                                <px:PXGridLevel DataMember="StateActionParamsPerAction">
                                    <Mode InitNewRow="True" />
                                    <RowTemplate>
                                        <pxa:PXFormulaCombo ID="edSAPValue" runat="server" DataField="Value" EditButton="True"
                                            FieldsAutoRefresh="True" FieldsRootAutoRefresh="true" LastNodeName="Fields and Parameters"
                                            IsInternalVisible="false" IsExternalVisible="false" OnRootFieldsNeeded="edValue_RootFieldsNeeded"
                                            SkinID="GI" />
                                    </RowTemplate>
                                    <Columns>
                                        <px:PXGridColumn DataField="IsActive" Type="CheckBox" Width="60px" TextAlign="Center" CommitChanges="True" />
                                        <px:PXGridColumn DataField="Parameter" Width="200px" CommitChanges="True" />
                                        <px:PXGridColumn DataField="IsFromScheme" Type="CheckBox" Width="60px" TextAlign="Center" CommitChanges="True" />
                                        <px:PXGridColumn DataField="Value" Width="200px" CommitChanges="True" Key="value" AllowSort="False" MatrixMode="true" AllowStrings="True" DisplayMode="Value" />
                                        <px:PXGridColumn DataField="Status" Width="50px" />
                                    </Columns>
                                </px:PXGridLevel>
                            </Levels>
                            <CallbackCommands>
                                <Refresh CommitChanges="True" PostData="Page" RepaintControls="OwnerContent" />
                                <InitRow CommitChanges="true" />
                            </CallbackCommands>
                        </px:PXGrid>
                    </Template>
                </px:PXTabItem>
            </Items>
        </px:PXTab>
        <px:PXPanel ID="PXPanel1" runat="server" SkinID="Buttons">
            <px:PXButton ID="PXButtonOK" runat="server" DialogResult="OK" Text="OK" />
            <px:PXButton ID="PXButtonCancel" runat="server" DialogResult="Cancel" Text="Cancel">
                <AutoCallBack Enabled="True" Target="ds" Command="ActionClosePanel" />
            </px:PXButton>
        </px:PXPanel>
    </px:PXSmartPanel>

    <px:PXSmartPanel ID="PanelViewChanges" runat="server" Caption="Changes" CaptionVisible="True"
        Width="800px" Height="600px" Key="Changes" AutoRepaint="True">
        <px:PXFormView runat="server" ID="FormPanelViewChanges" DataMember="Changes" DataSourceID="ds" Height="100%"
            Width="100%">
            <AutoSize Enabled="True"></AutoSize>
            <Template>
                <px:PXHtmlView runat="server" ID="edMessages" DataField="Messages" TextMode="MultiLine" Height="100%" Width="100%"
                    Style="font-family: 'Courier New', monospace; font-size: 10pt; line-height: 16px;">
                    <AutoSize Enabled="True" MinHeight="350" Container="Parent" />
                </px:PXHtmlView>
            </Template>
        </px:PXFormView>
        <px:PXPanel ID="FormPanelViewChangesButtons" runat="server" SkinID="Buttons">
            <px:PXButton ID="FormPanelViewChangesButtonRevert" runat="server"  CommandName="actionReset" CommandSourceID="ds"  >
                
            </px:PXButton>
            <px:PXButton ID="FormPanelViewChangesButtonCancel" runat="server" DialogResult="Cancel" Text="Close">
            </px:PXButton>
        </px:PXPanel>
    </px:PXSmartPanel>

    <%--AutoRepaint="True"--%>
    <px:PXSmartPanel runat="server" ID="PanelReorderActions" Key="FilterActionsFolder"
        Caption="Reorder Actions"
        CaptionVisible="True"
        AcceptButtonID="PXButtonOK"
        CancelButtonID="PXButtonCancel"
        ShowCloseButton="False"
        ShowAfterLoad="True"
        LoadOnDemand="True"
        Width="900"
        Height="500px">

        <px:PXFormView runat="server" DataSourceID="ds" ID="FormFilterActionsFolder" DataMember="FilterActionsFolder" Width="100%" SkinID="Transparent" AutoRepaint="True">
            <Template>
                <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
                <px:PXDropDown runat="server" ID="MenuFolderType" DataField="MenuFolderType" CommitChanges="True" />

            </Template>
        </px:PXFormView>
        <px:PXGrid ID="GridActionsOrder" runat="server" DataSourceID="ds"
            SyncPosition="True"
            KeepPosition="True"
            Width="100%" AdjustPageSize="Auto" BorderWidth="0px" AllowSearch="True" SkinID="Details"
            AllowPaging="false"
            AutoAdjustColumns="true">
            <ActionBar>
                <Actions>
                    <AdjustColumns ToolBarVisible="false" />
                    <ExportExcel ToolBarVisible="false" />
                </Actions>
                <CustomItems>
                    <px:PXToolBarButton Key="cmdactionMoveUp" ImageKey="ArrowUp">
                        <AutoCallBack Command="actionMoveUp" Target="ds" />
                    </px:PXToolBarButton>

                    <px:PXToolBarButton Key="cmdactionMoveDown" ImageKey="ArrowDown">
                        <AutoCallBack Command="actionMoveDown" Target="ds" />
                    </px:PXToolBarButton>

                </CustomItems>
            </ActionBar>
            <%--		<AutoCallBack Enabled="True" Target="GridActionsOrder" Command="Save"></AutoCallBack>--%>
            <Levels>
                <px:PXGridLevel DataMember="ViewActionsOrder">
                    <Mode InitNewRow="False" />
                    <Columns>
                        <px:PXGridColumn DataField="ActionName" Width="250px" />
                        <px:PXGridColumn DataField="DisplayName" Width="250px" />
                        <px:PXGridColumn DataField="ActionType" Width="250px" />
                    </Columns>
                </px:PXGridLevel>
            </Levels>
            <AutoSize Enabled="True" MinHeight="100" />
        </px:PXGrid>
        <px:PXPanel ID="PXPanel2" runat="server" SkinID="Buttons">
            <px:PXButton ID="PXButton1" runat="server" DialogResult="OK" Text="OK" />
            <px:PXButton ID="PXButton2" runat="server" DialogResult="Cancel" Text="Cancel">
                <AutoCallBack Enabled="True" Target="ds" Command="ActionClosePanel" />

            </px:PXButton>
        </px:PXPanel>
    </px:PXSmartPanel>

    <px:PXSmartPanel runat="server" ID="PanelCategoryManagment" Key="ManageCategories"
        Caption="Manage Categories"
        CaptionVisible="True"
        AcceptButtonID="PXButtonCategiesOK"
        CancelButtonID="PXButtonCategiesCancel"
        ShowCloseButton="False"
        ShowAfterLoad="True"
        LoadOnDemand="True"
        Width="900"
        Height="500px">
     
        <px:PXGrid ID="GridCategoryManagment" runat="server" DataSourceID="ds"
            SyncPosition="True"
            KeepPosition="True"
            Width="100%" AdjustPageSize="Auto" BorderWidth="0px" AllowSearch="True" SkinID="Details"
            AllowPaging="false"
            AutoAdjustColumns="true">
            <ActionBar>
                <Actions>
                    <AdjustColumns ToolBarVisible="false" />
                    <ExportExcel ToolBarVisible="false" />
                    <AddNew ToolBarVisible="false"></AddNew>
                </Actions>
                <CustomItems>
                     <px:PXToolBarButton Key="cmdaddCategory" ImageKey="AddNew">
                        <AutoCallBack Command="addCategory" Target="ds" />
                    </px:PXToolBarButton>
                    <px:PXToolBarButton Key="cmdcategoryMoveUp" ImageKey="ArrowUp">
                        <AutoCallBack Command="categoryMoveUp" Target="ds" />
                    </px:PXToolBarButton>
                    <px:PXToolBarButton Key="cmdcategoryMoveDown" ImageKey="ArrowDown">
                        <AutoCallBack Command="categoryMoveDown" Target="ds" />
                    </px:PXToolBarButton>
                </CustomItems>
            </ActionBar>
            <%--		<AutoCallBack Enabled="True" Target="GridActionsOrder" Command="Save"></AutoCallBack>--%>
            <Levels>
                <px:PXGridLevel DataMember="ManageCategories">
                    <Mode InitNewRow="False" />
                    <Columns>
                        <px:PXGridColumn DataField="CategoryName" Width="250px" />
                        <px:PXGridColumn DataField="DisplayName" Width="250px" />
                    </Columns>
                </px:PXGridLevel>
            </Levels>
            <AutoSize Enabled="True" MinHeight="100" />
        </px:PXGrid>
        <px:PXPanel ID="PXPanelButton2" runat="server" SkinID="Buttons">
            <px:PXButton ID="PXButtonCategiesOK" runat="server" DialogResult="OK" Text="OK" />
            <px:PXButton ID="PXButtonCategiesCancel" runat="server" DialogResult="Cancel" Text="Cancel">
                <AutoCallBack Enabled="True" Target="ds" Command="ActionClosePanel" />
            </px:PXButton>
        </px:PXPanel>
    </px:PXSmartPanel>

    <px:PXSmartPanel ID="AddCategoryPanel" runat="server"
                 Caption="New Category"
                 CaptionVisible="True"
                 AutoRepaint="True"
                 Key="AddCategoryRecord">
    <px:PXLayoutRule ID="PXLayoutRule8" runat="server" StartRow="True" />
    <px:PXFormView ID="PXFormView5" runat="server"
                   SkinID="Transparent"
                   DataMember="AddCategoryRecord"
                   DataSourceID="ds"
                   AutoRepaint="True">
        <Template>
            <px:PXLayoutRule ID="PXLayoutRule1" runat="server" ControlSize="M"></px:PXLayoutRule>
            <px:PXTextEdit ID="edNewCategoryNameName" runat="server" AllowNull="False" DataField="CategoryName" CommitChanges="True">
            </px:PXTextEdit>
            <px:PXTextEdit ID="edNewCategoryNameDisplayName" runat="server" DataField="DisplayName">
            </px:PXTextEdit>
        </Template>
    </px:PXFormView>
    <px:PXPanel ID="PXPanel10" runat="server" SkinID="Buttons">
        <px:PXButton ID="PXButton15" runat="server"
                     CausesValidation="False"
                     Text="OK"
                     DialogResult="OK" />

        <px:PXButton ID="PXButton16" runat="server"
                     CausesValidation="False" DialogResult="Cancel" Text="Cancel" />
    </px:PXPanel>
</px:PXSmartPanel>

    <px:PXSmartPanel ID="ConfirmRevert" runat="server" Caption="Revert Changes" CaptionVisible="True"
        Width="600px" Height="300px" Key="ConfirmRevert" AutoRepaint="True" ClientEvents-AfterHide="OnConfirmRevertClosed">
        <px:PXFormView runat="server" ID="PXFormViewConfirmRevert1" DataMember="ConfirmRevert" DataSourceID="ds" Height="100%"
            Width="100%" AutoRepaint="True">
            <AutoSize Enabled="True"></AutoSize>
            <Template>
                <px:PXHtmlView runat="server" ID="PXHtmlViewConfirmRevert1" DataField="Message" TextMode="MultiLine" Height="100%" Width="100%"
                    Style="font-family: 'Courier New', monospace; font-size: 10pt; line-height: 16px;">
                    <AutoSize Enabled="True" Container="Parent" />
                </px:PXHtmlView>
            </Template>
        </px:PXFormView>
        <px:PXPanel ID="PXPanelConfirmRevert2" runat="server" SkinID="Buttons">
            <px:PXButton ID="PXButtonConfirmRevert1" runat="server" Text="OK" DialogResult="OK" />
            <px:PXButton ID="PXButtonConfirmRevert2" runat="server" DialogResult="Cancel" Text="Close" />
        </px:PXPanel>
    </px:PXSmartPanel>
        <script type="text/javascript">
            function OnConfirmRevertClosed() {
                if (px_alls['ConfirmRevert'].dialogResult === PXDialogResult.OK) {
                    px_alls['PanelViewChanges'].hide();
                }
            }

            function fixActiveControl(panel) {
                panel.setActiveControl();
            }
        </script>
</asp:Content>
