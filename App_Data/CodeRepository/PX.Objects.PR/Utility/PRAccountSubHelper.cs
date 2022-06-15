using PX.Data;
using PX.Data.BQL.Fluent;
using System;
using System.Linq;

namespace PX.Objects.PR
{
	public class PRAccountSubHelper
	{
		public static bool IsVisiblePerSetup<TExpenseAcctDefault, TExpenseSubMask>(PXGraph graph, string compareValue)
			where TExpenseAcctDefault : IBqlField
			where TExpenseSubMask : IBqlField
		{
			PXCache setupCache = graph.Caches[typeof(PRSetup)];
			PRSetup payrollPreferences = setupCache?.Current as PRSetup ??
				new SelectFrom<PRSetup>.View(graph).SelectSingle();

			if (payrollPreferences == null)
			{
				return false;
			}
			if (compareValue.Equals(setupCache.GetValue(payrollPreferences, typeof(TExpenseAcctDefault).Name)))
			{
				return true;
			}

			if (setupCache != null)
			{
				PRSubAccountMaskAttribute subMaskAttribute = setupCache.GetAttributesOfType<PRSubAccountMaskAttribute>(payrollPreferences, typeof(TExpenseSubMask).Name).FirstOrDefault();
				if (subMaskAttribute != null)
				{
					string subMask = (string)setupCache.GetValue(payrollPreferences, typeof(TExpenseSubMask).Name);
					PRDimensionMaskAttribute dimensionMaskAttribute = subMaskAttribute.GetAttribute<PRDimensionMaskAttribute>();
					if (dimensionMaskAttribute != null)
					{
						return dimensionMaskAttribute.GetSegmentMaskValues(subMask).Contains(compareValue);
					}
				}
			}

			return false;
		}
	}
}
