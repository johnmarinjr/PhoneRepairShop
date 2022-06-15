<%@ Page Language="C#" CodeFile="SM303020.aspx.cs" Inherits="Pages_SM303020" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true" ValidateRequest="false" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormTab.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <script type="text/javascript">
        function commandResult(ds, context) {
            var autoIdGrid = px_all["ctl00_phG_tab_t1_gridAutoIdentifyingRules"];
            var rolesGrid = px_all["ctl00_phG_tab_t3_gridRolesMappingRules"];

            var ruleRow = null;
            switch (context.command) {
                case "AutoIdentityRuleDown":
                    ruleRow = autoIdGrid.activeRow.nextRow();
                    break;
                case "AutoIdentityRuleUp":
                    ruleRow = autoIdGrid.activeRow.prevRow();
                    break;
                case "RoleMappingRuleDown":
                    ruleRow = rolesGrid.activeRow.nextRow();
                    break;
                case "RoleMappingRuleUp":
                    ruleRow = rolesGrid.activeRow.prevRow();
                    break;
            }

            if (ruleRow) ruleRow.activate();
        }
    </script>
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.OidcClient.OidcProviderMaint" PrimaryView="Providers">
        <CallbackCommands>
            <px:PXDSCallbackCommand DependOnGrid="gridAutoIdentifyingRules" Name="AutoIdentityRuleUp" Visible="False" />
		    <px:PXDSCallbackCommand DependOnGrid="gridAutoIdentifyingRules" Name="AutoIdentityRuleDown" Visible="False" />
            <px:PXDSCallbackCommand DependOnGrid="gridRolesMappingRules" Name="RoleMappingRuleUp" Visible="False" />
		    <px:PXDSCallbackCommand DependOnGrid="gridRolesMappingRules" Name="RoleMappingRuleDown" Visible="False" />
		</CallbackCommands>
        <ClientEvents CommandPerformed="commandResult"/>
    </px:PXDataSource>
</asp:Content>

<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">

    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Width="100%"
		Caption="Provider Summary" DataMember="Providers">
                <Template>
            <px:PXLayoutRule runat="server" StartRow="True" ControlSize="XL" LabelsWidth="M" StartColumn="True" />
            <px:PXSelector ID="edName" runat="server" DataField="Name" DataSourceID="ds" AutoRefresh="True" FilterByAllFields="true">
                        <GridProperties>
                            <Columns>
							    <px:PXGridColumn DataField="Name" Width="300px" />
							    <px:PXGridColumn DataField="IssuerIdentifier" Width="300px" />
							    <px:PXGridColumn DataField="Active" Type="CheckBox" />
						    </Columns>
                </GridProperties>   
                    </px:PXSelector>
                    <px:PXTextEdit ID="edIssuerIdentifier" runat="server" DataField="IssuerIdentifier" />
            <px:PXCheckBox ID="edActive" runat="server" DataField="Active" Text="Active" />
            <px:PXTextEdit ID="edClientId" runat="server" DataField="ClientId" />
            <px:PXTextEdit ID="edClientSecret" runat="server" DataField="ClientSecret" />
            <px:PXDropDown ID="edUserIdentityClaimType" runat="server" DataField="UserIdentityClaimType" />
            <px:PXDropDown ID="edScopes" runat="server" DataField="Scopes" />

            <px:PXLayoutRule runat="server" LabelsWidth="XS" ControlSize="XS" GroupCaption="Icon" StartGroup="True"  StartColumn="True"/>
			<px:PXLabel ID="lblIconImgUploader" runat="server">Recommended Size: Width 100px, Height 100px</px:PXLabel>
            <px:PXImageUploader ID="edIcon" runat="server" DataField="Icon" Width="390px" AllowUpload="True" 
                SuppressLabel="True" AllowNoImage="true" />
            <px:PXTextEdit ID="lblIcon" runat="server" DataField="IconGetter" Width="322px" Enabled="False" />
                </Template>
    </px:PXFormView>
</asp:Content>

<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
    <px:PXTab ID="tab" runat="server" Width="100%" DataSourceID="ds" DataMember="CurrentProvider">
        <Items>
            <px:PXTabItem Text="Authentication settings">
                <Template>
                    <px:PXLayoutRule runat="server" StartRow="True" ControlSize="XL" LabelsWidth="M" StartColumn="True" />
                    <px:PXButton ID="btnAutoconfiguration" runat="server" Text="Autoconfiguration" CommandName="Autoconfiguration" CommandSourceID="ds"/>
                    <px:PXTextEdit ID="edAuthorizationEndpoint" runat="server" DataField="AuthorizationEndpoint" />
                    <px:PXDropDown ID="edFlow" runat="server" DataField="Flow" CommitChanges="true" />
                    <px:PXDropDown ID="edResponseType" runat="server" DataField="ResponseType" />
                    <px:PXDropDown ID="edResponseMode" runat="server" DataField="ResponseMode" />
                    <px:PXTextEdit ID="edTokenEndpoint" runat="server" DataField="TokenEndpoint" />
                    <px:PXTextEdit ID="edJWKSetLocation" runat="server" DataField="JWKSetLocation" />
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="User Binding Rules">
                <Template>
                    <px:PXLayoutRule runat="server" StartRow="True" ControlSize="XL" StartColumn="True" SuppressLabel="true" />
                    <px:PXCheckBox ID="edAllowAutoIdentifyingUsers" runat="server" DataField="AllowAutoIdentifyingUsers" />
                    <px:PXGrid ID="gridAutoIdentifyingRules" runat="server" DataSourceID="ds" AutoRefresh="True" AutoAdjustColumns="True" 
                               Width="100%" Height="100%" AllowSearch="True" SkinID="Details" MatrixMode="true">
                        <ActionBar>
                            <Actions>
                                <AdjustColumns ToolBarVisible="False" />
                                <ExportExcel ToolBarVisible="False" />
                            </Actions>
                        </ActionBar> 
                        <Mode InitNewRow="True" AllowRowSelect="False" />
                        <Levels>
                            <px:PXGridLevel DataMember="AutoIdentifyingUsersRules">
                                <Columns>
                                    <px:PXGridColumn DataField="Active" Width="60" Type="CheckBox" TextAlign="Center" AllowResize="false" />
                                    <px:PXGridColumn DataField="OpenBrackets" Type="DropDownList" AllowNull="False" Width="100px" CommitChanges="True"
                                        AutoCallBack="true"/>
                                    <px:PXGridColumn DataField="UserField" Type="DropDownList" AllowNull="False" Width="200px" />
                                    <px:PXGridColumn DataField="ClaimType" Type="DropDownList" Width="200px" CommitChanges="True" />
                                    <px:PXGridColumn DataField="Scope" Type="DropDownList" Width="200px" />
                                    <px:PXGridColumn DataField="CloseBrackets" Type="DropDownList" AllowNull="False" Width="100px" />
                                    <px:PXGridColumn DataField="Operator" Type="DropDownList" AllowNull="False" Width="90px" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Enabled="True" MinHeight="150" />
                        <ActionBar>
						    <CustomItems>
							  <px:PXToolBarButton CommandName="AutoIdentityRuleUp" CommandSourceID="ds" Tooltip="Move Row Up">
									<Images Normal="main@ArrowUp" />
								</px:PXToolBarButton>
								<px:PXToolBarButton CommandName="AutoIdentityRuleDown" CommandSourceID="ds" Tooltip="Move Row Down">
									<Images Normal="main@ArrowDown" />
								</px:PXToolBarButton>
						    </CustomItems>
					    </ActionBar>
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="User Creation Rules">
                <Template>
                    <px:PXLayoutRule runat="server" StartRow="True" ControlSize="XL" StartColumn="True" SuppressLabel="true" LabelsWidth="XS" />
                    <px:PXCheckBox ID="edAllowAutoAddingUsers" runat="server" DataField="AllowAutoAddingUsers" />
                    <px:PXSelector ID="edLoginType" runat="server" DataField="LoginTypeID" CommitChanges="True" AllowEdit="True" 
                        SuppressLabel="false" />
                    <px:PXGrid ID="gridAutoAddingRules" runat="server" DataSourceID="ds" AutoRefresh="True" AutoAdjustColumns="True" 
                               Width="100%" Height="100%" AllowSearch="True" SkinID="Details" MatrixMode="true">
                        <ActionBar>
                            <Actions>
                                <AdjustColumns ToolBarVisible="False" />
                                <ExportExcel ToolBarVisible="False" />
                            </Actions>
                        </ActionBar> 
                        <Mode InitNewRow="True" AllowRowSelect="False" />
                        <Levels>
                            <px:PXGridLevel DataMember="AutoAddingUsersRules">
                                <Columns>
                                    <px:PXGridColumn DataField="Active" Width="60" Type="CheckBox" TextAlign="Center" AllowResize="false" />
                                    <px:PXGridColumn DataField="UserField" Type="DropDownList" AllowNull="False" Width="200px" />
                                    <px:PXGridColumn DataField="ClaimType" Type="DropDownList" Width="200px" CommitChanges="True" />
                                    <px:PXGridColumn DataField="Scope" Type="DropDownList" Width="200px" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Enabled="True" MinHeight="150" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Role Mapping Rules">
                <Template>
                    <px:PXLayoutRule runat="server" StartRow="True" ControlSize="XL" StartColumn="True" SuppressLabel="true" LabelsWidth="XS" />
                    <px:PXCheckBox ID="edAllowOverridingLocalRoles" runat="server" DataField="AllowOverridingLocalRoles" CommitChanges="true" />
                    <px:PXTextEdit ID="edRolesClaimType" runat="server" DataField="RolesClaimType" SuppressLabel="false" />
                    <px:PXTextEdit ID="edRolesScope" runat="server" DataField="RolesScope" SuppressLabel="false" />
                    <px:PXGrid ID="gridRolesMappingRules" runat="server" DataSourceID="ds" AutoRefresh="True" AutoAdjustColumns="True" 
                               Width="100%" Height="100%" AllowSearch="True" SkinID="Details" MatrixMode="true">
                        <ActionBar>
                            <Actions>
                                <AdjustColumns ToolBarVisible="False" />
                                <ExportExcel ToolBarVisible="False" />
                            </Actions>
                        </ActionBar> 
                        <Mode InitNewRow="True" AllowRowSelect="False" />
                        <Levels>
                            <px:PXGridLevel DataMember="RolesMappingRules">
                                <Columns>
                                    <px:PXGridColumn DataField="Active" Width="60" Type="CheckBox" TextAlign="Center" AllowResize="false" />
                                    <px:PXGridColumn DataField="ClaimValue" Width="200px" />
                                    <px:PXGridColumn DataField="RoleName" Type="DropDownList" AllowNull="False" Width="200px" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Enabled="True" MinHeight="150" />
                        <ActionBar>
						    <CustomItems>
							  <px:PXToolBarButton CommandName="RoleMappingRuleUp" CommandSourceID="ds" Tooltip="Move Row Up">
									<Images Normal="main@ArrowUp" />
								</px:PXToolBarButton>
								<px:PXToolBarButton CommandName="RoleMappingRuleDown" CommandSourceID="ds" Tooltip="Move Row Down">
									<Images Normal="main@ArrowDown" />
								</px:PXToolBarButton>
						    </CustomItems>
					    </ActionBar>
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
        </Items>
        <AutoSize Container="Window" Enabled="True" MinHeight="150" />
    </px:PXTab>  

    <px:PXSmartPanel ID="pnlChangeID" runat="server" Caption="Specify New Name" CaptionVisible="true" DesignView="Hidden" LoadOnDemand="true" 
        Key="ChangeIDDialog" CreateOnDemand="false" AutoCallBack-Enabled="true" AcceptButtonID="btnOK"
        AutoCallBack-Target="formChangeID" AutoCallBack-Command="Refresh" CallBackMode-CommitChanges="True" CallBackMode-PostData="Page">
        <px:PXFormView ID="formChangeID" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" CaptionVisible="False" DataMember="ChangeIDDialog">
            <ContentStyle BackColor="Transparent" BorderStyle="None" />
            <Template>
                <px:PXLayoutRule ID="rlAcctCD" runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
                <px:PXTextEdit ID="edAcctCD" runat="server" DataField="CD" />
            </Template>
        </px:PXFormView>
        <px:PXPanel ID="pnlChangeIDButton" runat="server" SkinID="Buttons">
            <px:PXButton ID="btnOK" runat="server" DialogResult="OK" Text="OK">
                <AutoCallBack Target="formChangeID" Command="Save" />
            </px:PXButton>
            <px:PXButton ID="PXButton3" runat="server" DialogResult="Cancel" Text="Cancel" />
        </px:PXPanel>
    </px:PXSmartPanel>

    <px:PXSmartPanel ID="pnlRedirectURI" runat="server" Caption="Redirect URI" CaptionVisible="true" DesignView="Hidden" LoadOnDemand="true" 
        Key="StaticInfo" CreateOnDemand="false" Width="500px">
        <px:PXFormView ID="formRedirectURI" runat="server" DataSourceID="ds" Width="100%" CaptionVisible="False" DataMember="StaticInfo">
            <ContentStyle BackColor="Transparent" BorderStyle="None" />
            <Template>
                <px:PXLayoutRule ID="rlRedirectURI" runat="server" StartColumn="True" SuppressLabel="true" ControlSize="XL" />
                <px:PXTextEdit ID="edRedirectURI" runat="server" DataField="RedirectURI" Width="430px" SuppressLabel="true" Enabled="false" style="margin-bottom: 20px;" />                
                <px:PXButton ID="btnCpy" runat="server" Text="Copy" AlignLeft="true">
                    <ClientEvents Click="copy_Click" />
                </px:PXButton>
            </Template>
        </px:PXFormView>   
        <script type="text/javascript">
            function copy_Click() {
                try {
                    var redirectUriInput = px_alls["edRedirectURI"];
                    redirectUriInput.select();
                    document.execCommand("copy");
                }
                catch (e) {

                }
            }
        </script>
    </px:PXSmartPanel>

    <px:PXSmartPanel ID="pnlMetadata" runat="server" Caption="Provider Metadata" CaptionVisible="true" DesignView="Hidden" LoadOnDemand="true" 
        Key="MetadataDocumentInfo" CreateOnDemand="false" Width="800px" CloseButtonDialogResult="OK" AutoCallBack-Enabled="true" >
        <px:PXFormView ID="formMetadata" runat="server" DataSourceID="ds" Width="100%" CaptionVisible="False" DataMember="MetadataDocumentInfo">
            <ContentStyle BackColor="Transparent" BorderStyle="None" />
            <Template>
                <px:PXLayoutRule ID="rlRedirectURI" runat="server" StartColumn="True" ControlSize="XL" />
                <px:PXTextEdit ID="edMetadataURI" runat="server" DataField="MetadataURI" Enabled="false" Width="550px" style="margin-bottom: 20px;"/>
                <px:PXLabel ID="lblMetadataDocument" runat="server" Text="Metadata Document:" />
                <px:PXTextEdit ID="edMetadataDocument" runat="server" DataField="MetadataDocument" Enabled="false" SuppressLabel="true"
                    TextMode="MultiLine" Width="700px" Height="400px" />
            </Template>
        </px:PXFormView>
    </px:PXSmartPanel>
    
</asp:Content>
