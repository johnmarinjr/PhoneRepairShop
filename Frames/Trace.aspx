<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Trace.aspx.cs" Inherits="Frames_Trace" %>
<%@ Register TagPrefix="px" TagName="TraceItem" Src="~/Controls/TraceItem.ascx" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
	<title>Error Trace</title>
	<meta http-equiv="content-script-type" content="text/javascript">
	<style type="text/css"> 
		body {font-family:"Verdana";font-weight:normal;font-size: .7em;color:black;} 
		b {font-family:"Verdana";font-weight:bold;color:black;margin-top: -5px}
		H1 { font-family:"Verdana";font-weight:normal;font-size:18pt;color:maroon; font-weight:600; padding-left:10px; padding-top:10px }
		.version {color: gray;}
		.controls { padding-right:10px; background-color:White;text-align:right; padding-bottom:10px; }
		.button { width:auto; border: 1px ridge #C9C9C9; cursor:pointer; padding:2px; font-weight:bold; color:navy; }

	</style> 
	<script type="text/javascript">
		function repaintImage(elem, url)
		{
			var ar = url.split('@'), innerDiv = elem.getElementsByTagName("div")[0];
			var css1 = "sprite-icon " + ar[0] + "-icon", css = elem.className;
			var css2 = ar[0] + "-icon-img " + ar[0] + "-" + ar[1];

			var i1 = css.indexOf("sprite-"), i2 = css.lastIndexOf("-icon");
			var newCss = [css.substring(0, i1), css1, css.substring(i2 + 6)];
			newCss = newCss.join(" ").trim();

			if (newCss != css) elem.className = newCss;
			elem.setAttribute("icon", ar[1]);
			if (innerDiv.className != css2) innerDiv.className = css2;

			return elem;
		}

		function SendScript(sender)
		{
			var parent = sender.parentNode;

			var tagList = parent.getElementsByTagName('input');
			var elem = tagList.item(0);
			elem.click();
		}

		function Togle(imgBtn)
		{
			var parent = imgBtn.parentNode;
			var elem = parent.getElementsByTagName("div")[2];

			if (elem.style.display == 'none')
			{
				elem.style.display = '';
				repaintImage(imgBtn, "tree@Collapse");
			}
			else
			{
				elem.style.display = 'none';
				repaintImage(imgBtn, "tree@Expand");
			}
		}
	</script>
	<script type="text/javascript">
		function ExpandAll()
		{
			var tagList = document.getElementsByName('outputDiv');
			for (var i = 0; i < tagList.length; i++)
			{
				var elem = tagList.item(i);
				elem.style.display = '';
			}

			var tagList = document.getElementsByName('outputImg');
			for (var i = 0; i < tagList.length; i++)
			{
				var elem = tagList.item(i);
				repaintImage(elem, "tree@Collapse");
			}
		}
		function CollapseAll()
		{
			var tagList = document.getElementsByName('outputDiv');
			for (var i = 0; i < tagList.length; i++)
			{
				var elem = tagList.item(i);
				elem.style.display = 'none';
			}

			var tagList = document.getElementsByName('outputImg');
			for (var i = 0; i < tagList.length; i++)
			{
				var elem = tagList.item(i);
				repaintImage(elem, "tree@Expand");
			}
		}
		function ReportIncident(incId) {
			var opt = {
				title: '<span style="font-size:20px;">Logs Submitted</span> <span style="float: right;cursor: pointer;font-size:16px;" id="closeBtn">✕</span>',
				body: '<p>Technical details about the last actions have been submitted to Acumatica Inc. If you want to contact your Acumatica support provider about this log, save the following trace log ID:</p> <span style="font-weight: bold;margin-right:6px;padding-top:3px;">' + incId + '</span>',
				buttons: {
					elements: []
				}
			}
			adialog.dialog(opt);
		}
	</script>
	<script type='text/javascript'>
		(function (root, factory) {
			'use strict';
			window.adialog = factory(root.jQuery);
		}(this, function init($, undefined) {
			var exports = {};

			var templates = {
				contStyle: " \
		display: flex; \
		font: 13px Arial; \
		position: fixed; \
		align-items: center; \
		justify-content: center; \
		z-index: 1; \
		left: 0; \
		top: 0; \
		width: 100%; \
		height: 100%; \
		overflow: auto; \
		background-color: rgba(0,0,0,0.15);",

				boxStyle: ".dialog-lib-boxStyle { \
		background-color: transparent; \
		margin: auto; \
		border: 0px solid #888; \
		min-width: 300px; \
		max-width: 800px; \
		min-height: 100px; \
		display: flex; \
		position: absolute; \
		flex-direction: column; \
		justify-content: space-between; \
		border-radius: 10px; \
		box-shadow: 1px 1px 4px 2px silver; \
		animation: traffic .2s forwards; \
	}",

				titleStyle: ".dialog-lib-titleStyle { \
		padding: 20px 20px 0px 20px; \
		font-weight: bold; \
		font-size: 15px; \
	}",
				bodyStyle: ".dialog-lib-bodyStyle { \
		padding: 20px 20px 10px 20px; \
		font-size: 16px; \
	}",
				buttonsStyle: ".dialog-lib-buttonsStyle { \
		justify-content: flex-end; \
		padding: 20px 20px 20px 20px; \
		display: flex; \
		gap: 10px; \
	}",
				btnStyle: ".dialog-lib-btnStyle { \
		color: white; \
		background-color: #027acc; \
		border: solid 1px #027acc; \
		border-radius:8px; cursor: pointer; min-width: 80px; \
		height: 40px; \
		font-size: 15px; \
		font-weight: 400; \
	} \
	.dialog-lib-btnStyle:hover { \
		background-color: #3399cc; \
	} \
	"
			}

			exports.dialog = function (p) {
				if (!p) return;

				var boxEl, titleEl, bodyEl, buttonsEl;

				const contEl = document.createElement("DIV");

				if (p.style && p.style.container) contEl.setAttribute("style", p.style.container);
				else contEl.setAttribute("style", templates.contStyle);

				const styleEl = document.createElement("STYLE");
				styleEl.innerText = "@keyframes traffic { \
		100%{background: #fefefe; } \
		}"+ templates.boxStyle + templates.bodyStyle + templates.titleStyle + templates.buttonsStyle + templates.btnStyle;
				contEl.appendChild(styleEl);

				boxEl = document.createElement("DIV");

				if (p.style && p.style.box) boxEl.setAttribute("style", p.style.box);
				boxEl.classList.add("dialog-lib-boxStyle");

				contEl.appendChild(boxEl);

				if (p.title) {
					titleEl = document.createElement("DIV");
					if (p.style && p.style.title) titleEl.setAttribute("style", p.style.title);
					titleEl.classList.add("dialog-lib-titleStyle");
					titleEl.innerHTML = p.title;
					boxEl.appendChild(titleEl);

					closeBtn = titleEl.lastChild;
					if (closeBtn) {
						closeBtn.addEventListener("click", function () {
							document.body.removeChild(contEl);
						});
					}
				}

				if (p.body) {
					bodyEl = document.createElement("DIV");
					bodyEl.innerHTML = p.body;
					if (p.style && p.style.body) bodyEl.setAttribute("style", p.style.body);
					bodyEl.classList.add("dialog-lib-bodyStyle");
					boxEl.appendChild(bodyEl);

					spanIncId = bodyEl.lastChild;
					incId = spanIncId.innerHTML;
					if (spanIncId) {
						const img = document.createElement("div");
						img.innerHTML = '<div class="sprite-icon main-icon" icon="Copy" style="padding-bottom:0.2em;"><div class="main-icon-img main-Copy"></div></div>';
						img.style = "cursor:pointer;display:inline;";
						img.addEventListener("click", function () {
							navigator.clipboard.writeText(spanIncId.innerHTML);
						});
						bodyEl.appendChild(img);
					}
				}

				if (p.buttons) {
					buttonsEl = document.createElement("DIV");
					if (p.buttons.style) buttonsEl.setAttribute("style", p.buttons.style);
					buttonsEl.classList.add("dialog-lib-buttonsStyle");

					if (p.buttons.elements) {
						p.buttons.elements.forEach(function (el) {
							const btn = document.createElement("BUTTON");
							btn.classList.add("dialog-lib-btnStyle");
							btn.style = el.style;
							btn.innerHTML = el.text || "button";
							if (el.click) btn.addEventListener("click", el.click);
							btn.addEventListener("click", function () {
								document.body.removeChild(contEl);
							});
							buttonsEl.appendChild(btn);
						});
					}
					boxEl.appendChild(buttonsEl);
				}

				document.body.appendChild(contEl);
				return ({ boxEl, titleEl, bodyEl, buttonsEl });
			}
			return exports;
		}));
	</script>
</head>
<body style=" background-color:white">
	<form id="frm" runat="server" class="allowSelect">
		<span>
			<h1>
				<px:PXLabel runat="server" ID="lblTraceCaption" Text="Acumatica Trace:" SkinID="Transparent" />
				<asp:Button runat="server" ID="btnReportIncident" Text="SUBMIT LOGS" OnClick="btnReportIncident_Click" CssClass="login_button" />
			</h1>
			<hr width="99%" size="2" color="#999999" />
		</span> 
		<div class="controls">
			<span aligm="center" class="button" onclick="ExpandAll()" >
					<px:PXImage runat="server" ID="imExpandAll" ImageUrl="main@ArrowDown" />
					<span style="text-decoration:underline">Expand All</span>
			</span>
			&nbsp
			<span aligm="center" class="button" onclick="CollapseAll()" >
					<px:PXImage runat="server" ID="imCollapseAll" ImageUrl="main@ArrowUp" />
					<span style="text-decoration:underline">Collapse All</span>
			</span>
		</div> 
		<div id="placeholder" runat="server" style="Width:100%; Height:100%; background-color:White;" >
		</div>
		<br />
		<span>
			<hr width="99%" size="2" color="#999999" />
			<b>Version:</b>
			<asp:Label ID="lblVersion" runat="server" Text="Acumatca 0.00" /> &nbsp&nbsp
			<px:PXLabel runat="server" ID="lblCustList" Style="font-weight:bold; color:black" Text="Customization:" />
			<asp:Label ID="lblCustomization" runat="server" />
		</span> 
	</form>
</body>
</html>
