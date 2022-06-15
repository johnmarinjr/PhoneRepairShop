<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="PR302000.aspx.cs"
	Inherits="Page_PR302000" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormTab.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<script type="text/javascript">
        function onDSCommandPerformed(src, args) {
            if (args.command == "ReloadPage") {
                px_alls.ds.executeCallback("CheckTaxUpdateTimestamp");
            }
        }

        function refreshFringeReducingRateGrids() {
            px_alls.grdFringeBenefitsReducingRate.refresh();
            px_alls.grdFringeEarningsReducingRate.refresh();
        }
    </script>

	<px:pxdatasource id="ds" runat="server" visible="True" width="100%" typename="PX.Objects.PR.PRPayChecksAndAdjustments" primaryview="Document">
		<CallbackCommands>
			<px:PXDSCallbackCommand CommitChanges="True" Name="Save" />
			<px:PXDSCallbackCommand Name="Insert" PostData="Self" />
			<px:PXDSCallbackCommand Name="First" PostData="Self" StartNewGroup="True" />
			<px:PXDSCallbackCommand Name="Last" PostData="Self" />
			<px:PXDSCallbackCommand Name="CopyPaste" Visible="false" />
			<px:PXDSCallbackCommand Name="PXCopyPastePaymentAction" Visible="true" />
			<px:PXDSCallbackCommand Name="ViewTaxSplits" Visible="false" />
			<px:PXDSCallbackCommand Name="ViewTaxDetails" Visible="false" />
			<px:PXDSCallbackCommand Name="ViewTaxableWageDetails" Visible="false" />
			<px:PXDSCallbackCommand Name="ViewDeductionDetails" Visible="false" />
			<px:PXDSCallbackCommand Name="ViewBenefitDetails" Visible="false" />
			<px:PXDSCallbackCommand Name="ViewPTODetails" Visible="false" />
			<px:PXDSCallbackCommand Name="CopySelectedEarningDetailLine" Visible="false" />
			<px:PXDSCallbackCommand Name="ImportTimeActivities" Visible="false" />
			<px:PXDSCallbackCommand Name="ToggleSelectedTimeActivities" Visible="False" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="AddSelectedTimeActivities" Visible="False" CommitChanges="true" />
            <px:PXDSCallbackCommand Name="AddSelectedTimeActivitiesAndClose" Visible="False" CommitChanges="true" />
			<px:PXDSCallbackCommand Name="ViewTimeActivity" Visible="False" DependOnGrid="gridEarnings" />
			<px:PXDSCallbackCommand Name="DeleteEarningDetail" Visible="false" />
            <px:PXDSCallbackCommand Name="ViewOvertimeRules" Visible="false" />
            <px:PXDSCallbackCommand Name="ViewExistingPayment" Visible="false" />
            <px:PXDSCallbackCommand Name="ViewExistingPayrollBatch" Visible="false" />
            <px:PXDSCallbackCommand Name="viewDirectDepositSplits" Visible="false" />
            <px:PXDSCallbackCommand Name="RevertOvertimeCalculation" Visible="False" CommitChanges="true" />
			<px:PXDSCallbackCommand Name="RedirectTaxMaintenance" Visible="false" />
			<px:PXDSCallbackCommand Name="ViewProjectDeductionAndBenefitPackages" Visible="false" />
            <px:PXDSCallbackCommand Name="RevertPTOSplitCalculation" Visible="False" CommitChanges="true" />
			<px:PXDSCallbackCommand Name="ViewRecordsOfEmployment" Visible="False" DependOnGrid="gridRecordsOfEmployment" />
		</CallbackCommands>
		<ClientEvents CommandPerformed="onDSCommandPerformed" />
	</px:pxdatasource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:pxformview id="form" runat="server" datasourceid="ds" width="100%" datamember="Document">
		<Template>
			<px:PXLayoutRule runat="server" LabelsWidth="SM" ControlSize="SM" StartColumn="True" />
			<px:PXDropDown ID="edDocType" runat="server" DataField="DocType" CommitChanges="True" />
			<px:PXSelector ID="edRefNbr" runat="server" DataField="RefNbr" AutoRefresh="true" />
			<px:PXDropDown ID="edStatus" runat="server" DataField="Status" />
			<px:PXCheckBox ID="edHold" runat="server" DataField="Hold" CommitChanges="true" />
			<px:PXSelector ID="edPayGroupID" runat="server" DataField="PayGroupID" CommitChanges="True" />
			<px:PXSelector ID="edPayPeriodID" runat="server" DataField="PayPeriodID" CommitChanges="True" AutoRefresh="True" />
			<px:PXSelector ID="edFinPeriodID" runat="server" DataField="FinPeriodID" />

			<px:PXLayoutRule runat="server" LabelsWidth="M" ControlSize="XM" StartColumn="True" />
			<px:PXSelector ID="edEmployeeID" runat="server" DataField="EmployeeID" CommitChanges="True" AutoRefresh="True" />
			<px:PXSelector ID="edPaymentMethodID" runat="server" DataField="PaymentMethodID" CommitChanges="True" />
			<px:PXSelector ID="edCashAccountID" runat="server" DataField="CashAccountID" CommitChanges="True" AutoRefresh="True" />
			<px:PXDateTimeEdit ID="edStartDate" runat="server" DataField="StartDate" />
			<px:PXDateTimeEdit ID="edEndDate" runat="server" DataField="EndDate" CommitChanges="True" />
			<px:PXDateTimeEdit ID="edTransactionDate" runat="server" DataField="TransactionDate" />
			<px:PXTextEdit ID="edDocDesc" runat="server" DataField="DocDesc" />

			<px:PXLayoutRule runat="server" ControlSize="SM" LabelsWidth="SM" StartColumn="True" />
			<px:PXNumberEdit ID="edGrossAmount" runat="server" DataField="GrossAmount" />
			<px:PXNumberEdit ID="edDedAmount" runat="server" DataField="DedAmount" />
			<px:PXNumberEdit ID="edTaxAmount" runat="server" DataField="TaxAmount" />
			<px:PXNumberEdit ID="edNetAmount" runat="server" DataField="NetAmount" />

			<px:PXDropDown ID="edTerminationReason" DataField="TerminationReason" runat="server" />
			<px:PXCheckBox ID="edIsRehirable" DataField="IsRehirable" runat="server" />
			<px:PXDateTimeEdit ID="edTerminationDate" DataField="TerminationDate" runat="server" />
			<px:PXCheckBox ID="edShowROETab" runat="server"
                DataField="ShowROETab" AlignLeft="True" >
            </px:PXCheckBox>
		</Template>
	</px:pxformview>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:pxtab id="tab" runat="server" width="100%" datamember="CurrentDocument">
		<Items>
			<px:PXTabItem Text="Earning">
				<Template>
					<px:PXGrid ID="gridEarnings" runat="server" DataSourceID="ds" SkinID="DetailsInTab" Width="100%" Height="500px" KeepPosition="True" SyncPosition="True" AllowPaging="True" AdjustPageSize="Auto">
						<Levels>
							<px:PXGridLevel DataMember="Earnings">
								<Mode InitNewRow="True" />
								<RowTemplate>
                                    <px:PXCheckBox runat="server" ID="edAllowCopy" DataField="AllowCopy" />
									<px:PXSelector runat="server" ID="edBranchID" DataField="BranchID" />
									<px:PXDateTimeEdit runat="server" ID="edDate" DataField="Date" />
									<px:PXSelector runat="server" ID="edTypeCD" DataField="TypeCD" />
									<px:PXTextEdit runat="server" ID="edTypeCD_Description" DataField="TypeCD_Description" />
									<px:PXSelector runat="server" ID="edLocationID" DataField="LocationID" />
									<px:PXNumberEdit runat="server" ID="edHours" DataField="Hours" />
									<px:PXNumberEdit runat="server" ID="edUnits" DataField="Units" />
									<px:PXDropDown runat="server" ID="edUnitType" DataField="UnitType" />
									<px:PXNumberEdit runat="server" ID="edRate" DataField="Rate" />
									<px:PXCheckBox runat="server" ID="edManualRate" DataField="ManualRate" />
									<px:PXNumberEdit runat="server" ID="edAmount" DataField="Amount" />
									<px:PXSelector runat="server" ID="edAccountID" DataField="AccountID" />
									<px:PXMaskEdit runat="server" ID="edSubID" DataField="SubID" />
									<px:PXSelector runat="server" ID="edProjectID" DataField="ProjectID" />
									<px:PXSelector runat="server" ID="edProjectTaskID" DataField="ProjectTaskID" AutoRefresh="true" />
									<px:PXCheckBox runat="server" ID="edCertifiedJob" DataField="CertifiedJob" />
									<px:PXSelector runat="server" ID="edCostCodeID" DataField="CostCodeID" />
									<px:PXSelector runat="server" ID="edUnionID" DataField="UnionID" />
									<px:PXSelector runat="server" ID="edLabourItemID" DataField="LabourItemID" />
									<px:PXSelector runat="server" ID="edWorkCodeID" DataField="WorkCodeID" />
									<px:PXSelector runat="server" ID="edShiftID" DataField="ShiftID" AutoRefresh="true"/>
									<px:PXTextEdit runat="server" ID="edSourceNoteID" DataField="SourceNoteID" />
								</RowTemplate>
								<Columns>
                                    <px:PXGridColumn DataField="AllowCopy" Type="CheckBox" AllowShowHide="False" Visible="False" />
									<px:PXGridColumn DataField="BranchID" Width="90px" CommitChanges="True" />
									<px:PXGridColumn DataField="Date" Width="80px" CommitChanges="True" />
									<px:PXGridColumn DataField="TypeCD" Width="65px" CommitChanges="True" />
									<px:PXGridColumn DataField="TypeCD_Description" Width="110px" />
									<px:PXGridColumn DataField="LocationID" Width="70px" CommitChanges="True" />
                                    <px:PXGridColumn DataField="Hours" Width="60px" CommitChanges="True" TextAlign="Right" />
									<px:PXGridColumn DataField="Units" Width="60px" CommitChanges="True" TextAlign="Right" />
									<px:PXGridColumn DataField="UnitType" Width="80px" CommitChanges="True" />
									<px:PXGridColumn DataField="Rate" TextAlign="Right" CommitChanges="True" />
                                    <PX:PXGridColumn DataField="ManualRate" CommitChanges="true" Type="CheckBox" />
									<px:PXGridColumn DataField="Amount" Width="100px" TextAlign="Right" CommitChanges="True" />
									<px:PXGridColumn DataField="AccountID" Width="75px" CommitChanges="True" />
									<px:PXGridColumn DataField="SubID" Width="100px" CommitChanges="True" />
									<px:PXGridColumn DataField="ProjectID" Width="150px" CommitChanges="True" />
									<px:PXGridColumn DataField="ProjectTaskID" Width="150px" CommitChanges="True" />
                                    <px:PXGridColumn DataField="CertifiedJob" Width="70px" TextAlign="Center" Type="CheckBox" CommitChanges="True" />
									<px:PXGridColumn DataField="CostCodeID" Width="100px" CommitChanges="True" />
                                    <px:PXGridColumn DataField="UnionID" Width="70px" CommitChanges="True" />
                                    <px:PXGridColumn DataField="LabourItemID" Width="70px" CommitChanges="True" />
                                    <px:PXGridColumn DataField="WorkCodeID" Width="70px" CommitChanges="True" />
									<px:PXGridColumn DataField="ShiftID" CommitChanges="True" />
									<px:PXGridColumn DataField="SourceNoteID" LinkCommand="ViewTimeActivity" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
                        <ActionBar>
							<Actions>
								<Delete ToolBarVisible="False" />
							</Actions>
							<CustomItems>
								<px:PXToolBarButton DependOnGrid="gridEarnings" CommandSourceID="ds" ImageKey="RecordDel" DisplayStyle="Image">
                                    <AutoCallBack Command="DeleteEarningDetail" Target="ds" />
                                </px:PXToolBarButton>
                                <px:PXToolBarButton Text="Copy Selected Entry" DependOnGrid="gridEarnings" CommandSourceID="ds" StateColumn="AllowCopy">
                                    <AutoCallBack Command="CopySelectedEarningDetailLine" Target="ds" />
                                </px:PXToolBarButton>
								<px:PXToolBarButton Text="Overtime Rules" DependOnGrid="grid" CommandSourceID="ds">
									<AutoCallBack Command="ViewOvertimeRules" Target="ds" />
								</px:PXToolBarButton>
								<px:PXToolBarButton Text="Import Time Activities" CommandSourceID="ds">
									<AutoCallBack Command="ImportTimeActivities" Target="ds" />
								</px:PXToolBarButton>
								<px:PXToolBarButton CommandSourceID="ds">
									<AutoCallBack Command="RevertPTOSplitCalculation" Target="ds" />
								</px:PXToolBarButton>
							</CustomItems>
						</ActionBar>
						<AutoSize Container="Window" Enabled="True" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Summary">
				<Template>
					<px:PXGrid ID="grdSummaryEarnings" runat="server" DataSourceID="ds" SkinID="Inquire" Width="100%" Height="500px">
						<Levels>
							<px:PXGridLevel DataMember="SummaryEarnings">
								<RowTemplate>
								</RowTemplate>
								<Columns>
									<px:PXGridColumn DataField="TypeCD" Width="60px" CommitChanges="True" />
									<px:PXGridColumn DataField="TypeCD_Description" Width="175px" />
									<px:PXGridColumn DataField="LocationID" Width="60px" CommitChanges="True" />
									<px:PXGridColumn DataField="Hours" Width="60px" CommitChanges="True" TextAlign="Right" />
									<px:PXGridColumn DataField="Rate" TextAlign="Right" CommitChanges="True" />
									<px:PXGridColumn DataField="Amount" Width="100px" TextAlign="Right" />
									<px:PXGridColumn DataField="MTDAmount" Width="100px" TextAlign="Right" />
									<px:PXGridColumn DataField="QTDAmount" Width="100px" TextAlign="Right" />
									<px:PXGridColumn DataField="YTDAmount" Width="100px" TextAlign="Right" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<AutoSize Container="Window" Enabled="True" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Deductions" RepaintOnDemand="false">
				<Template>
					<px:PXGrid ID="grdDeductions" runat="server" DataSourceID="ds" SkinID="DetailsInTab" Width="100%" Height="500px">
						<Levels>
							<px:PXGridLevel DataMember="Deductions">
								<Mode InitNewRow="True" />
								<RowTemplate>
									<px:PXSelector ID="edCodeID" runat="server" DataField="CodeID" AllowEdit="True" AutoRefresh="true" />
									<px:PXCheckBox ID="edIsPayableBenefit" runat="server" DataField="PRDeductCode__IsPayableBenefit" AllowEdit="false" />
								</RowTemplate>
								<Columns>
									<px:PXGridColumn DataField="CodeID" Width="130px" CommitChanges="True" />
									<px:PXGridColumn DataField="CodeID_Description" Width="185px" />
                                    <px:PXGridColumn DataField="IsActive" Width="60px" Type="CheckBox" CommitChanges="True" />
									<px:PXGridColumn DataField="Source" Width="130px" />
									<px:PXGridColumn DataField="ContribType" Width="200px" />
									<px:PXGridColumn DataField="DedAmount" TextAlign="Right" Width="120px" CommitChanges="True" />
									<px:PXGridColumn DataField="CntAmount" TextAlign="Right" Width="150px" CommitChanges="True" />
									<px:PXGridColumn DataField="SaveOverride" Width="60px" Type="CheckBox" CommitChanges="True" />
									<px:PXGridColumn DataField="YtdAmount" TextAlign="Right" Width="120px" />
									<px:PXGridColumn DataField="EmployerYtdAmount" TextAlign="Right" Width="150px" />
                                    <px:PXGridColumn DataField="PRDeductCode__IsPayableBenefit" Type="CheckBox" />
                                </Columns>
							</px:PXGridLevel>
						</Levels>
						<AutoSize Container="Window" MinHeight="400" Enabled="True" />
						<ActionBar>
							<Actions>
								<Delete ToolBarVisible="False" />
							</Actions>
							<CustomItems>
								<px:PXToolBarButton>
									<AutoCallBack Command="ViewDeductionDetails" Target="ds" />
								</px:PXToolBarButton>
								<px:PXToolBarButton>
									<AutoCallBack Command="ViewBenefitDetails" Target="ds" />
								</px:PXToolBarButton>
							</CustomItems>
						</ActionBar>
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Taxes">
				<Template>
					<px:PXSplitContainer ID="splitTaxes" runat="server" PositionInPercent="true" SplitterPosition="50" Orientation="Vertical" Height="100%">
						<Template1>
							<px:PXGrid ID="grdTaxes" runat="server" DataSourceID="ds" Width="100%" SkinID="DetailsInTab" Height="500px" SyncPosition="true">
								<Levels>
									<px:PXGridLevel DataMember="Taxes">
										<Columns>
											<px:PXGridColumn DataField="TaxID" Width="130px" CommitChanges="True" />
											<px:PXGridColumn DataField="TaxID_Description" Width="200px" />
											<px:PXGridColumn DataField="TaxCategory" CommitChanges="True" Width="150px" />
											<px:PXGridColumn DataField="TaxAmount" TextAlign="Right" Width="130px" CommitChanges="True" />
											<px:PXGridColumn DataField="WageBaseAmount" TextAlign="Right" Width="130px" />
											<px:PXGridColumn DataField="WageBaseGrossAmt" TextAlign="Right" Width="100px" />
											<px:PXGridColumn DataField="WageBaseHours" TextAlign="Right" Width="100px" />
											<px:PXGridColumn DataField="YtdAmount" TextAlign="Right" Width="130px" />
										</Columns>
									</px:PXGridLevel>
								</Levels>
								<ActionBar>
									<CustomItems>
										<px:PXToolBarButton DependOnGrid="grid" CommandSourceID="ds">
											<AutoCallBack Command="ViewTaxDetails" Target="ds" />
										</px:PXToolBarButton>
										<px:PXToolBarButton DependOnGrid="grid" CommandSourceID="ds">
											<AutoCallBack Command="ViewTaxableWageDetails" Target="ds" />
										</px:PXToolBarButton>
									</CustomItems>
								</ActionBar>
								<AutoSize Container="Window" MinHeight="400" Enabled="True" />
								<AutoCallBack Target="taxSplitGrid" Command="Refresh" />
							</px:PXGrid>
						</Template1>
						<Template2>
							<px:PXGrid ID="taxSplitGrid" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" Height="150px" SkinID="Inquire">
								<Levels>
									<px:PXGridLevel DataMember="TaxSplits">
										<Columns>
											<px:PXGridColumn DataField="WageType" Width="150px" />
											<px:PXGridColumn DataField="TaxID" Width="130px" />
											<px:PXGridColumn DataField="WageBaseAmount" Width="120px" />
										</Columns>
									</px:PXGridLevel>
								</Levels>
								<AutoSize Enabled="true" MinHeight="150" />
								<Mode AllowAddNew="False" AllowDelete="False" />
								<ActionBar ActionsVisible="true" />
							</px:PXGrid>
						</Template2>
						<AutoSize Enabled="true" Container="Window" MinHeight="400" />
					</px:PXSplitContainer>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Paid Time Off" RepaintOnDemand="false">
				<Template>
					<px:PXFormView ID="formPTOSettings" runat="server" DataSourceID="ds" DataMember="CurrentDocument" SkinID="Transparent">
						<Template>
							<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="0px" ControlSize="M" />
							<px:PXCheckBox ID="chkAutoPayCarryover" runat="server" DataField="AutoPayCarryover" AlignLeft="true" CommitChanges="true" />
						</Template>
					</px:PXFormView>
					<px:PXGrid runat="server" DataSourceID="ds" ID="grdPaymentPTOBanks" SkinID="Inquire" Width="100%">
						<Levels>
							<px:PXGridLevel DataMember="PaymentPTOBanks">
								<RowTemplate>
									<px:PXNumberEdit ID="edAccrualRate" runat="server" DataField="AccrualRate" />
								</RowTemplate>
								<Columns>
									<px:PXGridColumn DataField="BankID" CommitChanges="true" />
									<px:PXGridColumn DataField="IsActive" Type="CheckBox" TextAlign="Center" Width="60px" CommitChanges="true" />
									<px:PXGridColumn DataField="BankID_Description" />
									<px:PXGridColumn DataField="EffectiveStartDate" />
									<px:PXGridColumn DataField="IsCertifiedJob" Type="CheckBox" TextAlign="Center" Width="60px" CommitChanges="true" />
									<px:PXGridColumn DataField="AccrualMethod" />
									<px:PXGridColumn DataField="AccrualRate" CommitChanges="true" />
									<px:PXGridColumn DataField="HoursPerYear" CommitChanges="true"/>
									<px:PXGridColumn DataField="AccrualLimit" />
									<px:PXGridColumn DataField="AccrualAmount" />
									<px:PXGridColumn DataField="AccrualMoney" />
									<px:PXGridColumn DataField="FrontLoadingAmount" />
									<px:PXGridColumn DataField="CarryoverAmount" />
									<px:PXGridColumn DataField="CarryoverMoney" />
									<px:PXGridColumn DataField="TotalAccrual" />
									<px:PXGridColumn DataField="TotalAccrualMoney" />
									<px:PXGridColumn DataField="DisbursementAmount" />
									<px:PXGridColumn DataField="DisbursementMoney" />
									<px:PXGridColumn DataField="PaidCarryoverAmount" />
									<px:PXGridColumn DataField="TotalDisbursement" />
									<px:PXGridColumn DataField="TotalDisbursementMoney" />
									<px:PXGridColumn DataField="SettlementDiscardAmount" />
									<px:PXGridColumn DataField="AccumulatedAmount" />
									<px:PXGridColumn DataField="AccumulatedMoney" />
									<px:PXGridColumn DataField="UsedAmount" />
									<px:PXGridColumn DataField="UsedMoney" />
									<px:PXGridColumn DataField="AvailableAmount" />
									<px:PXGridColumn DataField="AvailableMoney" />
									<px:PXGridColumn DataField="CalculationFormula" Width="250px"/>
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<ActionBar>
							<CustomItems>
								<px:PXToolBarButton>
									<AutoCallBack Command="ViewPTODetails" Target="ds" />
								</px:PXToolBarButton>
							</CustomItems>
						</ActionBar>
						<AutoSize Container="Window" Enabled="True" MinHeight="300" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Workers' Compensation">
				<Template>
					<px:PXGrid ID="grdWC" runat="server" DataSourceID="ds" SkinID="DetailsInTab" Width="100%" Height="500px">
						<Levels>
							<px:PXGridLevel DataMember="WCPremiums">
								<RowTemplate>
									<px:PXNumberEdit ID="edWCRate" runat="server" DataField="Rate" />
								</RowTemplate>
								<Columns>
									<px:PXGridColumn DataField="BranchID" />
									<px:PXGridColumn DataField="WorkCodeID" Width="200px" CommitChanges="true" />
									<px:PXGridColumn DataField="PMWorkCode__Description" Width="200px" />
									<px:PXGridColumn DataField="DeductCodeID" Width="120px" CommitChanges="true" />
									<px:PXGridColumn DataField="PRDeductCode__State" Width="60px" />
									<px:PXGridColumn DataField="DeductionCalcType" />
									<px:PXGridColumn DataField="BenefitCalcType" />
									<px:PXGridColumn DataField="DeductionRate" CommitChanges="true" />
									<px:PXGridColumn DataField="Rate" CommitChanges="true" />
									<px:PXGridColumn DataField="RegularWageBaseHours" Width="120px" CommitChanges="true" />
									<px:PXGridColumn DataField="OvertimeWageBaseHours" Width="120px" CommitChanges="true" />
									<px:PXGridColumn DataField="WageBaseHours" Width="120px" CommitChanges="true" />
									<px:PXGridColumn DataField="RegularWageBaseAmount" Width="120px" CommitChanges="true" />
									<px:PXGridColumn DataField="OvertimeWageBaseAmount" Width="120px" CommitChanges="true" />
									<px:PXGridColumn DataField="WageBaseAmount" Width="120px" CommitChanges="true" />
									<px:PXGridColumn DataField="DeductionAmount" Width="120px" />
									<px:PXGridColumn DataField="Amount" Width="120px" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<AutoSize Container="Window" Enabled="True" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Union">
				<Template>
					<px:PXGrid ID="grdUnion" runat="server" DataSourceID="ds" SkinID="DetailsInTab" Width="100%" Height="500px">
						<Levels>
							<px:PXGridLevel DataMember="UnionPackageDeductions">
								<RowTemplate>
									<px:PXSelector runat="server" ID="edPackageUnionID" DataField="UnionID" />
									<px:PXSelector runat="server" ID="edPackageUnionLaborItem" DataField="LaborItemID" />
									<px:PXSelector runat="server" ID="edPackageUnionDeductCode" DataField="DeductCodeID" />
								</RowTemplate>
								<Columns>
									<px:PXGridColumn DataField="UnionID" Width="120px" CommitChanges="true" />
									<px:PXGridColumn DataField="LaborItemID" CommitChanges="true" />
									<px:PXGridColumn DataField="DeductCodeID" Width="120px" CommitChanges="true" />
									<px:PXGridColumn DataField="DeductionCalcType" />
									<px:PXGridColumn DataField="BenefitCalcType" />
									<px:PXGridColumn DataField="RegularWageBaseHours" Width="120px" CommitChanges="true" />
									<px:PXGridColumn DataField="OvertimeWageBaseHours" Width="120px" CommitChanges="true" />
									<px:PXGridColumn DataField="WageBaseHours" Width="120px" CommitChanges="true" />
									<px:PXGridColumn DataField="RegularWageBaseAmount" Width="120px" CommitChanges="true" />
									<px:PXGridColumn DataField="OvertimeWageBaseAmount" Width="120px" CommitChanges="true" />
									<px:PXGridColumn DataField="WageBaseAmount" Width="120px" CommitChanges="true" />
									<px:PXGridColumn DataField="DeductionAmount" Width="120px" />
									<px:PXGridColumn DataField="BenefitAmount" Width="120px" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<AutoSize Container="Window" Enabled="True" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Certified Project">
				<Template>
					<px:PXSplitContainer ID="splitCertifiedProject" runat="server" PositionInPercent="true" SplitterPosition="60" Orientation="Vertical" Height="100%">
						<Template1>
							<px:PXGrid ID="grdFringeBenefits" runat="server" DataSourceID="ds" SkinID="Inquire" Width="100%" SyncPosition="true">
								<Levels>
									<px:PXGridLevel DataMember="PaymentFringeBenefits">
										<Columns>
											<px:PXGridColumn DataField="ProjectID" />
											<px:PXGridColumn DataField="LaborItemID" />
											<px:PXGridColumn DataField="ProjectTaskID" />
											<px:PXGridColumn DataField="ApplicableHours" />
											<px:PXGridColumn DataField="ProjectHours" />
											<px:PXGridColumn DataField="FringeRate" />
											<px:PXGridColumn DataField="ReducingRate" />
											<px:PXGridColumn DataField="CalculatedFringeRate" />
											<px:PXGridColumn DataField="PaidFringeAmount" />
										</Columns>
									</px:PXGridLevel>
								</Levels>
								<ActionBar>
									<CustomItems>
										<px:PXToolBarButton>
											<AutoCallBack Command="ViewProjectDeductionAndBenefitPackages" Target="ds" />
										</px:PXToolBarButton>
									</CustomItems>
								</ActionBar>
								<AutoSize Container="Window" Enabled="True" />
								<ClientEvents AfterRowChange="refreshFringeReducingRateGrids" />
							</px:PXGrid>
						</Template1>
						<Template2>
							<px:PXSplitContainer ID="splitFringeBenefit" runat="server" PositionInPercent="true" SplitterPosition="50" Orientation="Horizontal" Height="100%">
								<Template1>
									<px:PXGrid ID="grdFringeBenefitsReducingRate" runat="server" DataSourceID="ds" SkinID="Inquire" Width="100%" AllowPaging="false"
										Caption="Benefits Decreasing the Rate" CaptionVisible="true">
										<Levels>
											<px:PXGridLevel DataMember="PaymentFringeBenefitsDecreasingRate">
												<Columns>
													<px:PXGridColumn DataField="DeductCodeID" Width="120px" />
													<px:PXGridColumn DataField="AnnualizationException" Width="120px" Type="CheckBox" TextAlign="Center" />
													<px:PXGridColumn DataField="AnnualHours" />
													<px:PXGridColumn DataField="AnnualWeeks" />
													<px:PXGridColumn DataField="ApplicableHours" />
													<px:PXGridColumn DataField="Amount" />
													<px:PXGridColumn DataField="BenefitRate" />
												</Columns>
											</px:PXGridLevel>
										</Levels>
										<AutoSize Container="Window" Enabled="True" />
									</px:PXGrid>
								</Template1>
								<Template2>
									<px:PXGrid ID="grdFringeEarningsReducingRate" runat="server" DataSourceID="ds" SkinID="Inquire" Width="100%" AllowPaging="false"
										Caption="Excess Pay Rate Calculation" CaptionVisible="true">
										<Levels>
											<px:PXGridLevel DataMember="PaymentFringeEarningsDecreasingRate">
												<Columns>
													<px:PXGridColumn DataField="EarningTypeCD" Width="120px" />
													<px:PXGridColumn DataField="ActualPayRate" />
													<px:PXGridColumn DataField="PrevailingWage" />
													<px:PXGridColumn DataField="Amount" />
													<px:PXGridColumn DataField="AnnualizationException" Width="120px" Type="CheckBox" TextAlign="Center" />
													<px:PXGridColumn DataField="AnnualHours" />
													<px:PXGridColumn DataField="AnnualWeeks" />
													<px:PXGridColumn DataField="ApplicableHours" />
													<px:PXGridColumn DataField="BenefitRate" />
												</Columns>
											</px:PXGridLevel>
										</Levels>
										<AutoSize Container="Window" Enabled="True" />
									</px:PXGrid>
								</Template2>
								<AutoSize Container="Window" Enabled="True" />
							</px:PXSplitContainer>
						</Template2>
						<AutoSize Container="Window" Enabled="True" MinHeight="400" />
					</px:PXSplitContainer>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Financial" BindingContext="form" RepaintOnDemand="false">
				<Template>
					<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" GroupCaption="Link to GL" StartGroup="True" />
					<px:PXSelector ID="edBatchNbr" runat="server" DataField="BatchNbr" Enabled="False" AllowEdit="True" />
					<px:PXSegmentMask CommitChanges="True" ID="edBranchID" runat="server" DataField="BranchID" />
					<px:PXSelector ID="edCountyID" runat="server" DataField="CountryID" />
					<px:PXTextEdit ID="edOrigRefNbr" runat="server" DataField="OrigRefNbr" Enabled="False">
						<LinkCommand Target="ds" Command="ViewOriginalDocument" />
					</px:PXTextEdit>
					<px:PXTextEdit ID="edPaymentBatchNbr" runat="server" DataField="PaymentBatchNbr" Enabled="False">
						<LinkCommand Target="ds" Command="ViewPaymentBatch" />
					</px:PXTextEdit>
					<px:PXButton ID="btnShowDDSplits" runat="server" Text="View Direct Deposit Splits" CommandName="viewDirectDepositSplits" CommandSourceID="ds" />

					<px:PXLayoutRule runat="server" LabelsWidth="SM" ControlSize="XM" GroupCaption="Additional Details" StartGroup="True" />
					<px:PXDropDown ID="edChkVoidType" runat="server" DataField="ChkVoidType" />
					<px:PXTextEdit ID="edExtRefNbr" runat="server" DataField="ExtRefNbr"/>
                    <px:PXDropDown ID="edEmpType" runat="server" DataField="EmpType" CommitChanges="True" />
                    <px:PXLayoutRule runat="server" Merge="True" />
                    <px:PXNumberEdit ID="edRegularAmount" runat="server" DataField="RegularAmount" CommitChanges="True" />
                    <px:PXCheckBox ID="edManualRegularAmount" runat="server" DataField="ManualRegularAmount" CommitChanges="True" />
                    <px:PXLayoutRule runat="server" />
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Records of Employment" BindingContext="form" VisibleExp="DataControls[&quot;edShowROETab&quot;].Value==true">
				<Template>
					<px:PXGrid ID="gridRecordsOfEmployment" runat="server" DataSourceID="ds" SkinID="DetailsInTab" Width="100%" KeepPosition="True" SyncPosition="True" AllowPaging="True" AdjustPageSize="Auto">
						<Levels>
							<px:PXGridLevel DataMember="RecordsOfEmployment">
                                <Mode AllowAddNew="false" />
								<Columns>
									<px:PXGridColumn DataField="RefNbr" 
										LinkCommand="ViewRecordsOfEmployment">
                                    </px:PXGridColumn>
									<px:PXGridColumn DataField="Status" />                                        
									<px:PXGridColumn DataField="Amendment" Type="CheckBox" TextAlign="Center" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<AutoSize Enabled="True" />
						<ActionBar>
                            <Actions>
                                <AddNew ToolBarVisible = "False" />
                            </Actions>
						</ActionBar>
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
		</Items>
		<AutoSize Container="Window" Enabled="True" MinHeight="150" />
	</px:pxtab>
	<px:pxsmartpanel runat="server" id="pnlOvertimeRules" caption="Overtime Rules Used for Calculation" captionvisible="true" key="PaymentOvertimeRules" autorepaint="True">
        <px:PXFormView ID="OvertimeRulesForm" runat="server" DataSourceID="ds" DataMember="CurrentDocument" RenderStyle="Simple" SkinID="Transparent">
            <Template>
                <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
                <px:PXCheckBox ID="chkApplyOvertimeRules" runat="server" DataField="ApplyOvertimeRules" AlignLeft="true" CommitChanges="true" />
            </Template>
        </px:PXFormView>
        <px:PXGrid ID="OvertimeRulesGrid" runat="server" SyncPosition="True" DataSourceID="ds" SkinID="Inquire">
            <Levels>
                <px:PXGridLevel DataMember="PaymentOvertimeRules">
                    <Columns>
                        <px:PXGridColumn DataField="IsActive" TextAlign="Center" Type="CheckBox" AllowCheckAll="True" CommitChanges="True" Width="60px" />
                        <px:PXGridColumn DataField="OvertimeRuleID" Width="170px" />
                        <px:PXGridColumn DataField="PROvertimeRule__Description" Width="240px" />
                        <px:PXGridColumn DataField="PROvertimeRule__DisbursingTypeCD" Width="100px" />
                        <px:PXGridColumn DataField="PROvertimeRule__OvertimeMultiplier" TextAlign="Right" Width="70px" />
                        <px:PXGridColumn DataField="PROvertimeRule__RuleType" Width="70px" />
                        <px:PXGridColumn DataField="PROvertimeRule__WeekDay" Width="90px" />
                        <px:PXGridColumn DataField="PROvertimeRule__OvertimeThreshold" Width="100px" />
                        <px:PXGridColumn DataField="PROvertimeRule__State" Width="53px" />
                        <px:PXGridColumn DataField="PROvertimeRule__UnionID" Width="150px" />
                        <px:PXGridColumn DataField="PROvertimeRule__ProjectID" Width="150px" />
                    </Columns>
                </px:PXGridLevel>
            </Levels>
            <AutoSize Enabled="true" MinHeight="300" />
        </px:PXGrid>
        <px:PXPanel ID="pnlOvertimeRulesButtons" runat="server" SkinID="Buttons">
			<px:PXButton ID="btnRevertOvertimeCalculation" runat="server" CommandName="RevertOvertimeCalculation" CommandSourceID="ds" Text="Revert Overtime Calculations and Close" SyncVisible="false" DialogResult="OK" />
			<px:PXButton ID="btnOK" runat="server" Text="OK" DialogResult="OK" />
		</px:PXPanel>
    </px:pxsmartpanel>
	<px:pxsmartpanel id="PanelTimeActivities" runat="server" key="TimeActivities" width="1100px" height="500px"
		caption="Import Time Activities" captionvisible="true" autorepaint="true" designview="Content" showafterload="true">
		<px:PXFormView ID="formTimeActivities" runat="server" CaptionVisible="False" DataMember="ImportTimeActivitiesFilter" DataSourceID="ds"
			Width="100%" SkinID="Transparent">
			<Template>
				<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="SM" />
                <px:PXCheckBox ID="edShowImportedActivities" runat="server" DataField="ShowImportedActivities" CommitChanges="True" AlignLeft="True" />
			</Template>
		</px:PXFormView>
		<px:PXGrid ID="gridTimeActivities" runat="server" DataSourceID="ds" Width="100%" SkinID="Inquire" AdjustPageSize="Auto" AllowSearch="True" SyncPosition="True" >
			<ActionBar PagerVisible="False">
				<PagerSettings Mode="NextPrevFirstLast" />
				<CustomItems>
                    <px:PXToolBarButton Text="Toggle Selected" Key="cmdToggleSelectedTimeActivities">
                        <AutoCallBack Command="ToggleSelectedTimeActivities" Target="ds" />
                    </px:PXToolBarButton>
                </CustomItems>
			</ActionBar>
			<Levels>
				<px:PXGridLevel DataMember="TimeActivities">
					<RowTemplate>
						<px:PXTimeSpan TimeMode="True" ID="edTimeSpent" runat="server" DataField="TimeSpent" InputMask="hh:mm" />
					</RowTemplate>
					<Columns>
                        <px:PXGridColumn DataField="Selected" AllowNull="False" TextAlign="Center" Type="CheckBox" />
						<px:PXGridColumn DataField="OwnerID" />
						<px:PXGridColumn DataField="OwnerID_Description" />
						<px:PXGridColumn DataField="Branch__BranchCD" />
                        <px:PXGridColumn DataField="Date" />
						<px:PXGridColumn DataField="TimeSpent" RenderEditorText="True" />
                        <px:PXGridColumn DataField="EarningTypeID" />
						<px:PXGridColumn DataField="ProjectID" />
						<px:PXGridColumn DataField="ProjectTaskID" />
						<px:PXGridColumn DataField="CertifiedJob" TextAlign="Center" Type="CheckBox" />
						<px:PXGridColumn DataField="UnionID" />
						<px:PXGridColumn DataField="LabourItemID" />
						<px:PXGridColumn DataField="CostCodeID" />
						<px:PXGridColumn DataField="WorkCodeID" />
					</Columns>
				</px:PXGridLevel>
			</Levels>
			<AutoSize Enabled="true" />
		</px:PXGrid>
		<px:PXPanel ID="PXPanel2" runat="server" SkinID="Buttons">
			<px:PXButton ID="PXButton1" runat="server" CommandName="AddSelectedTimeActivities" CommandSourceID="ds" Text="Add" DependOnGrid="gridTimeActivities" />
			<px:PXButton ID="PXButton2" runat="server" CommandName="AddSelectedTimeActivitiesAndClose" CommandSourceID="ds" Text="Add & Close" DependOnGrid="gridTimeActivities" DialogResult="OK" />
			<px:PXButton ID="PXButton3" runat="server" DialogResult="No" Text="Cancel" />
		</px:PXPanel>
	</px:pxsmartpanel>
	<px:pxsmartpanel runat="server" id="pnlExistingPayment" caption="Existing Paycheck" captionvisible="true" key="ExistingPayment" autorepaint="True" closebuttondialogresult="No">
        <px:PXFormView ID="ExistingPaymentForm" runat="server" DataSourceID="ds" DataMember="ExistingPayment" RenderStyle="Simple" SkinID="Transparent">
            <Template>
                <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" SuppressLabel="True" />
                <px:PXTextEdit ID="edViewExistingPaymentMessage" runat="server" DataField="Message" TextMode="MultiLine" Enabled="False" Height="100" Width="300" />
            </Template>
        </px:PXFormView>
        <px:PXButton ID="btnViewExistingPayment" runat="server" Text="View Existing Paycheck" CommandName="ViewExistingPayment" CommandSourceID="ds" DialogResult="OK" />
        <px:PXButton ID="btnContinueEditingPayment1" runat="server" Text="Continue Editing" DialogResult="No" />
    </px:pxsmartpanel>
	<px:pxsmartpanel runat="server" id="pnlExistingPayrollBatch" caption="Existing Payroll Batch" captionvisible="true" key="ExistingPayrollBatch" autorepaint="True" closebuttondialogresult="No">
        <px:PXFormView ID="ExistingPayrollBatchForm" runat="server" DataSourceID="ds" DataMember="ExistingPayrollBatch" RenderStyle="Simple" SkinID="Transparent">
            <Template>
                <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" SuppressLabel="True" />
                <px:PXTextEdit ID="edViewExistingPayrollBatchMessage" runat="server" DataField="Message" TextMode="MultiLine" Enabled="False" Height="120" Width="300"/>
            </Template>
        </px:PXFormView>
		<px:PXButton ID="btnViewExistingPayrollBatch" runat="server" Text="View Existing Payroll Batch" CommandName="ViewExistingPayrollBatch" CommandSourceID="ds" DialogResult="OK" />
        <px:PXButton ID="btnContinueEditingPayment2" runat="server" Text="Continue Editing" DialogResult="No" />
    </px:pxsmartpanel>
	<px:pxsmartpanel runat="server" ID="spDeductionDetails" AutoRepaint="true" Caption="Deduction Details" CaptionVisible="true" Key="DeductionDetails" Height="400px">
		<px:PXGrid ID="DeductionDetailsGrid" runat="server" DataSourceID="ds" SkinID="Details">
			<Levels>
				<px:PXGridLevel DataMember="DeductionDetails">
					<Columns>
						<px:PXGridColumn DataField="BranchID" CommitChanges="true" />
						<px:PXGridColumn DataField="CodeID" CommitChanges="true" />
						<px:PXGridColumn DataField="CodeID_Description" />
						<px:PXGridColumn DataField="Amount" TextAlign="Right" />
						<px:PXGridColumn DataField="AccountID" Width="100px" CommitChanges="True" />
						<px:PXGridColumn DataField="SubID" Width="100px" CommitChanges="True" />
					</Columns>
				</px:PXGridLevel>
			</Levels>
			<AutoSize Enabled="true" MinHeight="150" />
		</px:PXGrid>
	</px:pxsmartpanel>
	<px:pxsmartpanel runat="server" id="spBenefitDetails" autorepaint="true" caption="Benefit Details" captionvisible="true" key="BenefitDetails" width="300px" height="600px">
		<px:PXGrid ID="BenefitDetailsGrid" runat="server" DataSourceID="ds" SkinID="Details">
			<Levels>
				<px:PXGridLevel DataMember="BenefitDetails">
					<Columns>
						<px:PXGridColumn DataField="BranchID" CommitChanges="true" />
						<px:PXGridColumn DataField="CodeID" Width="100px" CommitChanges="true" />
						<px:PXGridColumn DataField="CodeID_Description" Width="200px" />
						<px:PXGridColumn DataField="Amount" TextAlign="Right" CommitChanges="true" />
						<px:PXGridColumn DataField="LiabilityAccountID" Width="100px" CommitChanges="True" />
						<px:PXGridColumn DataField="LiabilitySubID" Width="100px" CommitChanges="True" />
						<px:PXGridColumn DataField="ExpenseAccountID" Width="100px" CommitChanges="True" />
						<px:PXGridColumn DataField="ExpenseSubID" Width="100px" CommitChanges="True" />
						<px:PXGridColumn DataField="ProjectID" Width="120px" CommitChanges="true" />
						<px:PXGridColumn DataField="ProjectTaskID" Width="120px" CommitChanges="true" />
						<px:PXGridColumn DataField="EarningTypeCD" Width="120px" CommitChanges="true" />
						<px:PXGridColumn DataField="LabourItemID" Width="70px" CommitChanges="true" />
						<px:PXGridColumn DataField="CostCodeID" Width="100px" CommitChanges="True" />
					</Columns>
				</px:PXGridLevel>
			</Levels>
			<AutoSize Enabled="true" MinHeight="150" />
		</px:PXGrid>
	</px:pxsmartpanel>
	<px:pxsmartpanel runat="server" id="spTaxDetails" autorepaint="true" caption="Tax Details" captionvisible="true" key="TaxDetails" width="300px" height="600px">
		<px:PXGrid ID="TaxDetailsGrid" runat="server" DataSourceID="ds" SkinID="Details">
			<Levels>
				<px:PXGridLevel DataMember="TaxDetails">
					<Columns>
						<px:PXGridColumn DataField="BranchID" CommitChanges="True" />
						<px:PXGridColumn DataField="TaxID" CommitChanges="True" Width="75px" />
						<px:PXGridColumn DataField="TaxID_Description" Width="150px" />
						<px:PXGridColumn DataField="TaxCategory" Width="100px" />
						<px:PXGridColumn DataField="Amount" Width="100px" CommitChanges="True" TextAlign="Right" />
						<px:PXGridColumn DataField="LiabilityAccountID" Width="100px" CommitChanges="True" />
						<px:PXGridColumn DataField="LiabilitySubID" Width="100px" CommitChanges="True" />
						<px:PXGridColumn DataField="ExpenseAccountID" Width="100px" CommitChanges="True" />
						<px:PXGridColumn DataField="ExpenseSubID" Width="100px" CommitChanges="True" />
						<px:PXGridColumn DataField="ProjectID" Width="120px" CommitChanges="true" />
						<px:PXGridColumn DataField="ProjectTaskID" Width="120px" CommitChanges="true" />
						<px:PXGridColumn DataField="EarningTypeCD" Width="120px" CommitChanges="true" />
						<px:PXGridColumn DataField="LabourItemID" Width="70px" CommitChanges="true" />
						<px:PXGridColumn DataField="CostCodeID" Width="100px" CommitChanges="True" />
					</Columns>
				</px:PXGridLevel>
			</Levels>
			<AutoSize Enabled="true" MinHeight="150" />
		</px:PXGrid>
	</px:pxsmartpanel>
	<px:PXSmartPanel runat="server" ID="spTaxableWageDetails" AutoRepaint="true" Caption="Taxable Wage Details" CaptionVisible="true" Key="PaymentTaxApplicableAmounts"
		Width="1200px" Height="600px" LoadOnDemand="True">
		<px:PXSplitContainer ID="taxableWageDetailsSplitContainer" runat="server" PositionInPercent="true" SplitterPosition="50" Orientation="Vertical" Height="100%">
			<Template1>
				<px:PXGrid ID="grdTaxAdjGrsExmptAmt" runat="server" DataSourceID="ds" SkinID="Inquire" Caption="Tax Applicable Amounts" CaptionVisible="true">
					<Levels>
						<px:PXGridLevel DataMember="Taxes">
							<Columns>
								<px:PXGridColumn DataField="TaxID" />
								<px:PXGridColumn DataField="AdjustedGrossAmount" />
								<px:PXGridColumn DataField="ExemptionAmount" />
							</Columns>
						</px:PXGridLevel>
					</Levels>
					<AutoSize Enabled="true" MinHeight="150" />
				</px:PXGrid>
			</Template1>
			<Template2>
				<px:PXGrid ID="grdTaxApplicablAmounts" runat="server" DataSourceID="ds" SkinID="Details" Caption="Wage Amounts Allowed" CaptionVisible="true">
					<Levels>
						<px:PXGridLevel DataMember="PaymentTaxApplicableAmounts">
							<RowTemplate>
								<px:PXSelector runat="server" ID="edWageTypeID" DataField="WageTypeID" />
							</RowTemplate>
							<Columns>
								<px:PXGridColumn DataField="TaxID" />
								<px:PXGridColumn DataField="WageTypeID" />
								<px:PXGridColumn DataField="IsSupplemental" Type="CheckBox" TextAlign="Center" />
								<px:PXGridColumn DataField="AmountAllowed" />
							</Columns>
						</px:PXGridLevel>
					</Levels>
					<AutoSize Enabled="true" MinHeight="150" />
				</px:PXGrid>
			</Template2>
		</px:PXSplitContainer>
	</px:PXSmartPanel>
	<px:PXSmartPanel runat="server" ID="pnlPTODetails" AutoRepaint="true" Caption="PTO Details" CaptionVisible="true" Key="PTODetails" Width="300px" Height="600px">
		<px:PXGrid ID="grdPTODetails" runat="server" DataSourceID="ds" SkinID="Details">
			<Levels>
				<px:PXGridLevel DataMember="PTODetails">
					<Columns>
						<px:PXGridColumn DataField="BranchID" CommitChanges="true" />
						<px:PXGridColumn DataField="BankID" CommitChanges="true" />
						<px:PXGridColumn DataField="BankID_Description" />
						<px:PXGridColumn DataField="Amount" TextAlign="Right" CommitChanges="true" />
						<px:PXGridColumn DataField="LiabilityAccountID" CommitChanges="True" />
						<px:PXGridColumn DataField="LiabilitySubID" CommitChanges="True" />
						<px:PXGridColumn DataField="AssetAccountID" CommitChanges="True" />
						<px:PXGridColumn DataField="AssetSubID" CommitChanges="True" />
						<px:PXGridColumn DataField="ExpenseAccountID" CommitChanges="True" />
						<px:PXGridColumn DataField="ExpenseSubID" CommitChanges="True" />
						<px:PXGridColumn DataField="ProjectID" CommitChanges="true" />
						<px:PXGridColumn DataField="ProjectTaskID" CommitChanges="true" />
						<px:PXGridColumn DataField="EarningTypeCD" CommitChanges="true" />
						<px:PXGridColumn DataField="LabourItemID" CommitChanges="true" />
						<px:PXGridColumn DataField="CostCodeID" CommitChanges="True" />
					</Columns>
				</px:PXGridLevel>
			</Levels>
			<AutoSize Enabled="true" MinHeight="150" />
		</px:PXGrid>
	</px:PXSmartPanel>
	<px:pxsmartpanel runat="server" ID="pnlDirectDepositSplits" Key="DirectDepositSplits" Caption="Direct Deposit Splits" CaptionVisible="true" AutoRepaint="true">
		<px:PXGrid ID="gridDDSplits" runat="server" DataSourceID="ds" SkinID="Inquire">
			<Levels>
				<px:PXGridLevel DataMember="DirectDepositSplits">
					<Columns>
						<px:PXGridColumn DataField="BankAcctNbr" />
						<px:PXGridColumn DataField="BankAcctType"/>
						<px:PXGridColumn DataField="BankName"/>
						<px:PXGridColumn DataField="BankRoutingNbr" />
						<px:PXGridColumn DataField="Amount"/>
					</Columns>
				</px:PXGridLevel>
			</Levels>
			<AutoSize Enabled="true" MinHeight="150" />
		</px:PXGrid>
	</px:pxsmartpanel>
	<px:pxsmartpanel runat="server" id="pnlProjectDedBenPackages" autorepaint="true" caption="Certified Project Deduction and Benefit Packages" captionvisible="true" key="ProjectPackageDeductions">
		<px:PXGrid ID="grdProject" runat="server" DataSourceID="ds" SkinID="DetailsInTab" Width="100%" Height="500px">
			<Levels>
				<px:PXGridLevel DataMember="ProjectPackageDeductions">
					<RowTemplate>
						<px:PXSelector runat="server" ID="edPackageProjectID" DataField="ProjectID" />
						<px:PXSelector runat="server" ID="edPackageProjectLaborItem" DataField="LaborItemID" />
						<px:PXSelector runat="server" ID="edPackageProjectDeductCode" DataField="DeductCodeID" />
					</RowTemplate>
					<Columns>
						<px:PXGridColumn DataField="ProjectID" Width="120px" CommitChanges="true" />
						<px:PXGridColumn DataField="LaborItemID" CommitChanges="true" />
						<px:PXGridColumn DataField="DeductCodeID" Width="120px" CommitChanges="true" />
						<px:PXGridColumn DataField="DeductionCalcType" />
						<px:PXGridColumn DataField="BenefitCalcType" />
						<px:PXGridColumn DataField="RegularWageBaseHours" Width="120px" CommitChanges="true" />
						<px:PXGridColumn DataField="OvertimeWageBaseHours" Width="120px" CommitChanges="true" />
						<px:PXGridColumn DataField="WageBaseHours" Width="120px" CommitChanges="true" />
						<px:PXGridColumn DataField="RegularWageBaseAmount" Width="120px" CommitChanges="true" />
						<px:PXGridColumn DataField="OvertimeWageBaseAmount" Width="120px" CommitChanges="true" />
						<px:PXGridColumn DataField="WageBaseAmount" Width="120px" CommitChanges="true" />
						<px:PXGridColumn DataField="DeductionAmount" Width="120px" />
						<px:PXGridColumn DataField="BenefitAmount" Width="120px" />
					</Columns>
				</px:PXGridLevel>
			</Levels>
			<AutoSize Enabled="true" MinHeight="150" />
		</px:PXGrid>
    </px:pxsmartpanel>
	<px:pxsmartpanel runat="server" id="pnlUpdateTaxesWarning" caption="Taxes need to be updated" captionvisible="true" key="UpdateTaxesPopupView" autorepaint="True">
        <px:PXFormView ID="formUpdateTaxesWarning" runat="server" DataSourceID="ds" DataMember="UpdateTaxesPopupView" RenderStyle="Simple" SkinID="Transparent">
            <Template>
                <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" SuppressLabel="True" />
                <px:PXTextEdit ID="edUpdateTaxesMessage" runat="server" DataField="Message" TextMode="MultiLine" Enabled="False" Height="100" Width="300" />
            </Template>
        </px:PXFormView>
		<px:PXPanel ID="pnlUpdateTaxesWarningButtons" runat="server" SkinID="Buttons">
			<px:PXButton ID="btnGoToTaxMaintenance" runat="server" CommandName="RedirectTaxMaintenance" CommandSourceID="ds" DialogResult="OK" AlignLeft="false" />
		</px:PXPanel>
    </px:pxsmartpanel>
</asp:Content>
