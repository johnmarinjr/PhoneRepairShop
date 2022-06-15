<px:PXSmartPanel
	ID="pnlSelectContactForLink" runat="server" Caption="Select the Contact for the Lead" CaptionVisible="True" AutoRepaint="True"
	Key="Link_SelectEntityForLink" LoadOnDemand="True" ShowAfterLoad="true" CloseButtonDialogResult="Abort" CancelButtonID="btnSelectContactForLinkBack"
	Width="520px" Height="314px">

	<px:PXFormView
		ID="frmSelectContactForLink" runat="server" DataSourceID="ds" Width="100%" Height="38px" SkinID="Transparent"
		DataMember="Link_SelectEntityForLink">

		<Template>
			<px:PXLabel runat="server" Text="Select the contact of the account to be associated with the lead."
						Style="position: absolute; top: 14px; left:19px; width: 95%; font-style: italic"/>
		</Template>
		<CallbackCommands>
			<Save RepaintControls="None" />
		</CallbackCommands>

	</px:PXFormView>

	<px:PXGrid
		ID="grdSelectContactForLink" runat="server" DataSourceID="ds" SkinID="Inquire" MatrixMode="True" Width="100%" Height="188px"
		AutoAdjustColumns="True" AllowFilter="False" AllowPaging="False" AllowSearch="False" FilesIndicator="False" NoteIndicator="False"
		SyncPosition="True">

		<Levels>
			<px:PXGridLevel DataMember="Link_SelectEntityForLink">
				<Columns>
					<px:PXGridColumn DataField="DisplayName" Width="125px" />
					<px:PXGridColumn DataField="Salutation" Width="95px" />
					<px:PXGridColumn DataField="EMail" Width="125px" />
					<px:PXGridColumn DataField="Phone1" Width="80px" />
					<px:PXGridColumn DataField="IsPrimary" Type="CheckBox" Width="70px" />
				</Columns>
				<Layout FooterVisible="False" HeaderVisible="True" ColumnsMenu="False"/>
			</px:PXGridLevel>
		</Levels>

		<CallbackCommands>
			<Save RepaintControls="None" />
			<FetchRow RepaintControls="None" />
		</CallbackCommands>

		<ActionBar>
			<Actions>
				<AdjustColumns Enabled="False"/>
				<Refresh Enabled="False"/>
				<ExportExcel Enabled="False"/>
			</Actions>
		</ActionBar>

		<Mode AllowAddNew="False" AllowColMoving="False" AllowDelete="False" />

	</px:PXGrid>

	<px:PXPanel ID="pnlBtnSelectContactForLink" runat="server" SkinID="Transparent">
		<px:PXButton ID="btnSelectContactForLinkBack" runat="server" CommandSourceID="ds" CallbackUpdatable="true"
		             style="float: left; margin: 2px" Text="Back" DialogResult="No" Visible="false"/>
		
		<px:PXButton ID="btnSelectContactForLinkCancel" runat="server" style="float: right; margin: 2px" CommandSourceID="ds" Text="Cancel" DialogResult="Abort"/>
		<px:PXButton ID="btnSelectContactForLinkNext" runat="server" style="float: right; margin: 2px" CommandSourceID="ds" Text="Next" DialogResult="Yes"/>
	</px:PXPanel>
</px:PXSmartPanel>

<px:PXSmartPanel
	ID="pnlLinkContact" runat="server" Caption="Associate the Contact with the Lead" CaptionVisible="True" Key="Link_Filter" LoadOnDemand="True"
	ShowAfterLoad="True" AutoRepaint="True"	AcceptButtonID="btnProcessLinkContact" CancelButtonID="btnCancelLinkContact" CloseButtonDialogResult="Abort"
	Width="520px" >

	<px:PXFormView
		ID="frmLinkContact" runat="server" Width="100%" Height="45px" CaptionVisible="False" DataMember="Link_Filter" DataSourceID="ds"
		SkinID="Transparent">

		<Template>
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="L" ControlSize="XM" ColumnWidth="100%" />
			<px:PXCheckBox ID="chkReplaceValues" runat="server" DataField="ProcessLink" AlignLeft="True" CommitChanges="True" />
		</Template>
		<CallbackCommands>
			<Save RepaintControls="None" RepaintControlsIDs="grdLinkContact" />
		</CallbackCommands>

	</px:PXFormView>

	<px:PXFormView ID="frmGrdLinkContact" runat="server" Width="100%" Height="237px" CaptionVisible="False" DataSourceID="ds" SkinID="Transparent" DataMember="Link_Filter">
		<Template>

			<px:PXTextEdit	ID="edCaption" runat="server" DataField="Caption" SuppressLabel="True"
							IsClientControl="false" DisableSpellcheck="True" SkinID="Label"
							Style="padding: 0px 0px 10px 15px; width: 95%; font-style: italic"
							CssClass="labelH textEditLabel ReadOnly"/>

			<px:PXGrid
				ID="grdLinkContact" runat="server" DataSourceID="ds" SkinID="Inquire" MatrixMode="True" Width="100%" Height="160px"
				AutoAdjustColumns="True" AllowFilter="False" AllowPaging="False" AllowSearch="False" FilesIndicator="False" NoteIndicator="False">

				<Levels>
					<px:PXGridLevel DataMember="Link_VisibleComparisonRows">
						<Columns>
							<px:PXGridColumn DataField="FieldDisplayName" Width="150px" />
							<px:PXGridColumn DataField="LeftValueSelected" TextAlign="Center" Type="CheckBox" Width="30px" AllowCheckAll="True" AutoCallBack="True" />
							<px:PXGridColumn DataField="LeftValue" Width="150px" />
							<px:PXGridColumn DataField="RightValueSelected" TextAlign="Center" Type="CheckBox" Width="30px" AllowCheckAll="True" AutoCallBack="True" />
							<px:PXGridColumn DataField="RightValue" Width="150px" />
						</Columns>
						<Layout FooterVisible="False" HeaderVisible="True" ColumnsMenu="False"/>
					</px:PXGridLevel>
				</Levels>

				<CallbackCommands>
					<Save RepaintControls="None" />
					<FetchRow RepaintControls="None" />
				</CallbackCommands>

				<ActionBar>
					<Actions>
						<AdjustColumns Enabled="False"/>
						<Refresh Enabled="False"/>
						<ExportExcel Enabled="False"/>
					</Actions>
				</ActionBar>

				<Mode AllowAddNew="False" AllowColMoving="False" AllowDelete="False" />

			</px:PXGrid>
		</Template>
	</px:PXFormView>

	<px:PXPanel ID="pnlBtnLinkContact" runat="server" CommandSourceID="ds" SkinID="Transparent">
		<px:PXButton ID="btnBackLinkContact" runat="server" CommandSourceID="ds" CallbackUpdatable="true"
		             style="float: left; margin: 2px" Text="Back" DialogResult="No" Visible="false"/>

		<px:PXButton ID="btnCancelLinkContact" runat="server" style="float: right; margin: 2px" CommandSourceID="ds" Text="Cancel" DialogResult="Abort"/>
		<px:PXButton ID="btnProcessLinkContact" runat="server" style="float: right; margin: 2px" CommandSourceID="ds" Text="Associate" DialogResult="Yes"/>
	</px:PXPanel>
</px:PXSmartPanel>