using Newtonsoft.Json.Linq;
using PX.Common;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using AMMessages = PX.Web.UI.Msg;

/// <summary>
/// Provides client script configuration
/// </summary>
public class ClientSideAppsHelper
{
	
	[PXInternalUseOnly]
	private static string LocalizeString(string str)
	{
		if (string.IsNullOrEmpty(str)) return string.Empty;
		return HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(str)).Replace("\"", "\\\"");
	}

	

	[PXInternalUseOnly]
	private static string GetExternalBandle(string fileName)
	{
		string foundedString = null;

		string fileContent = File.ReadAllText(fileName);
		var fileNameArray = fileName.Split(Path.DirectorySeparatorChar);
		var bundleOfFile = fileNameArray[fileNameArray.Length - 2];

		var m = Regex.Match(fileContent, "\"controls/" + bundleOfFile + "/vendor-bundle\":(\\s+|)\\[.*?\\]", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.Compiled);
		if (m.Success)
		{
			return m.Value;
		}

		return foundedString;
	}

	[PXInternalUseOnly]
	[Obsolete]
	public static string RenderScriptConfiguration()
	{
		var lang = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
		var localizedNoResults = PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.SuggesterNothingFound);
		var cacheKey = "ClientSideAppsConfig:" + lang;
		string cachedResult = HttpContext.Current.Cache[cacheKey] as string;
		if (cachedResult != null)
		{
			return cachedResult;
		}

		var appRoot = HttpContext.Current.Request.ApplicationPath;
		if (appRoot.Equals("/"))
		{
			appRoot = "";
		}

		const string clientSideAppsPath = "/scripts/ca/";
		var clientSideAppsRoot = appRoot + clientSideAppsPath;

		var scriptDir = System.Web.HttpContext.Current.Server.MapPath("~" + clientSideAppsPath);
		var bundleFiles = Directory.GetFiles(scriptDir, "*-bundle.js", SearchOption.AllDirectories);
		var requireMetaFiles = Directory.GetFiles(scriptDir, "require-meta.js", SearchOption.AllDirectories);

		var vendorBundleTicks = File.GetLastWriteTime(Path.Combine(scriptDir, "vendor-bundle.js")).Ticks;

		var latestBundleTicks = bundleFiles.Select(f => File.GetLastWriteTime(f).Ticks).Max();

		var metaBundles = string.Join(",", requireMetaFiles.Select(GetExternalBandle).ToArray());

		var bundles = bundleFiles
			.Select(x => x.Replace(scriptDir, "").Replace("\\", "/"))
			.ToArray()
			;

		var apps = bundles.Where(b => b.Contains("app-")).ToList();
		//var resources = bundles.Where(b => b.Contains("resources-")).ToArray();
		var controls = bundles.Where(b => b.Contains("controls-")).ToArray();
		var controlBundles = controls.Select(a =>
		{
			var bundleName = a.Replace(".js", "");
			var moduleName = bundleName.Replace("controls-bundle", "index");
			return string.Format(@"""{0}"":[""{1}""]", bundleName, moduleName);
		}).ToArray();

		System.Collections.Generic.Dictionary<string, string> ManufacturingStrings = new System.Collections.Generic.Dictionary<string, string>
		{
			{ "PRODUCTION_ORDERS", LocalizeString(AMMessages.UI_PRODUCTION_ORDERS) },
			{ "WORK_CENTER", LocalizeString(AMMessages.UI_WORK_CENTER) },
			{ "MACHINE", LocalizeString(AMMessages.UI_MACHINE) },
			{ "PERIOD", LocalizeString(AMMessages.UI_PERIOD) },
			{ "OPERATION_DESCRIPTION", LocalizeString(AMMessages.UI_OPERATION_DESCRIPTION) },
			{ "PRODUCTION_ORDER_INFORMATION", LocalizeString(AMMessages.UI_PRODUCTION_ORDER_INFORMATION) },
			{ "FULLSCREEN", LocalizeString(AMMessages.UI_FULLSCREEN) },
			{ "MAXIMIZE", LocalizeString(AMMessages.UI_MAXIMIZE) },
			{ "LATE_ORDERS", LocalizeString(AMMessages.UI_LATE_ORDERS) },
			{ "Column_Configuration", LocalizeString(AMMessages.UI_Column_Configuration)},
			{ "Column_Configuration_filter", LocalizeString(AMMessages.UI_Column_Configuration_filter) },
			{ "Available_Columns", LocalizeString(AMMessages.UI_Available_Columns) },
			{ "Selected_Columns", LocalizeString(AMMessages.UI_Selected_Columns) },
			{ "Reset_To_Default", LocalizeString(AMMessages.UI_Reset_To_Default) },
			{ "Confirm", LocalizeString(AMMessages.UI_Confirm) },
			{ "Confirm_Reset_Text", LocalizeString(AMMessages.UI_Confirm_Reset_Text) },
			{ "OK", LocalizeString(AMMessages.UI_OK) },
			{ "Cancel", LocalizeString(AMMessages.UI_Cancel) },
			{ "NO_RECORDS_TO_DISPLAY", LocalizeString(AMMessages.UI_NO_RECORDS_TO_DISPLAY) },
			{ "PRESET_NAME_hourAndDay", LocalizeString(AMMessages.UI_PRESET_NAME_hourAndDay) },
			{ "PRESET_NAME_monthAndYear", LocalizeString(AMMessages.UI_PRESET_NAME_monthAndYear) },
			{ "PRESET_NAME_weekAndDay", LocalizeString(AMMessages.UI_PRESET_NAME_weekAndDay) },
			{ "PRESET_NAME_weekAndMonth", LocalizeString(AMMessages.UI_PRESET_NAME_weekAndMonth) }
		};

		var sb = new StringBuilder();
		sb.Append(@"
<script>
");
		sb.AppendFormat("var __svg_icons_path = \"{0}/Content/svg_icons/\";\n", appRoot);
		sb.Append(@"
window.ClientLocalizedStrings = {
");
		sb.AppendFormat("AM: {0}, \n", Newtonsoft.Json.JsonConvert.SerializeObject(ManufacturingStrings));
		sb.AppendFormat("currentLocale: \"{0}\", \n", System.Threading.Thread.CurrentThread.CurrentCulture.Name);
		sb.AppendFormat("noResultsFound: \"{0}\",\n", HttpUtility.HtmlDecode(localizedNoResults).Replace("\"", "\\\""));
		sb.AppendLine("lastUpdate: {");
		sb.AppendFormat("JustNow: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.LastUpdateJustNow)).Replace("\"", "\\\""));
		sb.AppendFormat("MinsAgo: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.LastUpdateMinsAgo)).Replace("\"", "\\\""));
		sb.AppendFormat("HoursAgo: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.LastUpdateHoursAgo)).Replace("\"", "\\\""));
		sb.AppendFormat("DaysAgo: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.LastUpdateDaysAgo)).Replace("\"", "\\\""));
		sb.AppendFormat("LongAgo: \"{0}\"\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.LastUpdateLongAgo)).Replace("\"", "\\\""));
		sb.AppendLine("},");
		sb.AppendLine("payment: {");
		sb.AppendFormat("Amount: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.PaymentAmount)).Replace("\"", "\\\""));
		sb.AppendFormat("TitlePay: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.PaymentTitlePay)).Replace("\"", "\\\""));
		sb.AppendFormat("TitleCreateProfile: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.PaymentTitleCreateProfile)).Replace("\"", "\\\""));
		sb.AppendFormat("CardNumber: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.PaymentCardNumber)).Replace("\"", "\\\""));
		sb.AppendFormat("ExpirationDate: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.PaymentExpirationDate)).Replace("\"", "\\\""));
		sb.AppendFormat("Cvc: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.PaymentCvc)).Replace("\"", "\\\""));
		sb.AppendFormat("Name: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.PaymentName)).Replace("\"", "\\\""));
		sb.AppendFormat("Phone: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.PaymentPhone)).Replace("\"", "\\\""));
		sb.AppendFormat("Address: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.PaymentAddress)).Replace("\"", "\\\""));
		sb.AppendFormat("AddressLine1: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.PaymentAddressLine1)).Replace("\"", "\\\""));
		sb.AppendFormat("AddressLine2: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.PaymentAddressLine2)).Replace("\"", "\\\""));
		sb.AppendFormat("Email: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.PaymentEmail)).Replace("\"", "\\\""));
		sb.AppendFormat("City: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.PaymentCity)).Replace("\"", "\\\""));
		sb.AppendFormat("State: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.PaymentState)).Replace("\"", "\\\""));
		sb.AppendFormat("Zip: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.PaymentZip)).Replace("\"", "\\\""));
		sb.AppendFormat("Pay: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.PaymentPay)).Replace("\"", "\\\""));
		sb.AppendFormat("Save: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.PaymentSave)).Replace("\"", "\\\""));
		sb.AppendFormat("ContactInfo: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.PaymentContactInfo)).Replace("\"", "\\\""));
		sb.AppendLine("},");
		sb.AppendLine("tree: {");
		sb.AppendFormat("AddSibling: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.TreeAddSibling)).Replace("\"", "\\\""));
		sb.AppendFormat("AddChild: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.TreeAddChild)).Replace("\"", "\\\""));
		sb.AppendFormat("Rename: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.TreeRename)).Replace("\"", "\\\""));
		sb.AppendFormat("Delete: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.TreeDelete)).Replace("\"", "\\\""));
		sb.AppendLine("}}");

		sb.AppendFormat(@"
window.globalControlsModules=[""{0}""];
", String.Join("\",\"", controls.Select(x => x.Replace(".js", "").Replace("/controls-bundle", ""))));

		var bundleArray = apps.Select(a =>
		{
			var bundleName = a.Replace(".js", "");
			var moduleName = bundleName.Replace("app-bundle", "main");
			return string.Format(@"""{0}"":[""{1}""]", bundleName, moduleName);
		}).Union(controlBundles)

		.ToArray();




		sb.AppendFormat(@"
requirejs = {{
	baseUrl: ""{0}"",
	paths: {{
		root: """"
	}},
	waitSeconds: 30,
	urlArgs: ""b={2}"",
	packages: [],
	stubModules: [
	""text""
	],
	shim: {{}},
	bundles: {{{1}
	,{3}
	}}

}}
</script>", clientSideAppsRoot, string.Join(",\n", bundleArray), latestBundleTicks, metaBundles);

		sb.AppendFormat(@"<script src=""{0}vendor-bundle.js?b={1}"" data-main=""apps/enhance/main"" defer></script>", clientSideAppsRoot, vendorBundleTicks);

		sb.AppendFormat(@"<!--{0}-->", System.DateTime.UtcNow);

		var result = sb.ToString();

		HttpContext.Current.Cache.Insert(cacheKey, result, new System.Web.Caching.CacheDependency(bundleFiles), System.Web.Caching.Cache.NoAbsoluteExpiration, System.Web.Caching.Cache.NoSlidingExpiration);


		return result;
	}


}
