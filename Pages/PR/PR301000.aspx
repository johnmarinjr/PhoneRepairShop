<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="PR301000.aspx.cs"
	Inherits="Page_PR301000" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormTab.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<script type="text/javascript">

		function onDSCommandPerformed(src, args) {
			if (args.command == "ReloadPage") {
				px_alls.ds.executeCallback("CheckTaxUpdateTimestamp");
			}
		}
	</script>
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.PR.PRPayBatchEntry" PrimaryView="Document" HeaderDescriptionField="HeaderDescription">
		<CallbackCommands>
			<px:PXDSCallbackCommand CommitChanges="True" Name="Save" />
			<px:PXDSCallbackCommand Name="Insert" PostData="Self" />
			<px:PXDSCallbackCommand Name="First" PostData="Self" StartNewGroup="True" />
			<px:PXDSCallbackCommand Name="Last" PostData="Self" />
            <px:PXDSCallbackCommand Name="CopyPaste" Visible="false" />
			<px:PXDSCallbackCommand Name="AddEmployees" Visible="False" CommitChanges="true" />
			<px:PXDSCallbackCommand Name="AddSelectedEmployees" Visible="False" CommitChanges="true" />
            <px:PXDSCallbackCommand Name="AddSelectedEmployeesAndClose" Visible="False" CommitChanges="true" />
            <px:PXDSCallbackCommand Name="ToggleSelected" Visible="False" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="ViewEarningDetails" Visible="False" CommitChanges="true" />
            <px:PXDSCallbackCommand Name="CopySelectedEarningDetailLine" Visible="false" />
			<px:PXDSCallbackCommand Name="CopySelectedEmployeeEarningDetailLine" Visible="false" />
			<px:PXDSCallbackCommand Name="ImportTimeActivities" Visible="false" />
			<px:PXDSCallbackCommand Name="ToggleSelectedTimeActivities" Visible="False" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="AddSelectedTimeActivities" Visible="False" CommitChanges="true" />
            <px:PXDSCallbackCommand Name="AddSelectedTimeActivitiesAndClose" Visible="False" CommitChanges="true" />
			<px:PXDSCallbackCommand Name="ViewTimeActivity" Visible="False" DependOnGrid="gridEarningDetails" />
            <px:PXDSCallbackCommand Name="ViewPayCheck" Visible="False" DependOnGrid="PXGrid1" />
            <px:PXDSCallbackCommand Name="ViewVoidPayCheck" Visible="False" DependOnGrid="PXGrid1" />
			<px:PXDSCallbackCommand Name="RedirectTaxMaintenance" Visible="false" />
        </CallbackCommands>
		<ClientEvents CommandPerformed="onDSCommandPerformed" />
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" DataMember="Document" Width="100%">
		<Template>
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="S" />
			<px:PXSelector ID="edBatchNbr" runat="server" DataField="BatchNbr" AutoRefresh="True" CommitChanges="True" />
			<px:PXDropDown ID="edStatus" runat="server" DataField="Status" Width="100px" />
			<px:PXCheckBox ID="edHold" runat="server" DataField="Hold" CommitChanges="True" />
			<px:PXDropDown ID="edPayrollType" runat="server" DataField="PayrollType" CommitChanges="True" />
			<px:PXSelector ID="edPayGroupID" runat="server" DataField="PayGroupID" CommitChanges="True" />

			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
			<px:PXSelector ID="edPayPeriodID" runat="server" DataField="PayPeriodID" CommitChanges="True" AutoRefresh="True"/>
			<px:PXDateTimeEdit ID="edStartDate" runat="server" DataField="StartDate" />
			<px:PXDateTimeEdit ID="edEndDate" runat="server" DataField="EndDate" />
			<px:PXDateTimeEdit ID="edTransactionDate" runat="server" DataField="TransactionDate" />
			<px:PXLayoutRule runat="server" ColumnSpan="2"></px:PXLayoutRule>
			<px:PXTextEdit ID="edDocDesc" runat="server" DataField="DocDesc" />

			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" StartGroup="True" />
			<px:PXPanel ID="XX" runat="server" RenderStyle="Simple">
				<px:PXLayoutRule runat="server" ControlSize="SM" LabelsWidth="SM" StartColumn="True" />
				<px:PXNumberEdit ID="edNumberOfEmployees" runat="server" DataField="NumberOfEmployees" />
				<px:PXNumberEdit ID="edTotalHourQty" runat="server" DataField="PRBatchTotalsFilter.TotalHourQty" />
				<px:PXNumberEdit ID="edTotalEarnings" runat="server" DataField="PRBatchTotalsFilter.TotalEarnings" />
			</px:PXPanel>
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXTab ID="tab" runat="server" Width="100%" Height="150px" DataSourceID="ds" DataMember="Transactions">
		<Items>
			<px:PXTabItem Text="Employee">
				<Template>
					<px:PXGrid ID="PXGrid1" runat="server" DataSourceID="ds" SkinID="DetailsInTab" Width="100%" KeepPosition="True" SyncPosition="True" AllowPaging="True" AdjustPageSize="Auto">
						<Levels>
							<px:PXGridLevel DataMember="Transactions">
                                <Mode AllowAddNew="false" />
								<Columns>
                                    <px:PXGridColumn DataField="EmployeeID" CommitChanges="True" AllowShowHide="False" Visible="False"/>
                                    <px:PXGridColumn DataField="AcctCD" Width="120px" />
                                    <px:PXGridColumn DataField="AcctName" Width="300px" />
									<px:PXGridColumn DataField="HourQty" Width="60px" CommitChanges="True" TextAlign="Right" />
									<px:PXGridColumn DataField="Rate" TextAlign="Right" CommitChanges="True" />
									<px:PXGridColumn DataField="Amount" Width="100px" TextAlign="Right" />
                                    <px:PXGridColumn DataField="PaymentDocAndRef" Width="105px"
                                        LinkCommand="ViewPayCheck">
                                    </px:PXGridColumn>
                                    <px:PXGridColumn DataField="VoidPaymentDocAndRef" Width="145px"
                                        LinkCommand="ViewVoidPayCheck">
                                    </px:PXGridColumn>
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<AutoSize Enabled="True" />
						<ActionBar>
                            <Actions>
                                <AddNew ToolBarVisible = "False" />
                            </Actions>
							<CustomItems>
								<px:PXToolBarButton Text="Add Bulk Employees">
									<AutoCallBack Command="AddEmployees" Target="ds">
										<Behavior CommitChanges="True" PostData="Page" />
									</AutoCallBack>
								</px:PXToolBarButton>
								<px:PXToolBarButton Text="Employee Earning Details">
									<AutoCallBack Command="ViewEarningDetails" Target="ds">
										<Behavior CommitChanges="True" PostData="Page" />
									</AutoCallBack>
								</px:PXToolBarButton>
							</CustomItems>
						</ActionBar>
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Earning">
				<Template>
					<px:PXGrid ID="gridEarningDetails" runat="server" DataSourceID="ds" Width="100%" SkinID="Details" AdjustPageSize="Auto" AllowSearch="True" SyncPosition="True" >
						<CallbackCommands>
							<Refresh CommitChanges="true" />
						</CallbackCommands>
						<ActionBar PagerVisible="False">
							<PagerSettings Mode="NextPrevFirstLast" />
						</ActionBar>
						<Levels>
							<px:PXGridLevel DataMember="EarningDetails">
								<Columns>
									<px:PXGridColumn DataField="AllowCopy" Type="CheckBox" AllowShowHide="False" Visible="False" />
									<px:PXGridColumn DataField="EmployeeID" CommitChanges="True" Width="120px" />
									<px:PXGridColumn DataField="EmployeeID_Description" Width="300px" />
									<px:PXGridColumn DataField="BranchID" Width="90px" CommitChanges="True" />
									<px:PXGridColumn DataField="Date" Width="80px" CommitChanges="True" />
									<px:PXGridColumn DataField="TypeCD" Width="65px" CommitChanges="True" />
									<px:PXGridColumn DataField="TypeCD_EPEarningType_Description" Width="110px" />
									<px:PXGridColumn DataField="LocationID" Width="70px" CommitChanges="True" />
									<px:PXGridColumn DataField="Hours" Width="60px" CommitChanges="True" TextAlign="Right" />
									<px:PXGridColumn DataField="Units" Width="60px" CommitChanges="True" TextAlign="Right" />
									<px:PXGridColumn DataField="UnitType" Width="60px" />
									<px:PXGridColumn DataField="Rate" Width="60" TextAlign="Right" CommitChanges="True" />
									<px:PXGridColumn DataField="ManualRate" Type="CheckBox" CommitChanges="True" />
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
									<px:PXGridColumn DataField="ExcelRecordID" />
								</Columns>
								<RowTemplate>
									<px:PXSelector ID="edShiftID" runat="server" DataField="ShiftID" AutoRefresh="true"/>
								</RowTemplate>
							</px:PXGridLevel>
						</Levels>
						<Mode InitNewRow="True" AllowUpload="True" />
						<ActionBar>
							<CustomItems>
								<px:PXToolBarButton Text="Copy Selected Entry" DependOnGrid="gridEarningDetails" CommandSourceID="ds" StateColumn="AllowCopy">
									<AutoCallBack Command="CopySelectedEarningDetailLine" Target="ds" />
								</px:PXToolBarButton>
								<px:PXToolBarButton Text="Import Time Activities" CommandSourceID="ds">
									<AutoCallBack Command="ImportTimeActivities" Target="ds" />
								</px:PXToolBarButton>
							</CustomItems>
						</ActionBar>
						<AutoSize Enabled="true" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Deductions and Benefits">
				<Template>
					<px:PXGrid runat="server" ID="PXGrid2" SkinID="Inquire" SyncPosition="True" DataSourceID="ds" KeepPosition="True" Width="100%" AllowPaging="True" AdjustPageSize="Auto">
						<Levels>
							<px:PXGridLevel DataMember="Deductions">
								<Columns>
                                    <px:PXGridColumn DataField="IsEnabled" Type="CheckBox" AllowCheckAll="True" TextAlign="Center" />
									<px:PXGridColumn DataField="CodeID" Width="140" />
									<px:PXGridColumn DataField="PRDeductCode__Description" Width="220" />
									<px:PXGridColumn DataField="PRDeductCode__ContribType" Width="70" />
									<px:PXGridColumn DataField="PRDeductCode__DedCalcType" Width="70" />
									<px:PXGridColumn DataField="PRDeductCode__DedAmount" Width="100" />
									<px:PXGridColumn DataField="PRDeductCode__DedPercent" Width="100" />
									<px:PXGridColumn DataField="PRDeductCode__CntCalcType" Width="70" />
									<px:PXGridColumn DataField="PRDeductCode__CntAmount" Width="100" />
									<px:PXGridColumn DataField="PRDeductCode__CntPercent" Width="100" />
									<px:PXGridColumn DataField="PRDeductCode__IsGarnishment" Width="60" Type="CheckBox"/>
								</Columns>
								<RowTemplate>
									<px:PXSelector runat="server" ID="codeIDSelector" DataField="CodeID" AllowEdit="True" />
								</RowTemplate>
							</px:PXGridLevel>
						</Levels>
                        <AutoSize Enabled="True" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
            <px:PXTabItem Text="Overtime Rules">
                <Template>
                    <px:PXFormView ID="formOvertimeRules" runat="server" DataSourceID="ds" DataMember="CurrentDocument" RenderStyle="Normal" SkinID="Transparent">
                        <Template>
                            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
                            <px:PXCheckBox ID="chkApplyOvertimeRules" runat="server" DataField="ApplyOvertimeRules" AlignLeft="true" CommitChanges="true"  />
                        </Template>
                    </px:PXFormView>
                    <px:PXGrid ID="PXGridOvertimeRules" runat="server" DataSourceID="ds" SkinID="Inquire" Width="100%" KeepPosition="True" SyncPosition="True" AllowPaging="True" AdjustPageSize="Auto">
                        <Levels>
                            <px:PXGridLevel DataMember="BatchOvertimeRules">
                                <Columns>
                                    <px:PXGridColumn DataField="IsActive" TextAlign="Center" Type="CheckBox" AllowCheckAll="True" CommitChanges="True" />
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
                        <AutoSize Enabled="True" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
		</Items>
		<AutoSize Container="Window" Enabled="True" MinHeight="150" />
	</px:PXTab>

	<%-- Employees Lookup --%>
	<px:PXSmartPanel ID="PanelEmployees" runat="server" Key="employees" Width="1100px" Height="500px"
		Caption="Add Bulk Employees" CaptionVisible="true" AutoRepaint="true" DesignView="Content" ShowAfterLoad="true" CloseButtonDialogResult="No">
		<px:PXFormView ID="formEmployees" runat="server" CaptionVisible="False" DataMember="AddEmployeeFilter" DataSourceID="ds"
			Width="100%" SkinID="Transparent">
			<Template>
				<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
				<px:PXSelector ID="edEmployeeClassID" runat="server" DataField="EmployeeClassID" CommitChanges="True" />
                <px:PXDropDown ID="edEmployeeType" runat="server" DataField="EmployeeType" CommitChanges="True" />

				<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
				<px:PXCheckBox CommitChanges="True" ID="chkUseQuickPay" AlignLeft="true" runat="server" DataField="UseQuickPay" Width="320" />
				<px:PXCheckBox CommitChanges="True" ID="chkUseTimeSheets" AlignLeft="true" runat="server" DataField="UseTimeSheets" Width="320" />
				<px:PXCheckBox CommitChanges="True" ID="chkUseSalesComm" AlignLeft="true" runat="server" DataField="UseSalesComm" Width="320" />
			</Template>
		</px:PXFormView>
		<px:PXGrid ID="gridEmployees" runat="server" DataSourceID="ds" Style="height: 189px;"
			AutoAdjustColumns="true" Width="100%" SkinID="Inquire" AdjustPageSize="Auto" AllowSearch="True" FastFilterID="edEmployeeClassID"
            FastFilterFields="EmployeeClassID, EmployeeType">
			<CallbackCommands>
				<Refresh CommitChanges="true"></Refresh>
			</CallbackCommands>
			<ActionBar PagerVisible="False">
				<PagerSettings Mode="NextPrevFirstLast" />
                <CustomItems>
                    <px:PXToolBarButton Text="Toggle Selected" Key="cmdToggleSelected">
                        <AutoCallBack Command="ToggleSelected" Target="ds" />
                    </px:PXToolBarButton>
                </CustomItems>
			</ActionBar>
			<Levels>
				<px:PXGridLevel DataMember="Employees">
					<Columns>
						<px:PXGridColumn AllowNull="False" DataField="Selected" TextAlign="Center" Type="CheckBox" />
						<px:PXGridColumn DataField="AcctCD" />
						<px:PXGridColumn DataField="AcctName" />
						<px:PXGridColumn DataField="EmployeeClassID" />
                        <px:PXGridColumn DataField="EmpType" />
                        <px:PXGridColumn DataField="ActivePositionID" />
                        <px:PXGridColumn DataField="DepartmentID" />
					</Columns>
				</px:PXGridLevel>
			</Levels>
			<AutoSize Enabled="true" />
		</px:PXGrid>
		<px:PXPanel ID="addEmployeesButtonPanel" runat="server" SkinID="Buttons">
			<px:PXButton ID="addSelectedEmployeesBtn" runat="server" CommandName="AddSelectedEmployees" CommandSourceID="ds" Text="Add" DependOnGrid="gridEmployees" />
			<px:PXButton ID="addSelectedEmployeesAndCloseBtn" runat="server" CommandName="AddSelectedEmployeesAndClose" CommandSourceID="ds" Text="Add & Close" DependOnGrid="gridEmployees" DialogResult="OK" />
			<px:PXButton ID="PXButton6" runat="server" DialogResult="No" Text="Cancel" />
		</px:PXPanel>
	</px:PXSmartPanel>

	<%-- Earning Details Lookup --%>
	<px:PXSmartPanel ID="PanelEarningDetails" runat="server" Key="EmployeeEarningDetails" Width="1100px" Height="700px"
		Caption="Employee Earning Details" CaptionVisible="true" AutoRepaint="true" DesignView="Content" ShowAfterLoad="true">
		<px:PXFormView ID="formEarningDetails" runat="server" CaptionVisible="False" DataMember="CurrentTransaction" DataSourceID="ds"
			Width="100%" SkinID="Transparent">
			<Template>
				<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
				<px:PXTextEdit ID="edEmployeeID" runat="server" DataField="AcctCD" Enabled="False" />
				<px:PXDropDown ID="edEmpType" runat="server" DataField="EmpType" CommitChanges="True" />
                <px:PXLayoutRule runat="server" Merge="True" />
                <px:PXNumberEdit ID="edRegularAmount" runat="server" DataField="RegularAmount" CommitChanges="True" />
                <px:PXCheckBox ID="edManualRegularAmount" runat="server" DataField="ManualRegularAmount" CommitChanges="True" />
                <px:PXLayoutRule runat="server" />
				<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
				<px:PXNumberEdit ID="edHourQty" runat="server" DataField="HourQty" Enabled="False" />
				<px:PXNumberEdit ID="edAmount" runat="server" DataField="Amount" Enabled="False" />
			</Template>
		</px:PXFormView>
		<px:PXGrid ID="gridEmployeeEarningDetails" runat="server" DataSourceID="ds" Style="height: 189px;" Width="100%" SkinID="Details" AdjustPageSize="Auto" AllowSearch="True" SyncPosition="True" >
			<CallbackCommands>
				<Refresh CommitChanges="true"></Refresh>
			</CallbackCommands>
			<ActionBar PagerVisible="False">
				<PagerSettings Mode="NextPrevFirstLast" />
			</ActionBar>
			<Levels>
				<px:PXGridLevel DataMember="EmployeeEarningDetails">
					<Mode InitNewRow="True" />
					<RowTemplate>
						<px:PXSelector ID="edShiftID2" runat="server" DataField="ShiftID" AutoRefresh="true"/>
					</RowTemplate>
					<Columns>
                        <px:PXGridColumn DataField="AllowCopy" Type="CheckBox" AllowShowHide="False" Visible="False" />
                        <px:PXGridColumn DataField="BranchID" Width="90px" CommitChanges="True" />
                        <px:PXGridColumn DataField="Date" Width="80px" CommitChanges="True" />
                        <px:PXGridColumn DataField="TypeCD" Width="65px" CommitChanges="True" />
                        <px:PXGridColumn DataField="TypeCD_EPEarningType_Description" Width="110px" />
                        <px:PXGridColumn DataField="LocationID" Width="70px" CommitChanges="True" />
                        <px:PXGridColumn DataField="Hours" Width="60px" CommitChanges="True" TextAlign="Right" />
                        <px:PXGridColumn DataField="Units" Width="60px" CommitChanges="True" TextAlign="Right" />
                        <px:PXGridColumn DataField="UnitType" Width="60px" />
                        <px:PXGridColumn DataField="Rate" Width="60" TextAlign="Right" CommitChanges="True" />
                        <px:PXGridColumn DataField="ManualRate" Type="CheckBox" CommitChanges="True" />
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
						<px:PXGridColumn DataField="ShiftID" Width="70px" CommitChanges="True" />
					</Columns>
				</px:PXGridLevel>
			</Levels>
            <ActionBar>
                <CustomItems>
                    <px:PXToolBarButton Text="Copy Selected Entry" DependOnGrid="gridEmployeeEarningDetails" CommandSourceID="ds" StateColumn="AllowCopy">
                        <AutoCallBack Command="CopySelectedEmployeeEarningDetailLine" Target="ds" />
                    </px:PXToolBarButton>
                </CustomItems>
            </ActionBar>
			<AutoSize Enabled="true" />
		</px:PXGrid>
		<px:PXPanel ID="PXPanel1" runat="server" SkinID="Buttons">
			<px:PXButton ID="PXButton2" runat="server" Text="OK" DialogResult="OK" />
		</px:PXPanel>
	</px:PXSmartPanel>
	<px:PXSmartPanel ID="PanelTimeActivities" runat="server" Key="TimeActivities" Width="1100px" Height="500px"
		Caption="Import Time Activities" CaptionVisible="true" AutoRepaint="true" DesignView="Content" ShowAfterLoad="true">
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
			<px:PXButton ID="PXButton3" runat="server" CommandName="AddSelectedTimeActivitiesAndClose" CommandSourceID="ds" Text="Add & Close" DependOnGrid="gridTimeActivities" DialogResult="OK" />
			<px:PXButton ID="PXButton4" runat="server" DialogResult="No" Text="Cancel" />
		</px:PXPanel>
	</px:PXSmartPanel>
	<px:PXSmartPanel runat="server" ID="pnlUpdateTaxesWarning" Caption="Taxes need to be updated" CaptionVisible="true" Key="UpdateTaxesPopupView" AutoRepaint="True">
        <px:PXFormView ID="formUpdateTaxesWarning" runat="server" DataSourceID="ds" DataMember="UpdateTaxesPopupView" RenderStyle="Simple" SkinID="Transparent">
            <Template>
                <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" SuppressLabel="True" />
                <px:PXTextEdit ID="edUpdateTaxesMessage" runat="server" DataField="Message" TextMode="MultiLine" Enabled="False" Height="100" Width="300" />
            </Template>
        </px:PXFormView>
		<px:PXPanel ID="pnlUpdateTaxesWarningButtons" runat="server" SkinID="Buttons">
			<px:PXButton ID="btnGoToTaxMaintenance" runat="server" Text="Tax Maintenance" CommandName="RedirectTaxMaintenance" CommandSourceID="ds" DialogResult="OK" AlignLeft="false" />
		</px:PXPanel>
    </px:PXSmartPanel>
</asp:Content>
