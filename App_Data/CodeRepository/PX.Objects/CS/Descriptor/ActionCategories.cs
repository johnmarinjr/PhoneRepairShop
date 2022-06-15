using System;
using PX.Data;
using PX.Common;

namespace PX.Objects.CS
{
	[PXLocalizable(Messages.Prefix)]
	public static class ActionCategories
	{
		// Add your messages here as follows (see line below):
		// public const string YourMessage = "Your message here.";

		#region Category Names for non-workflow screens
		public const string Processing = "Processing";
		public const string DocumentProcessing = "Document Processing";
		public const string PeriodManagement = "Period Management";
		public const string CompanyManagement = "Company Management";
		public const string ReportManagement = "Report Management";
		public const string Other = "Other";
		#endregion

		public static string GetLocal(string message)
		{
			return PXLocalizer.Localize(message, typeof(Messages).FullName);
		}
	}
}
