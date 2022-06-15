<px:PXTabItem Text="Related Items">
	<Template>
		<px:PXGrid ID="relatedItemsGrid" runat="server" DataSourceID="ds" Height="150px" Width="100%" ActionsPosition="Top" SkinID="Details" 
			SyncPosition="True" FilesIndicator="True" NoteIndicator="True" >
			<Levels>
				<px:PXGridLevel DataMember="RelatedItems" DataKeyNames="InventoryID,Relation" ImportDataMember="RelatedItems">
					<Columns>
						<px:PXGridColumn DataField="Relation" CommitChanges="true" Type="DropDownList" />
						<px:PXGridColumn DataField="Rank" CommitChanges="true"/>
						<px:PXGridColumn DataField="Tag" CommitChanges="true" Type="DropDownList" />
						<px:PXGridColumn DataField="RelatedInventoryID" CommitChanges="true" LinkCommand="ViewRelatedItem" />
						<px:PXGridColumn DataField="Desc" />
						<px:PXGridColumn DataField="UOM" DisplayFormat="&gt;aaaaaa" CommitChanges="true" />
						<px:PXGridColumn DataField="Qty" />
						<px:PXGridColumn DataField="EffectiveDate" CommitChanges="true" />
						<px:PXGridColumn DataField="ExpirationDate" CommitChanges="true" />
						<px:PXGridColumn DataField="Interchangeable" TextAlign="Center" Type="CheckBox" />
						<px:PXGridColumn DataField="Required" TextAlign="Center" Type="CheckBox" CommitChanges="true" />
						<px:PXGridColumn DataField="IsActive" TextAlign="Center" Type="CheckBox" CommitChanges="true" />
					</Columns>
					<RowTemplate>
						<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
						<px:PXDropDown ID="edRelation" runat="server" DataField="Relation" CommitChanges="true" />
						<px:PXNumberEdit ID="edRank" runat="server" DataField="Rank" CommitChanges="true" />
						<px:PXDropDown ID="edTag" runat="server" DataField="Tag" CommitChanges="true" />
						<px:PXSelector ID="edRelatedInventoryID" runat="server" DataField="RelatedInventoryID" AllowEdit="True" CommitChanges="true" />
						<px:PXTextEdit ID="edRelatedInventoryDesc" runat="server" DataField="Desc" Enabled="False" />
						<px:PXSelector ID="edRelatedInventoryUOM" runat="server" Size="s" DataField="UOM" AutoRefresh="True" CommitChanges="true" />
						<px:PXNumberEdit ID="edRelatedItemQty" runat="server" DataField="Qty" />
						<px:PXDateTimeEdit ID="edEffectiveDate" runat="server" DataField="EffectiveDate" />
						<px:PXDateTimeEdit ID="edExpirationDate" runat="server" DataField="ExpirationDate" />
						<px:PXCheckBox ID="edInterchangeable" runat="server" DataField="Interchangeable" />
						<px:PXCheckBox ID="edRelatedItemRequired" runat="server" DataField="Required" CommitChanges="true" />
						<px:PXCheckBox ID="edRelatedItemIsActive" runat="server" DataField="IsActive" CommitChanges="true" />
					</RowTemplate>
					<Layout FormViewHeight="" />
				</px:PXGridLevel>
			</Levels>
			<Mode InitNewRow="true" AllowUpload="True" />
			<AutoSize Enabled="True" MinHeight="150" />
		</px:PXGrid>
	</Template>
</px:PXTabItem>