<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="PR101060.aspx.cs"
	Inherits="Page_PR101060" Title="Deductions and Benefits" %>

<%@ MasterType VirtualPath="~/MasterPages/FormTab.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" PrimaryView="Document" TypeName="PX.Objects.PR.PRDedBenCodeMaint" HeaderDescriptionField="Description">
		<CallbackCommands>
			<px:PXDSCallbackCommand CommitChanges="True" Name="Save" PopupCommand="" PopupCommandTarget="" PopupPanel="" />
			<px:PXDSCallbackCommand Name="Insert" PostData="Self" />
			<px:PXDSCallbackCommand Name="CopyPaste" Visible="False" />
			<px:PXDSCallbackCommand Name="First" PostData="Self" StartNewGroup="True" RepaintControls="All" />
			<px:PXDSCallbackCommand Name="Last" PostData="Self" RepaintControls="All" />
			<px:PXDSCallbackCommand Name="Next" RepaintControls="All" />
			<px:PXDSCallbackCommand Name="Previous" RepaintControls="All" />
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" DataMember="Document" TabIndex="2500" AllowCollapse="true">
		<Template>
			<!-- HEADER AREA -->
			<px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="M" />
			<px:PXSelector ID="edCodeCD" runat="server" DataField="CodeCD" CommitChanges="true" />
			<px:PXTextEdit runat="server" DataField="Description" ID="edDescription" />
					
			<px:PXDropDown runat="server" DataField="ContribType" ID="edContribType" CommitChanges="True" />
			<px:PXDropDown runat="server" DataField="AssociatedSource" ID="edAssociatedSource" CommitChanges="true" />
			<px:PXSelector ID="edBAccountID" runat="server" AllowEdit="true" DataField="BAccountID" />
				<px:PXDropDown runat="server" DataField="DedInvDescrType" ID="edDedInvDescrType" CommitChanges="True" />

			<px:PXLayoutRule runat="server" StartColumn="True" ControlSize="XM" LabelsWidth="S" />
				<px:PXCheckBox runat="server" DataField="IsActive" ID="chkIsActive" />
				<px:PXCheckBox runat="server" DataField="IsGarnishment" ID="chkIsGarnishment" CommitChanges="true" />
				<px:PXCheckBox runat="server" DataField="AffectsTaxes" ID="chkAffectsTaxes" CommitChanges="true" />
				<px:PXCheckBox runat="server" DataField="AcaApplicable" ID="chkAcaApplicable" CommitChanges="true" />
                <px:PXCheckBox runat="server" DataField="IsPayableBenefit" ID="chkIsPayableBenefit" CommitChanges="true" />

			<px:PXLayoutRule runat="server" StartRow="True" LabelsWidth="M" ColumnSpan="2" />
				<px:PXTextEdit runat="server" DataField="VndInvDescr" ID="edVndInvDescr" />		

			<px:PXCheckBox runat="server" ID="chkShowApplicableWageTab" DataField="ShowApplicableWageTab" />
			<px:PXCheckBox runat="server" ID="hidShowUSTaxSettingsTab" DataField="ShowUSTaxSettingsTab" Visible="false" />
			<px:PXCheckBox runat="server" ID="hidShowCANTaxSettingsTab" DataField="ShowCANTaxSettingsTab" Visible="false" />
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXTab ID="tab" runat="server" Width="100%" Style="margin-top: 0px;" DataMember="CurrentDocument">
		<Items>
			<px:PXTabItem Text="US Tax Settings" VisibleExp="DataControls[&quot;hidShowUSTaxSettingsTab&quot;].Value==true" BindingContext="form">
				<Template>
					<px:PXFormView ID="taxSettingsForm" runat="server" DataMember="CurrentDocument">
						<Template>
							<px:PXLayoutRule runat="server" StartRow="True" ControlSize="XM" LabelsWidth="SM" />
								<px:PXDropDown runat="server" ID="edIncludeType" DataField="IncludeType" CommitChanges="True" />
								<px:PXSelector runat="server" ID="edBenefitTypeCD" DataField="BenefitTypeCD" CommitChanges="true" />
							<px:PXLayoutRule runat="server" StartColumn="True" ControlSize="XM" LabelsWidth="SM" />
								<px:PXCheckBox runat="server" DataField="AllowSupplementalElection" ID="edAllowSupplementalElection" />
						</Template>
					</px:PXFormView>
					<px:PXGrid ID="grdUSTaxability" runat="server" DataSourceID="ds" Width="100%" SkinID="Details" AllowPaging="false">
						<Levels>
							<px:PXGridLevel DataMember="DeductCodeTaxesUS" DataKeyNames="CodeID, TaxID">
								<Columns>
									<px:PXGridColumn DataField="TaxID" Width="125px" CommitChanges="true" />
									<px:PXGridColumn DataField="TaxID_Description" Width="300px" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<AutoSize Enabled="True" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="CAN Tax Settings" VisibleExp="DataControls[&quot;hidShowCANTaxSettingsTab&quot;].Value==true" BindingContext="form">
				<Template>
					<px:PXFormView ID="frmCANTaxSettings" runat="server" DataMember="CurrentDocument">
						<Template>
							<px:PXLayoutRule runat="server" StartRow="True" ControlSize="XM" LabelsWidth="SM" />
								<px:PXSelector runat="server" ID="edBenefitTypeCDCAN" DataField="BenefitTypeCDCAN" CommitChanges="true" />
						</Template>
					</px:PXFormView>
					<px:PXGrid ID="grdCANTaxability" runat="server" DataSourceID="ds" Width="100%" SkinID="Inquire" AllowPaging="false">
						<Levels>
							<px:PXGridLevel DataMember="DeductCodeTaxesCAN">
								<Columns>
									<px:PXGridColumn DataField="TaxID" Width="125px" CommitChanges="true" />
									<px:PXGridColumn DataField="TaxID_Description" Width="300px" />
									<px:PXGridColumn DataField="IsDeductionPreTax" Type="CheckBox" TextAlign="Center" Width="100px" />
									<px:PXGridColumn DataField="IsBenefitTaxable" Type="CheckBox" TextAlign="Center" Width="100px" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<AutoSize Enabled="True" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem VisibleExp="DataControls[&quot;chkAcaApplicable&quot;].Value==true" BindingContext="form" Text="ACA Information">
				<AutoCallBack Enabled="true" Command="Refresh" Target="gridAca"></AutoCallBack>
				<Template>
					<px:PXFormView ID="acaInformationForm" runat="server" DataMember="CurrentDocument">
						<Template>
							<px:PXLayoutRule runat="server" StartRow="True" ControlSize="XM" LabelsWidth="M" />
								<px:PXNumberEdit runat="server" ID="edMinimumIndividualContribution" DataField="MinimumIndividualContribution" />
						</Template>
					</px:PXFormView>
					<px:PXGrid ID="gridAca" runat="server" DataSourceID="ds" Height="105px" Width="100%" SkinID="Details" AllowPaging="false">
						<Levels>
							<px:PXGridLevel DataMember="AcaInformation" DataKeyNames="CodeID, CoverageType">
								<Columns>
									<px:PXGridColumn DataField="CoverageType" Width="250px" CommitChanges="true" />
									<px:PXGridColumn DataField="HealthPlanType" Width="650px" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<AutoSize Container="Parent" Enabled="True" MinHeight="200"></AutoSize>
						<ActionBar ActionsText="False"></ActionBar>
						<AutoCallBack ActiveBehavior="true" Enabled="true"></AutoCallBack>
					</px:PXGrid>					
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Employee Deduction" VisibleExp="DataControls[&quot;edContribType&quot;].Value!=CNT" BindingContext="form" >
				<Template>
					<px:PXLayoutRule runat="server" StartColumn="True" ControlSize="XM" LabelsWidth="M" />
                    <px:PXDropDown runat="server" ID="edDedCalcType" DataField="DedCalcType" CommitChanges="True" />
					<px:PXNumberEdit runat="server" ID="edDedAmount" DataField="DedAmount" AllowNull="True" />
					<px:PXNumberEdit runat="server" ID="edDedPercent" DataField="DedPercent" AllowNull="True" />
					<px:PXDropDown runat="server" ID="edDedMaxFreqType" DataField="DedMaxFreqType" CommitChanges="True" />
					<px:PXNumberEdit runat="server" ID="edDedMaxAmount" DataField="DedMaxAmount" AllowNull="True" />
					<px:PXDropDown runat="server" ID="edDedApplicableEarnings" DataField="DedApplicableEarnings" CommitChanges="True" />
					<px:PXSelector runat="server" ID="edDedReportType" DataField="DedReportType" />
					<px:PXSelector runat="server" ID="edDedReportTypeCAN" DataField="DedReportTypeCAN" />
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Employer Contribution" VisibleExp="DataControls[&quot;edContribType&quot;].Value!=DED" BindingContext="form" >
				<Template>
					<px:PXLayoutRule runat="server" StartColumn="True" ControlSize="XM" LabelsWidth="M" />
					    <px:PXDropDown runat="server" ID="edCntCalcType" DataField="CntCalcType" CommitChanges="True" />
					<px:PXNumberEdit runat="server" ID="edCntAmount" DataField="CNtAmount" AllowNull="True" />
					<px:PXNumberEdit runat="server" ID="edCntPercent" DataField="CntPercent" AllowNull="True" />
					<px:PXDropDown runat="server" ID="edCntMaxFreqType" DataField="CntMaxFreqType" CommitChanges="True" />
					<px:PXNumberEdit runat="server" ID="edCntMaxAmount" DataField="CntMaxAmount" AllowNull="True" />
					<px:PXDropDown runat="server" ID="edCntApplicableEarnings" DataField="CntApplicableEarnings" CommitChanges="True" />
					<px:PXSelector runat="server" ID="edCntReportType" DataField="CntReportType" />
					<px:PXSelector runat="server" ID="edCntReportTypeCAN" DataField="CntReportTypeCAN" />
					<px:PXSelector runat="server" ID="edCertifiedReportType" DataField="CertifiedReportType" />

					<px:PXLayoutRule runat="server" StartColumn="True" ControlSize="XM" LabelsWidth="S" />
					<px:PXCheckBox runat="server" ID="edNoFinancialTransaction" DataField="NoFinancialTransaction" CommitChanges="true" />
					<px:PXCheckBox runat="server" ID="edContributesToGrossCalculation" DataField="ContributesToGrossCalculation" CommitChanges="true" />
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="WCC Code" VisibleExp="DataControls[&quot;edAssociatedSource&quot;].Value==WCC" BindingContext="form" >
				<Template>
					<px:PXFormView ID="wcForm" runat="server" DataMember="CurrentDocument">
						<Template>
							<px:PXLayoutRule runat="server" StartColumn="True" ControlSize="XM" LabelsWidth="S" />
								<px:PXSelector runat="server" ID="edState" DataField="State" />
						</Template>
					</px:PXFormView>
					<px:PXSplitContainer runat="server" Orientation="Vertical" PositionInPercent="true" SplitterPosition="50">
						<Template1>
							<px:PXGrid ID="gridWCRates" runat="server" DataSourceID="ds" Width="100%" SkinID="Details" AllowPaging="false"
								Caption="Rates" CaptionVisible="true" SyncPosition="true">
								<Levels>
									<px:PXGridLevel DataMember="WorkCompensationRates">
										<RowTemplate>
											<px:PXNumberEdit ID="edRate" runat="server" DataField="Rate" />
										</RowTemplate>
										<Columns>
											<px:PXGridColumn DataField="IsActive" Width="60px" TextAlign="Center" Type="CheckBox" />
											<px:PXGridColumn DataField="WorkCodeID" Width="200px" CommitChanges="true" />
											<px:PXGridColumn DataField="PMWorkCode__Description" Width="300px" />
											<px:PXGridColumn DataField="BranchID" CommitChanges="true" />
											<px:PXGridColumn DataField="DeductionRate" Width="120px" />
											<px:PXGridColumn DataField="Rate" Width="120px" />
											<px:PXGridColumn DataField="EffectiveDate" Width="250px" CommitChanges="true" />
										</Columns>
									</px:PXGridLevel>
								</Levels>
								<AutoSize Container="Parent" Enabled="True" MinHeight="200" />
								<ActionBar ActionsText="False">
									<Actions>
										<Delete Enabled="false" />
									</Actions>
								</ActionBar>
								<AutoCallBack Enabled="true" Command="Refresh" Target="gridWCMaxInsurableWages" />
							</px:PXGrid>
						</Template1>
						<Template2>
							<px:PXGrid ID="gridWCMaxInsurableWages" runat="server" DataSourceID="ds" Width="100%" SkinID="Details" AllowPaging="false" Caption="Maximum Insurable Wages" CaptionVisible="true">
								<Levels>
									<px:PXGridLevel DataMember="MaximumInsurableWages">
										<RowTemplate>
											<px:PXNumberEdit ID="edMaximumInsurableWage" runat="server" DataField="MaximumInsurableWage" />
										</RowTemplate>
										<Columns>
											<px:PXGridColumn DataField="MaximumInsurableWage" />
											<px:PXGridColumn DataField="EffectiveDate" CommitChanges="true" Width="250px" />
										</Columns>
									</px:PXGridLevel>
								</Levels>
								<AutoSize Container="Parent" Enabled="True" MinHeight="200" />
							</px:PXGrid>
						</Template2>
						<AutoSize Container="Parent" Enabled="True" MinHeight="200" />
					</px:PXSplitContainer>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="Applicable Wage" VisibleExp="DataControls[&quot;chkShowApplicableWageTab&quot;].Value = 1" BindingContext="form">
				<Template>
					<px:PXSplitContainer runat="server" ID="applicableWageLeft" Orientation="Vertical" PositionInPercent="true" SplitterPosition="50">
						<Template1>
							<px:PXSplitContainer runat="server" ID="applicableWageLeftTop" Orientation="Horizontal" PositionInPercent="true" SplitterPosition="33">
								<Template1>
									<px:PXFormView runat="server" ID="frmEarningsIncreasingWage" DataMember="CurrentDocument" Width="100%"
										CaptionVisible="true" Caption="Earnings Increasing Applicable Wage">
										<Template>
											<px:PXLayoutRule runat="server" LabelsWidth="M" ControlSize="M" />
											<px:PXDropDown runat="server" ID="edEarningsIncreasingWageIncludeType" DataField="EarningsIncreasingWageIncludeType" CommitChanges="true" />
											<px:PXGrid runat="server" ID="grdEarningsIncreasingWage" DataSourceID="ds" Width="100%" SkinID="Details" AdjustPageSize="Auto">
												<Levels>
													<px:PXGridLevel DataMember="EarningsIncreasingWage">
														<Columns>
															<px:PXGridColumn DataField="ApplicableTypeCD" />
															<px:PXGridColumn DataField="EPEarningType__Description" />
															<px:PXGridColumn DataField="EPEarningType__EarningTypeCategory" />
														</Columns>
													</px:PXGridLevel>
												</Levels>
												<AutoSize Container="Window" Enabled="True" />
											</px:PXGrid>
										</Template>
									</px:PXFormView>
								</Template1>
								<Template2>
									<px:PXSplitContainer runat="server" ID="applicableWageLeftBottom" Orientation="Horizontal" PositionInPercent="true" SplitterPosition="50">
										<Template1>
											<px:PXFormView runat="server" ID="frmBenefitsIncreasingWage" DataMember="CurrentDocument" Width="100%"
												CaptionVisible="true" Caption="Benefits Increasing Applicable Wage">
												<Template>
													<px:PXLayoutRule runat="server" LabelsWidth="M" ControlSize="M" />
													<px:PXDropDown runat="server" ID="edBenefitsIncreasingWageIncludeType" DataField="BenefitsIncreasingWageIncludeType" CommitChanges="true" />
													<px:PXGrid runat="server" ID="grdBenefitsIncreasingWage" DataSourceID="ds" Width="100%" SkinID="Details" AdjustPageSize="Auto">
														<Levels>
															<px:PXGridLevel DataMember="BenefitsIncreasingWage">
																<Columns>
																	<px:PXGridColumn DataField="ApplicableBenefitCodeID" />
																	<px:PXGridColumn DataField="ApplicableBenefitCodeID_Description" />
																</Columns>
															</px:PXGridLevel>
														</Levels>
														<AutoSize Container="Window" Enabled="True" />
													</px:PXGrid>
												</Template>
											</px:PXFormView>
										</Template1>
										<Template2>
											<px:PXFormView runat="server" ID="frmTaxesIncreasingWage" DataMember="CurrentDocument" Width="100%"
												CaptionVisible="true" Caption="Employer Taxes Increasing Applicable Wage">
												<Template>
													<px:PXLayoutRule runat="server" LabelsWidth="M" ControlSize="M" />
													<px:PXDropDown runat="server" ID="edTaxesIncreasingWageIncludeType" DataField="TaxesIncreasingWageIncludeType" CommitChanges="true" />
													<px:PXGrid runat="server" ID="grdTaxesIncreasingWage" DataSourceID="ds" Width="100%" SkinID="Details" AdjustPageSize="Auto">
														<Levels>
															<px:PXGridLevel DataMember="TaxesIncreasingWage">
																<Columns>
																	<px:PXGridColumn DataField="ApplicableTaxID" />
																	<px:PXGridColumn DataField="PRTaxCode__Description" />
																	<px:PXGridColumn DataField="PRTaxCode__TaxCategory" />
																</Columns>
															</px:PXGridLevel>
														</Levels>
														<AutoSize Container="Window" Enabled="True" />
													</px:PXGrid>
												</Template>
											</px:PXFormView>
										</Template2>
										<AutoSize Container="Window" Enabled="True" />
									</px:PXSplitContainer>
								</Template2>
								<AutoSize Container="Window" Enabled="True" />
							</px:PXSplitContainer>
						</Template1>
						<Template2>
							<px:PXSplitContainer runat="server" ID="applicableWageRight" Orientation="Horizontal" PositionInPercent="true" SplitterPosition="33">
								<Template1>
									<px:PXFormView runat="server" ID="frmDeductionsDecreasingWage" DataMember="CurrentDocument" Width="100%"
										CaptionVisible="true" Caption="Deductions Decreasing Applicable Wage">
										<Template>
											<px:PXLayoutRule runat="server" LabelsWidth="M" ControlSize="M" />
											<px:PXDropDown runat="server" ID="edDeductionsDecreasingWageIncludeType" DataField="DeductionsDecreasingWageIncludeType" CommitChanges="true" />
											<px:PXGrid runat="server" ID="grdDeductionsDecreasingWage" DataSourceID="ds" Width="100%" SkinID="Details" AdjustPageSize="Auto">
												<Levels>
													<px:PXGridLevel DataMember="DeductionsDecreasingWage">
														<Columns>
															<px:PXGridColumn DataField="ApplicableDeductionCodeID" />
															<px:PXGridColumn DataField="ApplicableDeductionCodeID_Description" />
														</Columns>
													</px:PXGridLevel>
												</Levels>
												<AutoSize Container="Window" Enabled="True" />
											</px:PXGrid>
										</Template>
									</px:PXFormView>
								</Template1>
								<Template2>
									<px:PXFormView runat="server" ID="frmTaxesDecreasingWage" DataMember="CurrentDocument" Width="100%"
										CaptionVisible="true" Caption="Employee Taxes Decreasing Applicable Wage">
										<Template>
											<px:PXLayoutRule runat="server" LabelsWidth="M" ControlSize="M" />
											<px:PXDropDown runat="server" ID="edTaxesDecreasingWageIncludeType" DataField="TaxesDecreasingWageIncludeType" CommitChanges="true" />
											<px:PXGrid runat="server" ID="grdTaxesDecreasingWage" DataSourceID="ds" Width="100%" SkinID="Details" AdjustPageSize="Auto">
												<Levels>
													<px:PXGridLevel DataMember="TaxesDecreasingWage">
														<Columns>
															<px:PXGridColumn DataField="ApplicableTaxID" />
															<px:PXGridColumn DataField="PRTaxCode__Description" />
															<px:PXGridColumn DataField="PRTaxCode__TaxCategory" />
														</Columns>
													</px:PXGridLevel>
												</Levels>
												<AutoSize Container="Window" Enabled="True" />
											</px:PXGrid>
										</Template>
									</px:PXFormView>
								</Template2>
								<AutoSize Container="Window" Enabled="True" />
							</px:PXSplitContainer>
						</Template2>
						<AutoSize Container="Window" Enabled="True" MinHeight="1000" />
					</px:PXSplitContainer>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="GL Accounts" RepaintOnDemand="false">
				<Template>
					<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
					<px:PXSelector ID="edDedLiabilityAcctID" runat="server" DataField="DedLiabilityAcctID" />
					<px:PXSegmentMask ID="edDedLiabilitySubID" runat="server" DataField="DedLiabilitySubID" />
					<px:PXSelector ID="edBenefitExpenseAcctID" runat="server" DataField="BenefitExpenseAcctID" />
					<px:PXSegmentMask ID="edBenefitExpenseSubID" runat="server" DataField="BenefitExpenseSubID" />
					<px:PXSelector ID="edBenefitLiabilityAcctID" runat="server" DataField="BenefitLiabilityAcctID" />
					<px:PXSegmentMask ID="edBenefitLiabilitySubID" runat="server" DataField="BenefitLiabilitySubID" />
				</Template>
			</px:PXTabItem>
		</Items>
		<AutoSize Container="Window" Enabled="True" MinHeight="150" />
	</px:PXTab>
</asp:Content>
