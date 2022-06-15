<px:PXSmartPanel ID="PanelCreateQuote" runat="server" Style="z-index: 108; position: absolute; left: 27px; top: 99px;" Caption="Create Quote"
					 CaptionVisible="True" LoadOnDemand="true" ShowAfterLoad="true" Key="QuoteInfo" AutoCallBack-Enabled="true" AutoCallBack-Target="formCreateQuote" AutoCallBack-Command="Refresh"
					 CallBackMode-CommitChanges="True" CallBackMode-PostData="Page" AcceptButtonID="CreateQuoteBtnConvert" CancelButtonID="CreateQuoteBtnCancel">
	<px:PXTab ID="tabPanelCreateQuote" runat="server" Width="100%" DataSourceID="ds" DataMember="QuoteInfo">
		<Items>
			<px:PXTabItem Text="Main" RepaintOnDemand="True">
				<Template>
					<px:PXFormView ID="formCreateQuote" runat="server" DataSourceID="ds" Width="100%" Caption="Services Settings" CaptionVisible="False" SkinID="Transparent"
					   DataMember="QuoteInfo">
						<Template>
							<px:PXLayoutRule ID="PXLayoutRule6" runat="server" LabelsWidth="S" ControlSize="XM" />
							<px:PXDropDown ID="edQuoteType" runat="server" DataField="QuoteType" CommitChanges="true"/>
							<px:PXCheckBox ID="edAddProductsFromOpportunity" runat="server" DataField="AddProductsFromOpportunity" CommitChanges="true"/>
							<px:PXCheckBox ID="edMakeNewQuotePrimary" runat="server" DataField="MakeNewQuotePrimary" CommitChanges="true"/>
							<px:PXCheckBox ID="edRecalculatePrices" runat="server" DataField="RecalculatePrices" CommitChanges="true"/>
							<px:PXCheckBox ID="edOverrideManualPrices" runat="server" DataField="OverrideManualPrices" CommitChanges="true" Style="margin-left: 25px"/>
							<px:PXCheckBox ID="edRecalculateDiscounts" runat="server" DataField="RecalculateDiscounts" CommitChanges="true"/>
							<px:PXCheckBox ID="edOverrideManualDiscounts" runat="server" DataField="OverrideManualDiscounts" CommitChanges="true" Style="margin-left: 25px"/>
							<px:PXCheckBox ID="edOverrideManualDocGroupDiscounts" runat="server" DataField="OverrideManualDocGroupDiscounts" CommitChanges="true" Style="margin-left: 25px"/>
						</Template>
					</px:PXFormView>
				</Template>
			</px:PXTabItem>
			<px:PXTabItem Text="User-Defined Fields" RepaintOnDemand="false">
				<Template>
					<px:PXGrid ID="grdCreateQuoteInfoUDF" runat="server" Width="500px" DataSourceID="ds" MatrixMode="True" AutoAdjustColumns="true">
						<Levels>
							<px:PXGridLevel DataMember="CreateQuoteInfoUDF">
								<Columns>
									<px:PXGridColumn DataField="DisplayName" Width="250px"/>
									<px:PXGridColumn DataField="Value" AutoCallBack="True" CommitChanges="True" Width="250px"/>
								</Columns>
								<Layout ColumnsMenu="False" />
								<Mode AllowAddNew="false" AllowDelete="false" />
							</px:PXGridLevel>
						</Levels>
						<Mode InitNewRow="true" />
						<AutoSize Enabled="True" MinHeight="150" />
					</px:PXGrid>
				</Template>
			</px:PXTabItem>
		</Items>
	</px:PXTab>
	<px:PXPanel ID="formCreateQuoteBtn" runat="server" SkinID="Buttons">
		<px:PXButton ID="CreateQuoteBtnReview" runat="server" Text="Create and Review" CommitChanges="True" DialogResult="Yes"></px:PXButton>
		<px:PXButton ID="CreateQuoteBtnConvert" runat="server" Text="Create" CommitChanges="True" DialogResult="No" Style="margin-left: 5px" />
		<px:PXButton ID="CreateQuoteBtnCancel" runat="server" Text="Cancel" CommitChanges="True" DialogResult="Cancel" Style="margin-left: 5px" />
	</px:PXPanel>
</px:PXSmartPanel>