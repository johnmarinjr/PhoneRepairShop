<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="IN204025.aspx.cs"
    Inherits="Page_IN204025" Title="Scan Warehouse Path" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:content id="cont1" contentplaceholderid="phDS" runat="Server">
    <script language="javascript" type="text/javascript">
        function Barcode_Initialize(ctrl) {
            ctrl.element.addEventListener('keydown', function (e) {
                if (e.keyCode === 13) { //Enter key 
                    e.preventDefault();
                    e.stopPropagation();
                }
            });
        };
    </script>
    <script language="javascript" type="text/javascript">
        function ActionCallback(callbackContext) {
            var baseUrl = (location.href.indexOf("HideScript") > 0) ? "../../Sounds/" : "../../../Sounds/";
            var edInfoMessageSoundFile = px_alls["edInfoMessageSoundFile"];

            if ((callbackContext.info.name.toLowerCase().startsWith("scan") || callbackContext.info.name == "ElapsedTime") && callbackContext.control.longRunInProcess == null && edInfoMessageSoundFile != null) {
                var soundFile = edInfoMessageSoundFile.getValue();
                if (soundFile != null && soundFile != "") {
                    var audio = new Audio(baseUrl + soundFile + '.wav');
                    audio.play();
                }
            }
        };

        window.addEventListener('load', function () { px_callback.addHandler(ActionCallback); });
    </script>
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.IN.WMS.INScanWarehousePath+Host" PrimaryView="HeaderView">
        <CallbackCommands>
        </CallbackCommands>
    </px:PXDataSource>
</asp:content>
<asp:content id="cont2" contentplaceholderid="phF" runat="Server">
    <px:PXFormView ID="formHeader" runat="server" DataSourceID="ds" Height="120px" Width="100%" Visible="true" DataMember="HeaderView" DefaultControlID="edBarcode" FilesIndicator="True">
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="L" />
            <px:PXTextEdit ID="edBarcode" runat="server" DataField="Barcode" CommitChanges="true">
                <AutoCallBack Command="Scan" Target="ds">
                    <Behavior CommitChanges="True" />
                </AutoCallBack>
                <ClientEvents Initialize="Barcode_Initialize"/>
            </px:PXTextEdit>
            <px:PXSelector ID="edSiteID" runat="server" DataField="SiteID" AllowEdit="true" />
            <px:PXNumberEdit ID="edNextPathIndex" runat="server" DataField="NextPathIndex" />
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="L" ColumnWidth="M" />
            <px:PXTextEdit ID="edMessage" runat="server" DataField="Message" Width="800px" Style="font-size: 10pt; font-weight: bold;" SuppressLabel="true" TextMode="MultiLine" Height="55px" SkinID="Label" DisableSpellcheck="True" Enabled="False" />
            <px:PXLayoutRule runat="server" ColumnSpan="2" Merge="True" />
            <px:PXTextEdit ID="edMode" runat="server" DataField="Mode"/>
        </Template>
    </px:PXFormView>
    <px:PXFormView ID="formInfo" runat="server" DataSourceID="ds" DataMember="Info">
        <Template>
            <px:PXTextEdit ID="edInfoMode" runat="server" DataField="Mode"/>
            <px:PXTextEdit ID="edInfoMessage" runat="server" DataField="Message"/>
            <px:PXTextEdit ID="edInfoMessageSoundFile" runat="server" DataField="MessageSoundFile"/>
            <px:PXTextEdit ID="edInfoInstructions" runat="server" DataField="Instructions"/>
            <px:PXTextEdit ID="edInfoPrompt" runat="server" DataField="Prompt"/>
        </Template>
    </px:PXFormView>
</asp:content>
<asp:content id="cont3" contentplaceholderid="phG" runat="Server">
    <px:PXTab ID="tab" runat="server" Height="540px" Style="z-index: 100;" Width="100%">
        <Items>
            <px:PXTabItem Text="Warehouse">
                <Template>
                    <px:PXGrid ID="grid" runat="server" DataSourceID="ds" Width="100%" SkinID="DetailsInTab" SyncPosition="true" FilesIndicator="False" NoteIndicator="False">
                        <Levels>
                            <px:PXGridLevel DataMember="location">
                                <Columns>
                                    <px:PXGridColumn DataField="LocationCD"/>
                                    <px:PXGridColumn DataField="Descr"/>
                                    <px:PXGridColumn DataField="PathPriority" />
                                </Columns>
                                <RowTemplate>
                                    <px:PXTextEdit runat="server" ID="edLocation" DataField="LocationCD" Enabled ="False"/>
                                    <px:PXTextEdit runat="server" ID="edDescr" DataField="Descr" Enabled ="False"/>
                                </RowTemplate>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Container="Window" Enabled="True" MinHeight="150" />
                        <Mode AllowAddNew="False" AllowDelete="False" AllowUpdate="False" AllowSort="False" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Scan Log">
                <Template>
                    <px:PXGrid ID="grid4" runat="server" DataSourceID="ds" Style="height: 250px;" Width="100%" SkinID="Inquire" Height="372px" TabIndex="-7375" OnRowDataBound="LogGrid_RowDataBound">
                        <Levels>
                            <px:PXGridLevel DataMember="Logs">
                                <Columns>
                                    <px:PXGridColumn DataField="ScanTime" />
                                    <px:PXGridColumn DataField="Mode" />
                                    <px:PXGridColumn DataField="Prompt" />
                                    <px:PXGridColumn DataField="Scan" />
                                    <px:PXGridColumn DataField="Message" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Container="Window" Enabled="True" MinHeight="400" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
        </Items>
        <AutoSize Enabled="True" Container="Window" />
    </px:PXTab>
</asp:content>