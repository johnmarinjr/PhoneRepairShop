using PX.Data;
using PX.Payroll.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PX.Objects.PR
{
	public class TaxSplitWageTypeListAttribute : PXIntListAttribute
	{
		public TaxSplitWageTypeListAttribute() : base(
			new int[] { TaxSplitWageType.Tips, TaxSplitWageType.Others },
			new string[] { Messages.Tips, Messages.NotTips })
		{
		}

		public override void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			base.FieldSelecting(sender, e);

			PRPayment currentPayment = (PRPayment)sender.Graph.Caches[typeof(PRPayment)].Current;
			if (currentPayment != null)
			{
				if (currentPayment.CountryID == LocationConstants.USCountryCode)
				{
					List<PRTypeMeta> wageTypes = PRTypeSelectorAttribute.GetAll<PRWage>(currentPayment.CountryID);
					_AllowedValues = wageTypes.Select(x => x.ID).ToArray();
					_AllowedLabels = wageTypes.Select(x => x.Name.ToUpper()).ToArray();
				}
				else
				{
					_AllowedValues = new int[] { TaxSplitWageType.Tips, TaxSplitWageType.Others };
					_AllowedLabels = new string[] { Messages.Tips, Messages.NotTips };
				}
			}
		}
	}
}
