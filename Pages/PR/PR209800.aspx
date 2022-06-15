<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="PR209800.aspx.cs"
	Inherits="Page_PR209800" Title="Workers' Compensation Codes" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
		<script type="text/javascript">
			function refreshChildGrids(e, args) {
				if (args.oldRow != null && args.oldRow.element.id != args.row.element.id) {
					px_alls.rateGrid.refresh();
					px_alls.costCodesGrid.refresh();
					px_alls.projectTasksGrid.refresh();
					px_alls.laborItemsGrid.refresh();
				}
			}

			function initChildGrids() {
				px_alls.rateGrid.refresh();
				px_alls.costCodesGrid.refresh();
				px_alls.projectTasksGrid.refresh();
				px_alls.laborItemsGrid.refresh();
			}
		</script>

	<px:PXDataSource ID="ds" Width="100%" runat="server" TypeName="PX.Objects.PR.PRWorkCodeMaint" PrimaryView="Filter" Visible="True">
		<CallbackCommands>
			<px:PXDSCallbackCommand Name="Save" CommitChanges="True" />
			<px:PXDSCallbackCommand Name="ViewMaximumInsurableWages" Visible="false" />
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phG" runat="Server">
	<px:PXSplitContainer ID="splitContainerWorkCodes" runat="server" PositionInPercent="true" SplitterPosition="40" Orientation="Vertical" Height="100%">
		<Template1>
			<px:PXFormView runat="server" ID="frmCountryID" DataMember="Filter" Width="100%">
				<Template>
					<px:PXLayoutRule runat="server" StartRow="true" LabelsWidth="XS" ControlSize="M" />
					<px:PXSelector runat="server" ID="edCountryID" DataField="CountryID" CommitChanges="true" />
				</Template>
			</px:PXFormView>
			<px:PXGrid ID="grid1" runat="server" Height="400px" Width="100%" Style="z-index: 100" AllowPaging="True" AllowSearch="true"
				AdjustPageSize="Auto" DataSourceID="ds" SkinID="Details" SyncPosition="true" FastFilterFields="Description" CaptionVisible="true" Caption="WCC Code" KeepPosition="true">
				<Levels>
					<px:PXGridLevel DataMember="WorkCompensationCodes">
						<Columns>
							<px:PXGridColumn DataField="IsActive" Type="CheckBox" />
							<px:PXGridColumn DataField="WorkCodeID" />
							<px:PXGridColumn DataField="Description" />
						</Columns>
					</px:PXGridLevel>
				</Levels>
				<ActionBar>
					<CustomItems>
						<px:PXToolBarButton>
							<AutoCallBack Command="ViewMaximumInsurableWages" Target="ds" />
						</px:PXToolBarButton>
					</CustomItems>
				</ActionBar>
				<AutoSize Container="Parent" Enabled="True" MinHeight="200" />
				<ClientEvents AfterRowChange="refreshChildGrids" AfterRefresh="initChildGrids" />
			</px:PXGrid>
		</Template1>
		<Template2>
			<px:PXSplitContainer ID="splitContainerRightPane" runat="server" PositionInPercent="true" SplitterPosition="50" Orientation="Horizontal" Height="100%" SkinID="Horizontal">
				<Template1>
					<px:PXGrid ID="rateGrid" runat="server" Height="400px" Width="100%" Style="z-index: 100" AllowPaging="False" AllowSearch="true"
						DataSourceID="ds" SkinID="Details" SyncPosition="true" CaptionVisible="true" Caption="Rate">
						<Levels>
							<px:PXGridLevel DataMember="WorkCompensationRates">
								<RowTemplate>
									<px:PXNumberEdit ID="edRate" runat="server" DataField="Rate" />
								</RowTemplate>
								<Columns>
									<px:PXGridColumn DataField="IsActive" Type="CheckBox" TextAlign="Center" Width="60px" />
									<px:PXGridColumn DataField="DeductCodeID" Width="120px" CommitChanges="true" />
									<px:PXGridColumn DataField="DeductionCalcType" Width="150px" />
									<px:PXGridColumn DataField="PRDeductCode__CntCalcType" Width="150px" />
									<px:PXGridColumn DataField="PRDeductCode__State" Width="80px" />
									<px:PXGridColumn DataField="BranchID" CommitChanges="true" />
									<px:PXGridColumn DataField="DeductionRate" Width="80px" />
									<px:PXGridColumn DataField="Rate" Width="80px" />
									<px:PXGridColumn DataField="EffectiveDate" CommitChanges="true" Width="200px" />
								</Columns>
							</px:PXGridLevel>
						</Levels>
						<AutoSize Container="Parent" Enabled="True" MinHeight="200" />
						<ActionBar>
							<Actions>
								<Delete Enabled="false" />
							</Actions>
						</ActionBar>
					</px:PXGrid>
				</Template1>
				<Template2>
					<px:PXLabel runat="server" Text="Sources" cssClass="GridCaption" Width="100%" />
					<px:PXTab ID="tabBottomRight" runat="server" Width="100%">
						<Items>
							<px:PXTabItem Text="Project Tasks">
								<Template>
									<px:PXGrid ID="projectTasksGrid" runat="server" Width="100%" AllowPaging="False" DataSourceID="ds" SkinID="Details">
										<Levels>
											<px:PXGridLevel DataMember="ProjectTaskSources">
												<Columns>
													<px:PXGridColumn DataField="ProjectID" CommitChanges="true" />
													<px:PXGridColumn DataField="ProjectTaskID" />
												</Columns>
											</px:PXGridLevel>
										</Levels>
										<AutoSize Container="Parent" Enabled="True" MinHeight="200" />
									</px:PXGrid>
								</Template>
							</px:PXTabItem>
							<px:PXTabItem Text="Labor Items">
								<Template>
									<px:PXGrid ID="laborItemsGrid" runat="server" Width="100%" AllowPaging="False" DataSourceID="ds" SkinID="Details">
										<Levels>
											<px:PXGridLevel DataMember="LaborItemSources">
												<Columns>
													<px:PXGridColumn DataField="LaborItemID" />
												</Columns>
											</px:PXGridLevel>
										</Levels>
										<AutoSize Container="Parent" Enabled="True" MinHeight="200" />
									</px:PXGrid>
								</Template>
							</px:PXTabItem>
							<px:PXTabItem Text="Cost Codes">
								<Template>
									<px:PXGrid ID="costCodesGrid" runat="server" Width="100%" AllowPaging="False" DataSourceID="ds" SkinID="Details">
										<Levels>
											<px:PXGridLevel DataMember="CostCodeRanges">
												<Columns>
													<px:PXGridColumn DataField="CostCodeFrom" Width="120px" />
													<px:PXGridColumn DataField="CostCodeTo" Width="120px" />
												</Columns>
											</px:PXGridLevel>
										</Levels>
										<AutoSize Container="Parent" Enabled="True" MinHeight="200" />
									</px:PXGrid>
								</Template>
							</px:PXTabItem>
						</Items>
						<AutoSize Container="Parent" Enabled="True" MinHeight="200" />
					</px:PXTab>
				</Template2>
				<AutoSize Container="Parent" Enabled="True" MinHeight="200" />
			</px:PXSplitContainer>
		</Template2>
		<AutoSize Enabled="true" Container="Window" />
	</px:PXSplitContainer>
		<px:PXSmartPanel runat="server" ID="pnlMaximumInsurabelWages" AutoRepaint="true" Caption="Maximum Insurable Wages" CaptionVisible="true" Key="MaximumInsurableWages">
		<px:PXGrid ID="grdMaximumInsurabelWages" runat="server" DataSourceID="ds" SkinID="Details">
			<Levels>
				<px:PXGridLevel DataMember="MaximumInsurableWages">
					<Mode InitNewRow="true" />
					<RowTemplate>
						<px:PXSelector runat="server" ID="edDeductCodeID" DataField="DeductCodeID" AutoRefresh="true" />
						<px:PXNumberEdit runat="server" ID="edMaximumInsurableWage" DataField="MaximumInsurableWage" />
					</RowTemplate>
					<Columns>
						<px:PXGridColumn DataField="DeductCodeID" CommitChanges="true" />
						<px:PXGridColumn DataField="State__StateID" />
						<px:PXGridColumn DataField="MaximumInsurableWage" />
						<px:PXGridColumn DataField="EffectiveDate" CommitChanges="true" Width="200px" />
					</Columns>
				</px:PXGridLevel>
			</Levels>
			<AutoSize Enabled="true" MinHeight="400" />
		</px:PXGrid>
    </px:PXSmartPanel>
</asp:Content>
