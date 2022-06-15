<%@ Page Language="C#" MasterPageFile="~/MasterPages/ClearWorkspace.master" CodeFile="Payment.aspx.cs" AutoEventWireup="true" Inherits="Page_Show" Title="Untitled Page" %>
<%@ Register TagPrefix="a" Namespace="System.Windows.Forms" Assembly="System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" %>
<%@ MasterType VirtualPath="~/MasterPages/ClearWorkspace.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<script type="text/javascript">
		var result = window.location.href.split('?');
		const plugin = document.createElement("qp-plugin-service");
		plugin.setAttribute("config.bind", unescape(result[1]));
		plugin.setAttribute("mobileOptions", result[2]);
		document.body.appendChild(plugin);
	</script>
</asp:Content>