using System;

namespace PX.Objects.PR
{
	public static class PRUtils
	{
		public static decimal Round(decimal num, int precision = 2)
		{
			return Math.Round(num, precision, MidpointRounding.AwayFromZero);
		}

		public static decimal Round(decimal? num, int precision = 2)
		{
			return Round(num.GetValueOrDefault(), precision);
		}
	}
}
