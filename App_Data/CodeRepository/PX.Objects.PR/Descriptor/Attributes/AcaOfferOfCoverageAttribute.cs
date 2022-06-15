using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR
{
	public class AcaOfferOfCoverage
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				new string[] { Code1A, Code1B, Code1C, Code1D, Code1E, Code1F, Code1G, Code1H, Code1J, Code1K, Code1L, Code1M, Code1N, Code1O, Code1P, Code1Q, Code1R, Code1S, Code1T, Code1U },
				new string[] { Code1A, Code1B, Code1C, Code1D, Code1E, Code1F, Code1G, Code1H, Code1J, Code1K, Code1L, Code1M, Code1N, Code1O, Code1P, Code1Q, Code1R, Code1S, Code1T, Code1U })
			{ }
		}

		public const string Code1A = "1A";
		public const string Code1B = "1B";
		public const string Code1C = "1C";
		public const string Code1D = "1D";
		public const string Code1E = "1E";
		public const string Code1F = "1F";
		public const string Code1G = "1G";
		public const string Code1H = "1H";
		public const string Code1J = "1J";
		public const string Code1K = "1K";
		public const string Code1L = "1L";
		public const string Code1M = "1M";
		public const string Code1N = "1N";
		public const string Code1O = "1O";
		public const string Code1P = "1P";
		public const string Code1Q = "1Q";
		public const string Code1R = "1R";
		public const string Code1S = "1S";
		public const string Code1T = "1T";
		public const string Code1U = "1U";

		private static Dictionary<string, int> _CodeOrdering = new Dictionary<string, int>()
		{
			{ Code1A, 1 },
			{ Code1B, 4 },
			{ Code1C, 2 },
			{ Code1D, 2 },
			{ Code1E, 3 },
			{ Code1F, 5 },
			{ Code1G, 6 },
			{ Code1H, 1000 },
			{ Code1J, 1000 },
			{ Code1K, 1000 },
			{ Code1L, 1000 },
			{ Code1M, 1000 },
			{ Code1N, 1000 },
			{ Code1O, 1000 },
			{ Code1P, 1000 },
			{ Code1Q, 1000 },
			{ Code1R, 1000 },
			{ Code1S, 1000 },
			{ Code1T, 1000 },
			{ Code1U, 1000 },
		};

		public static int Compare(string a, string b)
		{
			if (!_CodeOrdering.TryGetValue(a, out int priA))
			{
				throw new PXException(Messages.UnknownOfferOfCoverageCode, a);
			}
			if (!_CodeOrdering.TryGetValue(b, out int priB))
			{
				throw new PXException(Messages.UnknownOfferOfCoverageCode, b);
			}

			return b.CompareTo(a);
		}

		public static string GetDefault()
		{
			return Code1H;
		}

		public delegate bool EmployeeAlwaysPartTimeDelegate();
		public static string GetDeductionOfferOfCoverage(IEnumerable<PRAcaDeductCoverageInfo> healthPlanTypes, EmployeeAlwaysPartTimeDelegate partTimeDelegate = null)
		{
			string eeHealthPlanType = healthPlanTypes.FirstOrDefault(x => x.CoverageType == AcaCoverageType.Employee)?.HealthPlanType;
			string spouseHealthPlanType = healthPlanTypes.FirstOrDefault(x => x.CoverageType == AcaCoverageType.Spouse)?.HealthPlanType;
			string childrenHealthPlanType = healthPlanTypes.FirstOrDefault(x => x.CoverageType == AcaCoverageType.Children)?.HealthPlanType;


			// 1A: MEC coverage that meets MV was offered to EE, spouse and children
			if (eeHealthPlanType == AcaHealthPlanType.MeetsEssentialCoverageAndValue &&
				spouseHealthPlanType == AcaHealthPlanType.MeetsEssentialCoverageAndValue &&
				childrenHealthPlanType == AcaHealthPlanType.MeetsEssentialCoverageAndValue)
			{
				return Code1A;
			}
			// 1C: MEC coverage that meets MV was offered to EE and children
			else if (eeHealthPlanType == AcaHealthPlanType.MeetsEssentialCoverageAndValue && childrenHealthPlanType == AcaHealthPlanType.MeetsEssentialCoverageAndValue)
			{
				return Code1C;
			}
			// 1D: MEC coverage that meets MV was offered to EE and spouse
			else if (eeHealthPlanType == AcaHealthPlanType.MeetsEssentialCoverageAndValue && spouseHealthPlanType == AcaHealthPlanType.MeetsEssentialCoverageAndValue)
			{
				return Code1D;
			}
			// 1E: MEC coverage that meets MV was offered to EE, MEC coverage that doesn't meet MV was offered to spouse and children
			else if (eeHealthPlanType == AcaHealthPlanType.MeetsEssentialCoverageAndValue &&
				spouseHealthPlanType == AcaHealthPlanType.MeetsEssentialCoverage &&
				childrenHealthPlanType == AcaHealthPlanType.MeetsEssentialCoverage)
			{
				return Code1E;
			}
			// 1B: MEC coverage that meets MV was offered to EE
			else if (eeHealthPlanType == AcaHealthPlanType.MeetsEssentialCoverageAndValue)
			{
				return Code1B;
			}
			// 1F: MEC coverage that doesn't meet MV was offered to EE
			else if (eeHealthPlanType == AcaHealthPlanType.MeetsEssentialCoverage)
			{
				return Code1F;
			}
			else
			{
				if (eeHealthPlanType == AcaHealthPlanType.SelfInsured && partTimeDelegate != null && partTimeDelegate())
				{
					// 1G: Self insured coverage was offered to the employee who is not full time at any point in the year
					return AcaOfferOfCoverage.Code1G;
				}

				// Anything else
				return AcaOfferOfCoverage.GetDefault();
			}
		}

		public static bool MeetsMinimumCoverageRequirement(string code)
		{
			return code == Code1A || code == Code1C || code == Code1E || code == Code1J || code == Code1K
				|| code == Code1M || code == Code1N || code == Code1P || code == Code1Q;
		}

		public static bool MeetsMinimumCoverageRequirement(IEnumerable<PRAcaDeductCoverageInfo> healthPlanTypes)
		{
			return MeetsMinimumCoverageRequirement(GetDeductionOfferOfCoverage(healthPlanTypes));
		}
	}
}
