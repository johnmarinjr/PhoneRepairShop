using PX.Data;
using PX.Payroll.Data;

namespace PX.Objects.PR
{
	public class TaxAttributeStateAttribute : PXEventSubscriberAttribute, IPXFieldSelectingSubscriber
	{
		public void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			if (e.Row != null)
			{
				PRTaxCode parent = PXParentAttribute.SelectParent<PRTaxCode>(sender, e.Row);
				sender.SetValue(e.Row, _FieldOrdinal, string.IsNullOrEmpty(parent?.TaxState)
					? parent?.CountryID == LocationConstants.USCountryCode ? LocationConstants.USFederalStateCode : LocationConstants.CanadaFederalStateCode
					: parent.TaxState);
			}
		}
	}
}
