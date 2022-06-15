namespace PX.Objects.Extensions.MultiCurrency
{
	public static class PXCurrencyHelperExtension
	{
		public static decimal RoundCury(this IPXCurrencyHelper pXCurrencyHelper, decimal val)
		{
			return pXCurrencyHelper.GetDefaultCurrencyInfo()?.RoundCury(val) ?? val;
		}
	}
}