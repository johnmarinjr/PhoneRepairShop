<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="PR303000.aspx.cs"
	Inherits="Page_PR303000" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormTab.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.PR.PRRecordOfEmploymentMaint" PrimaryView="Document" HeaderDescriptionField="HeaderDescription">
		<CallbackCommands>
            <px:PXDSCallbackCommand Name="CopyPaste" Visible="false" />
			<px:PXDSCallbackCommand Name="AddressLookupSelectAction" CommitChanges="true" Visible="false" />
			<px:PXDSCallbackCommand Name="AddressLookup" SelectControlsIDs="headerForm" RepaintControls="None" RepaintControlsIDs="ds,fAddressInfo" CommitChanges="true" Visible="false" />
        </CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView ID="headerForm" runat="server" DataSourceID="ds" DataMember="Document" Width="100%">
		<Template>
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="SM" />
			<px:PXSelector ID="edRefNbr" runat="server" DataField="RefNbr" />
			<px:PXDropDown ID="edStatus" runat="server" DataField="Status" />
			<px:PXSelector ID="edEmployeeID" runat="server" DataField="EmployeeID" CommitChanges="True" />
			<px:PXCheckBox ID="edAmendment" runat="server" DataField="Amendment" CommitChanges="true" />
			<px:PXTextEdit ID="edAmendedRefNbr" runat="server" DataField="AmendedRefNbr" />

			<px:PXLayoutRule runat="server" LabelsWidth="M" ControlSize="XM" StartColumn="True" />
			<px:PXDropDown ID="edReasonForROE" runat="server" DataField="ReasonForROE" CommitChanges="True" />
			<px:PXDropDown ID="edPeriodType" runat="server" DataField="PeriodType" />			
			<px:PXTextEdit ID="edComments" runat="server" DataField="Comments" CommitChanges="True" />
			<px:PXTextEdit ID="edDocDesc" runat="server" DataField="DocDesc" CommitChanges="True" />
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXTab ID="tab" runat="server" Width="100%" Height="150px" DataSourceID="ds" DataMember="CurrentDocument">
		<Items>
			<px:PXTabItem Text="Administrative" BindingContext="form" RepaintOnDemand="false">
				<Template>
					<px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="M" />
					<px:PXSegmentMask CommitChanges="True" ID="edBranchID" runat="server" DataField="BranchID" />
					<px:PXFormView ID="fAddressInfo" runat="server" DataSourceID="ds" DataMember="Address" Caption="Employer Info" RenderStyle="FieldSet" TabIndex="300">
						<Template>
							<px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="M" StartColumn="True" />
							<px:PXButton ID="btnAddressLookup" runat="server" CommandName="AddressLookup" CommandSourceID="ds" Size="xs" TabIndex="-1" />
							<px:PXLayoutRule runat="server" />
							<px:PXTextEdit ID="edAddressLine1" runat="server" DataField="AddressLine1" />
							<px:PXTextEdit ID="edAddressLine2" runat="server" DataField="AddressLine2" />
							<px:PXTextEdit ID="edCity" runat="server" DataField="City" />
							<px:PXSelector ID="edCountryID" runat="server" DataField="CountryID" AllowAddNew="True" DataSourceID="ds" CommitChanges="true" AutoRefresh="False" />
							<px:PXSelector ID="edState" runat="server" DataField="State" AllowAddNew="True" DataSourceID="ds" AutoRefresh="True" />
							<px:PXMaskEdit ID="edPostalCode" runat="server" DataField="PostalCode" CommitChanges="true" />
						</Template>
					</px:PXFormView>
					<px:PXLayoutRule runat="server" LabelsWidth="XM" ControlSize="XM" StartColumn="True" />
					<px:PXTextEdit ID="edCRAPayrollAccountNumber" runat="server" DataField="CRAPayrollAccountNumber" />
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Period of Employment" BindingContext="form" RepaintOnDemand="false">
				<Template>
					<px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="XM" StartColumn="True" />
					<px:PXDateTimeEdit ID="edFirstDayWorked" runat="server" DataField="FirstDayWorked" CommitChanges="True" />
					<px:PXDateTimeEdit ID="edLastDayForWhichPaid" runat="server" DataField="LastDayForWhichPaid" CommitChanges="True" />
					<px:PXDateTimeEdit ID="edFinalPayPeriodEndingDate" runat="server" DataField="FinalPayPeriodEndingDate" CommitChanges="True" />
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Separation Payments">
				<Template>
					<px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="M" StartColumn="True" />
					<px:PXNumberEdit ID="edVacationPay" runat="server" DataField="VacationPay" />
					<px:PXSplitContainer ID="splitContainerHolidaysAndOtherMonies" runat="server" PositionInPercent="true" SplitterPosition="50" Orientation="Vertical" Height="400px" Width="1000px">
						<Template1>
							<px:PXGrid ID="gridStatutoryHolidays" runat="server" DataSourceID="ds" SkinID="DetailsInTab" Caption="Statutory Holidays Pay For (Block 17B):" Height="100%">
								<Levels>
									<px:PXGridLevel DataMember="StatutoryHolidays">
										<Columns>
											<px:PXGridColumn DataField="Date" />
											<px:PXGridColumn DataField="Amount" />
										</Columns>
									</px:PXGridLevel>
								</Levels>
							</px:PXGrid>
						</Template1>
						<Template2>
							<px:PXGrid ID="gridOtherMonies" runat="server" DataSourceID="ds" SkinID="DetailsInTab" Caption="Other Monies (Block 17C):" Height="100%">
								<Levels>
									<px:PXGridLevel DataMember="OtherMonies">
										<Columns>
											<px:PXGridColumn DataField="TypeCD" />
											<px:PXGridColumn DataField="TypeCD_Description" />
											<px:PXGridColumn DataField="Amount" />
										</Columns>
									</px:PXGridLevel>
								</Levels>
							</px:PXGrid>
						</Template2>
					</px:PXSplitContainer>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Insurable Hours">
				<Template>
					<px:PXSplitContainer ID="splitContainerInsurableEarnings" runat="server" PositionInPercent="true" SplitterPosition="50" Orientation="Vertical" Height="400px" Width="1000px">
						<Template1>
							<px:PXGrid ID="gridInsurableEarnings" runat="server" DataSourceID="ds" SkinID="DetailsInTab" Height="100%" Caption="Insurable Earnings by Pay Period (Block 15C):" >
								<Levels>
									<px:PXGridLevel DataMember="InsurableEarnings">
										<Columns>
											<px:PXGridColumn DataField="PayPeriodID" />
											<px:PXGridColumn DataField="InsurableEarnings" />
											<px:PXGridColumn DataField="InsurableHours" />
										</Columns>
									</px:PXGridLevel>
								</Levels>
								<AutoSize Enabled="true" />
							</px:PXGrid>
						</Template1>
						<Template2>
							<px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="M" StartColumn="True" />
							<px:PXNumberEdit ID="edTotalInsurableHours" runat="server" DataField="TotalInsurableHours" />
							<px:PXNumberEdit ID="edTotalInsurableEarnings" runat="server" DataField="TotalInsurableEarnings" />
						</Template2>
					</px:PXSplitContainer>
				</Template>
			</px:PXTabItem>
		</Items>
		<AutoSize Container="Window" Enabled="True" MinHeight="150" />
	</px:PXTab>
	<!--#include file="~\Pages\Includes\AddressLookupPanel.inc"-->
</asp:Content>
