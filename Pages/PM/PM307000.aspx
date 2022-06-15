<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="PM307000.aspx.cs" Inherits="Page_PM307000"
    Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource EnableAttributes="True" ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.PM.ProformaEntry" PrimaryView="Document" BorderStyle="NotSet" PageLoadBehavior="GoLastRecord">
        <CallbackCommands>
            <px:PXDSCallbackCommand Name="AutoApplyPrepayments" Visible="false" />
        </CallbackCommands>
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" DataMember="Document" Caption="Proforma Summary" FilesIndicator="True"
        NoteIndicator="True" BPEventsIndicator="true" ActivityIndicator="True" ActivityField="NoteActivity" LinkPage="">
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="S"/>
            <px:PXSelector ID="edRefNbr" runat="server" DataField="RefNbr" AutoRefresh="true" >
				<GridProperties FastFilterFields="RefNbr, ProjectID, CustomerID" />
			</px:PXSelector>
            <px:PXDropDown ID="edStatus" runat="server" DataField="Status" />
            <px:PXCheckBox ID="chkHold" runat="server" DataField="Hold" CommitChanges="true" Visible="false" />
            
            <px:PXDateTimeEdit ID="edInvoiceDate" runat="server" DataField="InvoiceDate" CommitChanges="true"/>
            <px:PXSelector ID="edFinPeriodID" runat="server" DataField="FinPeriodID" CommitChanges="true" AutoRefresh="True" />
            <px:PXTextEdit ID="edProjectNbr" runat="server" DataField="ProjectNbr" CommitChanges="true" />
            <px:PXLayoutRule runat="server" ColumnSpan="2" />
            <px:PXTextEdit ID="edDescription" runat="server" DataField="Description" />

            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SS" ControlSize="XM"/>
            <px:PXSegmentMask CommitChanges="True" ID="edProjectID" runat="server" DataField="ProjectID" DataSourceID="ds" AutoRefresh="True" AllowAddNew="True" AllowEdit="True"/>
            <px:PXSegmentMask CommitChanges="True" ID="edCustomerID" runat="server" DataField="CustomerID" DataSourceID="ds" AutoRefresh="True" AllowAddNew="True" AllowEdit="True"/>
            <px:PXSegmentMask CommitChanges="True" ID="edLocationID" runat="server" DataField="LocationID" DataSourceID="ds" />
            <pxa:PXCurrencyRate DataField="CuryID" ID="edCury" runat="server" DataSourceID="ds" RateTypeView="CurrencyInfo" DataMember="Currency"></pxa:PXCurrencyRate>
            <px:PXSelector ID="edProjectCuryID" runat="server" DataField="Project.CuryID" Enabled="false" />
            
            
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="S"/>

            <px:PXNumberEdit ID="edCuryProgressiveTotal" runat="server" DataField="CuryProgressiveTotal" Enabled="False" />
            <px:PXNumberEdit ID="edCuryTransactionalTotal" runat="server" DataField="CuryTransactionalTotal" Enabled="False" />
            <px:PXNumberEdit ID="edCuryTaxTotalWithRetainage" runat="server" DataField="CuryTaxTotalWithRetainage" Enabled="False"  />
            <px:PXNumberEdit ID="edDocTotal" runat="server" DataField="CuryDocTotal" Enabled="False" />
            <px:PXNumberEdit ID="edOverflow" runat="server" DataField="Overflow.CuryOverflowTotal" Enabled="False" />
            <px:PXNumberEdit ID="edCuryRetainageTotal" runat="server" DataField="CuryRetainageTotal" Enabled="False"  />
            <px:PXNumberEdit ID="edCuryAmountDue" runat="server" DataField="CuryAmountDue" Enabled="False"  />
            <px:PXLayoutRule runat="server" ID="retainageRule1" StartColumn="True" ControlSize="S" LabelsWidth="SM" />
            <px:PXNumberEdit runat="server" ID="edRetainagePct" DataField="RetainagePct" Enabled="False" />
            <px:PXNumberEdit runat="server" ID="edCuryAllocatedRetainedTotal" DataField="CuryAllocatedRetainedTotal" Enabled="False" />
        </Template>
    </px:PXFormView>
    <px:PXSmartPanel ID="DetailsPanel" runat="server" Height="396px" Width="850px" Caption="Transaction Details" CaptionVisible="True" Key="Details" AutoCallBack-Command="Refresh"
        AutoCallBack-Enabled="True" AutoCallBack-Target="TransactionLinesGrid" LoadOnDemand="true" AutoRepaint="true">
        <px:PXGrid ID="DetailsGrid" runat="server" Height="240px" Width="100%" DataSourceID="ds" SkinID="Details" SyncPosition="true">
            <AutoSize Enabled="true" />
            <Levels>
                <px:PXGridLevel DataMember="Details">
                    <Columns>
                        <px:PXGridColumn DataField="RefNbr" LinkCommand="ViewTranDocument"/>
                        <px:PXGridColumn DataField="InventoryID" />
                        <px:PXGridColumn DataField="Description" />
                        <px:PXGridColumn DataField="ResourceID" />
                        <px:PXGridColumn DataField="BAccountID" />                        
                        <px:PXGridColumn DataField="Date" />
                        <px:PXGridColumn DataField="Billable" TextAlign="Center" Type="CheckBox" />
                        <px:PXGridColumn DataField="Qty" Label="Qty" TextAlign="Right" />
                        <px:PXGridColumn DataField="UOM" />
                        <px:PXGridColumn DataField="ProjectCuryAmount" TextAlign="Right" />
                        <px:PXGridColumn DataField="InvoicedQty" TextAlign="Right" />
                        <px:PXGridColumn DataField="ProjectCuryInvoicedAmount" TextAlign="Right" />
						<px:PXGridColumn DataField="ProjectCuryID" />
                    </Columns>
                    <RowTemplate>                                    
                        <px:PXSegmentMask runat="server" ID="edInventoryIDDtl" DataField="InventoryID" />
                    </RowTemplate>
                </px:PXGridLevel>
            </Levels>
            <ActionBar>
                <Actions>
                    <AddNew MenuVisible="False" ToolBarVisible="False" />
                    <Delete MenuVisible="True" ToolBarVisible="Top" />
                    <NoteShow MenuVisible="False" ToolBarVisible="False" />
                </Actions>
            </ActionBar>
            <Mode AllowAddNew="False" AllowDelete="True" AllowUpdate="False" />
        </px:PXGrid>
    </px:PXSmartPanel>
    <px:PXSmartPanel ID="AppendUnbilledPanel" runat="server" Height="396px" Width="850px" Caption="Upload Unbilled Transactions" CaptionVisible="True" Key="Unbilled" AutoCallBack-Command="Refresh"
        AutoCallBack-Enabled="True" AutoCallBack-Target="UnbilledGrid" LoadOnDemand="true" AutoRepaint="true">
        <px:PXGrid ID="UnbilledGrid" runat="server" Height="240px" Width="100%" DataSourceID="ds" SkinID="Details" SyncPosition="true">
            <AutoSize Enabled="true" />
            <Levels>
                <px:PXGridLevel DataMember="Unbilled">
                    <Columns>
                        <px:PXGridColumn DataField="Selected" Label="Selected" Type="CheckBox" AllowCheckAll="true" />
                        <px:PXGridColumn DataField="BranchID" Label="Branch" />
                        <px:PXGridColumn DataField="RefNbr" LinkCommand="ViewTranDocument"/>
                        <px:PXGridColumn DataField="InventoryID" />
                        <px:PXGridColumn DataField="Description" />
                        <px:PXGridColumn DataField="ResourceID" />
                        <px:PXGridColumn DataField="BAccountID" />                        
                        <px:PXGridColumn DataField="Date" />
                        <px:PXGridColumn DataField="Billable" TextAlign="Center" Type="CheckBox" />
                        <px:PXGridColumn DataField="Qty" Label="Qty" TextAlign="Right" />
                        <px:PXGridColumn DataField="UOM" />
                        <px:PXGridColumn DataField="BillableQty" Label="Billable Qty" TextAlign="Right" />
                        <px:PXGridColumn DataField="TranCuryUnitRate" Label="Unit Rate" TextAlign="Right" />
                        <px:PXGridColumn DataField="TranCuryAmount" TextAlign="Right" />
						<px:PXGridColumn DataField="TranCuryID" />
                        <px:PXGridColumn DataField="AccountGroupID" Label="Account Group" />
                        <px:PXGridColumn DataField="AccountID" Label="Account" />
                        <px:PXGridColumn DataField="SubID" Label="Subaccount" />
                        <px:PXGridColumn DataField="OffsetAccountID" Label="Offset Account" />
                        <px:PXGridColumn DataField="OffsetSubID" Label="Offset SubAccount" />
                    </Columns>
                    <RowTemplate>                                    
                        <px:PXSegmentMask runat="server" ID="edInventoryIDUbl" DataField="InventoryID" />
                    </RowTemplate>
                </px:PXGridLevel>
            </Levels>
            <ActionBar>
                <Actions>
                    <AddNew MenuVisible="False" ToolBarVisible="False" />
                    <Delete MenuVisible="True" ToolBarVisible="Top" />
                    <NoteShow MenuVisible="False" ToolBarVisible="False" />
                </Actions>
            </ActionBar>
            <Mode AllowAddNew="False" AllowDelete="True" AllowUpdate="False" />
        </px:PXGrid>
         <px:PXPanel ID="PXPanelBtn" runat="server" SkinID="Buttons">
            <px:PXButton ID="PXButtonAdd" runat="server" Text="Upload" CommandName="AppendSelected"  CommandSourceID="ds" />
            <px:PXButton ID="PXButtonAddClose" runat="server" Text="Upload & Close" DialogResult="OK"  />
            <px:PXButton ID="PXButtonClose" runat="server" DialogResult="Cancel" Text="Close" />      
        </px:PXPanel>
    </px:PXSmartPanel>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
    <px:PXTab ID="tab" runat="server" Width="100%" Height="511px" DataSourceID="ds" DataMember="DocumentSettings">
        <Activity HighlightColor="" SelectedColor="" Width="" Height=""></Activity>
        <Items >
            <px:PXTabItem Text="Progress Billing">
                <Template>
                    <px:PXGrid ID="ProgressiveLinesGrid" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" Height="150px" SkinID="DetailsInTab" SyncPosition="True" RepaintColumns="true">
                        <Levels>
                            <px:PXGridLevel DataMember="ProgressiveLines">
                                <Columns>
                                    <px:PXGridColumn AutoCallBack="True" DataField="BranchID" />
                                    <px:PXGridColumn AutoCallBack="True" DataField="AccountGroupID" />
                                    <px:PXGridColumn AutoCallBack="True" DataField="TaskID" AllowDragDrop="true" LinkCommand ="ViewProgressLineTask"/>
                                    <px:PXGridColumn AutoCallBack="True" DataField="InventoryID" AllowDragDrop="true" LinkCommand ="ViewProgressLineInventory"/>
                                    <px:PXGridColumn AutoCallBack="True" DataField="CostCodeID" AllowDragDrop="true" />
                                    <px:PXGridColumn DataField="Description" />
                                    <px:PXGridColumn DataField="PMRevenueBudget__RevisedQty" TextAlign="Right" />
                                    <px:PXGridColumn DataField="PMRevenueBudget__CuryRevisedAmount" TextAlign="Right"  />
                                    <px:PXGridColumn DataField="ActualQty" TextAlign="Right" />
                                    <px:PXGridColumn DataField="PMRevenueBudget__CuryActualAmount" TextAlign="Right" />
                                    <px:PXGridColumn DataField="PMRevenueBudget__CuryInvoicedAmount" TextAlign="Right" />
                                    <px:PXGridColumn DataField="PreviouslyInvoicedQty" TextAlign="Right" CommitChanges="true" />
                                    <px:PXGridColumn DataField="CuryPreviouslyInvoiced" TextAlign="Right" CommitChanges="true" />
                                    <px:PXGridColumn DataField="CompletedPct" TextAlign="Right" CommitChanges="true" />
                                    <px:PXGridColumn DataField="Qty" TextAlign="Right" CommitChanges="true"/>
                                    <px:PXGridColumn DataField="UOM"/>
                                    <px:PXGridColumn DataField="CuryUnitPrice"/>
                                    <px:PXGridColumn DataField="CuryAmount" TextAlign="Right" CommitChanges="true" />
                                    <px:PXGridColumn DataField="CuryMaterialStoredAmount" TextAlign="Right" CommitChanges="true" />
                                    <px:PXGridColumn DataField="CuryPrepaidAmount" TextAlign="Right" CommitChanges="true" />
                                    <px:PXGridColumn DataField="CuryLineTotal" TextAlign="Right" CommitChanges="true"/>
                                    <px:PXGridColumn DataField="CurrentInvoicedPct" TextAlign="Right" CommitChanges="true" />
                                    <px:PXGridColumn DataField="RetainagePct" TextAlign="Right" CommitChanges="true"/>
                                    <px:PXGridColumn DataField="CuryAllocatedRetainedAmount" TextAlign="Right" />
                                    <px:PXGridColumn DataField="CuryRetainage" TextAlign="Right" CommitChanges="true"/>
                                    <px:PXGridColumn DataField="TaxCategoryID" CommitChanges="true"/>
                                    <px:PXGridColumn DataField="AccountID" />
                                    <px:PXGridColumn DataField="SubID" />
                                    <px:PXGridColumn DataField="DefCode" />
                                    <px:PXGridColumn DataField="SortOrder" />
                                    <px:PXGridColumn DataField="LineNbr" />
                                    <px:PXGridColumn DataField="ProgressBillingBase"/>
                                </Columns>
                                <RowTemplate>
                                    <px:PXSegmentMask runat="server" ID="edInventoryIDPL" DataField="InventoryID" />
                                </RowTemplate>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Enabled="True" MinHeight="150" />
                        <ActionBar>
                            <CustomItems>
                                <px:PXToolBarButton Text="Upload from Budget" Tooltip="Upload lines from the revenue budget">
                                    <AutoCallBack Command="UploadFromBudget" Target="ds">
                                        <Behavior CommitChanges="True" />
                                    </AutoCallBack>
                                </px:PXToolBarButton>
                            </CustomItems>
                        </ActionBar>
                        <Mode AllowDragRows="true" />
                        <CallbackCommands PasteCommand="ProgressPasteLine">
                            <%--<Save PostData="Container" />--%>
                        </CallbackCommands>
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Time and Material">
                <Template>
                    <px:PXGrid ID="TransactionLinesGrid" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" Height="150px" SkinID="DetailsInTab" SyncPosition="True">
                        <Levels>
                            <px:PXGridLevel DataMember="TransactionLines">
                                <RowTemplate>
                                    <px:PXSegmentMask runat="server" ID="edInventoryIDTL" DataField="InventoryID" />
                                </RowTemplate>
                                <Columns>
                                    <px:PXGridColumn DataField="Option" MatrixMode="true" CommitChanges="true" />
                                    <px:PXGridColumn AutoCallBack="True" DataField="BranchID" />
                                    <px:PXGridColumn AutoCallBack="True" DataField="TaskID" AllowDragDrop="true" LinkCommand ="ViewTransactLineTask"/>
                                    <px:PXGridColumn AutoCallBack="True" DataField="InventoryID" AllowDragDrop="true" LinkCommand ="ViewTransactLineInventory"/>
                                    <px:PXGridColumn AutoCallBack="True" DataField="CostCodeID" AllowDragDrop="true" />
                                    <px:PXGridColumn DataField="Description" />
                                    <px:PXGridColumn DataField="ResourceID" />
                                    <px:PXGridColumn DataField="VendorID" LinkCommand ="ViewVendor"/>
                                    <px:PXGridColumn DataField="Date" />
                                    <px:PXGridColumn DataField="BillableQty" TextAlign="Right" />
                                    <px:PXGridColumn DataField="CuryBillableAmount" TextAlign="Right" />
                                   
                                    <px:PXGridColumn DataField="Qty" TextAlign="Right" CommitChanges="true"/>
                                    <px:PXGridColumn DataField="UOM" AutoCallBack="True"/>
                                    <px:PXGridColumn DataField="CuryUnitPrice" TextAlign="Right" CommitChanges="true"/>
                                    <px:PXGridColumn DataField="CuryAmount" TextAlign="Right" CommitChanges="true"/>
                                    <px:PXGridColumn DataField="CuryPrepaidAmount" TextAlign="Right" CommitChanges="true" />
                                    <px:PXGridColumn DataField="CuryMaxAmount" TextAlign="Right" />
                                    <px:PXGridColumn DataField="CuryAvailableAmount" TextAlign="Right" />
                                    <px:PXGridColumn DataField="CuryLineTotal" TextAlign="Right" CommitChanges="true"/>
                                    <px:PXGridColumn DataField="CuryOverflowAmount" TextAlign="Right" />
                                    <px:PXGridColumn DataField="RetainagePct" TextAlign="Right" CommitChanges="true"/>
                                    <px:PXGridColumn DataField="CuryRetainage" TextAlign="Right" CommitChanges="true"/>
                                    <px:PXGridColumn DataField="TaxCategoryID" CommitChanges="true"/>
                                    <px:PXGridColumn DataField="AccountID" />
                                    <px:PXGridColumn DataField="SubID" />
                                    <px:PXGridColumn DataField="DefCode" />
                                    <px:PXGridColumn DataField="SortOrder" />
                                    <px:PXGridColumn DataField="LineNbr" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Enabled="True" MinHeight="150" />
                        <ActionBar>
                            <CustomItems>
                                <px:PXToolBarButton Text="Upload Unbilled Transactions" Tooltip="Upload Unbilled Transactions">
                                    <AutoCallBack Command="UploadUnbilled" Target="ds">
                                        <Behavior CommitChanges="True" />
                                    </AutoCallBack>
                                </px:PXToolBarButton>
                                <px:PXToolBarButton Text="View Transaction Details" PopupPanel="DetailsPanel" />
                            </CustomItems>
                        </ActionBar>
                        <Mode InitNewRow="True" AllowDragRows="true" />
                        <CallbackCommands PasteCommand="TransactPasteLine">
                            <%--<Save PostData="Container" />--%>
                        </CallbackCommands>
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Taxes">
                <Template>
                    <px:PXGrid ID="TaxDetailsGrid" runat="server" Width="100%" SkinID="DetailsInTab" Height="300px" TabIndex="500">
                        <AutoSize Enabled="True" MinHeight="150" />
                        <Levels>
                            <px:PXGridLevel DataMember="Taxes">
                                <RowTemplate>
                                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
                                    <px:PXSelector CommitChanges="True" ID="edTaxID" runat="server" DataField="TaxID" />
                                    <px:PXNumberEdit ID="edTaxRate" runat="server" DataField="TaxRate" Enabled="False" />
                                    <px:PXNumberEdit ID="edTaxableAmt" runat="server" DataField="TaxableAmt" />
                                    <px:PXNumberEdit ID="edTaxAmt" runat="server" DataField="TaxAmt" />
                                </RowTemplate>
                                <Columns>
                                    <px:PXGridColumn DataField="TaxID" />
                                    <px:PXGridColumn DataField="TaxRate" TextAlign="Right" />
                                    <px:PXGridColumn DataField="CuryTaxableAmt" TextAlign="Right" />
									<px:PXGridColumn DataField="CuryExemptedAmt" TextAlign="Right" />
                                    <px:PXGridColumn DataField="CuryTaxAmt" TextAlign="Right" />
                                    <px:PXGridColumn DataField="CuryRetainedTaxableAmt" TextAlign="Right" />
                                    <px:PXGridColumn DataField="CuryRetainedTaxAmt" TextAlign="Right" />
                                    <px:PXGridColumn DataField="Tax__TaxType" />
                                    <px:PXGridColumn DataField="Tax__PendingTax" Type="CheckBox" TextAlign="Center" />
                                    <px:PXGridColumn DataField="Tax__ReverseTax" Type="CheckBox" TextAlign="Center" />
                                    <px:PXGridColumn DataField="Tax__ExemptTax" Type="CheckBox" TextAlign="Center" />
                                    <px:PXGridColumn DataField="Tax__StatisticalTax" Type="CheckBox" TextAlign="Center" />
                                </Columns>
                                <Layout FormViewHeight="" />
                            </px:PXGridLevel>
                        </Levels>
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            
            <px:PXTabItem Text="Financial">
                <Template>
                    <px:PXLayoutRule runat="server" StartColumn="True" GroupCaption="Invoice Settings" ControlSize="XM" LabelsWidth="SM" />
                     <px:PXDropDown ID="edARInvoiceDocType" runat="server" DataField="ARInvoiceDocType" DisplayMode="Text" />
                     <px:PXSelector ID="edARInvoiceRefNbr" runat="server" DataField="ARInvoiceRefNbr" AllowEdit="True"/>
                    <px:PXSelector ID="edBranchID" runat="server" DataField="BranchID" CommitChanges="true"/>
                    <px:PXSelector ID="edTaxZone" runat="server" DataField="TaxZoneID" CommitChanges="true"/>
                     <px:PXTextEdit ID="edExternalTaxExemptionNumber" runat="server" DataField="ExternalTaxExemptionNumber" CommitChanges="true"/>
                     <px:PXDropDown ID="edAvalaraCustomerUsageType" runat="server" DataField="AvalaraCustomerUsageType" CommitChanges="true"/>
                    <px:PXSelector ID="edTermsID" runat="server" DataField="TermsID" CommitChanges="true"/>
                    <px:PXDateTimeEdit ID="edDueDate" runat="server" DataField="DueDate" />
                    <px:PXDateTimeEdit ID="edDiscDate" runat="server" DataField="DiscDate" />
                    <px:PXLayoutRule runat="server" StartColumn="True" StartGroup="true" GroupCaption="Previous Revisions" ControlSize="L" LabelsWidth="SM" />
                    <px:PXGrid runat="server" ID="gridRevisions" SyncPosition="True" FilesIndicator="False" AllowFilter="False" AllowSearch="False" Width="600" Height="220" Caption="Revisions" CaptionVisible="False" AllowPaging="False" AdjustPageSize="None" NoteIndicator="False">
                        <Levels>
                            <px:PXGridLevel DataMember="Revisions">
                                <RowTemplate>
                                     <px:PXSelector ID="edRevisionARInvoiceRefNbr" runat="server" DataField="ARInvoiceRefNbr" Enabled="False" AllowEdit="True"/>
                                    <px:PXSelector ID="edReversedARInvoiceRefNbr" runat="server" DataField="ReversedARInvoiceRefNbr" Enabled="False" AllowEdit="True"/>
                                </RowTemplate>
                                <Columns>
                                    <px:PXGridColumn DataField="RevisionID" />                                   
                                    <px:PXGridColumn DataField="CuryDocTotal" />
                                    <px:PXGridColumn DataField="CuryRetainageTotal" />
                                    <px:PXGridColumn DataField="CuryTaxTotal" />
                                    <px:PXGridColumn DataField="ARInvoiceDocType" />
                                    <px:PXGridColumn DataField="ARInvoiceRefNbr" />
                                    <px:PXGridColumn DataField="ReversedARInvoiceDocType" />
                                    <px:PXGridColumn DataField="ReversedARInvoiceRefNbr" />  
                                    <px:PXGridColumn DataField="Description" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Enabled="False" />
                        <Mode AllowDragRows="False" AllowAddNew="False" AllowDelete="False" AllowSort="False" />
                        <ActionBar Position="None" />
                    </px:PXGrid>

                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Approvals">
                <Template>
                    <px:PXGrid ID="gridApproval" runat="server" DataSourceID="ds" Width="100%" SkinID="DetailsInTab" NoteIndicator="True">
                        <AutoSize Enabled="true" />
                        <Mode AllowAddNew="false" AllowDelete="false" AllowUpdate="false" />

                        <Levels>
                            <px:PXGridLevel DataMember="Approval">
                                <Columns>
                                    <px:PXGridColumn DataField="ApproverEmployee__AcctCD" />
                                    <px:PXGridColumn DataField="ApproverEmployee__AcctName" />
                                    <px:PXGridColumn DataField="WorkgroupID" />
                                    <px:PXGridColumn DataField="ApprovedByEmployee__AcctCD" />
                                    <px:PXGridColumn DataField="ApprovedByEmployee__AcctName" />
                                    <px:PXGridColumn DataField="ApproveDate" />
                                    <px:PXGridColumn DataField="Status" AllowNull="False" AllowUpdate="False" RenderEditorText="True" />
                                    <px:PXGridColumn DataField="Reason" AllowUpdate="False" />
                                    <px:PXGridColumn DataField="AssignmentMapID"  Visible="false" SyncVisible="false"/>
                                    <px:PXGridColumn DataField="RuleID" Visible="false" SyncVisible="false" />
                                    <px:PXGridColumn DataField="StepID" Visible="false" SyncVisible="false" />
                                    <px:PXGridColumn DataField="CreatedDateTime" Visible="false" SyncVisible="false" />
                                </Columns>
                                <Layout FormViewHeight="" />
                            </px:PXGridLevel>
                        </Levels>
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Addresses">
                <Template>
                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
                    <px:PXFormView ID="Billing_Contact" runat="server" Caption="BILL-TO CONTACT" DataMember="Billing_Contact" RenderStyle="Fieldset">
                        <Template>
                            <px:PXLayoutRule ID="PXLayoutRule1" runat="server" ControlSize="XM" LabelsWidth="SM" StartColumn="True" />
                            <px:PXCheckBox CommitChanges="True" ID="chkOverrideContact" runat="server" DataField="OverrideContact" />
                            <px:PXTextEdit ID="edFullName" runat="server" DataField="FullName" />
                            <px:PXTextEdit ID="edAttention" runat="server" DataField="Attention" />
                            <px:PXMaskEdit ID="edPhone1" runat="server" DataField="Phone1" />
                            <px:PXMailEdit ID="edEmail" runat="server" DataField="Email" CommandSourceID="ds" />
                        </Template>
                    </px:PXFormView>
                    <px:PXFormView ID="Billing_Address" runat="server" Caption="BILL-TO ADDRESS" DataMember="Billing_Address" RenderStyle="Fieldset">
                        <Template>
                            <px:PXLayoutRule ID="PXLayoutRule1" runat="server" ControlSize="XM" LabelsWidth="SM" StartColumn="True" />
                            <px:PXCheckBox CommitChanges="True" ID="chkOverrideAddress" runat="server" DataField="OverrideAddress" Height="18px" />
                            <px:PXTextEdit ID="edAddressLine1" runat="server" DataField="AddressLine1" />
                            <px:PXTextEdit ID="edAddressLine2" runat="server" DataField="AddressLine2" />
                            <px:PXTextEdit ID="edCity" runat="server" DataField="City" />
                            <px:PXSelector ID="edCountryID" runat="server" DataField="CountryID" AutoRefresh="True" CommitChanges="true" />
                            <px:PXSelector ID="edState" runat="server" DataField="State" AutoRefresh="True" />
                            <px:PXMaskEdit CommitChanges="True" ID="edPostalCode" runat="server" DataField="PostalCode" />
                            <px:PXCheckBox ID="edIsValidated" runat="server" DataField="IsValidated" Enabled="False" />
                        </Template>
					</px:PXFormView>
					<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
					<px:PXFormView ID="Shipping_Contact" runat="server" Caption="SHIP-TO CONTACT" DataMember="Shipping_Contact" RenderStyle="Fieldset">
						<Template>
							<px:PXLayoutRule ID="PXLayoutRule1" runat="server" ControlSize="XM" LabelsWidth="SM" StartColumn="True" />
							<px:PXCheckBox CommitChanges="True" ID="chkOverrideContact" runat="server" DataField="OverrideContact" />
							<px:PXTextEdit ID="edFullName" runat="server" DataField="FullName" />
							<px:PXTextEdit ID="edAttention" runat="server" DataField="Attention" />
							<px:PXMaskEdit ID="edPhone1" runat="server" DataField="Phone1" />
							<px:PXMailEdit ID="edEmail" runat="server" DataField="Email" CommandSourceID="ds" />
						</Template>
					</px:PXFormView>
					<px:PXFormView ID="Shipping_Address" runat="server" Caption="SHIP-TO ADDRESS" DataMember="Shipping_Address" RenderStyle="Fieldset">
						<Template>
							<px:PXLayoutRule ID="PXLayoutRule1" runat="server" ControlSize="XM" LabelsWidth="SM" StartColumn="True" />
							<px:PXCheckBox CommitChanges="True" ID="chkOverrideAddress" runat="server" DataField="OverrideAddress" Height="18px" />
							<px:PXTextEdit ID="edAddressLine1" runat="server" DataField="AddressLine1" />
							<px:PXTextEdit ID="edAddressLine2" runat="server" DataField="AddressLine2" />
							<px:PXTextEdit ID="edCity" runat="server" DataField="City" />
							<px:PXSelector ID="edCountryID" runat="server" DataField="CountryID" AutoRefresh="True" CommitChanges="true" />
							<px:PXSelector ID="edState" runat="server" DataField="State" AutoRefresh="True" />
							<px:PXMaskEdit CommitChanges="True" ID="edPostalCode" runat="server" DataField="PostalCode" />
							<px:PXCheckBox ID="edIsValidated" runat="server" DataField="IsValidated" Enabled="False" />
						</Template>
					</px:PXFormView>
                </Template>
            </px:PXTabItem>

        </Items>
        <AutoSize Container="Window" Enabled="True" MinHeight="150" />
    </px:PXTab>
    <!--#include file="~\Pages\Includes\CRApprovalReasonPanel.inc"-->
</asp:Content>
