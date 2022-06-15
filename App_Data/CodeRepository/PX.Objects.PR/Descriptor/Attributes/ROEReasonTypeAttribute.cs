using PX.Data;
using PX.Objects.EP;

namespace PX.Objects.PR
{
	public class ROEReason
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute()
				: base(
				new string[] { A00, A01, B00, D00, E00, E02, E03, E04, E05, E06, E09, E10, E11, F00, G00, G07, H00, J00, K00, K12, K13, K14, K15, K16, K17, M00, M08, N00, P00, Z00 },
				new string[] { Messages.A00, Messages.A01, Messages.B00, Messages.D00, Messages.E00, Messages.E02, Messages.E03, Messages.E04, Messages.E05, Messages.E06,
					Messages.E09, Messages.E10, Messages.E11, Messages.F00, Messages.G00, Messages.G07, Messages.H00, Messages.J00, Messages.K00, Messages.K12,
					Messages.K13, Messages.K14, Messages.K15, Messages.K16, Messages.K17, Messages.M00, Messages.M08, Messages.N00, Messages.P00, Messages.Z00 })
			{ }
		}

		public const string A00 = "A00";
		public const string A01 = "A01";
		public const string B00 = "B00";
		public const string D00 = "D00";
		public const string E00 = "E00";
		public const string E02 = "E02";
		public const string E03 = "E03";
		public const string E04 = "E04";
		public const string E05 = "E05";
		public const string E06 = "E06";
		public const string E09 = "E09";
		public const string E10 = "E10";
		public const string E11 = "E11";
		public const string F00 = "F00";
		public const string G00 = "G00";
		public const string G07 = "G07";
		public const string H00 = "H00";
		public const string J00 = "J00";
		public const string K00 = "K00";
		public const string K12 = "K12";
		public const string K13 = "K13";
		public const string K14 = "K14";
		public const string K15 = "K15";
		public const string K16 = "K16";
		public const string K17 = "K17";
		public const string M00 = "M00";
		public const string M08 = "M08";
		public const string N00 = "N00";
		public const string P00 = "P00";
		public const string Z00 = "Z00";

		public static string GetROEReason(string terminationReason)
		{
			switch (terminationReason)
			{
				case EPTermReason.Retirement:
					return E05;
				case EPTermReason.Layoff:
					return K00;
				case EPTermReason.TerminatedForCause:
					return M00;
				case EPTermReason.Resignation:
					return E00;
				case EPTermReason.Deceased:
				case EPTermReason.MedicalIssues:
					return D00;
			}

			return K00;
		}
	}
}
