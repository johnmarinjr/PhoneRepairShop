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

namespace PX.Objects.PO.GraphExtension.POOrderEntryExt
{
	public class POOrderDiscountEngine : PXGraphExtension<DiscountEngine<POLine, POOrderDiscountDetail>>
	{
		public static bool IsActive()
		{
			return !PXAccess.FeatureInstalled<FeaturesSet.vendorDiscounts>();
		}

		#region Overrides

		[PXOverride]
		public virtual void CalculateDocumentDiscountRate(PXCache cache,
													PXSelectBase<POLine> documentDetails,
													POLine currentLine,
													PXSelectBase<POOrderDiscountDetail> discountDetails,
													bool forceFormulaCalculation,
													Action<PXCache, PXSelectBase<POLine>, POLine, PXSelectBase<POOrderDiscountDetail>, bool> baseMethod)
		{
			if (Base.IsDocumentDiscountRateCalculationNeeded(cache, currentLine, discountDetails))
			{
				baseMethod(cache, documentDetails, currentLine, discountDetails, forceFormulaCalculation);
			}
		}

		#endregion
	}
}
