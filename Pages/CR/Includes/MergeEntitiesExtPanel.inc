<px:PXSmartPanel ID="pnlMergeEntities" runat="server" Key="Merge_Filter" CloseButtonDialogResult="Abort"
                 LoadOnDemand="True" ShowAfterLoad="True" AutoRepaint="True" AcceptButtonID="btnProcessMergeEntities" CancelButtonID="btnCancelMergeEntities"
                 Caption="Merge Conflicts" CaptionVisible="True" Width="520px">

	<px:PXFormView ID="frmMergeEntities" runat="server" DataSourceID="ds" DataMember="Merge_Filter"
	               Width="100%" Height="55px" CaptionVisible="False" SkinID="Transparent">

		<Template>
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="L" ControlSize="XM" ColumnWidth="100%" />
			<px:PXDropDown ID="drpTargetRecord" runat="server" DataField="TargetRecord" CommitChanges="True"  LabelWidth="50%" />
		</Template>
		<CallbackCommands>
			<Save RepaintControls="None" RepaintControlsIDs="grdMergeEntities" />
		</CallbackCommands>

	</px:PXFormView>

	<px:PXFormView ID="frmGrdMergeEntities" runat="server" DataSourceID="ds" DataMember="Merge_Filter"
	               SkinID="Transparent" Width="100%" Height="237px" CaptionVisible="False" >
		<Template>

			<px:PXTextEdit ID="edCaption" runat="server" DataField="Caption" SuppressLabel="True"
			               IsClientControl="false" DisableSpellcheck="True" SkinID="Label"
			               Style="padding: 0px 0px 10px 15px; width: 95%; font-style: italic"
			               CssClass="labelH textEditLabel ReadOnly"/>

			<px:PXGrid ID="grdMergeEntities" runat="server" DataSourceID="ds"
			           SkinID="Inquire" MatrixMode="True" Width="100%" Height="160px" AutoAdjustColumns="True" AllowFilter="False"
			           AllowPaging="False" AllowSearch="False" FilesIndicator="False" NoteIndicator="False"  SyncPosition="True">

				<Levels>
					<px:PXGridLevel DataMember="Merge_VisibleComparisonRows">
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

	<px:PXPanel ID="pnlBtnMergeEntities" runat="server" CommandSourceID="ds" SkinID="Transparent">

		<px:PXButton ID="btnCancelMergeEntities" runat="server" CommandSourceID="ds"  DialogResult="Abort"
		             style="float: right; margin: 2px"  Text="Cancel"/>

		<px:PXButton ID="btnProcessMergeEntities" runat="server" CommandSourceID="ds"  DialogResult="Yes"
		             style="float: right; margin: 2px"  Text="Resolve"/>

	</px:PXPanel>
</px:PXSmartPanel>