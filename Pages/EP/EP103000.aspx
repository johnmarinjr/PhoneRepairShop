<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="EP103000.aspx.cs" Inherits="Page_EP103000" Title="Shift Codes" %>

<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.EP.EPShiftCodeSetup" PrimaryView="Codes">
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">
	<script type="text/javascript">		
		function refreshGridDetails(sender, args) {
			if (args.oldRow != null && args.oldRow.element.id != args.row.element.id) {
				px_alls.gridDetails.refresh();
			}
		}		
	</script>

	<px:PXSplitContainer ID="splitShiftCodes" runat="server" PositionInPercent="false" SplitterPosition="550" Orientation="Vertical" Height="100%">
		<Template1>
			<px:PXGrid ID="gridSummary" runat="server" Width="100%" AllowPaging="True" AllowSearch="true" AdjustPageSize="Auto"
				DataSourceID="ds" SkinID="Details" SyncPosition="true" KeepPosition="true">
				<Levels>
					<px:PXGridLevel DataMember="Codes">
						<Columns>
							<px:PXGridColumn DataField="IsActive" Type="CheckBox" TextAlign="Center" />
							<px:PXGridColumn DataField="ShiftCD" Width="150px" CommitChanges="true" />
							<px:PXGridColumn DataField="Description" Width="250px" />
						</Columns>
					</px:PXGridLevel>
				</Levels>
				<AutoSize Container="Window" Enabled="True" MinHeight="200" />
				<ClientEvents AfterRowChange="refreshGridDetails" />
			</px:PXGrid>
		</Template1>
		<Template2>
			<px:PXGrid ID="gridDetails" runat="server" Width="100%" Style="z-index: 100"
				AllowPaging="True" AllowSearch="true" AdjustPageSize="Auto" DataSourceID="ds" SkinID="Details">
				<Levels>
					<px:PXGridLevel DataMember="Rates">
						<Columns>
							<px:PXGridColumn DataField="EffectiveDate" CommitChanges="true" />
							<px:PXGridColumn DataField="Type" CommitChanges="true" />
							<px:PXGridColumn DataField="Percent" CommitChanges="true" />
							<px:PXGridColumn DataField="WageAmount" CommitChanges="true" />
							<px:PXGridColumn DataField="CostingAmount" CommitChanges="true" />
							<px:PXGridColumn DataField="BurdenAmount" />
						</Columns>
					</px:PXGridLevel>
				</Levels>
				<AutoSize Container="Window" Enabled="True" MinHeight="200" />
			</px:PXGrid>
		</Template2>
		<AutoSize Enabled="true" Container="Window" MinHeight="400" />
	</px:PXSplitContainer>
</asp:Content>
