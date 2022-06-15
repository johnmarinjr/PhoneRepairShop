using PX.Common;
using PX.Data;
using PX.Objects.Common.Discount;
using PX.Objects.Common.Discount.Mappers;
using PX.Objects.CS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.SO.GraphExtensions.SOOrderEntryExt
{
	public class SOOrderDiscountEngine : PXGraphExtension<DiscountEngine<SOLine, SOOrderDiscountDetail>>
	{
		public static bool IsActive()
		{
			return !PXAccess.FeatureInstalled<FeaturesSet.customerDiscounts>();
		}

		#region Overrides

		[PXOverride]
		public virtual void CalculateDocumentDiscountRate(PXCache cache,
													PXSelectBase<SOLine> documentDetails,
													SOLine currentLine,
													PXSelectBase<SOOrderDiscountDetail> discountDetails,
													bool forceFormulaCalculation,
													Action<PXCache, PXSelectBase<SOLine>, SOLine, PXSelectBase<SOOrderDiscountDetail>, bool> baseMethod)
		{
			if (Base.IsDocumentDiscountRateCalculationNeeded(cache, currentLine, discountDetails))
			{
				baseMethod(cache, documentDetails, currentLine, discountDetails, forceFormulaCalculation);
			}
		}

		#endregion
	}
}
