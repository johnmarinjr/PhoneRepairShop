<%@ Page Language="C#" MasterPageFile="~/MasterPages/Workspace.master" AutoEventWireup="true"
    ValidateRequest="false" CodeFile="ShowWiki.aspx.cs" Inherits="Page_ShowWiki"
    Title="Untitled Page" %>

<%@ Import Namespace="PX.Data" %>

<%@ Register TagPrefix="a" Namespace="System.Windows.Forms" Assembly="System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" %>
<%@ MasterType VirtualPath="~/MasterPages/Workspace.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
<style type="text/css">
        /*.collapsible.collapsed .hide{ display:none;}
        .collapsible:not(.collapsed) .show{ display:none;}*/
	h1.wikiH1, h2.wikiH2 {
		padding-top: initial;
		margin-top: 1.6em;
		position:relative;
	}
	[collapserange].anim {
		transition: height 0.15s ease-in;
	}

	[collapserange].folded {
		display: none;
	}
	[collapserange] {
		overflow-y: hidden;
	}

	[collapse] .fold-arrow > span {
		background-repeat: no-repeat;
		display: block;
		width: 24px;
		height: 24px;
		position: absolute;
		left: 0px;
		/*top: -262px;*/
	}

	[collapse] > .fold-wrap > .fold-arrow {
		/*overflow: hidden;*/
		width: 20px;
		height: 20px;
		position: absolute;
		transition: transform 0.15s linear;
		/*transform: rotateZ(90deg);*/
	}
	.fold-wrap{
		display:inline-block;
		position:relative;
		width:0px;
	}
	.fold-arrow{
		padding-right:5px;
	}
	h1[collapse] > .fold-wrap > .fold-arrow {
		top: 3px;
	}
	h2[collapse] > .fold-wrap > .fold-arrow {
		top: 1px;
	}
	.filler{
		float: right;
		height: 25px;
		width: 125px;
		/*background: pink;*/
	}
	h1 > .jumptopedit, h1 > .jumptop, h1 > .editwiki, h1 > .editwikitop {
		top: 7px;
	}
	h2 > .jumptopedit, h2 > .jumptop, h2 > .editwiki, h2 > .editwikitop {
		top: 5px;
	}
	h1:not(:hover) > .jumptopedit, h1:not(:hover) > .jumptop, h1:not(:hover) > .editwiki, h1:not(:hover) > .editwikitop,
	h2:not(:hover) > .jumptopedit, h2:not(:hover) > .jumptop, h2:not(:hover) > .editwiki, h2:not(:hover) > .editwikitop {
		display:none;
	}

	.jumptopedit, .jumptop, .editwiki, .editwikitop{
		position: absolute;
		font-size: small;
		font-weight: normal;
	}
	.jumptopedit, .jumptop, .editwiki{
		right: 5px;
	}
	.editwikitop {
		right: 85px;
	}


	[collapse] .fold-arrow:not(.tilt) {
		transform: rotateZ(-180deg);
	}

	[collapse]{
		position: relative;
		overflow: hidden;
	}

	.toggle-all.expand span.collapse, .toggle-all:not(.expand) span.expand{
		display:none;
	}
	.toggle-all
	{
		cursor: pointer;
	}
	.fold-arrow > span {
	  background-image: url(data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PHBhdGggIGZpbGw9IiM4MDgwODAiIGQ9Ik0xMiAxNS43bC02LTYgMS40LTEuNCA0LjYgNC42IDQuNi00LjZMMTggOS43bC02IDZ6Ii8+PC9zdmc+);
	}

	.HintBox, .WarnBox, .DangerBox, .GoodPracticeBox
	{
        width: unset;
		border-radius: 10px;
		border: none;
		padding: 15px;
	}
	.WarnBox, .WarnBox table.GrayBoxContent td.boxcontent, .WarnBox table.GrayBoxContent td.warncell
	{
		background-color: #fef9eb;
	}
	.HintBox, .HintBox table.GrayBoxContent td.boxcontent, .HintBox table.GrayBoxContent td.hintcell
	{
		background-color: #ebf4fc;
	}
	.DangerBox, .DangerBox table.GrayBoxContent td.boxcontent, .DangerBox table.GrayBoxContent td.dangercell
	{
		background-color: #ffebeb;
	}
	.GoodPracticeBox, .GoodPracticeBox table.GrayBoxContent td.boxcontent, .GoodPracticeBox table.GrayBoxContent td.goodpracticecell
	{
		background-color: #eefbee;
	}
	.WarnBox div.text-BoxWarn:before
	{
		color: #f5a623;
	}
    .DangerBox div.text-BoxWarn:before
	{
		color: #ff0000;
	}
	.WarnBox .GrayBoxContent td.warncell, .HintBox .GrayBoxContent td.hintcell, .DangerBox .GrayBoxContent td.dangercell, .GoodPracticeBox .GrayBoxContent td.goodpracticecell
	{
		padding: 0px 10px 0 0;
		vertical-align: top;
	}
	.WarnBox p, .HintBox p, .DangerBox p, .GoodPracticeBox p
	{
		margin: 0;
	}
	.WarnBox .text-icon, .DangerBox .text-icon
	{
		height: 22px;
		width: 22px;
		font-size: 22px;
	}
	.HintBox i.ac-info
	{
		color: #0278d7;
		font-size: 22px;
	}	
	.GoodPracticeBox i.ac-check_circle
	{
		color: #2fc728;
		font-size: 22px;
	}
	pre
	{
		min-height: 18px;
		overflow: auto;
	}
	td>a>img
	{
		width: 100%
	}
	i
	{
		word-break: break-all;
	}
	code, tt
	{
		word-break: break-all;
	}
	.DurationText 
	{
		color: gray;
		font-size: 12px;
		display: flex;
	}
	.DurationText .Text {
		padding: 0 0;
	}
	
	pre.HideState
	{
		max-height: 18px;
		height: 18px;
		overflow: hidden;
	}
	pre div.HideCodeDiv
	{
		padding: 0;
		margin: 0;
		margin-top: -22px;
		height: 22px;
		text-align: right;
		font-family: Arial, sans-serif;
	}
	pre div.HideCodeDiv span
	{
		display: none;
	}
	pre:hover div.HideCodeDiv span, pre.HideState:hover div.HideCodeDiv span
	{
		display: block;
	}
	pre div.HideCodeDiv a.HideCodeBtn
	{
		margin: 0;
		padding: 0;
		cursor: pointer;
		display: flex;
		flex-direction: row;
		justify-content: flex-end;
	}
	a.HideCodeBtn span {
		position: absolute;
		border: 1px solid transparent;
	}
	pre
	{
		white-space: pre-wrap;
		min-height: 18px;
	}
	pre .CopyCodeDiv
	{
		float: right;
		margin: 0px;
		padding: 0px;
		text-align: end;
		visibility: hidden;
	}
	pre:hover .CopyCodeDiv
	{
		visibility: visible;
	}
	.CopyCodeBtn
	{
		color: black;
		background: transparent;
		font-weight: normal;
		border: 2px none #ac73cc;
		height: 20px;
		border-radius: 4px;
		text-transform: uppercase;
		cursor: pointer;
		font-size: 12px;
		padding: 0 6px 0 4px;
	}
	.CopyCodeBtn:hover
	{
		background: #027acc;
		color: white;
	}
    </style>
		<script type="text/javascript">
			var getButtons = function ()
			{
			    if (!px_alls) return;
				var buttons = px_alls['ToolBar'].items;
				if (!buttons) return;
				var expandAll = buttons.filter(function (_) { return _.commandName == 'expandAll' }).pop();
				var collapseAll = buttons.filter(function (_) { return _.commandName == 'collapseAll' }).pop();
				return (expandAll && collapseAll) ? { 'expand': expandAll, 'collapse': collapseAll } : undefined;
			}
			var updateToggler = function ()
			{
				var buttons = getButtons();
				if (!buttons) return;

				if (document.body.querySelectorAll('.collapsible.collapsed').length)
				{
					buttons.expand.setVisible(true);
					buttons.collapse.setVisible(false);
				} else if (document.body.querySelectorAll('.collapsible:not(.collapsed)').length)
				{
					buttons.expand.setVisible(false);
					buttons.collapse.setVisible(true);
				}
			}
			
			var toggle = function (e)
			{
				var target;
				var parent = e.target.parentNode;
				if (e.target.nodeName == 'A' && e.target.classList.contains('anchorlink') && !e.target.classList.contains('wikilink'))
				{
					var hash = e.target.href.split('#').pop();
					if (!hash) return;
					var sibling = document.body.querySelector('#' + hash);
					if (!sibling) return;
					target = sibling.nextElementSibling;
					if (!target.classList.contains('collapsed')) return;
				}
				else target = e.target;
				while (target && !(target.getAttribute && target.getAttribute('collapse'))) target = target.parentNode;
				if (!target) return;
				var parentAttr = target.getAttribute('parentsec');
				if (parentAttr)
				{
					var parentHeader = document.querySelector('[collapse="' + parentAttr + '"]');
					if (parentHeader && parentHeader.classList.contains('collapsed'))
					{
						toggle({ 'target': parentHeader });
					}
				}
				var attr = target.getAttribute('collapse');
				var div = document.body.querySelector('[collapserange="' + attr + '"]');
				if (!div) return;
				if (div.classList.contains('anim')) return;
				var span = target.querySelector('.fold-arrow');
				if (target.classList.contains('collapsed'))
				{
					//target.scrollIntoView();
					div.classList.remove('folded');
					target.classList.remove('collapsed');
					var height = div.offsetHeight;
					div.style.height = "0px";
					div.classList.add('anim');
					span.classList.remove('tilt');
					setTimeout(function ()
					{
						div.style.height = height + "px"; //needed for chrome & firefox
						//span.classList.remove('tilt');
						setTimeout(function ()
						{
							div.classList.remove('anim');
							div.style.height = "";
							updateToggler();
						}, 100);
					}, 10);//should be greater than zero for firefox, zero is enough for chrome
				} else
				{
					div.classList.add('anim');
					div.style.height = div.offsetHeight + "px";
					setTimeout(function ()
					{
						div.style.height = "0px"; //needed for chrome & firefox
						span.classList.add('tilt');
						setTimeout(function ()
						{
							div.classList.add('folded');
							div.classList.remove('anim');
							div.style.height = "";
							target.classList.add('collapsed');
							updateToggler();
						}, 100);
					}, 10);//should be greater than zero for firefox, zero is enough for chrome
					Array.prototype.forEach.call(document.querySelectorAll('[parentsec="' + attr + '"]'),
						function (item)
						{
							if (!item.classList.contains('collapsed'))
							{
								toggle({ 'target': item });
							}
						});
					
				}
			};

			function initializeDataSource(sender, ev)
			{
				px_alls['form'].focus();
				px_alls['ToolBar'].events.addEventHandler('buttonClick', function (id, ev)
				{
					if (ev.button.commandName == 'expandAll')
					{
						var items = document.body.querySelectorAll('.collapsible.collapsed');
						Array.prototype.forEach.call(items, function (elem) { toggle({ 'target': elem }) });
						ev.cancel = true;
					}
					else if(ev.button.commandName == 'collapseAll')
					{
						var items = document.body.querySelectorAll('.collapsible:not(.collapsed)');
						Array.prototype.forEach.call(items, function (elem) { toggle({ 'target': elem }) });
						ev.cancel = true;
					}
				})
				document.body.addEventListener('click', toggle);
				__px_cm(window).registerAfterLoad(updateToggler);
			}
			function performedDataSource(sender, ev) {
				if (ev.command === "saveTypoInfo") {
					setTimeout(function () {
						document.getSelection().empty();
						typoToolBtn.setEnabled(false);
					}, 200);
				}
			}
		</script>
    
    <px:PXDataSource ID="ds" runat="server" Visible="True" TypeName="PX.SM.WikiShowReader"
        PrimaryView="Pages" style="float: left" QPToolBar="False" >
        <CallbackCommands>
            <px:PXDSCallbackCommand Name="Insert" PostData="Self" StartNewGroup="True" />
            <px:PXDSCallbackCommand Name="getFile" Visible="False" />
            <px:PXDSCallbackCommand Name="viewProps" Visible="False" />
            <px:PXDSCallbackCommand Name="checkOut" Visible="False" />
            <px:PXDSCallbackCommand Name="undoCheckOut" Visible="False" />
            <px:PXDSCallbackCommand Name="SaveTypoInfo" CommitChanges="True" ClosePopup="true" />
        </CallbackCommands>
        <ClientEvents Initialize="initializeDataSource" ButtonClick="performedDataSource" CommandPerformed="performedDataSource" />
    </px:PXDataSource>

    <px:PXDataSource ID="dsTemplate" runat="server" Visible="False"
        PrimaryView="Pages" TypeName="PX.SM.WikiNotificationTemplateMaintenanceNoRefresh" QPToolBar="False">
        <DataTrees>
            <px:PXTreeDataMember TreeKeys="Key" TreeView="EntityItems" />
        </DataTrees>
        <CallbackCommands>
            <px:PXDSCallbackCommand Name="cancel" Visible="False" />
        </CallbackCommands>
    </px:PXDataSource>

    <px:PXToolBar ID="toolbar1" runat="server" SkinID="Navigation" ImageSet="main">
        <Items>
            <px:PXToolBarButton Key="print" Text="Print" Target="main" Tooltip="Print Current Article" ImageKey="Print" DisplayStyle="Text" />
            <px:PXToolBarButton Key="export" Text="Export" ImageKey="Export" DisplayStyle="Text">
                <MenuItems>
                    <px:PXMenuItem Text="Plain Text">
                    </px:PXMenuItem>
                    <px:PXMenuItem Text="Word">
                    </px:PXMenuItem>
                </MenuItems>
            </px:PXToolBarButton>
        </Items>
        <Layout ItemsAlign="Left" />
        <ClientEvents ButtonClick="onButtonClick" />
    </px:PXToolBar>
    <div style="clear: left" />

    <div id="Summary" runat="server">
        <px:PXFormView ID="PXFormView3" runat="server" CaptionVisible="False" Style="margin: 15px; padding-top: 15px; padding-left: 15px; padding-bottom: 15px; position: static; background-color: #22b14c;"
            Width="890px" AllowFocus="False" RenderStyle="Simple" Visible="False">
            <Template>
                <px:PXLabel ID="UserMessage" runat="server" Text="Chto-to" Style="position: static; color: white; font-size: 14pt; height: 60px;" />
            </Template>
        </px:PXFormView>

        <px:PXFormView ID="PXFormView1" runat="server" CaptionVisible="False" Style="position: static;"
            Width="925px" AllowFocus="False" RenderStyle="Simple">
            <Template>
                <div style="padding: 5px; position: static;">
                    <div style="border-style: none;">
                        <table style="position: static; margin-left: 5px; border-color: #ECE9E8; height: 60px;" width="auto">
                            <tr>
                                <td style="height: 60px; width: auto;">
                                    <table style="position: static; margin-left: 5px; border-color: #ECE9E8; height: 60px;" width="auto">
                                        <tr>
                                            <td>
                                                <px:PXLabel runat="server" ID="PXKB" Text="KB:" Style="font-size: 18pt; text-wrap: none; white-space: nowrap" />
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style="height: 12px;">
                                                <px:PXLabel runat="server" ID="PXCategori" Text="Category:" Style="text-wrap: none; white-space: nowrap" />
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style="height: 12px;">
                                                <px:PXLabel runat="server" ID="PXProduct" Text="Applies to:" Style="text-wrap: none; white-space: nowrap" />
                                            </td>
                                        </tr>
                                    </table>
                                </td>

                                <td style="height: 60px; width: 100%;" />

                                <td style="height: 70px; width: auto; border: solid; border-color: black; border-width: thin; margin-right: 5px; padding-right: 5px;">
                                    <table style="position: static; margin-left: 5px; border-color: #ECE9E8; height: 82px; margin-right: 5px; padding-right: 5px;" width="auto">
                                        <tr>
                                            <td style="height: 12px;">
                                                <px:PXLabel runat="server" ID="PXKBName" Text="Article:" Style="text-wrap: none; white-space: nowrap" />
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style="height: 12px;">
                                                <px:PXLabel runat="server" ID="PXCreateDate" Text="Created Date: " Style="text-wrap: none; white-space: nowrap" />
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style="height: 12px;">
                                                <px:PXLabel runat="server" ID="PXLastPublished" Text="Last Modified:" Style="text-wrap: none; white-space: nowrap" />
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style="height: 12px;">
                                                <px:PXLabel runat="server" ID="PXViews" Text="Views:" Style="text-wrap: none; white-space: nowrap" />
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style="height: 12px;">
                                                <px:PXLabel runat="server" ID="PXRating" Text="Rating:" Style="text-wrap: none; white-space: nowrap" />
                                                <px:PXImage runat="server" ID="PXImage1" ImageUrl="main@FavoritesGray" />
                                                <px:PXImage runat="server" ID="PXImage2" ImageUrl="main@FavoritesGray" />
                                                <px:PXImage runat="server" ID="PXImage3" ImageUrl="main@FavoritesGray" />
                                                <px:PXImage runat="server" ID="PXImage4" ImageUrl="main@FavoritesGray" />
                                                <px:PXImage runat="server" ID="PXImage5" ImageUrl="main@FavoritesGray" />
                                                <px:PXLabel runat="server" ID="PXdAvRate" Text="" Style="text-wrap: none; white-space: nowrap" />
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                        </table>
                    </div>
                </div>
            </Template>
        </px:PXFormView>
    </div>

    <px:PXFormView ID="form" runat="server" DataSourceID="ds" Height="150px" Style="z-index: 100;"
        Width="100%" DataMember="Pages" DataKeyNames="PageID" SkinID="Transparent" NoteIndicator="False" FilesIndicator="False">
        <Searches>
            <px:PXQueryStringParam Name="PageID" QueryStringField="PageID" Type="String" />
            <px:PXQueryStringParam Name="Language" QueryStringField="Language" Type="String" />
            <px:PXQueryStringParam Name="PageRevisionID" QueryStringField="PageRevisionID" Type="Int32" />
            <px:PXQueryStringParam Name="Wiki" QueryStringField="Wiki" Type="String" />
            <px:PXQueryStringParam Name="Art" QueryStringField="Art" Type="String" />
            <px:PXQueryStringParam Name="Parent" QueryStringField="From" Type="String" />
            <px:PXControlParam ControlID="form" Name="PageID" PropertyName="NewDataKey[&quot;PageID&quot;]" Type="String" />
        </Searches>
        <AutoSize Enabled="True" Container="Window" />
    </px:PXFormView>

    <div id="Rating" runat="server">
        <px:PXFormView ID="PXFormView2" runat="server" CaptionVisible="False" Style="position: static;"
            Width="100%" AllowFocus="False">
            <Template>
                <div style="padding: 5px; position: static;">
                    <div style="border-style: none;">
                        <table style="position: static; border-color: #ECE9E8; height: 20px;" cellpadding="0" cellspacing="0" width="100% ">
                            <tr>
                                <td style="margin-left: 15px; height: 12px; width: 60px;">
                                    <px:PXLabel runat="server" ID="lblRate" Text="Rate this article :" Style="margin-left: 10px; text-wrap: none; white-space: nowrap" />
                                </td>

                                <td style="height: 20px;">
                                    <px:PXDropDown ID="Rate" runat="server" Style="height: 20px; width: 110px; margin-left: 10px" OnCallBack="ddRate_PageRate">
                                        <AutoCallBack Command="ddRate_PageRate">
                                        </AutoCallBack>
                                    </px:PXDropDown>
                                </td>

                                <td style="height: 20px; white-space: nowrap;">
                                    <px:PXButton ID="Button" runat="server" Style="height: 20px; margin-left: 10px;" Text="Rate!" OnCallBack="Rate_PageRate">
                                        <AutoCallBack Command="Rate_PageRate">
                                        </AutoCallBack>
                                    </px:PXButton>
                                </td>

                                <td style="height: 20px; width: 100%;" />

                                <td style="height: 20px; width: 60px;">
                                    <px:PXButton ID="PXButton2" runat="server" Style="height: 20px;" Text="Feedback" OnCallBack="Feedback_Rate">
                                        <AutoCallBack Command="Feedback_Rate">
                                        </AutoCallBack>
                                    </px:PXButton>
                                </td>
                            </tr>
                        </table>
                    </div>
                </div>
            </Template>
        </px:PXFormView>
    </div>

    <div id="dvAnalytics" runat="server">
    </div>

    <div id="SmartPanels" runat="server">
       <px:PXSmartPanel ID="pnlGetLink" runat="server" Caption="This article URL" ForeColor="Black"
            Height="117px" Style="position: static" Width="353px" Position="UnderOwner">
            <px:PXLabel ID="lblLink" runat="server" Style="position: absolute; left: 9px; top: 9px;"
                Text="Internal Link :"></px:PXLabel>
            <px:PXTextEdit ID="edtLink" runat="server" Style="position: absolute; left: 81px; top: 9px;"
                Width="256px">
            </px:PXTextEdit>
            <px:PXLabel ID="lblUrl" runat="server" Style="position: absolute; left: 9px; top: 36px;"
                Text="External Link :"></px:PXLabel>
            <px:PXTextEdit ID="edtUrl" runat="server" Style="position: absolute; left: 81px; top: 36px;"
                Width="256px">
            </px:PXTextEdit>
            <px:PXLabel ID="lblPublicUrl" runat="server" Style="position: absolute; left: 9px; top: 63px;"
                Text="Public Link :"></px:PXLabel>
            <px:PXTextEdit ID="edPublicUrl" runat="server" Style="position: absolute; left: 81px; top: 63px;"
                Width="256px">
            </px:PXTextEdit>
            <px:PXButton ID="PXButton1" runat="server" DialogResult="Cancel" Style="left: 263px; position: absolute; top: 90px; height: 20px;"
                Text="Close" Width="80px">
            </px:PXButton>
        </px:PXSmartPanel>
        <px:PXSmartPanel ID="pnlWikiText" runat="server" CaptionVisible="True" Height="544px"
            Style="position: static" Width="814px">
            <px:PXTextEdit ID="edWikiText" runat="server" Height="536px" Style="position: static; color: Black;"
                TextMode="MultiLine" Width="806px" ReadOnly="True">
            </px:PXTextEdit>
        </px:PXSmartPanel>

        <px:PXSmartPanel ID="PXTypoPanel" runat="server" DesignView="Hidden" CaptionVisible="True" Caption="Send Report" 
        AutoRepaint="true" LoadOnDemand="true" AutoCallBack-Command="Refresh" AutoCallBack-Enabled="true"
        AutoCallBack-Target="PXTypoForm" Key="TypoParametersFilter"
        CallBackMode-CommitChanges="True"
        AcceptButtonID="PXButtonTypoOk" CancelButtonID="PXButtonTypoCancel">
            <px:PXFormView ID="PXTypoForm" runat="server" Style="z-index: 100" Width="100%" CaptionVisible="False" SkinID="Transparent"
            DataSourceID="ds" DataMember="TypoParametersFilter" CheckChanges="False">
            <ClientEvents Initialize="typoOnShow" AfterRepaint="typoOnShow" />
            <Template>
                <px:PXLayoutRule ID="PXLayoutRule44" runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
                <px:PXLabel Style="height:48px" runat="server" Text="Click <B>Send</B> to report a problem. <BR/><B style='margin:14px 0 4px 0;display:block'>Details:</B>"></px:PXLabel>
                <px:PXLabel runat="server" ID="TypoText" Text="Select the text to report a problem"></px:PXLabel>
                <px:PXTextEdit runat="server" style="visibility:hidden" ID="ComplaintTextParameter" DataField="ComplaintTextParameter" LabelWidth="0"></px:PXTextEdit>
            </Template>
        </px:PXFormView>

        <px:PXPanel ID="PXPanelTypoButtons" runat="server" SkinID="Buttons">
            <px:PXButton ID="PXButtonTypoOk" runat="server" DialogResult="OK" 
            CommitChanges="True" CommandSourceID="ds" Text="Send" />
            <px:PXButton ID="PXButtonTypoCancel" runat="server" DialogResult="Cancel" Text="Cancel" />
        </px:PXPanel>

        </px:PXSmartPanel>
    </div>

    <script type="text/javascript">
        px_callback.baseProcessRedirect = px_callback.processRedirect;
        px_callback.processRedirect = function (result, context) {
            var flag = true;
            if (context == null) context = this.context;
            if (context != null && context.context != null)
                if (context.context.command == "delete") {
                    __refreshMainMenu(); flag = false;
                }
            if (flag) this.baseProcessRedirect(result, context);
        }

        function typoOnShow(a,b,c) {
            setTimeout(function () {
                var btnOk = a.px_context.px_all[a.ID.substr(0, a.ID.lastIndexOf("_") + 1) + "PXButtonTypoOk"];
				var inputPar1 = a.controls[a.ID + "_ComplaintTextParameter"];
                var spanTypo = b.srcElement.querySelectorAll("span")[1];
                if (!typo.originalText) typo.originalText = spanTypo.innerText;
                inputPar1.updateValue(typo.text || "");
                btnOk.setEnabled(typo.text != "");
                var showText = "<span style='color:red'> " + typo.text || typo.originalText + " </span>";
                if (typo.text) {
                    if (typo.selectionObj) {
                        if (typo.text.split(/\W/g).length == 1) {
                            var txt = typo.selectionObj.focusNode.textContent;
                            if (txt.indexOf(typo.text) != -1) {
                                var pos1 = typo.selectionObj.anchorOffset;
                                var pos2 = pos1;
                                var rng = 40;
                                if (pos1 - rng < 0) pos1 = 0; else pos1 = pos1 - rng
                                var str1 = txt.substring(pos1, pos2);
                                while (pos1 != 0 && str1) {
                                    if (str1[0] != " ") str1 = str1.substr(1); else break;
                                }
                                var str2 = txt.substr(pos2 + typo.text.length, rng);

                                showText = str1 + "<span style='color:red'> " + typo.text + " </span>" + str2;
                            }
                        }
                    }
                    //spanTypo.style.wordBreak = "break-all";
                    spanTypo.style.width = "350px";
                    spanTypo.style.height = "auto";
                    spanTypo.style.border = "1px solid #D2D4D7";
                    spanTypo.style.padding = "3px";
                    spanTypo.innerHTML = showText;
                }
                else {
                    spanTypo.innerHTML = typo.originalText;
                }
                typo.text = "";
                document.getSelection().empty();
            }, 100);
        }
        function onButtonClick(sender, e) {
            if (e.button.key == "print") {
                // printLink is defined on server
                window.open(printLink, '',
                    'scrollbars=yes,height=600,width=800,resizable=yes,toolbar=no,location=no,status=no,menubar=yes');
            }
        }

        function addEstimateReadTime() {
            var lCount = document.body.innerText.split(/\s+/g).length;
            var norma = 400;
            var duration = Math.round(lCount / norma);
            if (duration < 1) duration = 1;
            var txt = "<%=PXMessages.LocalizeNoPrefix(InfoMessages.WikiReadDuration)%>";
            if (duration == 1) txt = "<%=PXMessages.LocalizeNoPrefix(InfoMessages.WikiReadDurationOne)%>";
            var durationText = '<div class="DurationText"><div class="Text">' + duration + ' ' + txt + '</div></div>';
            var place = document.querySelector("h1.pagetitle");
            if (place) place.insertAdjacentHTML("beforeend", durationText);
        }

        window.addEventListener("load", addEstimateReadTime);
        function hideCode(e) {
            var preEl = e.closest("pre");
            var clsList = preEl.classList;
            var extraCodeEl = preEl.querySelector('span#extraCode');
            if (!extraCodeEl) {
                var txtArr = preEl.innerHTML.split(/\n/g);
                txtArr[0] += "<span id='extraCode' style='display:none'>";
                var txtText = txtArr.join("\n") + "</span>";
                preEl.innerHTML = txtText;
                extraCodeEl = preEl.querySelector('span#extraCode');
            }
            var hideEl = preEl.querySelector("span#hideCode");
            var showEl = preEl.querySelector("span#showCode");
            var cls = "HideState";
            if (clsList.contains(cls)) {
                clsList.remove(cls);
                hideEl.style.display = "";
                showEl.style.display = "none";
                extraCodeEl.style.display = "";
            }
            else {
                clsList.add(cls);
                hideEl.style.display = "none";
                showEl.style.display = "";
                extraCodeEl.style.display = "none";
            }
        }
        function addHideCodeButton() {
            var txtShow = "<%=PXMessages.LocalizeNoPrefix(InfoMessages.ShowCode) %>";
            var txtHide = "<%=PXMessages.LocalizeNoPrefix(InfoMessages.HideCode) %>";
            var template = '<div class="HideCodeDiv"><a href="javascript:void(0)" onclick="event.preventDefault(); hideCode(this);" class="HideCodeBtn"><span id="hideCode">' + txtHide + '</span><span id="showCode" style="display:none;">' + txtShow + '</span></a></div>';
            document.querySelectorAll("div.wiki pre").forEach(function (el) { if (el.innerHTML.trim().split(/\n/g).length > 1) el.insertAdjacentHTML("afterbegin", template); });
        }
        window.addEventListener("load", addHideCodeButton);

        function copyToClipboard(f) {
            var text = f.closest("pre").innerHTML.replace(/<div.*\/div>/ig, "");
            var textDiv = document.createElement("div");
            textDiv.innerHTML = text;
            var textArea = document.createElement("textarea");
            textArea.value = textDiv.innerText;

            textArea.style.top = "0";
            textArea.style.left = "0";
            textArea.style.position = "fixed";

            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();

            try {
                var successful = document.execCommand('copy');
            } catch (err) {
                console.error('Fallback: Oops, unable to copy', err);
            }

            document.body.removeChild(textArea);
        }
        function addCopyCodeButton() {
            var txt = "<%=PXMessages.LocalizeNoPrefix(InfoMessages.CopyCode) %>";
            var template = '<div class="CopyCodeDiv"><button onclick="event.preventDefault(); copyToClipboard(this);" class="CopyCodeBtn"><i class="ac main-Copy" style="font-family: FontAwesome !important;"></i> ' + txt + '</button></div>';
            document.querySelectorAll("div.wiki pre").forEach(function (el) { el.insertAdjacentHTML("afterbegin", template); });
        }
        window.addEventListener("load", addCopyCodeButton);

        window.addEventListener("load", function () {
            setTimeout(function () {
                if (document.body.offsetWidth <= 640 && window.parent) {
                    var wpb = window.parent.document.body;
                    if (wpb.querySelector("div#leftColumn").style.display != "none") {
                        wpb.querySelector("#btnSwapTofC").click();
                    }
                    document.querySelector("div#page-caption").style.display = "none";
                    document.querySelector("div.dataSource").style.display = "none";
                    document.querySelector("div.dataSource+div").style.display = "none";
                    document.querySelector("div.wiki table").style.width = "auto";
                    var toolTipEl = window.parent.document.querySelector("#tooltip");
                    if (toolTipEl) toolTipEl.style.visibility = "hidden";
                    setTimeout(function () {
                        var wikiDiv = document.body.querySelector("div.wiki");
                        if (wikiDiv) {
                            wikiDiv.style.paddingLeft = ".5em";
                            wikiDiv.style.paddingRight = ".5em";
                            wikiDiv.parentNode.parentNode.style.height = "auto";
                        }
                        document.body.querySelectorAll("pre").forEach(function (el) {
                            var w = window.screen.width - 60;
                            el.style.maxWidth = w + "px";
                        });
                        window.addEventListener("orientationchange", function () {
                            document.body.querySelectorAll("pre").forEach(function (el) {
                                var w = window.screen.width - 60;
                                el.style.maxWidth = w + "px";
                            });
                        }, false);
                    }, 100);
                }
            }, 100);
        });
        
        var typo = {
            originalText: "",
            selectionObj: null,
            text: ""
        };

        var typoToolBtn;

        function getSelectionTypo() {
            if (!typoToolBtn) {
                typoToolBtn = document.querySelector("li>div[data-cmd='saveTypoInfo']");
                if (!typoToolBtn) return;
                typoToolBtn = typoToolBtn.object;
            }
            typoToolBtn.setEnabled(false);
            if (window.getSelection) {
                var tmp1 = window.getSelection();
                if (!tmp1.focusNode) return;
                var tmp2 = tmp1.toString().trim();
                if (tmp2) {
                    typo.selectionObj = {
                        focusNode: tmp1.focusNode,
                        anchorOffset: tmp1.anchorOffset
                    }
                    typo.text = tmp2.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
                    typoToolBtn.setEnabled(true);
                }
            } else if (document.selection) {
                var tmp1 = document.selection.createRange().text.trim();
                if (tmp1) {
                    typo.text = tmp1.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
                    typo.selectionObj = null;
                    typoToolBtn.setEnabled(true);
                }
            }
            else {
                typo.selectionObj = null;
                typo.text = "";
            }
        }

        var idLoad = setInterval(function () {
            if (!typoToolBtn) {
                typoToolBtn = document.querySelector("li>div[data-cmd='saveTypoInfo']");
                if (!typoToolBtn) return;
                typoToolBtn = typoToolBtn.object;
                if (!typoToolBtn) {
                    typoToolBtn = null;
                    return;
                }
                setTimeout(function () {
                    typoToolBtn.setEnabled(false);
                    clearInterval(idLoad);
                    document.addEventListener('selectionchange', getSelectionTypo);
                    window.addEventListener('unload', function () {
                        document.removeEventListener('selectionchange', getSelectionTypo);
                    });
                }, 300);
            }
        }, 500);
	</script>
</asp:Content>
