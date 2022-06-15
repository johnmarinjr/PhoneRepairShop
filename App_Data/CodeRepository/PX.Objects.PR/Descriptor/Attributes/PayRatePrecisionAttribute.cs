using PX.Data;

namespace PX.Objects.PR
{
	public class PayRatePrecisionAttribute : PXEventSubscriberAttribute
	{
		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			short? payRateDecimalPlaces = PRSetupHelper.GetPayrollPreferences(sender.Graph)?.PayRateDecimalPlaces;

			if (payRateDecimalPlaces != null)
			{
				PXDBDecimalAttribute.SetPrecision(sender, FieldName, payRateDecimalPlaces);
				PXDecimalAttribute.SetPrecision(sender, FieldName, payRateDecimalPlaces);
			}
		}
	}
}
