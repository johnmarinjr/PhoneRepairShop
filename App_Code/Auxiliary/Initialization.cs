using PX.Data;
using PX.Data.Wiki.Parser;

public static class Initialization
{
	public static void ProcessApplication()
	{
		InitReports();
		DITAConversionType();
	}

	private static void InitReports()
	{
		PX.Reports.ReportFileManager.ReportsDir = "~/ReportsDefault";
		PX.Reports.ReportFileManager.CustomReportsDir = "~/ReportsCustomized";

		System.Type reportFunctionsType = System.Web.Compilation.PXBuildManager.GetType(PX.Data.Reports.ReportLauncherHelper._REPORTFUNCTIONS_TYPE, false);
		if (reportFunctionsType != null)
			PX.Common.Parser.ExpressionContext.RegisterExternalObject("Payments", System.Activator.CreateInstance(reportFunctionsType));

		System.Type commonReportFunctionsType = System.Web.Compilation.PXBuildManager.GetType(PX.Data.Reports.ReportLauncherHelper._COMMONREPORTFUNCTIONS_TYPE, false);
		if (commonReportFunctionsType != null)
			PX.Common.Parser.ExpressionContext.RegisterExternalObject("CustomFunc", System.Activator.CreateInstance(commonReportFunctionsType));


		System.Type weatherIntegrationUnitOfMeasureServiceType = System.Web.Compilation.PXBuildManager.GetType(PX.Data.Reports.ReportLauncherHelper._WEATHERINTEGRATIONUNITOFMEASURESERVICE_TYPE, false);
		if (weatherIntegrationUnitOfMeasureServiceType != null)
			PX.Common.Parser.ExpressionContext.RegisterExternalObject("WeatherIntegrationUnitOfMeasureService", System.Activator.CreateInstance(weatherIntegrationUnitOfMeasureServiceType));
	}

	private static void DITAConversionType()
	{
		PX.Data.Wiki.WikiExportCollection.RegisterWikiExport("ConversionType1", typeof(PX.Data.Wiki.ConversionType1));
		PX.Data.Wiki.WikiExportCollection.RegisterWikiExport("ConversionType2", typeof(PX.Data.Wiki.ConversionType2));
		PX.Data.Wiki.WikiExportCollection.RegisterWikiExport("ConversionType3", typeof(PX.Data.Wiki.ConversionType3));
	}
}
