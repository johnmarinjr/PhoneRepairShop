using System;

namespace PX.Objects.PR
{
	/// <summary>
	/// PR and wildcard, to use with Like BQL operator
	/// </summary>
	public class pr_ : PX.Data.BQL.BqlString.Constant<pr_>
	{
		public pr_() : base(PRWildCard) { }
		public const string PRWildCard = "PR%";
	}

	internal static class PRSubAccountMaskConstants
	{
		public const string EarningMaskName = "PREarnings";
		public const string AlternateEarningMaskName = "PREarningsAlternate";
		public const string DeductionMaskName = "PRDeductions";
		public const string BenefitExpenseMaskName = "PRBenefitExpense";
		public const string AlternateBenefitExpenseMaskName = "PRBenefitExpenseAlternate";
		public const string TaxMaskName = "PRTaxes";
		public const string TaxExpenseMaskName = "PRTaxExpense";
		public const string AlternateTaxExpenseMaskName = "PRTaxExpenseAlternate";
		public const string PTOMaskName = "PRPTO";
		public const string PTOExpenseMaskName = "PRPTOExpense";
		public const string AlternatePTOExpenseMaskName = "PRPTOExpenseAlternate";
	}

	public static class PRQueryParameters
	{
		public const string DownloadAuf = "DbgDownloadAuf";
	}

	internal static class PRFileNames
	{
		public const string Auf = "auf.txt";
	}

	public static class PayStubsDirectDepositReportParameters
	{
		public const string ReportID = "PR641015";
		public const string BatchNbr = "DDBatchID";
	}

	public class GLAccountSubSource
	{
		public const string Branch = "B";
		public const string Employee = "E";
		public const string DeductionCode = "D";
		public const string LaborItem = "L";
		public const string PayGroup = "G";
		public const string EarningType = "R";
		public const string TaxCode = "X";
		public const string Project = "J";
		public const string Task = "T";
		public const string PTOBank = "O";
	}

	public static class DateConstants
	{
		public const byte WeeksPerYear = 52;
	}
	
	public static class WebserviceContants
	{
		// This corresponds to the maxReceivedMessageSize of the binding used by the server, defined in the WS's web.config
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		public const int MaxRequestLength = 65536;
		// An estimated ratio of the payload portion of the total SOAP envelop used for WCF communication to the WS
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		public const double PayloadRatio = 0.9;

		// Has to match the first value defined in PX.Payroll.Data.AatrixPredefinedFields in the payrollws repo
		public const int FirstAatrixPredefinedField = 4000;

		// Has to be greater than the last value defined in PX.Payroll.Data.AatrixPredefinedFields in the payrollws repo
		public const int LastAatrixPredefinedField = 4999;
		
		public const string IncludeRailroadTaxesSetting = "IncludeRailroadTaxes";
		public const string CompanyWagesYtdSetting = "CompanyWagesYtd";
		public const string CompanyWagesQtdSetting = "CompanyWagesQtd";
	}

	internal static class PRSelectionPeriodIDs
	{
		public const string LastMonth = "LastMonth";
		public const string Last12Months = "Last12Months";
		public const string CurrentQuarter = "CurrentQuarter";
		public const string CurrentCalYear = "CurrentCalYear";
		public const string CurrentFinYear = "CurrentFinYear";
	}
}
