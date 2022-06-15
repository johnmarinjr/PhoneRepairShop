<px:PXTabItem Text="Relations" LoadOnDemand="True">
	<Template>
		<px:PXGrid ID="grdRelations" runat="server" Height="400px" Width="100%" AllowPaging="True" SyncPosition="True" MatrixMode="True"
			ActionsPosition="Top" AllowSearch="true" DataSourceID="ds" SkinID="Details" OnRowDataBound="RelationsGrid_RowDataBound">
			<Levels>
				<px:PXGridLevel DataMember="Relations">
				<Columns>
					<px:PXGridColumn DataField="Role" CommitChanges="True" />
					<px:PXGridColumn DataField="IsPrimary" Type="CheckBox" TextAlign="Center" CommitChanges="True" />
					<px:PXGridColumn DataField="TargetType" CommitChanges="True" />
					<px:PXGridColumn DataField="TargetNoteID" DisplayMode="Text" LinkCommand="Relations_TargetDetails" CommitChanges="True" />
					<px:PXGridColumn DataField="EntityID" AutoCallBack="true" LinkCommand="Relations_EntityDetails" CommitChanges="True" />
					<px:PXGridColumn DataField="Name" />
					<px:PXGridColumn DataField="ContactID" AutoCallBack="true" TextAlign="Left" TextField="ContactName" DisplayMode="Text" LinkCommand="Relations_ContactDetails" />
					<px:PXGridColumn DataField="Email" />
					<px:PXGridColumn DataField="AddToCC" Type="CheckBox" TextAlign="Center" />
				</Columns>
				<RowTemplate>
					<px:PXSelector ID="edTargetNoteID" runat="server" DataField="TargetNoteID" FilterByAllFields="True" AutoRefresh="True" />
					<px:PXSelector ID="edRelEntityID" runat="server" DataField="EntityID" FilterByAllFields="True" AutoRefresh="True" />
					<px:PXSelector ID="edRelContactID" runat="server" DataField="ContactID" FilterByAllFields="True" AutoRefresh="True" />
				</RowTemplate>
				</px:PXGridLevel>
			</Levels>
			<Mode InitNewRow="True" ></Mode>
			<AutoSize Enabled="True" MinHeight="100" MinWidth="100" ></AutoSize>
		</px:PXGrid>
	</Template>
</px:PXTabItem>